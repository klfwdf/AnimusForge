using System;
using System.Reflection;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000931 RID: 2353
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class OptionalAttribute : Attribute
	{
		// Token: 0x06006035 RID: 24629 RVA: 0x0014BBCF File Offset: 0x00149DCF
		internal static Attribute GetCustomAttribute(RuntimeParameterInfo parameter)
		{
			if (!parameter.IsOptional)
			{
				return null;
			}
			return new OptionalAttribute();
		}

		// Token: 0x06006036 RID: 24630 RVA: 0x0014BBE0 File Offset: 0x00149DE0
		internal static bool IsDefined(RuntimeParameterInfo parameter)
		{
			return parameter.IsOptional;
		}

		// Token: 0x06006037 RID: 24631 RVA: 0x0014BBE8 File Offset: 0x00149DE8
		[__DynamicallyInvokable]
		public OptionalAttribute()
		{
		}
	}
}
