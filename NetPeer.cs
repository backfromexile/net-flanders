using System;
using System.Net;

namespace NetFlanders
{
    /// <summary>
    /// Class for sending and receiving data from specific peers
    /// </summary>
    public sealed class NetPeer
    {
        private readonly NetSocket _socket;

        internal object ConnectionState => throw new NotImplementedException(); //TODO: connection state, USE STATE MACHINE FOR THE CONNECTION STATE INTERNALLY

        private IPEndPoint? _endPoint;
        internal IPEndPoint EndPoint => _endPoint ?? throw new InvalidOperationException();


        public TimeSpan Ping => throw new NotImplementedException();

        private int _mtu;
        internal int Mtu => throw new NotImplementedException();

        private long _connectionId = -1; //TODO: connection id
        public long Id => _connectionId > 0 ? _connectionId : throw new InvalidOperationException();

        private TimeSpan _timeSinceLastPacket;
        private TimeSpan _resendDelay; //TODO: should be round trip time * factor + constant value

        private NetStats _stats; //TODO: net stats
        public NetStats Statistics => _stats;


        private TimeSpan _slidingAverageRoundTripTime; //TODO: sliding average of rtt
        private readonly NetChannel?[] _channels;

        internal NetPeer(NetSocket socket, IPEndPoint endPoint, NetChannel?[] channels)
        {
            _socket = socket;
            _endPoint = endPoint;
            _channels = channels;
        }

        public void Send<TMessage>(TMessage message, byte channelId)
            where TMessage : INetMessage
        {
            var writer = new NetDataWriter();
            writer.PutMessage(message);

            Send(writer, channelId);
        }

        public void Send(NetDataWriter message, byte channelId)
        {
            if (message.Size > _mtu)
                throw new InvalidOperationException();

            var channel = _channels[channelId];
            if (channel is null)
                throw new ArgumentNullException($"Channel {channelId} is not initialized!");

            var packet = channel.PreparePacket(message);
            _socket.Send(this, packet);
        }
    }
}