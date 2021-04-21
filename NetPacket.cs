using System;

namespace NetFlanders
{
    internal readonly struct NetPacket
    {
        internal static int HeaderSize = sizeof(byte) + sizeof(ushort);

        public readonly NetPacketType PacketType;

        public readonly ushort SequenceNumber;
        public readonly ReadOnlyMemory<byte> Body;

        public int Size => HeaderSize + Body.Length;

        public NetPacket(NetPacketType packetType, ushort sequenceNumber) : this(packetType, sequenceNumber, new ReadOnlyMemory<byte>())
        {
        }

        public NetPacket(NetPacketType packetType, ushort sequenceNumber, ReadOnlyMemory<byte> body)
        {
            PacketType = packetType;
            SequenceNumber = sequenceNumber;
            Body = body;
        }

    }
}
