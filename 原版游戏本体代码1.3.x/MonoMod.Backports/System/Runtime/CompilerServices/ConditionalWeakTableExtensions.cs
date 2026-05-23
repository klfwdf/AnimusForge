using System;
using System.Collections.Generic;
using System.Reflection;

namespace System.Runtime.CompilerServices
{
	// Token: 0x02000040 RID: 64
	[NullableContext(1)]
	[Nullable(0)]
	public static class ConditionalWeakTableExtensions
	{
		// Token: 0x06000276 RID: 630 RVA: 0x0000C0A4 File Offset: 0x0000A2A4
		[return: Nullable(new byte[] { 1, 0, 1, 1 })]
		public static IEnumerable<KeyValuePair<TKey, TValue>> AsEnumerable<TKey, [Nullable(2)] TValue>(this ConditionalWeakTable<TKey, TValue> self) where TKey : class where TValue : class
		{
			ThrowHelper.ThrowIfArgumentNull(self, "self", null);
			IEnumerable<KeyValuePair<TKey, TValue>> enumerable = self as IEnumerable<KeyValuePair<TKey, TValue>>;
			if (enumerable != null)
			{
				return enumerable;
			}
			ICWTEnumerable<KeyValuePair<TKey, TValue>> icwtenumerable = self as ICWTEnumerable<KeyValuePair<TKey, TValue>>;
			if (icwtenumerable != null)
			{
				return icwtenumerable.SelfEnumerable;
			}
			return new CWTEnumerable<TKey, TValue>(self);
		}

		// Token: 0x06000277 RID: 631 RVA: 0x0000C0E0 File Offset: 0x0000A2E0
		[return: Nullable(new byte[] { 1, 0, 1, 1 })]
		public static IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator<TKey, [Nullable(2)] TValue>(this ConditionalWeakTable<TKey, TValue> self) where TKey : class where TValue : class
		{
			ThrowHelper.ThrowIfArgumentNull(self, "self", null);
			IEnumerable<KeyValuePair<TKey, TValue>> enumerable = self as IEnumerable<KeyValuePair<TKey, TValue>>;
			if (enumerable != null)
			{
				return enumerable.GetEnumerator();
			}
			ICWTEnumerable<KeyValuePair<TKey, TValue>> icwtenumerable = self as ICWTEnumerable<KeyValuePair<TKey, TValue>>;
			if (icwtenumerable != null)
			{
				return icwtenumerable.GetEnumerator();
			}
			ConditionalWeakTableExtensions.CWTInfoHolder<TKey, TValue>.GetKeys get_Keys = ConditionalWeakTableExtensions.CWTInfoHolder<TKey, TValue>.get_Keys;
			if (get_Keys != null)
			{
				return ConditionalWeakTableExtensions.<GetEnumerator>g__Enumerate|2_0<TKey, TValue>(self, get_Keys(self));
			}
			throw new PlatformNotSupportedException("Could not get Keys property of ConditionalWeakTable to enumerate it");
		}

		// Token: 0x06000278 RID: 632 RVA: 0x0000C13C File Offset: 0x0000A33C
		public static void Clear<TKey, [Nullable(2)] TValue>(this ConditionalWeakTable<TKey, TValue> self) where TKey : class where TValue : class
		{
			ThrowHelper.ThrowIfArgumentNull(self, "self", null);
			using (IEnumerator<KeyValuePair<TKey, TValue>> enumerator = self.GetEnumerator<TKey, TValue>())
			{
				while (enumerator.MoveNext())
				{
					KeyValuePair<TKey, TValue> keyValuePair = enumerator.Current;
					self.Remove(keyValuePair.Key);
				}
			}
		}

		// Token: 0x06000279 RID: 633 RVA: 0x0000C198 File Offset: 0x0000A398
		public static bool TryAdd<TKey, [Nullable(2)] TValue>(this ConditionalWeakTable<TKey, TValue> self, TKey key, TValue value) where TKey : class where TValue : class
		{
			ThrowHelper.ThrowIfArgumentNull(self, "self", null);
			bool didAdd = false;
			self.GetValue(key, delegate([Nullable(1)] TKey _)
			{
				didAdd = true;
				return value;
			});
			return didAdd;
		}

		// Token: 0x0600027A RID: 634 RVA: 0x0000C1DF File Offset: 0x0000A3DF
		[CompilerGenerated]
		[return: Nullable(new byte[] { 1, 0, 1, 0 })]
		internal static IEnumerator<KeyValuePair<TKey, TValue>> <GetEnumerator>g__Enumerate|2_0<TKey, TValue>([Nullable(new byte[] { 1, 1, 0 })] ConditionalWeakTable<TKey, TValue> cwt, IEnumerable<TKey> keys) where TKey : class where TValue : class
		{
			foreach (TKey key in keys)
			{
				TValue value;
				if (cwt.TryGetValue(key, out value))
				{
					yield return new KeyValuePair<TKey, TValue>(key, value);
				}
			}
			IEnumerator<TKey> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x0200006E RID: 110
		[NullableContext(0)]
		private static class CWTInfoHolder<[Nullable(1)] TKey, [Nullable(2)] TValue> where TKey : class where TValue : class
		{
			// Token: 0x060002F2 RID: 754 RVA: 0x0000D534 File Offset: 0x0000B734
			static CWTInfoHolder()
			{
				PropertyInfo property = typeof(ConditionalWeakTable<TKey, TValue>).GetProperty("Keys", BindingFlags.Instance | BindingFlags.NonPublic);
				ConditionalWeakTableExtensions.CWTInfoHolder<TKey, TValue>.get_KeysMethod = ((property != null) ? property.GetGetMethod(true) : null);
				if (ConditionalWeakTableExtensions.CWTInfoHolder<TKey, TValue>.get_KeysMethod != null)
				{
					ConditionalWeakTableExtensions.CWTInfoHolder<TKey, TValue>.get_Keys = (ConditionalWeakTableExtensions.CWTInfoHolder<TKey, TValue>.GetKeys)Delegate.CreateDelegate(typeof(ConditionalWeakTableExtensions.CWTInfoHolder<TKey, TValue>.GetKeys), ConditionalWeakTableExtensions.CWTInfoHolder<TKey, TValue>.get_KeysMethod);
				}
			}

			// Token: 0x040000D5 RID: 213
			[Nullable(2)]
			private static readonly MethodInfo get_KeysMethod;

			// Token: 0x040000D6 RID: 214
			[Nullable(new byte[] { 2, 0, 0 })]
			public static readonly ConditionalWeakTableExtensions.CWTInfoHolder<TKey, TValue>.GetKeys get_Keys;

			// Token: 0x02000073 RID: 115
			// (Invoke) Token: 0x060002FD RID: 765
			public delegate IEnumerable<TKey> GetKeys(ConditionalWeakTable<TKey, TValue> cwt);
		}
	}
}
