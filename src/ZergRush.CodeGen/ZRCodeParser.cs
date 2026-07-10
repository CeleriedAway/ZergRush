using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZergRush.CodeGen;

public sealed class ZRCodeParser
{
    static readonly SymbolDisplayFormat FullNameFormat =
        SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
            .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    readonly Dictionary<string, ZRType> typesByFullName = new(StringComparer.Ordinal);
    readonly HashSet<string> declaredTypeNames = new(StringComparer.Ordinal);
    readonly List<ZRType> parsedTypes = new();
    readonly List<PendingGenericInstanceRegistration> pendingGenericInstanceRegistrations = new();

    sealed class PendingGenericInstanceRegistration
    {
        public required ZRType Type;
        public required INamedTypeSymbol Symbol;
        public required TypeDeclarationSyntax Declaration;
        public required SemanticModel SemanticModel;
        public required ZRAttributeInfo Attribute;
    }

    public IReadOnlyList<ZRType> ParseInputs(IEnumerable<string> inputs)
    {
        var files = ExpandInputFiles(inputs);
        return ParseFiles(files);
    }

    public IReadOnlyList<ZRType> ParseFiles(IEnumerable<string> files)
    {
        typesByFullName.Clear();
        declaredTypeNames.Clear();
        parsedTypes.Clear();
        pendingGenericInstanceRegistrations.Clear();

        var syntaxTrees = files
            .Where(File.Exists)
            .Select(file => CSharpSyntaxTree.ParseText(
                File.ReadAllText(file),
                new CSharpParseOptions(LanguageVersion.Preview),
                file))
            .ToList();

        var compilation = CSharpCompilation.Create(
            "ZRCodeParserInput",
            syntaxTrees,
            DefaultReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

        foreach (var tree in syntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
            var root = tree.GetRoot();

            foreach (var typeDecl in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                ParseTypeDeclaration(typeDecl, model);
            }

            foreach (var enumDecl in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
            {
                ParseEnumDeclaration(enumDecl, model);
            }
        }

        RegisterExplicitGenericInstances(compilation);
        LinkChildTypes();
        BuildDataMembers();
        return parsedTypes;
    }

    public static IReadOnlyList<string> ExpandInputFiles(IEnumerable<string> inputs)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var input in inputs)
        {
            if (string.IsNullOrWhiteSpace(input)) continue;

            var fullPath = Path.GetFullPath(input);
            if (Directory.Exists(fullPath))
            {
                AddDirectoryFiles(fullPath, result);
                continue;
            }

            if (!File.Exists(fullPath)) continue;

            switch (Path.GetExtension(fullPath).ToLowerInvariant())
            {
                case ".cs":
                    result.Add(fullPath);
                    break;
                case ".csproj":
                    AddProjectFiles(fullPath, result);
                    break;
                case ".sln":
                    AddSolutionFiles(fullPath, result);
                    break;
            }
        }

        return result.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
    }

