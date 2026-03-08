using System;
using System.Security;

namespace System.IO.IsolatedStorage
{
	// Token: 0x020001B3 RID: 435
	[SecurityCritical]
	public class IsolatedStorageSecurityState : SecurityState
	{
		// Token: 0x06001B50 RID: 6992 RVA: 0x0005C9C0 File Offset: 0x0005ABC0
		internal static IsolatedStorageSecurityState CreateStateToIncreaseQuotaForApplication(long newQuota, long usedSize)
		{
			return new IsolatedStorageSecurityState
			{
				m_Options = IsolatedStorageSecurityOptions.IncreaseQuotaForApplication,
				m_Quota = newQuota,
				m_UsedSize = usedSize
			};
		}

		// Token: 0x06001B51 RID: 6993 RVA: 0x0005C9E9 File Offset: 0x0005ABE9
		[SecurityCritical]
		private IsolatedStorageSecurityState()
		{
		}

		// Token: 0x17000317 RID: 791
		// (get) Token: 0x06001B52 RID: 6994 RVA: 0x0005C9F1 File Offset: 0x0005ABF1
		public IsolatedStorageSecurityOptions Options
		{
			get
			{
				return this.m_Options;
			}
		}

		// Token: 0x17000318 RID: 792
		// (get) Token: 0x06001B53 RID: 6995 RVA: 0x0005C9F9 File Offset: 0x0005ABF9
		public long UsedSize
		{
			get
			{
				return this.m_UsedSize;
			}
		}

		// Token: 0x17000319 RID: 793
		// (get) Token: 0x06001B54 RID: 6996 RVA: 0x0005CA01 File Offset: 0x0005AC01
		// (set) Token: 0x06001B55 RID: 6997 RVA: 0x0005CA09 File Offset: 0x0005AC09
		public long Quota
		{
			get
			{
				return this.m_Quota;
			}
			set
			{
				this.m_Quota = value;
			}
		}

		// Token: 0x06001B56 RID: 6998 RVA: 0x0005CA12 File Offset: 0x0005AC12
		[SecurityCritical]
		public override void EnsureState()
		{
			if (!base.IsStateAvailable())
			{
				throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Operation"));
			}
		}

		// Token: 0x04000977 RID: 2423
		private long m_UsedSize;

		// Token: 0x04000978 RID: 2424
		private long m_Quota;

		// Token: 0x04000979 RID: 2425
		private IsolatedStorageSecurityOptions m_Options;
	}
}
