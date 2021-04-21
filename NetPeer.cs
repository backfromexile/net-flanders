using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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
    internal sealed class NetPeer
    {

        #region ping
        public TimeSpan Ping => new TimeSpan(_roundTripTime.Ticks / _pingCount / 2);

        private TimeSpan _roundTripTime;
        private int _pingCount;
        private ushort _pingSequence;
        private readonly Stopwatch _pingStopwatch = new Stopwatch();
        private readonly ConcurrentDictionary<ushort, TimeSpan> _pingTimes = new ConcurrentDictionary<ushort, TimeSpan>();
        private readonly object _pingLock = new object();
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
            SendPing();

            PollPackets();
        }

        private void PollPackets()
        {
            while (_unreliableChannel.TryPollPacket(out var packet))
            {
                //TODO: 
                Socket.Logger.Log($"Polled unreliable packet {packet.SequenceNumber}");
            }

            while (_reliableChannel.TryPollPacket(out var packet))
            {
                //TODO: 
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
                    ConnectionResponse?.Invoke(this, true);
                    break;

                case NetPacketType.ConnectionReject:
                    ConnectionResponse?.Invoke(this, false);
                    break;

                case NetPacketType.ConnectionRequest:
                    // ignore connection requests in client mode
                    if (Socket.ClientMode)
                        return;

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
                    _unreliableChannel.HandleReceivedPacket(packet);
                    return;

                case NetPacketType.Reliable:
                    _reliableChannel.HandleReceivedPacket(packet);
                    return;

                case NetPacketType.Ping:
                    {
                        var answer = new NetPacket(NetPacketType.Pong, packet.SequenceNumber);
                        Send(answer);
                        return;
                    }

                case NetPacketType.Pong:
                    {
                        //TODO: ping should use a sliding window and not be all-time average
                        //TODO: remove old ping times where the packet was dropped (keep older ones for a short time)
                        //TODO: 
                        var seq = packet.SequenceNumber;
                        if (!_pingTimes.TryRemove(seq, out var time))
                            return;

                        Socket.Logger.LogDebug($"{_endpoint}: Still waiting for {_pingTimes.Count} ping packets");

                        var now = _pingStopwatch.Elapsed;
                        lock (_pingLock)
                        {
                            _roundTripTime += (now - time);
                            _pingCount++;
                        }
                        return;
                    }

                case NetPacketType.Disconnect:
                    {
                        State.Apply(NetPeerCommand.Disconnect);

                        Disconnected?.Invoke(DisconnectReason.RemoteDisconnected);
                        return;
                    }

                case NetPacketType.Ack:
                    {
                        _reliableChannel.HandleAck(packet);
                        return;
                    }

                case NetPacketType.Debug:
                    {
                        //TODO: debug
                        return;
                    }
            }
        }

        private void SendPing()
        {
            ushort seq;
            lock (_pingLock)
            {
                seq = ++_pingSequence;
            }

            var time = _pingStopwatch.Elapsed;
            _pingTimes.TryAdd(seq, time);

            Send(new NetPacket(NetPacketType.Ping, seq));
        }

        internal void Send(NetPacket packet)
        {
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
