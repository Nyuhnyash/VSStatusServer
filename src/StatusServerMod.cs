using System;
using System.IO;
using System.Linq;
using StatusServer.ServerListPing;
using StatusServer.ServerListPing.Standard;
using StatusServer.ServerListPing.Standard.Extension;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

[assembly: ModInfo("Status Server", Side = "Server")]

namespace StatusServer
{
    
    public class StatusServerMod : ModSystem
    {
        private IStatusServer _statusServer;

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
            
            var payload = new ExtendedStatusPayload
            {
                Version = (string)typeof(GameVersion).GetField(nameof(GameVersion.ShortGameVersion)).GetRawConstantValue(),
                Players = new PlayersPayload { Max = api.Server.Config.MaxClients },
                Description = api.Server.Config.ServerName,
            };

            if (File.Exists(config.IconFile))
            {
                payload.Favicon = String.Format("data:image/png;base64,{0}",
                    Convert.ToBase64String(File.ReadAllBytes(config.IconFile)));
            }

            _statusServer = new StatusTcpServer(Mod.Logger, config.Port);
            _statusServer.GetStatusPayload = () => {
                var players = api.World.AllOnlinePlayers
                    .Select(player => new PlayerPayload(player.PlayerName, player.PlayerUID))
                    .ToArray();
        
                payload.Players.Online = players.Length;
                payload.Players.Sample = players;

                if (config.EnabledExtensions.Contains("world"))
                {
                    payload.World = new WorldPayload
                    {
                        Datetime = api.World.Calendar.PrettyDate(),
                    };
                }
                
                return payload;
            };

            try {
                _statusServer.Start();
            }
            catch (Exception e)
            {
                Mod.Logger.Error(e.Message + e.StackTrace);
                return;
            }

            Mod.Logger.Notification(Lang.Get("Status server started on port {0}", config.Port));

        }

        public override void Dispose()
        {
            _statusServer.Dispose();
            Mod.Logger.Notification(Lang.Get("Status server stopped"));
        }

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side.IsServer();
        }
    }
}
