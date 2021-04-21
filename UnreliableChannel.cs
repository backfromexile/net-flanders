using System;

namespace NetFlanders
{

    internal sealed class UnreliableChannel : SequencedChannel
    {
        public UnreliableChannel(NetPeer peer) : base(peer)
        {
        }

        internal override void OnPacketReceived(NetPacket packet)
        {
            throw new NotImplementedException();
        }
    }
}
