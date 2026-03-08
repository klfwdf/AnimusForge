using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000486 RID: 1158
	internal abstract class TraceLoggingTypeInfo
	{
		// Token: 0x06003757 RID: 14167 RVA: 0x000D5162 File Offset: 0x000D3362
		internal TraceLoggingTypeInfo(Type dataType)
		{
			if (dataType == null)
			{
				throw new ArgumentNullException("dataType");
			}
			this.name = dataType.Name;
			this.dataType = dataType;
		}

		// Token: 0x06003758 RID: 14168 RVA: 0x000D51A0 File Offset: 0x000D33A0
		internal TraceLoggingTypeInfo(Type dataType, string name, EventLevel level, EventOpcode opcode, EventKeywords keywords, EventTags tags)
		{
			if (dataType == null)
			{
				throw new ArgumentNullException("dataType");
			}
			if (name == null)
			{
				throw new ArgumentNullException("eventName");
			}
			Statics.CheckName(name);
			this.name = name;
			this.keywords = keywords;
			this.level = level;
			this.opcode = opcode;
			this.tags = tags;
			this.dataType = dataType;
		}

		// Token: 0x17000817 RID: 2071
		// (get) Token: 0x06003759 RID: 14169 RVA: 0x000D5216 File Offset: 0x000D3416
		public string Name
		{
			get
			{
				return this.name;
			}
		}

		// Token: 0x17000818 RID: 2072
		// (get) Token: 0x0600375A RID: 14170 RVA: 0x000D521E File Offset: 0x000D341E
		public EventLevel Level
		{
			get
			{
				return this.level;
			}
		}

		// Token: 0x17000819 RID: 2073
		// (get) Token: 0x0600375B RID: 14171 RVA: 0x000D5226 File Offset: 0x000D3426
		public EventOpcode Opcode
		{
			get
			{
				return this.opcode;
			}
		}

		// Token: 0x1700081A RID: 2074
		// (get) Token: 0x0600375C RID: 14172 RVA: 0x000D522E File Offset: 0x000D342E
		public EventKeywords Keywords
		{
			get
			{
				return this.keywords;
			}
		}

		// Token: 0x1700081B RID: 2075
		// (get) Token: 0x0600375D RID: 14173 RVA: 0x000D5236 File Offset: 0x000D3436
		public EventTags Tags
		{
			get
			{
				return this.tags;
			}
		}

		// Token: 0x1700081C RID: 2076
		// (get) Token: 0x0600375E RID: 14174 RVA: 0x000D523E File Offset: 0x000D343E
		internal Type DataType
		{
			get
			{
				return this.dataType;
			}
		}

		// Token: 0x0600375F RID: 14175
		public abstract void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format);

		// Token: 0x06003760 RID: 14176
		public abstract void WriteObjectData(TraceLoggingDataCollector collector, object value);

		// Token: 0x06003761 RID: 14177 RVA: 0x000D5246 File Offset: 0x000D3446
		public virtual object GetData(object value)
		{
			return value;
		}

		// Token: 0x040018A6 RID: 6310
		private readonly string name;

		// Token: 0x040018A7 RID: 6311
		private readonly EventKeywords keywords;

		// Token: 0x040018A8 RID: 6312
		private readonly EventLevel level = (EventLevel)(-1);

		// Token: 0x040018A9 RID: 6313
		private readonly EventOpcode opcode = (EventOpcode)(-1);

		// Token: 0x040018AA RID: 6314
		private readonly EventTags tags;

		// Token: 0x040018AB RID: 6315
		private readonly Type dataType;
	}
}
