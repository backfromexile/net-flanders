namespace NetFlanders
{
    internal abstract class SequencedChannel
    {
        protected readonly NetPeer Peer;
        protected ushort Sequence => _sequence;
        private ushort _sequence;

        protected SequencedChannel(NetPeer peer)
        {
            Peer = peer;
        }

        internal abstract void HandlePacket(NetPacket packet);
    }
}
