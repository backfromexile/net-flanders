using System;
using System.Collections.Concurrent;

namespace NetFlanders
{
    internal enum NetChannelFlags : byte
    {
        Unreliable = 0,
        Reliable = 1 << 0,
        Sequenced = 1 << 1,
    }

    internal sealed class NetChannel
    {
        private readonly NetChannelFlags _flags;
        public NetChannelFlags Flags => _flags;

        private readonly byte _id;
        private ushort _sequenceId;

        public byte Id => _id;

        private readonly NetDataWriter _writer = new NetDataWriter();

        public NetChannel(byte id, NetChannelFlags flags)
        {
            _id = id;
            _flags = flags;
        }

        internal void HandleAck()
        {

        }

        internal NetPacket PreparePacket(NetDataWriter message)
        {
            _writer.Clear();
            _writer.Put(_flags);

            if (_flags.HasFlag(NetChannelFlags.Sequenced))
            {
                _writer.Put(_sequenceId);
                _sequenceId++;
            }
            _writer.PutRaw(message.GetRawData());

            return new NetPacket(NetPacketFlags.None, _writer.GetReader());
        }
    }
}