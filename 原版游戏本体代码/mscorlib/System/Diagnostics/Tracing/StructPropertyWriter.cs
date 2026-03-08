using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200044E RID: 1102
	internal class StructPropertyWriter<ContainerType, ValueType> : PropertyAccessor<ContainerType>
	{
		// Token: 0x06003658 RID: 13912 RVA: 0x000D313C File Offset: 0x000D133C
		public StructPropertyWriter(PropertyAnalysis property)
		{
			this.valueTypeInfo = (TraceLoggingTypeInfo<ValueType>)property.typeInfo;
			this.getter = (StructPropertyWriter<ContainerType, ValueType>.Getter)Statics.CreateDelegate(typeof(StructPropertyWriter<ContainerType, ValueType>.Getter), property.getterInfo);
		}

		// Token: 0x06003659 RID: 13913 RVA: 0x000D3178 File Offset: 0x000D1378
		public override void Write(TraceLoggingDataCollector collector, ref ContainerType container)
		{
			ValueType valueType = ((container == null) ? default(ValueType) : this.getter(ref container));
			this.valueTypeInfo.WriteData(collector, ref valueType);
		}

		// Token: 0x0600365A RID: 13914 RVA: 0x000D31B8 File Offset: 0x000D13B8
		public override object GetData(ContainerType container)
		{
			return (container == null) ? default(ValueType) : this.getter(ref container);
		}

		// Token: 0x04001853 RID: 6227
		private readonly TraceLoggingTypeInfo<ValueType> valueTypeInfo;

		// Token: 0x04001854 RID: 6228
		private readonly StructPropertyWriter<ContainerType, ValueType>.Getter getter;

		// Token: 0x02000B9E RID: 2974
		// (Invoke) Token: 0x06006CA1 RID: 27809
		private delegate ValueType Getter(ref ContainerType container);
	}
}
