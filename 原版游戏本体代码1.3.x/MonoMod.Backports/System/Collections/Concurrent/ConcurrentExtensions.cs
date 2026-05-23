using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Collections.Concurrent
{
	// Token: 0x02000027 RID: 39
	[NullableContext(1)]
	[Nullable(0)]
	public static class ConcurrentExtensions
	{
		// Token: 0x0600019E RID: 414 RVA: 0x00009BE8 File Offset: 0x00007DE8
		public static void Clear<[Nullable(2)] T>(this ConcurrentBag<T> bag)
		{
			ThrowHelper.ThrowIfArgumentNull(bag, "bag", null);
			T t;
			while (bag.TryTake(out t))
			{
			}
		}

		// Token: 0x0600019F RID: 415 RVA: 0x00009C0C File Offset: 0x00007E0C
		public static void Clear<[Nullable(2)] T>(this ConcurrentQueue<T> queue)
		{
			ThrowHelper.ThrowIfArgumentNull(queue, "queue", null);
			T t;
			while (queue.TryDequeue(out t))
			{
			}
		}

		// Token: 0x060001A0 RID: 416 RVA: 0x00009C30 File Offset: 0x00007E30
		public static TValue AddOrUpdate<TKey, [Nullable(2)] TValue, [Nullable(2)] TArg>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, Func<TKey, TArg, TValue> addValueFactory, Func<TKey, TValue, TArg, TValue> updateValueFactory, TArg factoryArgument)
		{
			ThrowHelper.ThrowIfArgumentNull(dict, "dict", null);
			return dict.AddOrUpdate(key, ([Nullable(1)] TKey k) => addValueFactory(k, factoryArgument), ([Nullable(1)] TKey k, TValue v) => updateValueFactory(k, v, factoryArgument));
		}

		// Token: 0x060001A1 RID: 417 RVA: 0x00009C84 File Offset: 0x00007E84
		public static TValue GetOrAdd<TKey, [Nullable(2)] TValue, [Nullable(2)] TArg>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
		{
			ThrowHelper.ThrowIfArgumentNull(dict, "dict", null);
			return dict.GetOrAdd(key, ([Nullable(1)] TKey k) => valueFactory(k, factoryArgument));
		}

		// Token: 0x060001A2 RID: 418 RVA: 0x00009CC4 File Offset: 0x00007EC4
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
