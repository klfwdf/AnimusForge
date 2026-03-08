using System;
using System.Collections.Generic;
using System.Threading;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000487 RID: 1159
	internal abstract class TraceLoggingTypeInfo<DataType> : TraceLoggingTypeInfo
	{
		// Token: 0x06003762 RID: 14178 RVA: 0x000D5249 File Offset: 0x000D3449
		protected TraceLoggingTypeInfo()
			: base(typeof(DataType))
		{
		}

		// Token: 0x06003763 RID: 14179 RVA: 0x000D525B File Offset: 0x000D345B
		protected TraceLoggingTypeInfo(string name, EventLevel level, EventOpcode opcode, EventKeywords keywords, EventTags tags)
			: base(typeof(DataType), name, level, opcode, keywords, tags)
		{
		}

		// Token: 0x1700081D RID: 2077
		// (get) Token: 0x06003764 RID: 14180 RVA: 0x000D5274 File Offset: 0x000D3474
		public static TraceLoggingTypeInfo<DataType> Instance
		{
			get
			{
				return TraceLoggingTypeInfo<DataType>.instance ?? TraceLoggingTypeInfo<DataType>.InitInstance();
			}
		}

		// Token: 0x06003765 RID: 14181
		public abstract void WriteData(TraceLoggingDataCollector collector, ref DataType value);

		// Token: 0x06003766 RID: 14182 RVA: 0x000D5284 File Offset: 0x000D3484
		public override void WriteObjectData(TraceLoggingDataCollector collector, object value)
		{
			DataType dataType = ((value == null) ? default(DataType) : ((DataType)((object)value)));
			this.WriteData(collector, ref dataType);
		}

		// Token: 0x06003767 RID: 14183 RVA: 0x000D52B0 File Offset: 0x000D34B0
		internal static TraceLoggingTypeInfo<DataType> GetInstance(List<Type> recursionCheck)
		{
			if (TraceLoggingTypeInfo<DataType>.instance == null)
			{
				int count = recursionCheck.Count;
				TraceLoggingTypeInfo<DataType> value = Statics.CreateDefaultTypeInfo<DataType>(recursionCheck);
				Interlocked.CompareExchange<TraceLoggingTypeInfo<DataType>>(ref TraceLoggingTypeInfo<DataType>.instance, value, null);
				recursionCheck.RemoveRange(count, recursionCheck.Count - count);
			}
			return TraceLoggingTypeInfo<DataType>.instance;
		}

		// Token: 0x06003768 RID: 14184 RVA: 0x000D52F3 File Offset: 0x000D34F3
		private static TraceLoggingTypeInfo<DataType> InitInstance()
		{
			return TraceLoggingTypeInfo<DataType>.GetInstance(new List<Type>());
		}

		// Token: 0x040018AC RID: 6316
		private static TraceLoggingTypeInfo<DataType> instance;
	}
}
