using System;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil.Metadata;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000257 RID: 599
	internal class DefaultReflectionImporter : IReflectionImporter
	{
		// Token: 0x06000D72 RID: 3442 RVA: 0x0002C594 File Offset: 0x0002A794
		public DefaultReflectionImporter(ModuleDefinition module)
		{
			Mixin.CheckModule(module);
			this.module = module;
		}

		// Token: 0x06000D73 RID: 3443 RVA: 0x0002C5AC File Offset: 0x0002A7AC
		private TypeReference ImportType(Type type, ImportGenericContext context, Type[] required_modifiers, Type[] optional_modifiers)
		{
			TypeReference import = this.ImportType(type, context);
			foreach (Type modifier in required_modifiers)
			{
				import = new RequiredModifierType(this.ImportType(modifier, context), import);
			}
			foreach (Type modifier2 in optional_modifiers)
			{
				import = new OptionalModifierType(this.ImportType(modifier2, context), import);
			}
			return import;
		}

		// Token: 0x06000D74 RID: 3444 RVA: 0x0002C60C File Offset: 0x0002A80C
		private TypeReference ImportType(Type type, ImportGenericContext context)
		{
			return this.ImportType(type, context, DefaultReflectionImporter.ImportGenericKind.Open);
		}

		// Token: 0x06000D75 RID: 3445 RVA: 0x0002C618 File Offset: 0x0002A818
		private TypeReference ImportType(Type type, ImportGenericContext context, DefaultReflectionImporter.ImportGenericKind import_kind)
		{
			if (DefaultReflectionImporter.IsTypeSpecification(type) || DefaultReflectionImporter.ImportOpenGenericType(type, import_kind))
			{
				return this.ImportTypeSpecification(type, context);
			}
			TypeReference reference = new TypeReference(string.Empty, type.Name, this.module, this.ImportScope(type), type.IsValueType);
			reference.etype = DefaultReflectionImporter.ImportElementType(type);
			if (DefaultReflectionImporter.IsNestedType(type))
			{
				reference.DeclaringType = this.ImportType(type.DeclaringType, context, import_kind);
			}
			else
			{
				reference.Namespace = type.Namespace ?? string.Empty;
			}
			if (type.IsGenericType)
			{
				DefaultReflectionImporter.ImportGenericParameters(reference, type.GetGenericArguments());
			}
			return reference;
		}

		// Token: 0x06000D76 RID: 3446 RVA: 0x0002C6B7 File Offset: 0x0002A8B7
		protected virtual IMetadataScope ImportScope(Type type)
		{
			return this.ImportScope(type.Assembly);
		}

		// Token: 0x06000D77 RID: 3447 RVA: 0x0002C6C5 File Offset: 0x0002A8C5
		private static bool ImportOpenGenericType(Type type, DefaultReflectionImporter.ImportGenericKind import_kind)
		{
			return type.IsGenericType && type.IsGenericTypeDefinition && import_kind == DefaultReflectionImporter.ImportGenericKind.Open;
		}

		// Token: 0x06000D78 RID: 3448 RVA: 0x0002C6DD File Offset: 0x0002A8DD
		private static bool ImportOpenGenericMethod(MethodBase method, DefaultReflectionImporter.ImportGenericKind import_kind)
		{
			return method.IsGenericMethod && method.IsGenericMethodDefinition && import_kind == DefaultReflectionImporter.ImportGenericKind.Open;
		}

		// Token: 0x06000D79 RID: 3449 RVA: 0x0002C6F5 File Offset: 0x0002A8F5
		private static bool IsNestedType(Type type)
		{
			return type.IsNested;
		}

		// Token: 0x06000D7A RID: 3450 RVA: 0x0002C700 File Offset: 0x0002A900
		private TypeReference ImportTypeSpecification(Type type, ImportGenericContext context)
		{
			if (type.IsByRef)
			{
				return new ByReferenceType(this.ImportType(type.GetElementType(), context));
			}
			if (type.IsPointer)
			{
				return new PointerType(this.ImportType(type.GetElementType(), context));
			}
			if (type.IsArray)
			{
				return new ArrayType(this.ImportType(type.GetElementType(), context), type.GetArrayRank());
			}
			if (type.IsGenericType)
			{
				return this.ImportGenericInstance(type, context);
			}
			if (type.IsGenericParameter)
			{
				return DefaultReflectionImporter.ImportGenericParameter(type, context);
			}
			throw new NotSupportedException(type.FullName);
		}

		// Token: 0x06000D7B RID: 3451 RVA: 0x0002C790 File Offset: 0x0002A990
		private static TypeReference ImportGenericParameter(Type type, ImportGenericContext context)
		{
			if (context.IsEmpty)
			{
				throw new InvalidOperationException();
			}
			if (type.DeclaringMethod != null)
			{
				return context.MethodParameter(DefaultReflectionImporter.NormalizeMethodName(type.DeclaringMethod), type.GenericParameterPosition);
			}
			if (type.DeclaringType != null)
			{
				return context.TypeParameter(DefaultReflectionImporter.NormalizeTypeFullName(type.DeclaringType), type.GenericParameterPosition);
			}
			throw new InvalidOperationException();
		}

		// Token: 0x06000D7C RID: 3452 RVA: 0x0002C7FF File Offset: 0x0002A9FF
		private static string NormalizeMethodName(MethodBase method)
		{
			return DefaultReflectionImporter.NormalizeTypeFullName(method.DeclaringType) + "." + method.Name;
		}

		// Token: 0x06000D7D RID: 3453 RVA: 0x0002C81C File Offset: 0x0002AA1C
		private static string NormalizeTypeFullName(Type type)
		{
			if (DefaultReflectionImporter.IsNestedType(type))
			{
				return DefaultReflectionImporter.NormalizeTypeFullName(type.DeclaringType) + "/" + type.Name;
			}
			return type.FullName;
		}

		// Token: 0x06000D7E RID: 3454 RVA: 0x0002C848 File Offset: 0x0002AA48
		private TypeReference ImportGenericInstance(Type type, ImportGenericContext context)
		{
			TypeReference element_type = this.ImportType(type.GetGenericTypeDefinition(), context, DefaultReflectionImporter.ImportGenericKind.Definition);
			Type[] arguments = type.GetGenericArguments();
			GenericInstanceType instance = new GenericInstanceType(element_type, arguments.Length);
			Collection<TypeReference> instance_arguments = instance.GenericArguments;
			context.Push(element_type);
			TypeReference result;
			try
			{
				for (int i = 0; i < arguments.Length; i++)
				{
					instance_arguments.Add(this.ImportType(arguments[i], context));
				}
				result = instance;
			}
			finally
			{
				context.Pop();
			}
			return result;
		}

		// Token: 0x06000D7F RID: 3455 RVA: 0x0002C8C8 File Offset: 0x0002AAC8
		private static bool IsTypeSpecification(Type type)
		{
			return type.HasElementType || DefaultReflectionImporter.IsGenericInstance(type) || type.IsGenericParameter;
		}

		// Token: 0x06000D80 RID: 3456 RVA: 0x0002C8E2 File Offset: 0x0002AAE2
		private static bool IsGenericInstance(Type type)
		{
			return type.IsGenericType && !type.IsGenericTypeDefinition;
		}

		// Token: 0x06000D81 RID: 3457 RVA: 0x0002C8F8 File Offset: 0x0002AAF8
		private static ElementType ImportElementType(Type type)
		{
			ElementType etype;
			if (!DefaultReflectionImporter.type_etype_mapping.TryGetValue(type, out etype))
			{
				return ElementType.None;
			}
			return etype;
		}

		// Token: 0x06000D82 RID: 3458 RVA: 0x0002C917 File Offset: 0x0002AB17
		protected AssemblyNameReference ImportScope(Assembly assembly)
		{
			return this.ImportReference(assembly.GetName());
		}

		// Token: 0x06000D83 RID: 3459 RVA: 0x0002C928 File Offset: 0x0002AB28
		public virtual AssemblyNameReference ImportReference(AssemblyName name)
		{
			Mixin.CheckName(name);
			AssemblyNameReference reference;
			if (this.TryGetAssemblyNameReference(name, out reference))
			{
				return reference;
			}
			reference = new AssemblyNameReference(name.Name, name.Version)
			{
				PublicKeyToken = name.GetPublicKeyToken(),
				Culture = name.CultureInfo.Name,
				HashAlgorithm = (AssemblyHashAlgorithm)name.HashAlgorithm
			};
			this.module.AssemblyReferences.Add(reference);
			return reference;
		}

		// Token: 0x06000D84 RID: 3460 RVA: 0x0002C998 File Offset: 0x0002AB98
		private bool TryGetAssemblyNameReference(AssemblyName name, out AssemblyNameReference assembly_reference)
		{
			Collection<AssemblyNameReference> references = this.module.AssemblyReferences;
			for (int i = 0; i < references.Count; i++)
			{
				AssemblyNameReference reference = references[i];
				if (!(name.FullName != reference.FullName))
				{
					assembly_reference = reference;
					return true;
				}
			}
			assembly_reference = null;
			return false;
		}

		// Token: 0x06000D85 RID: 3461 RVA: 0x0002C9E8 File Offset: 0x0002ABE8
		private FieldReference ImportField(FieldInfo field, ImportGenericContext context)
		{
			TypeReference declaring_type = this.ImportType(field.DeclaringType, context);
			if (DefaultReflectionImporter.IsGenericInstance(field.DeclaringType))
			{
				field = DefaultReflectionImporter.ResolveFieldDefinition(field);
			}
			context.Push(declaring_type);
			FieldReference result;
			try
			{
				result = new FieldReference
				{
					Name = field.Name,
					DeclaringType = declaring_type,
					FieldType = this.ImportType(field.FieldType, context, field.GetRequiredCustomModifiers(), field.GetOptionalCustomModifiers())
				};
			}
			finally
			{
				context.Pop();
			}
			return result;
		}

		// Token: 0x06000D86 RID: 3462 RVA: 0x0002CA74 File Offset: 0x0002AC74
		private static FieldInfo ResolveFieldDefinition(FieldInfo field)
		{
			return field.Module.ResolveField(field.MetadataToken);
		}

		// Token: 0x06000D87 RID: 3463 RVA: 0x0002CA87 File Offset: 0x0002AC87
		private static MethodBase ResolveMethodDefinition(MethodBase method)
		{
			return method.Module.ResolveMethod(method.MetadataToken);
		}

		// Token: 0x06000D88 RID: 3464 RVA: 0x0002CA9C File Offset: 0x0002AC9C
		private MethodReference ImportMethod(MethodBase method, ImportGenericContext context, DefaultReflectionImporter.ImportGenericKind import_kind)
		{
			if (DefaultReflectionImporter.IsMethodSpecification(method) || DefaultReflectionImporter.ImportOpenGenericMethod(method, import_kind))
			{
				return this.ImportMethodSpecification(method, context);
			}
			TypeReference declaring_type = this.ImportType(method.DeclaringType, context);
			if (DefaultReflectionImporter.IsGenericInstance(method.DeclaringType))
			{
				method = DefaultReflectionImporter.ResolveMethodDefinition(method);
			}
			MethodReference reference = new MethodReference
			{
				Name = method.Name,
				HasThis = DefaultReflectionImporter.HasCallingConvention(method, CallingConventions.HasThis),
				ExplicitThis = DefaultReflectionImporter.HasCallingConvention(method, CallingConventions.ExplicitThis),
				DeclaringType = this.ImportType(method.DeclaringType, context, DefaultReflectionImporter.ImportGenericKind.Definition)
			};
			if (DefaultReflectionImporter.HasCallingConvention(method, CallingConventions.VarArgs))
			{
				reference.CallingConvention &= MethodCallingConvention.VarArg;
			}
			if (method.IsGenericMethod)
			{
				DefaultReflectionImporter.ImportGenericParameters(reference, method.GetGenericArguments());
			}
			context.Push(reference);
			MethodReference result;
			try
			{
				MethodInfo method_info = method as MethodInfo;
				reference.ReturnType = ((method_info != null) ? this.ImportType(method_info.ReturnType, context, method_info.ReturnParameter.GetRequiredCustomModifiers(), method_info.ReturnParameter.GetOptionalCustomModifiers()) : this.ImportType(typeof(void), default(ImportGenericContext)));
				ParameterInfo[] parameters = method.GetParameters();
				Collection<ParameterDefinition> reference_parameters = reference.Parameters;
				foreach (ParameterInfo parameter in parameters)
				{
					reference_parameters.Add(new ParameterDefinition(this.ImportType(parameter.ParameterType, context, parameter.GetRequiredCustomModifiers(), parameter.GetOptionalCustomModifiers())));
				}
				reference.DeclaringType = declaring_type;
				result = reference;
			}
			finally
			{
				context.Pop();
			}
			return result;
		}

		// Token: 0x06000D89 RID: 3465 RVA: 0x0002CC28 File Offset: 0x0002AE28
		private static void ImportGenericParameters(IGenericParameterProvider provider, Type[] arguments)
		{
			Collection<GenericParameter> provider_parameters = provider.GenericParameters;
			for (int i = 0; i < arguments.Length; i++)
			{
				provider_parameters.Add(new GenericParameter(arguments[i].Name, provider));
			}
		}

		// Token: 0x06000D8A RID: 3466 RVA: 0x0002CC5E File Offset: 0x0002AE5E
		private static bool IsMethodSpecification(MethodBase method)
		{
			return method.IsGenericMethod && !method.IsGenericMethodDefinition;
		}

		// Token: 0x06000D8B RID: 3467 RVA: 0x0002CC74 File Offset: 0x0002AE74
		private MethodReference ImportMethodSpecification(MethodBase method, ImportGenericContext context)
		{
			MethodInfo method_info = method as MethodInfo;
			if (method_info == null)
			{
				throw new InvalidOperationException();
			}
			MethodReference element_method = this.ImportMethod(method_info.GetGenericMethodDefinition(), context, DefaultReflectionImporter.ImportGenericKind.Definition);
			GenericInstanceMethod instance = new GenericInstanceMethod(element_method);
			Type[] arguments = method.GetGenericArguments();
			Collection<TypeReference> instance_arguments = instance.GenericArguments;
			context.Push(element_method);
			MethodReference result;
			try
			{
				for (int i = 0; i < arguments.Length; i++)
				{
					instance_arguments.Add(this.ImportType(arguments[i], context));
				}
				result = instance;
			}
			finally
			{
				context.Pop();
			}
			return result;
		}

		// Token: 0x06000D8C RID: 3468 RVA: 0x0002CD08 File Offset: 0x0002AF08
		private static bool HasCallingConvention(MethodBase method, CallingConventions conventions)
		{
			return (method.CallingConvention & conventions) > (CallingConventions)0;
		}

		// Token: 0x06000D8D RID: 3469 RVA: 0x0002CD15 File Offset: 0x0002AF15
		public virtual TypeReference ImportReference(Type type, IGenericParameterProvider context)
		{
			Mixin.CheckType(type);
			return this.ImportType(type, ImportGenericContext.For(context), (context != null) ? DefaultReflectionImporter.ImportGenericKind.Open : DefaultReflectionImporter.ImportGenericKind.Definition);
		}

		// Token: 0x06000D8E RID: 3470 RVA: 0x0002CD31 File Offset: 0x0002AF31
		public virtual FieldReference ImportReference(FieldInfo field, IGenericParameterProvider context)
		{
			Mixin.CheckField(field);
			return this.ImportField(field, ImportGenericContext.For(context));
		}

		// Token: 0x06000D8F RID: 3471 RVA: 0x0002CD46 File Offset: 0x0002AF46
		public virtual MethodReference ImportReference(MethodBase method, IGenericParameterProvider context)
		{
			Mixin.CheckMethod(method);
			return this.ImportMethod(method, ImportGenericContext.For(context), (context != null) ? DefaultReflectionImporter.ImportGenericKind.Open : DefaultReflectionImporter.ImportGenericKind.Definition);
		}

		// Token: 0x040003FD RID: 1021
		protected readonly ModuleDefinition module;

		// Token: 0x040003FE RID: 1022
		private static readonly Dictionary<Type, ElementType> type_etype_mapping = new Dictionary<Type, ElementType>(18)
		{
			{
				typeof(void),
				ElementType.Void
			},
			{
				typeof(bool),
				ElementType.Boolean
			},
			{
				typeof(char),
				ElementType.Char
			},
			{
				typeof(sbyte),
				ElementType.I1
			},
			{
				typeof(byte),
				ElementType.U1
			},
			{
				typeof(short),
				ElementType.I2
			},
			{
				typeof(ushort),
				ElementType.U2
			},
			{
				typeof(int),
				ElementType.I4
			},
			{
				typeof(uint),
				ElementType.U4
			},
			{
				typeof(long),
				ElementType.I8
			},
			{
				typeof(ulong),
				ElementType.U8
			},
			{
				typeof(float),
				ElementType.R4
			},
			{
				typeof(double),
				ElementType.R8
			},
			{
				typeof(string),
				ElementType.String
			},
			{
				typeof(TypedReference),
				ElementType.TypedByRef
			},
			{
				typeof(IntPtr),
				ElementType.I
			},
			{
				typeof(UIntPtr),
				ElementType.U
			},
			{
				typeof(object),
				ElementType.Object
			}
		};

		// Token: 0x02000258 RID: 600
		private enum ImportGenericKind
		{
			// Token: 0x04000400 RID: 1024
			Definition,
			// Token: 0x04000401 RID: 1025
			Open
		}
	}
}
