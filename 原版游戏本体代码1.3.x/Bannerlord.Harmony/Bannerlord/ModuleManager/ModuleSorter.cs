using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Bannerlord.ModuleManager
{
	// Token: 0x02000023 RID: 35
	[NullableContext(1)]
	[Nullable(0)]
	internal static class ModuleSorter
	{
		// Token: 0x060001B2 RID: 434 RVA: 0x00008A88 File Offset: 0x00006C88
		public static IList<ModuleInfoExtended> Sort(IReadOnlyCollection<ModuleInfoExtended> source)
		{
			ModuleInfoExtended[] correctModules = (from x in source
				where ModuleUtilities.AreDependenciesPresent(source, x)
				orderby x.IsOfficial descending
				select x).ThenByDescending((ModuleInfoExtended mim) => mim.Id, new AlphanumComparatorFast()).ToArray<ModuleInfoExtended>();
			return ModuleSorter.TopologySort<ModuleInfoExtended>(correctModules, (ModuleInfoExtended module) => ModuleUtilities.GetDependencies(correctModules, module));
		}

		// Token: 0x060001B3 RID: 435 RVA: 0x00008B30 File Offset: 0x00006D30
		public static IList<ModuleInfoExtended> Sort(IReadOnlyCollection<ModuleInfoExtended> source, ModuleSorterOptions options)
		{
			ModuleInfoExtended[] correctModules = (from x in source
				where ModuleUtilities.AreDependenciesPresent(source, x)
				orderby x.IsOfficial descending
				select x).ThenByDescending((ModuleInfoExtended mim) => mim.Id, new AlphanumComparatorFast()).ToArray<ModuleInfoExtended>();
			return ModuleSorter.TopologySort<ModuleInfoExtended>(correctModules, (ModuleInfoExtended module) => ModuleUtilities.GetDependencies(correctModules, module, options));
		}

		// Token: 0x060001B4 RID: 436 RVA: 0x00008BDC File Offset: 0x00006DDC
		public static IList<T> TopologySort<[Nullable(2)] T>(IEnumerable<T> source, Func<T, IEnumerable<T>> getDependencies)
		{
			List<T> list = new List<T>();
			HashSet<T> visited = new HashSet<T>();
			Action<T> <>9__0;
			foreach (T item3 in source)
			{
				T item2 = item3;
				Action<T> addItem;
				if ((addItem = <>9__0) == null)
				{
					addItem = (<>9__0 = delegate(T item)
					{
						list.Add(item);
					});
				}
				ModuleSorter.Visit<T>(item2, getDependencies, addItem, visited);
			}
			return list;
		}

		// Token: 0x060001B5 RID: 437 RVA: 0x00008C70 File Offset: 0x00006E70
		public static void Visit<[Nullable(2)] T>(T item, Func<T, IEnumerable<T>> getDependencies, Action<T> addItem, HashSet<T> visited)
		{
			bool flag = visited.Contains(item);
			if (!flag)
			{
				visited.Add(item);
				IEnumerable<T> enumerable = getDependencies(item);
				bool flag2 = enumerable != null;
				if (flag2)
				{
					foreach (T item2 in enumerable)
					{
						ModuleSorter.Visit<T>(item2, getDependencies, addItem, visited);
					}
				}
				addItem(item);
			}
		}
	}
}
