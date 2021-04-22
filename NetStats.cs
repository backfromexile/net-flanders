namespace NetFlanders
{
    public struct NetStats
    {
        public NetChannelStats Reliable;
        public NetChannelStats Unreliable;
        public NetChannelStats Internal;

        public long LostPackets;
    }
}
