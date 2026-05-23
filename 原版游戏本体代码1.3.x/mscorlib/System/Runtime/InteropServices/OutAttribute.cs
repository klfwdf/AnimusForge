using System;
using System.Reflection;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000930 RID: 2352
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class OutAttribute : Attribute
	{
		// Token: 0x06006032 RID: 24626 RVA: 0x0014BBAE File Offset: 0x00149DAE
		internal static Attribute GetCustomAttribute(RuntimeParameterInfo parameter)
		{
			if (!parameter.IsOut)
			{
				return null;
			}
			return new OutAttribute();
		}

		// Token: 0x06006033 RID: 24627 RVA: 0x0014BBBF File Offset: 0x00149DBF
		internal static bool IsDefined(RuntimeParameterInfo parameter)
		{
			return parameter.IsOut;
		}

		// Token: 0x06006034 RID: 24628 RVA: 0x0014BBC7 File Offset: 0x00149DC7
		[__DynamicallyInvokable]
		public OutAttribute()
		{
		}
	}
}
