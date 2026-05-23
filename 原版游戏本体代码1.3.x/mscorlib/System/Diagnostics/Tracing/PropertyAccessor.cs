using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200044C RID: 1100
	internal abstract class PropertyAccessor<ContainerType>
	{
		// Token: 0x06003651 RID: 13905
		public abstract void Write(TraceLoggingDataCollector collector, ref ContainerType value);

		// Token: 0x06003652 RID: 13906
		public abstract object GetData(ContainerType value);

		// Token: 0x06003653 RID: 13907 RVA: 0x000D3038 File Offset: 0x000D1238
		public static PropertyAccessor<ContainerType> Create(PropertyAnalysis property)
		{
			Type returnType = property.getterInfo.ReturnType;
			if (!Statics.IsValueType(typeof(ContainerType)))
			{
				if (returnType == typeof(int))
				{
					return new ClassPropertyWriter<ContainerType, int>(property);
				}
				if (returnType == typeof(long))
				{
					return new ClassPropertyWriter<ContainerType, long>(property);
				}
				if (returnType == typeof(string))
				{
					return new ClassPropertyWriter<ContainerType, string>(property);
				}
			}
			return new NonGenericProperytWriter<ContainerType>(property);
		}
	}
}
