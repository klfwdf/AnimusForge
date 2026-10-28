using System;
using System.Collections.Generic;
using System.Linq;

namespace Bannerlord.ModuleManager;

internal static class ModuleSorter
{
	public static IList<ModuleInfoExtended> Sort(IReadOnlyCollection<ModuleInfoExtended> source)
	{
		ModuleInfoExtended[] correctModules = (from x in source
			where ModuleUtilities.AreDependenciesPresent(source, x)
			orderby x.IsOfficial descending
			select x).ThenByDescending<ModuleInfoExtended, string>((ModuleInfoExtended mim) => mim.Id, new AlphanumComparatorFast()).ToArray();
		return TopologySort(correctModules, (ModuleInfoExtended module) => ModuleUtilities.GetDependencies(correctModules, module));
	}

	public static IList<ModuleInfoExtended> Sort(IReadOnlyCollection<ModuleInfoExtended> source, ModuleSorterOptions options)
	{
		ModuleInfoExtended[] correctModules = (from x in source
			where ModuleUtilities.AreDependenciesPresent(source, x)
			orderby x.IsOfficial descending
			select x).ThenByDescending<ModuleInfoExtended, string>((ModuleInfoExtended mim) => mim.Id, new AlphanumComparatorFast()).ToArray();
		return TopologySort(correctModules, (ModuleInfoExtended module) => ModuleUtilities.GetDependencies(correctModules, module, options));
	}

	public static IList<T> TopologySort<T>(IEnumerable<T> source, Func<T, IEnumerable<T>> getDependencies)
	{
		List<T> list = new List<T>();
		HashSet<T> visited = new HashSet<T>();
		foreach (T item in source)
		{
			Visit(item, getDependencies, delegate(T item)
			{
				list.Add(item);
			}, visited);
		}
		return list;
	}

	public static void Visit<T>(T item, Func<T, IEnumerable<T>> getDependencies, Action<T> addItem, HashSet<T> visited)
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
				Visit(item2, getDependencies, addItem, visited);
			}
		}
		addItem(item);
	}
}
