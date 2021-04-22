using System;
using System.Linq;
using System.Threading.Tasks;

namespace NetFlanders
{
    public sealed class NetClient
    {
        private readonly NetSocket _socket;
        public NetPeer? ServerPeer { get; private set; }

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
                ServerPeer = _socket.ConnectedPeers.Single();
                ServerPeer.Disconnected += OnDisconnected;
            }

            return result;
        }

        private void OnDisconnected(DisconnectReason reason)
        {
            ServerPeer = null;
        }

        public void Send(NetSerializer serializer, bool reliable)
        {
            if (ServerPeer is null)
                throw new InvalidOperationException();

            var data = serializer.GetBytes();
            if (reliable)
            {
                ServerPeer.SendReliable(data);
            }
            else
            {
                ServerPeer.SendUnreliable(data);
            }
        }
    }
}
