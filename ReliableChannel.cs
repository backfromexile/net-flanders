using System;
using System.Diagnostics;

namespace NetFlanders
{
    internal sealed class ReliableChannel : SequencedChannel
    {
        private readonly Stopwatch _ackStopwatch = new Stopwatch();

        public ReliableChannel(NetPeer peer) : base(peer)
        {
            _ackStopwatch.Start();
        }

        internal void HandleAck(NetPacket packet)
        {
            throw new NotImplementedException();
        }

        protected override void OnPacketReceived(NetPacket packet)
        {
            Peer.Send(new NetPacket(NetPacketType.Ack, packet.SequenceNumber));
        }
    }
}
