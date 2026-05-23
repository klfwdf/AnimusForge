using System;
using System.Runtime.InteropServices;

namespace System.Security
{
	// Token: 0x020001C2 RID: 450
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	public sealed class SuppressUnmanagedCodeSecurityAttribute : Attribute
	{
	}
}
