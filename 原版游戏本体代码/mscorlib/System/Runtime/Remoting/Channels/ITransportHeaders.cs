using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x0200084C RID: 2124
	[ComVisible(true)]
	public interface ITransportHeaders
	{
		// Token: 0x17000EFF RID: 3839
		object this[object key]
		{
			[SecurityCritical]
			get;
			[SecurityCritical]
			set;
		}

		// Token: 0x06005A30 RID: 23088
		[SecurityCritical]
		IEnumerator GetEnumerator();
	}
}
