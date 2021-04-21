using System;

namespace NetFlanders
{
    public sealed class NetServer
    {
        private readonly NetSocket _socket;

        public NetServer(int port, NetConfig config)
        {
            _socket = new NetSocket(false, port, config);
            _socket.ConnectionRequest += OnConnectionRequest;
        }

        private bool OnConnectionRequest(NetPeer arg)
        {
            return true;
        }

        public void Start() => _socket.Start();

        public void Update() => _socket.Update();
    }
}
