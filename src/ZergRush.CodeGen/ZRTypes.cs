using System.Reflection;

namespace ZergRush.CodeGen;

public enum ZRTypeKind
{
    Unknown,
    Class,
    Struct,
    Interface,
    Enum,
    Delegate,
    Primitive,
    GenericParameter,
    Void,
    Error
}

public enum ZRCommonConstruct
{
    None,
    Array,
    List,
    Dictionary,
    Nullable,
    Cell,
    LivableSlot,
    Ref,
    OtherGeneric
}

[Flags]
public enum ZRTypeOption
{
    None = 0,
    DoNotGen = 1 << 0,
    DoNotSortFields = 1 << 1,
    DoNotInheritGenTags = 1 << 2,
    MultipleRefs = 1 << 3,
    ModelRootSetup = 1 << 4,
    PolymorphicNode = 1 << 5,
    Immutable = 1 << 6,
    HasRefId = 1 << 7,
    UpdatedEvent = 1 << 8,
    UidUseClassNameHash = 1 << 9,
    TargetFolder = 1 << 10,
    HasRootType = 1 << 11,
    HasConfigRootType = 1 << 12,
    HasCustomImplementation = 1 << 13,
    HasGenIgnore = 1 << 14,
    External = 1 << 15,
    GenericDefinition = 1 << 16,
    ConstructedGeneric = 1 << 17
}

[Flags]
public enum ZRMemberOption
{
    None = 0,
    CanBeNull = 1 << 0,
    Immutable = 1 << 1,
    JustData = 1 << 2,
    CantBeAncestor = 1 << 3,
    UidComponent = 1 << 4,
    HasDefaultValue = 1 << 5,
    HasArrayLengthConstraint = 1 << 6,
    UnconstrainedArrayLength = 1 << 7,
    HasGenInclude = 1 << 8,
    HasGenIgnore = 1 << 9
}

public enum ZRMemberKind
{
    Unknown,
    Field,
    Property
}

public enum ZRMemberVisibility
{
    Unknown,
    Private,
    Protected,
    Internal,
    ProtectedInternal,
    PrivateProtected,
    Public
}

public enum ZRDataKind
{
    Unknown,
    Member,
    Local,
    Temporary,
    Parameter,
    ArrayElement,
    ListElement,
    DictionaryKey,
    DictionaryValue,
    This
}

[Flags]
public enum ZRDataOption
{
    None = 0,
    CanBeNull = 1 << 0,
    SureIsNull = 1 << 1,
    HasRefAccess = 1 << 2,
    InsideConfigStorage = 1 << 3,
    InsideLivableContainer = 1 << 4,
    JustData = 1 << 5,
    CantBeAncestor = 1 << 6,
    Immutable = 1 << 7,
}

public enum FieldWrapperType
{
    None,
    Cell,
    LivableSlot,
    Nullable
}

public class ZRSourceLocation
{
    public string FilePath = "";
    public int Line;
    public int Column;
}

public class ZRTargetFolderInfo
{
    public string? Folder;
    public bool Inheritable = true;
    public int Priority = 1;
}

public class ZRCustomImplInfo
{
    public GenTaskFlags Flags;
    public bool GenerateBaseMethods;
    public bool Inheritable = true;
}

public class ZRGenericParameter
{
    public string Name = "";
    public List<ZRType> Constraints = new();
    public GenericParameterAttributes Attributes;
}

public class ZRAttributeInfo
{
    public string Name = "";
    public string FullName = "";
    public string SourceText = "";
    public List<object?> ConstructorArguments = new();
    public Dictionary<string, object?> NamedArguments = new();
}

public class ZRType
{
    public static readonly ZRType[] EmptyTypes = Array.Empty<ZRType>();

    public string Name = "";
    public string Namespace = "";
    public string FullName = "";
    public string MetadataName = "";

    public ZRTypeKind Kind;
    public ZRTypeOption Options;
    public ZRCommonConstruct CommonConstruct;

    public ZRType? CommonConstructArgType;
    public ZRType? GenericDefinition;
    public ZRType? BaseType;
    public ZRType? ElementType;
    public ZRType? EnumUnderlyingType;
    public ZRType? RootType;
    public ZRType? ConfigRootType;
    public int ArrayRank;

