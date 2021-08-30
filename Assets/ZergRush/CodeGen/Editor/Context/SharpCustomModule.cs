using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine;
using ZergRush;

#if UNITY_EDITOR

namespace ZergRush.CodeGen
{
    public class SharpCustomModule : CodeBuilder, IBuilder
    {
        public StringBuilder contentBuilder = new StringBuilder();
        public StringBuilder usings = new StringBuilder();

        SharpClassBuilder sharpClassBuilder;

        HashSet<string> usingsAppended = new HashSet<string>();
        
        public void usingSink(string include)
        {
            if (usingsAppended.Contains(include)) return;
            usingsAppended.Add(include);
            usings.AppendLine($"using {include};");
        }

        HashSet<string> definesAppended = new HashSet<string>();
        public void defineSink(string define)
        {
            if (definesAppended.Contains(define)) return;
            definesAppended.Add(define);
        }

        public void content(string source)
        {
            contentBuilder.AppendLine(CodeGenTools.Indent(indent) + source);
        }

        public int indent { get; set; }

        List<SharpClassBuilder> classes = new List<SharpClassBuilder>();
        public bool stubMode;
        
        public SharpClassBuilder Class(string name, string namespaceName, bool isStruct, bool isSealed, bool isPartial, bool isStatic)
        {
            sharpClassBuilder = new SharpClassBuilder(this, name, namespaceName, isStruct, isSealed, isPartial, isStatic, stubMode);
            classes.Add(sharpClassBuilder);
            return sharpClassBuilder;
        }

        public override void Commit()
        {
            string fileName = name + (string.IsNullOrEmpty(suffix) ? "" : $".{suffix}") + ".cs";
            if (contentBuilder.Length == 0 && classes.Count(c => !c.Empty) == 0) return;

            StringBuilder result = new StringBuilder(2000);

            if (definesAppended.Count > 0)
                result.AppendLine($"#if {definesAppended.PrintCollection(" && ")}");

            result.AppendLine("using System;");
            result.AppendLine("using System.Collections.Generic;");
            result.AppendLine("using System.Text;");
            result.Append(usings);
            result.Append(contentBuilder);
            
            classes.ForEach(c => c.Commit(s => result.AppendLine(s)));

            if (definesAppended.Count > 0)
                result.AppendLine("#endif");

            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            var combine = Path.Combine(path, fileName);
            File.WriteAllText(combine, GeneratorContext.PrepareToWrite(result.ToString()));
        }
    }
}

#endif