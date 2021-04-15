using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace NetFlanders
{
    public abstract class NetSocket
    {
        private class NetChannelDefinition
        {
            public readonly byte Id;
            public readonly NetChannelFlags Flags;

            public NetChannelDefinition(byte id, NetChannelFlags flags)
            {
                Id = id;
                Flags = flags;
            }
        }

        private readonly UdpClient _socket;
        private readonly NetChannelDefinition?[] _channelDefinitions = new NetChannelDefinition?[256];
        private readonly Dictionary<IPEndPoint, NetPeer> _peers = new Dictionary<IPEndPoint, NetPeer>();
        private readonly NetDataWriter _packetSerializer = new NetDataWriter();

        internal NetSocket(int port)
        {
            _socket = new UdpClient(port);
        }

        internal void Send(NetPeer peer, NetPacket packet)
        {
            //TODO: check connection state of peer

            _packetSerializer.Clear();
            _packetSerializer.PutMessage(packet);

            var bytes = _packetSerializer.GetRawData();

            _socket.Send(bytes, bytes.Length, peer.EndPoint);
        }

        public void RegisterChannel(byte channelId, bool reliable, bool sequenced)
        {
            //TODO: enforce that this can only be done until the socket is started!

            ref var channel = ref _channelDefinitions[channelId];
            if (channel is object)
                throw new InvalidOperationException();

            var flags = NetChannelFlags.Unreliable;
            if (reliable)
            {
                flags |= NetChannelFlags.Reliable;
            }
            if (sequenced)
            {
                flags |= NetChannelFlags.Sequenced;
            }
            channel = new NetChannelDefinition(channelId, flags);
        }

        public void Start()
        {
            BeginReceive();
        }

        private void BeginReceive()
        {
            _socket.BeginReceive(OnReceive, null);
        }

        private void OnReceive(IAsyncResult asyncResult)
        {
            IPEndPoint? endPoint = null;
            var bytes = _socket.EndReceive(asyncResult, ref endPoint);

            if (!_peers.TryGetValue(endPoint, out var peer))
            {
                peer = new NetPeer(this, endPoint, CreateChannels());
                _peers.Add(endPoint, peer);
            }
            Console.WriteLine($"Received {bytes.Length} bytes from {peer.EndPoint}");

            BeginReceive();
        }

        private NetChannel?[] CreateChannels()
        {
            return Array.ConvertAll(_channelDefinitions,
                channelDef => channelDef is null
                ? null
                : new NetChannel(channelDef.Id, channelDef.Flags)
            );
        }
    }
}