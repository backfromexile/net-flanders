using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

namespace NetFlanders
{
    public enum DisconnectReason
    {
        Timeout,
        RemoteDisconnected,
    }

    internal class NetPeer
    {
        #region ping
        public TimeSpan Ping => new TimeSpan(_roundTripTime.Ticks / (2 * _pingCount));

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

        private bool _connected = false; //TODO: this should be a state machine
        #endregion

        private readonly NetSocket _socket;
        private readonly IPEndPoint _endpoint;
        private readonly UnreliableChannel _unreliableChannel;
        private readonly ReliableChannel _reliableChannel;

        internal event Action<DisconnectReason>? Disconnected;


        private NetConfig Config => _socket.Config;

        internal NetPeer(NetSocket socket, IPEndPoint endpoint)
        {
            _socket = socket;
            _endpoint = endpoint;

            _unreliableChannel = new UnreliableChannel(this);
            _reliableChannel = new ReliableChannel(this);

            _pingStopwatch.Start();
            _lastPacketStopwatch.Start();
        }

        internal void SetConnected()
        {
            _connected = true;
        }

        internal void Update()
        {
            if (!_connected)
                return;

            var timeWithoutPacket = _lastPacketStopwatch.Elapsed - _lastPacketTime;
            if(timeWithoutPacket > Config.Timeout)
            {
                //TODO: we timed out
                _connected = false;

                Disconnected?.Invoke(DisconnectReason.Timeout);
                return;
            }

            SendPing();
        }

        internal void HandlePacket(NetPacket packet)
        {
            lock(_stateLock)
            {
                _lastPacketTime = _lastPacketStopwatch.Elapsed;
            }

            switch (packet.PacketType)
            {
                case NetPacketType.ConnectionRequest:
                case NetPacketType.ConnectionAccept:
                case NetPacketType.ConnectionReject:
                    throw new InvalidOperationException();

                case NetPacketType.Unreliable:
                    _unreliableChannel.HandlePacket(packet);
                    return;

                case NetPacketType.Reliable:
                    _reliableChannel.HandlePacket(packet);
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
                        var seq = packet.SequenceNumber;
                        if (!_pingTimes.TryRemove(seq, out var time))
                            return;

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
                        //TODO: handle disconnect

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
            _socket.Send(_endpoint, packet);
        }
    }
}
