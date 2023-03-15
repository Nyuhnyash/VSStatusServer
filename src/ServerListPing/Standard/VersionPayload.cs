namespace StatusServer.ServerListPing.Standard
{
    public class VersionPayload
    {
        public readonly int Protocol = 2000;

        public string Name;

        public static implicit operator VersionPayload(string name)
        {
            return new VersionPayload { Name = name };
        }
    }
}
