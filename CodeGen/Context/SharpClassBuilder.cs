using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if UNITY_EDITOR

namespace ZergRush.CodeGen
{
    public interface IBuilder
    {
        void content(string val);
        int indent { get; set; }
    }
    
    public class SharpClassBuilder : IBuilder
    {
        public SharpClassBuilder(SharpCustomModule module, string name, string namespaceName, bool isStruct, bool isSealed, bool isPartial, bool isStatic, bool stubMode)
        {
            this.module = module;
            this.name = name;
            this.namespaceName = namespaceName;
            this.isStruct = isStruct;
            this.isSealed = isSealed;
            this.isPartial = isPartial;
            this.isStatic = isStatic;
            this.stubMode = stubMode;

            indent++;
            if (namespaceName.Valid())
            {
                indent++;
            }
        }

        public SharpCustomModule module;
        public string name;
        public string namespaceName;
        public bool isStruct;
        public bool isSealed;
        public bool isPartial;
        public bool isStatic;
        public bool doNotGen;
        public bool stubMode;

        public void openBrace()
        {
            content("{");
            indent++;
        }

        public void closeBrace()
        {
            indent--;
            content("}");
        }
        public int indent { get; set; }

        StringBuilder classContent = new StringBuilder();
        StringBuilder _inheritance = new StringBuilder();
        
        List<MethodBuilder> methods = new List<MethodBuilder>();
        
        public MethodBuilder Method(string name, Type classType, MethodType type, Type returnType,
            string args, string genericTypes = "", string constraints = "")
        {
            var methodBuilder = new MethodBuilder(this, classType, name, type, returnType, args, genericTypes, constraints);
            methods.Add(methodBuilder);
            methodBuilder.stubMode = stubMode;
            return methodBuilder;
        }

        public void usingSink(string t)
        {
            module.usingSink(t);
        }

        public void defineSink(string define)
        {
            module.defineSink(define);
        }

        public void content(string source)
        {
            classContent.AppendLine(CodeGenTools.Indent(indent) + source);
        }
        
        public void inheritance(string type)
        {
            if (_inheritance.Length != 0)
            {
                _inheritance.Append(", ");
            }
            else
            {
                _inheritance.Append(" : ");
            }
            _inheritance.Append(type);
        }

        public bool Empty => classContent.Length == 0 && methods.Count == 0 || doNotGen;

        public void Commit(Action<string> lineSink)
        {
            if (Empty) return;

            lineSink("#if !INCLUDE_ONLY_CODE_GENERATION");

            string indent = "";
            if (string.IsNullOrEmpty(namespaceName) == false)
            {
                indent = "\t";
                lineSink($"namespace {namespaceName} {{");
            }

            lineSink($"");
            lineSink($"{indent}public {(isStatic ? "static " : "")}{(!isStruct && isSealed ? "sealed " : "")}{(isPartial ? "partial " : "")}" +
                 $"{(isStruct ? "struct" : "class")} {name}{_inheritance}");
            lineSink($"{indent}{{");
            if (classContent.Length > 0)
                lineSink(classContent.ToStringWithoutListLineEnd());
            foreach (var method in methods)
            {
                method.Commit(lineSink);
            }
            
            lineSink($"{indent}}}");

            if (string.IsNullOrEmpty(namespaceName) == false)
            {
                lineSink("}");
            }

            lineSink("#endif");
        }
    }
}

#endif