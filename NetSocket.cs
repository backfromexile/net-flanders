using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NetFlanders
{
    public class NetSocket
    {
        private readonly UdpClient _socket; //TODO: change this to a native socket to be able to catch socket errors (like Host Unreachable) and handle them
        private readonly ConcurrentDictionary<IPEndPoint, NetPeer> _peers = new ConcurrentDictionary<IPEndPoint, NetPeer>();
        private readonly NetLogger _logger;

        public readonly NetConfig Config;

        private event Action<NetPeer, bool>? ConnectionResponse;


        public NetSocket(NetSocketType type, NetConfig config) : this(type, 0, config)
        {
        }

        public NetSocket(NetSocketType type, int port, NetConfig config)
        {
            _logger = new NetLogger($"[{type}]");

            Config = config;

            var endpoint = new IPEndPoint(IPAddress.Any, port);
            _socket = new UdpClient(endpoint);
        }

        public void Start()
        {
            ReceiveAsync();
        }

        private void ReceiveAsync()
        {
            _socket.BeginReceive(OnReceive, null);
        }

        private NetPeer GetOrAddPeer(IPEndPoint endpoint)
        {
            var peer = new NetPeer(this, endpoint);
            return _peers.GetOrAdd(endpoint, peer);
        }

        private void OnReceive(IAsyncResult asyncResult)
        {
            IPEndPoint? endpoint = null;
            byte[] data = _socket.EndReceive(asyncResult, ref endpoint);
            ReceiveAsync();

            if (data.Length < NetPacket.HeaderSize)
            {
                _logger.LogWarning($"Received too small packet ({data.Length} bytes) from {endpoint}, ignoring...");
                return;
            }

            var peer = GetOrAddPeer(endpoint);

            //TODO: network byte order and use proper reader type
            var packetType = (NetPacketType)data[0];
            var sequence = BitConverter.ToUInt16(data, 1);
            var dataMemory = new ReadOnlyMemory<byte>(data, NetPacket.HeaderSize, data.Length - NetPacket.HeaderSize);
            var packet = new NetPacket(packetType, sequence, dataMemory);

            _logger.Log($"Received {packetType} packet ({data.Length} bytes) from {endpoint}");

            switch (packetType)
            {
                case NetPacketType.ConnectionAccept:
                    _logger.Log($"Received connection accept");
                    ConnectionResponse?.Invoke(peer, true);
                    break;

                case NetPacketType.ConnectionReject:
                    _logger.Log($"Received connection reject");
                    ConnectionResponse?.Invoke(peer, false);
                    break;

                case NetPacketType.ConnectionRequest:
                    _logger.Log($"Received connection request");
                    peer.Send(new NetPacket(NetPacketType.ConnectionAccept, 0));
                    break;

                default:
                    peer.HandlePacket(packet);
                    break;
            }
        }

        internal void Send(IPEndPoint endpoint, NetPacket packet)
        {
            //TODO: serialize properly
            byte[] datagram = new byte[NetPacket.HeaderSize + packet.Body.Length];
            datagram[0] = (byte)packet.PacketType;
            Buffer.BlockCopy(BitConverter.GetBytes(packet.SequenceNumber), 0, datagram, 1, sizeof(ushort));
            packet.Body.CopyTo(new Memory<byte>(datagram, 3, datagram.Length - 3));

            _logger.Log($"Sending {datagram.Length} bytes to {endpoint}");
            _socket.Send(datagram, datagram.Length, endpoint);
        }

        public void Update()
        {
            foreach (var peer in _peers.Values)
            {
                peer.Update();
            }

            //TODO: poll queued packets from the peers
        }

        public ConnectResult Connect(string host, int port)
        {
            var addresses = Dns.GetHostAddresses(host);
            if (addresses.Length == 0)
                return ConnectResult.HostNotFound;

            var address = addresses.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork
                                                        || addr.AddressFamily == AddressFamily.InterNetworkV6);
            if (address is null)
                return ConnectResult.HostNotFound;

            var endpoint = new IPEndPoint(address, port);
            Send(endpoint, new NetPacket(NetPacketType.ConnectionRequest, 0));
            
            var resetEvent = new ManualResetEvent(false);
            bool connected = false;
            Action<NetPeer, bool> callback = (peer, accepted) =>
            {
                connected = accepted;
                resetEvent.Set();
            };

            ConnectionResponse += callback;
            bool gotResponse = resetEvent.WaitOne(Config.Timeout);
            ConnectionResponse -= callback;

            if (!gotResponse)
                return ConnectResult.Timeout;

            if (connected)
            {
                var peer = GetOrAddPeer(endpoint);
                peer.SetConnected();
                return ConnectResult.Connected;
            }

            return ConnectResult.Rejected;
        }
    }
}
