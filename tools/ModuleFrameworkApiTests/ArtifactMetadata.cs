using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

// Read actual 1.3/1.4 PE metadata without loading either game implementation or its dependencies.
internal static class Program
{
    private static int checks;
    private static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("FAIL " + label);
        checks++;
    }
    private static string Qualified(MetadataReader reader, TypeDefinition definition)
        => reader.GetString(definition.Namespace) + "." + reader.GetString(definition.Name);
    private static List<string> Surface(string path)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        Check(pe.HasMetadata, "managed implementation metadata");
        MetadataReader reader = pe.GetMetadataReader();
        Check(reader.GetString(reader.GetAssemblyDefinition().Name) == "AnimusForge", "implementation assembly identity");
        var provider = new Names(); var lines = new List<string>();
        var api = new HashSet<string>(); var internalTypes = new HashSet<string>();
        bool foundMemoryOwner = false;
        foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
        {
            TypeDefinition type = reader.GetTypeDefinition(handle);
            string ns = reader.GetString(type.Namespace), name = reader.GetString(type.Name);
            if (ns == "AnimusForge.Refactor.Modules")
            {
                Check((type.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic,
                    name + " stays assembly-internal in actual DLL");
                internalTypes.Add(name);
            }
            if (ns == "AnimusForge" && name == "MyBehavior")
            {
                foundMemoryOwner = true;
                CheckMemoryOwner(reader, type, provider, lines);
            }
            if (ns != "AnimusForge.Api.V1") continue;
            Check((type.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public, name + " is published");
            api.Add(name); lines.Add("TYPE " + Qualified(reader, type) + " " + type.Attributes);
            foreach (MethodDefinitionHandle methodHandle in type.GetMethods())
            {
                MethodDefinition method = reader.GetMethodDefinition(methodHandle);
                if ((method.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public) continue;
                string methodName = reader.GetString(method.Name);
                Check(methodName != ".ctor", "DTO construction remains internal");
                MethodSignature<string> signature = method.DecodeSignature(provider, null);
                string args = string.Join(",", signature.ParameterTypes);
                string line = name + " " + method.Attributes + " " + signature.ReturnType + " " + methodName + "(" + args + ")";
                Check(!line.Contains("TaleWorlds") && !line.Contains("AnimusForge.Refactor"), "public signature has no game/internal types");
                lines.Add(line);
                foreach (ParameterHandle parameterHandle in method.GetParameters())
                {
                    Parameter parameter = reader.GetParameter(parameterHandle);
                    ConstantHandle constantHandle = parameter.GetDefaultValue();
                    lines.Add(name + "." + methodName + ":parameter:" + parameter.SequenceNumber + ":" + parameter.Attributes
                        + ":" + reader.GetString(parameter.Name) + ":" + Constant(reader, constantHandle));
                }
            }
            foreach (PropertyDefinitionHandle propertyHandle in type.GetProperties())
            {
                PropertyDefinition property = reader.GetPropertyDefinition(propertyHandle);
                Check(property.GetAccessors().Setter.IsNil, name + " property has no setter");
                lines.Add(name + ":property:" + reader.GetString(property.Name) + ":" + property.DecodeSignature(provider, null).ReturnType);
            }
            foreach (FieldDefinitionHandle fieldHandle in type.GetFields())
            {
                FieldDefinition field = reader.GetFieldDefinition(fieldHandle);
                if ((field.Attributes & FieldAttributes.FieldAccessMask) != FieldAttributes.Public) continue;
                string fieldName = reader.GetString(field.Name);
                // Enum value__ is the CLR-defined backing field, not a mutable DTO member.
                Check(fieldName == "value__" || (field.Attributes & FieldAttributes.Literal) != 0, "no new mutable public fields");
                lines.Add(name + ":field:" + fieldName + ":" + field.Attributes + ":" + field.DecodeSignature(provider, null)
                    + ":" + Constant(reader, field.GetDefaultValue()));
            }
        }
        string[] expected = { "AfApi", "AfCapabilityIds", "AfCapabilityInfo", "AfCapabilityState", "AfFrameworkSnapshot",
            "AfFrameworkState", "AfModuleCapabilityInfo", "AfModuleCapabilityState", "AfModuleInfo" };
        Check(foundMemoryOwner, "actual DLL includes legacy memory owner");
        Check(api.SetEquals(expected), "exact initial V1 type surface");
        foreach (string name in new[] { "IPolicyModulePort", "IGatheringModulePort", "ISiegeModulePort",
            "PolicyModuleAdapter", "GatheringModuleAdapter", "SiegeModuleAdapter", "TeamModuleServices",
            "InternalModuleDirectory", "ModuleFrameworkRuntime" })
            Check(internalTypes.Contains(name), "actual DLL contains internal " + name);
        lines.Sort(StringComparer.Ordinal);
        Console.WriteLine("ARTIFACT " + path + " SHA256=" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant());
        return lines;
    }
    private static void CheckMemoryOwner(MetadataReader reader, TypeDefinition type, Names provider, List<string> lines)
    {
        Check((type.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public, "legacy MyBehavior visibility preserved");
        int found = 0;
        foreach (MethodDefinitionHandle handle in type.GetMethods())
        {
            MethodDefinition method = reader.GetMethodDefinition(handle);
            string name = reader.GetString(method.Name);
            if (name != "CommitExternalDialogueHistory" && name != "CommitDialogueHistoryWithScene") continue;
            found++;
            bool legacy = name == "CommitExternalDialogueHistory";
            MethodSignature<string> signature = method.DecodeSignature(provider, null);
            Check((method.Attributes & MethodAttributes.Static) != 0, name + " remains static");
            Check((method.Attributes & MethodAttributes.MemberAccessMask) == (legacy ? MethodAttributes.Public : MethodAttributes.Assembly), name + " exact visibility");
            Check(signature.ReturnType == "AnimusForge.Refactor.Contracts.MemoryCommitResult", name + " existing result type");
            string expected = "String,Boolean,String,String,String,String" + (legacy ? "" : ",Int32");
            Check(string.Join(",", signature.ParameterTypes) == expected, name + " exact ABI parameter types");
            var parameters = method.GetParameters().Select(reader.GetParameter).Where(p => p.SequenceNumber > 0).ToArray();
            Check(string.Join(",", parameters.Select(p => reader.GetString(p.Name))) == "memoryId,isNonHero,npcName,playerText,aiText,extraFact" + (legacy ? "" : ",sceneSessionId"), name + " parameter names/order");
            Check(parameters.All(p => (p.Attributes & ParameterAttributes.Optional) == 0 && p.GetDefaultValue().IsNil), name + " no accidental optional ABI");
            lines.Add("MEMORY " + name + " " + method.Attributes + " " + signature.ReturnType + "(" + expected + ")");
        }
        Check(found == 2, "one legacy facade and one internal scene-aware memory owner");
    }

    private static string Constant(MetadataReader reader, ConstantHandle handle)
    {
        if (handle.IsNil) return "none";
        Constant value = reader.GetConstant(handle);
        return value.TypeCode + ":" + Convert.ToHexString(reader.GetBlobBytes(value.Value));
    }
    static void Main(string[] args)
    {
        Check(args.Length > 0 && args.Length % 2 == 0, "pass paired 1.3 and 1.4 artifact paths");
        List<string> baseline = null;
        for (int i = 0; i < args.Length; i += 2)
        {
            List<string> left = Surface(args[i]), right = Surface(args[i + 1]);
            Check(left.SequenceEqual(right), "1.3 and 1.4 public metadata signatures/defaults/constants identical");
            if (baseline != null) Check(baseline.SequenceEqual(left), "Debug and Release public metadata surface identical");
            baseline = left;
        }
        Console.WriteLine($"PASS {checks} actual-DLL metadata assertions; {args.Length} implementation DLLs; public V1/legacy memory signatures match; module ports and scene-aware memory owner remain internal.");
        Console.WriteLine("NOT TESTED: CLR loading, Bootstrap ordering, game object access, real sub-MOD execution.");
    }
    private sealed class Names : ISignatureTypeProvider<string, object>
    {
        public string GetArrayType(string element, ArrayShape shape) => element + "[rank=" + shape.Rank + "]";
        public string GetByReferenceType(string element) => element + "&";
        public string GetFunctionPointerType(MethodSignature<string> signature) => "fn(" + string.Join(",", signature.ParameterTypes) + ")->" + signature.ReturnType;
        public string GetGenericInstantiation(string generic, ImmutableArray<string> arguments) => generic + "<" + string.Join(",", arguments) + ">";
        public string GetGenericMethodParameter(object context, int index) => "!!" + index;
        public string GetGenericTypeParameter(object context, int index) => "!" + index;
        public string GetModifiedType(string modifier, string unmodified, bool required) => unmodified + (required ? " modreq(" : " modopt(") + modifier + ")";
        public string GetPinnedType(string element) => element + " pinned";
        public string GetPointerType(string element) => element + "*";
        public string GetPrimitiveType(PrimitiveTypeCode code) => code.ToString();
        public string GetSZArrayType(string element) => element + "[]";
        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte kind) => Qualified(reader, reader.GetTypeDefinition(handle));
        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte kind)
        {
            TypeReference type = reader.GetTypeReference(handle);
            return reader.GetString(type.Namespace) + "." + reader.GetString(type.Name);
        }
        public string GetTypeFromSpecification(MetadataReader reader, object context, TypeSpecificationHandle handle, byte kind)
            => reader.GetTypeSpecification(handle).DecodeSignature(this, context);
    }
}
