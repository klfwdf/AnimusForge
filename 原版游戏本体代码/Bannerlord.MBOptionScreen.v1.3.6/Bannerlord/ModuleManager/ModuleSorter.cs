using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Bannerlord.ModuleManager
{
	// Token: 0x02000065 RID: 101
	[NullableContext(1)]
	[Nullable(0)]
	internal static class ModuleSorter
	{
		// Token: 0x060003BA RID: 954 RVA: 0x0000D7E0 File Offset: 0x0000B9E0
		public static IList<ModuleInfoExtended> Sort(IReadOnlyCollection<ModuleInfoExtended> source)
		{
			ModuleInfoExtended[] correctModules = (from x in source
				where ModuleUtilities.AreDependenciesPresent(source, x)
				orderby x.IsOfficial descending
				select x).ThenByDescending((ModuleInfoExtended mim) => mim.Id, new AlphanumComparatorFast()).ToArray<ModuleInfoExtended>();
			return ModuleSorter.TopologySort<ModuleInfoExtended>(correctModules, (ModuleInfoExtended module) => ModuleUtilities.GetDependencies(correctModules, module));
		}

		// Token: 0x060003BB RID: 955 RVA: 0x0000D880 File Offset: 0x0000BA80
		public static IList<ModuleInfoExtended> Sort(IReadOnlyCollection<ModuleInfoExtended> source, ModuleSorterOptions options)
		{
			ModuleInfoExtended[] correctModules = (from x in source
				where ModuleUtilities.AreDependenciesPresent(source, x)
				orderby x.IsOfficial descending
				select x).ThenByDescending((ModuleInfoExtended mim) => mim.Id, new AlphanumComparatorFast()).ToArray<ModuleInfoExtended>();
			return ModuleSorter.TopologySort<ModuleInfoExtended>(correctModules, (ModuleInfoExtended module) => ModuleUtilities.GetDependencies(correctModules, module, options));
		}

		// Token: 0x060003BC RID: 956 RVA: 0x0000D928 File Offset: 0x0000BB28
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

		// Token: 0x060003BD RID: 957 RVA: 0x0000D9B0 File Offset: 0x0000BBB0
		public static void Visit<[Nullable(2)] T>(T item, Func<T, IEnumerable<T>> getDependencies, Action<T> addItem, HashSet<T> visited)
		{
			if (visited.Contains(item))
			{
				return;
			}
			visited.Add(item);
			IEnumerable<T> enumerable = getDependencies(item);
			if (enumerable != null)
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
