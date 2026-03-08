using System;

namespace System.Reflection
{
	// Token: 0x020005EC RID: 1516
	[__DynamicallyInvokable]
	public static class IntrospectionExtensions
	{
		// Token: 0x06004653 RID: 18003 RVA: 0x00102594 File Offset: 0x00100794
		[__DynamicallyInvokable]
		public static TypeInfo GetTypeInfo(this Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			IReflectableType reflectableType = (IReflectableType)type;
			if (reflectableType == null)
			{
				return null;
			}
			return reflectableType.GetTypeInfo();
		}
	}
}
