using System;

namespace Mono.Cecil.Rocks
{
	// Token: 0x0200045D RID: 1117
	internal static class TypeReferenceRocks
	{
		// Token: 0x06001840 RID: 6208 RVA: 0x0004CF12 File Offset: 0x0004B112
		public static ArrayType MakeArrayType(this TypeReference self)
		{
			return new ArrayType(self);
		}

		// Token: 0x06001841 RID: 6209 RVA: 0x0004CF1C File Offset: 0x0004B11C
		public static ArrayType MakeArrayType(this TypeReference self, int rank)
		{
			if (rank == 0)
			{
				throw new ArgumentOutOfRangeException("rank");
			}
			ArrayType array = new ArrayType(self);
			for (int i = 1; i < rank; i++)
			{
				array.Dimensions.Add(default(ArrayDimension));
			}
			return array;
		}

		// Token: 0x06001842 RID: 6210 RVA: 0x0004CF5F File Offset: 0x0004B15F
		public static PointerType MakePointerType(this TypeReference self)
		{
			return new PointerType(self);
		}

		// Token: 0x06001843 RID: 6211 RVA: 0x0004CF67 File Offset: 0x0004B167
		public static ByReferenceType MakeByReferenceType(this TypeReference self)
		{
			return new ByReferenceType(self);
		}

		// Token: 0x06001844 RID: 6212 RVA: 0x0004CF6F File Offset: 0x0004B16F
		public static OptionalModifierType MakeOptionalModifierType(this TypeReference self, TypeReference modifierType)
		{
			return new OptionalModifierType(modifierType, self);
		}

		// Token: 0x06001845 RID: 6213 RVA: 0x0004CF78 File Offset: 0x0004B178
		public static RequiredModifierType MakeRequiredModifierType(this TypeReference self, TypeReference modifierType)
		{
			return new RequiredModifierType(modifierType, self);
		}

		// Token: 0x06001846 RID: 6214 RVA: 0x0004CF84 File Offset: 0x0004B184
		public static GenericInstanceType MakeGenericInstanceType(this TypeReference self, params TypeReference[] arguments)
		{
			if (self == null)
			{
				throw new ArgumentNullException("self");
			}
			if (arguments == null)
			{
				throw new ArgumentNullException("arguments");
			}
			if (arguments.Length == 0)
			{
				throw new ArgumentException();
			}
			if (self.GenericParameters.Count != arguments.Length)
			{
				throw new ArgumentException();
			}
			GenericInstanceType instance = new GenericInstanceType(self, arguments.Length);
			foreach (TypeReference argument in arguments)
			{
				instance.GenericArguments.Add(argument);
			}
			return instance;
		}

		// Token: 0x06001847 RID: 6215 RVA: 0x0004CFF8 File Offset: 0x0004B1F8
		public static PinnedType MakePinnedType(this TypeReference self)
		{
			return new PinnedType(self);
		}

		// Token: 0x06001848 RID: 6216 RVA: 0x0004D000 File Offset: 0x0004B200
		public static SentinelType MakeSentinelType(this TypeReference self)
		{
			return new SentinelType(self);
		}
	}
}
