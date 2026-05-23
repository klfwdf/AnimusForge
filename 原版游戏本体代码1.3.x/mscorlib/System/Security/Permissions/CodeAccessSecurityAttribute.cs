using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x020002F0 RID: 752
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public abstract class CodeAccessSecurityAttribute : SecurityAttribute
	{
		// Token: 0x0600265F RID: 9823 RVA: 0x0008C47B File Offset: 0x0008A67B
		protected CodeAccessSecurityAttribute(SecurityAction action)
			: base(action)
		{
		}
	}
}
