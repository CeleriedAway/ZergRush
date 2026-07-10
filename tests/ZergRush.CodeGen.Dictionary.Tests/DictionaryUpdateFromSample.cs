using System.Collections.Generic;
using ZergRush.CodeGen;

namespace ZergRush.CodeGen.Dictionary.Tests;

[GenTask(GenTaskFlags.UpdateFrom)]
[GenInLocalFolder]
public partial class DictionaryValue
{
    public int number;
}

[GenTask(GenTaskFlags.UpdateFrom)]
[GenInLocalFolder]
public partial class DictionaryUpdateFromSample
{
    public Dictionary<int, DictionaryValue> values = new();
}
