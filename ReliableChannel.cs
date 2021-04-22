using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace NetFlanders
{
    internal sealed class ReliableChannel : SequencedChannel
    {
        private class NetPacketComparer : IComparer<NetPacket>
        {
            public int Compare(NetPacket x, NetPacket y)
            {
                return x.SequenceNumber.CompareTo(y.SequenceNumber);
            }
        }

        private readonly Stopwatch _ackStopwatch = new Stopwatch();
        private readonly SortedDictionary<NetPacket, TimeSpan> _waitingForAck = new SortedDictionary<NetPacket, TimeSpan>(new NetPacketComparer());
        private readonly ConcurrentDictionary<NetPacket, int> _resendCounter = new ConcurrentDictionary<NetPacket, int>();
        private readonly object _ackLock = new object();


        public ReliableChannel(NetPeer peer) : base(peer)
        {
            _ackStopwatch.Start();
        }

        internal void HandleAck(NetPacket packet)
        {
            Peer.Socket.Logger.Log($"Got ACK for {packet.SequenceNumber}");

            lock (_ackLock)
            {
                _waitingForAck.Remove(packet);
            }
        }

        internal void Update()
        {
            List<KeyValuePair<NetPacket, TimeSpan>> waiting;
            lock(_ackLock)
            {
                waiting = _waitingForAck.ToList();
            }
            foreach(var (packet, sendTime) in waiting)
            {
                // we can safely assume that packet with lower sequence number were sent earlier
                if (_ackStopwatch.Elapsed - sendTime < Peer.ResendDelay + Peer.Socket.Config.ResendTime)
                    break;

                // packet loss
                Interlocked.Increment(ref Peer.Stats.LostPackets);

                int resendAttempts = _resendCounter.AddOrUpdate(packet, 1, (_, count) => count + 1);
                if(resendAttempts > Peer.Socket.Config.ResendAttempts)
                {
                    //TODO: invoke Disconnected event
                    Peer.StateMachine.Apply(NetPeerCommand.Timeout);
                    return;
                }

                Resend(packet);
            }
        }

        private void Resend(NetPacket packet)
        {
            lock (_ackLock)
            {
                var time = _ackStopwatch.Elapsed;
                _waitingForAck[packet] = time;
            }

            Peer.Socket.Logger.Log($"Re-sending reliable packet {packet.SequenceNumber}");
            Peer.Send(packet);
        }

        protected override void OnPacketReceived(NetPacket packet)
        {
            Peer.Socket.Logger.Log($"Sending ACK for {packet.SequenceNumber}");
            Peer.Send(new NetPacket(NetPacketType.Ack, packet.SequenceNumber));
        }

        protected override void SendInternal(ushort sequence, byte[] data)
        {
            var packet = new NetPacket(NetPacketType.Reliable, sequence, data);

            lock(_ackLock)
            {
                var time = _ackStopwatch.Elapsed;
                _waitingForAck.Add(packet, time);
            }

            Peer.Socket.Logger.Log($"Sending reliable packet {packet.SequenceNumber}");
            Peer.Send(packet);
        }

        protected override bool OnPollPacket(NetPacket packet)
        {
            // don't allow polling if we are still waiting for a reliable packet
            return LastPolledSequence + 1 == packet.SequenceNumber;
        }
    }
}
