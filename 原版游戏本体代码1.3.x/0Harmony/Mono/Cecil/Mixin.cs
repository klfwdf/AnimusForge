using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;
using Mono.Collections.Generic;
using Mono.Security.Cryptography;

namespace Mono.Cecil
{
	// Token: 0x020001E1 RID: 481
	internal static class Mixin
	{
		// Token: 0x060008E2 RID: 2274 RVA: 0x0001C62E File Offset: 0x0001A82E
		public static bool IsNullOrEmpty<T>(this T[] self)
		{
			return self == null || self.Length == 0;
		}

		// Token: 0x060008E3 RID: 2275 RVA: 0x0001C63A File Offset: 0x0001A83A
		public static bool IsNullOrEmpty<T>(this Collection<T> self)
		{
			return self == null || self.size == 0;
		}

		// Token: 0x060008E4 RID: 2276 RVA: 0x0001C64A File Offset: 0x0001A84A
		public static T[] Resize<T>(this T[] self, int length)
		{
			Array.Resize<T>(ref self, length);
			return self;
		}

		// Token: 0x060008E5 RID: 2277 RVA: 0x0001C655 File Offset: 0x0001A855
		public static T[] Add<T>(this T[] self, T item)
		{
			if (self == null)
			{
				self = new T[] { item };
				return self;
			}
			self = self.Resize(self.Length + 1);
			self[self.Length - 1] = item;
			return self;
		}

		// Token: 0x060008E6 RID: 2278 RVA: 0x0001C688 File Offset: 0x0001A888
		public static Version CheckVersion(Version version)
		{
			if (version == null)
			{
				return Mixin.ZeroVersion;
			}
			if (version.Build == -1)
			{
				return new Version(version.Major, version.Minor, 0, 0);
			}
			if (version.Revision == -1)
			{
				return new Version(version.Major, version.Minor, version.Build, 0);
			}
			return version;
		}