    public List<ZRType> Interfaces = new();
    public List<ZRType> ChildTypes = new();
    public List<ZRType> GenericArguments = new();
    public List<ZRGenericParameter> GenericParameters = new();
    public List<ZRMember> Members = new();
    public List<ZRData> DataMembers = new();
    public List<ZRMethod> Methods = new();
    public List<ZRAttributeInfo> Attributes = new();
    public List<ZRCustomImplInfo> CustomImplementations = new();

    public GenTaskFlags Flags;
    public GenTaskFlags IgnoreFlags;
    public GenTaskFlags CustomImplementFlags;

    public ZRTargetFolderInfo? TargetFolder;
    public ZRSourceLocation? Source;

    public bool IsResolved = true;
    public bool IsAbstract;
    public bool IsSealed;
    public bool HasDeclaredParameterlessConstructor;
    public bool HasDeclaredConstructors;
    public string WrittenName = "";

    public bool IsClass => Kind == ZRTypeKind.Class;
    public bool IsStructLike => Kind == ZRTypeKind.Struct;
    public bool IsInterface => Kind == ZRTypeKind.Interface;
    public bool IsEnum => Kind == ZRTypeKind.Enum;
    public bool IsPrimitive => Kind == ZRTypeKind.Primitive;
    public bool IsValueType => Kind is ZRTypeKind.Struct or ZRTypeKind.Enum or ZRTypeKind.Primitive;
    public bool IsGenericParameter => Kind == ZRTypeKind.GenericParameter;
    public bool IsArray => CommonConstruct == ZRCommonConstruct.Array;
    public bool IsGenericType => GenericArguments.Count > 0 || GenericParameters.Count > 0 || GenericDefinition != null;
    public bool IsConstructedGenericType => (Options & ZRTypeOption.ConstructedGeneric) != 0 ||
                                            (GenericArguments.Count > 0 && GenericArguments.Any(arg => !arg.IsGenericParameter));
    public ZRType[] GenericTypeArguments => GenericArguments.ToArray();

    public ZRType[] GetGenericArguments()
    {
        if (GenericArguments.Count > 0) return GenericArguments.ToArray();
        return GenericParameters.Select(parameter => new ZRType
        {
            Name = parameter.Name,
            FullName = parameter.Name,
            MetadataName = parameter.Name,
            WrittenName = parameter.Name,
            Kind = ZRTypeKind.GenericParameter,
            GenericParameters = { new ZRGenericParameter
            {
                Name = parameter.Name,
                Constraints = parameter.Constraints,
                Attributes = parameter.Attributes
            } }
        }).ToArray();
    }

    public ZRType? GetElementType()
    {
        return ElementType ?? CommonConstructArgType ?? GenericArguments.FirstOrDefault();
    }

    public int GetArrayRank()
    {
        return ArrayRank == 0 && IsArray ? 1 : ArrayRank;
    }

    public ZRType GetGenericTypeDefinition()
    {
        return GenericDefinition ?? this;
    }

    public ZRType[] GetGenericParameterConstraints()
    {
        if (GenericParameters.Count == 1) return GenericParameters[0].Constraints.ToArray();
        return Array.Empty<ZRType>();
    }

    public object[] GetConstructors()
    {
        return HasDeclaredConstructors ? new object[] { new() } : Array.Empty<object>();
    }

    public object? GetConstructor(ZRType[] _)
    {
        return HasDeclaredParameterlessConstructor || IsValueType || !HasDeclaredConstructors ? new object() : null;
    }

    public ZRMethod[] GetMethods(BindingFlags _)
    {
        return Methods.ToArray();
    }

    public ZRType GetEnumUnderlyingType()
    {
        return EnumUnderlyingType ?? FromSystemType(typeof(int));
    }

    public override string ToString()
    {
        return WrittenName.Valid() ? WrittenName : FullName.Valid() ? FullName : Name;
    }

    public override bool Equals(object? obj)
    {
        return obj switch
        {
            ZRType other => string.Equals(ComparableFullName(this), ComparableFullName(other), StringComparison.Ordinal),
            Type systemType => MatchesSystemType(systemType),
            _ => false
        };
    }

    public override int GetHashCode()
    {
        return ComparableFullName(this).GetHashCode(StringComparison.Ordinal);
    }

    public bool MatchesSystemType(Type systemType)
    {
        var zr = FromSystemType(systemType);
        return string.Equals(ComparableFullName(this), ComparableFullName(zr), StringComparison.Ordinal) ||
               string.Equals(this.RealName(), zr.RealName(), StringComparison.Ordinal);
    }

