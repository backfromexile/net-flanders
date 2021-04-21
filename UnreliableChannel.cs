using System;

namespace NetFlanders
{

    internal sealed class UnreliableChannel : SequencedChannel
    {
        public UnreliableChannel(NetPeer peer) : base(peer)
        {
        }

        protected override void OnPacketReceived(NetPacket packet)
        {
            throw new NotImplementedException();
        }

        protected override void SendInternal(ushort sequence, byte[] data)
        {
            var packet = new NetPacket(NetPacketType.Reliable, sequence, data);
            Peer.Send(packet);
        }
    }
}
