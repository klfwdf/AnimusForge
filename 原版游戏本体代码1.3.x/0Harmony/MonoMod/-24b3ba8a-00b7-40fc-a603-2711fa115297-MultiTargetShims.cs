using System;
using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace MonoMod
{
	// Token: 0x020004D7 RID: 1239
	internal static class <24b3ba8a-00b7-40fc-a603-2711fa115297>MultiTargetShims
	{
		// Token: 0x06001B96 RID: 7062 RVA: 0x0005895A File Offset: 0x00056B5A
		[NullableContext(1)]
		public static TypeReference GetConstraintType(this GenericParameterConstraint constraint)
		{
			return constraint.ConstraintType;
		}
	}
}
