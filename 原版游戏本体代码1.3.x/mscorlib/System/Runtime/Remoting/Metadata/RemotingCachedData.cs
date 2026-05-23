using System;

namespace System.Runtime.Remoting.Metadata
{
	// Token: 0x020007D0 RID: 2000
	internal abstract class RemotingCachedData
	{
		// Token: 0x060056CA RID: 22218 RVA: 0x00134450 File Offset: 0x00132650
		internal SoapAttribute GetSoapAttribute()
		{
			if (this._soapAttr == null)
			{
				lock (this)
				{
					if (this._soapAttr == null)
					{
						this._soapAttr = this.GetSoapAttributeNoLock();
					}
				}
			}
			return this._soapAttr;
		}

		// Token: 0x060056CB RID: 22219
		internal abstract SoapAttribute GetSoapAttributeNoLock();

		// Token: 0x040027B6 RID: 10166
		private SoapAttribute _soapAttr;
	}
}
