namespace StatusServer.ServerListPing.Standard
{
    public class StatusPayload
    {
        public VersionPayload Version { get; set; }

        public PlayersPayload Players { get; set; }
        
        public DescriptionPayload Description { get; set; }

        /// <summary>
        /// Server icon, encoded in Base64
        /// </summary>
        public string Favicon { get; set; }
    }
}
