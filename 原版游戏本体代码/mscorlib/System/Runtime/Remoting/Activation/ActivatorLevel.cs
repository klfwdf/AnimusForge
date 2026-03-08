using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Activation
{
	// Token: 0x0200089A RID: 2202
	[ComVisible(true)]
	[Serializable]
	public enum ActivatorLevel
	{
		// Token: 0x040029F7 RID: 10743
		Construction = 4,
		// Token: 0x040029F8 RID: 10744
		Context = 8,
		// Token: 0x040029F9 RID: 10745
		AppDomain = 12,
		// Token: 0x040029FA RID: 10746
		Process = 16,
		// Token: 0x040029FB RID: 10747
		Machine = 20
	}
}
