using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace System.Runtime.Remoting.Lifetime
{
	// Token: 0x0200081D RID: 2077
	[SecurityCritical]
	[ComVisible(true)]
	[SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.Infrastructure)]
	public class ClientSponsor : MarshalByRefObject, ISponsor
	{
		// Token: 0x06005913 RID: 22803 RVA: 0x00139D36 File Offset: 0x00137F36
		public ClientSponsor()
		{
		}

		// Token: 0x06005914 RID: 22804 RVA: 0x00139D5F File Offset: 0x00137F5F
		public ClientSponsor(TimeSpan renewalTime)
		{
			this.m_renewalTime = renewalTime;
		}

		// Token: 0x17000EC4 RID: 3780
		// (get) Token: 0x06005915 RID: 22805 RVA: 0x00139D8F File Offset: 0x00137F8F
		// (set) Token: 0x06005916 RID: 22806 RVA: 0x00139D97 File Offset: 0x00137F97
		public TimeSpan RenewalTime
		{
			get
			{
				return this.m_renewalTime;
			}
			set
			{
				this.m_renewalTime = value;
			}
		}

		// Token: 0x06005917 RID: 22807 RVA: 0x00139DA0 File Offset: 0x00137FA0
		[SecurityCritical]
		public bool Register(MarshalByRefObject obj)
		{
			ILease lease = (ILease)obj.GetLifetimeService();
			if (lease == null)
			{
				return false;
			}
			lease.Register(this);
			Hashtable obj2 = this.sponsorTable;
			lock (obj2)
			{
				this.sponsorTable[obj] = lease;
			}
			return true;
		}

		// Token: 0x06005918 RID: 22808 RVA: 0x00139E00 File Offset: 0x00138000
		[SecurityCritical]
		public void Unregister(MarshalByRefObject obj)
		{
			ILease lease = null;
			Hashtable obj2 = this.sponsorTable;
			lock (obj2)
			{
				lease = (ILease)this.sponsorTable[obj];
			}
			if (lease != null)
			{
				lease.Unregister(this);
			}
		}

		// Token: 0x06005919 RID: 22809 RVA: 0x00139E58 File Offset: 0x00138058
		[SecurityCritical]
		public TimeSpan Renewal(ILease lease)
		{
			return this.m_renewalTime;
		}

		// Token: 0x0600591A RID: 22810 RVA: 0x00139E60 File Offset: 0x00138060
		[SecurityCritical]
		public void Close()
		{
			Hashtable obj = this.sponsorTable;
			lock (obj)
			{
				IDictionaryEnumerator enumerator = this.sponsorTable.GetEnumerator();
				while (enumerator.MoveNext())
				{
					((ILease)enumerator.Value).Unregister(this);
				}
				this.sponsorTable.Clear();
			}
		}

		// Token: 0x0600591B RID: 22811 RVA: 0x00139ECC File Offset: 0x001380CC
		[SecurityCritical]
		public override object InitializeLifetimeService()
		{
			return null;
		}

		// Token: 0x0600591C RID: 22812 RVA: 0x00139ED0 File Offset: 0x001380D0
		[SecuritySafeCritical]
		~ClientSponsor()
		{
		}

		// Token: 0x0400289B RID: 10395
		private Hashtable sponsorTable = new Hashtable(10);

		// Token: 0x0400289C RID: 10396
		private TimeSpan m_renewalTime = TimeSpan.FromMinutes(2.0);
	}
}
