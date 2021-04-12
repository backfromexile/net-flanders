using System;

namespace NetFlanders
{
    public interface INetDataWriter
    {
        int Size { get; }

        void Reset();
        void Put(byte value);
        void Put(sbyte value);
        void Put(ushort value);
        void Put(short value);
        void Put(uint value);
        void Put(int value);
        void Put(ulong value);
        void Put(long value);
        void Put(float value);
        void Put(double value);
        void Put(char value);
        void Put(string value);
        void Put<TEnum>(TEnum value) where TEnum : unmanaged, Enum;
        void PutMessage<TMessage>(TMessage value) where TMessage : INetMessage;

        void Put(byte[] value);
        void Put(sbyte[] value);
        void Put(ushort[] value);
        void Put(short[] value);
        void Put(uint[] value);
        void Put(int[] value);
        void Put(ulong[] value);
        void Put(long[] value);
        void Put(float[] value);
        void Put(double[] value);
        void Put(char[] value);
        void Put(string[] value);
        void Put<TEnum>(TEnum[] value) where TEnum : unmanaged, Enum;
    }
}