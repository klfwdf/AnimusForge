using System;
using System.Security;

namespace System.Runtime.Remoting.Contexts
{
	// Token: 0x0200080A RID: 2058
	[Serializable]
	internal class CallBackHelper
	{
		// Token: 0x17000EB4 RID: 3764
		// (get) Token: 0x060058B7 RID: 22711 RVA: 0x00138E79 File Offset: 0x00137079
		// (set) Token: 0x060058B8 RID: 22712 RVA: 0x00138E86 File Offset: 0x00137086
		internal bool IsEERequested
		{
			get
			{
				return (this._flags & 1) == 1;
			}
			set
			{
				if (value)
				{
					this._flags |= 1;
				}
			}
		}

		// Token: 0x17000EB5 RID: 3765
		// (set) Token: 0x060058B9 RID: 22713 RVA: 0x00138E99 File Offset: 0x00137099
		internal bool IsCrossDomain
		{
			set
			{
				if (value)
				{
					this._flags |= 256;
				}
			}
		}

		// Token: 0x060058BA RID: 22714 RVA: 0x00138EB0 File Offset: 0x001370B0
		internal CallBackHelper(IntPtr privateData, bool bFromEE, int targetDomainID)
		{
			this.IsEERequested = bFromEE;
			this.IsCrossDomain = targetDomainID != 0;
			this._privateData = privateData;
		}

		// Token: 0x060058BB RID: 22715 RVA: 0x00138ED0 File Offset: 0x001370D0
		[SecurityCritical]
		internal void Func()
		{
			if (this.IsEERequested)
			{
				Context.ExecuteCallBackInEE(this._privateData);
			}
		}

		// Token: 0x0400286F RID: 10351
		internal const int RequestedFromEE = 1;

		// Token: 0x04002870 RID: 10352
		internal const int XDomainTransition = 256;

		// Token: 0x04002871 RID: 10353
		private int _flags;

		// Token: 0x04002872 RID: 10354
		private IntPtr _privateData;
	}
}
