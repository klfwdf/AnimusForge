using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace MCM.UI.Extensions;

internal static class CollectionExtensions
{
	public static MBBindingList<T> AddRange<T>(this MBBindingList<T> list, IEnumerable<T> range)
	{
		foreach (T item in range)
		{
			list.Add(item);
		}
		return list;
	}

	public static IEnumerable<T> Parallel<T>(this IEnumerable<T> enumerable)
	{
		return enumerable.AsParallel().AsOrdered().WithExecutionMode(ParallelExecutionMode.ForceParallelism);
	}
}
