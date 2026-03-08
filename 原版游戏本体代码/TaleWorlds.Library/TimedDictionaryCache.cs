using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TaleWorlds.Library
{
	// Token: 0x02000095 RID: 149
	public class TimedDictionaryCache<TKey, TValue>
	{
		// Token: 0x0600054F RID: 1359 RVA: 0x00012F09 File Offset: 0x00011109
		public TimedDictionaryCache(long validMilliseconds)
		{
			this._dictionary = new Dictionary<TKey, ValueTuple<long, TValue>>();
			this._stopwatch = new Stopwatch();
			this._stopwatch.Start();
			this._validMilliseconds = validMilliseconds;
		}

		// Token: 0x06000550 RID: 1360 RVA: 0x00012F39 File Offset: 0x00011139
		public TimedDictionaryCache(TimeSpan validTimeSpan)
			: this((long)validTimeSpan.TotalMilliseconds)
		{
		}

		// Token: 0x06000551 RID: 1361 RVA: 0x00012F49 File Offset: 0x00011149
		private bool IsItemExpired(TKey key)
		{
			return this._stopwatch.ElapsedMilliseconds - this._dictionary[key].Item1 >= this._validMilliseconds;
		}

		// Token: 0x06000552 RID: 1362 RVA: 0x00012F73 File Offset: 0x00011173
		private bool RemoveIfExpired(TKey key)
		{
			if (this.IsItemExpired(key))
			{
				this._dictionary.Remove(key);
				return true;
			}
			return false;
		}

		// Token: 0x06000553 RID: 1363 RVA: 0x00012F90 File Offset: 0x00011190
		public void PruneExpiredItems()
		{
			List<TKey> list = new List<TKey>();
			foreach (KeyValuePair<TKey, ValueTuple<long, TValue>> keyValuePair in this._dictionary)
			{
				if (this.IsItemExpired(keyValuePair.Key))
				{
					list.Add(keyValuePair.Key);
				}
			}
			foreach (TKey key in list)
			{
				this._dictionary.Remove(key);
			}
		}

		// Token: 0x06000554 RID: 1364 RVA: 0x00013044 File Offset: 0x00011244
		public void Clear()
		{
			this._dictionary.Clear();
		}

		// Token: 0x06000555 RID: 1365 RVA: 0x00013051 File Offset: 0x00011251
		public bool ContainsKey(TKey key)
		{
			return this._dictionary.ContainsKey(key) && !this.RemoveIfExpired(key);
		}

		// Token: 0x06000556 RID: 1366 RVA: 0x0001306D File Offset: 0x0001126D
		public bool Remove(TKey key)
		{
			this.RemoveIfExpired(key);
			return this._dictionary.Remove(key);
		}

		// Token: 0x06000557 RID: 1367 RVA: 0x00013083 File Offset: 0x00011283
		public bool TryGetValue(TKey key, out TValue value)
		{
			if (this.ContainsKey(key))
			{
				value = this._dictionary[key].Item2;
				return true;
			}
			value = default(TValue);
			return false;
		}

		// Token: 0x17000092 RID: 146
		public TValue this[TKey key]
		{
			get
			{
				this.RemoveIfExpired(key);
				return this._dictionary[key].Item2;
			}
			set
			{
				this._dictionary[key] = new ValueTuple<long, TValue>(this._stopwatch.ElapsedMilliseconds, value);
			}
		}

		// Token: 0x0600055A RID: 1370 RVA: 0x000130EC File Offset: 0x000112EC
		public MBReadOnlyDictionary<TKey, TValue> AsReadOnlyDictionary()
		{
			this.PruneExpiredItems();
			Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
			foreach (KeyValuePair<TKey, ValueTuple<long, TValue>> keyValuePair in this._dictionary)
			{
				dictionary[keyValuePair.Key] = keyValuePair.Value.Item2;
			}
			return dictionary.GetReadOnlyDictionary<TKey, TValue>();
		}

		// Token: 0x040001A7 RID: 423
		[TupleElementNames(new string[] { "Timestamp", "Value" })]
		private readonly Dictionary<TKey, ValueTuple<long, TValue>> _dictionary;

		// Token: 0x040001A8 RID: 424
		private readonly Stopwatch _stopwatch;

		// Token: 0x040001A9 RID: 425
		private readonly long _validMilliseconds;
	}
}
