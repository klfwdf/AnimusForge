using System;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Threading;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x02000837 RID: 2103
	[Serializable]
	internal class CrossAppDomainData
	{
		// Token: 0x17000EE9 RID: 3817
		// (get) Token: 0x060059E1 RID: 23009 RVA: 0x0013CDFA File Offset: 0x0013AFFA
		internal virtual IntPtr ContextID
		{
			get
			{
				return new IntPtr((long)this._ContextID);
			}
		}

		// Token: 0x17000EEA RID: 3818
		// (get) Token: 0x060059E2 RID: 23010 RVA: 0x0013CE0C File Offset: 0x0013B00C
		internal virtual int DomainID
		{
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			get
			{
				return this._DomainID;
			}
		}

		// Token: 0x17000EEB RID: 3819
		// (get) Token: 0x060059E3 RID: 23011 RVA: 0x0013CE14 File Offset: 0x0013B014
		internal virtual string ProcessGuid
		{
			get
			{
				return this._processGuid;
			}
		}

		// Token: 0x060059E4 RID: 23012 RVA: 0x0013CE1C File Offset: 0x0013B01C
		internal CrossAppDomainData(IntPtr ctxId, int domainID, string processGuid)
		{
			this._DomainID = domainID;
			this._processGuid = processGuid;
			this._ContextID = ctxId.ToInt64();
		}

		// Token: 0x060059E5 RID: 23013 RVA: 0x0013CE50 File Offset: 0x0013B050
		internal bool IsFromThisProcess()
		{
			return Identity.ProcessGuid.Equals(this._processGuid);
		}

		// Token: 0x060059E6 RID: 23014 RVA: 0x0013CE62 File Offset: 0x0013B062
		[SecurityCritical]
		internal bool IsFromThisAppDomain()
		{
			return this.IsFromThisProcess() && Thread.GetDomain().GetId() == this._DomainID;
		}

		// Token: 0x040028EA RID: 10474
		private object _ContextID = 0;

		// Token: 0x040028EB RID: 10475
		private int _DomainID;

		// Token: 0x040028EC RID: 10476
		private string _processGuid;
	}
}
