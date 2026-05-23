using System;
using Mono.Cecil.Cil;

namespace Mono.Cecil
{
	// Token: 0x020002AC RID: 684
	internal sealed class TypeResolver
	{
		// Token: 0x0600115E RID: 4446 RVA: 0x000344FF File Offset: 0x000326FF
		public static TypeResolver For(TypeReference typeReference)
		{
			if (!typeReference.IsGenericInstance)
			{
				return new TypeResolver();
			}
			return new TypeResolver((GenericInstanceType)typeReference);
		}

		// Token: 0x0600115F RID: 4447 RVA: 0x0003451A File Offset: 0x0003271A
		public static TypeResolver For(TypeReference typeReference, MethodReference methodReference)
		{
			return new TypeResolver(typeReference as GenericInstanceType, methodReference as GenericInstanceMethod);
		}

		// Token: 0x06001160 RID: 4448 RVA: 0x00002B15 File Offset: 0x00000D15
		public TypeResolver()
		{
		}

		// Token: 0x06001161 RID: 4449 RVA: 0x0003452D File Offset: 0x0003272D
		public TypeResolver(GenericInstanceType typeDefinitionContext)
		{
			this._typeDefinitionContext = typeDefinitionContext;
		}

		// Token: 0x06001162 RID: 4450 RVA: 0x0003453C File Offset: 0x0003273C
		public TypeResolver(GenericInstanceMethod methodDefinitionContext)
		{
			this._methodDefinitionContext = methodDefinitionContext;
		}

		// Token: 0x06001163 RID: 4451 RVA: 0x0003454B File Offset: 0x0003274B
		public TypeResolver(GenericInstanceType typeDefinitionContext, GenericInstanceMethod methodDefinitionContext)
		{
			this._typeDefinitionContext = typeDefinitionContext;
			this._methodDefinitionContext = methodDefinitionContext;
		}

		// Token: 0x06001164 RID: 4452 RVA: 0x00034564 File Offset: 0x00032764
		public MethodReference Resolve(MethodReference method)
		{
			MethodReference methodReference = method;
			if (this.IsDummy())
			{
				return methodReference;
			}
			TypeReference declaringType = this.Resolve(method.DeclaringType);
			GenericInstanceMethod genericInstanceMethod = method as GenericInstanceMethod;
			if (genericInstanceMethod != null)
			{
				methodReference = new MethodReference(method.Name, method.ReturnType, declaringType);
				foreach (ParameterDefinition p in method.Parameters)
				{
					methodReference.Parameters.Add(new ParameterDefinition(p.Name, p.Attributes, p.ParameterType));
				}
				foreach (GenericParameter gp in genericInstanceMethod.ElementMethod.GenericParameters)
				{
					methodReference.GenericParameters.Add(new GenericParameter(gp.Name, methodReference));
				}
				methodReference.HasThis = method.HasThis;
				GenericInstanceMethod i = new GenericInstanceMethod(methodReference);
				foreach (TypeReference ga in genericInstanceMethod.GenericArguments)
				{
					i.GenericArguments.Add(this.Resolve(ga));
				}
				methodReference = i;
			}
			else
			{
				methodReference = new MethodReference(method.Name, method.ReturnType, declaringType);
				foreach (GenericParameter gp2 in method.GenericParameters)
				{
					methodReference.GenericParameters.Add(new GenericParameter(gp2.Name, methodReference));
				}
				foreach (ParameterDefinition p2 in method.Parameters)
				{
					methodReference.Parameters.Add(new ParameterDefinition(p2.Name, p2.Attributes, p2.ParameterType));
				}
				methodReference.HasThis = method.HasThis;
			}
			return methodReference;
		}

		// Token: 0x06001165 RID: 4453 RVA: 0x000347AC File Offset: 0x000329AC
		public FieldReference Resolve(FieldReference field)
		{
			TypeReference declaringType = this.Resolve(field.DeclaringType);
			if (declaringType == field.DeclaringType)
			{
				return field;
			}
			return new FieldReference(field.Name, field.FieldType, declaringType);
		}

		// Token: 0x06001166 RID: 4454 RVA: 0x000347E3 File Offset: 0x000329E3
		public TypeReference ResolveReturnType(MethodReference method)
		{
			return this.Resolve(GenericParameterResolver.ResolveReturnTypeIfNeeded(method));
		}

		// Token: 0x06001167 RID: 4455 RVA: 0x000347F1 File Offset: 0x000329F1
		public TypeReference ResolveParameterType(MethodReference method, ParameterReference parameter)
		{
			return this.Resolve(GenericParameterResolver.ResolveParameterTypeIfNeeded(method, parameter));
		}

		// Token: 0x06001168 RID: 4456 RVA: 0x00034800 File Offset: 0x00032A00
		public TypeReference ResolveVariableType(MethodReference method, VariableReference variable)
		{
			return this.Resolve(GenericParameterResolver.ResolveVariableTypeIfNeeded(method, variable));
		}

		// Token: 0x06001169 RID: 4457 RVA: 0x0003480F File Offset: 0x00032A0F
		public TypeReference ResolveFieldType(FieldReference field)
		{
			return this.Resolve(GenericParameterResolver.ResolveFieldTypeIfNeeded(field));
		}

