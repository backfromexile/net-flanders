using System;
using System.Linq;
using System.Threading.Tasks;

namespace NetFlanders
{
    public sealed class NetClient
    {
        private readonly NetSocket _socket;
        private NetPeer? _serverPeer;

        public TimeSpan Ping => _serverPeer?.Ping ?? TimeSpan.Zero;

        public NetStats Stats => _serverPeer?.Stats ?? default;

        public event Action<DisconnectReason>? Disconnected;

        public NetClient(NetConfig config)
        {
            _socket = new NetSocket(true, config);
        }

        public void Start() => _socket.Start();
        public void Update() => _socket.Update();
        public async Task<ConnectResult> ConnectAsync(string host, int port)
        {
            var result = await _socket.ConnectAsync(host, port);
            if (result == ConnectResult.Connected)
            {
                _serverPeer = _socket.ConnectedPeers.Single();
                _serverPeer.Disconnected += OnDisconnected;
            }

            return result;
        }

        private void OnDisconnected(DisconnectReason reason)
        {
            _serverPeer = null;

            Disconnected?.Invoke(reason);
        }

        public void Send(NetSerializer serializer, bool reliable)
        {
            if (_serverPeer is null)
                throw new InvalidOperationException();

            var data = serializer.GetBytes();
            if (reliable)
            {
                _serverPeer.SendReliable(data);
            }
            else
            {
                _serverPeer.SendUnreliable(data);
            }
        }
    }
}
