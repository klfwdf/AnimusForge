using System;
using System.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x020002AB RID: 683
	internal sealed class TypeReferenceEqualityComparer : EqualityComparer<TypeReference>
	{
		// Token: 0x06001157 RID: 4439 RVA: 0x00033EF2 File Offset: 0x000320F2
		public override bool Equals(TypeReference x, TypeReference y)
		{
			return TypeReferenceEqualityComparer.AreEqual(x, y, TypeComparisonMode.Exact);
		}

		// Token: 0x06001158 RID: 4440 RVA: 0x00033EFC File Offset: 0x000320FC
		public override int GetHashCode(TypeReference obj)
		{
			return TypeReferenceEqualityComparer.GetHashCodeFor(obj);
		}

		// Token: 0x06001159 RID: 4441 RVA: 0x00033F04 File Offset: 0x00032104
		public static bool AreEqual(TypeReference a, TypeReference b, TypeComparisonMode comparisonMode = TypeComparisonMode.Exact)
		{
			if (a == b)
			{
				return true;
			}
			if (a == null || b == null)
			{
				return false;
			}
			MetadataType aMetadataType = a.MetadataType;
			MetadataType bMetadataType = b.MetadataType;
			if (aMetadataType == MetadataType.GenericInstance || bMetadataType == MetadataType.GenericInstance)
			{
				return aMetadataType == bMetadataType && TypeReferenceEqualityComparer.AreEqual((GenericInstanceType)a, (GenericInstanceType)b, comparisonMode);
			}
			if (aMetadataType == MetadataType.Array || bMetadataType == MetadataType.Array)
			{
				if (aMetadataType != bMetadataType)
				{
					return false;
				}
				ArrayType a2 = (ArrayType)a;
				ArrayType b2 = (ArrayType)b;
				return a2.Rank == b2.Rank && TypeReferenceEqualityComparer.AreEqual(a2.ElementType, b2.ElementType, comparisonMode);
			}
			else
			{
				if (aMetadataType == MetadataType.Var || bMetadataType == MetadataType.Var)
				{
					return aMetadataType == bMetadataType && TypeReferenceEqualityComparer.AreEqual((GenericParameter)a, (GenericParameter)b, comparisonMode);
				}
				if (aMetadataType == MetadataType.MVar || bMetadataType == MetadataType.MVar)
				{
					return aMetadataType == bMetadataType && TypeReferenceEqualityComparer.AreEqual((GenericParameter)a, (GenericParameter)b, comparisonMode);
				}
				if (aMetadataType == MetadataType.ByReference || bMetadataType == MetadataType.ByReference)
				{
					return aMetadataType == bMetadataType && TypeReferenceEqualityComparer.AreEqual(((ByReferenceType)a).ElementType, ((ByReferenceType)b).ElementType, comparisonMode);
				}
				if (aMetadataType == MetadataType.Pointer || bMetadataType == MetadataType.Pointer)
				{
					return aMetadataType == bMetadataType && TypeReferenceEqualityComparer.AreEqual(((PointerType)a).ElementType, ((PointerType)b).ElementType, comparisonMode);
				}
				if (aMetadataType == MetadataType.RequiredModifier || bMetadataType == MetadataType.RequiredModifier)
				{
					if (aMetadataType != bMetadataType)
					{
						return false;
					}
					RequiredModifierType a3 = (RequiredModifierType)a;
					RequiredModifierType b3 = (RequiredModifierType)b;
					return TypeReferenceEqualityComparer.AreEqual(a3.ModifierType, b3.ModifierType, comparisonMode) && TypeReferenceEqualityComparer.AreEqual(a3.ElementType, b3.ElementType, comparisonMode);
				}
				else if (aMetadataType == MetadataType.OptionalModifier || bMetadataType == MetadataType.OptionalModifier)
				{
					if (aMetadataType != bMetadataType)
					{
						return false;
					}
					OptionalModifierType a4 = (OptionalModifierType)a;
					OptionalModifierType b4 = (OptionalModifierType)b;
					return TypeReferenceEqualityComparer.AreEqual(a4.ModifierType, b4.ModifierType, comparisonMode) && TypeReferenceEqualityComparer.AreEqual(a4.ElementType, b4.ElementType, comparisonMode);
				}
				else
				{
					if (aMetadataType == MetadataType.Pinned || bMetadataType == MetadataType.Pinned)
					{
						return aMetadataType == bMetadataType && TypeReferenceEqualityComparer.AreEqual(((PinnedType)a).ElementType, ((PinnedType)b).ElementType, comparisonMode);
					}
					if (aMetadataType == MetadataType.Sentinel || bMetadataType == MetadataType.Sentinel)
					{
						return aMetadataType == bMetadataType && TypeReferenceEqualityComparer.AreEqual(((SentinelType)a).ElementType, ((SentinelType)b).ElementType, comparisonMode);
					}
					if (!a.Name.Equals(b.Name) || !a.Namespace.Equals(b.Namespace))
					{
						return false;
					}
					TypeDefinition xDefinition = a.Resolve();
					TypeDefinition yDefinition = b.Resolve();
					if (comparisonMode == TypeComparisonMode.SignatureOnlyLoose)
					{
						return !(xDefinition.Module.Name != yDefinition.Module.Name) && !(xDefinition.Module.Assembly.Name.Name != yDefinition.Module.Assembly.Name.Name) && xDefinition.FullName == yDefinition.FullName;
					}
					return xDefinition == yDefinition;
				}
			}
		}

		// Token: 0x0600115A RID: 4442 RVA: 0x000341D0 File Offset: 0x000323D0
		private static bool AreEqual(GenericParameter a, GenericParameter b, TypeComparisonMode comparisonMode = TypeComparisonMode.Exact)
		{
			if (a == b)
			{
				return true;
			}
			if (a.Position != b.Position)
			{
				return false;
			}
			if (a.Type != b.Type)
			{
				return false;
			}
			TypeReference aOwnerType = a.Owner as TypeReference;
			if (aOwnerType != null && TypeReferenceEqualityComparer.AreEqual(aOwnerType, b.Owner as TypeReference, comparisonMode))
			{
				return true;
			}
			MethodReference aOwnerMethod = a.Owner as MethodReference;
			return (aOwnerMethod != null && comparisonMode != TypeComparisonMode.SignatureOnlyLoose && MethodReferenceComparer.AreEqual(aOwnerMethod, b.Owner as MethodReference)) || comparisonMode == TypeComparisonMode.SignatureOnly || comparisonMode == TypeComparisonMode.SignatureOnlyLoose;
		}

		// Token: 0x0600115B RID: 4443 RVA: 0x0003425C File Offset: 0x0003245C
		private static bool AreEqual(GenericInstanceType a, GenericInstanceType b, TypeComparisonMode comparisonMode = TypeComparisonMode.Exact)
		{
			if (a == b)
			{
				return true;
			}
			int aGenericArgumentsCount = a.GenericArguments.Count;
			if (aGenericArgumentsCount != b.GenericArguments.Count)
			{
				return false;
			}
			if (!TypeReferenceEqualityComparer.AreEqual(a.ElementType, b.ElementType, comparisonMode))
			{
				return false;
			}
			for (int i = 0; i < aGenericArgumentsCount; i++)
			{
				if (!TypeReferenceEqualityComparer.AreEqual(a.GenericArguments[i], b.GenericArguments[i], comparisonMode))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x0600115C RID: 4444 RVA: 0x000342D0 File Offset: 0x000324D0
		public static int GetHashCodeFor(TypeReference obj)
		{
			MetadataType metadataType = obj.MetadataType;
			if (metadataType == MetadataType.GenericInstance)
			{
				GenericInstanceType genericInstanceType = (GenericInstanceType)obj;
				int hashCode = TypeReferenceEqualityComparer.GetHashCodeFor(genericInstanceType.ElementType) * 486187739 + 31;
				for (int i = 0; i < genericInstanceType.GenericArguments.Count; i++)
				{
					hashCode = hashCode * 486187739 + TypeReferenceEqualityComparer.GetHashCodeFor(genericInstanceType.GenericArguments[i]);
				}
				return hashCode;
			}
			if (metadataType == MetadataType.Array)
			{
				ArrayType arrayType = (ArrayType)obj;
				return TypeReferenceEqualityComparer.GetHashCodeFor(arrayType.ElementType) * 486187739 + arrayType.Rank.GetHashCode();
			}
			if (metadataType == MetadataType.Var || metadataType == MetadataType.MVar)
			{
				GenericParameter genericParameter = (GenericParameter)obj;
				int num = genericParameter.Position.GetHashCode() * 486187739;
				int num2 = (int)metadataType;
				int hashCode2 = num + num2.GetHashCode();
				TypeReference ownerTypeReference = genericParameter.Owner as TypeReference;
				if (ownerTypeReference != null)
				{
					return hashCode2 * 486187739 + TypeReferenceEqualityComparer.GetHashCodeFor(ownerTypeReference);
				}
				MethodReference ownerMethodReference = genericParameter.Owner as MethodReference;
				if (ownerMethodReference != null)
				{
					return hashCode2 * 486187739 + MethodReferenceComparer.GetHashCodeFor(ownerMethodReference);
				}
				throw new InvalidOperationException("Generic parameter encountered with invalid owner");
			}
			else
			{
				if (metadataType == MetadataType.ByReference)
				{
					return TypeReferenceEqualityComparer.GetHashCodeFor(((ByReferenceType)obj).ElementType) * 486187739 * 37;
				}
				if (metadataType == MetadataType.Pointer)
				{
					return TypeReferenceEqualityComparer.GetHashCodeFor(((PointerType)obj).ElementType) * 486187739 * 41;
				}
				if (metadataType == MetadataType.RequiredModifier)
				{
					RequiredModifierType requiredModifierType = (RequiredModifierType)obj;
					return TypeReferenceEqualityComparer.GetHashCodeFor(requiredModifierType.ElementType) * 43 * 486187739 + TypeReferenceEqualityComparer.GetHashCodeFor(requiredModifierType.ModifierType);
				}
				if (metadataType == MetadataType.OptionalModifier)
				{
					OptionalModifierType optionalModifierType = (OptionalModifierType)obj;
					return TypeReferenceEqualityComparer.GetHashCodeFor(optionalModifierType.ElementType) * 47 * 486187739 + TypeReferenceEqualityComparer.GetHashCodeFor(optionalModifierType.ModifierType);
				}
				if (metadataType == MetadataType.Pinned)
				{
					return TypeReferenceEqualityComparer.GetHashCodeFor(((PinnedType)obj).ElementType) * 486187739 * 53;
				}
				if (metadataType == MetadataType.Sentinel)
				{
					return TypeReferenceEqualityComparer.GetHashCodeFor(((SentinelType)obj).ElementType) * 486187739 * 59;
				}
				if (metadataType == MetadataType.FunctionPointer)
				{
					throw new NotImplementedException("We currently don't handle function pointer types.");
				}
				return obj.Namespace.GetHashCode() * 486187739 + obj.FullName.GetHashCode();
			}
		}
	}
}