		// Token: 0x0600116A RID: 4458 RVA: 0x0003481D File Offset: 0x00032A1D
		public TypeReference Resolve(TypeReference typeReference)
		{
			return this.Resolve(typeReference, true);
		}

		// Token: 0x0600116B RID: 4459 RVA: 0x00034828 File Offset: 0x00032A28
		public TypeReference Resolve(TypeReference typeReference, bool includeTypeDefinitions)
		{
			if (this.IsDummy())
			{
				return typeReference;
			}
			if (this._typeDefinitionContext != null && this._typeDefinitionContext.GenericArguments.Contains(typeReference))
			{
				return typeReference;
			}
			if (this._methodDefinitionContext != null && this._methodDefinitionContext.GenericArguments.Contains(typeReference))
			{
				return typeReference;
			}
			GenericParameter genericParameter = typeReference as GenericParameter;
			if (genericParameter != null)
			{
				if (this._typeDefinitionContext != null && this._typeDefinitionContext.GenericArguments.Contains(genericParameter))
				{
					return genericParameter;
				}
				if (this._methodDefinitionContext != null && this._methodDefinitionContext.GenericArguments.Contains(genericParameter))
				{
					return genericParameter;
				}
				return this.ResolveGenericParameter(genericParameter);
			}
			else
			{
				ArrayType arrayType = typeReference as ArrayType;
				if (arrayType != null)
				{
					return new ArrayType(this.Resolve(arrayType.ElementType), arrayType.Rank);
				}
				PointerType pointerType = typeReference as PointerType;
				if (pointerType != null)
				{
					return new PointerType(this.Resolve(pointerType.ElementType));
				}
				ByReferenceType byReferenceType = typeReference as ByReferenceType;
				if (byReferenceType != null)
				{
					return new ByReferenceType(this.Resolve(byReferenceType.ElementType));
				}
				PinnedType pinnedType = typeReference as PinnedType;
				if (pinnedType != null)
				{
					return new PinnedType(this.Resolve(pinnedType.ElementType));
				}
				GenericInstanceType genericInstanceType = typeReference as GenericInstanceType;
				if (genericInstanceType != null)
				{
					GenericInstanceType newGenericInstanceType = new GenericInstanceType(genericInstanceType.ElementType);
					foreach (TypeReference genericArgument in genericInstanceType.GenericArguments)
					{
						newGenericInstanceType.GenericArguments.Add(this.Resolve(genericArgument));
					}
					return newGenericInstanceType;
				}
				RequiredModifierType requiredModType = typeReference as RequiredModifierType;
				if (requiredModType != null)
				{
					return this.Resolve(requiredModType.ElementType, includeTypeDefinitions);
				}
				if (includeTypeDefinitions)
				{
					TypeDefinition typeDefinition = typeReference as TypeDefinition;
					if (typeDefinition != null && typeDefinition.HasGenericParameters)
					{
						GenericInstanceType newGenericInstanceType2 = new GenericInstanceType(typeDefinition);
						foreach (GenericParameter gp in typeDefinition.GenericParameters)
						{
							newGenericInstanceType2.GenericArguments.Add(this.Resolve(gp));
						}
						return newGenericInstanceType2;
					}
				}
				if (typeReference is TypeSpecification)
				{
					throw new NotSupportedException(string.Format("The type {0} cannot be resolved correctly.", typeReference.FullName));
				}
				return typeReference;
			}
		}

		// Token: 0x0600116C RID: 4460 RVA: 0x00034A64 File Offset: 0x00032C64
		internal TypeResolver Nested(GenericInstanceMethod genericInstanceMethod)
		{
			return new TypeResolver(this._typeDefinitionContext as GenericInstanceType, genericInstanceMethod);
		}

		// Token: 0x0600116D RID: 4461 RVA: 0x00034A78 File Offset: 0x00032C78
		private TypeReference ResolveGenericParameter(GenericParameter genericParameter)
		{
			if (genericParameter.Owner == null)
			{
				return this.HandleOwnerlessInvalidILCode(genericParameter);
			}
			if (!(genericParameter.Owner is MemberReference))
			{
				throw new NotSupportedException();
			}
			if (genericParameter.Type == GenericParameterType.Type)
			{
				return this._typeDefinitionContext.GenericArguments[genericParameter.Position];
			}
			if (this._methodDefinitionContext == null)
			{
				return genericParameter;
			}
			return this._methodDefinitionContext.GenericArguments[genericParameter.Position];
		}

		// Token: 0x0600116E RID: 4462 RVA: 0x00034AE8 File Offset: 0x00032CE8
		private TypeReference HandleOwnerlessInvalidILCode(GenericParameter genericParameter)
		{
			if (genericParameter.Type == GenericParameterType.Method && this._typeDefinitionContext != null && genericParameter.Position < this._typeDefinitionContext.GenericArguments.Count)
			{
				return this._typeDefinitionContext.GenericArguments[genericParameter.Position];
			}
			return genericParameter.Module.TypeSystem.Object;
		}

		// Token: 0x0600116F RID: 4463 RVA: 0x00034B45 File Offset: 0x00032D45
		private bool IsDummy()
		{
			return this._typeDefinitionContext == null && this._methodDefinitionContext == null;
		}

		// Token: 0x04000630 RID: 1584
		private readonly IGenericInstance _typeDefinitionContext;

		// Token: 0x04000631 RID: 1585
		private readonly IGenericInstance _methodDefinitionContext;
	}
}
