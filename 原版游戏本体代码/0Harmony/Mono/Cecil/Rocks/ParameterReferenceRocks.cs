using System;

namespace Mono.Cecil.Rocks
{
	// Token: 0x02000459 RID: 1113
	internal static class ParameterReferenceRocks
	{
		// Token: 0x0600182D RID: 6189 RVA: 0x0004CA27 File Offset: 0x0004AC27
		public static int GetSequence(this ParameterReference self)
		{
			return self.Index + 1;
		}
	}
}
