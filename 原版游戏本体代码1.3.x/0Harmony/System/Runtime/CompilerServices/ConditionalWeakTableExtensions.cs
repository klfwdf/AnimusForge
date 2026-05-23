using System;
using System.Collections.Generic;
using System.Reflection;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020004B1 RID: 1201
	[NullableContext(1)]
	[Nullable(0)]
	internal static class ConditionalWeakTableExtensions
	{
		// Token: 0x06001AF3 RID: 6899 RVA: 0x000577CC File Offset: 0x000559CC
		[return: Nullable(new byte[] { 1, 0, 1, 1 })]
		public static IEnumerable<KeyValuePair<TKey, TValue>> AsEnumerable<TKey, [Nullable(2)] TValue>(this ConditionalWeakTable<TKey, TValue> self) where TKey : class where TValue : class
		{
			ThrowHelper.ThrowIfArgumentNull(self, "self", null);
			IEnumerable<KeyValuePair<TKey, TValue>> enumerable = self as IEnumerable<KeyValuePair<TKey, TValue>>;
			if (enumerable != null)
			{
				return enumerable;
			}
			ICWTEnumerable<KeyValuePair<TKey, TValue>> cwt = self as ICWTEnumerable<KeyValuePair<TKey, TValue>>;
			if (cwt != null)
			{
				return cwt.SelfEnumerable;
			}
			return new CWTEnumerable<TKey, TValue>(self);
		}

		// Token: 0x06001AF4 RID: 6900 RVA: 0x00057808 File Offset: 0x00055A08
		[return: Nullable(new byte[] { 1, 0, 1, 1 })]
		public static IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator<TKey, [Nullable(2)] TValue>(this ConditionalWeakTable<TKey, TValue> self) where TKey : class where TValue : class
		{
			ThrowHelper.ThrowIfArgumentNull(self, "self", null);
			IEnumerable<KeyValuePair<TKey, TValue>> enumerable = self as IEnumerable<KeyValuePair<TKey, TValue>>;
			if (enumerable != null)
			{
				return enumerable.GetEnumerator();
			}
			ICWTEnumerable<KeyValuePair<TKey, TValue>> cwtEnum = self as ICWTEnumerable<KeyValuePair<TKey, TValue>>;
			if (cwtEnum != null)
			{
				return cwtEnum.GetEnumerator();
			}
			ConditionalWeakTableExtensions.CWTInfoHolder<TKey, TValue>.GetKeys getKeys = ConditionalWeakTableExtensions.CWTInfoHolder<TKey, TValue>.get_Keys;
			if (getKeys != null)
			{
				return ConditionalWeakTableExtensions.<GetEnumerator>g__Enumerate|2_0<TKey, TValue>(self, getKeys(self));
			}
			throw new PlatformNotSupportedException("Could not get Keys property of ConditionalWeakTable to enumerate it");
		}

		// Token: 0x06001AF5 RID: 6901 RVA: 0x00057864 File Offset: 0x00055A64
		public static void Clear<TKey, [Nullable(2)] TValue>(this ConditionalWeakTable<TKey, TValue> self) where TKey : class where TValue : class
		{
			ThrowHelper.ThrowIfArgumentNull(self, "self", null);
			using (IEnumerator<KeyValuePair<TKey, TValue>> enumerator = self.GetEnumerator<TKey, TValue>())
			{
				while (enumerator.MoveNext())
				{
					KeyValuePair<TKey, TValue> kvp = enumerator.Current;
					self.Remove(kvp.Key);
				}
			}
		}

		// Token: 0x06001AF6 RID: 6902 RVA: 0x000578C0 File Offset: 0x00055AC0
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

		// Token: 0x06001AF7 RID: 6903 RVA: 0x00057907 File Offset: 0x00055B07
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

		// Token: 0x020004B2 RID: 1202
		[NullableContext(0)]
		private static class CWTInfoHolder<[Nullable(1)] TKey, [Nullable(2)] TValue> where TKey : class where TValue : class
		{
			// Token: 0x06001AF8 RID: 6904 RVA: 0x00057920 File Offset: 0x00055B20
			static CWTInfoHolder()
			{
				PropertyInfo property = typeof(ConditionalWeakTable<TKey, TValue>).GetProperty("Keys", BindingFlags.Instance | BindingFlags.NonPublic);
				ConditionalWeakTableExtensions.CWTInfoHolder<TKey, TValue>.get_KeysMethod = ((property != null) ? property.GetGetMethod(true) : null);
				if (ConditionalWeakTableExtensions.CWTInfoHolder<TKey, TValue>.get_KeysMethod != null)
				{
					ConditionalWeakTableExtensions.CWTInfoHolder<TKey, TValue>.get_Keys = (ConditionalWeakTableExtensions.CWTInfoHolder<TKey, TValue>.GetKeys)Delegate.CreateDelegate(typeof(ConditionalWeakTableExtensions.CWTInfoHolder<TKey, TValue>.GetKeys), ConditionalWeakTableExtensions.CWTInfoHolder<TKey, TValue>.get_KeysMethod);
				}
			}

			// Token: 0x04001120 RID: 4384
			[Nullable(2)]
			private static readonly MethodInfo get_KeysMethod;

			// Token: 0x04001121 RID: 4385
			[Nullable(new byte[] { 2, 0, 0 })]
			public static readonly ConditionalWeakTableExtensions.CWTInfoHolder<TKey, TValue>.GetKeys get_Keys;

			// Token: 0x020004B3 RID: 1203
			// (Invoke) Token: 0x06001AFA RID: 6906
			public delegate IEnumerable<TKey> GetKeys(ConditionalWeakTable<TKey, TValue> cwt);
		}
	}
}
