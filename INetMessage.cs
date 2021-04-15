namespace NetFlanders
{
    public interface INetMessage
    {
        void NetSerialize(NetDataWriter writer);
        void NetDeserialize(NetDataReader reader);
    }
}