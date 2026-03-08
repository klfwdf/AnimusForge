using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HarmonyLib
{
	// Token: 0x02000023 RID: 35
	internal static class InfixExtensions
	{
		// Token: 0x060000B6 RID: 182 RVA: 0x00005444 File Offset: 0x00003644
		internal static Infix[] FilterAndSort(this IEnumerable<Infix> infixes, MethodInfo innerMethod, int index, int total, bool debug)
		{
			return (from p in new PatchSorter((from fix in infixes
					where fix.Matches(innerMethod, index, total)
					select fix.patch).ToArray<Patch>(), debug).Sort()
				select new Infix(p)).ToArray<Infix>();
		}
	}
}
