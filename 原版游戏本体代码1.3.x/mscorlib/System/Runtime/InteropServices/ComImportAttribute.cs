using System;
using System.Reflection;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200092C RID: 2348
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class ComImportAttribute : Attribute
	{
		// Token: 0x06006027 RID: 24615 RVA: 0x0014BB16 File Offset: 0x00149D16
		internal static Attribute GetCustomAttribute(RuntimeType type)
		{
			if ((type.Attributes & TypeAttributes.Import) == TypeAttributes.NotPublic)
			{
				return null;
			}
			return new ComImportAttribute();
		}

		// Token: 0x06006028 RID: 24616 RVA: 0x0014BB2D File Offset: 0x00149D2D
		internal static bool IsDefined(RuntimeType type)
		{
			return (type.Attributes & TypeAttributes.Import) > TypeAttributes.NotPublic;
		}

		// Token: 0x06006029 RID: 24617 RVA: 0x0014BB3E File Offset: 0x00149D3E
		[__DynamicallyInvokable]
		public ComImportAttribute()
		{
		}
	}
}
