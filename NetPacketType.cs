namespace NetFlanders
{
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
        Pong = 201,

        Debug = byte.MaxValue,
    }
}
