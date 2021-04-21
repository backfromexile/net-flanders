using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;

namespace NetFlanders
{
    internal enum NetPeerState
    {
        Disconnected,
        ConnectionRequested,
        Connected,
    }

    internal enum NetPeerCommand
    {
        RequestConnection,
        ConnectionAccepted,
        ConnectionRejected,
        Disconnect,
        Timeout
    }

    public struct NetChannelStats
    {
        public long SentBytes;
        public long ReceivedBytes;

        public long SentPackets;
        public long ReceivedPackets;
    }

    public struct NetStats
    {
        public NetChannelStats Reliable;
        public NetChannelStats Unreliable;
        public NetChannelStats Internal;

        public long LostPackets;
    }

    internal sealed class NetPeer
    {
        public NetStats Stats;

        #region ping
        public TimeSpan Ping => _ping;
        private TimeSpan _ping;
        private TimeSpan _roundTripTime;
        private ushort _pingSequence;
        private readonly Stopwatch _pingStopwatch = new Stopwatch();
        private readonly SortedDictionary<ushort, TimeSpan> _pingTimes = new SortedDictionary<ushort, TimeSpan>();
        private readonly object _pingLock = new object();
        private readonly Queue<(TimeSpan received, TimeSpan ping)> _pings = new Queue<(TimeSpan received, TimeSpan ping)>(1000);
        #endregion

        #region common state
        private readonly Stopwatch _lastPacketStopwatch = new Stopwatch();
        private readonly object _stateLock = new object();
        private TimeSpan _lastPacketTime;

        public readonly StateMachine<NetPeerState, NetPeerCommand> State = new StateMachine<NetPeerState, NetPeerCommand>(NetPeerState.Disconnected)
            .Add(NetPeerState.Disconnected, NetPeerState.ConnectionRequested, NetPeerCommand.RequestConnection)
            .Add(NetPeerState.ConnectionRequested, NetPeerState.Connected, NetPeerCommand.ConnectionAccepted)
            .Add(NetPeerState.ConnectionRequested, NetPeerState.Disconnected, NetPeerCommand.ConnectionRejected)
            .Add(NetPeerState.ConnectionRequested, NetPeerState.Disconnected, NetPeerCommand.Timeout)
            .Add(NetPeerState.Connected, NetPeerState.Disconnected, NetPeerCommand.Disconnect);
        #endregion

        internal readonly NetSocket Socket;
        private readonly IPEndPoint _endpoint;
        private readonly UnreliableChannel _unreliableChannel;
        private readonly ReliableChannel _reliableChannel;
        internal TimeSpan ResendDelay => new TimeSpan((long)(Ping.Ticks * 2.5));

        internal event Action<DisconnectReason>? Disconnected;
        internal event Action<NetPeer, bool>? ConnectionResponse;
        internal event Func<NetPeer, bool>? ConnectionRequested; //TODO: additional data

        internal NetPeer(NetSocket socket, IPEndPoint endpoint)
        {
            Socket = socket;
            _endpoint = endpoint;

            _unreliableChannel = new UnreliableChannel(this);
            _reliableChannel = new ReliableChannel(this);

            _pingStopwatch.Start();
            _lastPacketStopwatch.Start();

            State.Start();
        }

        internal void Update()
        {
            if (State.State != NetPeerState.Connected)
                return;

            var timeWithoutPacket = _lastPacketStopwatch.Elapsed - _lastPacketTime;
            //we timed out
            if (timeWithoutPacket > Socket.Config.Timeout)
            {
                State.Apply(NetPeerCommand.Disconnect);
                Disconnected?.Invoke(DisconnectReason.Timeout);
                return;
            }

            _reliableChannel.Update();

            CleanUpOldPings();
            UpdatePingSlidingWindow();
            SendPing();

            PollPackets();
        }

        private void UpdatePingSlidingWindow()
        {
            lock (_pingLock)
            {
                var now = _pingStopwatch.Elapsed;
                while (_pings.Count > 0)
                {
                    var (time, ping) = _pings.Peek();
                    if (now - time < Socket.Config.PingWindow)
                        break;

                    _pings.Dequeue();
                    _roundTripTime -= ping;
                }

                int count = _pings.Count;
                if (count == 0)
                {
                    _ping = TimeSpan.Zero;
                    return;
                }

                _ping = new TimeSpan(_roundTripTime.Ticks / count / 2);
            }
        }

        private void CleanUpOldPings()
        {
            List<KeyValuePair<ushort, TimeSpan>> pingTimes;
            lock (_pingLock)
            {
                pingTimes = _pingTimes.ToList();
            }

            var remove = new List<KeyValuePair<ushort, TimeSpan>>();
            foreach (var pair in pingTimes)
            {
                if (_pingStopwatch.Elapsed - pair.Value < Socket.Config.PingWindow)
                    break;

                remove.Add(pair);
                // track lost ping packets
                Interlocked.Increment(ref Stats.LostPackets);
            }

            lock (_pingLock)
            {
                foreach (var (seq, _) in remove)
                {
                    _pingTimes.Remove(seq);
                }
            }
        }

        private void PollPackets()
        {
            while (_unreliableChannel.TryPollPacket(out var packet))
            {
                //TODO: poll unreliable packet
                Socket.Logger.Log($"Polled unreliable packet {packet.SequenceNumber}");
            }

            while (_reliableChannel.TryPollPacket(out var packet))
            {
                //TODO: poll reliable packet
                Socket.Logger.Log($"Polled reliable packet {packet.SequenceNumber}");
            }
        }

