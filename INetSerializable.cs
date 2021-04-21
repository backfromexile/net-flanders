namespace NetFlanders
{
    public interface INetSerializable
    {
        void Serialize(NetSerializer serializer);
        void Deserialize(NetDeserializer serializer);
    }
}
