using System;
using StatusServer.ServerListPing.Standard;


namespace StatusServer
{
    public interface IStatusServer : IDisposable
    {
        void Start();
        
        Func<StatusPayload> GetStatusPayload { get; set; }
    }
}
