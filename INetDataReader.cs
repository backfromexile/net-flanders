using System;

namespace NetFlanders
{
    public interface INetDataReader
    {
        int Size { get; }

        void Reset();
        byte ReadByte();
        sbyte ReadSByte();
        ushort ReadUInt16();
        short ReadInt16();
        uint ReadUInt32();
        int ReadInt32();
        ulong ReadUInt64();
        long ReadInt64();
        float ReadSingle();
        double ReadDouble();
        char ReadCharacter();
        string ReadString();

        TEnum ReadEnum<TEnum>() where TEnum : unmanaged, Enum;
        TMessage ReadMessage<TMessage>() where TMessage : INetMessage, new();
        TMessage ReadMessage<TMessage>(TMessage readInto) where TMessage : INetMessage;


        byte[] ReadByteArray();
        sbyte[] ReadSByteArray();
        ushort[] ReadUInt16Array();
        short[] ReadInt16Array();
        uint[] ReadUInt32Array();
        int[] ReadInt32Array();
        ulong[] ReadUInt64Array();
        long[] ReadInt64Array();
        float[] ReadSingleArray();
        double[] ReadDoubleArray();
        char[] ReadCharacterArray();
        string[] ReadStringArray();

        TEnum[] ReadEnumArray<TEnum>() where TEnum : unmanaged, Enum;
    }
}