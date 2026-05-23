using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TaleWorlds.Library;

namespace MCM.UI.Extensions
{
	// Token: 0x02000028 RID: 40
	[NullableContext(1)]
	[Nullable(0)]
	internal static class CollectionExtensions
	{
		// Token: 0x06000183 RID: 387 RVA: 0x00007090 File Offset: 0x00005290
		public static MBBindingList<T> AddRange<[Nullable(2)] T>(this MBBindingList<T> list, IEnumerable<T> range)
		{
			foreach (T item in range)
			{
				list.Add(item);
			}
			return list;
		}

		// Token: 0x06000184 RID: 388 RVA: 0x000070DC File Offset: 0x000052DC
		public static IEnumerable<T> Parallel<[Nullable(2)] T>(this IEnumerable<T> enumerable)
		{
			return enumerable.AsParallel<T>().AsOrdered<T>().WithExecutionMode(ParallelExecutionMode.ForceParallelism);
		}
	}
}
