using System;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000876 RID: 2166
	internal class SmuggledObjRef
	{
		// Token: 0x06005C30 RID: 23600 RVA: 0x00143324 File Offset: 0x00141524
		[SecurityCritical]
		public SmuggledObjRef(ObjRef objRef)
		{
			this._objRef = objRef;
		}

		// Token: 0x17000FD8 RID: 4056
		// (get) Token: 0x06005C31 RID: 23601 RVA: 0x00143333 File Offset: 0x00141533
		public ObjRef ObjRef
		{
			[SecurityCritical]
			get
			{
				return this._objRef;
			}
		}

		// Token: 0x0400299F RID: 10655
		[SecurityCritical]
		private ObjRef _objRef;
	}
}
