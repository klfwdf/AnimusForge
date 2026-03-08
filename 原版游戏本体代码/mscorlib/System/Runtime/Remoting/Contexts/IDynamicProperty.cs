using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Contexts
{
	// Token: 0x02000817 RID: 2071
	[ComVisible(true)]
	public interface IDynamicProperty
	{
		// Token: 0x17000EBC RID: 3772
		// (get) Token: 0x060058E0 RID: 22752
		string Name
		{
			[SecurityCritical]
			get;
		}
	}
}
