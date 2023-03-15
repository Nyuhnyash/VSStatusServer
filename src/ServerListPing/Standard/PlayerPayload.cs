using System;
using System.Security.Cryptography;
using System.Text;


namespace StatusServer.ServerListPing.Standard
{
    public class PlayerPayload
    {
        private static readonly MD5 _md5 = MD5.Create();

        public Guid Id;

        public string Name;

        public PlayerPayload(string name, string hashSource)
        {
            Name = name;
            Id = new Guid(_md5.ComputeHash(Encoding.UTF8.GetBytes(hashSource)));
        }
    }
}
