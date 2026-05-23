using System;
using Mono.Cecil.Metadata;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000259 RID: 601
	internal class DefaultMetadataImporter : IMetadataImporter
	{
		// Token: 0x06000D91 RID: 3473 RVA: 0x0002CEB9 File Offset: 0x0002B0B9
		public DefaultMetadataImporter(ModuleDefinition module)
		{
			Mixin.CheckModule(module);
			this.module = module;
		}

		// Token: 0x06000D92 RID: 3474 RVA: 0x0002CED0 File Offset: 0x0002B0D0
		private TypeReference ImportType(TypeReference type, ImportGenericContext context)
		{
			if (type.IsTypeSpecification())
			{
				return this.ImportTypeSpecification(type, context);
			}
			TypeReference reference = new TypeReference(type.Namespace, type.Name, this.module, this.ImportScope(type), type.IsValueType);
			MetadataSystem.TryProcessPrimitiveTypeReference(reference);
			if (type.IsNested)
			{
				reference.DeclaringType = this.ImportType(type.DeclaringType, context);
			}
			if (type.HasGenericParameters)
			{
				DefaultMetadataImporter.ImportGenericParameters(reference, type);
			}
			return reference;
		}

		// Token: 0x06000D93 RID: 3475 RVA: 0x0002CF44 File Offset: 0x0002B144
		protected virtual IMetadataScope ImportScope(TypeReference type)
		{
			return this.ImportScope(type.Scope);
		}

		// Token: 0x06000D94 RID: 3476 RVA: 0x0002CF54 File Offset: 0x0002B154
		protected IMetadataScope ImportScope(IMetadataScope scope)
		{
			switch (scope.MetadataScopeType)
			{
			case MetadataScopeType.AssemblyNameReference:
				return this.ImportReference((AssemblyNameReference)scope);
			case MetadataScopeType.ModuleReference:
				throw new NotImplementedException();
			case MetadataScopeType.ModuleDefinition:
				if (scope == this.module)
				{
					return scope;
				}
				return this.ImportReference(((ModuleDefinition)scope).Assembly.Name);
			default:
				throw new NotSupportedException();
			}
		}

		// Token: 0x06000D95 RID: 3477 RVA: 0x0002CFB8 File Offset: 0x0002B1B8
		public virtual AssemblyNameReference ImportReference(AssemblyNameReference name)
		{
			Mixin.CheckName(name);
			AssemblyNameReference reference;
			if (this.module.TryGetAssemblyNameReference(name, out reference))
			{
				return reference;
			}
			reference = new AssemblyNameReference(name.Name, name.Version)
			{
				Culture = name.Culture,
				HashAlgorithm = name.HashAlgorithm,
				IsRetargetable = name.IsRetargetable,
				IsWindowsRuntime = name.IsWindowsRuntime
			};
			byte[] pk_token = ((!name.PublicKeyToken.IsNullOrEmpty<byte>()) ? new byte[name.PublicKeyToken.Length] : Empty<byte>.Array);
			if (pk_token.Length != 0)
			{
				Buffer.BlockCopy(name.PublicKeyToken, 0, pk_token, 0, pk_token.Length);
			}
			reference.PublicKeyToken = pk_token;
			this.module.AssemblyReferences.Add(reference);
			return reference;
		}

		// Token: 0x06000D96 RID: 3478 RVA: 0x0002D070 File Offset: 0x0002B270
		private static void ImportGenericParameters(IGenericParameterProvider imported, IGenericParameterProvider original)
		{
			Collection<GenericParameter> parameters = original.GenericParameters;
			Collection<GenericParameter> imported_parameters = imported.GenericParameters;
			for (int i = 0; i < parameters.Count; i++)
			{
				imported_parameters.Add(new GenericParameter(parameters[i].Name, imported));
			}
		}

		// Token: 0x06000D97 RID: 3479 RVA: 0x0002D0B4 File Offset: 0x0002B2B4
		private TypeReference ImportTypeSpecification(TypeReference type, ImportGenericContext context)
		{
			ElementType etype = type.etype;
			switch (etype)
			{
			case ElementType.Ptr:
			{
				PointerType pointer = (PointerType)type;
				return new PointerType(this.ImportType(pointer.ElementType, context));
			}
			case ElementType.ByRef:
			{
				ByReferenceType byref = (ByReferenceType)type;
				return new ByReferenceType(this.ImportType(byref.ElementType, context));
			}
			case ElementType.ValueType:
			case ElementType.Class:
			case ElementType.TypedByRef:
			case (ElementType)23:
			case ElementType.I:
			case ElementType.U:
			case (ElementType)26:
			case ElementType.Object:
				break;
			case ElementType.Var:
			{
				GenericParameter var_parameter = (GenericParameter)type;
				if (var_parameter.DeclaringType == null)
				{
					throw new InvalidOperationException();
				}
				return context.TypeParameter(var_parameter.DeclaringType.FullName, var_parameter.Position);
			}
			case ElementType.Array:
			{
				ArrayType array = (ArrayType)type;
				ArrayType imported_array = new ArrayType(this.ImportType(array.ElementType, context));
				if (array.IsVector)
				{
					return imported_array;
				}
				Collection<ArrayDimension> dimensions = array.Dimensions;
				Collection<ArrayDimension> imported_dimensions = imported_array.Dimensions;
				imported_dimensions.Clear();
				for (int i = 0; i < dimensions.Count; i++)
				{
					ArrayDimension dimension = dimensions[i];
					imported_dimensions.Add(new ArrayDimension(dimension.LowerBound, dimension.UpperBound));
				}
				return imported_array;
			}
			case ElementType.GenericInst:
			{
				GenericInstanceType instance = (GenericInstanceType)type;
				TypeReference type2 = this.ImportType(instance.ElementType, context);
				Collection<TypeReference> arguments = instance.GenericArguments;
				GenericInstanceType imported_instance = new GenericInstanceType(type2, arguments.Count);
				Collection<TypeReference> imported_arguments = imported_instance.GenericArguments;
				for (int j = 0; j < arguments.Count; j++)
				{
					imported_arguments.Add(this.ImportType(arguments[j], context));
				}
				return imported_instance;
			}
			case ElementType.FnPtr:
			{
				FunctionPointerType fnptr = (FunctionPointerType)type;
				FunctionPointerType imported_fnptr = new FunctionPointerType
				{
					HasThis = fnptr.HasThis,
					ExplicitThis = fnptr.ExplicitThis,
					CallingConvention = fnptr.CallingConvention,
					ReturnType = this.ImportType(fnptr.ReturnType, context)
				};
				if (!fnptr.HasParameters)
				{
					return imported_fnptr;
				}
				for (int k = 0; k < fnptr.Parameters.Count; k++)
				{
					imported_fnptr.Parameters.Add(new ParameterDefinition(this.ImportType(fnptr.Parameters[k].ParameterType, context)));
				}
				return imported_fnptr;
			}
			case ElementType.SzArray:
			{
				ArrayType vector = (ArrayType)type;
				return new ArrayType(this.ImportType(vector.ElementType, context));
			}
			case ElementType.MVar:
			{
				GenericParameter mvar_parameter = (GenericParameter)type;
				if (mvar_parameter.DeclaringMethod == null)
				{
					throw new InvalidOperationException();
				}
				return context.MethodParameter(context.NormalizeMethodName(mvar_parameter.DeclaringMethod), mvar_parameter.Position);
			}
			case ElementType.CModReqD:
			{
				RequiredModifierType modreq = (RequiredModifierType)type;
				return new RequiredModifierType(this.ImportType(modreq.ModifierType, context), this.ImportType(modreq.ElementType, context));
			}
			case ElementType.CModOpt:
			{
				OptionalModifierType modopt = (OptionalModifierType)type;
				return new OptionalModifierType(this.ImportType(modopt.ModifierType, context), this.ImportType(modopt.ElementType, context));
			}
			default:
				if (etype == ElementType.Sentinel)
				{
					SentinelType sentinel = (SentinelType)type;
					return new SentinelType(this.ImportType(sentinel.ElementType, context));
				}
				if (etype == ElementType.Pinned)
				{
					PinnedType pinned = (PinnedType)type;
					return new PinnedType(this.ImportType(pinned.ElementType, context));
				}
				break;
			}
			throw new NotSupportedException(type.etype.ToString());
		}

		// Token: 0x06000D98 RID: 3480 RVA: 0x0002D408 File Offset: 0x0002B608
		private FieldReference ImportField(FieldReference field, ImportGenericContext context)
		{
			TypeReference declaring_type = this.ImportType(field.DeclaringType, context);
			context.Push(declaring_type);
			FieldReference result;
			try
			{
				result = new FieldReference
				{
					Name = field.Name,
					DeclaringType = declaring_type,
					FieldType = this.ImportType(field.FieldType, context)
				};
			}
			finally
			{
				context.Pop();
			}
			return result;
		}

		// Token: 0x06000D99 RID: 3481 RVA: 0x0002D474 File Offset: 0x0002B674
		private MethodReference ImportMethod(MethodReference method, ImportGenericContext context)
		{
			if (method.IsGenericInstance)
			{
				return this.ImportMethodSpecification(method, context);
			}
			TypeReference declaring_type = this.ImportType(method.DeclaringType, context);
			MethodReference reference = new MethodReference
			{
				Name = method.Name,
				HasThis = method.HasThis,
				ExplicitThis = method.ExplicitThis,
				DeclaringType = declaring_type,
				CallingConvention = method.CallingConvention
			};
			if (method.HasGenericParameters)
			{
				DefaultMetadataImporter.ImportGenericParameters(reference, method);
			}
			context.Push(reference);
			MethodReference result;
			try
			{
				reference.ReturnType = this.ImportType(method.ReturnType, context);
				if (!method.HasParameters)
				{
					result = reference;
				}
				else
				{
					Collection<ParameterDefinition> parameters = method.Parameters;
					ParameterDefinitionCollection reference_parameters = (reference.parameters = new ParameterDefinitionCollection(reference, parameters.Count));
					for (int i = 0; i < parameters.Count; i++)
					{
						reference_parameters.Add(new ParameterDefinition(this.ImportType(parameters[i].ParameterType, context)));
					}
					result = reference;
				}
			}
			finally
			{
				context.Pop();
			}
			return result;
		}

		// Token: 0x06000D9A RID: 3482 RVA: 0x0002D588 File Offset: 0x0002B788
		private MethodSpecification ImportMethodSpecification(MethodReference method, ImportGenericContext context)
		{
			if (!method.IsGenericInstance)
			{
				throw new NotSupportedException();
			}
			GenericInstanceMethod instance = (GenericInstanceMethod)method;
			GenericInstanceMethod imported_instance = new GenericInstanceMethod(this.ImportMethod(instance.ElementMethod, context));
			Collection<TypeReference> arguments = instance.GenericArguments;
			Collection<TypeReference> imported_arguments = imported_instance.GenericArguments;
			for (int i = 0; i < arguments.Count; i++)
			{
				imported_arguments.Add(this.ImportType(arguments[i], context));
			}
			return imported_instance;
		}

		// Token: 0x06000D9B RID: 3483 RVA: 0x0002D5F6 File Offset: 0x0002B7F6
		public virtual TypeReference ImportReference(TypeReference type, IGenericParameterProvider context)
		{
			Mixin.CheckType(type);
			return this.ImportType(type, ImportGenericContext.For(context));
		}

		// Token: 0x06000D9C RID: 3484 RVA: 0x0002D60B File Offset: 0x0002B80B
		public virtual FieldReference ImportReference(FieldReference field, IGenericParameterProvider context)
		{
			Mixin.CheckField(field);
			return this.ImportField(field, ImportGenericContext.For(context));
		}

		// Token: 0x06000D9D RID: 3485 RVA: 0x0002D620 File Offset: 0x0002B820
		public virtual MethodReference ImportReference(MethodReference method, IGenericParameterProvider context)
		{
			Mixin.CheckMethod(method);
			return this.ImportMethod(method, ImportGenericContext.For(context));
		}

		// Token: 0x04000402 RID: 1026
		protected readonly ModuleDefinition module;
	}
}
