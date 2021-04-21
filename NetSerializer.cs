using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace NetFlanders
{
    public sealed class NetSerializer
    {
        private readonly Encoding _encoding = Encoding.UTF8;

        private readonly byte[] _buffer;
        private int _position;
        public int Size => _position;

        public NetSerializer() : this(1024)
        {

        }

        public NetSerializer(int maxSize)
        {
            _buffer = new byte[maxSize];
        }

        public void Reset()
        {
            _position = 0;
        }

        public byte[] GetBytes()
        {
            byte[] result = new byte[_position];
            Buffer.BlockCopy(_buffer, 0, result, 0, result.Length);

            return result;
        }

        public void WriteRaw(ReadOnlyMemory<byte> bytes)
        {
            bytes.CopyTo(new Memory<byte>(_buffer, _position, bytes.Length));
            _position += bytes.Length;
        }

        public void Write(byte value)
        {
            _buffer[_position] = value;
            _position++;
        }

        public void Write(sbyte value)
        {
            Write((byte)value);
        }

        public void Write(short value)
        {
            value = IPAddress.HostToNetworkOrder(value);
            _buffer[_position + 0] = (byte)(value >> 0);
            _buffer[_position + 1] = (byte)(value >> 8);
            _position += sizeof(short);
        }

        public void Write(ushort value)
        {
            Write((short)value);
        }

        public void Write(int value)
        {
            value = IPAddress.HostToNetworkOrder(value);
            _buffer[_position + 0] = (byte)(value >> 0);
            _buffer[_position + 1] = (byte)(value >> 8);
            _buffer[_position + 2] = (byte)(value >> 16);
            _buffer[_position + 3] = (byte)(value >> 24);
            _position += sizeof(int);
        }

        public void Write(uint value)
        {
            Write((int)value);
        }

        public void Write(long value)
        {
            value = IPAddress.HostToNetworkOrder(value);
            _buffer[_position + 0] = (byte)(value >> 0);
            _buffer[_position + 1] = (byte)(value >> 8);
            _buffer[_position + 2] = (byte)(value >> 16);
            _buffer[_position + 3] = (byte)(value >> 24);
            _buffer[_position + 4] = (byte)(value >> 32);
            _buffer[_position + 5] = (byte)(value >> 40);
            _buffer[_position + 6] = (byte)(value >> 48);
            _buffer[_position + 7] = (byte)(value >> 56);
            _position += sizeof(long);
        }

        public void Write(ulong value)
        {
            Write((long)value);
        }

        public unsafe void Write(float value)
        {
            Write(*(int*)&value);
        }

        public unsafe void Write(double value)
        {
            Write(*(long*)&value);
        }

        public void Write(char value)
        {
            Write((short)value);
        }

        public void Write(string value)
        {
            var bytes = _encoding.GetBytes(value, 0, value.Length, _buffer, _position);
            _position += bytes;
        }

        public void Write(IEnumerable<byte> values)
        {
            if (values is byte[] array)
            {
                Write((ushort)array.Length);
                WriteRaw(array);
                return;
            }

            var lengthPos = _position;
            var length = (ushort)0;
            foreach (var value in values)
            {
                Write(value);
                length++;
            }

            var pos = _position;
            _position = lengthPos;
            Write(length);
            _position = pos;
        }

        public void Write(IEnumerable<sbyte> values)
        {
            var lengthPos = _position;
            var length = (ushort)0;
            foreach (var value in values)
            {
                Write(value);
                length++;
            }

            var pos = _position;
            _position = lengthPos;
            Write(length);
            _position = pos;
        }

        public void Write(IEnumerable<short> values)
        {
            var lengthPos = _position;
            var length = (ushort)0;
            foreach (var value in values)
            {
                Write(value);
                length++;
            }

            var pos = _position;
            _position = lengthPos;
            Write(length);
            _position = pos;
        }

        public void Write(IEnumerable<ushort> values)
        {
            var lengthPos = _position;
            var length = (ushort)0;
            foreach (var value in values)
            {
                Write(value);
                length++;
            }

            var pos = _position;
            _position = lengthPos;
            Write(length);
            _position = pos;
        }

        public void Write(IEnumerable<int> values)
        {
            var lengthPos = _position;
            var length = (ushort)0;
            foreach (var value in values)
            {
                Write(value);
                length++;
            }

            var pos = _position;
            _position = lengthPos;
            Write(length);
            _position = pos;
        }

        public void Write(IEnumerable<uint> values)
        {
            var lengthPos = _position;
            var length = (ushort)0;
            foreach (var value in values)
            {
                Write(value);
                length++;
            }

            var pos = _position;
            _position = lengthPos;
            Write(length);
            _position = pos;
        }

        public void Write(IEnumerable<long> values)
        {
            var lengthPos = _position;
            var length = (ushort)0;
            foreach (var value in values)
            {
                Write(value);
                length++;
            }

            var pos = _position;
            _position = lengthPos;
            Write(length);
            _position = pos;
        }

        public void Write(IEnumerable<ulong> values)
        {
            var lengthPos = _position;
            var length = (ushort)0;
            foreach (var value in values)
            {
                Write(value);
                length++;
            }

            var pos = _position;
            _position = lengthPos;
            Write(length);
            _position = pos;
        }

        public void Write(IEnumerable<float> values)
        {
            var lengthPos = _position;
            var length = (ushort)0;
            foreach (var value in values)
            {
                Write(value);
                length++;
            }

            var pos = _position;
            _position = lengthPos;
            Write(length);
            _position = pos;
        }

        public void Write(IEnumerable<double> values)
        {
            var lengthPos = _position;
            var length = (ushort)0;
            foreach (var value in values)
            {
                Write(value);
                length++;
            }

            var pos = _position;
            _position = lengthPos;
            Write(length);
            _position = pos;
        }

        public void Write(IEnumerable<char> values)
        {
            var lengthPos = _position;
            var length = (ushort)0;
            foreach (var value in values)
            {
                Write(value);
                length++;
            }

            var pos = _position;
            _position = lengthPos;
            Write(length);
            _position = pos;
        }

        public unsafe void Write<TEnum>(TEnum value)
            where TEnum : unmanaged, Enum
        {
            switch (sizeof(TEnum))
            {
                case sizeof(byte):
                    {
                        Write(*(byte*)&value);
                        return;
                    }

                case sizeof(short):
                    {
                        Write(*(short*)&value);
                        return;
                    }

                case sizeof(int):
                    {
                        Write(*(int*)&value);
                        return;
                    }

                case sizeof(long):
                    {
                        Write(*(long*)&value);
                        return;
                    }

                default: throw new NotSupportedException();
            }
        }

        public void Write<TEnum>(IEnumerable<TEnum> values)
            where TEnum : unmanaged, Enum
        {
            var lengthPos = _position;
            var length = (ushort)0;
            foreach (var value in values)
            {
                Write(value);
                length++;
            }

            var pos = _position;
            _position = lengthPos;
            Write(length);
            _position = pos;
        }

        public unsafe void WriteSerializable<T>(T value)
            where T : INetSerializable
        {
            value.Serialize(this);
        }
    }
}
