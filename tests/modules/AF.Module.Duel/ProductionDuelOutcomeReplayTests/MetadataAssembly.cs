using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

internal sealed class MetadataAssembly : IDisposable
{
    private static readonly IReadOnlyDictionary<ushort, OpCode> OpCodesByValue =
        typeof(OpCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OpCode))
            .Select(field => (OpCode)field.GetValue(null))
            .ToDictionary(opCode => unchecked((ushort)opCode.Value));

    private readonly FileStream _stream;
    private readonly PEReader _peReader;
    private readonly MetadataReader _reader;
    private readonly SignatureTypeNameProvider _typeProvider;
    private readonly Dictionary<string, TypeDefinitionHandle> _typesByName =
        new(StringComparer.Ordinal);
    private readonly Dictionary<MethodDefinitionHandle, TypeDefinitionHandle> _methodOwners = new();
    private readonly Dictionary<FieldDefinitionHandle, TypeDefinitionHandle> _fieldOwners = new();

    internal MetadataAssembly(string path)
    {
        Path = System.IO.Path.GetFullPath(path);
        _stream = File.OpenRead(Path);
        _peReader = new PEReader(_stream, PEStreamOptions.LeaveOpen);
        if (!_peReader.HasMetadata)
        {
            throw new InvalidOperationException("Production artifact has no CLR metadata: " + Path);
        }

        _reader = _peReader.GetMetadataReader();
        _typeProvider = new SignatureTypeNameProvider(this);
        foreach (TypeDefinitionHandle handle in _reader.TypeDefinitions)
        {
            string name = GetTypeName(handle);
            if (!_typesByName.TryAdd(name, handle))
            {
                throw new InvalidOperationException("Duplicate metadata type: " + name);
            }

            TypeDefinition definition = _reader.GetTypeDefinition(handle);
            foreach (MethodDefinitionHandle method in definition.GetMethods())
            {
                _methodOwners.Add(method, handle);
            }
            foreach (FieldDefinitionHandle field in definition.GetFields())
            {
                _fieldOwners.Add(field, handle);
            }
        }
    }

    internal string Path { get; }

    internal string AssemblyName => _reader.GetString(_reader.GetAssemblyDefinition().Name);

    internal Guid ModuleVersionId => _reader.GetGuid(_reader.GetModuleDefinition().Mvid);

    internal TypeView RequireType(string fullName)
    {
        if (!_typesByName.TryGetValue(fullName, out TypeDefinitionHandle handle))
        {
            throw new InvalidOperationException("Required production type is missing: " + fullName);
        }
        return new TypeView(this, handle);
    }

    internal IReadOnlyList<MethodView> FindMethods(string declaringType, string methodName)
    {
        TypeView type = RequireType(declaringType);
        return type.Methods.Where(method => method.Name == methodName).ToArray();
    }

    internal MethodView RequireMethod(string declaringType, string methodName, params string[] parameterTypes)
    {
        MethodView[] matches = FindMethods(declaringType, methodName)
            .Where(method => method.ParameterTypes.SequenceEqual(parameterTypes, StringComparer.Ordinal))
            .ToArray();
        if (matches.Length != 1)
        {
            string available = string.Join(", ", FindMethods(declaringType, methodName)
                .Select(method => method.DisplaySignature));
            throw new InvalidOperationException(
                "Expected exactly one production method " + declaringType + "::" + methodName
                + "(" + string.Join(",", parameterTypes) + "); found " + matches.Length
                + ". Available: " + available);
        }
        return matches[0];
    }

    internal MethodView RequireUniqueMethod(string declaringType, string methodName)
    {
        MethodView[] matches = FindMethods(declaringType, methodName).ToArray();
        if (matches.Length != 1)
        {
            throw new InvalidOperationException(
                "Expected exactly one production method " + declaringType + "::" + methodName
                + "; found " + matches.Length + ".");
        }
        return matches[0];
    }

    internal IReadOnlyList<MethodCallView> GetDirectCalls(MethodView method)
    {
        List<MethodCallView> calls = new();
        foreach (IlInstruction instruction in ReadInstructions(method.Handle))
        {
            if (instruction.Operand is not EntityHandle handle
                || instruction.OpCode.OperandType != OperandType.InlineMethod)
            {
                continue;
            }

            MethodCallView call = ResolveMethod(handle);
            if (call != null)
            {
                calls.Add(call);
            }
        }
        return calls;
    }

    internal bool CallsTransitively(
        MethodView source,
        Func<MethodCallView, bool> target,
        out IReadOnlyList<string> path)
    {
        Queue<(MethodDefinitionHandle Handle, List<string> Path)> queue = new();
        HashSet<MethodDefinitionHandle> visited = new();
        queue.Enqueue((source.Handle, new List<string> { source.DisplaySignature }));

        while (queue.Count > 0)
        {
            (MethodDefinitionHandle handle, List<string> currentPath) = queue.Dequeue();
            if (!visited.Add(handle))
            {
                continue;
            }

            MethodView current = GetMethod(handle);
            foreach (MethodCallView call in GetDirectCalls(current))
            {
                List<string> nextPath = new(currentPath) { call.DisplaySignature };
                if (target(call))
                {
                    path = nextPath;
                    return true;
                }
                if (call.DefinitionHandle.HasValue && !visited.Contains(call.DefinitionHandle.Value))
                {
                    queue.Enqueue((call.DefinitionHandle.Value, nextPath));
                }
            }
        }

        path = Array.Empty<string>();
        return false;
    }

    internal IReadOnlyList<FieldReferenceView> GetReferencedFields(MethodView method)
    {
        List<FieldReferenceView> fields = new();
        foreach (IlInstruction instruction in ReadInstructions(method.Handle))
        {
            if (instruction.Operand is EntityHandle handle
                && instruction.OpCode.OperandType == OperandType.InlineField)
            {
                FieldReferenceView field = ResolveField(handle);
                if (field != null)
                {
                    fields.Add(field);
                }
            }
        }
        return fields;
    }

    internal IReadOnlyList<string> GetLoadedStrings(MethodView method)
    {
        return ReadInstructions(method.Handle)
            .Where(instruction => instruction.OpCode == OpCodes.Ldstr && instruction.Operand is string)
            .Select(instruction => (string)instruction.Operand)
            .ToArray();
    }

    internal IReadOnlyList<int> GetLoadedInt32Constants(MethodView method)
    {
        List<int> values = new();
        foreach (IlInstruction instruction in ReadInstructions(method.Handle))
        {
            if (instruction.OpCode == OpCodes.Ldc_I4 && instruction.Operand is int value)
            {
                values.Add(value);
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_S && instruction.Operand is sbyte shortValue)
            {
                values.Add(shortValue);
            }
            else if (TryGetImplicitInt32(instruction.OpCode, out int implicitValue))
            {
                values.Add(implicitValue);
            }
        }
        return values;
    }

    internal IReadOnlyDictionary<string, int> GetEnumValues(string typeName)
    {
        TypeView type = RequireType(typeName);
        Dictionary<string, int> values = new(StringComparer.Ordinal);
        foreach (FieldView field in type.Fields.Where(field => field.IsLiteral && field.Name != "value__"))
        {
            ConstantHandle constantHandle = _reader.GetFieldDefinition(field.Handle).GetDefaultValue();
            if (constantHandle.IsNil)
            {
                throw new InvalidOperationException("Enum field has no constant: " + typeName + "." + field.Name);
            }
            Constant constant = _reader.GetConstant(constantHandle);
            BlobReader blob = _reader.GetBlobReader(constant.Value);
            int value = constant.TypeCode switch
            {
                ConstantTypeCode.SByte => blob.ReadSByte(),
                ConstantTypeCode.Byte => blob.ReadByte(),
                ConstantTypeCode.Int16 => blob.ReadInt16(),
                ConstantTypeCode.UInt16 => blob.ReadUInt16(),
                ConstantTypeCode.Int32 => blob.ReadInt32(),
                _ => throw new InvalidOperationException(
                    "Unsupported enum constant type " + constant.TypeCode + " for " + typeName + "." + field.Name)
            };
            values.Add(field.Name, value);
        }
        return values;
    }

    internal ParameterView GetParameter(MethodView method, int sequenceNumber)
    {
        MethodDefinition definition = _reader.GetMethodDefinition(method.Handle);
        foreach (ParameterHandle handle in definition.GetParameters())
        {
            Parameter parameter = _reader.GetParameter(handle);
            if (parameter.SequenceNumber == sequenceNumber)
            {
                object defaultValue = null;
                ConstantHandle constantHandle = parameter.GetDefaultValue();
                if (!constantHandle.IsNil)
                {
                    defaultValue = ReadConstant(_reader.GetConstant(constantHandle));
                }
                return new ParameterView(
                    parameter.Attributes,
                    defaultValue,
                    !constantHandle.IsNil);
            }
        }
        throw new InvalidOperationException(
            "Parameter sequence " + sequenceNumber + " is missing from " + method.DisplaySignature);
    }

    public void Dispose()
    {
        _peReader.Dispose();
        _stream.Dispose();
    }

    private object ReadConstant(Constant constant)
    {
        BlobReader blob = _reader.GetBlobReader(constant.Value);
        return constant.TypeCode switch
        {
            ConstantTypeCode.Boolean => blob.ReadBoolean(),
            ConstantTypeCode.SByte => blob.ReadSByte(),
            ConstantTypeCode.Byte => blob.ReadByte(),
            ConstantTypeCode.Int16 => blob.ReadInt16(),
            ConstantTypeCode.UInt16 => blob.ReadUInt16(),
            ConstantTypeCode.Int32 => blob.ReadInt32(),
            ConstantTypeCode.UInt32 => blob.ReadUInt32(),
            ConstantTypeCode.Int64 => blob.ReadInt64(),
            ConstantTypeCode.UInt64 => blob.ReadUInt64(),
            ConstantTypeCode.Single => blob.ReadSingle(),
            ConstantTypeCode.Double => blob.ReadDouble(),
            ConstantTypeCode.Char => (char)blob.ReadUInt16(),
            ConstantTypeCode.String => blob.ReadUTF16(blob.Length),
            ConstantTypeCode.NullReference => null,
            _ => throw new InvalidOperationException("Unsupported metadata constant type: " + constant.TypeCode)
        };
    }

    private MethodView GetMethod(MethodDefinitionHandle handle)
    {
        MethodDefinition definition = _reader.GetMethodDefinition(handle);
        MethodSignature<string> signature = definition.DecodeSignature(_typeProvider, genericContext: null);
        string owner = GetTypeName(_methodOwners[handle]);
        return new MethodView(
            handle,
            owner,
            _reader.GetString(definition.Name),
            signature.ReturnType,
            signature.ParameterTypes.ToArray(),
            definition.Attributes);
    }

    private MethodCallView ResolveMethod(EntityHandle handle)
    {
        if (handle.Kind == HandleKind.MethodSpecification)
        {
            handle = _reader.GetMethodSpecification((MethodSpecificationHandle)handle).Method;
        }

        if (handle.Kind == HandleKind.MethodDefinition)
        {
            MethodView method = GetMethod((MethodDefinitionHandle)handle);
            return new MethodCallView(
                method.DeclaringType,
                method.Name,
                method.ReturnType,
                method.ParameterTypes,
                method.Handle);
        }

        if (handle.Kind != HandleKind.MemberReference)
        {
            return null;
        }

        MemberReference reference = _reader.GetMemberReference((MemberReferenceHandle)handle);
        if (reference.GetKind() != MemberReferenceKind.Method)
        {
            return null;
        }
        MethodSignature<string> signature = reference.DecodeMethodSignature(_typeProvider, genericContext: null);
        return new MethodCallView(
            ResolveMemberParentType(reference.Parent),
            _reader.GetString(reference.Name),
            signature.ReturnType,
            signature.ParameterTypes.ToArray(),
            null);
    }

    private FieldReferenceView ResolveField(EntityHandle handle)
    {
        if (handle.Kind == HandleKind.FieldDefinition)
        {
            FieldDefinitionHandle fieldHandle = (FieldDefinitionHandle)handle;
            FieldDefinition definition = _reader.GetFieldDefinition(fieldHandle);
            return new FieldReferenceView(
                GetTypeName(_fieldOwners[fieldHandle]),
                _reader.GetString(definition.Name),
                definition.DecodeSignature(_typeProvider, genericContext: null));
        }

        if (handle.Kind != HandleKind.MemberReference)
        {
            return null;
        }
        MemberReference reference = _reader.GetMemberReference((MemberReferenceHandle)handle);
        if (reference.GetKind() != MemberReferenceKind.Field)
        {
            return null;
        }
        return new FieldReferenceView(
            ResolveMemberParentType(reference.Parent),
            _reader.GetString(reference.Name),
            reference.DecodeFieldSignature(_typeProvider, genericContext: null));
    }

    private string ResolveMemberParentType(EntityHandle parent)
    {
        return parent.Kind switch
        {
            HandleKind.TypeDefinition => GetTypeName((TypeDefinitionHandle)parent),
            HandleKind.TypeReference => GetTypeName((TypeReferenceHandle)parent),
            HandleKind.TypeSpecification => _reader.GetTypeSpecification((TypeSpecificationHandle)parent)
                .DecodeSignature(_typeProvider, genericContext: null),
            HandleKind.MethodDefinition => GetMethod((MethodDefinitionHandle)parent).DeclaringType,
            _ => "<" + parent.Kind + ">"
        };
    }

    private IReadOnlyList<IlInstruction> ReadInstructions(MethodDefinitionHandle handle)
    {
        MethodDefinition definition = _reader.GetMethodDefinition(handle);
        if (definition.RelativeVirtualAddress == 0)
        {
            return Array.Empty<IlInstruction>();
        }

        byte[] il = _peReader.GetMethodBody(definition.RelativeVirtualAddress).GetILBytes();
        List<IlInstruction> instructions = new();
        int index = 0;
        while (index < il.Length)
        {
            int offset = index;
            ushort value = il[index++];
            if (value == 0xfe)
            {
                if (index >= il.Length)
                {
                    throw new InvalidOperationException("Truncated two-byte opcode at IL_" + offset.ToString("x4"));
                }
                value = (ushort)(0xfe00 | il[index++]);
            }
            if (!OpCodesByValue.TryGetValue(value, out OpCode opCode))
            {
                throw new InvalidOperationException("Unknown opcode 0x" + value.ToString("x4") + " at IL_" + offset.ToString("x4"));
            }

            object operand = null;
            switch (opCode.OperandType)
            {
                case OperandType.InlineNone:
                    break;
                case OperandType.ShortInlineBrTarget:
                    operand = unchecked((sbyte)il[index]);
                    index += 1;
                    break;
                case OperandType.ShortInlineI:
                    operand = unchecked((sbyte)il[index]);
                    index += 1;
                    break;
                case OperandType.ShortInlineVar:
                    operand = il[index++];
                    break;
                case OperandType.InlineVar:
                    operand = BitConverter.ToUInt16(il, index);
                    index += 2;
                    break;
                case OperandType.InlineBrTarget:
                case OperandType.InlineI:
                    operand = BitConverter.ToInt32(il, index);
                    index += 4;
                    break;
                case OperandType.InlineI8:
                    operand = BitConverter.ToInt64(il, index);
                    index += 8;
                    break;
                case OperandType.ShortInlineR:
                    operand = BitConverter.ToSingle(il, index);
                    index += 4;
                    break;
                case OperandType.InlineR:
                    operand = BitConverter.ToDouble(il, index);
                    index += 8;
                    break;
                case OperandType.InlineString:
                {
                    int token = BitConverter.ToInt32(il, index);
                    index += 4;
                    operand = _reader.GetUserString(MetadataTokens.UserStringHandle(token & 0x00ffffff));
                    break;
                }
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                {
                    int token = BitConverter.ToInt32(il, index);
                    index += 4;
                    operand = MetadataTokens.EntityHandle(token);
                    break;
                }
                case OperandType.InlineSig:
                    operand = BitConverter.ToInt32(il, index);
                    index += 4;
                    break;
                case OperandType.InlineSwitch:
                {
                    int count = BitConverter.ToInt32(il, index);
                    index += 4;
                    if (count < 0 || count > (il.Length - index) / 4)
                    {
                        throw new InvalidOperationException("Invalid switch operand at IL_" + offset.ToString("x4"));
                    }
                    int[] targets = new int[count];
                    for (int i = 0; i < count; i++)
                    {
                        targets[i] = BitConverter.ToInt32(il, index);
                        index += 4;
                    }
                    operand = targets;
                    break;
                }
                default:
                    throw new InvalidOperationException(
                        "Unsupported operand type " + opCode.OperandType + " at IL_" + offset.ToString("x4"));
            }

            if (index > il.Length)
            {
                throw new InvalidOperationException("Truncated operand at IL_" + offset.ToString("x4"));
            }
            instructions.Add(new IlInstruction(opCode, operand));
        }
        return instructions;
    }

    private static bool TryGetImplicitInt32(OpCode opCode, out int value)
    {
        if (opCode == OpCodes.Ldc_I4_M1)
        {
            value = -1;
            return true;
        }
        OpCode[] values =
        {
            OpCodes.Ldc_I4_0,
            OpCodes.Ldc_I4_1,
            OpCodes.Ldc_I4_2,
            OpCodes.Ldc_I4_3,
            OpCodes.Ldc_I4_4,
            OpCodes.Ldc_I4_5,
            OpCodes.Ldc_I4_6,
            OpCodes.Ldc_I4_7,
            OpCodes.Ldc_I4_8
        };
        for (int index = 0; index < values.Length; index++)
        {
            if (opCode == values[index])
            {
                value = index;
                return true;
            }
        }
        value = 0;
        return false;
    }

    private string GetTypeName(TypeDefinitionHandle handle)
    {
        TypeDefinition definition = _reader.GetTypeDefinition(handle);
        string name = _reader.GetString(definition.Name);
        TypeDefinitionHandle declaringType = definition.GetDeclaringType();
        if (!declaringType.IsNil)
        {
            return GetTypeName(declaringType) + "+" + name;
        }
        string namespaceName = _reader.GetString(definition.Namespace);
        return string.IsNullOrEmpty(namespaceName) ? name : namespaceName + "." + name;
    }

    private string GetTypeName(TypeReferenceHandle handle)
    {
        TypeReference reference = _reader.GetTypeReference(handle);
        string name = _reader.GetString(reference.Name);
        if (reference.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            return GetTypeName((TypeReferenceHandle)reference.ResolutionScope) + "+" + name;
        }
        string namespaceName = _reader.GetString(reference.Namespace);
        return string.IsNullOrEmpty(namespaceName) ? name : namespaceName + "." + name;
    }

    private string GetTypeName(EntityHandle handle)
    {
        return handle.Kind switch
        {
            HandleKind.TypeDefinition => GetTypeName((TypeDefinitionHandle)handle),
            HandleKind.TypeReference => GetTypeName((TypeReferenceHandle)handle),
            HandleKind.TypeSpecification => _reader.GetTypeSpecification((TypeSpecificationHandle)handle)
                .DecodeSignature(_typeProvider, genericContext: null),
            _ => "<" + handle.Kind + ">"
        };
    }

    internal sealed class TypeView
    {
        private readonly MetadataAssembly _assembly;

        internal TypeView(MetadataAssembly assembly, TypeDefinitionHandle handle)
        {
            _assembly = assembly;
            Handle = handle;
        }

        internal TypeDefinitionHandle Handle { get; }

        internal string FullName => _assembly.GetTypeName(Handle);

        internal TypeAttributes Attributes => _assembly._reader.GetTypeDefinition(Handle).Attributes;

        internal string BaseType
        {
            get
            {
                EntityHandle handle = _assembly._reader.GetTypeDefinition(Handle).BaseType;
                return handle.IsNil ? string.Empty : _assembly.GetTypeName(handle);
            }
        }

        internal IReadOnlyList<MethodView> Methods => _assembly._reader.GetTypeDefinition(Handle)
            .GetMethods()
            .Select(_assembly.GetMethod)
            .ToArray();

        internal IReadOnlyList<FieldView> Fields => _assembly._reader.GetTypeDefinition(Handle)
            .GetFields()
            .Select(handle =>
            {
                FieldDefinition definition = _assembly._reader.GetFieldDefinition(handle);
                return new FieldView(
                    handle,
                    _assembly._reader.GetString(definition.Name),
                    definition.DecodeSignature(_assembly._typeProvider, genericContext: null),
                    definition.Attributes);
            })
            .ToArray();
    }

    internal sealed record MethodView(
        MethodDefinitionHandle Handle,
        string DeclaringType,
        string Name,
        string ReturnType,
        IReadOnlyList<string> ParameterTypes,
        MethodAttributes Attributes)
    {
        internal string DisplaySignature =>
            ReturnType + " " + DeclaringType + "::" + Name + "(" + string.Join(",", ParameterTypes) + ")";

        internal bool IsPublic => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;

        internal bool IsStatic => (Attributes & MethodAttributes.Static) != 0;
    }

    internal sealed record FieldView(
        FieldDefinitionHandle Handle,
        string Name,
        string FieldType,
        FieldAttributes Attributes)
    {
        internal bool IsLiteral => (Attributes & FieldAttributes.Literal) != 0;
        internal bool IsPrivate => (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private;
        internal bool IsStatic => (Attributes & FieldAttributes.Static) != 0;
        internal bool IsInitOnly => (Attributes & FieldAttributes.InitOnly) != 0;
    }

    internal sealed record MethodCallView(
        string DeclaringType,
        string Name,
        string ReturnType,
        IReadOnlyList<string> ParameterTypes,
        MethodDefinitionHandle? DefinitionHandle)
    {
        internal string DisplaySignature =>
            ReturnType + " " + DeclaringType + "::" + Name + "(" + string.Join(",", ParameterTypes) + ")";
    }

    internal sealed record FieldReferenceView(string DeclaringType, string Name, string FieldType);

    internal sealed record ParameterView(
        ParameterAttributes Attributes,
        object DefaultValue,
        bool HasDefaultValue)
    {
        internal bool IsOut => (Attributes & ParameterAttributes.Out) != 0;
    }

    private sealed record IlInstruction(OpCode OpCode, object Operand);

    private sealed class SignatureTypeNameProvider : ISignatureTypeProvider<string, object>
    {
        private readonly MetadataAssembly _assembly;

        internal SignatureTypeNameProvider(MetadataAssembly assembly)
        {
            _assembly = assembly;
        }

        public string GetArrayType(string elementType, ArrayShape shape)
        {
            return elementType + "[" + new string(',', Math.Max(0, shape.Rank - 1)) + "]";
        }

        public string GetByReferenceType(string elementType) => elementType + "&";

        public string GetFunctionPointerType(MethodSignature<string> signature) =>
            "methodptr(" + string.Join(",", signature.ParameterTypes) + ")->" + signature.ReturnType;

        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) =>
            genericType + "<" + string.Join(",", typeArguments) + ">";

        public string GetGenericMethodParameter(object genericContext, int index) => "!!" + index;

        public string GetGenericTypeParameter(object genericContext, int index) => "!" + index;

        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;

        public string GetPinnedType(string elementType) => elementType;

        public string GetPointerType(string elementType) => elementType + "*";

        public string GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            return typeCode switch
            {
                PrimitiveTypeCode.Void => "System.Void",
                PrimitiveTypeCode.Boolean => "System.Boolean",
                PrimitiveTypeCode.Char => "System.Char",
                PrimitiveTypeCode.SByte => "System.SByte",
                PrimitiveTypeCode.Byte => "System.Byte",
                PrimitiveTypeCode.Int16 => "System.Int16",
                PrimitiveTypeCode.UInt16 => "System.UInt16",
                PrimitiveTypeCode.Int32 => "System.Int32",
                PrimitiveTypeCode.UInt32 => "System.UInt32",
                PrimitiveTypeCode.Int64 => "System.Int64",
                PrimitiveTypeCode.UInt64 => "System.UInt64",
                PrimitiveTypeCode.Single => "System.Single",
                PrimitiveTypeCode.Double => "System.Double",
                PrimitiveTypeCode.String => "System.String",
                PrimitiveTypeCode.IntPtr => "System.IntPtr",
                PrimitiveTypeCode.UIntPtr => "System.UIntPtr",
                PrimitiveTypeCode.Object => "System.Object",
                PrimitiveTypeCode.TypedReference => "System.TypedReference",
                _ => "<primitive:" + typeCode + ">"
            };
        }

        public string GetSZArrayType(string elementType) => elementType + "[]";

        public string GetTypeFromDefinition(
            MetadataReader reader,
            TypeDefinitionHandle handle,
            byte rawTypeKind) => _assembly.GetTypeName(handle);

        public string GetTypeFromReference(
            MetadataReader reader,
            TypeReferenceHandle handle,
            byte rawTypeKind) => _assembly.GetTypeName(handle);

        public string GetTypeFromSpecification(
            MetadataReader reader,
            object genericContext,
            TypeSpecificationHandle handle,
            byte rawTypeKind) => reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
    }
}
