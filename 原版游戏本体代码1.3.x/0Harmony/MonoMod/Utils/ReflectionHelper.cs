using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace MonoMod.Utils
{
	// Token: 0x020008D9 RID: 2265
	[NullableContext(1)]
	[Nullable(0)]
	internal static class ReflectionHelper
	{
		// Token: 0x06002F14 RID: 12052 RVA: 0x000A2E50 File Offset: 0x000A1050
		private static MemberInfo _Cache(string cacheKey, MemberInfo value)
		{
			if (cacheKey != null && value == null)
			{
				bool flag;
				MMDbgLog.DebugLogErrorStringHandler debugLogErrorStringHandler = new MMDbgLog.DebugLogErrorStringHandler(21, 1, ref flag);
				if (flag)
				{
					debugLogErrorStringHandler.AppendLiteral("ResolveRefl failure: ");
					debugLogErrorStringHandler.AppendFormatted(cacheKey);
				}
				MMDbgLog.Error(ref debugLogErrorStringHandler);
			}
			if (cacheKey != null && value != null)
			{
				Dictionary<string, WeakReference> resolveReflectionCache = ReflectionHelper.ResolveReflectionCache;
				lock (resolveReflectionCache)
				{
					ReflectionHelper.ResolveReflectionCache[cacheKey] = new WeakReference(value);
				}
			}
			return value;
		}

		// Token: 0x06002F15 RID: 12053 RVA: 0x000A2EDC File Offset: 0x000A10DC
		public static Assembly Load(ModuleDefinition module)
		{
			Helpers.ThrowIfArgumentNull<ModuleDefinition>(module, "module");
			Assembly result;
			using (MemoryStream stream = new MemoryStream())
			{
				module.Write(stream);
				stream.Seek(0L, SeekOrigin.Begin);
				result = ReflectionHelper.Load(stream);
			}
			return result;
		}

		// Token: 0x06002F16 RID: 12054 RVA: 0x000A2F30 File Offset: 0x000A1130
		public static Assembly Load(Stream stream)
		{
			Helpers.ThrowIfArgumentNull<Stream>(stream, "stream");
			MemoryStream ms = stream as MemoryStream;
			Assembly asm;
			if (ms != null)
			{
				asm = Assembly.Load(ms.GetBuffer());
			}
			else
			{
				using (MemoryStream copy = new MemoryStream())
				{
					stream.CopyTo(copy);
					copy.Seek(0L, SeekOrigin.Begin);
					asm = Assembly.Load(copy.GetBuffer());
				}
			}
			AppDomain.CurrentDomain.AssemblyResolve += delegate(object s, ResolveEventArgs e)
			{
				if (!(e.Name == asm.FullName))
				{
					return null;
				}
				return asm;
			};
			return asm;
		}

		// Token: 0x06002F17 RID: 12055 RVA: 0x000A2FCC File Offset: 0x000A11CC
		[return: Nullable(2)]
		public static Type GetType(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return null;
			}
			Type type = Type.GetType(name);
			if (type != null)
			{
				return type;
			}
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				type = assemblies[i].GetType(name);
				if (type != null)
				{
					return type;
				}
			}
			return null;
		}

		// Token: 0x06002F18 RID: 12056 RVA: 0x000A3024 File Offset: 0x000A1224
		public static bool HashIs(this AssemblyNameReference asmRef, Assembly asm, bool defaultIfNoHash = true)
		{
			Helpers.ThrowIfArgumentNull<AssemblyNameReference>(asmRef, "asmRef");
			Helpers.ThrowIfArgumentNull<Assembly>(asm, "asm");
			byte[] hash2 = asmRef.Hash;
			int? num = ((hash2 != null) ? new int?(hash2.Length) : null);
			int num2 = ReflectionHelper.AssemblyHashPrefix.Length + 4;
			if ((num.GetValueOrDefault() == num2) & (num != null))
			{
				byte[] hash = asmRef.Hash;
				for (int i = 0; i < ReflectionHelper.AssemblyHashPrefix.Length; i++)
				{
					if (hash[i] != ReflectionHelper.AssemblyHashPrefix[i])
					{
						return false;
					}
				}
				byte[] rest = BitConverter.GetBytes(asm.GetHashCode());
				for (int j = 0; j < 4; j++)
				{
					if (hash[ReflectionHelper.AssemblyHashPrefix.Length + j] != rest[j])
					{
						return false;
					}
				}
				return true;
			}
			return defaultIfNoHash;
		}

		// Token: 0x06002F19 RID: 12057 RVA: 0x000A30E4 File Offset: 0x000A12E4
		public static void ApplyRuntimeHash(this AssemblyNameReference asmRef, Assembly asm)
		{
			Helpers.ThrowIfArgumentNull<AssemblyNameReference>(asmRef, "asmRef");
			Helpers.ThrowIfArgumentNull<Assembly>(asm, "asm");
			byte[] hash = new byte[ReflectionHelper.AssemblyHashPrefix.Length + 4];
			Array.Copy(ReflectionHelper.AssemblyHashPrefix, 0, hash, 0, ReflectionHelper.AssemblyHashPrefix.Length);
			Array.Copy(BitConverter.GetBytes(asm.GetHashCode()), 0, hash, ReflectionHelper.AssemblyHashPrefix.Length, 4);
			asmRef.HashAlgorithm = (AssemblyHashAlgorithm)4294967295U;
			asmRef.Hash = hash;
		}

		// Token: 0x06002F1A RID: 12058 RVA: 0x000A3154 File Offset: 0x000A1354
		public static string GetRuntimeHashedFullName(this Assembly asm)
		{
			Helpers.ThrowIfArgumentNull<Assembly>(asm, "asm");
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 3);
			defaultInterpolatedStringHandler.AppendFormatted(asm.FullName);
			defaultInterpolatedStringHandler.AppendFormatted(ReflectionHelper.AssemblyHashNameTag);
			defaultInterpolatedStringHandler.AppendFormatted<int>(asm.GetHashCode());
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x06002F1B RID: 12059 RVA: 0x000A31A4 File Offset: 0x000A13A4
		public static string GetRuntimeHashedFullName(this AssemblyNameReference asm)
		{
			Helpers.ThrowIfArgumentNull<AssemblyNameReference>(asm, "asm");
			if (asm.HashAlgorithm != (AssemblyHashAlgorithm)4294967295U)
			{
				return asm.FullName;
			}
			byte[] hash = asm.Hash;
			if (hash.Length != ReflectionHelper.AssemblyHashPrefix.Length + 4)
			{
				return asm.FullName;
			}
			for (int i = 0; i < ReflectionHelper.AssemblyHashPrefix.Length; i++)
			{
				if (hash[i] != ReflectionHelper.AssemblyHashPrefix[i])
				{
					return asm.FullName;
				}
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 3);
			defaultInterpolatedStringHandler.AppendFormatted(asm.FullName);
			defaultInterpolatedStringHandler.AppendFormatted(ReflectionHelper.AssemblyHashNameTag);
			defaultInterpolatedStringHandler.AppendFormatted<int>(BitConverter.ToInt32(hash, ReflectionHelper.AssemblyHashPrefix.Length));
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x06002F1C RID: 12060 RVA: 0x000A324A File Offset: 0x000A144A
		public static Type ResolveReflection(this TypeReference mref)
		{
			return (Type)ReflectionHelper._ResolveReflection(mref, null);
		}

		// Token: 0x06002F1D RID: 12061 RVA: 0x000A3258 File Offset: 0x000A1458
		public static MethodBase ResolveReflection(this MethodReference mref)
		{
			return (MethodBase)ReflectionHelper._ResolveReflection(mref, null);
		}

		// Token: 0x06002F1E RID: 12062 RVA: 0x000A3266 File Offset: 0x000A1466
		public static FieldInfo ResolveReflection(this FieldReference mref)
		{
			return (FieldInfo)ReflectionHelper._ResolveReflection(mref, null);
		}

		// Token: 0x06002F1F RID: 12063 RVA: 0x000A3274 File Offset: 0x000A1474
		public static PropertyInfo ResolveReflection(this PropertyReference mref)
		{
			return (PropertyInfo)ReflectionHelper._ResolveReflection(mref, null);
		}

		// Token: 0x06002F20 RID: 12064 RVA: 0x000A3282 File Offset: 0x000A1482
		public static EventInfo ResolveReflection(this EventReference mref)
		{
			return (EventInfo)ReflectionHelper._ResolveReflection(mref, null);
		}

		// Token: 0x06002F21 RID: 12065 RVA: 0x000A3290 File Offset: 0x000A1490
		public static MemberInfo ResolveReflection(this MemberReference mref)
		{
			return ReflectionHelper._ResolveReflection(mref, null);
		}

		// Token: 0x06002F22 RID: 12066 RVA: 0x000A329C File Offset: 0x000A149C
		[NullableContext(2)]
		[return: NotNullIfNotNull("mref")]
		private static MemberInfo _ResolveReflection(MemberReference mref, [Nullable(new byte[] { 2, 1 })] Module[] modules)
		{
			if (mref == null)
			{
				return null;
			}
			DynamicMethodReference dmref = mref as DynamicMethodReference;
			if (dmref != null)
			{
				return dmref.DynamicMethod;
			}
			MethodReference methodReference = mref as MethodReference;
			string cacheKey = ((methodReference != null) ? methodReference.GetID(null, null, true, false) : null) ?? mref.FullName;
			TypeReference typeReference;
			if ((typeReference = mref.DeclaringType) == null)
			{
				typeReference = (mref as TypeReference) ?? null;
			}
			TypeReference tscope = typeReference;
			ValueTuple<string, string> valueTuple = ReflectionHelper.<_ResolveReflection>g__GetScope|21_0(mref);
			string asmName = valueTuple.Item1;
			string moduleName = valueTuple.Item2;
			if (mref is IGenericInstance)
			{
				IEnumerable<string> keyGroup = ReflectionHelper.<_ResolveReflection>g__GetGenericArgumentsRecursive|21_2(mref).Select(delegate(MemberReference x)
				{
					ValueTuple<string, string> valueTuple2 = ReflectionHelper.<_ResolveReflection>g__GetScope|21_0(x);
					string asmName = valueTuple2.Item1;
					string moduleName = valueTuple2.Item2;
					return ReflectionHelper.<_ResolveReflection>g__ToCacheKeyPart|21_1(asmName, moduleName);
				});
				cacheKey += string.Concat(keyGroup.ToArray<string>());
			}
			else
			{
				cacheKey += ReflectionHelper.<_ResolveReflection>g__ToCacheKeyPart|21_1(asmName, moduleName);
			}
			Dictionary<string, WeakReference> obj = ReflectionHelper.ResolveReflectionCache;
			lock (obj)
			{
				WeakReference cachedRef;
				if (ReflectionHelper.ResolveReflectionCache.TryGetValue(cacheKey, out cachedRef) && cachedRef != null)
				{
					MemberInfo cached = cachedRef.SafeGetTarget() as MemberInfo;
					if (cached != null)
					{
						return cached;
					}
				}
			}
			if (mref is GenericParameter)
			{
				throw new NotSupportedException("ResolveReflection on GenericParameter currently not supported");
			}
			MethodReference method = mref as MethodReference;
			Type type;
			if (method != null && mref.DeclaringType is ArrayType)
			{
				type = (Type)ReflectionHelper._ResolveReflection(mref.DeclaringType, modules);
				string methodID = method.GetID(null, null, false, false);
				MethodBase found = type.GetMethods((BindingFlags)(-1)).Cast<MethodBase>().Concat(type.GetConstructors((BindingFlags)(-1)))
					.FirstOrDefault((MethodBase m) => m.GetID(null, null, false, false, false) == methodID);
				if (found != null)
				{
					return ReflectionHelper._Cache(cacheKey, found);
				}
			}
			if (tscope == null)
			{
				throw new ArgumentException("MemberReference hasn't got a DeclaringType / isn't a TypeReference in itself");
			}
			if (asmName == null && moduleName == null)
			{
				throw new NotSupportedException("Unsupported scope type " + tscope.Scope.GetType().FullName);
			}
			bool tryAssemblyCache = true;
			bool refetchingModules = false;
			bool nullifyModules = false;
			Func<Type, bool> <>9__24;
			Func<MethodInfo, bool> <>9__25;
			Func<FieldInfo, bool> <>9__26;
			TypeSpecification ts;
			MemberInfo member;
			for (;;)
			{
				if (nullifyModules)
				{
					modules = null;
				}
				nullifyModules = true;
				if (modules == null)
				{
					Assembly[] asms = null;
					if (tryAssemblyCache && refetchingModules)
					{
						refetchingModules = false;
						tryAssemblyCache = false;
					}
					if (tryAssemblyCache)
					{
						obj = ReflectionHelper.AssemblyCache;
						lock (obj)
						{
							WeakReference asmRef2;
							if (ReflectionHelper.AssemblyCache.TryGetValue(asmName, out asmRef2))
							{
								Assembly asm2 = asmRef2.SafeGetTarget() as Assembly;
								if (asm2 != null)
								{
									asms = new Assembly[] { asm2 };
								}
							}
						}
					}
					if (asms == null && !refetchingModules)
					{
						Dictionary<string, WeakReference[]> assembliesCache = ReflectionHelper.AssembliesCache;
						lock (assembliesCache)
						{
							WeakReference[] asmRefs;
							if (ReflectionHelper.AssembliesCache.TryGetValue(asmName, out asmRefs))
							{
								asms = (from asmRef in asmRefs
									select asmRef.SafeGetTarget() as Assembly into asm
									where asm != null
									select asm).ToArray<Assembly>();
							}
						}
					}
					if (asms == null)
					{
						int split = asmName.IndexOf(ReflectionHelper.AssemblyHashNameTag, StringComparison.Ordinal);
						int hash;
						if (split != -1 && int.TryParse(asmName.Substring(split + 2), out hash))
						{
							asms = (from other in AppDomain.CurrentDomain.GetAssemblies()
								where other.GetHashCode() == hash
								select other).ToArray<Assembly>();
							if (asms.Length == 0)
							{
								asms = null;
							}
							asmName = asmName.Substring(0, split);
						}
						if (asms == null)
						{
							asms = (from other in AppDomain.CurrentDomain.GetAssemblies()
								where other.GetName().FullName == asmName
								select other).ToArray<Assembly>();
							if (asms.Length == 0)
							{
								asms = (from other in AppDomain.CurrentDomain.GetAssemblies()
									where other.GetName().Name == asmName
									select other).ToArray<Assembly>();
							}
							if (asms.Length == 0)
							{
								Assembly loaded = Assembly.Load(new AssemblyName(asmName));
								if (loaded != null)
								{
									asms = new Assembly[] { loaded };
								}
							}
						}
						if (asms.Length != 0)
						{
							Dictionary<string, WeakReference[]> assembliesCache = ReflectionHelper.AssembliesCache;
							lock (assembliesCache)
							{
								ReflectionHelper.AssembliesCache[asmName] = (from asm in asms
									select new WeakReference(asm)).ToArray<WeakReference>();
							}
						}
					}
					IEnumerable<Module> source;
					if (!string.IsNullOrEmpty(moduleName))
					{
						source = from asm in asms
							select asm.GetModule(moduleName);
					}
					else
					{
						source = asms.SelectMany((Assembly asm) => asm.GetModules());
					}
					modules = (from mod in source
						where mod != null
						select mod).ToArray<Module>();
					if (modules.Length == 0)
					{
						break;
					}
				}
				TypeReference tref = mref as TypeReference;
				if (tref != null)
				{
					if (tref.FullName == "<Module>")
					{
						goto Block_40;
					}
					ts = mref as TypeSpecification;
					if (ts != null)
					{
						goto Block_41;
					}
					type = (from module in modules
						select module.GetType(mref.FullName.Replace("/", "+", StringComparison.Ordinal), false, false)).FirstOrDefault((Type m) => m != null);
					if (type == null)
					{
						type = modules.Select(delegate(Module module)
						{
							IEnumerable<Type> types = module.GetTypes();
							Func<Type, bool> predicate;
							if ((predicate = <>9__24) == null)
							{
								predicate = (<>9__24 = (Type m) => mref.Is(m));
							}
							return types.FirstOrDefault(predicate);
						}).FirstOrDefault((Type m) => m != null);
					}
					if (!(type == null) || refetchingModules)
					{
						goto IL_6F2;
					}
				}
				else
				{
					TypeReference declaringType = mref.DeclaringType;
					bool typeless = ((declaringType != null) ? declaringType.FullName : null) == "<Module>";
					GenericInstanceMethod mrefGenMethod = mref as GenericInstanceMethod;
					if (mrefGenMethod != null)
					{
						member = ReflectionHelper._ResolveReflection(mrefGenMethod.ElementMethod, modules);
						MethodInfo methodInfo = member as MethodInfo;
						MemberInfo memberInfo;
						if (methodInfo == null)
						{
							memberInfo = null;
						}
						else
						{
							memberInfo = methodInfo.MakeGenericMethod((from arg in mrefGenMethod.GenericArguments
								select ReflectionHelper._ResolveReflection(arg, null) as Type).ToArray<Type>());
						}
						member = memberInfo;
					}
					else if (typeless)
					{
						if (mref is MethodReference)
						{
							member = modules.Select(delegate(Module module)
							{
								IEnumerable<MethodInfo> methods = module.GetMethods((BindingFlags)(-1));
								Func<MethodInfo, bool> predicate;
								if ((predicate = <>9__25) == null)
								{
									predicate = (<>9__25 = (MethodInfo m) => mref.Is(m));
								}
								return methods.FirstOrDefault(predicate);
							}).FirstOrDefault((MethodInfo m) => m != null);
						}
						else
						{
							if (!(mref is FieldReference))
							{
								goto IL_823;
							}
							member = modules.Select(delegate(Module module)
							{
								IEnumerable<FieldInfo> fields = module.GetFields((BindingFlags)(-1));
								Func<FieldInfo, bool> predicate;
								if ((predicate = <>9__26) == null)
								{
									predicate = (<>9__26 = (FieldInfo m) => mref.Is(m));
								}
								return fields.FirstOrDefault(predicate);
							}).FirstOrDefault((FieldInfo m) => m != null);
						}
					}
					else
					{
						Type declType = (Type)ReflectionHelper._ResolveReflection(mref.DeclaringType, modules);
						if (mref is MethodReference)
						{
							member = declType.GetMethods((BindingFlags)(-1)).Cast<MethodBase>().Concat(declType.GetConstructors((BindingFlags)(-1)))
								.FirstOrDefault((MethodBase m) => mref.Is(m));
						}
						else if (mref is FieldReference)
						{
							member = declType.GetFields((BindingFlags)(-1)).FirstOrDefault((FieldInfo m) => mref.Is(m));
						}
						else
						{
							member = declType.GetMembers((BindingFlags)(-1)).FirstOrDefault((MemberInfo m) => mref.Is(m));
						}
					}
					if (!(member == null) || refetchingModules)
					{
						goto IL_8ED;
					}
				}
				refetchingModules = true;
			}
			throw new MissingMemberException("Cannot resolve assembly / module " + asmName + " / " + moduleName);
			Block_40:
			throw new ArgumentException("Type <Module> cannot be resolved to a runtime reflection type");
			Block_41:
			type = (Type)ReflectionHelper._ResolveReflection(ts.ElementType, null);
			if (ts.IsByReference)
			{
				return ReflectionHelper._Cache(cacheKey, type.MakeByRefType());
			}
			if (ts.IsPointer)
			{
				return ReflectionHelper._Cache(cacheKey, type.MakePointerType());
			}
			if (ts.IsArray)
			{
				return ReflectionHelper._Cache(cacheKey, ((ArrayType)ts).IsVector ? type.MakeArrayType() : type.MakeArrayType(((ArrayType)ts).Dimensions.Count));
			}
			if (ts.IsGenericInstance)
			{
				return ReflectionHelper._Cache(cacheKey, type.MakeGenericType((from arg in ((GenericInstanceType)ts).GenericArguments
					select ReflectionHelper._ResolveReflection(arg, null) as Type).ToArray<Type>()));
			}
			IL_6F2:
			return ReflectionHelper._Cache(cacheKey, type);
			IL_823:
			throw new NotSupportedException("Unsupported <Module> member type " + mref.GetType().FullName);
			IL_8ED:
			return ReflectionHelper._Cache(cacheKey, member);
		}

		// Token: 0x06002F23 RID: 12067 RVA: 0x000A3BD8 File Offset: 0x000A1DD8
		public static SignatureHelper ResolveReflection(this Mono.Cecil.CallSite csite, Module context)
		{
			return csite.ResolveReflectionSignature(context);
		}

		// Token: 0x06002F24 RID: 12068 RVA: 0x000A3BE4 File Offset: 0x000A1DE4
		public static SignatureHelper ResolveReflectionSignature(this IMethodSignature csite, Module context)
		{
			Helpers.ThrowIfArgumentNull<IMethodSignature>(csite, "csite");
			Helpers.ThrowIfArgumentNull<Module>(context, "context");
			SignatureHelper shelper;
			switch (csite.CallingConvention)
			{
			case MethodCallingConvention.C:
				shelper = ReflectionHelper.GetUnmanagedSigHelper(context, CallingConvention.Cdecl, csite.ReturnType.ResolveReflection());
				break;
			case MethodCallingConvention.StdCall:
				shelper = ReflectionHelper.GetUnmanagedSigHelper(context, CallingConvention.StdCall, csite.ReturnType.ResolveReflection());
				break;
			case MethodCallingConvention.ThisCall:
				shelper = ReflectionHelper.GetUnmanagedSigHelper(context, CallingConvention.ThisCall, csite.ReturnType.ResolveReflection());
				break;
			case MethodCallingConvention.FastCall:
				shelper = ReflectionHelper.GetUnmanagedSigHelper(context, CallingConvention.FastCall, csite.ReturnType.ResolveReflection());
				break;
			case MethodCallingConvention.VarArg:
				shelper = SignatureHelper.GetMethodSigHelper(context, CallingConventions.VarArgs, csite.ReturnType.ResolveReflection());
				break;
			default:
				if (csite.ExplicitThis)
				{
					shelper = SignatureHelper.GetMethodSigHelper(context, CallingConventions.ExplicitThis, csite.ReturnType.ResolveReflection());
				}
				else
				{
					shelper = SignatureHelper.GetMethodSigHelper(context, CallingConventions.Standard, csite.ReturnType.ResolveReflection());
				}
				break;
			}
			if (context != null)
			{
				List<Type> modReq = new List<Type>();
				List<Type> modOpt = new List<Type>();
				using (Collection<ParameterDefinition>.Enumerator enumerator = csite.Parameters.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						ParameterDefinition param = enumerator.Current;
						if (param.ParameterType.IsSentinel)
						{
							shelper.AddSentinel();
						}
						if (param.ParameterType.IsPinned)
						{
							shelper.AddArgument(param.ParameterType.ResolveReflection(), true);
						}
						else
						{
							modOpt.Clear();
							modReq.Clear();
							TypeReference paramTypeRef = param.ParameterType;
							for (;;)
							{
								TypeSpecification paramTypeSpec = paramTypeRef as TypeSpecification;
								if (paramTypeSpec == null)
								{
									break;
								}
								RequiredModifierType paramTypeModReq = paramTypeRef as RequiredModifierType;
								if (paramTypeModReq == null)
								{
									OptionalModifierType paramTypeOptReq = paramTypeRef as OptionalModifierType;
									if (paramTypeOptReq != null)
									{
										modOpt.Add(paramTypeOptReq.ModifierType.ResolveReflection());
									}
								}
								else
								{
									modReq.Add(paramTypeModReq.ModifierType.ResolveReflection());
								}
								paramTypeRef = paramTypeSpec.ElementType;
							}
							shelper.AddArgument(param.ParameterType.ResolveReflection(), modReq.ToArray(), modOpt.ToArray());
						}
					}
					return shelper;
				}
			}
			foreach (ParameterDefinition param2 in csite.Parameters)
			{
				shelper.AddArgument(param2.ParameterType.ResolveReflection());
			}
			return shelper;
		}

		// Token: 0x06002F25 RID: 12069 RVA: 0x000A3E58 File Offset: 0x000A2058
		static ReflectionHelper()
		{
			MethodInfo getUnmanagedSigHelperMethod = ReflectionHelper.GetUnmanagedSigHelperMethod;
			ReflectionHelper.GetUnmanagedSigHelper = ((getUnmanagedSigHelperMethod != null) ? getUnmanagedSigHelperMethod.TryCreateDelegate<ReflectionHelper.GetUnmanagedSigHelperDelegate>() : null) ?? delegate(Module _, CallingConvention _, Type _)
			{
				throw new NotImplementedException("Unmanaged calling conventions are not supported");
			};
			object[] array = new object[2];
			array[0] = 0;
			ReflectionHelper._CacheGetterArgs = array;
			ReflectionHelper.t_RuntimeType = typeof(Type).Assembly.GetType("System.RuntimeType");
			Type type = ReflectionHelper.t_RuntimeType;
			ReflectionHelper.t_RuntimeTypeCache = ((type != null) ? type.GetNestedType("RuntimeTypeCache", BindingFlags.Public | BindingFlags.NonPublic) : null);
			PropertyInfo propertyInfo;
			if (!(ReflectionHelper.t_RuntimeTypeCache == null))
			{
				Type type2 = ReflectionHelper.t_RuntimeType;
				propertyInfo = ((type2 != null) ? type2.GetProperty("Cache", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, ReflectionHelper.t_RuntimeTypeCache, Type.EmptyTypes, null) : null);
			}
			else
			{
				propertyInfo = null;
			}
			ReflectionHelper.p_RuntimeType_Cache = propertyInfo;
			Type type3 = ReflectionHelper.t_RuntimeTypeCache;
			ReflectionHelper.m_RuntimeTypeCache_GetFieldList = ((type3 != null) ? type3.GetMethod("GetFieldList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : null);
			Type type4 = ReflectionHelper.t_RuntimeTypeCache;
			ReflectionHelper.m_RuntimeTypeCache_GetPropertyList = ((type4 != null) ? type4.GetMethod("GetPropertyList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : null);
			ReflectionHelper._CacheFixed = new ConditionalWeakTable<Type, ReflectionHelper.CacheFixEntry>();
			ReflectionHelper.t_RuntimeModule = typeof(Module).Assembly.GetType("System.Reflection.RuntimeModule");
			Type type5 = typeof(Module).Assembly.GetType("System.Reflection.RuntimeModule");
			ReflectionHelper.p_RuntimeModule_RuntimeType = ((type5 != null) ? type5.GetProperty("RuntimeType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : null);
			Type type6 = typeof(Module).Assembly.GetType("System.Reflection.RuntimeModule");
			ReflectionHelper.f_RuntimeModule__impl = ((type6 != null) ? type6.GetField("_impl", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : null);
			Type type7 = typeof(Module).Assembly.GetType("System.Reflection.RuntimeModule");
			ReflectionHelper.m_RuntimeModule_GetGlobalType = ((type7 != null) ? type7.GetMethod("GetGlobalType", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : null);
			ReflectionHelper.f_SignatureHelper_module = typeof(SignatureHelper).GetField("m_module", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? typeof(SignatureHelper).GetField("module", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		// Token: 0x06002F26 RID: 12070 RVA: 0x000A4100 File Offset: 0x000A2300
		public static void FixReflectionCacheAuto(this Type type)
		{
			type.FixReflectionCache();
		}

		// Token: 0x06002F27 RID: 12071 RVA: 0x000A4108 File Offset: 0x000A2308
		[NullableContext(2)]
		public static void FixReflectionCache(this Type type)
		{
			if (ReflectionHelper.t_RuntimeType == null || ReflectionHelper.p_RuntimeType_Cache == null || ReflectionHelper.m_RuntimeTypeCache_GetFieldList == null || ReflectionHelper.m_RuntimeTypeCache_GetPropertyList == null)
			{
				return;
			}
			while (type != null)
			{
				if (ReflectionHelper.t_RuntimeType.IsInstanceOfType(type))
				{
					ReflectionHelper.CacheFixEntry entry = ReflectionHelper._CacheFixed.GetValue(type, delegate(Type rt)
					{
						ReflectionHelper.CacheFixEntry cacheFixEntry = new ReflectionHelper.CacheFixEntry();
						object cache = (cacheFixEntry.Cache = ReflectionHelper.p_RuntimeType_Cache.GetValue(rt, ArrayEx.Empty<object>()));
						Array properties = (cacheFixEntry.Properties = ReflectionHelper._GetArray(cache, ReflectionHelper.m_RuntimeTypeCache_GetPropertyList));
						Array fields = (cacheFixEntry.Fields = ReflectionHelper._GetArray(cache, ReflectionHelper.m_RuntimeTypeCache_GetFieldList));
						ReflectionHelper._FixReflectionCacheOrder<PropertyInfo>(properties);
						ReflectionHelper._FixReflectionCacheOrder<FieldInfo>(fields);
						cacheFixEntry.NeedsVerify = false;
						return cacheFixEntry;
					});
					if (entry.NeedsVerify && !ReflectionHelper._Verify(entry, type))
					{
						ReflectionHelper.CacheFixEntry obj = entry;
						lock (obj)
						{
							ReflectionHelper._FixReflectionCacheOrder<PropertyInfo>(entry.Properties);
							ReflectionHelper._FixReflectionCacheOrder<FieldInfo>(entry.Fields);
						}
					}
					entry.NeedsVerify = true;
				}
				type = type.DeclaringType;
			}
		}

		// Token: 0x06002F28 RID: 12072 RVA: 0x000A41F0 File Offset: 0x000A23F0
		private static bool _Verify(ReflectionHelper.CacheFixEntry entry, Type type)
		{
			object cache;
			if (entry.Cache != (cache = ReflectionHelper.p_RuntimeType_Cache.GetValue(type, ArrayEx.Empty<object>())))
			{
				entry.Cache = cache;
				entry.Properties = ReflectionHelper._GetArray(cache, ReflectionHelper.m_RuntimeTypeCache_GetPropertyList);
				entry.Fields = ReflectionHelper._GetArray(cache, ReflectionHelper.m_RuntimeTypeCache_GetFieldList);
				return false;
			}
			Array properties;
			if (entry.Properties != (properties = ReflectionHelper._GetArray(cache, ReflectionHelper.m_RuntimeTypeCache_GetPropertyList)))
			{
				entry.Properties = properties;
				entry.Fields = ReflectionHelper._GetArray(cache, ReflectionHelper.m_RuntimeTypeCache_GetFieldList);
				return false;
			}
			Array fields;
			if (entry.Fields != (fields = ReflectionHelper._GetArray(cache, ReflectionHelper.m_RuntimeTypeCache_GetFieldList)))
			{
				entry.Fields = fields;
				return false;
			}
			return true;
		}

		// Token: 0x06002F29 RID: 12073 RVA: 0x000A4290 File Offset: 0x000A2490
		private static Array _GetArray([Nullable(2)] object cache, MethodInfo getter)
		{
			getter.Invoke(cache, ReflectionHelper._CacheGetterArgs);
			object obj = getter.Invoke(cache, ReflectionHelper._CacheGetterArgs);
			Array array = obj as Array;
			if (array != null)
			{
				return array;
			}
			Type cerArrayListType = getter.ReturnType;
			if (cerArrayListType != null && cerArrayListType.Namespace == "System.Reflection" && cerArrayListType.Name == "CerArrayList`1")
			{
				return (Array)cerArrayListType.GetField("m_array", (BindingFlags)(-1)).GetValue(obj);
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(30, 1);
			defaultInterpolatedStringHandler.AppendLiteral("Unknown reflection cache type ");
			defaultInterpolatedStringHandler.AppendFormatted<Type>(obj.GetType());
			throw new InvalidOperationException(defaultInterpolatedStringHandler.ToStringAndClear());
		}

		// Token: 0x06002F2A RID: 12074 RVA: 0x000A4338 File Offset: 0x000A2538
		[NullableContext(0)]
		private static void _FixReflectionCacheOrder<T>([Nullable(2)] Array orig) where T : MemberInfo
		{
			if (orig == null)
			{
				return;
			}
			List<T> list = new List<T>(orig.Length);
			for (int i = 0; i < orig.Length; i++)
			{
				list.Add((T)((object)orig.GetValue(i)));
			}
			list.Sort(delegate(T a, T b)
			{
				if (a == b)
				{
					return 0;
				}
				if (a == null)
				{
					return 1;
				}
				if (b == null)
				{
					return -1;
				}
				return a.MetadataToken - b.MetadataToken;
			});
			for (int j = orig.Length - 1; j >= 0; j--)
			{
				orig.SetValue(list[j], j);
			}
		}

		// Token: 0x06002F2B RID: 12075 RVA: 0x000A43C4 File Offset: 0x000A25C4
		[NullableContext(2)]
		public static Type GetModuleType(this Module module)
		{
			if (module == null || ReflectionHelper.t_RuntimeModule == null || !ReflectionHelper.t_RuntimeModule.IsInstanceOfType(module))
			{
				return null;
			}
			if (ReflectionHelper.p_RuntimeModule_RuntimeType != null)
			{
				return (Type)ReflectionHelper.p_RuntimeModule_RuntimeType.GetValue(module, ArrayEx.Empty<object>());
			}
			if (ReflectionHelper.f_RuntimeModule__impl != null && ReflectionHelper.m_RuntimeModule_GetGlobalType != null)
			{
				return (Type)ReflectionHelper.m_RuntimeModule_GetGlobalType.Invoke(null, new object[] { ReflectionHelper.f_RuntimeModule__impl.GetValue(module) });
			}
			return null;
		}

		// Token: 0x06002F2C RID: 12076 RVA: 0x000A4459 File Offset: 0x000A2659
		[return: Nullable(2)]
		public static Type GetRealDeclaringType(this MemberInfo member)
		{
			Type result;
			if ((result = Helpers.ThrowIfNull<MemberInfo>(member, "member").DeclaringType) == null)
			{
				Module module = member.Module;
				if (module == null)
				{
					return null;
				}
				result = module.GetModuleType();
			}
			return result;
		}

		// Token: 0x06002F2D RID: 12077 RVA: 0x000A4480 File Offset: 0x000A2680
		private static Module GetSignatureHelperModule(SignatureHelper signature)
		{
			if (ReflectionHelper.f_SignatureHelper_module == null)
			{
				throw new InvalidOperationException("Unable to find module field for SignatureHelper");
			}
			return (Module)ReflectionHelper.f_SignatureHelper_module.GetValue(signature);
		}

		// Token: 0x06002F2E RID: 12078 RVA: 0x000A44AA File Offset: 0x000A26AA
		public static Mono.Cecil.CallSite ImportCallSite(this ModuleDefinition moduleTo, ICallSiteGenerator signature)
		{
			return Helpers.ThrowIfNull<ICallSiteGenerator>(signature, "signature").ToCallSite(moduleTo);
		}

		// Token: 0x06002F2F RID: 12079 RVA: 0x000A44BD File Offset: 0x000A26BD
		public static Mono.Cecil.CallSite ImportCallSite(this ModuleDefinition moduleTo, SignatureHelper signature)
		{
			return Helpers.ThrowIfNull<ModuleDefinition>(moduleTo, "moduleTo").ImportCallSite(ReflectionHelper.GetSignatureHelperModule(signature), Helpers.ThrowIfNull<SignatureHelper>(signature, "signature").GetSignature());
		}

		// Token: 0x06002F30 RID: 12080 RVA: 0x000A44E5 File Offset: 0x000A26E5
		public static Mono.Cecil.CallSite ImportCallSite(this ModuleDefinition moduleTo, Module moduleFrom, int token)
		{
			return Helpers.ThrowIfNull<ModuleDefinition>(moduleTo, "moduleTo").ImportCallSite(moduleFrom, Helpers.ThrowIfNull<Module>(moduleFrom, "moduleFrom").ResolveSignature(token));
		}

		// Token: 0x06002F31 RID: 12081 RVA: 0x000A450C File Offset: 0x000A270C
		public static Mono.Cecil.CallSite ImportCallSite(this ModuleDefinition moduleTo, Module moduleFrom, byte[] data)
		{
			ReflectionHelper.<>c__DisplayClass52_0 CS$<>8__locals1;
			CS$<>8__locals1.moduleTo = moduleTo;
			CS$<>8__locals1.moduleFrom = moduleFrom;
			Helpers.ThrowIfArgumentNull<ModuleDefinition>(CS$<>8__locals1.moduleTo, "moduleTo");
			Helpers.ThrowIfArgumentNull<Module>(CS$<>8__locals1.moduleFrom, "moduleFrom");
			Helpers.ThrowIfArgumentNull<byte[]>(data, "data");
			Mono.Cecil.CallSite callsite = new Mono.Cecil.CallSite(CS$<>8__locals1.moduleTo.TypeSystem.Void);
			Mono.Cecil.CallSite result;
			using (MemoryStream stream = new MemoryStream(data, false))
			{
				ReflectionHelper.<>c__DisplayClass52_1 CS$<>8__locals2;
				CS$<>8__locals2.reader = new BinaryReader(stream);
				try
				{
					ReflectionHelper.<ImportCallSite>g__ReadMethodSignature|52_0(callsite, ref CS$<>8__locals1, ref CS$<>8__locals2);
					result = callsite;
				}
				finally
				{
					if (CS$<>8__locals2.reader != null)
					{
						((IDisposable)CS$<>8__locals2.reader).Dispose();
					}
				}
			}
			return result;
		}

		// Token: 0x06002F32 RID: 12082 RVA: 0x000A45CC File Offset: 0x000A27CC
		[CompilerGenerated]
		[return: Nullable(new byte[] { 0, 2, 2 })]
		internal static ValueTuple<string, string> <_ResolveReflection>g__GetScope|21_0(MemberReference mref)
		{
			TypeReference typeReference;
			if ((typeReference = mref.DeclaringType) == null)
			{
				typeReference = (mref as TypeReference) ?? null;
			}
			TypeReference tscope = typeReference;
			IMetadataScope metadataScope = ((tscope != null) ? tscope.Scope : null);
			AssemblyNameReference asmNameRef = metadataScope as AssemblyNameReference;
			ValueTuple<string, string> result;
			if (asmNameRef == null)
			{
				ModuleDefinition moduleDef = metadataScope as ModuleDefinition;
				if (moduleDef == null)
				{
					if (!(metadataScope is ModuleReference))
					{
						result = new ValueTuple<string, string>(null, null);
					}
					else
					{
						result = new ValueTuple<string, string>(tscope.Module.Assembly.Name.GetRuntimeHashedFullName(), tscope.Module.Name);
					}
				}
				else
				{
					result = new ValueTuple<string, string>(moduleDef.Assembly.Name.GetRuntimeHashedFullName(), moduleDef.Name);
				}
			}
			else
			{
				result = new ValueTuple<string, string>(asmNameRef.GetRuntimeHashedFullName(), null);
			}
			return result;
		}

		// Token: 0x06002F33 RID: 12083 RVA: 0x000A467C File Offset: 0x000A287C
		[NullableContext(2)]
		[CompilerGenerated]
		[return: Nullable(1)]
		internal static string <_ResolveReflection>g__ToCacheKeyPart|21_1(string asmName, string moduleName)
		{
			return " | " + (asmName ?? "NOASSEMBLY") + ", " + (moduleName ?? "NOMODULE");
		}

		// Token: 0x06002F34 RID: 12084 RVA: 0x000A46A1 File Offset: 0x000A28A1
		[CompilerGenerated]
		internal static IEnumerable<MemberReference> <_ResolveReflection>g__GetGenericArgumentsRecursive|21_2(MemberReference mref)
		{
			ReflectionHelper.<<_ResolveReflection>g__GetGenericArgumentsRecursive|21_2>d <<_ResolveReflection>g__GetGenericArgumentsRecursive|21_2>d = new ReflectionHelper.<<_ResolveReflection>g__GetGenericArgumentsRecursive|21_2>d(-2);
			<<_ResolveReflection>g__GetGenericArgumentsRecursive|21_2>d.<>3__mref = mref;
			return <<_ResolveReflection>g__GetGenericArgumentsRecursive|21_2>d;
		}

		// Token: 0x06002F35 RID: 12085 RVA: 0x000A46B4 File Offset: 0x000A28B4
		[CompilerGenerated]
		internal static void <ImportCallSite>g__ReadMethodSignature|52_0(IMethodSignature method, ref ReflectionHelper.<>c__DisplayClass52_0 A_1, ref ReflectionHelper.<>c__DisplayClass52_1 A_2)
		{
			byte callConv = A_2.reader.ReadByte();
			if ((callConv & 32) != 0)
			{
				method.HasThis = true;
				callConv = (byte)((int)callConv & -33);
			}
			if ((callConv & 64) != 0)
			{
				method.ExplicitThis = true;
				callConv = (byte)((int)callConv & -65);
			}
			method.CallingConvention = (MethodCallingConvention)callConv;
			if ((callConv & 16) != 0)
			{
				ReflectionHelper.<ImportCallSite>g__ReadCompressedUInt32|52_1(ref A_2);
			}
			uint paramCount = ReflectionHelper.<ImportCallSite>g__ReadCompressedUInt32|52_1(ref A_2);
			method.MethodReturnType.ReturnType = ReflectionHelper.<ImportCallSite>g__ReadTypeSignature|52_4(ref A_1, ref A_2);
			int i = 0;
			while ((long)i < (long)((ulong)paramCount))
			{
				method.Parameters.Add(new ParameterDefinition(ReflectionHelper.<ImportCallSite>g__ReadTypeSignature|52_4(ref A_1, ref A_2)));
				i++;
			}
		}

		// Token: 0x06002F36 RID: 12086 RVA: 0x000A4748 File Offset: 0x000A2948
		[CompilerGenerated]
		internal static uint <ImportCallSite>g__ReadCompressedUInt32|52_1(ref ReflectionHelper.<>c__DisplayClass52_1 A_0)
		{
			byte first = A_0.reader.ReadByte();
			if ((first & 128) == 0)
			{
				return (uint)first;
			}
			if ((first & 64) == 0)
			{
				return (((uint)first & 4294967167U) << 8) | (uint)A_0.reader.ReadByte();
			}
			return (uint)((((int)first & -193) << 24) | ((int)A_0.reader.ReadByte() << 16) | ((int)A_0.reader.ReadByte() << 8) | (int)A_0.reader.ReadByte());
		}

		// Token: 0x06002F37 RID: 12087 RVA: 0x000A47BC File Offset: 0x000A29BC
		[CompilerGenerated]
		internal static int <ImportCallSite>g__ReadCompressedInt32|52_2(ref ReflectionHelper.<>c__DisplayClass52_1 A_0)
		{
			byte b = A_0.reader.ReadByte();
			A_0.reader.BaseStream.Seek(-1L, SeekOrigin.Current);
			uint num = ReflectionHelper.<ImportCallSite>g__ReadCompressedUInt32|52_1(ref A_0);
			int v = (int)num >> 1;
			if ((num & 1U) == 0U)
			{
				return v;
			}
			int num2 = (int)(b & 192);
			if (num2 == 0 || num2 == 64)
			{
				return v - 64;
			}
			if (num2 != 128)
			{
				return v - 268435456;
			}
			return v - 8192;
		}

		// Token: 0x06002F38 RID: 12088 RVA: 0x000A4828 File Offset: 0x000A2A28
		[CompilerGenerated]
		internal static TypeReference <ImportCallSite>g__GetTypeDefOrRef|52_3(ref ReflectionHelper.<>c__DisplayClass52_0 A_0, ref ReflectionHelper.<>c__DisplayClass52_1 A_1)
		{
			uint num = ReflectionHelper.<ImportCallSite>g__ReadCompressedUInt32|52_1(ref A_1);
			uint rid = num >> 2;
			uint token;
			switch (num & 3U)
			{
			case 0U:
				token = 33554432U | rid;
				break;
			case 1U:
				token = 16777216U | rid;
				break;
			case 2U:
				token = 452984832U | rid;
				break;
			default:
				token = 0U;
				break;
			}
			return A_0.moduleTo.ImportReference(A_0.moduleFrom.ResolveType((int)token));
		}

		// Token: 0x06002F39 RID: 12089 RVA: 0x000A4890 File Offset: 0x000A2A90
		[CompilerGenerated]
		internal static TypeReference <ImportCallSite>g__ReadTypeSignature|52_4(ref ReflectionHelper.<>c__DisplayClass52_0 A_0, ref ReflectionHelper.<>c__DisplayClass52_1 A_1)
		{
			MetadataType etype = (MetadataType)A_1.reader.ReadByte();
			switch (etype)
			{
			case MetadataType.Void:
				return A_0.moduleTo.TypeSystem.Void;
			case MetadataType.Boolean:
				return A_0.moduleTo.TypeSystem.Boolean;
			case MetadataType.Char:
				return A_0.moduleTo.TypeSystem.Char;
			case MetadataType.SByte:
				return A_0.moduleTo.TypeSystem.SByte;
			case MetadataType.Byte:
				return A_0.moduleTo.TypeSystem.Byte;
			case MetadataType.Int16:
				return A_0.moduleTo.TypeSystem.Int16;
			case MetadataType.UInt16:
				return A_0.moduleTo.TypeSystem.UInt16;
			case MetadataType.Int32:
				return A_0.moduleTo.TypeSystem.Int32;
			case MetadataType.UInt32:
				return A_0.moduleTo.TypeSystem.UInt32;
			case MetadataType.Int64:
				return A_0.moduleTo.TypeSystem.Int64;
			case MetadataType.UInt64:
				return A_0.moduleTo.TypeSystem.UInt64;
			case MetadataType.Single:
				return A_0.moduleTo.TypeSystem.Single;
			case MetadataType.Double:
				return A_0.moduleTo.TypeSystem.Double;
			case MetadataType.String:
				return A_0.moduleTo.TypeSystem.String;
			case MetadataType.Pointer:
				return new PointerType(ReflectionHelper.<ImportCallSite>g__ReadTypeSignature|52_4(ref A_0, ref A_1));
			case MetadataType.ByReference:
				return new ByReferenceType(ReflectionHelper.<ImportCallSite>g__ReadTypeSignature|52_4(ref A_0, ref A_1));
			case MetadataType.ValueType:
			case MetadataType.Class:
				return ReflectionHelper.<ImportCallSite>g__GetTypeDefOrRef|52_3(ref A_0, ref A_1);
			case MetadataType.Var:
			case MetadataType.GenericInstance:
			case MetadataType.MVar:
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(38, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Unsupported generic callsite element: ");
				defaultInterpolatedStringHandler.AppendFormatted<MetadataType>(etype);
				throw new NotSupportedException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			case MetadataType.Array:
			{
				ArrayType array = new ArrayType(ReflectionHelper.<ImportCallSite>g__ReadTypeSignature|52_4(ref A_0, ref A_1));
				uint rank = ReflectionHelper.<ImportCallSite>g__ReadCompressedUInt32|52_1(ref A_1);
				uint[] sizes = new uint[ReflectionHelper.<ImportCallSite>g__ReadCompressedUInt32|52_1(ref A_1)];
				for (int i = 0; i < sizes.Length; i++)
				{
					sizes[i] = ReflectionHelper.<ImportCallSite>g__ReadCompressedUInt32|52_1(ref A_1);
				}
				int[] lowBounds = new int[ReflectionHelper.<ImportCallSite>g__ReadCompressedUInt32|52_1(ref A_1)];
				for (int j = 0; j < lowBounds.Length; j++)
				{
					lowBounds[j] = ReflectionHelper.<ImportCallSite>g__ReadCompressedInt32|52_2(ref A_1);
				}
				array.Dimensions.Clear();
				int k = 0;
				while ((long)k < (long)((ulong)rank))
				{
					int? lower = null;
					int? upper = null;
					if (k < lowBounds.Length)
					{
						lower = new int?(lowBounds[k]);
					}
					if (k < sizes.Length)
					{
						int? num = lower;
						int num2 = (int)sizes[k];
						upper = ((num != null) ? new int?(num.GetValueOrDefault() + num2 - 1) : null);
					}
					array.Dimensions.Add(new ArrayDimension(lower, upper));
					k++;
				}
				return array;
			}
			case MetadataType.TypedByReference:
				return A_0.moduleTo.TypeSystem.TypedReference;
			case (MetadataType)23:
			case (MetadataType)26:
				break;
			case MetadataType.IntPtr:
				return A_0.moduleTo.TypeSystem.IntPtr;
			case MetadataType.UIntPtr:
				return A_0.moduleTo.TypeSystem.UIntPtr;
			case MetadataType.FunctionPointer:
			{
				FunctionPointerType functionPointerType = new FunctionPointerType();
				ReflectionHelper.<ImportCallSite>g__ReadMethodSignature|52_0(functionPointerType, ref A_0, ref A_1);
				return functionPointerType;
			}
			case MetadataType.Object:
				return A_0.moduleTo.TypeSystem.Object;
			case (MetadataType)29:
				return new ArrayType(ReflectionHelper.<ImportCallSite>g__ReadTypeSignature|52_4(ref A_0, ref A_1));
			case MetadataType.RequiredModifier:
				return new RequiredModifierType(ReflectionHelper.<ImportCallSite>g__GetTypeDefOrRef|52_3(ref A_0, ref A_1), ReflectionHelper.<ImportCallSite>g__ReadTypeSignature|52_4(ref A_0, ref A_1));
			case MetadataType.OptionalModifier:
				return new OptionalModifierType(ReflectionHelper.<ImportCallSite>g__GetTypeDefOrRef|52_3(ref A_0, ref A_1), ReflectionHelper.<ImportCallSite>g__ReadTypeSignature|52_4(ref A_0, ref A_1));
			default:
				if (etype == MetadataType.Sentinel)
				{
					return new SentinelType(ReflectionHelper.<ImportCallSite>g__ReadTypeSignature|52_4(ref A_0, ref A_1));
				}
				if (etype == MetadataType.Pinned)
				{
					return new PinnedType(ReflectionHelper.<ImportCallSite>g__ReadTypeSignature|52_4(ref A_0, ref A_1));
				}
				break;
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(30, 1);
			defaultInterpolatedStringHandler2.AppendLiteral("Unsupported callsite element: ");
			defaultInterpolatedStringHandler2.AppendFormatted<MetadataType>(etype);
			throw new NotSupportedException(defaultInterpolatedStringHandler2.ToStringAndClear());
		}

		// Token: 0x04003B6D RID: 15213
		internal static readonly bool IsCoreBCL = typeof(object).Assembly.GetName().Name == "System.Private.CoreLib";

		// Token: 0x04003B6E RID: 15214
		internal static readonly Dictionary<string, WeakReference> AssemblyCache = new Dictionary<string, WeakReference>();

		// Token: 0x04003B6F RID: 15215
		internal static readonly Dictionary<string, WeakReference[]> AssembliesCache = new Dictionary<string, WeakReference[]>();

		// Token: 0x04003B70 RID: 15216
		internal static readonly Dictionary<string, WeakReference> ResolveReflectionCache = new Dictionary<string, WeakReference>();

		// Token: 0x04003B71 RID: 15217
		public static readonly byte[] AssemblyHashPrefix = new UTF8Encoding(false).GetBytes("MonoModRefl").Concat(new byte[1]).ToArray<byte>();

		// Token: 0x04003B72 RID: 15218
		public static readonly string AssemblyHashNameTag = "@#";

		// Token: 0x04003B73 RID: 15219
		private const BindingFlags _BindingFlagsAll = (BindingFlags)(-1);

		// Token: 0x04003B74 RID: 15220
		[Nullable(2)]
		private static readonly MethodInfo GetUnmanagedSigHelperMethod = typeof(SignatureHelper).GetMethod("GetMethodSigHelper", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[]
		{
			typeof(Module),
			typeof(CallingConvention),
			typeof(Type)
		}, null);

		// Token: 0x04003B75 RID: 15221
		private static readonly ReflectionHelper.GetUnmanagedSigHelperDelegate GetUnmanagedSigHelper;

		// Token: 0x04003B76 RID: 15222
		[Nullable(new byte[] { 1, 2 })]
		private static readonly object[] _CacheGetterArgs;

		// Token: 0x04003B77 RID: 15223
		[Nullable(2)]
		private static Type t_RuntimeType;

		// Token: 0x04003B78 RID: 15224
		[Nullable(2)]
		private static Type t_RuntimeTypeCache;

		// Token: 0x04003B79 RID: 15225
		[Nullable(2)]
		private static PropertyInfo p_RuntimeType_Cache;

		// Token: 0x04003B7A RID: 15226
		[Nullable(2)]
		private static MethodInfo m_RuntimeTypeCache_GetFieldList;

		// Token: 0x04003B7B RID: 15227
		[Nullable(2)]
		private static MethodInfo m_RuntimeTypeCache_GetPropertyList;

		// Token: 0x04003B7C RID: 15228
		private static readonly ConditionalWeakTable<Type, ReflectionHelper.CacheFixEntry> _CacheFixed;

		// Token: 0x04003B7D RID: 15229
		[Nullable(2)]
		private static Type t_RuntimeModule;

		// Token: 0x04003B7E RID: 15230
		[Nullable(2)]
		private static PropertyInfo p_RuntimeModule_RuntimeType;

		// Token: 0x04003B7F RID: 15231
		[Nullable(2)]
		private static FieldInfo f_RuntimeModule__impl;

		// Token: 0x04003B80 RID: 15232
		[Nullable(2)]
		private static MethodInfo m_RuntimeModule_GetGlobalType;

		// Token: 0x04003B81 RID: 15233
		[Nullable(2)]
		private static readonly FieldInfo f_SignatureHelper_module;

		// Token: 0x020008DA RID: 2266
		// (Invoke) Token: 0x06002F3B RID: 12091
		[NullableContext(0)]
		[return: Nullable(1)]
		private delegate SignatureHelper GetUnmanagedSigHelperDelegate(Module module, CallingConvention callConv, Type returnType);

		// Token: 0x020008DB RID: 2267
		[NullableContext(2)]
		[Nullable(0)]
		private class CacheFixEntry
		{
			// Token: 0x04003B82 RID: 15234
			public object Cache;

			// Token: 0x04003B83 RID: 15235
			public Array Properties;

			// Token: 0x04003B84 RID: 15236
			public Array Fields;

			// Token: 0x04003B85 RID: 15237
			public bool NeedsVerify;
		}
	}
}
