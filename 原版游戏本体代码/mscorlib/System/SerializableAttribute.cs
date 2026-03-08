using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x0200013D RID: 317
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = false)]
	[ComVisible(true)]
	public sealed class SerializableAttribute : Attribute
	{
		// Token: 0x0600130B RID: 4875 RVA: 0x000382A9 File Offset: 0x000364A9
		internal static Attribute GetCustomAttribute(RuntimeType type)
		{
			if ((type.Attributes & TypeAttributes.Serializable) != TypeAttributes.Serializable)
			{
				return null;
			}
			return new SerializableAttribute();
		}

		// Token: 0x0600130C RID: 4876 RVA: 0x000382C5 File Offset: 0x000364C5
		internal static bool IsDefined(RuntimeType type)
		{
			return type.IsSerializable;
		}
	}
}
