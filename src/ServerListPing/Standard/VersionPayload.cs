namespace StatusServer.ServerListPing.Standard
{
    public class VersionPayload
    {
        public int Protocol { get; } = 2000;

        public string Name { get; set; }

        public static implicit operator VersionPayload(string name)
        {
            return new VersionPayload { Name = name };
        }
    }
}
