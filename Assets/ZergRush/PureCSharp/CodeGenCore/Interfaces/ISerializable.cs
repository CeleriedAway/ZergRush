using System.IO;

public interface IBinaryDeserializable
{
    void Deserialize(BinaryReader reader);
}

public interface IBinarySerializable
{
    void Serialize(BinaryWriter writer);
}

public interface ISerializable : IBinarySerializable, IBinaryDeserializable
{
}