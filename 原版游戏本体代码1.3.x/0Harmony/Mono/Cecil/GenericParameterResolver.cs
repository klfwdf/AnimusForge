using System;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000245 RID: 581
	internal sealed class GenericParameterResolver
	{
		// Token: 0x06000D28 RID: 3368 RVA: 0x0002C004 File Offset: 0x0002A204
		internal static TypeReference ResolveReturnTypeIfNeeded(MethodReference methodReference)
		{
			if (methodReference.DeclaringType.IsArray && methodReference.Name == "Get")
			{
				return methodReference.ReturnType;
			}
			GenericInstanceMethod genericInstanceMethod = methodReference as GenericInstanceMethod;
			GenericInstanceType declaringGenericInstanceType = methodReference.DeclaringType as GenericInstanceType;
			if (genericInstanceMethod == null && declaringGenericInstanceType == null)
			{
				return methodReference.ReturnType;
			}
			return GenericParameterResolver.ResolveIfNeeded(genericInstanceMethod, declaringGenericInstanceType, methodReference.ReturnType);
		}

		// Token: 0x06000D29 RID: 3369 RVA: 0x0002C064 File Offset: 0x0002A264
		internal static TypeReference ResolveFieldTypeIfNeeded(FieldReference fieldReference)
		{
			return GenericParameterResolver.ResolveIfNeeded(null, fieldReference.DeclaringType as GenericInstanceType, fieldReference.FieldType);
		}

		// Token: 0x06000D2A RID: 3370 RVA: 0x0002C080 File Offset: 0x0002A280
		internal static TypeReference ResolveParameterTypeIfNeeded(MethodReference method, ParameterReference parameter)
		{
			GenericInstanceMethod genericInstanceMethod = method as GenericInstanceMethod;
			GenericInstanceType declaringGenericInstanceType = method.DeclaringType as GenericInstanceType;
			if (genericInstanceMethod == null && declaringGenericInstanceType == null)
			{
				return parameter.ParameterType;
			}
			return GenericParameterResolver.ResolveIfNeeded(genericInstanceMethod, declaringGenericInstanceType, parameter.ParameterType);
		}

		// Token: 0x06000D2B RID: 3371 RVA: 0x0002C0BC File Offset: 0x0002A2BC
		internal static TypeReference ResolveVariableTypeIfNeeded(MethodReference method, VariableReference variable)
		{
			GenericInstanceMethod genericInstanceMethod = method as GenericInstanceMethod;
			GenericInstanceType declaringGenericInstanceType = method.DeclaringType as GenericInstanceType;
			if (genericInstanceMethod == null && declaringGenericInstanceType == null)
			{
				return variable.VariableType;
			}
			return GenericParameterResolver.ResolveIfNeeded(genericInstanceMethod, declaringGenericInstanceType, variable.VariableType);
		}

		// Token: 0x06000D2C RID: 3372 RVA: 0x0002C0F8 File Offset: 0x0002A2F8
		private static TypeReference ResolveIfNeeded(IGenericInstance genericInstanceMethod, IGenericInstance declaringGenericInstanceType, TypeReference parameterType)
		{
			ByReferenceType byRefType = parameterType as ByReferenceType;
			if (byRefType != null)
			{
				return GenericParameterResolver.ResolveIfNeeded(genericInstanceMethod, declaringGenericInstanceType, byRefType);
			}
			ArrayType arrayType = parameterType as ArrayType;
			if (arrayType != null)
			{
				return GenericParameterResolver.ResolveIfNeeded(genericInstanceMethod, declaringGenericInstanceType, arrayType);
			}
			GenericInstanceType genericInstanceType = parameterType as GenericInstanceType;
			if (genericInstanceType != null)
			{
				return GenericParameterResolver.ResolveIfNeeded(genericInstanceMethod, declaringGenericInstanceType, genericInstanceType);
			}
			GenericParameter genericParameter = parameterType as GenericParameter;
			if (genericParameter != null)
			{
				return GenericParameterResolver.ResolveIfNeeded(genericInstanceMethod, declaringGenericInstanceType, genericParameter);
			}
			RequiredModifierType requiredModifierType = parameterType as RequiredModifierType;
			if (requiredModifierType != null && GenericParameterResolver.ContainsGenericParameters(requiredModifierType))
			{
				return GenericParameterResolver.ResolveIfNeeded(genericInstanceMethod, declaringGenericInstanceType, requiredModifierType.ElementType);
			}
			if (GenericParameterResolver.ContainsGenericParameters(parameterType))
			{
				throw new Exception("Unexpected generic parameter.");
			}
			return parameterType;
		}

		// Token: 0x06000D2D RID: 3373 RVA: 0x0002C189 File Offset: 0x0002A389
		private static TypeReference ResolveIfNeeded(IGenericInstance genericInstanceMethod, IGenericInstance genericInstanceType, GenericParameter genericParameterElement)
		{
			if (genericParameterElement.MetadataType != MetadataType.MVar)
			{
				return genericInstanceType.GenericArguments[genericParameterElement.Position];
			}
			if (genericInstanceMethod == null)
			{
				return genericParameterElement;
			}
			return genericInstanceMethod.GenericArguments[genericParameterElement.Position];
		}

		// Token: 0x06000D2E RID: 3374 RVA: 0x0002C1BD File Offset: 0x0002A3BD
		private static ArrayType ResolveIfNeeded(IGenericInstance genericInstanceMethod, IGenericInstance genericInstanceType, ArrayType arrayType)
		{
			return new ArrayType(GenericParameterResolver.ResolveIfNeeded(genericInstanceMethod, genericInstanceType, arrayType.ElementType), arrayType.Rank);
		}

		// Token: 0x06000D2F RID: 3375 RVA: 0x0002C1D7 File Offset: 0x0002A3D7
		private static ByReferenceType ResolveIfNeeded(IGenericInstance genericInstanceMethod, IGenericInstance genericInstanceType, ByReferenceType byReferenceType)
		{
			return new ByReferenceType(GenericParameterResolver.ResolveIfNeeded(genericInstanceMethod, genericInstanceType, byReferenceType.ElementType));
		}

		// Token: 0x06000D30 RID: 3376 RVA: 0x0002C1EC File Offset: 0x0002A3EC
		private static GenericInstanceType ResolveIfNeeded(IGenericInstance genericInstanceMethod, IGenericInstance genericInstanceType, GenericInstanceType genericInstanceType1)
		{
			if (!GenericParameterResolver.ContainsGenericParameters(genericInstanceType1))
			{
				return genericInstanceType1;
			}
			GenericInstanceType newGenericInstance = new GenericInstanceType(genericInstanceType1.ElementType);
			foreach (TypeReference genericArgument in genericInstanceType1.GenericArguments)
			{
				if (!genericArgument.IsGenericParameter)
				{
					newGenericInstance.GenericArguments.Add(GenericParameterResolver.ResolveIfNeeded(genericInstanceMethod, genericInstanceType, genericArgument));
				}
				else
				{
					GenericParameter genParam = (GenericParameter)genericArgument;
					GenericParameterType type = genParam.Type;
					if (type != GenericParameterType.Type)
					{
						if (type == GenericParameterType.Method)
						{
							if (genericInstanceMethod == null)
							{
								newGenericInstance.GenericArguments.Add(genParam);
							}
							else
							{
								newGenericInstance.GenericArguments.Add(genericInstanceMethod.GenericArguments[genParam.Position]);
							}
						}
					}
					else
					{
						if (genericInstanceType == null)
						{
							throw new NotSupportedException();
						}
						newGenericInstance.GenericArguments.Add(genericInstanceType.GenericArguments[genParam.Position]);
					}
				}
			}
			return newGenericInstance;
		}

		// Token: 0x06000D31 RID: 3377 RVA: 0x0002C2E0 File Offset: 0x0002A4E0
		private static bool ContainsGenericParameters(TypeReference typeReference)
		{
			if (typeReference is GenericParameter)
			{
				return true;
			}
			ArrayType arrayType = typeReference as ArrayType;
			if (arrayType != null)
			{
				return GenericParameterResolver.ContainsGenericParameters(arrayType.ElementType);
			}
			PointerType pointerType = typeReference as PointerType;
			if (pointerType != null)
			{
				return GenericParameterResolver.ContainsGenericParameters(pointerType.ElementType);
			}
			ByReferenceType byRefType = typeReference as ByReferenceType;
			if (byRefType != null)
			{
				return GenericParameterResolver.ContainsGenericParameters(byRefType.ElementType);
			}
			SentinelType sentinelType = typeReference as SentinelType;
			if (sentinelType != null)
			{
				return GenericParameterResolver.ContainsGenericParameters(sentinelType.ElementType);
			}
			PinnedType pinnedType = typeReference as PinnedType;
			if (pinnedType != null)
			{
				return GenericParameterResolver.ContainsGenericParameters(pinnedType.ElementType);
			}
			RequiredModifierType requiredModifierType = typeReference as RequiredModifierType;
			if (requiredModifierType != null)
			{
				return GenericParameterResolver.ContainsGenericParameters(requiredModifierType.ElementType);
			}
			GenericInstanceType genericInstance = typeReference as GenericInstanceType;
			if (genericInstance != null)
			{
				using (Collection<TypeReference>.Enumerator enumerator = genericInstance.GenericArguments.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (GenericParameterResolver.ContainsGenericParameters(enumerator.Current))
						{
							return true;
						}
					}
				}
				return false;
			}
			if (typeReference is TypeSpecification)
			{
				throw new NotSupportedException();
			}
			return false;
		}
	}
}
