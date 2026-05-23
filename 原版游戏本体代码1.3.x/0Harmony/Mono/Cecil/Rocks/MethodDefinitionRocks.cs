using System;

namespace Mono.Cecil.Rocks
{
	// Token: 0x02000455 RID: 1109
	internal static class MethodDefinitionRocks
	{
		// Token: 0x06001823 RID: 6179 RVA: 0x0004C8F4 File Offset: 0x0004AAF4
		public static MethodDefinition GetBaseMethod(this MethodDefinition self)
		{
			if (self == null)
			{
				throw new ArgumentNullException("self");
			}
			if (!self.IsVirtual)
			{
				return self;
			}
			if (self.IsNewSlot)
			{
				return self;
			}
			for (TypeDefinition base_type = MethodDefinitionRocks.ResolveBaseType(self.DeclaringType); base_type != null; base_type = MethodDefinitionRocks.ResolveBaseType(base_type))
			{
				MethodDefinition @base = MethodDefinitionRocks.GetMatchingMethod(base_type, self);
				if (@base != null)
				{
					return @base;
				}
			}
			return self;
		}

		// Token: 0x06001824 RID: 6180 RVA: 0x0004C94C File Offset: 0x0004AB4C
		public static MethodDefinition GetOriginalBaseMethod(this MethodDefinition self)
		{
			if (self == null)
			{
				throw new ArgumentNullException("self");
			}
			for (;;)
			{
				MethodDefinition @base = self.GetBaseMethod();
				if (@base == self)
				{
					break;
				}
				self = @base;
			}
			return self;
		}

		// Token: 0x06001825 RID: 6181 RVA: 0x0004C978 File Offset: 0x0004AB78
		private static TypeDefinition ResolveBaseType(TypeDefinition type)
		{
			if (type == null)
			{
				return null;
			}
			TypeReference base_type = type.BaseType;
			if (base_type == null)
			{
				return null;
			}
			return base_type.Resolve();
		}

		// Token: 0x06001826 RID: 6182 RVA: 0x0004C99C File Offset: 0x0004AB9C
		private static MethodDefinition GetMatchingMethod(TypeDefinition type, MethodDefinition method)
		{
			return MetadataResolver.GetMethod(type.Methods, method);
		}
	}
}
