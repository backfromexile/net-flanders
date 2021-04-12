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

        public object ConnectionState => throw new NotImplementedException(); //TODO: connection state, USE STATE MACHINE FOR THE CONNECTION STATE INTERNALLY

        private IPEndPoint? _endPoint;
        public IPEndPoint EndPoint => _endPoint ?? throw new InvalidOperationException();


        public TimeSpan Ping => throw new NotImplementedException();

        private int _mtu;
        public int Mtu => throw new NotImplementedException();

        private long _connectionId = -1; //TODO: connection id
        public long Id => _connectionId > 0 ? _connectionId : throw new InvalidOperationException();

        private TimeSpan _timeSinceLastPacket;
        private TimeSpan _resendDelay; //TODO: should be round trip time * factor + constant value

        private NetStats _stats; //TODO: net stats
        public NetStats Statistics => _stats;


        private TimeSpan _slidingAverageRoundTripTime; //TODO: sliding average of rtt
        private readonly NetChannel?[] _channels = new NetChannel?[byte.MaxValue + 1];

        internal NetPeer(NetSocket socket, IPEndPoint endPoint)
        {
            _socket = socket;
            _endPoint = endPoint;
        }

        //public void Start()
        //{
        //    BeginReceive();
        //}

        internal void RegisterChannel(byte channelId, NetChannel channel)
        {
            ref var channelObj = ref _channels[channelId];
            if (channel is object)
                throw new InvalidOperationException();

            channelObj = channel;
        }

        public void QueueMessage<TMessage>(TMessage message, byte channelId)
            where TMessage : INetMessage
        {
            message.NetSerialize();

            QueueMessage(bytes, channelId);
        }

        public void QueueMessage(byte[] message, byte channelId)
        {
            if (message.Length > _mtu)
                throw new InvalidOperationException();

            var channel = _channels[channelId];
            if (channel is null)
                throw new ArgumentNullException($"Channel {channelId} is not initialized!");

            var packet = new NetPacket(NetPacketFlags.None, message);
            channel.QueuePacket(packet);
        }

        internal void Send(byte[] data)
        {
            _socket.Send(this, data);
        }

        /*
        private void BeginReceive()
        {
            _socket.BeginReceive(OnReceive, null);
        }

        private void OnReceive(IAsyncResult asyncResult)
        {
            IPEndPoint? endPoint = null;
            var bytes = _socket.EndReceive(asyncResult, ref endPoint);
            Console.WriteLine($"Received {bytes.Length} bytes from {endPoint}");

            BeginReceive();
        }
        */
    }
}