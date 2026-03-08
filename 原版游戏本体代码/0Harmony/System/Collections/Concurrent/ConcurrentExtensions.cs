using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Collections.Concurrent
{
	// Token: 0x02000489 RID: 1161
	[NullableContext(1)]
	[Nullable(0)]
	internal static class ConcurrentExtensions
	{
		// Token: 0x060019EF RID: 6639 RVA: 0x00054C5C File Offset: 0x00052E5C
		public static void Clear<[Nullable(2)] T>(this ConcurrentBag<T> bag)
		{
			ThrowHelper.ThrowIfArgumentNull(bag, "bag", null);
			T t;
			while (bag.TryTake(out t))
			{
			}
		}

		// Token: 0x060019F0 RID: 6640 RVA: 0x00054C80 File Offset: 0x00052E80
		public static void Clear<[Nullable(2)] T>(this ConcurrentQueue<T> queue)
		{
			ThrowHelper.ThrowIfArgumentNull(queue, "queue", null);
			T t;
			while (queue.TryDequeue(out t))
			{
			}
		}

		// Token: 0x060019F1 RID: 6641 RVA: 0x00054CA4 File Offset: 0x00052EA4
		public static TValue AddOrUpdate<TKey, [Nullable(2)] TValue, [Nullable(2)] TArg>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, Func<TKey, TArg, TValue> addValueFactory, Func<TKey, TValue, TArg, TValue> updateValueFactory, TArg factoryArgument)
		{
			ThrowHelper.ThrowIfArgumentNull(dict, "dict", null);
			return dict.AddOrUpdate(key, ([Nullable(1)] TKey k) => addValueFactory(k, factoryArgument), ([Nullable(1)] TKey k, TValue v) => updateValueFactory(k, v, factoryArgument));
		}

		// Token: 0x060019F2 RID: 6642 RVA: 0x00054CF8 File Offset: 0x00052EF8
		public static TValue GetOrAdd<TKey, [Nullable(2)] TValue, [Nullable(2)] TArg>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
		{
			ThrowHelper.ThrowIfArgumentNull(dict, "dict", null);
			return dict.GetOrAdd(key, ([Nullable(1)] TKey k) => valueFactory(k, factoryArgument));
		}

		// Token: 0x060019F3 RID: 6643 RVA: 0x00054D38 File Offset: 0x00052F38
		public static bool TryRemove<TKey, [Nullable(2)] TValue>(this ConcurrentDictionary<TKey, TValue> dict, [Nullable(new byte[] { 0, 1, 1 })] KeyValuePair<TKey, TValue> item)
		{
			ThrowHelper.ThrowIfArgumentNull(dict, "dict", null);
			TValue value;
			if (!dict.TryRemove(item.Key, out value))
			{
				return false;
			}
			if (EqualityComparer<TValue>.Default.Equals(item.Value, value))
			{
				return true;
			}
			dict.AddOrUpdate(item.Key, ([Nullable(1)] TKey _) => value, ([Nullable(1)] TKey _, TValue _) => value);
			return false;
		}
	}
}
