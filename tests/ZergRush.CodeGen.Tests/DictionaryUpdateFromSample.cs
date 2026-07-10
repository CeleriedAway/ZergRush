using System.Collections.Generic;
using ZergRush.Samples;

namespace ZergRush.CodeGen.Tests;

[GenTask(GenTaskFlags.UpdateFrom)]
[GenInLocalFolder]
public partial class DictionaryUpdateFromSample
{
    public Dictionary<int, OtherData> values = new();
}
