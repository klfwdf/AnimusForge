using System;
using System.Reflection;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200092E RID: 2350
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class PreserveSigAttribute : Attribute
	{
		// Token: 0x0600602C RID: 24620 RVA: 0x0014BB5D File Offset: 0x00149D5D
		internal static Attribute GetCustomAttribute(RuntimeMethodInfo method)
		{
			if ((method.GetMethodImplementationFlags() & MethodImplAttributes.PreserveSig) == MethodImplAttributes.IL)
			{
				return null;
			}
			return new PreserveSigAttribute();
		}

		// Token: 0x0600602D RID: 24621 RVA: 0x0014BB74 File Offset: 0x00149D74
		internal static bool IsDefined(RuntimeMethodInfo method)
		{
			return (method.GetMethodImplementationFlags() & MethodImplAttributes.PreserveSig) > MethodImplAttributes.IL;
		}

		// Token: 0x0600602E RID: 24622 RVA: 0x0014BB85 File Offset: 0x00149D85
		[__DynamicallyInvokable]
		public PreserveSigAttribute()
		{
		}
	}
}
