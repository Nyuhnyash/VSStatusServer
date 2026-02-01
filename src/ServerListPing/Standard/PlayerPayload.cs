using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;


namespace StatusServer.ServerListPing.Standard
{
    public class PlayerPayload
    {
        // ThreadLocal ensures thread-safety for MD5 computation
        private static readonly ThreadLocal<MD5> _md5 = new ThreadLocal<MD5>(() => MD5.Create());

        public Guid Id { get; set; }

        public string Name { get; set; }

        public PlayerPayload(string name, string hashSource)
        {
            Name = name;
            Id = new Guid(_md5.Value.ComputeHash(Encoding.UTF8.GetBytes(hashSource)));
        }
    }
}
