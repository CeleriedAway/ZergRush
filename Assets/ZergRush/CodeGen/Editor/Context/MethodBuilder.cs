using System;
using System.Text;

#if UNITY_EDITOR

public enum MethodType
{
    Instance,
    Extension,
    StaticFunction,
    StaticExtern,
    Abstract,
    Virtual,
    Override
}

namespace ZergRush.CodeGen
{
    public enum MethodAccess
    {
        Public,
        Protected,
        Private
    }
    public class MethodBuilder
    {
        public MethodBuilder(SharpClassBuilder classBuilder, Type classType, string name, MethodType type, Type returnType, string args, string genericSuffix, string constraints)
        {
            this.classBuilder = classBuilder;
            this.name = name;
            this.type = type;
            this.returnType = returnType;
            this.args = args;
            this.genericSuffix = genericSuffix;
            this.constraints = constraints;
            this.classType = classType;
        }

        public SharpClassBuilder classBuilder;
        public string name;
        public MethodType type;
        public Type returnType;
        public string args;
        public string genericSuffix;
        public string constraints;
        public Type classType;
        public bool async;
        public bool isDebug;
        public MethodAccess access = MethodAccess.Public;

        public bool doNotCallBaseMethod;
        public bool needBaseValCall;

        public bool stubMode;
        public bool doNotGen;
        public int indent = 0;

        StringBuilder builder = new StringBuilder();
        public void content(string str)
        {
            builder.Append(CodeGenTools.Indent(indent + classBuilder.indent));
            builder.AppendLine(str);
        }

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

        public string RemoveBaseIfNeeded(string name)
        {
            if (name.StartsWith("Base"))
            {
                return name.Remove(0, 4);
            }

            return name;
        }

        public void Commit(Action<string> sink)
        {
            if (doNotGen) return;
            
            //if (stubMode && (type == MethodType.Override || (classBuilder.name == "SerializationExtensions"))) return;
            if (stubMode && (type == MethodType.Override )) return;
            
            var indent = "";
            if (classBuilder.namespaceName.Valid())
                indent = ("\t\t");
            else 
                indent = ("\t");

            sink(indent + sig());
            sink($"{indent}{{");
            if (!stubMode)
            {
                if (isDebug)
                {
                    sink("#if !RELEASE");
                }
                if (needBaseValCall && !doNotCallBaseMethod)
                {
                    if (returnType != typeof(void))
                    {
                        sink($"{indent}\tvar baseVal = base.{RemoveBaseIfNeeded(name)}({CodeGenTools.ExtranctArgNames(args)});");
                    }
                    else
                    {
                        sink($"{indent}\tbase.{RemoveBaseIfNeeded(name)}({CodeGenTools.ExtranctArgNames(args)});");
                    }
                }
                sink(builder.ToStringWithoutListLineEnd());
                if (isDebug)
                {
                    sink("#else");
                    sink($"{indent}\tthrow new HackCommandExecutionOnReleaseBuild();");
                    sink("#endif");
                }
            }
            else
            {
                sink(indent + "\tthrow new NotImplementedException();");
            }
            
            sink($"{indent}}}");
        }

        public string sig()
        {
            string sig = access.ToString().ToLower() + " ";
            if (type == MethodType.StaticFunction || type == MethodType.Extension) sig += "static ";
            if (type == MethodType.Abstract) sig += "abstract ";
            else if (type == MethodType.Override) sig += "override ";
            else if (type == MethodType.Virtual) sig += "virtual ";
            
            if (async) sig += "async ";

            sig += $"{(returnType != null ? returnType.RealName(true) : "")} {name}{(genericSuffix.Valid() ? $"{genericSuffix}" : "")}";
            
            var funcFirstArg = type == MethodType.Extension ? $"this {classType.RealName(true)} self" : "";
            sig += $"({CodeGenTools.MergeSig(funcFirstArg, args)}) {constraints}";

            return sig;
        }
    }
}

#endif