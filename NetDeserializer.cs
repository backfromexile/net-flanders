using System;
using System.Net;
using System.Text;

namespace NetFlanders
{
    public sealed class NetDeserializer
    {
        private readonly Encoding _encoding = Encoding.UTF8;
        private readonly byte[] _buffer;
        public int Size => _buffer.Length;
        private int _position;

        public NetDeserializer(byte[] data) : this(new ReadOnlyMemory<byte>(data))
        {
        }

        public NetDeserializer(ReadOnlyMemory<byte> data)
        {
            // we don't want to hold the reference to the array in case someone changes values
            _buffer = new byte[data.Length];
            data.CopyTo(_buffer);
        }

        public byte ReadByte()
        {
            return _buffer[_position++];
        }

        public sbyte ReadSByte()
        {
            return (sbyte)_buffer[_position++];
        }

        public short ReadInt16()
        {
            var value = BitConverter.ToInt16(_buffer, _position);
            value = IPAddress.HostToNetworkOrder(value);

            _position += sizeof(short);

            return value;
        }

        public ushort ReadUInt16()
        {
            return (ushort)ReadInt16();
        }

        public int ReadInt32()
        {
            var value = BitConverter.ToInt32(_buffer, _position);
            value = IPAddress.HostToNetworkOrder(value);

            _position += sizeof(int);

            return value;
        }

        public uint ReadUInt32()
        {
            return (uint)ReadInt32();
        }

        public long ReadInt64()
        {
            var value = BitConverter.ToInt64(_buffer, _position);
            value = IPAddress.HostToNetworkOrder(value);

            _position += sizeof(long);

            return value;
        }

        public ulong ReadUInt64()
        {
            return (ulong)ReadInt64();
        }

        public unsafe float ReadSingle()
        {
            var value = ReadInt32();
            return *(float*)&value;
        }

        public unsafe double ReadDouble()
        {
            var value = ReadInt64();
            return *(double*)&value;
        }

        public char ReadChar()
        {
            return (char)ReadInt16();
        }

        public string ReadString()
        {
            var bytes = ReadUInt16();
            var result = _encoding.GetString(_buffer, _position, bytes);
            _position += bytes;
            return result;
        }

        public unsafe TEnum ReadEnum<TEnum>()
            where TEnum : unmanaged, Enum
        {
            switch (sizeof(TEnum))
            {
                case sizeof(byte):
                    {
                        var value = ReadByte();
                        return *(TEnum*)&value;
                    }

                case sizeof(short):
                    {
                        var value = ReadInt16();
                        return *(TEnum*)&value;
                    }

                case sizeof(int):
                    {
                        var value = ReadInt32();
                        return *(TEnum*)&value;
                    }

                case sizeof(long):
                    {
                        var value = ReadInt64();
                        return *(TEnum*)&value;
                    }

                default: throw new NotSupportedException();
            }
        }

        public T ReadSerializable<T>()
            where T : INetSerializable, new()
        {
            var result = new T();
            result.Deserialize(this);
            return result;
        }
    }
}
