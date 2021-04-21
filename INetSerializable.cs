namespace NetFlanders
{
    internal interface INetSerializable
    {
        void Serialize(NetSerializer serializer);
        void Deserialize(NetDeserializer serializer);
    }
}
