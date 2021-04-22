using System;
using System.Collections.Generic;

namespace NetFlanders
{
    public sealed class NetServer
    {
        private readonly NetSocket _socket;

        //TODO: these events don't do shit right now
        public event Action<NetPeer>? PeerConnected;
        public event Action<NetPeer>? PeerDisconnected;

        public IReadOnlyCollection<NetPeer> ConnectedPeers => _socket.ConnectedPeers;

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

        public void SendToAll(NetSerializer serializer, bool reliable)
        {
            foreach(var peer in ConnectedPeers)
            {
                peer.SendReliable(serializer.GetBytes());
            }
        }
    }
}
