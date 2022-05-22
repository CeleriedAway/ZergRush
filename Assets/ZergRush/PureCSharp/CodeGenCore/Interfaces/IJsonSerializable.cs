using Newtonsoft.Json;

public interface IJsonSerializable
{
    void WriteJsonFields(JsonTextWriter writer);
    void ReadFromJsonField(JsonTextReader reader, string name);
}