using System;
using System.Collections.Generic;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x020002B3 RID: 691
	internal sealed class TypeDefinitionProjection
	{
		// Token: 0x060011A3 RID: 4515 RVA: 0x0003519D File Offset: 0x0003339D
		public TypeDefinitionProjection(TypeDefinition type, TypeDefinitionTreatment treatment, Collection<MethodDefinition> redirectedMethods, Collection<KeyValuePair<InterfaceImplementation, InterfaceImplementation>> redirectedInterfaces)
		{
			this.Attributes = type.Attributes;
			this.Name = type.Name;
			this.Treatment = treatment;
			this.RedirectedMethods = redirectedMethods;
			this.RedirectedInterfaces = redirectedInterfaces;
		}

		// Token: 0x04000660 RID: 1632
		public readonly TypeAttributes Attributes;

		// Token: 0x04000661 RID: 1633
		public readonly string Name;

		// Token: 0x04000662 RID: 1634
		public readonly TypeDefinitionTreatment Treatment;

		// Token: 0x04000663 RID: 1635
		public readonly Collection<MethodDefinition> RedirectedMethods;

		// Token: 0x04000664 RID: 1636
		public readonly Collection<KeyValuePair<InterfaceImplementation, InterfaceImplementation>> RedirectedInterfaces;
	}
}
