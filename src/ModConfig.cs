using System;
using System.Collections.Generic;


namespace StatusServer
{
    public class ModConfig
    {
        public ushort Port;
        public string IconFile;
        private IEnumerable<string> _enabledExtensions;

        public IEnumerable<string> EnabledExtensions
        {
            get { return _enabledExtensions ?? Array.Empty<string>(); }
            set { _enabledExtensions = value; }
        }

        public static ModConfig Default
        {
            get { return new ModConfig
            {
                Port = 25565,
                IconFile = "server-icon.png",
                EnabledExtensions = new List<string> { "world" },
            }; }
        }
    }
}
