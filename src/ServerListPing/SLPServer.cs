using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StatusServer.ServerListPing.Standard;
using Vintagestory.API.Common;

namespace StatusServer.ServerListPing
{
    public class StatusTcpServer : IStatusServer
    {
        // Cached JSON settings for better performance
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };

        // Simple thread-local buffer pool to reduce allocations
        private static readonly ThreadLocal<byte[]> BufferPool = new ThreadLocal<byte[]>(() => new byte[8192]);

        private readonly ILogger _logger;
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts;
        private readonly ServerOptions _options;
        
        // Rate limiting: track requests per IP
        private readonly ConcurrentDictionary<string, RateLimitEntry> _rateLimiter;
        
        private Task _listenTask;
        private volatile bool _isRunning;
        private int _activeConnections;

        public Func<StatusPayload> GetStatusPayload { get; set; }
        
        public StatusTcpServer(ILogger logger, ushort port) 
            : this(logger, port, ServerOptions.Default)
        {
        }
        
        public StatusTcpServer(ILogger logger, ushort port, ServerOptions options)
        {
            _logger = logger;
            _listener = new TcpListener(IPAddress.Any, port);
            _cts = new CancellationTokenSource();
            _options = options ?? ServerOptions.Default;
            _rateLimiter = new ConcurrentDictionary<string, RateLimitEntry>();
        }
        
        public void Start()
        {
            _listener.Start(_options.Backlog);
            _isRunning = true;
            _listenTask = Task.Run(() => ListenAsync(_cts.Token));
            
            // Start rate limiter cleanup task
            if (_options.EnableRateLimiting)
            {
                Task.Run(() => RateLimiterCleanupAsync(_cts.Token));
            }
        }