    public static bool operator ==(ZRType? left, ZRType? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ZRType? left, ZRType? right)
    {
        return !(left == right);
    }

    public static bool operator ==(ZRType? left, Type? right)
    {
        if (left is null || right is null) return left is null && right is null;
        return left.MatchesSystemType(right);
    }

    public static bool operator !=(ZRType? left, Type? right)
    {
        return !(left == right);
    }

    public static bool operator ==(Type? left, ZRType? right)
    {
        return right == left;
    }

    public static bool operator !=(Type? left, ZRType? right)
    {
        return !(right == left);
    }

    public static implicit operator ZRType(Type type)
    {
        return FromSystemType(type);
    }

    public Type? TryResolveSystemType()
    {
        if (this == typeof(void)) return typeof(void);
        if (this == typeof(int)) return typeof(int);
        if (this == typeof(uint)) return typeof(uint);
        if (this == typeof(short)) return typeof(short);
        if (this == typeof(ushort)) return typeof(ushort);
        if (this == typeof(long)) return typeof(long);
        if (this == typeof(ulong)) return typeof(ulong);
        if (this == typeof(byte)) return typeof(byte);
        if (this == typeof(sbyte)) return typeof(sbyte);
        if (this == typeof(float)) return typeof(float);
        if (this == typeof(double)) return typeof(double);
        if (this == typeof(decimal)) return typeof(decimal);
        if (this == typeof(string)) return typeof(string);
        if (this == typeof(char)) return typeof(char);
        if (this == typeof(bool)) return typeof(bool);
        if (this == typeof(object)) return typeof(object);
        if (this == typeof(Guid)) return typeof(Guid);
        if (this == typeof(DateTime)) return typeof(DateTime);
        if (IsArray && ElementType?.TryResolveSystemType() is { } elementType)
        {
            return elementType.MakeArrayType(GetArrayRank());
        }

        if (CommonConstruct == ZRCommonConstruct.Nullable &&
            (CommonConstructArgType ?? GenericArguments.FirstOrDefault())?.TryResolveSystemType() is { } nullableType)
        {
            return typeof(Nullable<>).MakeGenericType(nullableType);
        }

        return System.Type.GetType(FullName) ??
               System.Type.GetType(WrittenName) ??
               AppDomain.CurrentDomain.GetAssemblies()
                   .Select(assembly => assembly.GetType(FullName))
                   .FirstOrDefault(type => type != null);
    }

    public static ZRType FromSystemType(Type type)
    {
        if (type == typeof(void))
        {
            return new ZRType { Name = "Void", FullName = "void", MetadataName = "Void", WrittenName = "void", Kind = ZRTypeKind.Void };
        }

        if (type.IsArray)
        {
            var element = FromSystemType(type.GetElementType()!);
            return new ZRType
            {
                Name = element.Name + "[]",
                FullName = element.FullName + "[]",
                MetadataName = element.MetadataName + "[]",
                WrittenName = element.RealName(true) + "[]",
                Kind = ZRTypeKind.Class,
                CommonConstruct = ZRCommonConstruct.Array,
                ElementType = element,
                CommonConstructArgType = element,
                ArrayRank = type.GetArrayRank()
            };
        }

        var nullable = Nullable.GetUnderlyingType(type);
        if (nullable != null)
        {
            var arg = FromSystemType(nullable);
            return new ZRType
            {
                Name = "Nullable`1",
                Namespace = "System",
                FullName = $"System.Nullable<{arg.FullName}>",
                MetadataName = "Nullable`1",
                WrittenName = $"{arg.RealName(true)}?",
                Kind = ZRTypeKind.Struct,
                CommonConstruct = ZRCommonConstruct.Nullable,
                CommonConstructArgType = arg,
                GenericArguments = { arg }
            };
        }

        var result = new ZRType
        {
            Name = type.Name,
            Namespace = type.Namespace ?? "",
            FullName = KnownCSharpName(type) ?? type.FullName ?? type.Name,
            MetadataName = type.Name,
            WrittenName = KnownCSharpName(type) ?? type.FullName ?? type.Name,
            Kind = KindFromSystemType(type),
            IsAbstract = type.IsAbstract,
            IsSealed = type.IsSealed,
            HasDeclaredParameterlessConstructor = type.GetConstructor(Type.EmptyTypes) != null,
            HasDeclaredConstructors = type.GetConstructors().Length > 0
        };

        if (type.IsGenericType)
        {
            result.GenericArguments = type.GetGenericArguments().Select(FromSystemType).ToList();
            result.CommonConstructArgType = result.GenericArguments.FirstOrDefault();
            result.GenericDefinition = type.IsGenericTypeDefinition ? result : FromSystemType(type.GetGenericTypeDefinition());
            result.CommonConstruct = CommonConstructFromSystemType(type);
            result.Options |= type.IsGenericTypeDefinition ? ZRTypeOption.GenericDefinition : ZRTypeOption.ConstructedGeneric;
        }

        if (type.BaseType != null && type.BaseType != typeof(object))
        {
            result.BaseType = FromSystemType(type.BaseType);
        }

        result.Interfaces = type.GetInterfaces().Select(FromSystemType).ToList();
        return result;
    }

