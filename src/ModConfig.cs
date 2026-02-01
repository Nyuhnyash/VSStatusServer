using System;
using System.Collections.Generic;


namespace StatusServer
{
    public class ModConfig
    {
        public ushort Port { get; set; }
        
        public string IconFile { get; set; }
        
        public int StartDelaySeconds { get; set; }
        
        private IEnumerable<string> _enabledExtensions;

        public IEnumerable<string> EnabledExtensions
        {
            get => _enabledExtensions ?? Array.Empty<string>();
            set => _enabledExtensions = value;
        }
        
        // Server options
        
        /// <summary>
        /// Client connection timeout in milliseconds.
        /// </summary>
        public int ClientTimeoutMs { get; set; }
        
        /// <summary>
        /// Maximum pending connections in the listen backlog.
        /// </summary>
        public int Backlog { get; set; }
        
        /// <summary>
        /// Maximum number of concurrent client connections.
        /// </summary>
        public int MaxConcurrentConnections { get; set; }
        
        /// <summary>
        /// Enable rate limiting per IP address.
        /// </summary>
        public bool EnableRateLimiting { get; set; }
        
        /// <summary>
        /// Rate limit time window in seconds.
        /// </summary>
        public int RateLimitWindowSeconds { get; set; }
        
        /// <summary>
        /// Maximum requests allowed per IP within the rate limit window.
        /// </summary>
        public int RateLimitMaxRequests { get; set; }

        public static ModConfig Default => new ModConfig
        {
            Port = 25565,
            IconFile = "server-icon.png",
            StartDelaySeconds = 10,
            EnabledExtensions = new List<string> { "world" },
            
            // Server defaults
            ClientTimeoutMs = 5000,
            Backlog = 10,
            MaxConcurrentConnections = 50,
            EnableRateLimiting = true,
            RateLimitWindowSeconds = 60,
            RateLimitMaxRequests = 30,
        };
    }
}