        public void Dispose()
        {
            _isRunning = false;
            _cts.Cancel();
            _listener.Stop();
            
            // Wait for listen task to complete with timeout
            try
            {
                _listenTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch (AggregateException)
            {
                // Task was cancelled, this is expected
            }
            
            _cts.Dispose();
            _rateLimiter.Clear();
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (_isRunning && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var client = await _listener.AcceptTcpClientAsync();
                        
                        // Check max connections
                        if (_activeConnections >= _options.MaxConcurrentConnections)
                        {
                            client.Close();
                            continue;
                        }
                        
                        // Handle client in a separate task for parallelism
                        _ = HandleClientAsync(client, cancellationToken);
                    }
                    catch (ObjectDisposedException)
                    {
                        // Listener was stopped, exit gracefully
                        return;
                    }
                    catch (SocketException ex)
                    {
                        if (_isRunning)
                        {
                            _logger.Warning("Socket error accepting client: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    _logger.Error("Fatal error in status server: " + ex);
                    _logger.Notification("Status server stopped due to an exception");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _activeConnections);
            
            try
            {
                // Set timeouts to prevent slow/malicious clients
                client.ReceiveTimeout = _options.ClientTimeoutMs;
                client.SendTimeout = _options.ClientTimeoutMs;
                
                // Rate limiting check
                if (_options.EnableRateLimiting)
                {
                    var clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                    if (!CheckRateLimit(clientIp))
                    {
                        _logger.Debug("Rate limit exceeded for " + clientIp);
                        return;
                    }
                }
                
                await Task.Run(() => HandleClient(client), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Shutdown requested
            }
            catch (IOException ex)
            {
                _logger.Debug("IO error handling client: " + ex.Message);
            }
            catch (SocketException ex)
            {
                if (_isRunning)
                {
                    _logger.Debug("Socket error handling client: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error handling client: " + ex);
            }
            finally
            {
                client.Close();
                Interlocked.Decrement(ref _activeConnections);
            }
        }

        private bool CheckRateLimit(string clientIp)
        {
            var now = DateTime.UtcNow;
            var entry = _rateLimiter.GetOrAdd(clientIp, _ => new RateLimitEntry());
            
            lock (entry)
            {
                // Reset if window expired
                if ((now - entry.WindowStart).TotalSeconds >= _options.RateLimitWindowSeconds)
                {
                    entry.WindowStart = now;
                    entry.RequestCount = 0;
                }
                
                entry.RequestCount++;
                return entry.RequestCount <= _options.RateLimitMaxRequests;
            }
        }

        private async Task RateLimiterCleanupAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                    
                    var now = DateTime.UtcNow;
                    var expiredKeys = _rateLimiter
                        .Where(kvp => (now - kvp.Value.WindowStart).TotalSeconds > _options.RateLimitWindowSeconds * 2)
                        .Select(kvp => kvp.Key)
                        .ToList();
                    
                    foreach (var key in expiredKeys)
                    {
                        _rateLimiter.TryRemove(key, out _);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Debug("Rate limiter cleanup error: " + ex.Message);
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
            {
                // Handshake: https://wiki.vg/Protocol#Handshake
                var length = reader.Read7BitEncodedInt();
                
                if (length == 0xFE) // Legacy ping
                {
                    SendLegacyResponse(stream);
                    return;
                }

                /* Packet ID (0) */ reader.ReadByte();
                /* Protocol (47) */ reader.Read7BitEncodedInt();
                /* Host */          reader.ReadString();
                /* Port */          reader.ReadUInt16();
                var state = reader.Read7BitEncodedInt();

                if (state != 1) return; // Login state - ignore

                // Handle status request
                HandlePacket(stream, reader);

                // Handle follow-on ping (optional)
                if (stream.DataAvailable)
                {
                    HandlePacket(stream, reader);
                }
            }
        }
        
        /// <summary>
        /// Handles a single packet from the client.
        /// https://wiki.vg/Protocol#Packet_format
        /// </summary>
        private void HandlePacket(NetworkStream stream, BinaryReader reader)
        {
            /* Packet length */
            reader.Read7BitEncodedInt();
            var packetID = (byte)reader.Read7BitEncodedInt();

            switch (packetID)
            {
                case 0: // Status request
                    SendStatusResponse(stream);
                    break;

                case 1: // Ping
                    SendPingResponse(stream, reader.ReadInt64());
                    break;
            }
        }

        /// <summary>
        /// Sends status response using thread-local buffer.
        /// https://wiki.vg/Server_List_Ping#Status_Response
        /// </summary>
        private void SendStatusResponse(NetworkStream stream)
        {
            var json = JsonConvert.SerializeObject(GetStatusPayload(), Formatting.None, JsonSettings);
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            
            // Calculate packet size: VarInt length prefix + packet ID (1) + VarInt json length + json bytes
            var packetDataLength = 1 + GetVarIntSize(jsonBytes.Length) + jsonBytes.Length;
            var totalSize = GetVarIntSize(packetDataLength) + packetDataLength;
            
            // Use thread-local buffer if large enough, otherwise allocate
            var buffer = totalSize <= BufferPool.Value.Length 
                ? BufferPool.Value 
                : new byte[totalSize];
            
            var offset = 0;
            
            // Write packet length
            offset += WriteVarInt(buffer, offset, packetDataLength);
            
            // Write packet ID (0 for status response)
            buffer[offset++] = 0;
            
            // Write JSON length as VarInt
            offset += WriteVarInt(buffer, offset, jsonBytes.Length);
            
            // Write JSON bytes
            Buffer.BlockCopy(jsonBytes, 0, buffer, offset, jsonBytes.Length);
            offset += jsonBytes.Length;
            
            stream.Write(buffer, 0, offset);
        }

        /// <summary>
        /// Sends ping response using thread-local buffer.
        /// </summary>
        private void SendPingResponse(NetworkStream stream, long pingValue)
        {
            var buffer = BufferPool.Value;
            var offset = 0;
            
            // Packet length = 9 (1 byte packet ID + 8 bytes long)
            offset += WriteVarInt(buffer, offset, 9);
            
            // Packet ID (1 for ping)
            buffer[offset++] = 1;
            
            // Ping value (little-endian as per Minecraft protocol)
            var pingBytes = BitConverter.GetBytes(pingValue);
            Buffer.BlockCopy(pingBytes, 0, buffer, offset, 8);
            offset += 8;
            
            stream.Write(buffer, 0, offset);
        }

        /// <summary>
        /// Sends legacy status response.
        /// https://wiki.vg/Server_List_Ping#Server_to_client
        /// </summary>
        private void SendLegacyResponse(NetworkStream stream)
        {
            var payload = GetStatusPayload();
            
            // Build payload string using StringBuilder to reduce allocations
            var sb = new StringBuilder(256);
            sb.Append("§1\0");
            sb.Append("127\0");
            sb.Append(payload.Version.Name);
            sb.Append('\0');
            sb.Append(payload.Description.Text);
            sb.Append('\0');
            sb.Append(payload.Players.Online);
            sb.Append('\0');
            sb.Append(payload.Players.Max);
            
            var payloadString = sb.ToString();
            var outputLength = 3 + 2 * payloadString.Length;
            
            // Use thread-local buffer if large enough
            var buffer = outputLength <= BufferPool.Value.Length 
                ? BufferPool.Value 
                : new byte[outputLength];
            
            buffer[0] = 0xFF;
            buffer[1] = (byte)(((ushort)payloadString.Length >> 8) & 0xFF);
            buffer[2] = (byte)payloadString.Length;
            Encoding.BigEndianUnicode.GetBytes(payloadString, 0, payloadString.Length, buffer, 3);
            
            stream.Write(buffer, 0, outputLength);
        }
        
        /// <summary>
        /// Calculates the size of a VarInt encoding.
        /// </summary>
        private static int GetVarIntSize(int value)
        {
            var v = (uint)value;
            var size = 0;
            do
            {
                size++;
                v >>= 7;
            } while (v != 0);
            return size;
        }
        
        /// <summary>
        /// Writes a VarInt to a buffer and returns bytes written.
        /// </summary>
        private static int WriteVarInt(byte[] buffer, int offset, int value)
        {
            var v = (uint)value;
            var written = 0;
            
            while (v >= 0x80)
            {
                buffer[offset + written++] = (byte)(v | 0x80);
                v >>= 7;
            }
            
            buffer[offset + written++] = (byte)v;
            return written;
        }
        
        /// <summary>
        /// Rate limiting entry for tracking requests per IP.
        /// </summary>
        private class RateLimitEntry
        {
            public DateTime WindowStart = DateTime.UtcNow;
            public int RequestCount;
        }
    }
    
    /// <summary>
    /// Server configuration options.
    /// </summary>
    public class ServerOptions
    {
        /// <summary>
        /// Client connection timeout in milliseconds.
        /// </summary>
        public int ClientTimeoutMs { get; set; } = 5000;
        
        /// <summary>
        /// Maximum pending connections in the listen backlog.
        /// </summary>
        public int Backlog { get; set; } = 10;
        
        /// <summary>
        /// Maximum number of concurrent client connections.
        /// </summary>
        public int MaxConcurrentConnections { get; set; } = 50;
        
        /// <summary>
        /// Enable rate limiting per IP address.
        /// </summary>
        public bool EnableRateLimiting { get; set; } = true;
        
        /// <summary>
        /// Rate limit time window in seconds.
        /// </summary>
        public int RateLimitWindowSeconds { get; set; } = 60;
        
        /// <summary>
        /// Maximum requests allowed per IP within the rate limit window.
        /// </summary>
        public int RateLimitMaxRequests { get; set; } = 30;
        
        public static ServerOptions Default => new ServerOptions();
    }
}
