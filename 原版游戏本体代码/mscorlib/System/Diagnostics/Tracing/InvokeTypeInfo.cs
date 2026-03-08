using System;
using System.Collections.Generic;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200044A RID: 1098
	internal sealed class InvokeTypeInfo<ContainerType> : TraceLoggingTypeInfo<ContainerType>
	{
		// Token: 0x06003646 RID: 13894 RVA: 0x000D2D3C File Offset: 0x000D0F3C
		public InvokeTypeInfo(TypeAnalysis typeAnalysis)
			: base(typeAnalysis.name, typeAnalysis.level, typeAnalysis.opcode, typeAnalysis.keywords, typeAnalysis.tags)
		{
			if (typeAnalysis.properties.Length != 0)
			{
				this.properties = typeAnalysis.properties;
				this.accessors = new PropertyAccessor<ContainerType>[this.properties.Length];
				for (int i = 0; i < this.accessors.Length; i++)
				{
					this.accessors[i] = PropertyAccessor<ContainerType>.Create(this.properties[i]);
				}
			}
		}

		// Token: 0x06003647 RID: 13895 RVA: 0x000D2DC0 File Offset: 0x000D0FC0
		public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
		{
			TraceLoggingMetadataCollector traceLoggingMetadataCollector = collector.AddGroup(name);
			if (this.properties != null)
			{
				foreach (PropertyAnalysis propertyAnalysis in this.properties)
				{
					EventFieldFormat format2 = EventFieldFormat.Default;
					EventFieldAttribute fieldAttribute = propertyAnalysis.fieldAttribute;
					if (fieldAttribute != null)
					{
						traceLoggingMetadataCollector.Tags = fieldAttribute.Tags;
						format2 = fieldAttribute.Format;
					}
					propertyAnalysis.typeInfo.WriteMetadata(traceLoggingMetadataCollector, propertyAnalysis.name, format2);
				}
			}
		}

		// Token: 0x06003648 RID: 13896 RVA: 0x000D2E30 File Offset: 0x000D1030
		public override void WriteData(TraceLoggingDataCollector collector, ref ContainerType value)
		{
			if (this.accessors != null)
			{
				foreach (PropertyAccessor<ContainerType> propertyAccessor in this.accessors)
				{
					propertyAccessor.Write(collector, ref value);
				}
			}
		}

		// Token: 0x06003649 RID: 13897 RVA: 0x000D2E68 File Offset: 0x000D1068
		public override object GetData(object value)
		{
			if (this.properties != null)
			{
				List<string> list = new List<string>();
				List<object> list2 = new List<object>();
				for (int i = 0; i < this.properties.Length; i++)
				{
					object data = this.accessors[i].GetData((ContainerType)((object)value));
					list.Add(this.properties[i].name);
					list2.Add(this.properties[i].typeInfo.GetData(data));
				}
				return new EventPayload(list, list2);
			}
			return null;
		}

		// Token: 0x0600364A RID: 13898 RVA: 0x000D2EE8 File Offset: 0x000D10E8
		public override void WriteObjectData(TraceLoggingDataCollector collector, object valueObj)
		{
			if (this.accessors != null)
			{
				ContainerType containerType = ((valueObj == null) ? default(ContainerType) : ((ContainerType)((object)valueObj)));
				this.WriteData(collector, ref containerType);
			}
		}

		// Token: 0x0400184A RID: 6218
		private readonly PropertyAnalysis[] properties;

		// Token: 0x0400184B RID: 6219
		private readonly PropertyAccessor<ContainerType>[] accessors;
	}
}
