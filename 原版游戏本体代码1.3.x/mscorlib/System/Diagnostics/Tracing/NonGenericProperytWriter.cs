using System;
using System.Reflection;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200044D RID: 1101
	internal class NonGenericProperytWriter<ContainerType> : PropertyAccessor<ContainerType>
	{
		// Token: 0x06003655 RID: 13909 RVA: 0x000D30BB File Offset: 0x000D12BB
		public NonGenericProperytWriter(PropertyAnalysis property)
		{
			this.getterInfo = property.getterInfo;
			this.typeInfo = property.typeInfo;
		}

		// Token: 0x06003656 RID: 13910 RVA: 0x000D30DC File Offset: 0x000D12DC
		public override void Write(TraceLoggingDataCollector collector, ref ContainerType container)
		{
			object value = ((container == null) ? null : this.getterInfo.Invoke(container, null));
			this.typeInfo.WriteObjectData(collector, value);
		}

		// Token: 0x06003657 RID: 13911 RVA: 0x000D311E File Offset: 0x000D131E
		public override object GetData(ContainerType container)
		{
			if (container != null)
			{
				return this.getterInfo.Invoke(container, null);
			}
			return null;
		}

		// Token: 0x04001851 RID: 6225
		private readonly TraceLoggingTypeInfo typeInfo;

		// Token: 0x04001852 RID: 6226
		private readonly MethodInfo getterInfo;
	}
}
