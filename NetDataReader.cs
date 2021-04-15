using System;
using System.Text;

namespace NetFlanders
{
    public sealed class NetDataReader
    {
        private readonly Encoding _encoding = Encoding.UTF8;
        private readonly byte[] _buffer;

        private int _position = 0;
        public int Size => _buffer.Length;

        public byte[] RawData => _buffer;

        public NetDataReader(byte[] data)
        {
            _buffer = data;
        }

        public NetDataReader(NetDataReader reader)
        {
            _buffer = new byte[reader.Size - reader._position];
            Buffer.BlockCopy(reader._buffer, reader._position, _buffer, 0, _buffer.Length);
        }

        public void Reset()
        {
            _position = 0;
        }

        public byte ReadByte()
        {
            return _buffer[_position++];
        }

        public byte[] ReadByteArray()
        {
            var length = ReadUInt16();

            var result = new byte[length];
            Buffer.BlockCopy(_buffer, _position, result, 0, length);
            _position += length;

            return result;
        }

        public char ReadCharacter()
        {
            return (char)ReadInt16();
        }

        public char[] ReadCharacterArray()
        {
            var length = ReadUInt16();
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = ReadCharacter();
            }

            return result;
        }

        public unsafe double ReadDouble()
        {
            long value = ReadInt64();
            return *(double*)&value;
        }

        public double[] ReadDoubleArray()
        {
            var length = ReadUInt16();
            var result = new double[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = ReadDouble();
            }

            return result;
        }

        public short ReadInt16()
        {
            short value = (short)(
                  (_buffer[_position + 0] << 0)
                + (_buffer[_position + 1] << 8)
            );
            _position += sizeof(short);

            return value;
        }

        public short[] ReadInt16Array()
        {
            var length = ReadUInt16();
            var result = new short[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = ReadInt16();
            }

            return result;
        }

        public int ReadInt32()
        {
            int value = (
                  (_buffer[_position + 0] << 0)
                + (_buffer[_position + 1] << 8)
                + (_buffer[_position + 2] << 16)
                + (_buffer[_position + 3] << 24)
            );
            _position += sizeof(int);

            return value;
        }

        public int[] ReadInt32Array()
        {
            var length = ReadUInt16();
            var result = new int[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = ReadInt32();
            }

            return result;
        }

        public long ReadInt64()
        {
            long value = (
                  ((long)_buffer[_position + 0] << 0)
                + ((long)_buffer[_position + 1] << 8)
                + ((long)_buffer[_position + 2] << 16)
                + ((long)_buffer[_position + 3] << 24)
                + ((long)_buffer[_position + 4] << 32)
                + ((long)_buffer[_position + 5] << 40)
                + ((long)_buffer[_position + 6] << 48)
                + ((long)_buffer[_position + 7] << 56)
            );
            _position += sizeof(long);

            return value;
        }

        public long[] ReadInt64Array()
        {
            var length = ReadUInt16();
            var result = new long[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = ReadInt64();
            }

            return result;
        }

        public TMessage ReadMessage<TMessage>() where TMessage : INetMessage, new()
        {
            return ReadMessage(new TMessage());
        }

        public TMessage ReadMessage<TMessage>(TMessage readInto) where TMessage : INetMessage
        {
            readInto.NetDeserialize(this);
            return readInto;
        }

        public sbyte ReadSByte()
        {
            return (sbyte)_buffer[_position++];
        }

        public sbyte[] ReadSByteArray()
        {
            var length = ReadUInt16();

            var result = new sbyte[length];
            Buffer.BlockCopy(_buffer, _position, result, 0, length);
            _position += length;

            return result;
        }

        public unsafe float ReadSingle()
        {
            var value = ReadInt32();
            return *(float*)&value;
        }

        public float[] ReadSingleArray()
        {
            var length = ReadUInt16();
            var result = new float[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = ReadSingle();
            }

            return result;
        }

        public string ReadString()
        {
            short bytes = ReadInt16();
            string value = _encoding.GetString(_buffer, _position, bytes);
            _position += bytes;
            return value;
        }

        public string[] ReadStringArray()
        {
            var length = ReadUInt16();
            var result = new string[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = ReadString();
            }

            return result;
        }

        public ushort ReadUInt16()
        {
            return (ushort)ReadInt16();
        }

        public ushort[] ReadUInt16Array()
        {
            var length = ReadUInt16();
            var result = new ushort[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = ReadUInt16();
            }

            return result;
        }

        public uint ReadUInt32()
        {
            return (uint)ReadInt32();
        }

        public uint[] ReadUInt32Array()
        {
            var length = ReadUInt16();
            var result = new uint[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = ReadUInt32();
            }

            return result;
        }

        public ulong ReadUInt64()
        {
            return (ulong)ReadInt64();
        }

        public ulong[] ReadUInt64Array()
        {
            var length = ReadUInt16();
            var result = new ulong[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = ReadUInt64();
            }

            return result;
        }

       

        public unsafe TEnum ReadEnum<TEnum>() where TEnum : unmanaged, Enum
        {
            var underlyingType = Enum.GetUnderlyingType(typeof(TEnum));
            switch (underlyingType.FullName)
            {
                case "System.Byte":
                    {
                        var value = ReadByte();
                        return *(TEnum*)&value;
                    }

                case "System.SByte":
                    {
                        var value = ReadSByte();
                        return *(TEnum*)&value;
                    }

                case "System.UInt16":
                    {
                        var value = ReadUInt16();
                        return *(TEnum*)&value;
                    }

                case "System.Int16":
                    {
                        var value = ReadInt16();
                        return *(TEnum*)&value;
                    }

                case "System.UInt32":
                    {
                        var value = ReadUInt32();
                        return *(TEnum*)&value;
                    }

                case "System.Int32":
                    {
                        var value = ReadInt32();
                        return *(TEnum*)&value;
                    }

                case "System.UInt64":
                    {
                        var value = ReadUInt64();
                        return *(TEnum*)&value;
                    }

                case "System.Int64":
                    {
                        var value = ReadInt64();
                        return *(TEnum*)&value;
                    }

                default:
                    throw new NotSupportedException();
            }
        }

        public TEnum[] ReadEnumArray<TEnum>() where TEnum : unmanaged, Enum
        {
            var length = ReadUInt16();
            var result = new TEnum[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = ReadEnum<TEnum>();
            }

            return result;
        }
    }
}