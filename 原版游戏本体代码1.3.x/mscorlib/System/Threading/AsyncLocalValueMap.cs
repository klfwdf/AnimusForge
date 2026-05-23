using System;
using System.Collections.Generic;

namespace System.Threading
{
	// Token: 0x020004E9 RID: 1257
	internal static class AsyncLocalValueMap
	{
		// Token: 0x17000905 RID: 2309
		// (get) Token: 0x06003B87 RID: 15239 RVA: 0x000E25B7 File Offset: 0x000E07B7
		public static IAsyncLocalValueMap Empty { get; } = new AsyncLocalValueMap.EmptyAsyncLocalValueMap();

		// Token: 0x06003B88 RID: 15240 RVA: 0x000E25BE File Offset: 0x000E07BE
		public static bool IsEmpty(IAsyncLocalValueMap asyncLocalValueMap)
		{
			return asyncLocalValueMap == AsyncLocalValueMap.Empty;
		}

		// Token: 0x06003B89 RID: 15241 RVA: 0x000E25C8 File Offset: 0x000E07C8
		public static IAsyncLocalValueMap Create(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
		{
			if (value == null && treatNullValueAsNonexistent)
			{
				return AsyncLocalValueMap.Empty;
			}
			return new AsyncLocalValueMap.OneElementAsyncLocalValueMap(key, value);
		}

		// Token: 0x02000BE6 RID: 3046
		private sealed class EmptyAsyncLocalValueMap : IAsyncLocalValueMap
		{
			// Token: 0x06006F3F RID: 28479 RVA: 0x0017F214 File Offset: 0x0017D414
			public IAsyncLocalValueMap Set(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
			{
				if (value == null && treatNullValueAsNonexistent)
				{
					return this;
				}
				return new AsyncLocalValueMap.OneElementAsyncLocalValueMap(key, value);
			}

			// Token: 0x06006F40 RID: 28480 RVA: 0x0017F234 File Offset: 0x0017D434
			public bool TryGetValue(IAsyncLocal key, out object value)
			{
				value = null;
				return false;
			}
		}

		// Token: 0x02000BE7 RID: 3047
		private sealed class OneElementAsyncLocalValueMap : IAsyncLocalValueMap
		{
			// Token: 0x06006F42 RID: 28482 RVA: 0x0017F242 File Offset: 0x0017D442
			public OneElementAsyncLocalValueMap(IAsyncLocal key, object value)
			{
				this._key1 = key;
				this._value1 = value;
			}

			// Token: 0x06006F43 RID: 28483 RVA: 0x0017F258 File Offset: 0x0017D458
			public IAsyncLocalValueMap Set(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
			{
				if (value != null || !treatNullValueAsNonexistent)
				{
					if (key != this._key1)
					{
						return new AsyncLocalValueMap.TwoElementAsyncLocalValueMap(this._key1, this._value1, key, value);
					}
					return new AsyncLocalValueMap.OneElementAsyncLocalValueMap(key, value);
				}
				else
				{
					if (key != this._key1)
					{
						return this;
					}
					return AsyncLocalValueMap.Empty;
				}
			}

			// Token: 0x06006F44 RID: 28484 RVA: 0x0017F2A6 File Offset: 0x0017D4A6
			public bool TryGetValue(IAsyncLocal key, out object value)
			{
				if (key == this._key1)
				{
					value = this._value1;
					return true;
				}
				value = null;
				return false;
			}

			// Token: 0x04003600 RID: 13824
			private readonly IAsyncLocal _key1;

			// Token: 0x04003601 RID: 13825
			private readonly object _value1;
		}

		// Token: 0x02000BE8 RID: 3048
		private sealed class TwoElementAsyncLocalValueMap : IAsyncLocalValueMap
		{
			// Token: 0x06006F45 RID: 28485 RVA: 0x0017F2BF File Offset: 0x0017D4BF
			public TwoElementAsyncLocalValueMap(IAsyncLocal key1, object value1, IAsyncLocal key2, object value2)
			{
				this._key1 = key1;
				this._value1 = value1;
				this._key2 = key2;
				this._value2 = value2;
			}

			// Token: 0x06006F46 RID: 28486 RVA: 0x0017F2E4 File Offset: 0x0017D4E4
			public IAsyncLocalValueMap Set(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
			{
				if (value != null || !treatNullValueAsNonexistent)
				{
					if (key == this._key1)
					{
						return new AsyncLocalValueMap.TwoElementAsyncLocalValueMap(key, value, this._key2, this._value2);
					}
					if (key != this._key2)
					{
						return new AsyncLocalValueMap.ThreeElementAsyncLocalValueMap(this._key1, this._value1, this._key2, this._value2, key, value);
					}
					return new AsyncLocalValueMap.TwoElementAsyncLocalValueMap(this._key1, this._value1, key, value);
				}
				else
				{
					if (key == this._key1)
					{
						return new AsyncLocalValueMap.OneElementAsyncLocalValueMap(this._key2, this._value2);
					}
					if (key != this._key2)
					{
						return this;
					}
					return new AsyncLocalValueMap.OneElementAsyncLocalValueMap(this._key1, this._value1);
				}
			}

			// Token: 0x06006F47 RID: 28487 RVA: 0x0017F394 File Offset: 0x0017D594
			public bool TryGetValue(IAsyncLocal key, out object value)
			{
				if (key == this._key1)
				{
					value = this._value1;
					return true;
				}
				if (key == this._key2)
				{
					value = this._value2;
					return true;
				}
				value = null;
				return false;
			}

			// Token: 0x04003602 RID: 13826
			private readonly IAsyncLocal _key1;

			// Token: 0x04003603 RID: 13827
			private readonly IAsyncLocal _key2;

			// Token: 0x04003604 RID: 13828
			private readonly object _value1;

			// Token: 0x04003605 RID: 13829
			private readonly object _value2;
		}

		// Token: 0x02000BE9 RID: 3049
		private sealed class ThreeElementAsyncLocalValueMap : IAsyncLocalValueMap
		{
			// Token: 0x06006F48 RID: 28488 RVA: 0x0017F3C0 File Offset: 0x0017D5C0
			public ThreeElementAsyncLocalValueMap(IAsyncLocal key1, object value1, IAsyncLocal key2, object value2, IAsyncLocal key3, object value3)
			{
				this._key1 = key1;
				this._value1 = value1;
				this._key2 = key2;
				this._value2 = value2;
				this._key3 = key3;
				this._value3 = value3;
			}

			// Token: 0x06006F49 RID: 28489 RVA: 0x0017F3F8 File Offset: 0x0017D5F8
			public IAsyncLocalValueMap Set(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
			{
				if (value != null || !treatNullValueAsNonexistent)
				{
					if (key == this._key1)
					{
						return new AsyncLocalValueMap.ThreeElementAsyncLocalValueMap(key, value, this._key2, this._value2, this._key3, this._value3);
					}
					if (key == this._key2)
					{
						return new AsyncLocalValueMap.ThreeElementAsyncLocalValueMap(this._key1, this._value1, key, value, this._key3, this._value3);
					}
					if (key == this._key3)
					{
						return new AsyncLocalValueMap.ThreeElementAsyncLocalValueMap(this._key1, this._value1, this._key2, this._value2, key, value);
					}
					AsyncLocalValueMap.MultiElementAsyncLocalValueMap multiElementAsyncLocalValueMap = new AsyncLocalValueMap.MultiElementAsyncLocalValueMap(4);
					multiElementAsyncLocalValueMap.UnsafeStore(0, this._key1, this._value1);
					multiElementAsyncLocalValueMap.UnsafeStore(1, this._key2, this._value2);
					multiElementAsyncLocalValueMap.UnsafeStore(2, this._key3, this._value3);
					multiElementAsyncLocalValueMap.UnsafeStore(3, key, value);
					return multiElementAsyncLocalValueMap;
				}
				else
				{
					if (key == this._key1)
					{
						return new AsyncLocalValueMap.TwoElementAsyncLocalValueMap(this._key2, this._value2, this._key3, this._value3);
					}
					if (key == this._key2)
					{
						return new AsyncLocalValueMap.TwoElementAsyncLocalValueMap(this._key1, this._value1, this._key3, this._value3);
					}
					if (key != this._key3)
					{
						return this;
					}
					return new AsyncLocalValueMap.TwoElementAsyncLocalValueMap(this._key1, this._value1, this._key2, this._value2);
				}
			}

			// Token: 0x06006F4A RID: 28490 RVA: 0x0017F552 File Offset: 0x0017D752
			public bool TryGetValue(IAsyncLocal key, out object value)
			{
				if (key == this._key1)
				{
					value = this._value1;
					return true;
				}
				if (key == this._key2)
				{
					value = this._value2;
					return true;
				}
				if (key == this._key3)
				{
					value = this._value3;
					return true;
				}
				value = null;
				return false;
			}

			// Token: 0x04003606 RID: 13830
			private readonly IAsyncLocal _key1;

			// Token: 0x04003607 RID: 13831
			private readonly IAsyncLocal _key2;

			// Token: 0x04003608 RID: 13832
			private readonly IAsyncLocal _key3;

			// Token: 0x04003609 RID: 13833
			private readonly object _value1;

			// Token: 0x0400360A RID: 13834
			private readonly object _value2;

			// Token: 0x0400360B RID: 13835
			private readonly object _value3;
		}

		// Token: 0x02000BEA RID: 3050
		private sealed class MultiElementAsyncLocalValueMap : IAsyncLocalValueMap
		{
			// Token: 0x06006F4B RID: 28491 RVA: 0x0017F591 File Offset: 0x0017D791
			internal MultiElementAsyncLocalValueMap(int count)
			{
				this._keyValues = new KeyValuePair<IAsyncLocal, object>[count];
			}

			// Token: 0x06006F4C RID: 28492 RVA: 0x0017F5A5 File Offset: 0x0017D7A5
			internal void UnsafeStore(int index, IAsyncLocal key, object value)
			{
				this._keyValues[index] = new KeyValuePair<IAsyncLocal, object>(key, value);
			}

			// Token: 0x06006F4D RID: 28493 RVA: 0x0017F5BC File Offset: 0x0017D7BC
			public IAsyncLocalValueMap Set(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
			{
				int i = 0;
				while (i < this._keyValues.Length)
				{
					if (key == this._keyValues[i].Key)
					{
						if (value != null || !treatNullValueAsNonexistent)
						{
							AsyncLocalValueMap.MultiElementAsyncLocalValueMap multiElementAsyncLocalValueMap = new AsyncLocalValueMap.MultiElementAsyncLocalValueMap(this._keyValues.Length);
							Array.Copy(this._keyValues, 0, multiElementAsyncLocalValueMap._keyValues, 0, this._keyValues.Length);
							multiElementAsyncLocalValueMap._keyValues[i] = new KeyValuePair<IAsyncLocal, object>(key, value);
							return multiElementAsyncLocalValueMap;
						}
						if (this._keyValues.Length != 4)
						{
							AsyncLocalValueMap.MultiElementAsyncLocalValueMap multiElementAsyncLocalValueMap2 = new AsyncLocalValueMap.MultiElementAsyncLocalValueMap(this._keyValues.Length - 1);
							if (i != 0)
							{
								Array.Copy(this._keyValues, 0, multiElementAsyncLocalValueMap2._keyValues, 0, i);
							}
							if (i != this._keyValues.Length - 1)
							{
								Array.Copy(this._keyValues, i + 1, multiElementAsyncLocalValueMap2._keyValues, i, this._keyValues.Length - i - 1);
							}
							return multiElementAsyncLocalValueMap2;
						}
						if (i == 0)
						{
							return new AsyncLocalValueMap.ThreeElementAsyncLocalValueMap(this._keyValues[1].Key, this._keyValues[1].Value, this._keyValues[2].Key, this._keyValues[2].Value, this._keyValues[3].Key, this._keyValues[3].Value);
						}
						if (i == 1)
						{
							return new AsyncLocalValueMap.ThreeElementAsyncLocalValueMap(this._keyValues[0].Key, this._keyValues[0].Value, this._keyValues[2].Key, this._keyValues[2].Value, this._keyValues[3].Key, this._keyValues[3].Value);
						}
						if (i != 2)
						{
							return new AsyncLocalValueMap.ThreeElementAsyncLocalValueMap(this._keyValues[0].Key, this._keyValues[0].Value, this._keyValues[1].Key, this._keyValues[1].Value, this._keyValues[2].Key, this._keyValues[2].Value);
						}
						return new AsyncLocalValueMap.ThreeElementAsyncLocalValueMap(this._keyValues[0].Key, this._keyValues[0].Value, this._keyValues[1].Key, this._keyValues[1].Value, this._keyValues[3].Key, this._keyValues[3].Value);
					}
					else
					{
						i++;
					}
				}
				if (value == null && treatNullValueAsNonexistent)
				{
					return this;
				}
				if (this._keyValues.Length < 16)
				{
					AsyncLocalValueMap.MultiElementAsyncLocalValueMap multiElementAsyncLocalValueMap3 = new AsyncLocalValueMap.MultiElementAsyncLocalValueMap(this._keyValues.Length + 1);
					Array.Copy(this._keyValues, 0, multiElementAsyncLocalValueMap3._keyValues, 0, this._keyValues.Length);
					multiElementAsyncLocalValueMap3._keyValues[this._keyValues.Length] = new KeyValuePair<IAsyncLocal, object>(key, value);
					return multiElementAsyncLocalValueMap3;
				}
				AsyncLocalValueMap.ManyElementAsyncLocalValueMap manyElementAsyncLocalValueMap = new AsyncLocalValueMap.ManyElementAsyncLocalValueMap(17);
				foreach (KeyValuePair<IAsyncLocal, object> keyValuePair in this._keyValues)
				{
					manyElementAsyncLocalValueMap[keyValuePair.Key] = keyValuePair.Value;
				}
				manyElementAsyncLocalValueMap[key] = value;
				return manyElementAsyncLocalValueMap;
			}

			// Token: 0x06006F4E RID: 28494 RVA: 0x0017F91C File Offset: 0x0017DB1C
			public bool TryGetValue(IAsyncLocal key, out object value)
			{
				foreach (KeyValuePair<IAsyncLocal, object> keyValuePair in this._keyValues)
				{
					if (key == keyValuePair.Key)
					{
						value = keyValuePair.Value;
						return true;
					}
				}
				value = null;
				return false;
			}

			// Token: 0x0400360C RID: 13836
			internal const int MaxMultiElements = 16;

			// Token: 0x0400360D RID: 13837
			private readonly KeyValuePair<IAsyncLocal, object>[] _keyValues;
		}

		// Token: 0x02000BEB RID: 3051
		private sealed class ManyElementAsyncLocalValueMap : Dictionary<IAsyncLocal, object>, IAsyncLocalValueMap
		{
			// Token: 0x06006F4F RID: 28495 RVA: 0x0017F95F File Offset: 0x0017DB5F
			public ManyElementAsyncLocalValueMap(int capacity)
				: base(capacity)
			{
			}

			// Token: 0x06006F50 RID: 28496 RVA: 0x0017F968 File Offset: 0x0017DB68
			public IAsyncLocalValueMap Set(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
			{
				int count = base.Count;
				bool flag = base.ContainsKey(key);
				if (value != null || !treatNullValueAsNonexistent)
				{
					AsyncLocalValueMap.ManyElementAsyncLocalValueMap manyElementAsyncLocalValueMap = new AsyncLocalValueMap.ManyElementAsyncLocalValueMap(count + (flag ? 0 : 1));
					foreach (KeyValuePair<IAsyncLocal, object> keyValuePair in this)
					{
						manyElementAsyncLocalValueMap[keyValuePair.Key] = keyValuePair.Value;
					}
					manyElementAsyncLocalValueMap[key] = value;
					return manyElementAsyncLocalValueMap;
				}
				if (!flag)
				{
					return this;
				}
				if (count == 17)
				{
					AsyncLocalValueMap.MultiElementAsyncLocalValueMap multiElementAsyncLocalValueMap = new AsyncLocalValueMap.MultiElementAsyncLocalValueMap(16);
					int num = 0;
					foreach (KeyValuePair<IAsyncLocal, object> keyValuePair2 in this)
					{
						if (key != keyValuePair2.Key)
						{
							multiElementAsyncLocalValueMap.UnsafeStore(num++, keyValuePair2.Key, keyValuePair2.Value);
						}
					}
					return multiElementAsyncLocalValueMap;
				}
				AsyncLocalValueMap.ManyElementAsyncLocalValueMap manyElementAsyncLocalValueMap2 = new AsyncLocalValueMap.ManyElementAsyncLocalValueMap(count - 1);
				foreach (KeyValuePair<IAsyncLocal, object> keyValuePair3 in this)
				{
					if (key != keyValuePair3.Key)
					{
						manyElementAsyncLocalValueMap2[keyValuePair3.Key] = keyValuePair3.Value;
					}
				}
				return manyElementAsyncLocalValueMap2;
			}
		}
	}
}
