namespace StatusServer.ServerListPing.Standard
{
    public class StatusPayload
    {
        public VersionPayload Version;

        public PlayersPayload Players;
        
        public DescriptionPayload Description;

        /// <summary>
        /// Server icon, encoded in Base64
        /// </summary>
        public string Favicon;
    }
}
