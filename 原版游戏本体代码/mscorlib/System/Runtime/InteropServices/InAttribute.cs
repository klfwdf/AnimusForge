using System;
using System.Reflection;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200092F RID: 2351
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class InAttribute : Attribute
	{
		// Token: 0x0600602F RID: 24623 RVA: 0x0014BB8D File Offset: 0x00149D8D
		internal static Attribute GetCustomAttribute(RuntimeParameterInfo parameter)
		{
			if (!parameter.IsIn)
			{
				return null;
			}
			return new InAttribute();
		}

		// Token: 0x06006030 RID: 24624 RVA: 0x0014BB9E File Offset: 0x00149D9E
		internal static bool IsDefined(RuntimeParameterInfo parameter)
		{
			return parameter.IsIn;
		}

		// Token: 0x06006031 RID: 24625 RVA: 0x0014BBA6 File Offset: 0x00149DA6
		[__DynamicallyInvokable]
		public InAttribute()
		{
		}
	}
}