    static ZRTypeKind KindFromSystemType(Type type)
    {
        if (type == typeof(void)) return ZRTypeKind.Void;
        if (type.IsEnum) return ZRTypeKind.Enum;
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal)) return ZRTypeKind.Primitive;
        if (type.IsValueType) return ZRTypeKind.Struct;
        if (type.IsInterface) return ZRTypeKind.Interface;
        if (type.IsClass) return ZRTypeKind.Class;
        if (type.IsGenericParameter) return ZRTypeKind.GenericParameter;
        return ZRTypeKind.Unknown;
    }

    static ZRCommonConstruct CommonConstructFromSystemType(Type type)
    {
        var generic = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        if (generic == typeof(List<>)) return ZRCommonConstruct.List;
        if (generic == typeof(Dictionary<,>)) return ZRCommonConstruct.Dictionary;
        if (generic == typeof(Nullable<>)) return ZRCommonConstruct.Nullable;
        if (generic.Name is "Cell`1" or "ICell`1" or "ICellRW`1") return ZRCommonConstruct.Cell;
        if (generic.Name is "LivableSlot`1" or "DataSlot`1") return ZRCommonConstruct.LivableSlot;
        if (generic.Name == "Ref`1") return ZRCommonConstruct.Ref;
        return type.IsGenericType ? ZRCommonConstruct.OtherGeneric : ZRCommonConstruct.None;
    }

    static string ComparableFullName(ZRType type)
    {
        return type.FullName.Valid() ? type.FullName : type.WrittenName.Valid() ? type.WrittenName : type.Name;
    }

    static string? KnownCSharpName(Type type)
    {
        if (type == typeof(void)) return "void";
        if (type == typeof(int)) return "int";
        if (type == typeof(uint)) return "uint";
        if (type == typeof(short)) return "short";
        if (type == typeof(ushort)) return "ushort";
        if (type == typeof(long)) return "long";
        if (type == typeof(ulong)) return "ulong";
        if (type == typeof(byte)) return "byte";
        if (type == typeof(sbyte)) return "sbyte";
        if (type == typeof(float)) return "float";
        if (type == typeof(double)) return "double";
        if (type == typeof(decimal)) return "decimal";
        if (type == typeof(string)) return "string";
        if (type == typeof(char)) return "char";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(object)) return "object";
        return null;
    }
}

public class ZRMethod
{
    public string Name = "";
    public bool IsVirtual;
    public bool IsAbstract;
    public List<ZRParameter> Parameters = new();

    public ZRParameter[] GetParameters()
    {
        return Parameters.ToArray();
    }
}

public class ZRParameter
{
    public string Name = "";
    public ZRType ParameterType = ZRType.FromSystemType(typeof(object));
}

public class ZRMember
{
    public string Name = "";

    public ZRMemberKind Kind;
    public ZRMemberVisibility Visibility;
    public ZRMemberOption Options;

    // unwrapped type
    public ZRType? MemberType;
    public ZRType? ParentType;
    
    // Wrappers from outermost to innermost, for example Cell<Cell<int?>> => Cell, Cell, Nullable.
    public List<FieldWrapperType> WrapperTypes = new();
    // real type with wrapper for use in constructors
    public ZRType? DeclaredType;

    public GenTaskFlags IgnoreFlags;
    public GenTaskFlags IncludeFlags = GenTaskFlags.All;

    public object? DefaultValue;
    public int? ArrayLengthConstraint;

    public bool IsReadOnly;
    public bool IsResolved = true;

    public ZRSourceLocation? Source;
    public List<ZRAttributeInfo> Attributes = new();

