namespace NetFlanders
{
    internal enum NetPacketFlags : byte
    {
        None = 0,
        Ack = 1 << 0,
        Ping = 1 << 1,

        RequestConnection = 1 << 2,
        AcceptConnection = 1 << 3,

        Disconnect = 1 << 4,
        Shutdown = 1 << 5,

        //Merged = 1 << 7,
    }
}