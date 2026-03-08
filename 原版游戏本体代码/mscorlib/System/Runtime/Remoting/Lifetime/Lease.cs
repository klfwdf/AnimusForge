using System;
using System.Collections;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace System.Runtime.Remoting.Lifetime
{
	// Token: 0x02000820 RID: 2080
	internal class Lease : MarshalByRefObject, ILease
	{
		// Token: 0x0600592A RID: 22826 RVA: 0x00139EF8 File Offset: 0x001380F8
		internal Lease(TimeSpan initialLeaseTime, TimeSpan renewOnCallTime, TimeSpan sponsorshipTimeout, MarshalByRefObject managedObject)
		{
			this.id = Lease.nextId++;
			this.renewOnCallTime = renewOnCallTime;
			this.sponsorshipTimeout = sponsorshipTimeout;
			this.initialLeaseTime = initialLeaseTime;
			this.managedObject = managedObject;
			this.leaseManager = LeaseManager.GetLeaseManager();
			this.sponsorTable = new Hashtable(10);
			this.state = LeaseState.Initial;
		}

		// Token: 0x0600592B RID: 22827 RVA: 0x00139F60 File Offset: 0x00138160
		internal void ActivateLease()
		{
			this.leaseTime = DateTime.UtcNow.Add(this.initialLeaseTime);
			this.state = LeaseState.Active;
			this.leaseManager.ActivateLease(this);
		}

		// Token: 0x0600592C RID: 22828 RVA: 0x00139F99 File Offset: 0x00138199
		[SecurityCritical]
		public override object InitializeLifetimeService()
		{
			return null;
		}

		// Token: 0x17000ECA RID: 3786
		// (get) Token: 0x0600592D RID: 22829 RVA: 0x00139F9C File Offset: 0x0013819C
		// (set) Token: 0x0600592E RID: 22830 RVA: 0x00139FA4 File Offset: 0x001381A4
		public TimeSpan RenewOnCallTime
		{
			[SecurityCritical]
			get
			{
				return this.renewOnCallTime;
			}
			[SecurityCritical]
			[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
			set
			{
				if (this.state == LeaseState.Initial)
				{
					this.renewOnCallTime = value;
					return;
				}
				throw new RemotingException(Environment.GetResourceString("Remoting_Lifetime_InitialStateRenewOnCall", new object[] { this.state.ToString() }));
			}
		}

		// Token: 0x17000ECB RID: 3787
		// (get) Token: 0x0600592F RID: 22831 RVA: 0x00139FDF File Offset: 0x001381DF
		// (set) Token: 0x06005930 RID: 22832 RVA: 0x00139FE7 File Offset: 0x001381E7
		public TimeSpan SponsorshipTimeout
		{
			[SecurityCritical]
			get
			{
				return this.sponsorshipTimeout;
			}
			[SecurityCritical]
			[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
			set
			{
				if (this.state == LeaseState.Initial)
				{
					this.sponsorshipTimeout = value;
					return;
				}
				throw new RemotingException(Environment.GetResourceString("Remoting_Lifetime_InitialStateSponsorshipTimeout", new object[] { this.state.ToString() }));
			}
		}

		// Token: 0x17000ECC RID: 3788
		// (get) Token: 0x06005931 RID: 22833 RVA: 0x0013A022 File Offset: 0x00138222
		// (set) Token: 0x06005932 RID: 22834 RVA: 0x0013A02C File Offset: 0x0013822C
		public TimeSpan InitialLeaseTime
		{
			[SecurityCritical]
			get
			{
				return this.initialLeaseTime;
			}
			[SecurityCritical]
			[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
			set
			{
				if (this.state != LeaseState.Initial)
				{
					throw new RemotingException(Environment.GetResourceString("Remoting_Lifetime_InitialStateInitialLeaseTime", new object[] { this.state.ToString() }));
				}
				this.initialLeaseTime = value;
				if (TimeSpan.Zero.CompareTo(value) >= 0)
				{
					this.state = LeaseState.Null;
					return;
				}
			}
		}

		// Token: 0x17000ECD RID: 3789
		// (get) Token: 0x06005933 RID: 22835 RVA: 0x0013A08B File Offset: 0x0013828B
		public TimeSpan CurrentLeaseTime
		{
			[SecurityCritical]
			get
			{
				return this.leaseTime.Subtract(DateTime.UtcNow);
			}
		}

		// Token: 0x17000ECE RID: 3790
		// (get) Token: 0x06005934 RID: 22836 RVA: 0x0013A09D File Offset: 0x0013829D
		public LeaseState CurrentState
		{
			[SecurityCritical]
			get
			{
				return this.state;
			}
		}

		// Token: 0x06005935 RID: 22837 RVA: 0x0013A0A5 File Offset: 0x001382A5
		[SecurityCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public void Register(ISponsor obj)
		{
			this.Register(obj, TimeSpan.Zero);
		}

		// Token: 0x06005936 RID: 22838 RVA: 0x0013A0B4 File Offset: 0x001382B4
		[SecurityCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public void Register(ISponsor obj, TimeSpan renewalTime)
		{
			lock (this)
			{
				if (this.state != LeaseState.Expired && !(this.sponsorshipTimeout == TimeSpan.Zero))
				{
					object sponsorId = this.GetSponsorId(obj);
					Hashtable obj2 = this.sponsorTable;
					lock (obj2)
					{
						if (renewalTime > TimeSpan.Zero)
						{
							this.AddTime(renewalTime);
						}
						if (!this.sponsorTable.ContainsKey(sponsorId))
						{
							this.sponsorTable[sponsorId] = new Lease.SponsorStateInfo(renewalTime, Lease.SponsorState.Initial);
						}
					}
				}
			}
		}

		// Token: 0x06005937 RID: 22839 RVA: 0x0013A16C File Offset: 0x0013836C
		[SecurityCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public void Unregister(ISponsor sponsor)
		{
			lock (this)
			{
				if (this.state != LeaseState.Expired)
				{
					object sponsorId = this.GetSponsorId(sponsor);
					Hashtable obj = this.sponsorTable;
					lock (obj)
					{
						if (sponsorId != null)
						{
							this.leaseManager.DeleteSponsor(sponsorId);
							Lease.SponsorStateInfo sponsorStateInfo = (Lease.SponsorStateInfo)this.sponsorTable[sponsorId];
							this.sponsorTable.Remove(sponsorId);
						}
					}
				}
			}
		}

		// Token: 0x06005938 RID: 22840 RVA: 0x0013A20C File Offset: 0x0013840C
		[SecurityCritical]
		private object GetSponsorId(ISponsor obj)
		{
			object result = null;
			if (obj != null)
			{
				if (RemotingServices.IsTransparentProxy(obj))
				{
					result = RemotingServices.GetRealProxy(obj);
				}
				else
				{
					result = obj;
				}
			}
			return result;
		}

		// Token: 0x06005939 RID: 22841 RVA: 0x0013A234 File Offset: 0x00138434
		[SecurityCritical]
		private ISponsor GetSponsorFromId(object sponsorId)
		{
			RealProxy realProxy = sponsorId as RealProxy;
			object obj;
			if (realProxy != null)
			{
				obj = realProxy.GetTransparentProxy();
			}
			else
			{
				obj = sponsorId;
			}
			return (ISponsor)obj;
		}

		// Token: 0x0600593A RID: 22842 RVA: 0x0013A25E File Offset: 0x0013845E
		[SecurityCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
		public TimeSpan Renew(TimeSpan renewalTime)
		{
			return this.RenewInternal(renewalTime);
		}

		// Token: 0x0600593B RID: 22843 RVA: 0x0013A268 File Offset: 0x00138468
		internal TimeSpan RenewInternal(TimeSpan renewalTime)
		{
			TimeSpan result;
			lock (this)
			{
				if (this.state == LeaseState.Expired)
				{
					result = TimeSpan.Zero;
				}
				else
				{
					this.AddTime(renewalTime);
					result = this.leaseTime.Subtract(DateTime.UtcNow);
				}
			}
			return result;
		}

		// Token: 0x0600593C RID: 22844 RVA: 0x0013A2C8 File Offset: 0x001384C8
		internal void Remove()
		{
			if (this.state == LeaseState.Expired)
			{
				return;
			}
			this.state = LeaseState.Expired;
			this.leaseManager.DeleteLease(this);
		}

		// Token: 0x0600593D RID: 22845 RVA: 0x0013A2E8 File Offset: 0x001384E8
		[SecurityCritical]
		internal void Cancel()
		{
			lock (this)
			{
				if (this.state != LeaseState.Expired)
				{
					this.Remove();
					RemotingServices.Disconnect(this.managedObject, false);
					RemotingServices.Disconnect(this);
				}
			}
		}

		// Token: 0x0600593E RID: 22846 RVA: 0x0013A344 File Offset: 0x00138544
		internal void RenewOnCall()
		{
			lock (this)
			{
				if (this.state != LeaseState.Initial && this.state != LeaseState.Expired)
				{
					this.AddTime(this.renewOnCallTime);
				}
			}
		}

		// Token: 0x0600593F RID: 22847 RVA: 0x0013A39C File Offset: 0x0013859C
		[SecurityCritical]
		internal void LeaseExpired(DateTime now)
		{
			lock (this)
			{
				if (this.state != LeaseState.Expired)
				{
					if (this.leaseTime.CompareTo(now) < 0)
					{
						this.ProcessNextSponsor();
					}
				}
			}
		}

		// Token: 0x06005940 RID: 22848 RVA: 0x0013A3F4 File Offset: 0x001385F4
		[SecurityCritical]
		internal void SponsorCall(ISponsor sponsor)
		{
			bool flag = false;
			if (this.state == LeaseState.Expired)
			{
				return;
			}
			Hashtable obj = this.sponsorTable;
			lock (obj)
			{
				try
				{
					object sponsorId = this.GetSponsorId(sponsor);
					this.sponsorCallThread = Thread.CurrentThread.GetHashCode();
					Lease.AsyncRenewal asyncRenewal = new Lease.AsyncRenewal(sponsor.Renewal);
					Lease.SponsorStateInfo sponsorStateInfo = (Lease.SponsorStateInfo)this.sponsorTable[sponsorId];
					sponsorStateInfo.sponsorState = Lease.SponsorState.Waiting;
					IAsyncResult asyncResult = asyncRenewal.BeginInvoke(this, new AsyncCallback(this.SponsorCallback), null);
					if (sponsorStateInfo.sponsorState == Lease.SponsorState.Waiting && this.state != LeaseState.Expired)
					{
						this.leaseManager.RegisterSponsorCall(this, sponsorId, this.sponsorshipTimeout);
					}
					this.sponsorCallThread = 0;
				}
				catch (Exception)
				{
					flag = true;
					this.sponsorCallThread = 0;
				}
			}
			if (flag)
			{
				this.Unregister(sponsor);
				this.ProcessNextSponsor();
			}
		}

		// Token: 0x06005941 RID: 22849 RVA: 0x0013A4E8 File Offset: 0x001386E8
		[SecurityCritical]
		internal void SponsorTimeout(object sponsorId)
		{
			lock (this)
			{
				if (this.sponsorTable.ContainsKey(sponsorId))
				{
					Hashtable obj = this.sponsorTable;
					lock (obj)
					{
						Lease.SponsorStateInfo sponsorStateInfo = (Lease.SponsorStateInfo)this.sponsorTable[sponsorId];
						if (sponsorStateInfo.sponsorState == Lease.SponsorState.Waiting)
						{
							this.Unregister(this.GetSponsorFromId(sponsorId));
							this.ProcessNextSponsor();
						}
					}
				}
			}
		}

		// Token: 0x06005942 RID: 22850 RVA: 0x0013A584 File Offset: 0x00138784
		[SecurityCritical]
		private void ProcessNextSponsor()
		{
			object obj = null;
			TimeSpan timeSpan = TimeSpan.Zero;
			Hashtable obj2 = this.sponsorTable;
			lock (obj2)
			{
				IDictionaryEnumerator enumerator = this.sponsorTable.GetEnumerator();
				while (enumerator.MoveNext())
				{
					object key = enumerator.Key;
					Lease.SponsorStateInfo sponsorStateInfo = (Lease.SponsorStateInfo)enumerator.Value;
					if (sponsorStateInfo.sponsorState == Lease.SponsorState.Initial && timeSpan == TimeSpan.Zero)
					{
						timeSpan = sponsorStateInfo.renewalTime;
						obj = key;
					}
					else if (sponsorStateInfo.renewalTime > timeSpan)
					{
						timeSpan = sponsorStateInfo.renewalTime;
						obj = key;
					}
				}
			}
			if (obj != null)
			{
				this.SponsorCall(this.GetSponsorFromId(obj));
				return;
			}
			this.Cancel();
		}

		// Token: 0x06005943 RID: 22851 RVA: 0x0013A64C File Offset: 0x0013884C
		[SecurityCritical]
		internal void SponsorCallback(object obj)
		{
			this.SponsorCallback((IAsyncResult)obj);
		}

		// Token: 0x06005944 RID: 22852 RVA: 0x0013A65C File Offset: 0x0013885C
		[SecurityCritical]
		internal void SponsorCallback(IAsyncResult iar)
		{
			if (this.state == LeaseState.Expired)
			{
				return;
			}
			int hashCode = Thread.CurrentThread.GetHashCode();
			if (hashCode == this.sponsorCallThread)
			{
				WaitCallback callBack = new WaitCallback(this.SponsorCallback);
				ThreadPool.QueueUserWorkItem(callBack, iar);
				return;
			}
			AsyncResult asyncResult = (AsyncResult)iar;
			Lease.AsyncRenewal asyncRenewal = (Lease.AsyncRenewal)asyncResult.AsyncDelegate;
			ISponsor sponsor = (ISponsor)asyncRenewal.Target;
			Lease.SponsorStateInfo sponsorStateInfo = null;
			if (!iar.IsCompleted)
			{
				this.Unregister(sponsor);
				this.ProcessNextSponsor();
				return;
			}
			bool flag = false;
			TimeSpan renewalTime = TimeSpan.Zero;
			try
			{
				renewalTime = asyncRenewal.EndInvoke(iar);
			}
			catch (Exception)
			{
				flag = true;
			}
			if (flag)
			{
				this.Unregister(sponsor);
				this.ProcessNextSponsor();
				return;
			}
			object sponsorId = this.GetSponsorId(sponsor);
			Hashtable obj = this.sponsorTable;
			lock (obj)
			{
				if (this.sponsorTable.ContainsKey(sponsorId))
				{
					sponsorStateInfo = (Lease.SponsorStateInfo)this.sponsorTable[sponsorId];
					sponsorStateInfo.sponsorState = Lease.SponsorState.Completed;
					sponsorStateInfo.renewalTime = renewalTime;
				}
			}
			if (sponsorStateInfo == null)
			{
				this.ProcessNextSponsor();
				return;
			}
			if (sponsorStateInfo.renewalTime == TimeSpan.Zero)
			{
				this.Unregister(sponsor);
				this.ProcessNextSponsor();
				return;
			}
			this.RenewInternal(sponsorStateInfo.renewalTime);
		}

		// Token: 0x06005945 RID: 22853 RVA: 0x0013A7BC File Offset: 0x001389BC
		private void AddTime(TimeSpan renewalSpan)
		{
			if (this.state == LeaseState.Expired)
			{
				return;
			}
			DateTime utcNow = DateTime.UtcNow;
			DateTime dateTime = this.leaseTime;
			DateTime dateTime2 = utcNow.Add(renewalSpan);
			if (this.leaseTime.CompareTo(dateTime2) < 0)
			{
				this.leaseManager.ChangedLeaseTime(this, dateTime2);
				this.leaseTime = dateTime2;
				this.state = LeaseState.Active;
			}
		}

		// Token: 0x0400289D RID: 10397
		internal int id;

		// Token: 0x0400289E RID: 10398
		internal DateTime leaseTime;

		// Token: 0x0400289F RID: 10399
		internal TimeSpan initialLeaseTime;

		// Token: 0x040028A0 RID: 10400
		internal TimeSpan renewOnCallTime;

		// Token: 0x040028A1 RID: 10401
		internal TimeSpan sponsorshipTimeout;

		// Token: 0x040028A2 RID: 10402
		internal Hashtable sponsorTable;

		// Token: 0x040028A3 RID: 10403
		internal int sponsorCallThread;

		// Token: 0x040028A4 RID: 10404
		internal LeaseManager leaseManager;

		// Token: 0x040028A5 RID: 10405
		internal MarshalByRefObject managedObject;

		// Token: 0x040028A6 RID: 10406
		internal LeaseState state;

		// Token: 0x040028A7 RID: 10407
		internal static volatile int nextId;

		// Token: 0x02000C73 RID: 3187
		// (Invoke) Token: 0x060070B3 RID: 28851
		internal delegate TimeSpan AsyncRenewal(ILease lease);

		// Token: 0x02000C74 RID: 3188
		[Serializable]
		internal enum SponsorState
		{
			// Token: 0x040037FB RID: 14331
			Initial,
			// Token: 0x040037FC RID: 14332
			Waiting,
			// Token: 0x040037FD RID: 14333
			Completed
		}

		// Token: 0x02000C75 RID: 3189
		internal sealed class SponsorStateInfo
		{
			// Token: 0x060070B6 RID: 28854 RVA: 0x00184A08 File Offset: 0x00182C08
			internal SponsorStateInfo(TimeSpan renewalTime, Lease.SponsorState sponsorState)
			{
				this.renewalTime = renewalTime;
				this.sponsorState = sponsorState;
			}

			// Token: 0x040037FE RID: 14334
			internal TimeSpan renewalTime;

			// Token: 0x040037FF RID: 14335
			internal Lease.SponsorState sponsorState;
		}
	}
}
