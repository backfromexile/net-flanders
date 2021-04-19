using System;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetFlanders
{
    internal class SequencedChannel
    {
        private ushort _sequence;

        internal virtual void HandlePacket(NetPacket packet)
        {
            throw new NotImplementedException();
        }
    }

    internal class ReliableChannel : SequencedChannel
    {
        internal void HandleAck(NetPacket packet)
        {
            throw new NotImplementedException();
        }
    }

    public class NetSocket
    {
        private readonly UdpClient _socket;
        private readonly ConcurrentDictionary<IPEndPoint, NetPeer> _peers = new ConcurrentDictionary<IPEndPoint, NetPeer>();

        public NetSocket() : this(0)
        {
        }

        public NetSocket(int port)
        {
            var endpoint = new IPEndPoint(IPAddress.Any, port);
            _socket = new UdpClient(endpoint);
        }

        public void Start()
        {
            ReceiveAsync();
        }

        private void ReceiveAsync()
        {
            _socket.BeginReceive(OnReceive, null);
        }

        private void OnReceive(IAsyncResult asyncResult)
        {
            IPEndPoint? endpoint = null;
            byte[] data = _socket.EndReceive(asyncResult, ref endpoint);

            Console.WriteLine($"Received packet ({data.Length} bytes) from {endpoint}");
            if (data.Length < NetPacket.HeaderSize)
            {
                Console.WriteLine($"Received too small packet ({data.Length} bytes) from {endpoint}, ignoring...");
                ReceiveAsync();
                return;
            }

            //TODO: check if peer (fully) connected
            //TODO: remove this 
            if(!_peers.TryGetValue(endpoint, out var peer))
            {
                peer = new NetPeer(this, endpoint);
                _peers.TryAdd(endpoint, peer);
            }

            //TODO: network byte order and use proper reader type
            var packetType = (NetPacketType)data[0];
            var sequence = BitConverter.ToUInt16(data, 1);
            var dataMemory = new ReadOnlyMemory<byte>(data, NetPacket.HeaderSize, data.Length - NetPacket.HeaderSize);
            var packet = new NetPacket(packetType, sequence, dataMemory);

            peer.HandlePacket(packet);
        }

        public void Send(IPEndPoint endpoint, NetPacketType type, ushort sequence, byte[] data)
        {
            byte[] datagram = new byte[NetPacket.HeaderSize + data.Length];
            datagram[0] = (byte)type;
            Buffer.BlockCopy(BitConverter.GetBytes(sequence), 0, datagram, 1, sizeof(ushort));
            Buffer.BlockCopy(data, 0, datagram, 3, data.Length);

            Console.WriteLine($"Sending {datagram.Length} bytes to {endpoint}");
            _socket.Send(datagram, datagram.Length, endpoint);
        }
    }

    public enum NetPacketType : byte
    {
        Unreliable = 0,
        Reliable = 1,


        ConnectionRequest = 10,
        ConnectionAccept = 11,
        ConnectionReject = 12,

        Disconnect = 20,

        Ack = 100,

        Ping = 200,
        Debug = byte.MaxValue,
    }

    internal readonly struct NetPacket
    {
        internal static int HeaderSize = sizeof(byte) + sizeof(ushort);

        public readonly NetPacketType PacketType;

        public readonly ushort SequenceNumber;
        public readonly ReadOnlyMemory<byte> Body;

        public NetPacket(NetPacketType packetType, ushort sequenceNumber, ReadOnlyMemory<byte> body)
        {
            PacketType = packetType; 
             SequenceNumber = sequenceNumber;
            Body = body;
        }
    }

    internal class NetPeer
    {
        private readonly NetSocket _socket;
        private readonly IPEndPoint _endpoint;
        private readonly SequencedChannel _unreliableChannel = new SequencedChannel();
        private readonly ReliableChannel _reliableChannel = new ReliableChannel();

        internal NetPeer(NetSocket socket, IPEndPoint endpoint)
        {
            _socket = socket;
            _endpoint = endpoint;
        }

        internal void HandlePacket(NetPacket packet)
        {
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
                        //TODO: update ping
                        return;
                    }

                case NetPacketType.Disconnect:
                    {
                        //TODO: handle disconnect
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
    }
}
