using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NetFlanders
{
    public class NetSocket
    {
        private readonly UdpClient _socket; //TODO: change this to a native socket to be able to catch socket errors (like Host Unreachable) and handle them

        private readonly ConcurrentDictionary<IPEndPoint, NetPeer> _peers = new ConcurrentDictionary<IPEndPoint, NetPeer>();
        private readonly object _peersLock = new object();

        private readonly NetLogger _logger;

        public readonly NetConfig Config;


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
            NetPeer peer;
            lock (_peersLock)
            {
                if (!_peers.TryGetValue(endpoint, out peer))
                {
                    peer = new NetPeer(this, endpoint);
                    _peers.TryAdd(endpoint, peer);

                    peer.ConnectionRequested += OnConnectionRequesteed;
                }
            }
            return peer;
        }

        private bool OnConnectionRequesteed(NetPeer arg)
        {
            //TODO: allow connection request?
            return true;
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
            peer.HandlePacket(packet);
        }

        internal void Send(IPEndPoint endpoint, NetPacket packet)
        {
            byte[] datagram = SerializePacket(packet);
            _logger.Log($"Sending {datagram.Length} bytes to {endpoint}");

            _socket.Send(datagram, datagram.Length, endpoint);
        }

        internal Task SendAsync(IPEndPoint endpoint, NetPacket packet)
        {
            byte[] datagram = SerializePacket(packet);
            _logger.Log($"Sending {datagram.Length} bytes to {endpoint}");

            return _socket.SendAsync(datagram, datagram.Length, endpoint);
        }

        private static byte[] SerializePacket(NetPacket packet)
        {
            //TODO: serialize properly
            byte[] datagram = new byte[NetPacket.HeaderSize + packet.Body.Length];
            datagram[0] = (byte)packet.PacketType;
            Buffer.BlockCopy(BitConverter.GetBytes(packet.SequenceNumber), 0, datagram, 1, sizeof(ushort));
            packet.Body.CopyTo(new Memory<byte>(datagram, 3, datagram.Length - 3));
            return datagram;
        }

        public void Update()
        {
            foreach (var peer in _peers.Values)
            {
                peer.Update();
            }

            //TODO: poll queued packets from the peers
        }

        public async Task<ConnectResult> ConnectAsync(string host, int port)
        {
            //TODO: don't allow sending connection requests in server mode

            var addresses = Dns.GetHostAddresses(host);
            if (addresses.Length == 0)
                return ConnectResult.HostNotFound;

            var address = addresses.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork
                                                        || addr.AddressFamily == AddressFamily.InterNetworkV6);
            if (address is null)
                return ConnectResult.HostNotFound;

            var endpoint = new IPEndPoint(address, port);
            var peer = GetOrAddPeer(endpoint);
            peer.Send(new NetPacket(NetPacketType.ConnectionRequest, 0));

            var resetEvent = new SemaphoreSlim(0, 1);
            bool connected = false;
            var callback = new Action<NetPeer, bool>((peer, accepted) =>
            {
                connected = accepted;
                resetEvent.Release();
            });

            peer.ConnectionResponse += callback;
            bool gotResponse = await resetEvent.WaitAsync(Config.Timeout);
            peer.ConnectionResponse -= callback;

            if (!gotResponse)
                return ConnectResult.Timeout;

            if (connected)
            {
                peer.SetConnected();
                return ConnectResult.Connected;
            }

            _ = _peers.TryRemove(endpoint, out _);
            return ConnectResult.Rejected;
        }
    }
}