		// Token: 0x060008E7 RID: 2279 RVA: 0x0001C6E4 File Offset: 0x0001A8E4
		public static bool TryGetUniqueDocument(this MethodDebugInformation info, out Document document)
		{
			document = info.SequencePoints[0].Document;
			for (int i = 1; i < info.SequencePoints.Count; i++)
			{
				if (info.SequencePoints[i].Document != document)
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x060008E8 RID: 2280 RVA: 0x0001C734 File Offset: 0x0001A934
		public static void ResolveConstant(this IConstantProvider self, ref object constant, ModuleDefinition module)
		{
			if (module == null)
			{
				constant = Mixin.NoValue;
				return;
			}
			object syncRoot = module.SyncRoot;
			lock (syncRoot)
			{
				if (constant == Mixin.NotResolved)
				{
					if (module.HasImage())
					{
						constant = module.Read<IConstantProvider, object>(self, (IConstantProvider provider, MetadataReader reader) => reader.ReadConstant(provider));
					}
					else
					{
						constant = Mixin.NoValue;
					}
				}
			}
		}

		// Token: 0x060008E9 RID: 2281 RVA: 0x0001C7C0 File Offset: 0x0001A9C0
		public static bool GetHasCustomAttributes(this ICustomAttributeProvider self, ModuleDefinition module)
		{
			if (module.HasImage())
			{
				return module.Read<ICustomAttributeProvider, bool>(self, (ICustomAttributeProvider provider, MetadataReader reader) => reader.HasCustomAttributes(provider));
			}
			return false;
		}

		// Token: 0x060008EA RID: 2282 RVA: 0x0001C7F4 File Offset: 0x0001A9F4
		public static Collection<CustomAttribute> GetCustomAttributes(this ICustomAttributeProvider self, ref Collection<CustomAttribute> variable, ModuleDefinition module)
		{
			if (module.HasImage())
			{
				return module.Read<ICustomAttributeProvider, Collection<CustomAttribute>>(ref variable, self, (ICustomAttributeProvider provider, MetadataReader reader) => reader.ReadCustomAttributes(provider));
			}
			Interlocked.CompareExchange<Collection<CustomAttribute>>(ref variable, new Collection<CustomAttribute>(), null);
			return variable;
		}

		// Token: 0x060008EB RID: 2283 RVA: 0x0001C840 File Offset: 0x0001AA40
		public static bool ContainsGenericParameter(this IGenericInstance self)
		{
			Collection<TypeReference> arguments = self.GenericArguments;
			for (int i = 0; i < arguments.Count; i++)
			{
				if (arguments[i].ContainsGenericParameter)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060008EC RID: 2284 RVA: 0x0001C878 File Offset: 0x0001AA78
		public static void GenericInstanceFullName(this IGenericInstance self, StringBuilder builder)
		{
			builder.Append("<");
			Collection<TypeReference> arguments = self.GenericArguments;
			for (int i = 0; i < arguments.Count; i++)
			{
				if (i > 0)
				{
					builder.Append(",");
				}
				builder.Append(arguments[i].FullName);
			}
			builder.Append(">");
		}

		// Token: 0x060008ED RID: 2285 RVA: 0x0001C8D8 File Offset: 0x0001AAD8
		public static bool GetHasGenericParameters(this IGenericParameterProvider self, ModuleDefinition module)
		{
			if (module.HasImage())
			{
				return module.Read<IGenericParameterProvider, bool>(self, (IGenericParameterProvider provider, MetadataReader reader) => reader.HasGenericParameters(provider));
			}
			return false;
		}

		// Token: 0x060008EE RID: 2286 RVA: 0x0001C90C File Offset: 0x0001AB0C
		public static Collection<GenericParameter> GetGenericParameters(this IGenericParameterProvider self, ref Collection<GenericParameter> collection, ModuleDefinition module)
		{
			if (module.HasImage())
			{
				return module.Read<IGenericParameterProvider, Collection<GenericParameter>>(ref collection, self, (IGenericParameterProvider provider, MetadataReader reader) => reader.ReadGenericParameters(provider));
			}
			Interlocked.CompareExchange<Collection<GenericParameter>>(ref collection, new GenericParameterCollection(self), null);
			return collection;
		}

		// Token: 0x060008EF RID: 2287 RVA: 0x0001C959 File Offset: 0x0001AB59
		public static bool GetHasMarshalInfo(this IMarshalInfoProvider self, ModuleDefinition module)
		{
			if (module.HasImage())
			{
				return module.Read<IMarshalInfoProvider, bool>(self, (IMarshalInfoProvider provider, MetadataReader reader) => reader.HasMarshalInfo(provider));
			}
			return false;
		}

		// Token: 0x060008F0 RID: 2288 RVA: 0x0001C98B File Offset: 0x0001AB8B
		public static MarshalInfo GetMarshalInfo(this IMarshalInfoProvider self, ref MarshalInfo variable, ModuleDefinition module)
		{
			if (!module.HasImage())
			{
				return null;
			}
			return module.Read<IMarshalInfoProvider, MarshalInfo>(ref variable, self, (IMarshalInfoProvider provider, MetadataReader reader) => reader.ReadMarshalInfo(provider));
		}

		// Token: 0x060008F1 RID: 2289 RVA: 0x0001C9BE File Offset: 0x0001ABBE
		public static bool GetAttributes(this uint self, uint attributes)
		{
			return (self & attributes) > 0U;
		}

		// Token: 0x060008F2 RID: 2290 RVA: 0x0001C9C6 File Offset: 0x0001ABC6
		public static uint SetAttributes(this uint self, uint attributes, bool value)
		{
			if (value)
			{
				return self | attributes;
			}
			return self & ~attributes;
		}

		// Token: 0x060008F3 RID: 2291 RVA: 0x0001C9D3 File Offset: 0x0001ABD3
		public static bool GetMaskedAttributes(this uint self, uint mask, uint attributes)
		{
			return (self & mask) == attributes;
		}

		// Token: 0x060008F4 RID: 2292 RVA: 0x0001C9DB File Offset: 0x0001ABDB
		public static uint SetMaskedAttributes(this uint self, uint mask, uint attributes, bool value)
		{
			if (value)
			{
				self &= ~mask;
				return self | attributes;
			}
			return self & ~(mask & attributes);
		}

		// Token: 0x060008F5 RID: 2293 RVA: 0x0001C9BE File Offset: 0x0001ABBE
		public static bool GetAttributes(this ushort self, ushort attributes)
		{
			return (self & attributes) > 0;
		}

		// Token: 0x060008F6 RID: 2294 RVA: 0x0001C9F0 File Offset: 0x0001ABF0
		public static ushort SetAttributes(this ushort self, ushort attributes, bool value)
		{
			if (value)
			{
				return self | attributes;
			}
			return self & ~attributes;
		}

		// Token: 0x060008F7 RID: 2295 RVA: 0x0001C9FF File Offset: 0x0001ABFF
		public static bool GetMaskedAttributes(this ushort self, ushort mask, uint attributes)
		{
			return (long)(self & mask) == (long)((ulong)attributes);
		}

		// Token: 0x060008F8 RID: 2296 RVA: 0x0001CA09 File Offset: 0x0001AC09
		public static ushort SetMaskedAttributes(this ushort self, ushort mask, uint attributes, bool value)
		{
			if (value)
			{
				self &= ~mask;
				return (ushort)((uint)self | attributes);
			}
			return (ushort)((uint)self & ~((uint)mask & attributes));
		}

		// Token: 0x060008F9 RID: 2297 RVA: 0x0001CA21 File Offset: 0x0001AC21
		public static bool HasImplicitThis(this IMethodSignature self)
		{
			return self.HasThis && !self.ExplicitThis;
		}

		// Token: 0x060008FA RID: 2298 RVA: 0x0001CA38 File Offset: 0x0001AC38
		public static void MethodSignatureFullName(this IMethodSignature self, StringBuilder builder)
		{
			builder.Append("(");
			if (self.HasParameters)
			{
				Collection<ParameterDefinition> parameters = self.Parameters;
				for (int i = 0; i < parameters.Count; i++)
				{
					ParameterDefinition parameter = parameters[i];
					if (i > 0)
					{
						builder.Append(",");
					}
					if (parameter.ParameterType.IsSentinel)
					{
						builder.Append("...,");
					}
					builder.Append(parameter.ParameterType.FullName);
				}
			}
			builder.Append(")");
		}

		// Token: 0x060008FB RID: 2299 RVA: 0x0001CAC0 File Offset: 0x0001ACC0
		public static void CheckModule(ModuleDefinition module)
		{
			if (module == null)
			{
				throw new ArgumentNullException(Mixin.Argument.module.ToString());
			}
		}

		// Token: 0x060008FC RID: 2300 RVA: 0x0001CAE8 File Offset: 0x0001ACE8
		public static bool TryGetAssemblyNameReference(this ModuleDefinition module, AssemblyNameReference name_reference, out AssemblyNameReference assembly_reference)
		{
			Collection<AssemblyNameReference> references = module.AssemblyReferences;
			for (int i = 0; i < references.Count; i++)
			{
				AssemblyNameReference reference = references[i];
				if (Mixin.Equals(name_reference, reference))
				{
					assembly_reference = reference;
					return true;
				}
			}
			assembly_reference = null;
			return false;
		}

		// Token: 0x060008FD RID: 2301 RVA: 0x0001CB28 File Offset: 0x0001AD28
		private static bool Equals(byte[] a, byte[] b)
		{
			if (a == b)
			{
				return true;
			}
			if (a == null)
			{
				return false;
			}
			if (a.Length != b.Length)
			{
				return false;
			}
			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] != b[i])
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x060008FE RID: 2302 RVA: 0x0001CB63 File Offset: 0x0001AD63
		private static bool Equals<T>(T a, T b) where T : class, IEquatable<T>
		{
			return a == b || (a != null && a.Equals(b));
		}

		// Token: 0x060008FF RID: 2303 RVA: 0x0001CB8C File Offset: 0x0001AD8C
		private static bool Equals(AssemblyNameReference a, AssemblyNameReference b)
		{
			return a == b || (!(a.Name != b.Name) && Mixin.Equals<Version>(a.Version, b.Version) && !(a.Culture != b.Culture) && Mixin.Equals(a.PublicKeyToken, b.PublicKeyToken));
		}

		// Token: 0x06000900 RID: 2304 RVA: 0x0001CBF4 File Offset: 0x0001ADF4
		public static ParameterDefinition GetParameter(this Mono.Cecil.Cil.MethodBody self, int index)
		{
			MethodDefinition method = self.method;
			if (method.HasThis)
			{
				if (index == 0)
				{
					return self.ThisParameter;
				}
				index--;
			}
			Collection<ParameterDefinition> parameters = method.Parameters;
			if (index < 0 || index >= parameters.size)
			{
				return null;
			}
			return parameters[index];
		}

		// Token: 0x06000901 RID: 2305 RVA: 0x0001CC3C File Offset: 0x0001AE3C
		public static VariableDefinition GetVariable(this Mono.Cecil.Cil.MethodBody self, int index)
		{
			Collection<VariableDefinition> variables = self.Variables;
			if (index < 0 || index >= variables.size)
			{
				return null;
			}
			return variables[index];
		}

		// Token: 0x06000902 RID: 2306 RVA: 0x0001CC66 File Offset: 0x0001AE66
		public static bool GetSemantics(this MethodDefinition self, MethodSemanticsAttributes semantics)
		{
			return (self.SemanticsAttributes & semantics) > MethodSemanticsAttributes.None;
		}

		// Token: 0x06000903 RID: 2307 RVA: 0x0001CC73 File Offset: 0x0001AE73
		public static void SetSemantics(this MethodDefinition self, MethodSemanticsAttributes semantics, bool value)
		{
			if (value)
			{
				self.SemanticsAttributes |= semantics;
				return;
			}
			self.SemanticsAttributes &= ~semantics;
		}

		// Token: 0x06000904 RID: 2308 RVA: 0x0001CC97 File Offset: 0x0001AE97
		public static bool IsVarArg(this IMethodSignature self)
		{
			return self.CallingConvention == MethodCallingConvention.VarArg;
		}

		// Token: 0x06000905 RID: 2309 RVA: 0x0001CCA4 File Offset: 0x0001AEA4
		public static int GetSentinelPosition(this IMethodSignature self)
		{
			if (!self.HasParameters)
			{
				return -1;
			}
			Collection<ParameterDefinition> parameters = self.Parameters;
			for (int i = 0; i < parameters.Count; i++)
			{
				if (parameters[i].ParameterType.IsSentinel)
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x06000906 RID: 2310 RVA: 0x0001CCEC File Offset: 0x0001AEEC
		public static void CheckName(object name)
		{
			if (name == null)
			{
				throw new ArgumentNullException(Mixin.Argument.name.ToString());
			}
		}

		// Token: 0x06000907 RID: 2311 RVA: 0x0001CD14 File Offset: 0x0001AF14
		public static void CheckName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentNullOrEmptyException(Mixin.Argument.name.ToString());
			}
		}

		// Token: 0x06000908 RID: 2312 RVA: 0x0001CD40 File Offset: 0x0001AF40
		public static void CheckFileName(string fileName)
		{
			if (string.IsNullOrEmpty(fileName))
			{
				throw new ArgumentNullOrEmptyException(Mixin.Argument.fileName.ToString());
			}
		}

		// Token: 0x06000909 RID: 2313 RVA: 0x0001CD6C File Offset: 0x0001AF6C
		public static void CheckFullName(string fullName)
		{
			if (string.IsNullOrEmpty(fullName))
			{
				throw new ArgumentNullOrEmptyException(Mixin.Argument.fullName.ToString());
			}
		}

		// Token: 0x0600090A RID: 2314 RVA: 0x0001CD98 File Offset: 0x0001AF98
		public static void CheckStream(object stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException(Mixin.Argument.stream.ToString());
			}
		}

		// Token: 0x0600090B RID: 2315 RVA: 0x0001CDBD File Offset: 0x0001AFBD
		public static void CheckWriteSeek(Stream stream)
		{
			if (!stream.CanWrite || !stream.CanSeek)
			{
				throw new ArgumentException("Stream must be writable and seekable.");
			}
		}

		// Token: 0x0600090C RID: 2316 RVA: 0x0001CDDA File Offset: 0x0001AFDA
		public static void CheckReadSeek(Stream stream)
		{
			if (!stream.CanRead || !stream.CanSeek)
			{
				throw new ArgumentException("Stream must be readable and seekable.");
			}
		}

		// Token: 0x0600090D RID: 2317 RVA: 0x0001CDF8 File Offset: 0x0001AFF8
		public static void CheckType(object type)
		{
			if (type == null)
			{
				throw new ArgumentNullException(Mixin.Argument.type.ToString());
			}
		}

		// Token: 0x0600090E RID: 2318 RVA: 0x0001CE1D File Offset: 0x0001B01D
		public static void CheckType(object type, Mixin.Argument argument)
		{
			if (type == null)
			{
				throw new ArgumentNullException(argument.ToString());
			}
		}

		// Token: 0x0600090F RID: 2319 RVA: 0x0001CE38 File Offset: 0x0001B038
		public static void CheckField(object field)
		{
			if (field == null)
			{
				throw new ArgumentNullException(Mixin.Argument.field.ToString());
			}
		}

		// Token: 0x06000910 RID: 2320 RVA: 0x0001CE60 File Offset: 0x0001B060
		public static void CheckMethod(object method)
		{
			if (method == null)
			{
				throw new ArgumentNullException(Mixin.Argument.method.ToString());
			}
		}

		// Token: 0x06000911 RID: 2321 RVA: 0x0001CE88 File Offset: 0x0001B088
		public static void CheckParameters(object parameters)
		{
			if (parameters == null)
			{
				throw new ArgumentNullException(Mixin.Argument.parameters.ToString());
			}
		}

		// Token: 0x06000912 RID: 2322 RVA: 0x0001CEB0 File Offset: 0x0001B0B0
		public static uint GetTimestamp()
		{
			return (uint)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
		}

		// Token: 0x06000913 RID: 2323 RVA: 0x0001CEDF File Offset: 0x0001B0DF
		public static bool HasImage(this ModuleDefinition self)
		{
			return self != null && self.HasImage;
		}

		// Token: 0x06000914 RID: 2324 RVA: 0x0001CEEC File Offset: 0x0001B0EC
		public static string GetFileName(this Stream self)
		{
			FileStream file_stream = self as FileStream;
			if (file_stream == null)
			{
				return string.Empty;
			}
			return Path.GetFullPath(file_stream.Name);
		}

		// Token: 0x06000915 RID: 2325 RVA: 0x0001CF14 File Offset: 0x0001B114
		public static TargetRuntime ParseRuntime(this string self)
		{
			if (string.IsNullOrEmpty(self))
			{
				return TargetRuntime.Net_4_0;
			}
			switch (self[1])
			{
			case '1':
				if (self[3] != '0')
				{
					return TargetRuntime.Net_1_1;
				}
				return TargetRuntime.Net_1_0;
			case '2':
				return TargetRuntime.Net_2_0;
			}
			return TargetRuntime.Net_4_0;
		}

		// Token: 0x06000916 RID: 2326 RVA: 0x0001CF60 File Offset: 0x0001B160
		public static string RuntimeVersionString(this TargetRuntime runtime)
		{
			switch (runtime)
			{
			case TargetRuntime.Net_1_0:
				return "v1.0.3705";
			case TargetRuntime.Net_1_1:
				return "v1.1.4322";
			case TargetRuntime.Net_2_0:
				return "v2.0.50727";
			}
			return "v4.0.30319";
		}

		// Token: 0x06000917 RID: 2327 RVA: 0x0001CF91 File Offset: 0x0001B191
		public static bool IsWindowsMetadata(this ModuleDefinition module)
		{
			return module.MetadataKind > MetadataKind.Ecma335;
		}

		// Token: 0x06000918 RID: 2328 RVA: 0x0001CF9C File Offset: 0x0001B19C
		public static byte[] ReadAll(this Stream self)
		{
			MemoryStream memory = new MemoryStream((int)self.Length);
			byte[] buffer = new byte[1024];
			int read;
			while ((read = self.Read(buffer, 0, buffer.Length)) != 0)
			{
				memory.Write(buffer, 0, read);
			}
			return memory.ToArray();
		}

		// Token: 0x06000919 RID: 2329 RVA: 0x0001B842 File Offset: 0x00019A42
		public static void Read(object o)
		{
		}

		// Token: 0x0600091A RID: 2330 RVA: 0x0001CFE1 File Offset: 0x0001B1E1
		public static bool GetHasSecurityDeclarations(this ISecurityDeclarationProvider self, ModuleDefinition module)
		{
			if (module.HasImage())
			{
				return module.Read<ISecurityDeclarationProvider, bool>(self, (ISecurityDeclarationProvider provider, MetadataReader reader) => reader.HasSecurityDeclarations(provider));
			}
			return false;
		}

		// Token: 0x0600091B RID: 2331 RVA: 0x0001D014 File Offset: 0x0001B214
		public static Collection<SecurityDeclaration> GetSecurityDeclarations(this ISecurityDeclarationProvider self, ref Collection<SecurityDeclaration> variable, ModuleDefinition module)
		{
			if (module.HasImage)
			{
				return module.Read<ISecurityDeclarationProvider, Collection<SecurityDeclaration>>(ref variable, self, (ISecurityDeclarationProvider provider, MetadataReader reader) => reader.ReadSecurityDeclarations(provider));
			}
			Interlocked.CompareExchange<Collection<SecurityDeclaration>>(ref variable, new Collection<SecurityDeclaration>(), null);
			return variable;
		}

		// Token: 0x0600091C RID: 2332 RVA: 0x0001D060 File Offset: 0x0001B260
		public static TypeReference GetEnumUnderlyingType(this TypeDefinition self)
		{
			Collection<FieldDefinition> fields = self.Fields;
			for (int i = 0; i < fields.Count; i++)
			{
				FieldDefinition field = fields[i];
				if (!field.IsStatic)
				{
					return field.FieldType;
				}
			}
			throw new ArgumentException();
		}

		// Token: 0x0600091D RID: 2333 RVA: 0x0001D0A4 File Offset: 0x0001B2A4
		public static TypeDefinition GetNestedType(this TypeDefinition self, string fullname)
		{
			if (!self.HasNestedTypes)
			{
				return null;
			}
			Collection<TypeDefinition> nested_types = self.NestedTypes;
			for (int i = 0; i < nested_types.Count; i++)
			{
				TypeDefinition nested_type = nested_types[i];
				if (nested_type.TypeFullName() == fullname)
				{
					return nested_type;
				}
			}
			return null;
		}

		// Token: 0x0600091E RID: 2334 RVA: 0x0001D0EC File Offset: 0x0001B2EC
		public static bool IsPrimitive(this ElementType self)
		{
			return self - ElementType.Boolean <= 11 || self - ElementType.I <= 1;
		}

		// Token: 0x0600091F RID: 2335 RVA: 0x0001D0FF File Offset: 0x0001B2FF
		public static string TypeFullName(this TypeReference self)
		{
			if (!string.IsNullOrEmpty(self.Namespace))
			{
				return self.Namespace + "." + self.Name;
			}
			return self.Name;
		}

		// Token: 0x06000920 RID: 2336 RVA: 0x0001D12B File Offset: 0x0001B32B
		public static bool IsTypeOf(this TypeReference self, string @namespace, string name)
		{
			return self.Name == name && self.Namespace == @namespace;
		}

		// Token: 0x06000921 RID: 2337 RVA: 0x0001D14C File Offset: 0x0001B34C
		public static bool IsTypeSpecification(this TypeReference type)
		{
			ElementType etype = type.etype;
			switch (etype)
			{
			case ElementType.Ptr:
			case ElementType.ByRef:
			case ElementType.Var:
			case ElementType.Array:
			case ElementType.GenericInst:
			case ElementType.FnPtr:
			case ElementType.SzArray:
			case ElementType.MVar:
			case ElementType.CModReqD:
			case ElementType.CModOpt:
				break;
			case ElementType.ValueType:
			case ElementType.Class:
			case ElementType.TypedByRef:
			case (ElementType)23:
			case ElementType.I:
			case ElementType.U:
			case (ElementType)26:
			case ElementType.Object:
				return false;
			default:
				if (etype != ElementType.Sentinel && etype != ElementType.Pinned)
				{
					return false;
				}
				break;
			}
			return true;
		}

		// Token: 0x06000922 RID: 2338 RVA: 0x0001D1BE File Offset: 0x0001B3BE
		public static TypeDefinition CheckedResolve(this TypeReference self)
		{
			TypeDefinition typeDefinition = self.Resolve();
			if (typeDefinition == null)
			{
				throw new ResolutionException(self);
			}
			return typeDefinition;
		}

		// Token: 0x06000923 RID: 2339 RVA: 0x0001D1D0 File Offset: 0x0001B3D0
		public static bool TryGetCoreLibraryReference(this ModuleDefinition module, out AssemblyNameReference reference)
		{
			Collection<AssemblyNameReference> references = module.AssemblyReferences;
			for (int i = 0; i < references.Count; i++)
			{
				reference = references[i];
				if (Mixin.IsCoreLibrary(reference))
				{
					return true;
				}
			}
			reference = null;
			return false;
		}

		// Token: 0x06000924 RID: 2340 RVA: 0x0001D210 File Offset: 0x0001B410
		public static bool IsCoreLibrary(this ModuleDefinition module)
		{
			if (module.Assembly == null)
			{
				return false;
			}
			if (!Mixin.IsCoreLibrary(module.Assembly.Name))
			{
				return false;
			}
			if (module.HasImage)
			{
				if (module.Read<ModuleDefinition, bool>(module, (ModuleDefinition m, MetadataReader reader) => reader.image.GetTableLength(Table.AssemblyRef) > 0))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000925 RID: 2341 RVA: 0x0001D26E File Offset: 0x0001B46E
		public static void KnownValueType(this TypeReference type)
		{
			if (!type.IsDefinition)
			{
				type.IsValueType = true;
			}
		}

		// Token: 0x06000926 RID: 2342 RVA: 0x0001D280 File Offset: 0x0001B480
		private static bool IsCoreLibrary(AssemblyNameReference reference)
		{
			string name = reference.Name;
			return name == "mscorlib" || name == "System.Runtime" || name == "System.Private.CoreLib" || name == "netstandard";
		}

		// Token: 0x06000927 RID: 2343 RVA: 0x0001D2C8 File Offset: 0x0001B4C8
		public static ImageDebugHeaderEntry GetCodeViewEntry(this ImageDebugHeader header)
		{
			return header.GetEntry(ImageDebugType.CodeView);
		}

		// Token: 0x06000928 RID: 2344 RVA: 0x0001D2D1 File Offset: 0x0001B4D1
		public static ImageDebugHeaderEntry GetDeterministicEntry(this ImageDebugHeader header)
		{
			return header.GetEntry(ImageDebugType.Deterministic);
		}

		// Token: 0x06000929 RID: 2345 RVA: 0x0001D2DC File Offset: 0x0001B4DC
		public static ImageDebugHeader AddDeterministicEntry(this ImageDebugHeader header)
		{
			ImageDebugHeaderEntry entry = new ImageDebugHeaderEntry(new ImageDebugDirectory
			{
				Type = ImageDebugType.Deterministic
			}, Empty<byte>.Array);
			if (header == null)
			{
				return new ImageDebugHeader(entry);
			}
			ImageDebugHeaderEntry[] entries = new ImageDebugHeaderEntry[header.Entries.Length + 1];
			Array.Copy(header.Entries, entries, header.Entries.Length);
			entries[entries.Length - 1] = entry;
			return new ImageDebugHeader(entries);
		}

		// Token: 0x0600092A RID: 2346 RVA: 0x0001D342 File Offset: 0x0001B542
		public static ImageDebugHeaderEntry GetEmbeddedPortablePdbEntry(this ImageDebugHeader header)
		{
			return header.GetEntry(ImageDebugType.EmbeddedPortablePdb);
		}

		// Token: 0x0600092B RID: 2347 RVA: 0x0001D34C File Offset: 0x0001B54C
		public static ImageDebugHeaderEntry GetPdbChecksumEntry(this ImageDebugHeader header)
		{
			return header.GetEntry(ImageDebugType.PdbChecksum);
		}

		// Token: 0x0600092C RID: 2348 RVA: 0x0001D358 File Offset: 0x0001B558
		private static ImageDebugHeaderEntry GetEntry(this ImageDebugHeader header, ImageDebugType type)
		{
			if (!header.HasEntries)
			{
				return null;
			}
			for (int i = 0; i < header.Entries.Length; i++)
			{
				ImageDebugHeaderEntry entry = header.Entries[i];
				if (entry.Directory.Type == type)
				{
					return entry;
				}
			}
			return null;
		}

		// Token: 0x0600092D RID: 2349 RVA: 0x0001D39C File Offset: 0x0001B59C
		public static string GetPdbFileName(string assemblyFileName)
		{
			return Path.ChangeExtension(assemblyFileName, ".pdb");
		}

		// Token: 0x0600092E RID: 2350 RVA: 0x0001D3A9 File Offset: 0x0001B5A9
		public static string GetMdbFileName(string assemblyFileName)
		{
			return assemblyFileName + ".mdb";
		}

		// Token: 0x0600092F RID: 2351 RVA: 0x0001D3B8 File Offset: 0x0001B5B8
		public static bool IsPortablePdb(string fileName)
		{
			bool result;
			using (FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				result = Mixin.IsPortablePdb(file);
			}
			return result;
		}

		// Token: 0x06000930 RID: 2352 RVA: 0x0001D3F4 File Offset: 0x0001B5F4
		public static bool IsPortablePdb(Stream stream)
		{
			if (stream.Length < 4L)
			{
				return false;
			}
			long position = stream.Position;
			bool result;
			try
			{
				result = new BinaryReader(stream).ReadUInt32() == 1112167234U;
			}
			finally
			{
				stream.Position = position;
			}
			return result;
		}

		// Token: 0x06000931 RID: 2353 RVA: 0x0001D444 File Offset: 0x0001B644
		public static bool GetHasCustomDebugInformations(this ICustomDebugInformationProvider self, ref Collection<CustomDebugInformation> collection, ModuleDefinition module)
		{
			if (module.HasImage())
			{
				module.Read<ICustomDebugInformationProvider, Collection<CustomDebugInformation>>(ref collection, self, delegate(ICustomDebugInformationProvider provider, MetadataReader reader)
				{
					ISymbolReader symbol_reader = reader.module.symbol_reader;
					if (symbol_reader != null)
					{
						return symbol_reader.Read(provider);
					}
					return null;
				});
			}
			return !collection.IsNullOrEmpty<CustomDebugInformation>();
		}

		// Token: 0x06000932 RID: 2354 RVA: 0x0001D480 File Offset: 0x0001B680
		public static Collection<CustomDebugInformation> GetCustomDebugInformations(this ICustomDebugInformationProvider self, ref Collection<CustomDebugInformation> collection, ModuleDefinition module)
		{
			if (module.HasImage())
			{
				module.Read<ICustomDebugInformationProvider, Collection<CustomDebugInformation>>(ref collection, self, delegate(ICustomDebugInformationProvider provider, MetadataReader reader)
				{
					ISymbolReader symbol_reader = reader.module.symbol_reader;
					if (symbol_reader != null)
					{
						return symbol_reader.Read(provider);
					}
					return null;
				});
			}
			Interlocked.CompareExchange<Collection<CustomDebugInformation>>(ref collection, new Collection<CustomDebugInformation>(), null);
			return collection;
		}

		// Token: 0x06000933 RID: 2355 RVA: 0x0001D4CC File Offset: 0x0001B6CC
		public static uint ReadCompressedUInt32(this byte[] data, ref int position)
		{
			uint integer;
			if ((data[position] & 128) == 0)
			{
				integer = (uint)data[position];
				position++;
			}
			else if ((data[position] & 64) == 0)
			{
				integer = ((uint)data[position] & 4294967167U) << 8;
				integer |= (uint)data[position + 1];
				position += 2;
			}
			else
			{
				integer = ((uint)data[position] & 4294967103U) << 24;
				integer |= (uint)((uint)data[position + 1] << 16);
				integer |= (uint)((uint)data[position + 2] << 8);
				integer |= (uint)data[position + 3];
				position += 4;
			}
			return integer;
		}

		// Token: 0x06000934 RID: 2356 RVA: 0x0001D550 File Offset: 0x0001B750
		public static MetadataToken GetMetadataToken(this CodedIndex self, uint data)
		{
			uint rid;
			TokenType token_type;
			switch (self)
			{
			case CodedIndex.TypeDefOrRef:
				rid = data >> 2;
				switch (data & 3U)
				{
				case 0U:
					token_type = TokenType.TypeDef;
					break;
				case 1U:
					token_type = TokenType.TypeRef;
					break;
				case 2U:
					token_type = TokenType.TypeSpec;
					break;
				default:
					goto IL_5BB;
				}
				break;
			case CodedIndex.HasConstant:
				rid = data >> 2;
				switch (data & 3U)
				{
				case 0U:
					token_type = TokenType.Field;
					break;
				case 1U:
					token_type = TokenType.Param;
					break;
				case 2U:
					token_type = TokenType.Property;
					break;
				default:
					goto IL_5BB;
				}
				break;
			case CodedIndex.HasCustomAttribute:
				rid = data >> 5;
				switch (data & 31U)
				{
				case 0U:
					token_type = TokenType.Method;
					break;
				case 1U:
					token_type = TokenType.Field;
					break;
				case 2U:
					token_type = TokenType.TypeRef;
					break;
				case 3U:
					token_type = TokenType.TypeDef;
					break;
				case 4U:
					token_type = TokenType.Param;
					break;
				case 5U:
					token_type = TokenType.InterfaceImpl;
					break;
				case 6U:
					token_type = TokenType.MemberRef;
					break;
				case 7U:
					token_type = TokenType.Module;
					break;
				case 8U:
					token_type = TokenType.Permission;
					break;
				case 9U:
					token_type = TokenType.Property;
					break;
				case 10U:
					token_type = TokenType.Event;
					break;
				case 11U:
					token_type = TokenType.Signature;
					break;
				case 12U:
					token_type = TokenType.ModuleRef;
					break;
				case 13U:
					token_type = TokenType.TypeSpec;
					break;
				case 14U:
					token_type = TokenType.Assembly;
					break;
				case 15U:
					token_type = TokenType.AssemblyRef;
					break;
				case 16U:
					token_type = TokenType.File;
					break;
				case 17U:
					token_type = TokenType.ExportedType;
					break;
				case 18U:
					token_type = TokenType.ManifestResource;
					break;
				case 19U:
					token_type = TokenType.GenericParam;
					break;
				case 20U:
					token_type = TokenType.GenericParamConstraint;
					break;
				case 21U:
					token_type = TokenType.MethodSpec;
					break;
				default:
					goto IL_5BB;
				}
				break;
			case CodedIndex.HasFieldMarshal:
			{
				rid = data >> 1;
				uint num = data & 1U;
				if (num != 0U)
				{
					if (num != 1U)
					{
						goto IL_5BB;
					}
					token_type = TokenType.Param;
				}
				else
				{
					token_type = TokenType.Field;
				}
				break;
			}
			case CodedIndex.HasDeclSecurity:
				rid = data >> 2;
				switch (data & 3U)
				{
				case 0U:
					token_type = TokenType.TypeDef;
					break;
				case 1U:
					token_type = TokenType.Method;
					break;
				case 2U:
					token_type = TokenType.Assembly;
					break;
				default:
					goto IL_5BB;
				}
				break;
			case CodedIndex.MemberRefParent:
				rid = data >> 3;
				switch (data & 7U)
				{
				case 0U:
					token_type = TokenType.TypeDef;
					break;
				case 1U:
					token_type = TokenType.TypeRef;
					break;
				case 2U:
					token_type = TokenType.ModuleRef;
					break;
				case 3U:
					token_type = TokenType.Method;
					break;
				case 4U:
					token_type = TokenType.TypeSpec;
					break;
				default:
					goto IL_5BB;
				}
				break;
			case CodedIndex.HasSemantics:
			{
				rid = data >> 1;
				uint num = data & 1U;
				if (num != 0U)
				{
					if (num != 1U)
					{
						goto IL_5BB;
					}
					token_type = TokenType.Property;
				}
				else
				{
					token_type = TokenType.Event;
				}
				break;
			}
			case CodedIndex.MethodDefOrRef:
			{
				rid = data >> 1;
				uint num = data & 1U;
				if (num != 0U)
				{
					if (num != 1U)
					{
						goto IL_5BB;
					}
					token_type = TokenType.MemberRef;
				}
				else
				{
					token_type = TokenType.Method;
				}
				break;
			}
			case CodedIndex.MemberForwarded:
			{
				rid = data >> 1;
				uint num = data & 1U;
				if (num != 0U)
				{
					if (num != 1U)
					{
						goto IL_5BB;
					}
					token_type = TokenType.Method;
				}
				else
				{
					token_type = TokenType.Field;
				}
				break;
			}
			case CodedIndex.Implementation:
				rid = data >> 2;
				switch (data & 3U)
				{
				case 0U:
					token_type = TokenType.File;
					break;
				case 1U:
					token_type = TokenType.AssemblyRef;
					break;
				case 2U:
					token_type = TokenType.ExportedType;
					break;
				default:
					goto IL_5BB;
				}
				break;
			case CodedIndex.CustomAttributeType:
			{
				rid = data >> 3;
				uint num = data & 7U;
				if (num != 2U)
				{
					if (num != 3U)
					{
						goto IL_5BB;
					}
					token_type = TokenType.MemberRef;
				}
				else
				{
					token_type = TokenType.Method;
				}
				break;
			}
			case CodedIndex.ResolutionScope:
				rid = data >> 2;
				switch (data & 3U)
				{
				case 0U:
					token_type = TokenType.Module;
					break;
				case 1U:
					token_type = TokenType.ModuleRef;
					break;
				case 2U:
					token_type = TokenType.AssemblyRef;
					break;
				case 3U:
					token_type = TokenType.TypeRef;
					break;
				default:
					goto IL_5BB;
				}
				break;
			case CodedIndex.TypeOrMethodDef:
			{
				rid = data >> 1;
				uint num = data & 1U;
				if (num != 0U)
				{
					if (num != 1U)
					{
						goto IL_5BB;
					}
					token_type = TokenType.Method;
				}
				else
				{
					token_type = TokenType.TypeDef;
				}
				break;
			}
			case CodedIndex.HasCustomDebugInformation:
				rid = data >> 5;
				switch (data & 31U)
				{
				case 0U:
					token_type = TokenType.Method;
					break;
				case 1U:
					token_type = TokenType.Field;
					break;
				case 2U:
					token_type = TokenType.TypeRef;
					break;
				case 3U:
					token_type = TokenType.TypeDef;
					break;
				case 4U:
					token_type = TokenType.Param;
					break;
				case 5U:
					token_type = TokenType.InterfaceImpl;
					break;
				case 6U:
					token_type = TokenType.MemberRef;
					break;
				case 7U:
					token_type = TokenType.Module;
					break;
				case 8U:
					token_type = TokenType.Permission;
					break;
				case 9U:
					token_type = TokenType.Property;
					break;
				case 10U:
					token_type = TokenType.Event;
					break;
				case 11U:
					token_type = TokenType.Signature;
					break;
				case 12U:
					token_type = TokenType.ModuleRef;
					break;
				case 13U:
					token_type = TokenType.TypeSpec;
					break;
				case 14U:
					token_type = TokenType.Assembly;
					break;
				case 15U:
					token_type = TokenType.AssemblyRef;
					break;
				case 16U:
					token_type = TokenType.File;
					break;
				case 17U:
					token_type = TokenType.ExportedType;
					break;
				case 18U:
					token_type = TokenType.ManifestResource;
					break;
				case 19U:
					token_type = TokenType.GenericParam;
					break;
				case 20U:
					token_type = TokenType.GenericParamConstraint;
					break;
				case 21U:
					token_type = TokenType.MethodSpec;
					break;
				case 22U:
					token_type = TokenType.Document;
					break;
				case 23U:
					token_type = TokenType.LocalScope;
					break;
				case 24U:
					token_type = TokenType.LocalVariable;
					break;
				case 25U:
					token_type = TokenType.LocalConstant;
					break;
				case 26U:
					token_type = TokenType.ImportScope;
					break;
				default:
					goto IL_5BB;
				}
				break;
			default:
				goto IL_5BB;
			}
			return new MetadataToken(token_type, rid);
			IL_5BB:
			return MetadataToken.Zero;
		}

		// Token: 0x06000935 RID: 2357 RVA: 0x0001DB20 File Offset: 0x0001BD20
		public static uint CompressMetadataToken(this CodedIndex self, MetadataToken token)
		{
			uint ret = 0U;
			if (token.RID == 0U)
			{
				return ret;
			}
			switch (self)
			{
			case CodedIndex.TypeDefOrRef:
			{
				ret = token.RID << 2;
				TokenType tokenType = token.TokenType;
				if (tokenType == TokenType.TypeRef)
				{
					return ret | 1U;
				}
				if (tokenType == TokenType.TypeDef)
				{
					return ret | 0U;
				}
				if (tokenType == TokenType.TypeSpec)
				{
					return ret | 2U;
				}
				break;
			}
			case CodedIndex.HasConstant:
			{
				ret = token.RID << 2;
				TokenType tokenType = token.TokenType;
				if (tokenType == TokenType.Field)
				{
					return ret | 0U;
				}
				if (tokenType == TokenType.Param)
				{
					return ret | 1U;
				}
				if (tokenType == TokenType.Property)
				{
					return ret | 2U;
				}
				break;
			}
			case CodedIndex.HasCustomAttribute:
			{
				ret = token.RID << 5;
				TokenType tokenType = token.TokenType;
				if (tokenType <= TokenType.Event)
				{
					if (tokenType <= TokenType.Method)
					{
						if (tokenType <= TokenType.TypeRef)
						{
							if (tokenType == TokenType.Module)
							{
								return ret | 7U;
							}
							if (tokenType == TokenType.TypeRef)
							{
								return ret | 2U;
							}
						}
						else
						{
							if (tokenType == TokenType.TypeDef)
							{
								return ret | 3U;
							}
							if (tokenType == TokenType.Field)
							{
								return ret | 1U;
							}
							if (tokenType == TokenType.Method)
							{
								return ret | 0U;
							}
						}
					}
					else if (tokenType <= TokenType.MemberRef)
					{
						if (tokenType == TokenType.Param)
						{
							return ret | 4U;
						}
						if (tokenType == TokenType.InterfaceImpl)
						{
							return ret | 5U;
						}
						if (tokenType == TokenType.MemberRef)
						{
							return ret | 6U;
						}
					}
					else
					{
						if (tokenType == TokenType.Permission)
						{
							return ret | 8U;
						}
						if (tokenType == TokenType.Signature)
						{
							return ret | 11U;
						}
						if (tokenType == TokenType.Event)
						{
							return ret | 10U;
						}
					}
				}
				else if (tokenType <= TokenType.AssemblyRef)
				{
					if (tokenType <= TokenType.ModuleRef)
					{
						if (tokenType == TokenType.Property)
						{
							return ret | 9U;
						}
						if (tokenType == TokenType.ModuleRef)
						{
							return ret | 12U;
						}
					}
					else
					{
						if (tokenType == TokenType.TypeSpec)
						{
							return ret | 13U;
						}
						if (tokenType == TokenType.Assembly)
						{
							return ret | 14U;
						}
						if (tokenType == TokenType.AssemblyRef)
						{
							return ret | 15U;
						}
					}
				}
				else if (tokenType <= TokenType.ManifestResource)
				{
					if (tokenType == TokenType.File)
					{
						return ret | 16U;
					}
					if (tokenType == TokenType.ExportedType)
					{
						return ret | 17U;
					}
					if (tokenType == TokenType.ManifestResource)
					{
						return ret | 18U;
					}
				}
				else
				{
					if (tokenType == TokenType.GenericParam)
					{
						return ret | 19U;
					}
					if (tokenType == TokenType.MethodSpec)
					{
						return ret | 21U;
					}
					if (tokenType == TokenType.GenericParamConstraint)
					{
						return ret | 20U;
					}
				}
				break;
			}
			case CodedIndex.HasFieldMarshal:
			{
				ret = token.RID << 1;
				TokenType tokenType = token.TokenType;
				if (tokenType == TokenType.Field)
				{
					return ret | 0U;
				}
				if (tokenType == TokenType.Param)
				{
					return ret | 1U;
				}
				break;
			}
			case CodedIndex.HasDeclSecurity:
			{
				ret = token.RID << 2;
				TokenType tokenType = token.TokenType;
				if (tokenType == TokenType.TypeDef)
				{
					return ret | 0U;
				}
				if (tokenType == TokenType.Method)
				{
					return ret | 1U;
				}
				if (tokenType == TokenType.Assembly)
				{
					return ret | 2U;
				}
				break;
			}
			case CodedIndex.MemberRefParent:
			{
				ret = token.RID << 3;
				TokenType tokenType = token.TokenType;
				if (tokenType <= TokenType.TypeDef)
				{
					if (tokenType == TokenType.TypeRef)
					{
						return ret | 1U;
					}
					if (tokenType == TokenType.TypeDef)
					{
						return ret | 0U;
					}
				}
				else
				{
					if (tokenType == TokenType.Method)
					{
						return ret | 3U;
					}
					if (tokenType == TokenType.ModuleRef)
					{
						return ret | 2U;
					}
					if (tokenType == TokenType.TypeSpec)
					{
						return ret | 4U;
					}
				}
				break;
			}
			case CodedIndex.HasSemantics:
			{
				ret = token.RID << 1;
				TokenType tokenType = token.TokenType;
				if (tokenType == TokenType.Event)
				{
					return ret | 0U;
				}
				if (tokenType == TokenType.Property)
				{
					return ret | 1U;
				}
				break;
			}
			case CodedIndex.MethodDefOrRef:
			{
				ret = token.RID << 1;
				TokenType tokenType = token.TokenType;
				if (tokenType == TokenType.Method)
				{
					return ret | 0U;
				}
				if (tokenType == TokenType.MemberRef)
				{
					return ret | 1U;
				}
				break;
			}
			case CodedIndex.MemberForwarded:
			{
				ret = token.RID << 1;
				TokenType tokenType = token.TokenType;
				if (tokenType == TokenType.Field)
				{
					return ret | 0U;
				}
				if (tokenType == TokenType.Method)
				{
					return ret | 1U;
				}
				break;
			}
			case CodedIndex.Implementation:
			{
				ret = token.RID << 2;
				TokenType tokenType = token.TokenType;
				if (tokenType == TokenType.AssemblyRef)
				{
					return ret | 1U;
				}
				if (tokenType == TokenType.File)
				{
					return ret | 0U;
				}
				if (tokenType == TokenType.ExportedType)
				{
					return ret | 2U;
				}
				break;
			}
			case CodedIndex.CustomAttributeType:
			{
				ret = token.RID << 3;
				TokenType tokenType = token.TokenType;
				if (tokenType == TokenType.Method)
				{
					return ret | 2U;
				}
				if (tokenType == TokenType.MemberRef)
				{
					return ret | 3U;
				}
				break;
			}
			case CodedIndex.ResolutionScope:
			{
				ret = token.RID << 2;
				TokenType tokenType = token.TokenType;
				if (tokenType <= TokenType.TypeRef)
				{
					if (tokenType == TokenType.Module)
					{
						return ret | 0U;
					}
					if (tokenType == TokenType.TypeRef)
					{
						return ret | 3U;
					}
				}
				else
				{
					if (tokenType == TokenType.ModuleRef)
					{
						return ret | 1U;
					}
					if (tokenType == TokenType.AssemblyRef)
					{
						return ret | 2U;
					}
				}
				break;
			}
			case CodedIndex.TypeOrMethodDef:
			{
				ret = token.RID << 1;
				TokenType tokenType = token.TokenType;
				if (tokenType == TokenType.TypeDef)
				{
					return ret | 0U;
				}
				if (tokenType == TokenType.Method)
				{
					return ret | 1U;
				}
				break;
			}
			case CodedIndex.HasCustomDebugInformation:
			{
				ret = token.RID << 5;
				TokenType tokenType = token.TokenType;
				if (tokenType <= TokenType.ModuleRef)
				{
					if (tokenType <= TokenType.Param)
					{
						if (tokenType <= TokenType.TypeDef)
						{
							if (tokenType == TokenType.Module)
							{
								return ret | 7U;
							}
							if (tokenType == TokenType.TypeRef)
							{
								return ret | 2U;
							}
							if (tokenType == TokenType.TypeDef)
							{
								return ret | 3U;
							}
						}
						else
						{
							if (tokenType == TokenType.Field)
							{
								return ret | 1U;
							}
							if (tokenType == TokenType.Method)
							{
								return ret | 0U;
							}
							if (tokenType == TokenType.Param)
							{
								return ret | 4U;
							}
						}
					}
					else if (tokenType <= TokenType.Permission)
					{
						if (tokenType == TokenType.InterfaceImpl)
						{
							return ret | 5U;
						}
						if (tokenType == TokenType.MemberRef)
						{
							return ret | 6U;
						}
						if (tokenType == TokenType.Permission)
						{
							return ret | 8U;
						}
					}
					else if (tokenType <= TokenType.Event)
					{
						if (tokenType == TokenType.Signature)
						{
							return ret | 11U;
						}
						if (tokenType == TokenType.Event)
						{
							return ret | 10U;
						}
					}
					else
					{
						if (tokenType == TokenType.Property)
						{
							return ret | 9U;
						}
						if (tokenType == TokenType.ModuleRef)
						{
							return ret | 12U;
						}
					}
				}
				else if (tokenType <= TokenType.GenericParam)
				{
					if (tokenType <= TokenType.AssemblyRef)
					{
						if (tokenType == TokenType.TypeSpec)
						{
							return ret | 13U;
						}
						if (tokenType == TokenType.Assembly)
						{
							return ret | 14U;
						}
						if (tokenType == TokenType.AssemblyRef)
						{
							return ret | 15U;
						}
					}
					else if (tokenType <= TokenType.ExportedType)
					{
						if (tokenType == TokenType.File)
						{
							return ret | 16U;
						}
						if (tokenType == TokenType.ExportedType)
						{
							return ret | 17U;
						}
					}
					else
					{
						if (tokenType == TokenType.ManifestResource)
						{
							return ret | 18U;
						}
						if (tokenType == TokenType.GenericParam)
						{
							return ret | 19U;
						}
					}
				}
				else if (tokenType <= TokenType.Document)
				{
					if (tokenType == TokenType.MethodSpec)
					{
						return ret | 21U;
					}
					if (tokenType == TokenType.GenericParamConstraint)
					{
						return ret | 20U;
					}
					if (tokenType == TokenType.Document)
					{
						return ret | 22U;
					}
				}
				else if (tokenType <= TokenType.LocalVariable)
				{
					if (tokenType == TokenType.LocalScope)
					{
						return ret | 23U;
					}
					if (tokenType == TokenType.LocalVariable)
					{
						return ret | 24U;
					}
				}
				else
				{
					if (tokenType == TokenType.LocalConstant)
					{
						return ret | 25U;
					}
					if (tokenType == TokenType.ImportScope)
					{
						return ret | 26U;
					}
				}
				break;
			}
			}
			throw new ArgumentException();
		}

		// Token: 0x06000936 RID: 2358 RVA: 0x0001E224 File Offset: 0x0001C424
		public static int GetSize(this CodedIndex self, Func<Table, int> counter)
		{
			int bits;
			Table[] tables;
			switch (self)
			{
			case CodedIndex.TypeDefOrRef:
				bits = 2;
				tables = new Table[]
				{
					Table.TypeDef,
					Table.TypeRef,
					Table.TypeSpec
				};
				break;
			case CodedIndex.HasConstant:
				bits = 2;
				tables = new Table[]
				{
					Table.Field,
					Table.Param,
					Table.Property
				};
				break;
			case CodedIndex.HasCustomAttribute:
				bits = 5;
				tables = new Table[]
				{
					Table.Method,
					Table.Field,
					Table.TypeRef,
					Table.TypeDef,
					Table.Param,
					Table.InterfaceImpl,
					Table.MemberRef,
					Table.Module,
					Table.DeclSecurity,
					Table.Property,
					Table.Event,
					Table.StandAloneSig,
					Table.ModuleRef,
					Table.TypeSpec,
					Table.Assembly,
					Table.AssemblyRef,
					Table.File,
					Table.ExportedType,
					Table.ManifestResource,
					Table.GenericParam,
					Table.GenericParamConstraint,
					Table.MethodSpec
				};
				break;
			case CodedIndex.HasFieldMarshal:
				bits = 1;
				tables = new Table[]
				{
					Table.Field,
					Table.Param
				};
				break;
			case CodedIndex.HasDeclSecurity:
				bits = 2;
				tables = new Table[]
				{
					Table.TypeDef,
					Table.Method,
					Table.Assembly
				};
				break;
			case CodedIndex.MemberRefParent:
				bits = 3;
				tables = new Table[]
				{
					Table.TypeDef,
					Table.TypeRef,
					Table.ModuleRef,
					Table.Method,
					Table.TypeSpec
				};
				break;
			case CodedIndex.HasSemantics:
				bits = 1;
				tables = new Table[]
				{
					Table.Event,
					Table.Property
				};
				break;
			case CodedIndex.MethodDefOrRef:
				bits = 1;
				tables = new Table[]
				{
					Table.Method,
					Table.MemberRef
				};
				break;
			case CodedIndex.MemberForwarded:
				bits = 1;
				tables = new Table[]
				{
					Table.Field,
					Table.Method
				};
				break;
			case CodedIndex.Implementation:
				bits = 2;
				tables = new Table[]
				{
					Table.File,
					Table.AssemblyRef,
					Table.ExportedType
				};
				break;
			case CodedIndex.CustomAttributeType:
				bits = 3;
				tables = new Table[]
				{
					Table.Method,
					Table.MemberRef
				};
				break;
			case CodedIndex.ResolutionScope:
				bits = 2;
				tables = new Table[]
				{
					Table.Module,
					Table.ModuleRef,
					Table.AssemblyRef,
					Table.TypeRef
				};
				break;
			case CodedIndex.TypeOrMethodDef:
				bits = 1;
				tables = new Table[]
				{
					Table.TypeDef,
					Table.Method
				};
				break;
			case CodedIndex.HasCustomDebugInformation:
				bits = 5;
				tables = new Table[]
				{
					Table.Method,
					Table.Field,
					Table.TypeRef,
					Table.TypeDef,
					Table.Param,
					Table.InterfaceImpl,
					Table.MemberRef,
					Table.Module,
					Table.DeclSecurity,
					Table.Property,
					Table.Event,
					Table.StandAloneSig,
					Table.ModuleRef,
					Table.TypeSpec,
					Table.Assembly,
					Table.AssemblyRef,
					Table.File,
					Table.ExportedType,
					Table.ManifestResource,
					Table.GenericParam,
					Table.GenericParamConstraint,
					Table.MethodSpec,
					Table.Document,
					Table.LocalScope,
					Table.LocalVariable,
					Table.LocalConstant,
					Table.ImportScope
				};
				break;
			default:
				throw new ArgumentException();
			}
			int max = 0;
			for (int i = 0; i < tables.Length; i++)
			{
				max = Math.Max(counter(tables[i]), max);
			}
			if (max >= 1 << 16 - bits)
			{
				return 4;
			}
			return 2;
		}

		// Token: 0x06000937 RID: 2359 RVA: 0x0001E4FC File Offset: 0x0001C6FC
		public static RSA CreateRSA(this WriterParameters writer_parameters)
		{
			if (writer_parameters.StrongNameKeyBlob != null)
			{
				return CryptoConvert.FromCapiKeyBlob(writer_parameters.StrongNameKeyBlob);
			}
			string key_container;
			byte[] key;
			if (writer_parameters.StrongNameKeyContainer != null)
			{
				key_container = writer_parameters.StrongNameKeyContainer;
			}
			else if (!Mixin.TryGetKeyContainer(writer_parameters.StrongNameKeyPair, out key, out key_container))
			{
				return CryptoConvert.FromCapiKeyBlob(key);
			}
			return new RSACryptoServiceProvider(new CspParameters
			{
				Flags = CspProviderFlags.UseMachineKeyStore,
				KeyContainerName = key_container,
				KeyNumber = 2
			});
		}

		// Token: 0x06000938 RID: 2360 RVA: 0x0001E568 File Offset: 0x0001C768
		private static bool TryGetKeyContainer(ISerializable key_pair, out byte[] key, out string key_container)
		{
			SerializationInfo info = new SerializationInfo(typeof(StrongNameKeyPair), new FormatterConverter());
			key_pair.GetObjectData(info, default(StreamingContext));
			key = (byte[])info.GetValue("_keyPairArray", typeof(byte[]));
			key_container = info.GetString("_keyPairContainer");
			return key_container != null;
		}

		// Token: 0x040002ED RID: 749
		public static Version ZeroVersion = new Version(0, 0, 0, 0);

		// Token: 0x040002EE RID: 750
		public const int NotResolvedMarker = -2;

		// Token: 0x040002EF RID: 751
		public const int NoDataMarker = -1;

		// Token: 0x040002F0 RID: 752
		internal static object NoValue = new object();

		// Token: 0x040002F1 RID: 753
		internal static object NotResolved = new object();

		// Token: 0x040002F2 RID: 754
		public const string mscorlib = "mscorlib";

		// Token: 0x040002F3 RID: 755
		public const string system_runtime = "System.Runtime";

		// Token: 0x040002F4 RID: 756
		public const string system_private_corelib = "System.Private.CoreLib";

		// Token: 0x040002F5 RID: 757
		public const string netstandard = "netstandard";

		// Token: 0x040002F6 RID: 758
		public const int TableCount = 58;

		// Token: 0x040002F7 RID: 759
		public const int CodedIndexCount = 14;

		// Token: 0x020001E2 RID: 482
		public enum Argument
		{
			// Token: 0x040002F9 RID: 761
			name,
			// Token: 0x040002FA RID: 762
			fileName,
			// Token: 0x040002FB RID: 763
			fullName,
			// Token: 0x040002FC RID: 764
			stream,
			// Token: 0x040002FD RID: 765
			type,
			// Token: 0x040002FE RID: 766
			method,
			// Token: 0x040002FF RID: 767
			field,
			// Token: 0x04000300 RID: 768
			parameters,
			// Token: 0x04000301 RID: 769
			module,
			// Token: 0x04000302 RID: 770
			modifierType,
			// Token: 0x04000303 RID: 771
			eventType,
			// Token: 0x04000304 RID: 772
			fieldType,
			// Token: 0x04000305 RID: 773
			declaringType,
			// Token: 0x04000306 RID: 774
			returnType,
			// Token: 0x04000307 RID: 775
			propertyType,
			// Token: 0x04000308 RID: 776
			interfaceType,
			// Token: 0x04000309 RID: 777
			constraintType
		}
	}
}