        internal void HandlePacket(NetPacket packet)
        {
            lock (_stateLock)
            {
                _lastPacketTime = _lastPacketStopwatch.Elapsed;
            }

            switch (packet.PacketType)
            {
                case NetPacketType.ConnectionAccept:
                    Interlocked.Increment(ref Stats.Internal.ReceivedPackets);
                    Interlocked.Add(ref Stats.Internal.ReceivedBytes, packet.Size);

                    ConnectionResponse?.Invoke(this, true);
                    break;

                case NetPacketType.ConnectionReject:
                    Interlocked.Increment(ref Stats.Internal.ReceivedPackets);
                    Interlocked.Add(ref Stats.Internal.ReceivedBytes, packet.Size);

                    ConnectionResponse?.Invoke(this, false);
                    break;

                case NetPacketType.ConnectionRequest:
                    // ignore connection requests in client mode
                    if (Socket.ClientMode)
                        return;

                    Interlocked.Increment(ref Stats.Internal.ReceivedPackets);
                    Interlocked.Add(ref Stats.Internal.ReceivedBytes, packet.Size);

                    // let user decide through call
                    State.Apply(NetPeerCommand.RequestConnection);
                    bool? accept = ConnectionRequested?.Invoke(this);
                    if (accept is true)
                    {
                        State.Apply(NetPeerCommand.ConnectionAccepted);
                        Send(new NetPacket(NetPacketType.ConnectionAccept, 0));
                    }
                    else
                    {
                        State.Apply(NetPeerCommand.ConnectionRejected);
                        Send(new NetPacket(NetPacketType.ConnectionReject, 0));
                    }
                    break;

                case NetPacketType.Unreliable:
                    Interlocked.Increment(ref Stats.Unreliable.ReceivedPackets);
                    Interlocked.Add(ref Stats.Unreliable.ReceivedBytes, packet.Size);

                    _unreliableChannel.HandleReceivedPacket(packet);
                    return;

                case NetPacketType.Reliable:
                    Interlocked.Increment(ref Stats.Reliable.ReceivedPackets);
                    Interlocked.Add(ref Stats.Reliable.ReceivedBytes, packet.Size);

                    _reliableChannel.HandleReceivedPacket(packet);
                    return;

                case NetPacketType.Ping:
                    {
                        Interlocked.Increment(ref Stats.Internal.ReceivedPackets);
                        Interlocked.Add(ref Stats.Internal.ReceivedBytes, packet.Size);

                        var answer = new NetPacket(NetPacketType.Pong, packet.SequenceNumber);
                        Send(answer);
                        return;
                    }

                case NetPacketType.Pong:
                    {
                        Interlocked.Increment(ref Stats.Internal.ReceivedPackets);
                        Interlocked.Add(ref Stats.Internal.ReceivedBytes, packet.Size);

                        var seq = packet.SequenceNumber;
                        TimeSpan time;
                        lock (_pingLock)
                        {
                            if (!_pingTimes.TryGetValue(seq, out time))
                                return;

                            _pingTimes.Remove(seq);
                        }

                        Socket.Logger.LogDebug($"{_endpoint}: Still waiting for {_pingTimes.Count} ping packets");

                        var now = _pingStopwatch.Elapsed;
                        lock (_pingLock)
                        {
                            var ping = now - time;
                            _roundTripTime += ping;

                            _pings.Enqueue((now, ping));
                        }
                        return;
                    }

                case NetPacketType.Disconnect:
                    {
                        Interlocked.Increment(ref Stats.Internal.ReceivedPackets);
                        Interlocked.Add(ref Stats.Internal.ReceivedBytes, packet.Size);

                        State.Apply(NetPeerCommand.Disconnect);
                        Disconnected?.Invoke(DisconnectReason.RemoteDisconnected);
                        return;
                    }

                case NetPacketType.Ack:
                    {
                        Interlocked.Increment(ref Stats.Internal.ReceivedPackets);
                        Interlocked.Add(ref Stats.Internal.ReceivedBytes, packet.Size);

                        _reliableChannel.HandleAck(packet);
                        return;
                    }

                case NetPacketType.Debug:
                    {
                        Interlocked.Increment(ref Stats.Internal.ReceivedPackets);
                        Interlocked.Add(ref Stats.Internal.ReceivedBytes, packet.Size);

                        //TODO: debug
                        return;
                    }
            }
        }

        private void SendPing()
        {
            var time = _pingStopwatch.Elapsed;
            ushort seq;
            lock (_pingLock)
            {
                seq = ++_pingSequence;
                _pingTimes.Add(seq, time);
            }

            Send(new NetPacket(NetPacketType.Ping, seq));
        }

        internal void Send(NetPacket packet)
        {
            switch (packet.PacketType)
            {
                case NetPacketType.Unreliable:
                    Interlocked.Increment(ref Stats.Unreliable.SentPackets);
                    Interlocked.Add(ref Stats.Unreliable.SentBytes, packet.Size);
                    break;

                case NetPacketType.Reliable:
                    Interlocked.Increment(ref Stats.Reliable.SentPackets);
                    Interlocked.Add(ref Stats.Reliable.SentBytes, packet.Size);
                    break;

                case NetPacketType.ConnectionRequest:
                case NetPacketType.ConnectionAccept:
                case NetPacketType.ConnectionReject:
                case NetPacketType.Disconnect:
                case NetPacketType.Ack:
                case NetPacketType.Ping:
                case NetPacketType.Pong:
                case NetPacketType.Debug:
                    Interlocked.Increment(ref Stats.Internal.SentPackets);
                    Interlocked.Add(ref Stats.Internal.SentBytes, packet.Size);
                    break;
            }

            Socket.Send(_endpoint, packet);
        }

        internal void SendReliable(byte[] data)
        {
            _reliableChannel.Send(data);
        }

        internal void SendUnreliable(byte[] data)
        {
            _unreliableChannel.Send(data);
        }
    }
}