    public ZRData ToData(string? access = null, ZRDataKind kind = ZRDataKind.Member)
    {
        var options = ZRDataOption.None;

        if ((Options & ZRMemberOption.CanBeNull) != 0 ||
            WrapperTypes.Contains(FieldWrapperType.Nullable) ||
            WrapperTypes.Contains(FieldWrapperType.LivableSlot))
        {
            options |= ZRDataOption.CanBeNull;
        }

        if ((Options & ZRMemberOption.JustData) != 0)
        {
            options |= ZRDataOption.JustData;
        }

        if ((Options & ZRMemberOption.CantBeAncestor) != 0)
        {
            options |= ZRDataOption.CantBeAncestor;
        }

        if ((Options & ZRMemberOption.Immutable) != 0)
        {
            options |= ZRDataOption.Immutable;
        }

        if (WrapperTypes.Contains(FieldWrapperType.LivableSlot))
        {
            options |= ZRDataOption.InsideLivableContainer;
        }

        if (DeclaredType?.CommonConstruct == ZRCommonConstruct.Ref)
        {
            options |= ZRDataOption.HasRefAccess;
        }

        return new ZRData
        {
            BaseAccess = access ?? Name,
            Kind = kind,
            Options = options,
            Type = MemberType,
            RealType = DeclaredType,
            DeclaredType = DeclaredType,
            ParentType = ParentType,
            Member = this,
            WrapperTypes = new List<FieldWrapperType>(WrapperTypes),
            IgnoreFlags = IgnoreFlags,
            IncludeFlags = IncludeFlags,
            DefaultValue = DefaultValue,
            ArrayLengthConstraint = ArrayLengthConstraint,
            IsReadOnly = IsReadOnly,
            IsPrivate = Visibility == ZRMemberVisibility.Private,
            Source = Source,
            ValueTransformer = BuildDataAccess
        };
    }

    public string BuildDataAccess(string? access = null)
    {
        var result = access ?? Name;
        foreach (var wrapperType in WrapperTypes)
        {
            result = wrapperType switch
            {
                FieldWrapperType.Cell => $"{result}.value",
                FieldWrapperType.LivableSlot => $"{result}.value",
                FieldWrapperType.Nullable => result,
                _ => result
            };
        }

        return result;
    }
}

public class ZRData
{
    public string BaseAccess = "";
    public string AccessPrefix = "";
    public string PathLog = "";

    public ZRDataKind Kind;
    public ZRDataOption Options;

    public ZRType? Type;
    public ZRType? RealType;
    public ZRType? DeclaredType;
    public ZRType? ParentType;
    public ZRType? CarrierType;
    public ZRMember? Member;
    public ZRSourceLocation? Source;
    public List<FieldWrapperType> WrapperTypes = new();

    public GenTaskFlags IgnoreFlags;
    public GenTaskFlags IncludeFlags = GenTaskFlags.All;

    public object? DefaultValue;
    public int? ArrayLengthConstraint;

    public bool IsReadOnly;
    public bool SureIsNull;
    public bool IsPrivate;

    public Func<string, string> ValueTransformer = access => access;
    public CodeGen.ValueVrapperType ValueWrapper = CodeGen.ValueVrapperType.None;

    public string Name => BaseAccess;
    public string Access => (!string.IsNullOrEmpty(AccessPrefix) ? AccessPrefix + "." : "") + ValueTransformer(BaseAccess);
    public string RealAccess => (!string.IsNullOrEmpty(AccessPrefix) ? AccessPrefix + "." : "") + BaseAccess;
    public string PathName => string.IsNullOrEmpty(PathLog) ? $"\"{BaseAccess}\"" : PathLog;

    public bool CanBeNull
    {
        get => HasOption(ZRDataOption.CanBeNull);
        set => SetOption(ZRDataOption.CanBeNull, value);
    }

    public bool ImmutableData
    {
        get => HasOption(ZRDataOption.Immutable);
        set => SetOption(ZRDataOption.Immutable, value);
    }

    public bool CantBeAncestor
    {
        get => HasOption(ZRDataOption.CantBeAncestor);
        set => SetOption(ZRDataOption.CantBeAncestor, value);
    }

    public bool InsideConfigStorage
    {
        get => HasOption(ZRDataOption.InsideConfigStorage);
        set => SetOption(ZRDataOption.InsideConfigStorage, value);
    }

    public bool InsideLivableContainer
    {
        get => HasOption(ZRDataOption.InsideLivableContainer);
        set => SetOption(ZRDataOption.InsideLivableContainer, value);
    }

