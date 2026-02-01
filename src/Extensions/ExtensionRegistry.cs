using System;
using System.Collections.Generic;
using System.Linq;
using StatusServer.ServerListPing.Standard.Extension;
using Vintagestory.API.Common;
using Vintagestory.API.Server;


namespace StatusServer.Extensions
{
    /// <summary>
    /// Registry for status extensions. 
    /// Manages enabled extensions and applies them to payloads.
    /// </summary>
    public class ExtensionRegistry
    {
        private readonly List<IStatusExtension> _enabledExtensions = new List<IStatusExtension>();
        private readonly ILogger _logger;
        
        public ExtensionRegistry(ILogger logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// Registers and enables extensions based on configuration.
        /// </summary>
        /// <param name="enabledNames">Names of extensions to enable</param>
        public void RegisterExtensions(IEnumerable<string> enabledNames)
        {
            var enabledSet = new HashSet<string>(enabledNames, StringComparer.OrdinalIgnoreCase);
            
            // Register built-in extensions
            var builtInExtensions = new IStatusExtension[]
            {
                new WorldExtension(),
                // Add more built-in extensions here
            };
            
            foreach (var extension in builtInExtensions)
            {
                if (enabledSet.Contains(extension.Name))
                {
                    _enabledExtensions.Add(extension);
                    _logger.Debug($"Enabled status extension: {extension.Name}");
                }
            }
            
            _logger.Notification($"Loaded {_enabledExtensions.Count} status extension(s)");
        }
        
        /// <summary>
        /// Registers a custom extension at runtime.
        /// </summary>
        /// <param name="extension">The extension to register</param>
        public void RegisterCustomExtension(IStatusExtension extension)
        {
            if (extension == null) throw new ArgumentNullException(nameof(extension));
            
            // Check for duplicate names
            if (_enabledExtensions.Any(e => e.Name.Equals(extension.Name, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.Warning($"Extension '{extension.Name}' is already registered, skipping");
                return;
            }
            
            _enabledExtensions.Add(extension);
            _logger.Debug($"Registered custom extension: {extension.Name}");
        }
        
        /// <summary>
        /// Applies all enabled extensions to the payload.
        /// </summary>
        /// <param name="payload">The payload to modify</param>
        /// <param name="api">The server API</param>
        public void ApplyExtensions(ExtendedStatusPayload payload, ICoreServerAPI api)
        {
            foreach (var extension in _enabledExtensions)
            {
                try
                {
                    extension.Apply(payload, api);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error applying extension '{extension.Name}': {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Gets the count of enabled extensions.
        /// </summary>
        public int EnabledCount => _enabledExtensions.Count;
    }
}
