using System.Collections.Generic;


namespace StatusServer.ServerListPing.Standard
{
    public class PlayersPayload
    {
        public int Max { get; set; }

        public int Online { get; set; }

        public IEnumerable<PlayerPayload> Sample { get; set; }
    }
}