    public bool JustData
    {
        get => HasOption(ZRDataOption.JustData);
        set => SetOption(ZRDataOption.JustData, value);
    }

    public bool SureIsNullOption
    {
        get => HasOption(ZRDataOption.SureIsNull);
        set => SetOption(ZRDataOption.SureIsNull, value);
    }

    public string name => Name;
    public string baseAccess { get => BaseAccess; set => BaseAccess = value; }
    public string accessPrefix { get => AccessPrefix; set => AccessPrefix = value; }
    public string access => Access;
    public string realAccess => RealAccess;
    public string pathName => PathName;
    public string pathLog { get => PathLog; set => PathLog = value; }
    public ZRType? type { get => Type; set => Type = value; }
    public ZRType? realType { get => RealType; set => RealType = value; }
    public ZRType? carrierType { get => CarrierType; set => CarrierType = value; }
    public bool canBeNull { get => CanBeNull; set => CanBeNull = value; }
    public bool immutableData { get => ImmutableData; set => ImmutableData = value; }
    public GenTaskFlags ingoreFlags { get => IgnoreFlags; set => IgnoreFlags = value; }
    public bool isReadOnly { get => IsReadOnly; set => IsReadOnly = value; }
    public bool sureIsNull { get => SureIsNull; set { SureIsNull = value; SureIsNullOption = value; } }
    public bool isPrivate { get => IsPrivate; set => IsPrivate = value; }
    public bool cantBeAncestor { get => CantBeAncestor; set => CantBeAncestor = value; }
    public bool insideConfigStorage { get => InsideConfigStorage; set => InsideConfigStorage = value; }
    public bool insideLivableContainer { get => InsideLivableContainer; set => InsideLivableContainer = value; }
    public bool justData { get => JustData; set => JustData = value; }
    public object? defaultValue { get => DefaultValue; set => DefaultValue = value; }
    public Func<string, string> valueTransformer { get => ValueTransformer; set => ValueTransformer = value; }
    public CodeGen.ValueVrapperType isValueWrapper { get => ValueWrapper; set => ValueWrapper = value; }

    public static ZRData WithTypeAndName(ZRType? type, string name)
    {
        return new ZRData { Type = type, BaseAccess = name };
    }

    public ZRData Copy()
    {
        return new ZRData
        {
            BaseAccess = BaseAccess,
            AccessPrefix = AccessPrefix,
            PathLog = PathLog,
            Kind = Kind,
            Options = Options,
            Type = Type,
            RealType = RealType,
            DeclaredType = DeclaredType,
            ParentType = ParentType,
            CarrierType = CarrierType,
            Member = Member,
            Source = Source,
            WrapperTypes = new List<FieldWrapperType>(WrapperTypes),
            IgnoreFlags = IgnoreFlags,
            IncludeFlags = IncludeFlags,
            DefaultValue = DefaultValue,
            ArrayLengthConstraint = ArrayLengthConstraint,
            IsReadOnly = IsReadOnly,
            SureIsNull = SureIsNull,
            IsPrivate = IsPrivate,
            ValueTransformer = ValueTransformer,
            ValueWrapper = ValueWrapper
        };
    }

    public ZRData SetupIsCell()
    {
        RealType ??= DeclaredType ?? Type;
        foreach (var wrapperType in WrapperTypes)
        {
            switch (wrapperType)
            {
                case FieldWrapperType.Cell:
                    ValueTransformer = access => access + ".value";
                    ValueWrapper = CodeGen.ValueVrapperType.Cell;
                    break;
                case FieldWrapperType.LivableSlot:
                    ValueTransformer = access => access + ".value";
                    ValueWrapper = CodeGen.ValueVrapperType.LivableSlot;
                    CanBeNull = true;
                    InsideLivableContainer = true;
                    break;
                case FieldWrapperType.Nullable:
                    ValueWrapper = CodeGen.ValueVrapperType.Nullable;
                    CanBeNull = true;
                    break;
            }
        }

        return this;
    }

    public override string ToString()
    {
        return $"{nameof(BaseAccess)}: {BaseAccess}, {nameof(Type)}: {Type?.FullName ?? "<unknown>"}";
    }

    bool HasOption(ZRDataOption option)
    {
        return (Options & option) != 0;
    }

    void SetOption(ZRDataOption option, bool value)
    {
        if (value)
        {
            Options |= option;
        }
        else
        {
            Options &= ~option;
        }
    }
}
