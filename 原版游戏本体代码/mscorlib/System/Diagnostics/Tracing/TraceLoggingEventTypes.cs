using System;
using System.Collections.Generic;
using System.Reflection;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000484 RID: 1156
	internal class TraceLoggingEventTypes
	{
		// Token: 0x06003739 RID: 14137 RVA: 0x000D4A8D File Offset: 0x000D2C8D
		internal TraceLoggingEventTypes(string name, EventTags tags, params Type[] types)
			: this(tags, name, TraceLoggingEventTypes.MakeArray(types))
		{
		}

		// Token: 0x0600373A RID: 14138 RVA: 0x000D4A9D File Offset: 0x000D2C9D
		internal TraceLoggingEventTypes(string name, EventTags tags, params TraceLoggingTypeInfo[] typeInfos)
			: this(tags, name, TraceLoggingEventTypes.MakeArray(typeInfos))
		{
		}

		// Token: 0x0600373B RID: 14139 RVA: 0x000D4AB0 File Offset: 0x000D2CB0
		internal TraceLoggingEventTypes(string name, EventTags tags, ParameterInfo[] paramInfos)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			this.typeInfos = this.MakeArray(paramInfos);
			this.name = name;
			this.tags = tags;
			this.level = 5;
			TraceLoggingMetadataCollector traceLoggingMetadataCollector = new TraceLoggingMetadataCollector();
			for (int i = 0; i < this.typeInfos.Length; i++)
			{
				TraceLoggingTypeInfo traceLoggingTypeInfo = this.typeInfos[i];
				this.level = Statics.Combine((int)traceLoggingTypeInfo.Level, this.level);
				this.opcode = Statics.Combine((int)traceLoggingTypeInfo.Opcode, this.opcode);
				this.keywords |= traceLoggingTypeInfo.Keywords;
				string fieldName = paramInfos[i].Name;
				if (Statics.ShouldOverrideFieldName(fieldName))
				{
					fieldName = traceLoggingTypeInfo.Name;
				}
				traceLoggingTypeInfo.WriteMetadata(traceLoggingMetadataCollector, fieldName, EventFieldFormat.Default);
			}
			this.typeMetadata = traceLoggingMetadataCollector.GetMetadata();
			this.scratchSize = traceLoggingMetadataCollector.ScratchSize;
			this.dataCount = traceLoggingMetadataCollector.DataCount;
			this.pinCount = traceLoggingMetadataCollector.PinCount;
		}

		// Token: 0x0600373C RID: 14140 RVA: 0x000D4BA8 File Offset: 0x000D2DA8
		private TraceLoggingEventTypes(EventTags tags, string defaultName, TraceLoggingTypeInfo[] typeInfos)
		{
			if (defaultName == null)
			{
				throw new ArgumentNullException("defaultName");
			}
			this.typeInfos = typeInfos;
			this.name = defaultName;
			this.tags = tags;
			this.level = 5;
			TraceLoggingMetadataCollector traceLoggingMetadataCollector = new TraceLoggingMetadataCollector();
			foreach (TraceLoggingTypeInfo traceLoggingTypeInfo in typeInfos)
			{
				this.level = Statics.Combine((int)traceLoggingTypeInfo.Level, this.level);
				this.opcode = Statics.Combine((int)traceLoggingTypeInfo.Opcode, this.opcode);
				this.keywords |= traceLoggingTypeInfo.Keywords;
				traceLoggingTypeInfo.WriteMetadata(traceLoggingMetadataCollector, null, EventFieldFormat.Default);
			}
			this.typeMetadata = traceLoggingMetadataCollector.GetMetadata();
			this.scratchSize = traceLoggingMetadataCollector.ScratchSize;
			this.dataCount = traceLoggingMetadataCollector.DataCount;
			this.pinCount = traceLoggingMetadataCollector.PinCount;
		}

		// Token: 0x1700080D RID: 2061
		// (get) Token: 0x0600373D RID: 14141 RVA: 0x000D4C79 File Offset: 0x000D2E79
		internal string Name
		{
			get
			{
				return this.name;
			}
		}

		// Token: 0x1700080E RID: 2062
		// (get) Token: 0x0600373E RID: 14142 RVA: 0x000D4C81 File Offset: 0x000D2E81
		internal EventLevel Level
		{
			get
			{
				return (EventLevel)this.level;
			}
		}

		// Token: 0x1700080F RID: 2063
		// (get) Token: 0x0600373F RID: 14143 RVA: 0x000D4C89 File Offset: 0x000D2E89
		internal EventOpcode Opcode
		{
			get
			{
				return (EventOpcode)this.opcode;
			}
		}

		// Token: 0x17000810 RID: 2064
		// (get) Token: 0x06003740 RID: 14144 RVA: 0x000D4C91 File Offset: 0x000D2E91
		internal EventKeywords Keywords
		{
			get
			{
				return this.keywords;
			}
		}

		// Token: 0x17000811 RID: 2065
		// (get) Token: 0x06003741 RID: 14145 RVA: 0x000D4C99 File Offset: 0x000D2E99
		internal EventTags Tags
		{
			get
			{
				return this.tags;
			}
		}

		// Token: 0x06003742 RID: 14146 RVA: 0x000D4CA4 File Offset: 0x000D2EA4
		internal NameInfo GetNameInfo(string name, EventTags tags)
		{
			NameInfo nameInfo = this.nameInfos.TryGet(new KeyValuePair<string, EventTags>(name, tags));
			if (nameInfo == null)
			{
				nameInfo = this.nameInfos.GetOrAdd(new NameInfo(name, tags, this.typeMetadata.Length));
			}
			return nameInfo;
		}

		// Token: 0x06003743 RID: 14147 RVA: 0x000D4CE4 File Offset: 0x000D2EE4
		private TraceLoggingTypeInfo[] MakeArray(ParameterInfo[] paramInfos)
		{
			if (paramInfos == null)
			{
				throw new ArgumentNullException("paramInfos");
			}
			List<Type> recursionCheck = new List<Type>(paramInfos.Length);
			TraceLoggingTypeInfo[] array = new TraceLoggingTypeInfo[paramInfos.Length];
			for (int i = 0; i < paramInfos.Length; i++)
			{
				array[i] = Statics.GetTypeInfoInstance(paramInfos[i].ParameterType, recursionCheck);
			}
			return array;
		}

		// Token: 0x06003744 RID: 14148 RVA: 0x000D4D34 File Offset: 0x000D2F34
		private static TraceLoggingTypeInfo[] MakeArray(Type[] types)
		{
			if (types == null)
			{
				throw new ArgumentNullException("types");
			}
			List<Type> recursionCheck = new List<Type>(types.Length);
			TraceLoggingTypeInfo[] array = new TraceLoggingTypeInfo[types.Length];
			for (int i = 0; i < types.Length; i++)
			{
				array[i] = Statics.GetTypeInfoInstance(types[i], recursionCheck);
			}
			return array;
		}

		// Token: 0x06003745 RID: 14149 RVA: 0x000D4D7C File Offset: 0x000D2F7C
		private static TraceLoggingTypeInfo[] MakeArray(TraceLoggingTypeInfo[] typeInfos)
		{
			if (typeInfos == null)
			{
				throw new ArgumentNullException("typeInfos");
			}
			return (TraceLoggingTypeInfo[])typeInfos.Clone();
		}

		// Token: 0x04001897 RID: 6295
		internal readonly TraceLoggingTypeInfo[] typeInfos;

		// Token: 0x04001898 RID: 6296
		internal readonly string name;

		// Token: 0x04001899 RID: 6297
		internal readonly EventTags tags;

		// Token: 0x0400189A RID: 6298
		internal readonly byte level;

		// Token: 0x0400189B RID: 6299
		internal readonly byte opcode;

		// Token: 0x0400189C RID: 6300
		internal readonly EventKeywords keywords;

		// Token: 0x0400189D RID: 6301
		internal readonly byte[] typeMetadata;

		// Token: 0x0400189E RID: 6302
		internal readonly int scratchSize;

		// Token: 0x0400189F RID: 6303
		internal readonly int dataCount;

		// Token: 0x040018A0 RID: 6304
		internal readonly int pinCount;

		// Token: 0x040018A1 RID: 6305
		private ConcurrentSet<KeyValuePair<string, EventTags>, NameInfo> nameInfos;
	}
}
