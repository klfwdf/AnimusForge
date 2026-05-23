using System;
using System.Collections.Generic;
using System.Linq;

namespace Mono.Cecil.Rocks
{
	// Token: 0x02000456 RID: 1110
	internal static class ModuleDefinitionRocks
	{
		// Token: 0x06001827 RID: 6183 RVA: 0x0004C9AA File Offset: 0x0004ABAA
		public static IEnumerable<TypeDefinition> GetAllTypes(this ModuleDefinition self)
		{
			if (self == null)
			{
				throw new ArgumentNullException("self");
			}
			return self.Types.SelectMany(Functional.Y<TypeDefinition, IEnumerable<TypeDefinition>>((Func<TypeDefinition, IEnumerable<TypeDefinition>> f) => (TypeDefinition type) => type.NestedTypes.SelectMany(f).Prepend(type)));
		}
	}
}