    static void AddDirectoryFiles(string directory, HashSet<string> result)
    {
        foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            if (IsIgnoredGeneratedPath(file)) continue;
            result.Add(Path.GetFullPath(file));
        }
    }

    static void AddSolutionFiles(string solutionPath, HashSet<string> result)
    {
        var solutionDir = Path.GetDirectoryName(solutionPath) ?? "";
        foreach (var line in File.ReadLines(solutionPath))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("Project(", StringComparison.Ordinal)) continue;

            var parts = trimmed.Split('"');
            if (parts.Length < 6) continue;

            var projectPath = parts[5];
            if (!projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)) continue;

            AddProjectFiles(Path.GetFullPath(Path.Combine(solutionDir, projectPath)), result);
        }
    }

    static void AddProjectFiles(string projectPath, HashSet<string> result)
    {
        if (!File.Exists(projectPath)) return;

        var projectDir = Path.GetDirectoryName(projectPath) ?? "";
        var doc = XDocument.Load(projectPath);
        var compileItems = doc.Descendants()
            .Where(e => e.Name.LocalName == "Compile")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();

        if (compileItems.Count == 0)
        {
            AddDirectoryFiles(projectDir, result);
            return;
        }

        foreach (var include in compileItems)
        {
            var path = Path.GetFullPath(Path.Combine(projectDir, include!));
            if (File.Exists(path) && !IsIgnoredGeneratedPath(path))
            {
                result.Add(path);
            }
        }
    }

    static bool IsIgnoredGeneratedPath(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
    }

    static IEnumerable<MetadataReference> DefaultReferences()
    {
        var assemblyPaths = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator)
            ?? Array.Empty<string>();

        foreach (var path in assemblyPaths)
        {
            if (File.Exists(path)) yield return MetadataReference.CreateFromFile(path);
        }

        var ownAssembly = typeof(GenTaskFlags).Assembly.Location;
        if (File.Exists(ownAssembly))
        {
            yield return MetadataReference.CreateFromFile(ownAssembly);
        }
    }

    void ParseTypeDeclaration(TypeDeclarationSyntax declaration, SemanticModel model)
    {
        var symbol = model.GetDeclaredSymbol(declaration);
        var zrType = GetOrCreateDeclaredType(symbol, declaration.Identifier.Text, declaration);

        zrType.Kind = declaration switch
        {
            ClassDeclarationSyntax => ZRTypeKind.Class,
            StructDeclarationSyntax => ZRTypeKind.Struct,
            InterfaceDeclarationSyntax => ZRTypeKind.Interface,
            RecordDeclarationSyntax record when record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) => ZRTypeKind.Struct,
            RecordDeclarationSyntax => ZRTypeKind.Class,
            _ => ZRTypeKind.Unknown
        };
        zrType.IsAbstract = declaration.Modifiers.Any(SyntaxKind.AbstractKeyword);
        zrType.IsSealed = declaration.Modifiers.Any(SyntaxKind.SealedKeyword);
        zrType.HasDeclaredConstructors = declaration.Members.OfType<ConstructorDeclarationSyntax>().Any();
        zrType.HasDeclaredParameterlessConstructor = declaration.Members.OfType<ConstructorDeclarationSyntax>()
            .Any(ctor => ctor.ParameterList.Parameters.Count == 0);

        zrType.Source ??= SourceLocation(declaration);
        EnsureDefaultTargetFolder(zrType);
        zrType.Attributes = ReadAttributes(symbol, declaration.AttributeLists, model);
        ApplyTypeAttributes(zrType);
        if (symbol?.IsGenericType == true)
        {
            zrType.Options |= ZRTypeOption.GenericDefinition;
        }

        if (symbol != null)
        {
            zrType.BaseType = symbol.BaseType is { SpecialType: not SpecialType.System_Object }
                ? TypeFromSymbol(symbol.BaseType)
                : null;
            zrType.Interfaces = symbol.Interfaces.Select(t => TypeFromSymbol(t)).ToList();
            zrType.GenericParameters = symbol.TypeParameters.Select(ReadGenericParameter).ToList();
            RegisterConstructedGenericSurface(zrType.BaseType, zrType);
            foreach (var interfaceType in zrType.Interfaces)
            {
                RegisterConstructedGenericSurface(interfaceType, zrType);
            }
        }
        else
        {
            zrType.BaseType = ReadBaseTypeFromSyntax(declaration, model);
        }

        QueueExplicitGenericInstances(zrType, symbol, declaration, model);

        foreach (var field in declaration.Members.OfType<FieldDeclarationSyntax>())
        {
            if (field.Modifiers.Any(SyntaxKind.StaticKeyword)) continue;
            foreach (var variable in field.Declaration.Variables)
            {
                var member = ParseField(field, variable, model, zrType);
                zrType.Members.Add(member);
            }
        }

        foreach (var property in declaration.Members.OfType<PropertyDeclarationSyntax>())
        {
            if (property.Modifiers.Any(SyntaxKind.StaticKeyword)) continue;
            if (!HasAttribute(property.AttributeLists, "GenInclude")) continue;
            zrType.Members.Add(ParseProperty(property, model, zrType));
        }

        zrType.Methods = declaration.Members.OfType<MethodDeclarationSyntax>()
            .Where(method => !method.Modifiers.Any(SyntaxKind.StaticKeyword))
            .Select(method => ParseMethod(method, model, zrType))
            .ToList();
    }

    void QueueExplicitGenericInstances(
        ZRType type,
        INamedTypeSymbol? symbol,
        TypeDeclarationSyntax declaration,
        SemanticModel model)
    {
        foreach (var attribute in type.Attributes.Where(attribute => attribute.Name == "GenRegGenericInstance"))
        {
            if (symbol == null || !symbol.IsGenericType || symbol.TypeParameters.Length != 1)
            {
                throw AttributeError(
                    type,
                    attribute,
                    "can only be used on a generic declaration with exactly one type parameter");
            }

            pendingGenericInstanceRegistrations.Add(new PendingGenericInstanceRegistration
            {
                Type = type,
                Symbol = symbol,
                Declaration = declaration,
                SemanticModel = model,
                Attribute = attribute
            });
        }
    }

    void RegisterExplicitGenericInstances(CSharpCompilation compilation)
    {
        foreach (var registration in pendingGenericInstanceRegistrations)
        {
            var typeName = ArgAt(registration.Attribute, 0, "").Trim();
            if (string.IsNullOrEmpty(typeName))
            {
                throw AttributeError(registration.Type, registration.Attribute, "requires a non-empty type name");
            }

            var typeSyntax = SyntaxFactory.ParseTypeName(typeName);
            if (typeSyntax.ContainsDiagnostics)
            {
                throw AttributeError(registration.Type, registration.Attribute, $"contains invalid C# type syntax '{typeName}'");
            }

            var typeInfo = registration.SemanticModel.GetSpeculativeTypeInfo(
                registration.Declaration.Identifier.SpanStart,
                typeSyntax,
                SpeculativeBindingOption.BindAsTypeOrNamespace);
            var typeArgument = typeInfo.Type;
            if (typeArgument == null || typeArgument.TypeKind == TypeKind.Error)
            {
                throw AttributeError(registration.Type, registration.Attribute, $"could not resolve type '{typeName}'");
            }

            if (typeArgument.SpecialType == SpecialType.System_Void || ContainsTypeParameter(typeArgument))
            {
                throw AttributeError(registration.Type, registration.Attribute, $"type '{typeName}' must be a closed non-void type");
            }

            INamedTypeSymbol constructed;
            try
            {
                constructed = registration.Symbol.Construct(typeArgument);
            }
            catch (ArgumentException exception)
            {
                throw AttributeError(registration.Type, registration.Attribute, exception.Message);
            }

            ValidateGenericConstraints(compilation, registration, constructed, typeName);
            var registeredType = TypeFromNamedSymbol(constructed, constructed.ToDisplayString(FullNameFormat));
            RegisterConstructedGenericSurface(registeredType, registration.Type);
        }
    }

    static bool ContainsTypeParameter(ITypeSymbol type)
    {
        return type switch
        {
            ITypeParameterSymbol => true,
            IArrayTypeSymbol array => ContainsTypeParameter(array.ElementType),
            IPointerTypeSymbol pointer => ContainsTypeParameter(pointer.PointedAtType),
            INamedTypeSymbol named => named.TypeArguments.Any(ContainsTypeParameter),
            _ => false
        };
    }

    static readonly HashSet<string> GenericConstraintDiagnosticIds = new(StringComparer.Ordinal)
    {
        "CS0310",
        "CS0311",
        "CS0452",
        "CS0453",
        "CS0701",
        "CS0718",
        "CS8377"
    };

    static void ValidateGenericConstraints(
        CSharpCompilation compilation,
        PendingGenericInstanceRegistration registration,
        INamedTypeSymbol constructed,
        string typeName)
    {
        var probeSource = $"internal sealed class __ZRGenericConstraintProbe {{ private {constructed.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} value; }}";
        var probeTree = CSharpSyntaxTree.ParseText(
            probeSource,
            new CSharpParseOptions(LanguageVersion.Preview),
            "__ZRGenericConstraintProbe.cs");
        var constraintError = compilation.AddSyntaxTrees(probeTree)
            .GetDiagnostics()
            .FirstOrDefault(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error &&
                diagnostic.Location.SourceTree == probeTree &&
                GenericConstraintDiagnosticIds.Contains(diagnostic.Id));

        if (constraintError != null)
        {
            throw AttributeError(
                registration.Type,
                registration.Attribute,
                $"type '{typeName}' does not satisfy the generic constraints: {constraintError.GetMessage()}");
        }
    }

    static InvalidOperationException AttributeError(ZRType type, ZRAttributeInfo attribute, string message)
    {
        var location = type.Source == null
            ? type.FullName
            : $"{type.Source.FilePath}:{type.Source.Line}";
        return new InvalidOperationException($"{attribute.SourceText} on {type.FullName} at {location} {message}.");
    }

    void ParseEnumDeclaration(EnumDeclarationSyntax declaration, SemanticModel model)
    {
        var symbol = model.GetDeclaredSymbol(declaration);
        var zrType = GetOrCreateDeclaredType(symbol, declaration.Identifier.Text, declaration);
        zrType.Kind = ZRTypeKind.Enum;
        zrType.EnumUnderlyingType = declaration.BaseList?.Types.FirstOrDefault() is { } baseType
            ? TypeFromSyntax(baseType.Type, model)
            : ZRType.FromSystemType(typeof(int));
        zrType.Source ??= SourceLocation(declaration);
        EnsureDefaultTargetFolder(zrType);
        zrType.Attributes = ReadAttributes(symbol, declaration.AttributeLists, model);
        ApplyTypeAttributes(zrType);
    }

    void LinkChildTypes()
    {
        foreach (var type in typesByFullName.Values)
        {
            type.ChildTypes.Clear();
        }

        foreach (var type in typesByFullName.Values)
        {
            if (type.BaseType == null) continue;
            if (!typesByFullName.TryGetValue(type.BaseType.FullName, out var baseType)) continue;
            if (baseType.ChildTypes.All(child => child.FullName != type.FullName))
            {
                baseType.ChildTypes.Add(type);
            }
        }
    }

    void BuildDataMembers()
    {
        foreach (var type in typesByFullName.Values)
        {
            type.DataMembers = type.Members.Select(member =>
            {
                var data = member.ToData();
                data = data.WithOption(
                    ZRDataOption.InsideConfigStorage,
                    (type.Options & ZRTypeOption.HasConfigRootType) != 0);
                return data;
            }).ToList();

            if ((type.Options & ZRTypeOption.DoNotSortFields) == 0)
            {
                type.DataMembers = type.DataMembers.OrderBy(data => data.Access, StringComparer.Ordinal).ToList();
            }
        }
    }

    ZRMember ParseField(FieldDeclarationSyntax field, VariableDeclaratorSyntax variable, SemanticModel model, ZRType parent)
    {
        var symbol = model.GetDeclaredSymbol(variable) as IFieldSymbol;
        var declaredType = symbol?.Type is { } symbolType
            ? TypeFromSymbol(symbolType)
            : TypeFromSyntax(field.Declaration.Type, model);

        var member = new ZRMember
        {
            Name = variable.Identifier.Text,
            Kind = ZRMemberKind.Field,
            Visibility = VisibilityFromSymbol(symbol?.DeclaredAccessibility),
            ParentType = parent,
            DeclaredType = declaredType,
            MemberType = UnwrapMemberType(declaredType, out var wrappers),
            WrapperTypes = wrappers,
            IsReadOnly = field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword),
            IsResolved = symbol?.Type.TypeKind != TypeKind.Error,
            Source = SourceLocation(variable),
            Attributes = ReadAttributes(symbol, field.AttributeLists, model)
        };
        member.DeclaredType.WrittenName = field.Declaration.Type.ToString();
        RegisterConstructedGenericSurface(declaredType, parent);

        ApplyMemberAttributes(member);
        return member;
    }

    ZRMember ParseProperty(PropertyDeclarationSyntax property, SemanticModel model, ZRType parent)
    {
        var symbol = model.GetDeclaredSymbol(property) as IPropertySymbol;
        var declaredType = symbol?.Type is { } symbolType
            ? TypeFromSymbol(symbolType)
            : TypeFromSyntax(property.Type, model);

        var member = new ZRMember
        {
            Name = property.Identifier.Text,
            Kind = ZRMemberKind.Property,
            Visibility = VisibilityFromSymbol(symbol?.DeclaredAccessibility),
            ParentType = parent,
            DeclaredType = declaredType,
            MemberType = UnwrapMemberType(declaredType, out var wrappers),
            WrapperTypes = wrappers,
            IsReadOnly = property.AccessorList?.Accessors.All(a => !a.IsKind(SyntaxKind.SetAccessorDeclaration)) ?? false,
            IsResolved = symbol?.Type.TypeKind != TypeKind.Error,
            Source = SourceLocation(property),
            Attributes = ReadAttributes(symbol, property.AttributeLists, model)
        };
        member.DeclaredType.WrittenName = property.Type.ToString();
        RegisterConstructedGenericSurface(declaredType, parent);

        ApplyMemberAttributes(member);
        return member;
    }

    ZRType GetOrCreateDeclaredType(INamedTypeSymbol? symbol, string fallbackName, SyntaxNode declaration)
    {
        var fullName = symbol?.ToDisplayString(FullNameFormat) ?? fallbackName;
        if (typesByFullName.TryGetValue(fullName, out var existing))
        {
            if (declaredTypeNames.Add(fullName))
            {
                existing.Source ??= SourceLocation(declaration);
                parsedTypes.Add(existing);
            }
            return existing;
        }

        var type = symbol != null
            ? TypeFromNamedSymbol(symbol)
            : new ZRType
            {
                Name = fallbackName,
                FullName = fallbackName,
                MetadataName = fallbackName,
                WrittenName = fallbackName,
                IsResolved = false,
                Source = SourceLocation(declaration)
            };

        typesByFullName[fullName] = type;
        if (declaredTypeNames.Add(fullName))
        {
            parsedTypes.Add(type);
        }
        return type;
    }

    ZRType TypeFromSyntax(TypeSyntax syntax, SemanticModel model)
    {
        var symbol = model.GetTypeInfo(syntax).Type;
        if (symbol != null) return TypeFromSymbol(symbol, syntax.ToString());

        return new ZRType
        {
            Name = syntax.ToString(),
            FullName = syntax.ToString(),
            MetadataName = syntax.ToString(),
            WrittenName = syntax.ToString(),
            Kind = ZRTypeKind.Error,
            IsResolved = false
        };
    }

    ZRType? ReadBaseTypeFromSyntax(TypeDeclarationSyntax declaration, SemanticModel model)
    {
        var firstBase = declaration.BaseList?.Types.FirstOrDefault();
        return firstBase == null ? null : TypeFromSyntax(firstBase.Type, model);
    }

    ZRGenericParameter ReadGenericParameter(ITypeParameterSymbol symbol)
    {
        return new ZRGenericParameter
        {
            Name = symbol.Name,
            Constraints = symbol.ConstraintTypes.Select(t => TypeFromSymbol(t)).ToList(),
            Attributes = GenericParameterAttributes(symbol)
        };
    }

    static System.Reflection.GenericParameterAttributes GenericParameterAttributes(ITypeParameterSymbol symbol)
    {
        var attributes = System.Reflection.GenericParameterAttributes.None;
        if (symbol.HasConstructorConstraint)
        {
            attributes |= System.Reflection.GenericParameterAttributes.DefaultConstructorConstraint;
        }

        if (symbol.HasReferenceTypeConstraint)
        {
            attributes |= System.Reflection.GenericParameterAttributes.ReferenceTypeConstraint;
        }

        if (symbol.HasValueTypeConstraint)
        {
            attributes |= System.Reflection.GenericParameterAttributes.NotNullableValueTypeConstraint;
        }

        return attributes;
    }

    ZRMethod ParseMethod(MethodDeclarationSyntax method, SemanticModel model, ZRType owner)
    {
        var symbol = model.GetDeclaredSymbol(method);
        if (symbol != null)
        {
            RegisterConstructedGenericSurface(TypeFromSymbol(symbol.ReturnType), owner);
        }

        return new ZRMethod
        {
            Name = method.Identifier.Text,
            IsAbstract = method.Modifiers.Any(SyntaxKind.AbstractKeyword) || symbol?.IsAbstract == true,
            IsVirtual = method.Modifiers.Any(SyntaxKind.VirtualKeyword) ||
                        method.Modifiers.Any(SyntaxKind.OverrideKeyword) ||
                        symbol?.IsVirtual == true ||
                        symbol?.IsOverride == true,
            Parameters = method.ParameterList.Parameters.Select(parameter =>
            {
                var parameterSymbol = model.GetDeclaredSymbol(parameter) as IParameterSymbol;
                var parameterType = parameterSymbol?.Type != null
                    ? TypeFromSymbol(parameterSymbol.Type)
                    : parameter.Type != null
                        ? TypeFromSyntax(parameter.Type, model)
                        : ZRType.FromSystemType(typeof(object));
                RegisterConstructedGenericSurface(parameterType, owner);
                return new ZRParameter
                {
                    Name = parameter.Identifier.Text,
                    ParameterType = parameterType
                };
            }).ToList()
        };
    }

    ZRType TypeFromSymbol(ITypeSymbol symbol, string writtenName = "")
    {
        if (symbol is IArrayTypeSymbol arrayType)
        {
            var element = TypeFromSymbol(arrayType.ElementType);
            var arraySuffix = ArraySuffix(arrayType.Rank);
            return new ZRType
            {
                Name = element.Name + arraySuffix,
                FullName = element.FullName + arraySuffix,
                MetadataName = element.MetadataName + arraySuffix,
                WrittenName = string.IsNullOrWhiteSpace(writtenName) ? element.WrittenName + arraySuffix : writtenName,
                Kind = ZRTypeKind.Class,
                CommonConstruct = ZRCommonConstruct.Array,
                ElementType = element,
                CommonConstructArgType = element,
                ArrayRank = arrayType.Rank,
                IsResolved = arrayType.ElementType.TypeKind != TypeKind.Error
            };
        }

        if (symbol is INamedTypeSymbol namedType) return TypeFromNamedSymbol(namedType, writtenName);

        if (symbol is ITypeParameterSymbol typeParameter)
        {
            return new ZRType
            {
                Name = typeParameter.Name,
                FullName = typeParameter.Name,
                MetadataName = typeParameter.Name,
                WrittenName = string.IsNullOrWhiteSpace(writtenName) ? typeParameter.Name : writtenName,
                Kind = ZRTypeKind.GenericParameter
            };
        }

        var fullName = symbol.ToDisplayString(FullNameFormat);
        return new ZRType
        {
            Name = symbol.Name,
            FullName = fullName,
            MetadataName = symbol.MetadataName,
            WrittenName = string.IsNullOrWhiteSpace(writtenName) ? fullName : writtenName,
            Kind = symbol.TypeKind == TypeKind.Error ? ZRTypeKind.Error : ZRTypeKind.Unknown,
            IsResolved = symbol.TypeKind != TypeKind.Error
        };
    }

    ZRType TypeFromNamedSymbol(INamedTypeSymbol symbol, string writtenName = "")
    {
        var fullName = symbol.ToDisplayString(FullNameFormat);
        if (typesByFullName.TryGetValue(fullName, out var existing)) return existing;

        var type = new ZRType
        {
            Name = symbol.Name,
            Namespace = symbol.ContainingNamespace?.IsGlobalNamespace == false
                ? symbol.ContainingNamespace.ToDisplayString()
                : "",
            FullName = fullName,
            MetadataName = symbol.MetadataName,
            WrittenName = string.IsNullOrWhiteSpace(writtenName) ? fullName : writtenName,
            Kind = KindFromSymbol(symbol),
            CommonConstruct = CommonConstructFromSymbol(symbol),
            IsResolved = symbol.TypeKind != TypeKind.Error,
            IsAbstract = symbol.IsAbstract,
            IsSealed = symbol.IsSealed,
            HasDeclaredConstructors = symbol.InstanceConstructors.Any(ctor => !ctor.IsImplicitlyDeclared),
            HasDeclaredParameterlessConstructor = symbol.InstanceConstructors.Any(ctor =>
                !ctor.IsImplicitlyDeclared && ctor.Parameters.Length == 0)
        };

        typesByFullName[fullName] = type;

        type.GenericArguments = symbol.TypeArguments.Select(t => TypeFromSymbol(t)).ToList();
        type.CommonConstructArgType = type.GenericArguments.FirstOrDefault();
        if (symbol.NullableAnnotation == NullableAnnotation.Annotated && symbol.IsValueType)
        {
            type.CommonConstruct = ZRCommonConstruct.Nullable;
        }

        if (ShouldRegisterConstructedGeneric(symbol, type))
        {
            RegisterConstructedGeneric(type, symbol);
        }

        return type;
    }

    void RegisterConstructedGeneric(ZRType? type)
    {
        if (type == null) return;
        if (!IsConstructedGenericTarget(type)) return;
        if (typesByFullName.TryGetValue(type.FullName, out var existing))
        {
            type = existing;
        }

        if ((type.Options & ZRTypeOption.ConstructedGeneric) == 0)
        {
            type.Options |= ZRTypeOption.External | ZRTypeOption.ConstructedGeneric;
        }

        if (!declaredTypeNames.Contains(type.FullName) && parsedTypes.All(t => t.FullName != type.FullName))
        {
            parsedTypes.Add(type);
        }
    }

    void RegisterConstructedGeneric(ZRType? type, ZRType owner)
    {
        RegisterConstructedGeneric(type);
        if (type is not { Options: var options }) return;
        if ((options & ZRTypeOption.ConstructedGeneric) == 0) return;

        type.Source ??= owner.Source;
        type.TargetFolder ??= owner.TargetFolder;
        if (type.TargetFolder != null)
        {
            type.Options |= ZRTypeOption.TargetFolder;
        }
    }

    void RegisterConstructedGenericSurface(ZRType? type, ZRType owner)
    {
        if (type == null) return;

        RegisterConstructedGeneric(type, owner);
        if (type.IsArray)
        {
            RegisterConstructedGenericSurface(type.GetElementType(), owner);
        }

        foreach (var genericArgument in type.GetGenericArguments())
        {
            RegisterConstructedGenericSurface(genericArgument, owner);
        }
    }

    void RegisterConstructedGeneric(ZRType type, INamedTypeSymbol symbol)
    {
        if (!IsConstructedGenericTarget(type)) return;
        if ((type.Options & ZRTypeOption.ConstructedGeneric) != 0) return;

        type.Options |= ZRTypeOption.External | ZRTypeOption.ConstructedGeneric;
        type.GenericDefinition = TypeFromNamedSymbol(symbol.ConstructedFrom);
        type.BaseType = symbol.BaseType is { SpecialType: not SpecialType.System_Object }
            ? TypeFromSymbol(symbol.BaseType)
            : null;
        type.Interfaces = symbol.Interfaces.Select(t => TypeFromSymbol(t)).ToList();
        type.Members = symbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(field => !field.IsStatic)
            .Select(field => ParseExternalField(field, type))
            .ToList();

        if (!declaredTypeNames.Contains(type.FullName) && parsedTypes.All(t => t.FullName != type.FullName))
        {
            parsedTypes.Add(type);
        }
    }

    ZRMember ParseExternalField(IFieldSymbol field, ZRType parent)
    {
        var declaredType = TypeFromSymbol(field.Type);
        return new ZRMember
        {
            Name = field.Name,
            Kind = ZRMemberKind.Field,
            Visibility = VisibilityFromSymbol(field.DeclaredAccessibility),
            ParentType = parent,
            DeclaredType = declaredType,
            MemberType = UnwrapMemberType(declaredType, out var wrappers),
            WrapperTypes = wrappers,
            IsReadOnly = field.IsReadOnly,
            IsResolved = field.Type.TypeKind != TypeKind.Error,
            Attributes = new List<ZRAttributeInfo>()
        };
    }

    static bool ShouldRegisterConstructedGeneric(INamedTypeSymbol symbol, ZRType type)
    {
        return IsConstructedGenericTarget(type) &&
               symbol.TypeArguments.Any(arg => arg.TypeKind != TypeKind.TypeParameter);
    }

    static bool IsConstructedGenericTarget(ZRType type)
    {
        return type.CommonConstruct == ZRCommonConstruct.OtherGeneric &&
               type.GenericArguments.Count > 0 &&
               type.Kind is ZRTypeKind.Class or ZRTypeKind.Struct;
    }

    static ZRTypeKind KindFromSymbol(INamedTypeSymbol symbol)
    {
        if (symbol.SpecialType == SpecialType.System_Void) return ZRTypeKind.Void;
        if (symbol.SpecialType is SpecialType.System_String or SpecialType.System_Object) return ZRTypeKind.Class;
        if (symbol.SpecialType != SpecialType.None) return ZRTypeKind.Primitive;

        return symbol.TypeKind switch
        {
            TypeKind.Class => ZRTypeKind.Class,
            TypeKind.Struct => ZRTypeKind.Struct,
            TypeKind.Interface => ZRTypeKind.Interface,
            TypeKind.Enum => ZRTypeKind.Enum,
            TypeKind.Delegate => ZRTypeKind.Delegate,
            TypeKind.Error => ZRTypeKind.Error,
            _ => ZRTypeKind.Unknown
        };
    }

    static ZRCommonConstruct CommonConstructFromSymbol(INamedTypeSymbol symbol)
    {
        if (!symbol.IsGenericType) return ZRCommonConstruct.None;

        var fullName = symbol.ConstructedFrom.ToDisplayString(FullNameFormat);
        return fullName switch
        {
            "System.Collections.Generic.List<T>" => ZRCommonConstruct.List,
            "System.Collections.Generic.Dictionary<TKey, TValue>" => ZRCommonConstruct.Dictionary,
            "System.Nullable<T>" => ZRCommonConstruct.Nullable,
            _ when symbol.Name == "Cell" || symbol.Name == "ICell" || symbol.Name == "ICellRW" => ZRCommonConstruct.Cell,
            _ when symbol.Name is "LivableSlot" or "DataSlot" => ZRCommonConstruct.LivableSlot,
            _ when symbol.Name == "Ref" => ZRCommonConstruct.Ref,
            _ => ZRCommonConstruct.OtherGeneric
        };
    }

    ZRType? UnwrapMemberType(ZRType? declaredType, out List<FieldWrapperType> wrapperTypes)
    {
        wrapperTypes = new List<FieldWrapperType>();
        if (declaredType == null) return null;

        var currentType = declaredType;
        while (TryGetWrapper(currentType, out var wrapperType, out var innerType))
        {
            wrapperTypes.Add(wrapperType);
            if (ReferenceEquals(innerType, currentType))
            {
                break;
            }
            currentType = innerType ?? currentType;
        }

        return currentType;
    }

    static bool TryGetWrapper(ZRType type, out FieldWrapperType wrapperType, out ZRType? innerType)
    {
        innerType = type.CommonConstructArgType ?? type.GenericArguments.FirstOrDefault();
        switch (type.CommonConstruct)
        {
            case ZRCommonConstruct.Cell:
                wrapperType = FieldWrapperType.Cell;
                return innerType != null;
            case ZRCommonConstruct.LivableSlot:
                wrapperType = FieldWrapperType.LivableSlot;
                return innerType != null;
            case ZRCommonConstruct.Nullable:
                wrapperType = FieldWrapperType.Nullable;
                return innerType != null;
            default:
                wrapperType = FieldWrapperType.None;
                innerType = null;
                return false;
        }
    }

    List<ZRAttributeInfo> ReadAttributes(ISymbol? symbol, SyntaxList<AttributeListSyntax> syntaxAttributes, SemanticModel model)
    {
        var result = new List<ZRAttributeInfo>();
        var syntaxAttributeCount = syntaxAttributes.Sum(list => list.Attributes.Count);
        if (symbol != null)
        {
            result.AddRange(symbol.GetAttributes().Select(AttributeFromData));
        }

        if (result.Count < syntaxAttributeCount)
        {
            foreach (var attribute in syntaxAttributes.SelectMany(list => list.Attributes))
            {
                var sourceText = attribute.ToString();
                if (result.Any(a => a.SourceText == sourceText)) continue;
                result.Add(AttributeFromSyntax(attribute, model));
            }
        }

        return result;
    }

    ZRAttributeInfo AttributeFromData(AttributeData attribute)
    {
        var attributeType = attribute.AttributeClass;
        var info = new ZRAttributeInfo
        {
            Name = NormalizeAttributeName(attributeType?.Name ?? ""),
            FullName = attributeType?.ToDisplayString(FullNameFormat) ?? "",
            SourceText = attribute.ApplicationSyntaxReference?.GetSyntax().ToString() ?? ""
        };

        info.ConstructorArguments.AddRange(attribute.ConstructorArguments.Select(ConvertTypedConstant));
        foreach (var namedArg in attribute.NamedArguments)
        {
            info.NamedArguments[namedArg.Key] = ConvertTypedConstant(namedArg.Value);
        }

        return info;
    }

    ZRAttributeInfo AttributeFromSyntax(AttributeSyntax attribute, SemanticModel model)
    {
        var typeInfo = model.GetTypeInfo(attribute);
        var name = typeInfo.Type?.Name ?? attribute.Name.ToString().Split('.').Last();
        var info = new ZRAttributeInfo
        {
            Name = NormalizeAttributeName(name),
            FullName = typeInfo.Type?.ToDisplayString(FullNameFormat) ?? attribute.Name.ToString(),
            SourceText = attribute.ToString()
        };

        foreach (var argument in attribute.ArgumentList?.Arguments ?? default(SeparatedSyntaxList<AttributeArgumentSyntax>))
        {
            var value = model.GetConstantValue(argument.Expression);
            var converted = value.HasValue ? value.Value : argument.Expression.ToString();
            if (argument.NameEquals != null)
                info.NamedArguments[argument.NameEquals.Name.Identifier.Text] = converted;
            else
                info.ConstructorArguments.Add(converted);
        }

        return info;
    }

    object? ConvertTypedConstant(TypedConstant constant)
    {
        if (constant.Kind == TypedConstantKind.Array)
        {
            return constant.Values.Select(ConvertTypedConstant).ToArray();
        }

        if (constant.Kind == TypedConstantKind.Type && constant.Value is ITypeSymbol type)
        {
            return TypeFromSymbol(type);
        }

        return constant.Value;
    }

    void ApplyTypeAttributes(ZRType type)
    {
        foreach (var attribute in type.Attributes)
        {
            switch (attribute.Name)
            {
                case "GenTask":
                    type.Flags |= FirstArg<GenTaskFlags>(attribute);
                    break;
                case "GenTaskCustomImpl":
                    var custom = new ZRCustomImplInfo
                    {
                        Flags = FirstArg<GenTaskFlags>(attribute),
                        GenerateBaseMethods = ArgAt(attribute, 1, false),
                        Inheritable = ArgAt(attribute, 2, true)
                    };
                    type.CustomImplementations.Add(custom);
                    type.CustomImplementFlags |= custom.Flags;
                    type.Options |= ZRTypeOption.HasCustomImplementation;
                    break;
                case "GenIgnore":
                    type.IgnoreFlags |= ArgAt(attribute, 0, GenTaskFlags.All);
                    type.Options |= ZRTypeOption.HasGenIgnore;
                    break;
                case "GenDoNotSortFields":
                    type.Options |= ZRTypeOption.DoNotSortFields;
                    break;
                case "GenDoNotInheritGenTags":
                    type.Options |= ZRTypeOption.DoNotInheritGenTags;
                    break;
                case "GenMultipleRefs":
                    type.Options |= ZRTypeOption.MultipleRefs;
                    break;
                case "GenModelRootSetup":
                    type.Options |= ZRTypeOption.ModelRootSetup;
                    break;
                case "GenPolymorphicNode":
                    type.Options |= ZRTypeOption.PolymorphicNode;
                    break;
                case "Immutable":
                    type.Options |= ZRTypeOption.Immutable;
                    break;
                case "HasRefId":
                    type.Options |= ZRTypeOption.HasRefId;
                    break;
                case "GenUpdatedEvent":
                    type.Options |= ZRTypeOption.UpdatedEvent;
                    break;
                case "UIDUseClassNameHash":
                    type.Options |= ZRTypeOption.UidUseClassNameHash;
                    break;
                case "DoNotGen":
                    type.Options |= ZRTypeOption.DoNotGen;
                    break;
                case "RootType":
                    type.RootType = FirstArg<ZRType>(attribute);
                    type.Options |= ZRTypeOption.HasRootType;
                    break;
                case "ConfigRootType":
                    type.ConfigRootType = FirstArg<ZRType>(attribute);
                    type.Options |= ZRTypeOption.HasConfigRootType;
                    break;
                case "GenTargetFolder":
                    ApplyExplicitTargetFolder(type, attribute);
                    break;
                case "GenDefaultFolder":
                    type.Options |= ZRTypeOption.TargetFolder;
                    EnsureDefaultTargetFolder(type);
                    break;
                case "GenInLocalFolder":
                    type.Options |= ZRTypeOption.TargetFolder;
                    type.TargetFolder = new ZRTargetFolderInfo
                    {
                        Folder = LocalTargetFolder(type, ArgAt(attribute, 1, "x_generated")),
                        Priority = ArgAt(attribute, 0, 100),
                        Inheritable = ArgAt(attribute, 2, true)
                    };
                    break;
                case "GenZergRushFolder":
                    type.Options |= ZRTypeOption.TargetFolder;
                    type.TargetFolder = new ZRTargetFolderInfo
                    {
                        Folder = LocalTargetFolder(type, ArgAt(attribute, 0, "x_generated")),
                        Priority = 10000,
                        Inheritable = false
                    };
                    break;
            }
        }
    }

    void ApplyExplicitTargetFolder(ZRType type, ZRAttributeInfo attribute)
    {
        type.Options |= ZRTypeOption.TargetFolder;
        type.TargetFolder = new ZRTargetFolderInfo
        {
            Folder = ArgAt<string?>(attribute, 0, null),
            Inheritable = ArgAt(attribute, 1, true),
            Priority = ArgAt(attribute, 2, 1)
        };
    }

    static void EnsureDefaultTargetFolder(ZRType type)
    {
        type.TargetFolder ??= new ZRTargetFolderInfo
        {
            Folder = LocalTargetFolder(type, "x_generated"),
            Inheritable = true,
            Priority = 0
        };
    }

    static string LocalTargetFolder(ZRType type, string directoryName)
    {
        var sourceFile = type.Source?.FilePath;
        var sourceDir = string.IsNullOrWhiteSpace(sourceFile)
            ? Environment.CurrentDirectory
            : Path.GetDirectoryName(sourceFile) ?? Environment.CurrentDirectory;
        return Path.GetFullPath(Path.Combine(sourceDir, directoryName));
    }

    static void ApplyMemberAttributes(ZRMember member)
    {
        foreach (var attribute in member.Attributes)
        {
            switch (attribute.Name)
            {
                case "CanBeNull":
                    member.Options |= ZRMemberOption.CanBeNull;
                    break;
                case "Immutable":
                    member.Options |= ZRMemberOption.Immutable;
                    break;
                case "JustData":
                    member.Options |= ZRMemberOption.JustData;
                    break;
                case "CantBeAncestor":
                    member.Options |= ZRMemberOption.CantBeAncestor;
                    break;
                case "UIDComponent":
                    member.Options |= ZRMemberOption.UidComponent;
                    break;
                case "DefaultVal":
                    member.Options |= ZRMemberOption.HasDefaultValue;
                    member.DefaultValue = attribute.ConstructorArguments.FirstOrDefault();
                    break;
                case "GenArrayLengthConstraint":
                    member.Options |= ZRMemberOption.HasArrayLengthConstraint;
                    member.ArrayLengthConstraint = ArgAt<int?>(attribute, 0, null);
                    break;
                case "GenUnconstrainedArrayLength":
                    member.Options |= ZRMemberOption.UnconstrainedArrayLength;
                    member.ArrayLengthConstraint = -1;
                    break;
                case "GenInclude":
                    member.Options |= ZRMemberOption.HasGenInclude;
                    member.IncludeFlags = ArgAt(attribute, 0, GenTaskFlags.All);
                    break;
                case "GenIgnore":
                    member.Options |= ZRMemberOption.HasGenIgnore;
                    member.IgnoreFlags = ArgAt(attribute, 0, GenTaskFlags.All);
                    break;
            }
        }
    }

    static T? FirstArg<T>(ZRAttributeInfo attribute)
    {
        return ArgAt<T?>(attribute, 0, default);
    }

    static T ArgAt<T>(ZRAttributeInfo attribute, int index, T defaultValue)
    {
        if (attribute.ConstructorArguments.Count <= index) return defaultValue;
        var value = attribute.ConstructorArguments[index];
        if (value is T typedValue) return typedValue;

        if (value is int intValue && typeof(T).IsEnum)
        {
            return (T)Enum.ToObject(typeof(T), intValue);
        }

        if (value is string stringValue && typeof(T).IsEnum && Enum.TryParse(typeof(T), stringValue, out var enumValue))
        {
            return (T)enumValue;
        }

        return defaultValue;
    }

    static string NormalizeAttributeName(string name)
    {
        return name.EndsWith("Attribute", StringComparison.Ordinal)
            ? name[..^"Attribute".Length]
            : name;
    }

    static bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
    {
        foreach (var attribute in attributeLists.SelectMany(list => list.Attributes))
        {
            var name = attribute.Name.ToString().Split('.').Last();
            if (NormalizeAttributeName(name) == attributeName) return true;
        }

        return false;
    }

    static ZRMemberVisibility VisibilityFromSymbol(Accessibility? accessibility)
    {
        return accessibility switch
        {
            Accessibility.Private => ZRMemberVisibility.Private,
            Accessibility.Protected => ZRMemberVisibility.Protected,
            Accessibility.Internal => ZRMemberVisibility.Internal,
            Accessibility.ProtectedOrInternal => ZRMemberVisibility.ProtectedInternal,
            Accessibility.ProtectedAndInternal => ZRMemberVisibility.PrivateProtected,
            Accessibility.Public => ZRMemberVisibility.Public,
            _ => ZRMemberVisibility.Unknown
        };
    }

    static string ArraySuffix(int rank)
    {
        return rank <= 1 ? "[]" : "[" + new string(',', rank - 1) + "]";
    }

    static ZRSourceLocation SourceLocation(SyntaxNode node)
    {
        var lineSpan = node.SyntaxTree.GetLineSpan(node.Span);
        return new ZRSourceLocation
        {
            FilePath = node.SyntaxTree.FilePath,
            Line = lineSpan.StartLinePosition.Line + 1,
            Column = lineSpan.StartLinePosition.Character + 1
        };
    }
}
