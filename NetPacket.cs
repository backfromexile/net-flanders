namespace NetFlanders
{
    internal readonly struct NetPacket
    {
        internal static readonly int HeaderSize = sizeof(NetPacketFlags);

        public readonly NetPacketFlags Flags;
        public readonly byte[] RawData;

        public NetPacket(NetPacketFlags flags, byte[] rawData)
        {
            Flags = flags;
            RawData = rawData;
        }
    }
}