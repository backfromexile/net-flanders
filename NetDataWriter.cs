using System;
using System.Net;
using System.Text;

namespace NetFlanders
{
    public sealed class NetDataWriter
    {
        private readonly Encoding _encoding = Encoding.UTF8;
        private readonly byte[] _buffer = new byte[1024];

        private int _position = 0;
        public int Size => _position;

        public byte[] GetRawData()
        {
            var data = new byte[_position];
            Buffer.BlockCopy(_buffer, 0, data, 0, data.Length);
            return data;
        }

        public void PutRaw(byte[] value)
        {
            Buffer.BlockCopy(value, 0, _buffer, _position, value.Length);
            _position += value.Length * sizeof(byte);
        }

        public void Put(byte value)
        {
            _buffer[_position] = value;
            _position++;
        }

        public void Put(sbyte value)
        {
            _buffer[_position] = (byte)value;
            _position++;
        }

        public void Put(ushort value)
        {
            Put((short)value);
        }

        public void Put(short value)
        {
            value = IPAddress.HostToNetworkOrder(value);
            _buffer[_position + 0] = (byte)(value >> 0);
            _buffer[_position + 1] = (byte)(value >> 8);

            _position += sizeof(short);
        }

        internal NetDataReader GetReader()
        {
            var data = GetRawData();
            return new NetDataReader(data);
        }

        internal void Clear()
        {
            _position = 0;
        }

        public void Put(uint value)
        {
            Put((int)value);
        }

        public void Put(int value)
        {
            value = IPAddress.HostToNetworkOrder(value);
            _buffer[_position + 0] = (byte)(value >> 0);
            _buffer[_position + 1] = (byte)(value >> 8);
            _buffer[_position + 2] = (byte)(value >> 16);
            _buffer[_position + 3] = (byte)(value >> 24);

            _position += sizeof(int);
        }

        public void Put(ulong value)
        {
            Put((long)value);
        }

        public void Put(long value)
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

        public unsafe void Put(float value)
        {
            Put(*(int*)&value);
        }

        public unsafe void Put(double value)
        {
            Put(*(long*)&value);
        }

        public void Put(char value)
        {
            Put((short)value);
        }

        public void Put(string value)
        {
            ushort bytes = (ushort)_encoding.GetByteCount(value);
            Put(bytes);

            _ = _encoding.GetBytes(value, 0, value.Length, _buffer, _position);
            _position += bytes;
        }

        public void Put(byte[] value)
        {
            Put((ushort)value.Length);

            Buffer.BlockCopy(value, 0, _buffer, _position, value.Length);
            _position += value.Length * sizeof(byte);
        }

        public void Put(sbyte[] value)
        {
            Put((ushort)value.Length);

            Buffer.BlockCopy(value, 0, _buffer, _position, value.Length);
            _position += value.Length * sizeof(sbyte);
        }

        public void Put(ushort[] value)
        {
            Put((ushort)value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Put(value[i]);
            }
        }

        public void Put(short[] value)
        {
            Put((ushort)value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Put(value[i]);
            }
        }

        public void Put(uint[] value)
        {
            Put((ushort)value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Put(value[i]);
            }
        }

        public void Put(int[] value)
        {
            Put((ushort)value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Put(value[i]);
            }
        }

        public void Put(ulong[] value)
        {
            Put((ushort)value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Put(value[i]);
            }
        }

        public void Put(long[] value)
        {
            Put((ushort)value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Put(value[i]);
            }
        }

        public void Put(float[] value)
        {
            Put((ushort)value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Put(value[i]);
            }
        }

        public void Put(double[] value)
        {
            Put((ushort)value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Put(value[i]);
            }
        }

        public void Put(char[] value)
        {
            Put((ushort)value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Put(value[i]);
            }
        }

        public void Put(string[] value)
        {
            Put((ushort)value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Put(value[i]);
            }
        }

        public void PutMessage<TMessage>(TMessage value) where TMessage : INetMessage
        {
            value.NetSerialize(this);
        }
        public unsafe void Put<TEnum>(TEnum value) where TEnum : unmanaged, Enum
        {
            var underlyingType = Enum.GetUnderlyingType(typeof(TEnum));
            switch (underlyingType.FullName)
            {
                case "System.Byte":
                    Put(*(byte*)&value);
                    return;

                case "System.SByte":
                    Put(*(sbyte*)&value);
                    return;

                case "System.UInt16":
                    Put(*(ushort*)&value);
                    return;

                case "System.Int16":
                    Put(*(short*)&value);
                    return;

                case "System.UInt32":
                    Put(*(uint*)&value);
                    return;

                case "System.Int32":
                    Put(*(int*)&value);
                    return;

                case "System.UInt64":
                    Put(*(ulong*)&value);
                    return;

                case "System.Int64":
                    Put(*(long*)&value);
                    return;

                default:
                    throw new NotSupportedException();
            }
        }

        public void Put<TEnum>(TEnum[] value) where TEnum : unmanaged, Enum
        {
            Put((ushort)value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Put(value[i]);
            }
        }

        public void Put(NetDataReader reader)
        {
            PutRaw(reader.RawData);
        }
    }
}