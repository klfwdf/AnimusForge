using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HarmonyLib;

internal static class InfixExtensions
{
	internal static Infix[] FilterAndSort(this IEnumerable<Infix> infixes, MethodInfo innerMethod, int index, int total, bool debug)
	{
		return (from p in new PatchSorter((from fix in infixes
				where fix.Matches(innerMethod, index, total)
				select fix.patch).ToArray(), debug).Sort()
			select new Infix(p)).ToArray();
	}
}
