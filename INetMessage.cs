namespace NetFlanders
{
    public interface INetMessage
    {
        void NetSerialize(INetDataWriter writer);
        void NetDeserialize(INetDataReader reader);
    }
}