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
    public ZRType? RootType;
    public ZRType? ConfigRootType;
    public int ArrayRank;

    public List<ZRType> Interfaces = new();
    public List<ZRType> ChildTypes = new();
    public List<ZRType> GenericArguments = new();
    public List<ZRGenericParameter> GenericParameters = new();
    public List<ZRMember> Members = new();
    public List<ZRAttributeInfo> Attributes = new();
    public List<ZRCustomImplInfo> CustomImplementations = new();

    public GenTaskFlags Flags;
    public GenTaskFlags IgnoreFlags;
    public GenTaskFlags CustomImplementFlags;

    public ZRTargetFolderInfo? TargetFolder;
    public ZRSourceLocation? Source;

    public bool IsResolved = true;
    public string WrittenName = "";
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
}

public class ZRData
{
    public string Access = "";

    public ZRDataKind Kind;
    public ZRDataOption Options;

    public ZRType? Type;
}
