using System;

namespace System.Runtime.Remoting
{
	// Token: 0x020007C5 RID: 1989
	internal class RemoteAppEntry
	{
		// Token: 0x06005611 RID: 22033 RVA: 0x0013160F File Offset: 0x0012F80F
		internal RemoteAppEntry(string appName, string appURI)
		{
			this._remoteAppName = appName;
			this._remoteAppURI = appURI;
		}

		// Token: 0x06005612 RID: 22034 RVA: 0x00131625 File Offset: 0x0012F825
		internal string GetAppURI()
		{
			return this._remoteAppURI;
		}

		// Token: 0x04002789 RID: 10121
		private string _remoteAppName;

		// Token: 0x0400278A RID: 10122
		private string _remoteAppURI;
	}
}
