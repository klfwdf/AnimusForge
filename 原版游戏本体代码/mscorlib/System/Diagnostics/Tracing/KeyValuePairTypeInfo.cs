using System;
using System.Collections.Generic;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200047E RID: 1150
	internal sealed class KeyValuePairTypeInfo<K, V> : TraceLoggingTypeInfo<KeyValuePair<K, V>>
	{
		// Token: 0x060036F0 RID: 14064 RVA: 0x000D3AB1 File Offset: 0x000D1CB1
		public KeyValuePairTypeInfo(List<Type> recursionCheck)
		{
			this.keyInfo = TraceLoggingTypeInfo<K>.GetInstance(recursionCheck);
			this.valueInfo = TraceLoggingTypeInfo<V>.GetInstance(recursionCheck);
		}

		// Token: 0x060036F1 RID: 14065 RVA: 0x000D3AD4 File Offset: 0x000D1CD4
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			TraceLoggingMetadataCollector collector2 = collector.AddGroup(name);
			this.keyInfo.WriteMetadata(collector2, "Key", EventFieldFormat.Default);
			this.valueInfo.WriteMetadata(collector2, "Value", format);
		}

		// Token: 0x060036F2 RID: 14066 RVA: 0x000D3B10 File Offset: 0x000D1D10
		public override void WriteData(TraceLoggingDataCollector collector, ref KeyValuePair<K, V> value)
		{
			K key = value.Key;
			V value2 = value.Value;
			this.keyInfo.WriteData(collector, ref key);
			this.valueInfo.WriteData(collector, ref value2);
		}

		// Token: 0x060036F3 RID: 14067 RVA: 0x000D3B48 File Offset: 0x000D1D48
		public override object GetData(object value)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			KeyValuePair<K, V> keyValuePair = (KeyValuePair<K, V>)value;
			dictionary.Add("Key", this.keyInfo.GetData(keyValuePair.Key));
			dictionary.Add("Value", this.valueInfo.GetData(keyValuePair.Value));
			return dictionary;
		}

		// Token: 0x0400185D RID: 6237
		private readonly TraceLoggingTypeInfo<K> keyInfo;

		// Token: 0x0400185E RID: 6238
		private readonly TraceLoggingTypeInfo<V> valueInfo;
	}
}
