using System;
using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace MonoMod
{
	// Token: 0x0200080F RID: 2063
	internal static class MultiTargetShims
	{
		// Token: 0x06002786 RID: 10118 RVA: 0x0005895A File Offset: 0x00056B5A
		[NullableContext(1)]
		public static TypeReference GetConstraintType(this GenericParameterConstraint constraint)
		{
			return constraint.ConstraintType;
		}
	}
}
