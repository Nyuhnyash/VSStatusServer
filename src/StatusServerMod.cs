using System;
using System.IO;
using System.Linq;
using StatusServer.Extensions;
using StatusServer.ServerListPing;
using StatusServer.ServerListPing.Standard;
using StatusServer.ServerListPing.Standard.Extension;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

[assembly: ModInfo("Status Server Revised", Side = "Server")]

namespace StatusServer
{
    public class StatusServerMod : ModSystem
    {
        private IStatusServer _statusServer;
        private ExtensionRegistry _extensionRegistry;

        /// <summary>
        /// Gets the extension registry for registering custom extensions.
        /// Available after StartServerSide is called.
        /// </summary>
        public ExtensionRegistry ExtensionRegistry => _extensionRegistry;

        public override void StartServerSide(ICoreServerAPI api)
        {
            var configFile = Mod.Info.ModID + ".json";
            var config = api.LoadModConfig<ModConfig>(configFile);
            if (config == null)
            {
                api.StoreModConfig(config = ModConfig.Default, configFile);
            }
            
            if (config.Port == api.Server.Config.Port)
            {
                Mod.Logger.Error(Lang.Get("You can't reuse the game server port ({0})", config.Port));
                return;
            }

            // Initialize extension registry
            _extensionRegistry = new ExtensionRegistry(Mod.Logger);
            _extensionRegistry.RegisterExtensions(config.EnabledExtensions);

            // Cache static values that don't change during runtime
            var gameVersion = GameVersion.ShortGameVersion;
            var maxClients = api.Server.Config.MaxClients;
            var serverName = api.Server.Config.ServerName;

            // Load and cache favicon
            string favicon = null;
            if (File.Exists(config.IconFile))
            {
                try
                {
                    favicon = string.Format("data:image/png;base64,{0}",
                        Convert.ToBase64String(File.ReadAllBytes(config.IconFile)));
                }
                catch (Exception ex)
                {
                    Mod.Logger.Warning("Failed to load server icon: " + ex.Message);
                }
            }

            // Build server options from config
            var serverOptions = new ServerOptions
            {
                ClientTimeoutMs = config.ClientTimeoutMs,
                Backlog = config.Backlog,
                MaxConcurrentConnections = config.MaxConcurrentConnections,
                EnableRateLimiting = config.EnableRateLimiting,
                RateLimitWindowSeconds = config.RateLimitWindowSeconds,
                RateLimitMaxRequests = config.RateLimitMaxRequests,
            };

            _statusServer = new StatusTcpServer(Mod.Logger, config.Port, serverOptions);
            _statusServer.GetStatusPayload = () => BuildStatusPayload(
                api, gameVersion, maxClients, serverName, favicon, _extensionRegistry);

            var delayMs = config.StartDelaySeconds * 1000;
            if (delayMs > 0)
            {
                Mod.Logger.Notification(Lang.Get("Mod started. Listener will start in {0} seconds on port {1}", config.StartDelaySeconds, config.Port));
                api.Event.RegisterCallback(_ => StartStatusServer(config.Port), delayMs);
            }
            else
            {
                Mod.Logger.Notification(Lang.Get("Mod started"));
                StartStatusServer(config.Port);
            }
        }

        /// <summary>
        /// Builds a fresh StatusPayload for each request (thread-safe).
        /// </summary>
        private static ExtendedStatusPayload BuildStatusPayload(
            ICoreServerAPI api,
            string gameVersion,
            int maxClients,
            string serverName,
            string favicon,
            ExtensionRegistry extensionRegistry)
        {
            var payload = new ExtendedStatusPayload
            {
                Version = gameVersion,
                Players = new PlayersPayload { Max = maxClients },
                Description = serverName,
                Favicon = favicon,
            };

            // Get current online players
            var onlinePlayers = api.World?.AllOnlinePlayers;
            if (onlinePlayers != null)
            {
                var players = onlinePlayers
                    .Select(player => new PlayerPayload(player.PlayerName, player.PlayerUID))
                    .ToArray();
        
                payload.Players.Online = players.Length;
                payload.Players.Sample = players;
            }
            else
            {
                payload.Players.Online = 0;
                payload.Players.Sample = Array.Empty<PlayerPayload>();
            }

            // Apply all enabled extensions
            extensionRegistry.ApplyExtensions(payload, api);
            
            return payload;
        }

        private void StartStatusServer(ushort port)
        {
            try
            {
                _statusServer.Start();
            }
            catch (Exception ex)
            {
                Mod.Logger.Error("Failed to start status server: " + ex);
                return;
            }

            Mod.Logger.Notification(Lang.Get("Listening on port {0}", port));
        }

        public override void Dispose()
        {
            _statusServer?.Dispose();
            Mod.Logger.Notification(Lang.Get("Listener stopped"));
        }

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side.IsServer();
        }
    }
}
