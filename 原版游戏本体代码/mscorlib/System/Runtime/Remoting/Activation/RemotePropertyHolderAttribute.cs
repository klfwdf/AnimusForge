using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Security;

namespace System.Runtime.Remoting.Activation
{
	// Token: 0x02000897 RID: 2199
	internal class RemotePropertyHolderAttribute : IContextAttribute
	{
		// Token: 0x06005D22 RID: 23842 RVA: 0x00146A55 File Offset: 0x00144C55
		internal RemotePropertyHolderAttribute(IList cp)
		{
			this._cp = cp;
		}

		// Token: 0x06005D23 RID: 23843 RVA: 0x00146A64 File Offset: 0x00144C64
		[SecurityCritical]
		[ComVisible(true)]
		public virtual bool IsContextOK(Context ctx, IConstructionCallMessage msg)
		{
			return false;
		}

		// Token: 0x06005D24 RID: 23844 RVA: 0x00146A68 File Offset: 0x00144C68
		[SecurityCritical]
		[ComVisible(true)]
		public virtual void GetPropertiesForNewContext(IConstructionCallMessage ctorMsg)
		{
			for (int i = 0; i < this._cp.Count; i++)
			{
				ctorMsg.ContextProperties.Add(this._cp[i]);
			}
		}

		// Token: 0x040029F2 RID: 10738
		private IList _cp;
	}
}
