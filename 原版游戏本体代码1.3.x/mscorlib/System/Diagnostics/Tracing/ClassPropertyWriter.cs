using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200044F RID: 1103
	internal class ClassPropertyWriter<ContainerType, ValueType> : PropertyAccessor<ContainerType>
	{
		// Token: 0x0600365B RID: 13915 RVA: 0x000D31EA File Offset: 0x000D13EA
		public ClassPropertyWriter(PropertyAnalysis property)
		{
			this.valueTypeInfo = (TraceLoggingTypeInfo<ValueType>)property.typeInfo;
			this.getter = (ClassPropertyWriter<ContainerType, ValueType>.Getter)Statics.CreateDelegate(typeof(ClassPropertyWriter<ContainerType, ValueType>.Getter), property.getterInfo);
		}

		// Token: 0x0600365C RID: 13916 RVA: 0x000D3224 File Offset: 0x000D1424
		public override void Write(TraceLoggingDataCollector collector, ref ContainerType container)
		{
			ValueType valueType = ((container == null) ? default(ValueType) : this.getter(container));
			this.valueTypeInfo.WriteData(collector, ref valueType);
		}

		// Token: 0x0600365D RID: 13917 RVA: 0x000D326C File Offset: 0x000D146C
		public override object GetData(ContainerType container)
		{
			return (container == null) ? default(ValueType) : this.getter(container);
		}

		// Token: 0x04001855 RID: 6229
		private readonly TraceLoggingTypeInfo<ValueType> valueTypeInfo;

		// Token: 0x04001856 RID: 6230
		private readonly ClassPropertyWriter<ContainerType, ValueType>.Getter getter;

		// Token: 0x02000B9F RID: 2975
		// (Invoke) Token: 0x06006CA5 RID: 27813
		private delegate ValueType Getter(ContainerType container);
	}
}
