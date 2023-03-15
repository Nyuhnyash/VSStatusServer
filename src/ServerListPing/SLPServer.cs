using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StatusServer.ServerListPing.Standard;


namespace StatusServer.ServerListPing
{
    public class StatusTcpServer : IStatusServer
    {
        private static TcpListener _listener;
        
        public Func<StatusPayload> GetStatusPayload { get; set; }
        
        public StatusTcpServer(ushort port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
        }
        
        public async void Start()
        {
            if (GetStatusPayload == null) return;

            _listener.Start();

            try
            {
                await Listen();
            }
            catch (Exception e)
            {
                if (e is SocketException || e is ObjectDisposedException) 
                    return;

                throw;
            }
        }

        public void Dispose()
        {
            _listener.Stop();
        }

        private async Task Listen()
        {
            while (true)
            {
                try {
                    var client = await _listener.AcceptTcpClientAsync();
                    var stream = client.GetStream();

                    var reader = new BinaryReader(stream);
                    
                    // Handshake: https://wiki.vg/Protocol#Handshake
                    /* Packet length */ var length = reader.Read7BitEncodedInt();
                    if (length == 0xFE) // Legacy
                    {
                        var legacyPayload = LegacyStatusResponse();
                        stream.Write(legacyPayload, 0, legacyPayload.Length);
                        client.Close();
                        continue;
                    }

                    /* Packet ID (0) */ reader.ReadByte();
                    /* Protocol (47) */ reader.Read7BitEncodedInt();
                    /* Host */          reader.ReadString();
                    /* Port */          reader.ReadUInt16();
                    var state = reader.Read7BitEncodedInt();
                    
                    if (state != 1) continue; // Login (ignore)

                    HandlePacket(stream);
                    
                    // Follow-on ping (optional)
                    HandlePacket(stream);
                    client.Close();
                }
                catch (TargetInvocationException) { }
                catch (EndOfStreamException) { }
            }
        }
        
        /// <summary>
        /// https://wiki.vg/Protocol#Packet_format
        /// </summary>
        private void HandlePacket(NetworkStream stream)
        {
            var reader = new BinaryReader(stream);
            /* Packet length */
            reader.Read7BitEncodedInt();
            var packetID = (byte)reader.Read7BitEncodedInt();

            byte[] payload;
            switch (packetID)
            {
                case 0: // Status
                    payload = StatusResponse();
                    break;

                case 1: // Ping
                    var pong = reader.ReadInt64();
                    payload = BitConverter.GetBytes(pong);
                    break;

                default: return; // Ignore unknown packets
            }

            SendPacket(stream, packetID, payload); 
        }

        /// <summary>
        /// https://wiki.vg/Protocol#Packet_format
        /// </summary>
        private static async void SendPacket(NetworkStream stream, byte packetID, byte[] payload)
        {
            var buffer = new MemoryStream();
            var writer = new BinaryWriter(buffer);
            writer.Write7BitEncodedInt(1 + payload.Length);
            writer.Write(packetID);
            writer.Write(payload);
            
            var output = buffer.ToArray();
            await stream.WriteAsync(output, 0, output.Length);
        }

        /// <summary>
        /// https://wiki.vg/Server_List_Ping#Status_Response
        /// </summary>
        /// <returns>Packet 0x00 response payload</returns>
        private byte[] StatusResponse()
        {
            var json = JsonConvert.SerializeObject(GetStatusPayload(), Formatting.None, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
            });
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            
            var buffer = new MemoryStream();
            var writer = new BinaryWriter(buffer);
            writer.Write7BitEncodedInt(jsonBytes.Length);
            writer.Write(jsonBytes);
            writer.Close();
            return buffer.ToArray();
        }

        /// <summary>
        /// https://wiki.vg/Server_List_Ping#Server_to_client
        /// </summary>
        /// <returns>Full TCP response</returns>
        private byte[] LegacyStatusResponse()
        {
            var payload = GetStatusPayload();
            var payloadString = String.Join("\0", 
                "§1", 
                127,
                payload.Version.Name,
                payload.Description.Text,
                payload.Players.Online, 
                payload.Players.Max);

            var output = new byte[3 + 2 * payloadString.Length];
            output[0] = 0xFF;
            output[1] = (byte)(((ushort)payloadString.Length >> 8) & 0xFF);
            output[2] = (byte)payloadString.Length;
            Encoding.BigEndianUnicode.GetBytes(payloadString, 0, payloadString.Length, output, 3);
            
            return output;
        }
    }
}
