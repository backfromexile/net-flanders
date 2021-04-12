using System;
using System.Net.Sockets;

namespace NetFlanders
{
    public class NetSocket
    {
        private class NetChannelDefinition
        {
            public readonly byte Id;
            public readonly bool Reliable;
            public readonly bool Sequenced;

            public NetChannelDefinition(byte id, bool reliable, bool sequenced)
            {
                Id = id;
                Reliable = reliable;
                Sequenced = sequenced;
            }
        }

        private readonly UdpClient _socket;
        private readonly NetChannelDefinition?[] _channelDefinitions = new NetChannelDefinition?[256];

        internal NetSocket(int port)
        {
            _socket = new UdpClient(port);
        }

        internal void Send(NetPeer peer, byte[] data)
        {
            //TODO: check connection state of peer

            _socket.Send(data, data.Length, peer.EndPoint);
        }

        public void RegisterChannel(byte channelId, bool reliable, bool sequenced)
        {
            //TODO: enforce that this can only be done until the socket is started!

            ref var channel = ref _channelDefinitions[channelId];
            if (channel is object)
                throw new InvalidOperationException();

            channel = new NetChannelDefinition(channelId, reliable, sequenced);
        }
    }
}