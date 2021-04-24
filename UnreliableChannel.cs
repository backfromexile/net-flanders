namespace NetFlanders
{
    internal sealed class UnreliableChannel : SequencedChannel
    {
        public UnreliableChannel(NetPeer peer) : base(peer)
        {
        }

        protected override bool OnPollPacket(NetPacket packet)
        {
            var lostPackets = packet.SequenceNumber - LastPolledSequence - 1; // e.g. last sequence was 9 and next is 11, then we lost 11-9-1=1 packet
            Peer.Stats.LostPackets += lostPackets;

            return true;
        }

        protected override void SendInternal(ushort sequence, byte[] data)
        {
            var packet = new NetPacket(NetPacketType.Unreliable, sequence, data);
            Peer.Send(packet);
        }
    }
}
