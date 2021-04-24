using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NetFlanders
{
    internal sealed class NetSocket
    {
        private readonly UdpClient _socket; //TODO: change this to a native socket to be able to catch socket errors (like Host Unreachable) and handle them?

        private readonly ConcurrentDictionary<IPEndPoint, NetPeer> _peers = new ConcurrentDictionary<IPEndPoint, NetPeer>();
        internal IReadOnlyCollection<NetPeer> ConnectedPeers => _peers.Values.Where(peer => peer.StateMachine.State == NetPeerState.Connected).ToList();

        private readonly object _peersLock = new object();

        internal readonly NetLogger Logger;
        private static NetSerializer _serializer = new NetSerializer();
        private static object _serializeLock = new object();

        private readonly NetConfig _config;
        public NetConfig Config => _config;
        private readonly Thread _receiveThread;
        internal readonly bool ClientMode;

        internal event Func<NetPeer, bool>? ConnectionRequest;
        internal event Action<NetPeer>? PeerConnected;
        internal event Action<NetPeer, DisconnectReason>? PeerDisconnected;

        private bool _started;

        public NetSocket(bool clientMode, NetConfig config) : this(clientMode, 0, config)
        {
        }

        public NetSocket(bool clientMode, int port, NetConfig config)
        {
            Logger = new NetLogger($"[{(clientMode ? "Client" : "Server")}]");
            ClientMode = clientMode;

            _config = config;

            _socket = new UdpClient(AddressFamily.InterNetworkV6);
            _socket.Client.DualMode = true; // for both IPv4 and IPv6 to work
            _socket.SetConnectionResetErrorEnabled(false);
            var endpoint = new IPEndPoint(IPAddress.IPv6Any, port);
            _socket.Client.Bind(endpoint);

            _receiveThread = new Thread(ReceiveAsync);
        }

        public void Start()
        {
            if (_started)
                throw new InvalidOperationException("Cannot start the socket multiple times.");

            _started = true;
            _receiveThread.Start();
        }

        private void ReceiveAsync()
        {
            _ = _socket.BeginReceive(OnReceive, null);
        }

        private NetPeer GetOrAddPeer(IPEndPoint endpoint)
        {
            endpoint.Address = endpoint.Address.MapToIPv6();
            NetPeer peer;
            lock (_peersLock)
            {
                if (!_peers.TryGetValue(endpoint, out peer))
                {
                    peer = new NetPeer(this, endpoint);
                    _peers.TryAdd(endpoint, peer);

                    peer.ConnectionRequested += OnConnectionRequested;
                    peer.ConnectionResponse += OnPeerConnectionResponse;
                    peer.Disconnected += OnPeerDisconnected;
                }
            }
            return peer;
        }

        private void OnPeerDisconnected(NetPeer peer, DisconnectReason reason)
        {
            PeerDisconnected?.Invoke(peer, reason);
        }

        private void OnPeerConnectionResponse(NetPeer peer, bool accepted)
        {
            if (accepted)
            {
                PeerConnected?.Invoke(peer);
            }
        }

        private bool OnConnectionRequested(NetPeer peer)
        {
            return ConnectionRequest?.Invoke(peer) is true;
        }

        private void OnReceive(IAsyncResult asyncResult)
        {
            IPEndPoint endpoint = null!;
            byte[] data = _socket.EndReceive(asyncResult, ref endpoint);

            if (data.Length < NetPacket.HeaderSize)
            {
                Logger.LogWarning($"Received too small packet ({data.Length} bytes) from {endpoint}, ignoring...");
                ReceiveAsync();
                return;
            }

            var peer = GetOrAddPeer(endpoint);

            var deserializer = new NetDeserializer(data);
            var packetType = deserializer.ReadEnum<NetPacketType>();
            var sequence = deserializer.ReadUInt16();
            var dataMemory = new ReadOnlyMemory<byte>(data, NetPacket.HeaderSize, data.Length - NetPacket.HeaderSize);

            var packet = new NetPacket(packetType, sequence, dataMemory);

            Logger.LogDebug($"Received {packetType} packet ({data.Length} bytes) from {endpoint}");
            peer.HandlePacket(packet);

            ReceiveAsync();
        }

        internal void Send(IPEndPoint endpoint, NetPacket packet)
        {
            byte[] datagram = SerializePacket(packet);
            Logger.LogDebug($"Sending {datagram.Length} bytes to {endpoint} (sync)");

            _socket.Send(datagram, datagram.Length, endpoint);
        }

        internal Task SendAsync(IPEndPoint endpoint, NetPacket packet)
        {
            byte[] datagram = SerializePacket(packet);
            Logger.LogDebug($"Sending {datagram.Length} bytes to {endpoint} (async)");

            return _socket.SendAsync(datagram, datagram.Length, endpoint);
        }

        private static byte[] SerializePacket(NetPacket packet)
        {
            lock (_serializeLock)
            {
                _serializer.Reset();
                _serializer.Write(packet.PacketType);
                _serializer.Write(packet.SequenceNumber);
                _serializer.WriteRaw(packet.Body);

                return _serializer.GetBytes();
            }
        }

        public void Update()
        {
            foreach (var peer in _peers.Values)
            {
                peer.Update();
            }
        }

        public async Task<ConnectResult> ConnectAsync(string host, int port)
        {
            // don't allow sending connection requests in server mode
            if (!ClientMode)
                throw new InvalidOperationException("Cannot connect in server mode");


            var addresses = Dns.GetHostAddresses(host);
            if (addresses.Length == 0)
                return ConnectResult.HostNotFound;

            var address = addresses.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork
                                                        || addr.AddressFamily == AddressFamily.InterNetworkV6);
            if (address is null)
                return ConnectResult.HostNotFound;

            var endpoint = new IPEndPoint(address, port);
            var peer = GetOrAddPeer(endpoint);
            peer.StateMachine.Apply(NetPeerCommand.RequestConnection);


            var resetEvent = new SemaphoreSlim(0, 1);
            bool connected = false;
            var callback = new Action<NetPeer, bool>((peer, accepted) =>
            {
                connected = accepted;
                resetEvent.Release();
            });

            peer.ConnectionResponse += callback;

            try
            {
                peer.Send(new NetPacket(NetPacketType.ConnectionRequest, 0));
            }
            catch (SocketException)
            {
                return ConnectResult.Error;
            }

            bool gotResponse = await resetEvent.WaitAsync(_config.Timeout);
            peer.ConnectionResponse -= callback;

            if (!gotResponse)
            {
                peer.StateMachine.Apply(NetPeerCommand.Timeout);
                return ConnectResult.Timeout;
            }

            if (connected)
            {
                peer.StateMachine.Apply(NetPeerCommand.ConnectionAccepted);
                return ConnectResult.Connected;
            }

            _ = _peers.TryRemove(endpoint, out _);
            peer.StateMachine.Apply(NetPeerCommand.ConnectionRejected);
            return ConnectResult.Rejected;
        }
    }
}
