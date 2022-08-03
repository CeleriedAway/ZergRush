using Newtonsoft.Json;

public interface IJsonSerializable
{
    void WriteJsonFields(JsonTextWriter writer);
    bool ReadFromJsonField(JsonTextReader reader, string name);
}