using System.IO;
using ZergRush;

public interface IBinaryDeserializable
{
    void Deserialize(ZRBinaryReader reader);
}

public interface IBinarySerializable
{
    void Serialize(ZRBinaryWriter writer);
}

public interface ISerializable : IBinarySerializable, IBinaryDeserializable
{
}