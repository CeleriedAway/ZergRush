using Consumer;
using ZergRush;
var value = new Model { values = new[] { 1, 0, -4 } };
var copy = new Model(); copy.UpdateFrom(value);
var json = value.WriteToJsonString().ReadFromJson<Model>();
var binary = value.WriteToByteArray().Read<Model>();
foreach (var item in new[] { copy, json, binary })
{
    if (!item.values.SequenceEqual(value.values) || item.CalculateHash() != value.CalculateHash()) throw new Exception("Consumer round trip failed");
    item.CompareCheck(value, new ZRCompareCheckHelper(), error => throw new Exception(error));
}
// Existing consumer extension calls must remain unambiguous too.
var legacyJson = new ZRJsonTextWriter(new System.IO.StringWriter());
value.values.WriteJson(legacyJson);
var random = new ZergRandom(123);
var loadedRandom = random.WriteToByteArray().Read<ZergRandom>();
var jsonRandom = random.WriteToJsonString().ReadFromJson<ZergRandom>();
for (int i = 0; i < 100; i++)
{
    var next = random.Next();
    if (next != loadedRandom.Next() || next != jsonRandom.Next()) throw new Exception("Wrapper random state changed");
}
Console.WriteLine("PASS: standalone wrapper and consumer serialize/copy/hash/compare/JSON; old helpers coexist.");
