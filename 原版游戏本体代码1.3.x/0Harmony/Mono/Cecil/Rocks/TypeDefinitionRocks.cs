using System;
using System.Collections.Generic;
using System.Linq;

namespace Mono.Cecil.Rocks
{
	// Token: 0x0200045B RID: 1115
	internal static class TypeDefinitionRocks
	{
		// Token: 0x06001837 RID: 6199 RVA: 0x0004CDD0 File Offset: 0x0004AFD0
		public static IEnumerable<MethodDefinition> GetConstructors(this TypeDefinition self)
		{
			if (self == null)
			{
				throw new ArgumentNullException("self");
			}
			if (!self.HasMethods)
			{
				return Empty<MethodDefinition>.Array;
			}
			return from method in self.Methods
				where method.IsConstructor
				select method;
		}

		// Token: 0x06001838 RID: 6200 RVA: 0x0004CE24 File Offset: 0x0004B024
		public static MethodDefinition GetStaticConstructor(this TypeDefinition self)
		{
			if (self == null)
			{
				throw new ArgumentNullException("self");
			}
			if (!self.HasMethods)
			{
				return null;
			}
			return self.GetConstructors().FirstOrDefault((MethodDefinition ctor) => ctor.IsStatic);
		}

		// Token: 0x06001839 RID: 6201 RVA: 0x0004CE74 File Offset: 0x0004B074
		public static IEnumerable<MethodDefinition> GetMethods(this TypeDefinition self)
		{
			if (self == null)
			{
				throw new ArgumentNullException("self");
			}
			if (!self.HasMethods)
			{
				return Empty<MethodDefinition>.Array;
			}
			return from method in self.Methods
				where !method.IsConstructor
				select method;
		}

		// Token: 0x0600183A RID: 6202 RVA: 0x0004CEC7 File Offset: 0x0004B0C7
		public static TypeReference GetEnumUnderlyingType(this TypeDefinition self)
		{
			if (self == null)
			{
				throw new ArgumentNullException("self");
			}
			if (!self.IsEnum)
			{
				throw new ArgumentException();
			}
			return self.GetEnumUnderlyingType();
		}
	}
}
