using System.IO;

public interface ISerializable
{
    void Serialize(BinaryWriter writer);
    void Deserialize(BinaryReader reader);
}