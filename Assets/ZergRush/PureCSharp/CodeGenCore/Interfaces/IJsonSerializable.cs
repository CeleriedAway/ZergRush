using Newtonsoft.Json;
using ZergRush;

public interface IJsonSerializable
{
    void WriteJsonFields(ZRJsonTextWriter writer);
    bool ReadFromJsonField(ZRJsonTextReader reader, string name);
}