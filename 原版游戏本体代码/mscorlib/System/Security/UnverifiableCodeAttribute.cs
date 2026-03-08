using System;
using System.Runtime.InteropServices;

namespace System.Security
{
	// Token: 0x020001C3 RID: 451
	[AttributeUsage(AttributeTargets.Module, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	public sealed class UnverifiableCodeAttribute : Attribute
	{
	}
}
