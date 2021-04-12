using System;
using System.Collections.Concurrent;

namespace NetFlanders
{
    internal sealed class NetChannel
    {
        internal readonly bool IsReliable;
        internal readonly bool IsSequenced;
        internal readonly byte Id;

        private readonly NetPeer _peer;

        private ushort _sequenceId;

        private readonly ConcurrentQueue<NetPacket> _queuedPackets = new ConcurrentQueue<NetPacket>();

        public NetChannel(NetPeer peer, byte id, bool reliable, bool sequenced)
        {
            _peer = peer;
            Id = id;
            IsReliable = reliable;
            IsSequenced = sequenced;
        }

        public void QueuePacket(NetPacket packet)
        {
            _queuedPackets.Enqueue(packet);
        }

        internal void SendQueuedPackets()
        {
            while (_queuedPackets.TryDequeue(out var packet))
            {
                //TODO: merge packets?
                Send(packet);
            }
        }

        private void Send(NetPacket packet)
        {
            int headerSize = 1 + (IsSequenced ? sizeof(ushort) : 0);

            byte[] packetData = new byte[NetPacket.HeaderSize + headerSize + packet.RawData.Length];
            packetData[0] = (byte)packet.Flags;

            byte type = (byte)((IsReliable ? 1 << 0 : 0) | (IsSequenced ? 1 << 1 : 0));
            packetData[1] = type;
            if (IsSequenced)
            {
                NetBitWriter.Write(packetData, 2, _sequenceId);
                _sequenceId++;
            }
            Buffer.BlockCopy(packet.RawData, 0, packetData, NetPacket.HeaderSize, packet.RawData.Length);


            _peer.Send(packetData);
        }
    }
}