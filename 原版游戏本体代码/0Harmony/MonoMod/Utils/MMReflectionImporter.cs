using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace MonoMod.Utils
{
	// Token: 0x020008D2 RID: 2258
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class MMReflectionImporter : IReflectionImporter
	{
		// Token: 0x17000852 RID: 2130
		// (get) Token: 0x06002EEB RID: 12011 RVA: 0x000A15A0 File Offset: 0x0009F7A0
		// (set) Token: 0x06002EEC RID: 12012 RVA: 0x000A15A8 File Offset: 0x0009F7A8
		public bool UseDefault { get; set; }

		// Token: 0x06002EED RID: 12013 RVA: 0x000A15B4 File Offset: 0x0009F7B4
		public MMReflectionImporter(ModuleDefinition module)
		{
			Helpers.ThrowIfArgumentNull<ModuleDefinition>(module, "module");
			this.Module = module;
			this.Default = new DefaultReflectionImporter(module);
			this.ElementTypes = new Dictionary<Type, TypeReference>
			{
				{
					typeof(void),
					module.TypeSystem.Void
				},
				{
					typeof(bool),
					module.TypeSystem.Boolean
				},
				{
					typeof(char),
					module.TypeSystem.Char
				},
				{
					typeof(sbyte),
					module.TypeSystem.SByte
				},
				{
					typeof(byte),
					module.TypeSystem.Byte
				},
				{
					typeof(short),
					module.TypeSystem.Int16
				},
				{
					typeof(ushort),
					module.TypeSystem.UInt16
				},
				{
					typeof(int),
					module.TypeSystem.Int32
				},
				{
					typeof(uint),
					module.TypeSystem.UInt32
				},
				{
					typeof(long),
					module.TypeSystem.Int64
				},
				{
					typeof(ulong),
					module.TypeSystem.UInt64
				},
				{
					typeof(float),
					module.TypeSystem.Single
				},
				{
					typeof(double),
					module.TypeSystem.Double
				},
				{
					typeof(string),
					module.TypeSystem.String
				},
				{
					typeof(TypedReference),
					module.TypeSystem.TypedReference
				},
				{
					typeof(IntPtr),
					module.TypeSystem.IntPtr
				},
				{
					typeof(UIntPtr),
					module.TypeSystem.UIntPtr
				},
				{
					typeof(object),
					module.TypeSystem.Object
				}
			};
		}

		// Token: 0x06002EEE RID: 12014 RVA: 0x000A180D File Offset: 0x0009FA0D
		private bool TryGetCachedType(Type type, [MaybeNullWhen(false)] out TypeReference typeRef, MMReflectionImporter.GenericImportKind importKind)
		{
			if (importKind == MMReflectionImporter.GenericImportKind.Definition)
			{
				typeRef = null;
				return false;
			}
			return this.CachedTypes.TryGetValue(type, out typeRef);
		}

		// Token: 0x06002EEF RID: 12015 RVA: 0x000A1828 File Offset: 0x0009FA28
		private TypeReference SetCachedType(Type type, TypeReference typeRef, MMReflectionImporter.GenericImportKind importKind)
		{
			if (importKind == MMReflectionImporter.GenericImportKind.Definition)
			{
				return typeRef;
			}
			this.CachedTypes[type] = typeRef;
			return typeRef;
		}

		// Token: 0x06002EF0 RID: 12016 RVA: 0x000A184B File Offset: 0x0009FA4B
		[Obsolete("Please use the Assembly overload instead.")]
		public AssemblyNameReference ImportReference(AssemblyName reference)
		{
			Helpers.ThrowIfArgumentNull<AssemblyName>(reference, "reference");
			return this.Default.ImportReference(reference);
		}

		// Token: 0x06002EF1 RID: 12017 RVA: 0x000A1864 File Offset: 0x0009FA64
		public AssemblyNameReference ImportReference(Assembly asm)
		{
			MMReflectionImporter.<>c__DisplayClass20_0 CS$<>8__locals1;
			CS$<>8__locals1.<>4__this = this;
			CS$<>8__locals1.asm = asm;
			Helpers.ThrowIfArgumentNull<Assembly>(CS$<>8__locals1.asm, "asm");
			AssemblyNameReference asmRef;
			if (this.CachedAsms.TryGetValue(CS$<>8__locals1.asm, out asmRef))
			{
				return asmRef;
			}
			asmRef = this.<ImportReference>g__ImportReference|20_1(CS$<>8__locals1.asm.GetName(), ref CS$<>8__locals1);
			asmRef.ApplyRuntimeHash(CS$<>8__locals1.asm);
			return this.CachedAsms[CS$<>8__locals1.asm] = asmRef;
		}

		// Token: 0x06002EF2 RID: 12018 RVA: 0x000A18E0 File Offset: 0x0009FAE0
		public TypeReference ImportModuleType(Module module, [Nullable(2)] IGenericParameterProvider context)
		{
			Helpers.ThrowIfArgumentNull<Module>(module, "module");
			TypeReference typeRef;
			if (this.CachedModuleTypes.TryGetValue(module, out typeRef))
			{
				return typeRef;
			}
			return this.CachedModuleTypes[module] = new TypeReference(string.Empty, "<Module>", this.Module, this.ImportReference(module.Assembly));
		}

		// Token: 0x06002EF3 RID: 12019 RVA: 0x000A193A File Offset: 0x0009FB3A
		public TypeReference ImportReference(Type type, [Nullable(2)] IGenericParameterProvider context)
		{
			Helpers.ThrowIfArgumentNull<Type>(type, "type");
			return this._ImportReference(type, context, (context != null) ? MMReflectionImporter.GenericImportKind.Open : MMReflectionImporter.GenericImportKind.Definition);
		}

		// Token: 0x06002EF4 RID: 12020 RVA: 0x000A1956 File Offset: 0x0009FB56
		private static bool _IsGenericInstance(Type type, MMReflectionImporter.GenericImportKind importKind)
		{
			Helpers.ThrowIfArgumentNull<Type>(type, "type");
			return (type.IsGenericType && !type.IsGenericTypeDefinition) || (type.IsGenericType && type.IsGenericTypeDefinition && importKind == MMReflectionImporter.GenericImportKind.Open);
		}

		// Token: 0x06002EF5 RID: 12021 RVA: 0x000A198C File Offset: 0x0009FB8C
		private GenericInstanceType _ImportGenericInstance(Type type, [Nullable(2)] IGenericParameterProvider context, TypeReference typeRef)
		{
			Helpers.ThrowIfArgumentNull<Type>(type, "type");
			Helpers.ThrowIfArgumentNull<TypeReference>(typeRef, "typeRef");
			GenericInstanceType git = new GenericInstanceType(typeRef);
			foreach (Type arg in type.GetGenericArguments())
			{
				git.GenericArguments.Add(this._ImportReference(arg, context, MMReflectionImporter.GenericImportKind.Open));
			}
			return git;
		}

		// Token: 0x06002EF6 RID: 12022 RVA: 0x000A19E4 File Offset: 0x0009FBE4
		private TypeReference _ImportReference(Type type, [Nullable(2)] IGenericParameterProvider context, MMReflectionImporter.GenericImportKind importKind = MMReflectionImporter.GenericImportKind.Open)
		{
			Helpers.ThrowIfArgumentNull<Type>(type, "type");
			TypeReference typeRef;
			if (this.TryGetCachedType(type, out typeRef, importKind))
			{
				if (!MMReflectionImporter._IsGenericInstance(type, importKind))
				{
					return typeRef;
				}
				return this._ImportGenericInstance(type, context, typeRef);
			}
			else
			{
				if (this.UseDefault)
				{
					return this.SetCachedType(type, this.Default.ImportReference(type, context), importKind);
				}
				if (type.HasElementType)
				{
					if (type.IsByRef)
					{
						return this.SetCachedType(type, new ByReferenceType(this._ImportReference(type.GetElementType(), context, MMReflectionImporter.GenericImportKind.Open)), importKind);
					}
					if (type.IsPointer)
					{
						return this.SetCachedType(type, new PointerType(this._ImportReference(type.GetElementType(), context, MMReflectionImporter.GenericImportKind.Open)), importKind);
					}
					if (type.IsArray)
					{
						ArrayType at = new ArrayType(this._ImportReference(type.GetElementType(), context, MMReflectionImporter.GenericImportKind.Open), type.GetArrayRank());
						if (type != type.GetElementType().MakeArrayType())
						{
							for (int i = 0; i < at.Rank; i++)
							{
								at.Dimensions[i] = new ArrayDimension(new int?(0), null);
							}
						}
						return this.CachedTypes[type] = at;
					}
				}
				if (MMReflectionImporter._IsGenericInstance(type, importKind))
				{
					return this._ImportGenericInstance(type, context, this._ImportReference(type.GetGenericTypeDefinition(), context, MMReflectionImporter.GenericImportKind.Definition));
				}
				if (type.IsGenericParameter)
				{
					return this.SetCachedType(type, MMReflectionImporter.ImportGenericParameter(type, context), importKind);
				}
				if (this.ElementTypes.TryGetValue(type, out typeRef))
				{
					return this.SetCachedType(type, typeRef, importKind);
				}
				typeRef = new TypeReference(string.Empty, type.Name, this.Module, this.ImportReference(type.Assembly), type.IsValueType);
				if (type.IsNested)
				{
					typeRef.DeclaringType = this._ImportReference(type.DeclaringType, context, importKind);
				}
				else if (type.Namespace != null)
				{
					typeRef.Namespace = type.Namespace;
				}
				if (type.IsGenericType)
				{
					foreach (Type param in type.GetGenericArguments())
					{
						typeRef.GenericParameters.Add(new GenericParameter(param.Name, typeRef));
					}
				}
				return this.SetCachedType(type, typeRef, importKind);
			}
		}

		// Token: 0x06002EF7 RID: 12023 RVA: 0x000A1C00 File Offset: 0x0009FE00
		private static GenericParameter ImportGenericParameter(Type type, [Nullable(2)] IGenericParameterProvider context)
		{
			Helpers.ThrowIfArgumentNull<Type>(type, "type");
			MethodReference ctxMethodRef = context as MethodReference;
			if (ctxMethodRef != null)
			{
				if (type.DeclaringMethod != null)
				{
					return ctxMethodRef.GenericParameters[type.GenericParameterPosition];
				}
				context = ctxMethodRef.DeclaringType;
			}
			Type declaringType = type.DeclaringType;
			if (declaringType == null)
			{
				throw new InvalidOperationException();
			}
			Type dclType = declaringType;
			TypeReference ctxTypeRef = context as TypeReference;
			if (ctxTypeRef != null)
			{
				while (ctxTypeRef != null)
				{
					TypeReference ctxTypeRefEl = ctxTypeRef.GetElementType();
					if (ctxTypeRefEl.Is(dclType))
					{
						return ctxTypeRefEl.GenericParameters[type.GenericParameterPosition];
					}
					if (ctxTypeRef.Is(dclType))
					{
						return ctxTypeRef.GenericParameters[type.GenericParameterPosition];
					}
					ctxTypeRef = ctxTypeRef.DeclaringType;
				}
			}
			throw new NotSupportedException();
		}

		// Token: 0x06002EF8 RID: 12024 RVA: 0x000A1CB4 File Offset: 0x0009FEB4
		public FieldReference ImportReference(FieldInfo field, [Nullable(2)] IGenericParameterProvider context)
		{
			Helpers.ThrowIfArgumentNull<FieldInfo>(field, "field");
			FieldReference fieldRef;
			if (this.CachedFields.TryGetValue(field, out fieldRef))
			{
				return fieldRef;
			}
			if (this.UseDefault)
			{
				return this.CachedFields[field] = this.Default.ImportReference(field, context);
			}
			Type declType = field.DeclaringType;
			TypeReference declaringType = ((declType != null) ? this.ImportReference(declType, context) : this.ImportModuleType(field.Module, context));
			FieldInfo fieldOrig = field;
			if (declType != null && declType.IsGenericType)
			{
				field = field.Module.ResolveField(field.MetadataToken);
			}
			TypeReference typeRef = this._ImportReference(field.FieldType, declaringType, MMReflectionImporter.GenericImportKind.Open);
			Type[] requiredCustomModifiers = field.GetRequiredCustomModifiers();
			Type[] modOpts = field.GetOptionalCustomModifiers();
			foreach (Type mod in requiredCustomModifiers)
			{
				typeRef = new RequiredModifierType(this._ImportReference(mod, declaringType, MMReflectionImporter.GenericImportKind.Open), typeRef);
			}
			foreach (Type mod2 in modOpts)
			{
				typeRef = new OptionalModifierType(this._ImportReference(mod2, declaringType, MMReflectionImporter.GenericImportKind.Open), typeRef);
			}
			return this.CachedFields[fieldOrig] = new FieldReference(field.Name, typeRef, declaringType);
		}

		// Token: 0x06002EF9 RID: 12025 RVA: 0x000A1DF0 File Offset: 0x0009FFF0
		public MethodReference ImportReference(MethodBase method, [Nullable(2)] IGenericParameterProvider context)
		{
			Helpers.ThrowIfArgumentNull<MethodBase>(method, "method");
			return this._ImportReference(method, context, (context != null) ? MMReflectionImporter.GenericImportKind.Open : MMReflectionImporter.GenericImportKind.Definition);
		}

		// Token: 0x06002EFA RID: 12026 RVA: 0x000A1E0C File Offset: 0x000A000C
		private MethodReference _ImportReference(MethodBase method, [Nullable(2)] IGenericParameterProvider context, MMReflectionImporter.GenericImportKind importKind)
		{
			Helpers.ThrowIfArgumentNull<MethodBase>(method, "method");
			MethodReference methodRef;
			if (this.CachedMethods.TryGetValue(method, out methodRef) && importKind == MMReflectionImporter.GenericImportKind.Open)
			{
				return methodRef;
			}
			MethodInfo target = method as MethodInfo;
			if (target != null && target.IsDynamicMethod())
			{
				return new DynamicMethodReference(this.Module, target);
			}
			if (this.UseDefault)
			{
				return this.CachedMethods[method] = this.Default.ImportReference(method, context);
			}
			if ((method.IsGenericMethod && !method.IsGenericMethodDefinition) || (method.IsGenericMethod && method.IsGenericMethodDefinition && importKind == MMReflectionImporter.GenericImportKind.Open))
			{
				GenericInstanceMethod gim = new GenericInstanceMethod(this._ImportReference(((MethodInfo)method).GetGenericMethodDefinition(), context, MMReflectionImporter.GenericImportKind.Definition));
				foreach (Type arg in method.GetGenericArguments())
				{
					gim.GenericArguments.Add(this._ImportReference(arg, context, MMReflectionImporter.GenericImportKind.Open));
				}
				return this.CachedMethods[method] = gim;
			}
			Type declType = method.DeclaringType;
			methodRef = new MethodReference(method.Name, this._ImportReference(typeof(void), context, MMReflectionImporter.GenericImportKind.Open), (declType != null) ? this._ImportReference(declType, context, MMReflectionImporter.GenericImportKind.Definition) : this.ImportModuleType(method.Module, context));
			methodRef.HasThis = (method.CallingConvention & CallingConventions.HasThis) > (CallingConventions)0;
			methodRef.ExplicitThis = (method.CallingConvention & CallingConventions.ExplicitThis) > (CallingConventions)0;
			if ((method.CallingConvention & CallingConventions.VarArgs) != (CallingConventions)0)
			{
				methodRef.CallingConvention = MethodCallingConvention.VarArg;
			}
			MethodBase methodOrig = method;
			if (declType != null && declType.IsGenericType)
			{
				method = method.Module.ResolveMethod(method.MetadataToken);
			}
			if (method.IsGenericMethodDefinition)
			{
				foreach (Type param in method.GetGenericArguments())
				{
					methodRef.GenericParameters.Add(new GenericParameter(param.Name, methodRef));
				}
			}
			MethodReference methodReference = methodRef;
			MethodInfo methodInfo = method as MethodInfo;
			methodReference.ReturnType = this._ImportReference(((methodInfo != null) ? methodInfo.ReturnType : null) ?? typeof(void), methodRef, MMReflectionImporter.GenericImportKind.Open);
			foreach (ParameterInfo param2 in method.GetParameters())
			{
				methodRef.Parameters.Add(new ParameterDefinition(param2.Name, (Mono.Cecil.ParameterAttributes)param2.Attributes, this._ImportReference(param2.ParameterType, methodRef, MMReflectionImporter.GenericImportKind.Open)));
			}
			return this.CachedMethods[methodOrig] = methodRef;
		}

		// Token: 0x06002EFC RID: 12028 RVA: 0x000A20A0 File Offset: 0x000A02A0
		[CompilerGenerated]
		private bool <ImportReference>g__TryGetAssemblyNameReference|20_0(AssemblyName name, [Nullable(2)] [NotNullWhen(true)] out AssemblyNameReference assembly_reference, ref MMReflectionImporter.<>c__DisplayClass20_0 A_3)
		{
			Collection<AssemblyNameReference> references = this.Module.AssemblyReferences;
			for (int i = 0; i < references.Count; i++)
			{
				AssemblyNameReference reference = references[i];
				if (!(name.FullName != reference.FullName) && reference.HashIs(A_3.asm, true))
				{
					assembly_reference = reference;
					return true;
				}
			}
			assembly_reference = null;
			return false;
		}

		// Token: 0x06002EFD RID: 12029 RVA: 0x000A2100 File Offset: 0x000A0300
		[CompilerGenerated]
		private AssemblyNameReference <ImportReference>g__ImportReference|20_1(AssemblyName name, ref MMReflectionImporter.<>c__DisplayClass20_0 A_2)
		{
			AssemblyNameReference reference;
			if (this.<ImportReference>g__TryGetAssemblyNameReference|20_0(name, out reference, ref A_2))
			{
				return reference;
			}
			reference = new AssemblyNameReference(name.Name, name.Version)
			{
				PublicKeyToken = name.GetPublicKeyToken(),
				Culture = name.CultureInfo.Name,
				HashAlgorithm = (AssemblyHashAlgorithm)name.HashAlgorithm
			};
			this.Module.AssemblyReferences.Add(reference);
			return reference;
		}

		// Token: 0x04003B4B RID: 15179
		public static readonly IReflectionImporterProvider Provider = new MMReflectionImporter._Provider();

		// Token: 0x04003B4C RID: 15180
		public static readonly IReflectionImporterProvider ProviderNoDefault = new MMReflectionImporter._Provider
		{
			UseDefault = new bool?(false)
		};

		// Token: 0x04003B4D RID: 15181
		private readonly ModuleDefinition Module;

		// Token: 0x04003B4E RID: 15182
		private readonly DefaultReflectionImporter Default;

		// Token: 0x04003B4F RID: 15183
		private readonly Dictionary<Assembly, AssemblyNameReference> CachedAsms = new Dictionary<Assembly, AssemblyNameReference>();

		// Token: 0x04003B50 RID: 15184
		private readonly Dictionary<Module, TypeReference> CachedModuleTypes = new Dictionary<Module, TypeReference>();

		// Token: 0x04003B51 RID: 15185
		private readonly Dictionary<Type, TypeReference> CachedTypes = new Dictionary<Type, TypeReference>();

		// Token: 0x04003B52 RID: 15186
		private readonly Dictionary<FieldInfo, FieldReference> CachedFields = new Dictionary<FieldInfo, FieldReference>();

		// Token: 0x04003B53 RID: 15187
		private readonly Dictionary<MethodBase, MethodReference> CachedMethods = new Dictionary<MethodBase, MethodReference>();

		// Token: 0x04003B55 RID: 15189
		private readonly Dictionary<Type, TypeReference> ElementTypes;

		// Token: 0x020008D3 RID: 2259
		[NullableContext(0)]
		private class _Provider : IReflectionImporterProvider
		{
			// Token: 0x06002EFE RID: 12030 RVA: 0x000A2168 File Offset: 0x000A0368
			[NullableContext(1)]
			public IReflectionImporter GetReflectionImporter(ModuleDefinition module)
			{
				Helpers.ThrowIfArgumentNull<ModuleDefinition>(module, "module");
				MMReflectionImporter importer = new MMReflectionImporter(module);
				if (this.UseDefault != null)
				{
					importer.UseDefault = this.UseDefault.Value;
				}
				return importer;
			}

			// Token: 0x04003B56 RID: 15190
			public bool? UseDefault;
		}

		// Token: 0x020008D4 RID: 2260
		[NullableContext(0)]
		private enum GenericImportKind
		{
			// Token: 0x04003B58 RID: 15192
			Open,
			// Token: 0x04003B59 RID: 15193
			Definition
		}
	}
}
