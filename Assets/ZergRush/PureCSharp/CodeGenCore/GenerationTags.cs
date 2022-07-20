﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using ZergRush.Alive;
using Newtonsoft.Json;
using ZergRush.CodeGen;
using ZergRush.ReactiveCore;

namespace ZergRush.CodeGen
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class CanBeNull : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
// Useful to provide default value to cells in autogenerated constructors
    public class DefaultVal : Attribute
    {
        public object val;

        public DefaultVal(object val)
        {
            this.val = val;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class GenTask : Attribute
    {
        public GenTaskFlags flags;

        public GenTask(GenTaskFlags flags)
        {
            this.flags = flags;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property |
                    AttributeTargets.Struct)]
    public class GenIgnore : Attribute
    {
        public GenTaskFlags flags;

        public GenIgnore()
        {
            this.flags = GenTaskFlags.All;
        }

        public GenIgnore(GenTaskFlags flags)
        {
            this.flags = flags;
        }
    }

    // Needed if you want to store Livable object in hierarchy but wat it to behave just like usual data
    // Usefull if you store a prototype of a livable object
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class JustData : Attribute
    {
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = false)]
public class GenTargetFolder : Attribute
{
    public int priority;
    public string folder;

    public GenTargetFolder(string folder, int priority = 1)
    {
        this.folder = folder;
        this.priority = priority;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = false)]
public class GenDefaultFolder : GenTargetFolder
{
    public GenDefaultFolder() : base(null)
    {
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = false)]
public class GenInLocalFolder : GenTargetFolder
{
    static string GetPath(string dir, string sourceFilePath = "")
    {
        var path = Path.GetDirectoryName(sourceFilePath);
        return Path.Combine(path, $"{dir}");
    }
    
    public GenInLocalFolder(string dir = "x_generated", [CallerFilePath] string sourceFilePath = "") : base(GetPath(dir, sourceFilePath), 100)
    {
        //Debug.Log("~~~~~~~~~" + folder);
    }
}

public class GenZergRushFolder : GenInLocalFolder
{
    public GenZergRushFolder(string dir = "x_generated", [CallerFilePath] string sourceFilePath = "") : base(dir, sourceFilePath)
    {
        priority = 10000;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class DoNotGen : Attribute
{
    public DoNotGen()
    {
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class GenDoNotInheritGenTags : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public class IsDebug : Attribute
{
}

[AttributeUsage(AttributeTargets.Class)]
public class GenModelRootSetup : Attribute
{
}


[AttributeUsage(AttributeTargets.Class)]
public class GenPolymorphicNode : Attribute
{
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property)]
public class Immutable : Attribute
{
}

[AttributeUsage(AttributeTargets.Class)]
public class HasRefId : Attribute
{
}

[AttributeUsage(AttributeTargets.Class)]
public class RootType : Attribute
{
    public Type type;

    public RootType(Type type)
    {
        this.type = type;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class GenUpdatedEvent : Attribute
{
}

public class __GenReplaceFieldBase : Attribute
{
    public int line;
    public string file;
    public string name;
    public Type type;
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class GenUICell : __GenReplaceFieldBase
{
    GenUICell genUICell;

    public GenUICell([CallerFilePath] string file = "",
        [CallerMemberName] string member = "",
        [CallerLineNumber] int line = 0)
    {
        this.line = line;
        this.file = file;
        this.name = member;
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class GenRecordable : __GenReplaceFieldBase
{
    public GenRecordable(
        [CallerFilePath] string file = "",
        [CallerMemberName] string member = "",
        [CallerLineNumber] int line = 0)
    {
        this.line = line;
        this.file = file;
        this.name = member;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class GenTaskCustomImpl : GenTask
{
    public bool genBaseMethods;

    public GenTaskCustomImpl(GenTaskFlags flags, bool genBaseMethods = false) : base(flags)
    {
        this.flags = flags;
        this.genBaseMethods = genBaseMethods;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class GenInclude : Attribute
{
    public GenTaskFlags flags;

    public GenInclude()
    {
        this.flags = GenTaskFlags.All;
    }

    public GenInclude(GenTaskFlags flags)
    {
        this.flags = flags;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class CantBeAncestor : Attribute
{
}

// Says that this filed used in composition of unique identifier of this instance
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class UIDComponent : Attribute
{
}

// If this attribute is set this static function will be called during main codegeneration
// process and you can request types for generation in main chunk with CodeGen.RequestGen
// and use CodeGen.context to gen modules and classes ect...
[AttributeUsage(AttributeTargets.Method)]
public class CodeGenExtension : Attribute
{
}