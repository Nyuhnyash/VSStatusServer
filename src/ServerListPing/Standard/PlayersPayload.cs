using System.Collections.Generic;


namespace StatusServer.ServerListPing.Standard
{
    public class PlayersPayload
    {
        public int Max;

        public int Online;

        public IEnumerable<PlayerPayload> Sample;
    }
}
