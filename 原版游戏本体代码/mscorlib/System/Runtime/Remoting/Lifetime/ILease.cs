using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Lifetime
{
	// Token: 0x0200081E RID: 2078
	[ComVisible(true)]
	public interface ILease
	{
		// Token: 0x0600591D RID: 22813
		[SecurityCritical]
		void Register(ISponsor obj, TimeSpan renewalTime);

		// Token: 0x0600591E RID: 22814
		[SecurityCritical]
		void Register(ISponsor obj);

		// Token: 0x0600591F RID: 22815
		[SecurityCritical]
		void Unregister(ISponsor obj);

		// Token: 0x06005920 RID: 22816
		[SecurityCritical]
		TimeSpan Renew(TimeSpan renewalTime);

		// Token: 0x17000EC5 RID: 3781
		// (get) Token: 0x06005921 RID: 22817
		// (set) Token: 0x06005922 RID: 22818
		TimeSpan RenewOnCallTime
		{
			[SecurityCritical]
			get;
			[SecurityCritical]
			set;
		}

		// Token: 0x17000EC6 RID: 3782
		// (get) Token: 0x06005923 RID: 22819
		// (set) Token: 0x06005924 RID: 22820
		TimeSpan SponsorshipTimeout
		{
			[SecurityCritical]
			get;
			[SecurityCritical]
			set;
		}

		// Token: 0x17000EC7 RID: 3783
		// (get) Token: 0x06005925 RID: 22821
		// (set) Token: 0x06005926 RID: 22822
		TimeSpan InitialLeaseTime
		{
			[SecurityCritical]
			get;
			[SecurityCritical]
			set;
		}

		// Token: 0x17000EC8 RID: 3784
		// (get) Token: 0x06005927 RID: 22823
		TimeSpan CurrentLeaseTime
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000EC9 RID: 3785
		// (get) Token: 0x06005928 RID: 22824
		LeaseState CurrentState
		{
			[SecurityCritical]
			get;
		}
	}
}
