using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NetFlanders
{
    internal abstract class SequencedChannel
    {
        private struct TimedNetPacket
        {
            public TimeSpan Time;
            public NetPacket Packet;
        }
        private class NetPacketComparer : IComparer<TimedNetPacket>
        {
            public int Compare(TimedNetPacket x, TimedNetPacket y)
            {
                return x.Packet.SequenceNumber.CompareTo(y.Packet.SequenceNumber);
            }
        }

        protected readonly NetPeer Peer;

        private ushort _sequence;
        private readonly object _sendLock = new object();

        private readonly SortedSet<TimedNetPacket> _receivedPacketQueue = new SortedSet<TimedNetPacket>(new NetPacketComparer());
        private readonly object _receivedQueueLock = new object();
        private readonly Stopwatch _receiveTimer;
        private readonly HashSet<ushort> _receivedPackets = new HashSet<ushort>();

        protected SequencedChannel(NetPeer peer)
        {
            Peer = peer;

            _receiveTimer = Stopwatch.StartNew();
        }

        protected virtual void OnPacketReceived(NetPacket packet) { }

        internal void HandleReceivedPacket(NetPacket packet)
        {
            lock(_receivedQueueLock)
            {
                if (!_receivedPackets.Add(packet.SequenceNumber))
                    return;
            }

            //TODO: reliable channel: check if sequence number matches previous sequence + 1
            //TODO: unreliable channel: check if sequence number > previous sequence
            OnPacketReceived(packet);

            lock (_receivedQueueLock)
            {
                var time = _receiveTimer.Elapsed;
                _receivedPacketQueue.Add(new TimedNetPacket
                {
                    Time = time,
                    Packet = packet,
                });
            }
        }

        internal bool TryPollPacket(out NetPacket packet)
        {
            //TODO: reliable channel: don't allow polling if we are still waiting for a reliable packet
            //TODO: unreliable channel: track skipped(=lost) packets
            packet = default;

            lock (_receivedQueueLock)
            {
                if (_receivedPacketQueue.Count == 0)
                    return false;

                var first = _receivedPacketQueue.Min;
                if (_receiveTimer.Elapsed - first.Time < Peer.Socket.Config.PacketBufferTime)
                    return false;

                packet = first.Packet;
                _receivedPacketQueue.Remove(first);
                return true;
            }
        }

        internal void Send(byte[] data)
        {
            ushort seq;
            lock (_sendLock)
            {
                seq = ++_sequence;
            }

            SendInternal(seq, data);
        }

        protected abstract void SendInternal(ushort sequence, byte[] data);
    }
}
