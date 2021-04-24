using System;
using System.Collections.Generic;

namespace NetFlanders
{
    public sealed class NetServer
    {
        private readonly NetSocket _socket;

        public event Action<NetPeer>? PeerConnected;
        public event Action<NetPeer, DisconnectReason>? PeerDisconnected;

        public IReadOnlyCollection<NetPeer> ConnectedPeers => _socket.ConnectedPeers;

        public NetServer(int port, NetConfig config)
        {
            _socket = new NetSocket(false, port, config);
            _socket.ConnectionRequest += OnConnectionRequest;

            _socket.PeerConnected += OnPeerConnected;
            _socket.PeerDisconnected += OnPeerDisconnected;
        }

        private void OnPeerDisconnected(NetPeer peer, DisconnectReason reason)
        {
            PeerDisconnected?.Invoke(peer, reason);
        }

        private void OnPeerConnected(NetPeer peer)
        {
            PeerConnected?.Invoke(peer);
        }

        private bool OnConnectionRequest(NetPeer arg)
        {
            return true;
        }

        public void Start() => _socket.Start();

        public void Update() => _socket.Update();

        public void SendToAll(NetSerializer serializer, bool reliable)
        {
            var bytes = serializer.GetBytes();
            foreach (var peer in ConnectedPeers)
            {
                if (reliable)
                {
                    peer.SendReliable(bytes);
                }
                else
                {
                    peer.SendUnreliable(bytes);
                }
            }
        }
    }
}
