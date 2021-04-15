namespace NetFlanders
{
    internal struct NetPacket : INetMessage
    {
        internal static readonly int HeaderSize = sizeof(NetPacketFlags);

        public NetPacketFlags Flags;
        public NetDataReader Data;

        public NetPacket(NetPacketFlags flags, NetDataReader data)
        {
            Flags = flags;
            Data = data;
        }

        public void NetDeserialize(NetDataReader reader)
        {
            Flags = reader.ReadEnum<NetPacketFlags>();
            Data = new NetDataReader(reader);
        }

        public void NetSerialize(NetDataWriter writer)
        {
            writer.Put(Flags);
            writer.Put(Data);
        }
    }
}