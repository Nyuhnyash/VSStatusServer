namespace StatusServer.ServerListPing.Standard
{
    public class DescriptionPayload
    {
        public string Text { get; set; }

        public static implicit operator DescriptionPayload(string text)
        {
            return new DescriptionPayload { Text = text };
        }
    }
}
