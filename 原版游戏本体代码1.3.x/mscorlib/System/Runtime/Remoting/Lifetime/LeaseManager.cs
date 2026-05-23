using System;
using System.Collections;
using System.Diagnostics;
using System.Security;
using System.Threading;

namespace System.Runtime.Remoting.Lifetime
{
	// Token: 0x02000822 RID: 2082
	internal class LeaseManager
	{
		// Token: 0x0600594A RID: 22858 RVA: 0x0013A864 File Offset: 0x00138A64
		internal static bool IsInitialized()
		{
			DomainSpecificRemotingData remotingData = Thread.GetDomain().RemotingData;
			LeaseManager leaseManager = remotingData.LeaseManager;
			return leaseManager != null;
		}

		// Token: 0x0600594B RID: 22859 RVA: 0x0013A888 File Offset: 0x00138A88
		[SecurityCritical]
		internal static LeaseManager GetLeaseManager(TimeSpan pollTime)
		{
			DomainSpecificRemotingData remotingData = Thread.GetDomain().RemotingData;
			LeaseManager leaseManager = remotingData.LeaseManager;
			if (leaseManager == null)
			{
				DomainSpecificRemotingData obj = remotingData;
				lock (obj)
				{
					if (remotingData.LeaseManager == null)
					{
						remotingData.LeaseManager = new LeaseManager(pollTime);
					}
					leaseManager = remotingData.LeaseManager;
				}
			}
			return leaseManager;
		}

		// Token: 0x0600594C RID: 22860 RVA: 0x0013A8F0 File Offset: 0x00138AF0
		internal static LeaseManager GetLeaseManager()
		{
			DomainSpecificRemotingData remotingData = Thread.GetDomain().RemotingData;
			return remotingData.LeaseManager;
		}

		// Token: 0x0600594D RID: 22861 RVA: 0x0013A910 File Offset: 0x00138B10
		[SecurityCritical]
		private LeaseManager(TimeSpan pollTime)
		{
			this.pollTime = pollTime;
			this.leaseTimeAnalyzerDelegate = new TimerCallback(this.LeaseTimeAnalyzer);
			this.waitHandle = new AutoResetEvent(false);
			this.leaseTimer = new Timer(this.leaseTimeAnalyzerDelegate, null, -1, -1);
			this.leaseTimer.Change((int)pollTime.TotalMilliseconds, -1);
		}

		// Token: 0x0600594E RID: 22862 RVA: 0x0013A998 File Offset: 0x00138B98
		internal void ChangePollTime(TimeSpan pollTime)
		{
			this.pollTime = pollTime;
		}

		// Token: 0x0600594F RID: 22863 RVA: 0x0013A9A4 File Offset: 0x00138BA4
		internal void ActivateLease(Lease lease)
		{
			Hashtable obj = this.leaseToTimeTable;
			lock (obj)
			{
				this.leaseToTimeTable[lease] = lease.leaseTime;
			}
		}

		// Token: 0x06005950 RID: 22864 RVA: 0x0013A9F8 File Offset: 0x00138BF8
		internal void DeleteLease(Lease lease)
		{
			Hashtable obj = this.leaseToTimeTable;
			lock (obj)
			{
				this.leaseToTimeTable.Remove(lease);
			}
		}

		// Token: 0x06005951 RID: 22865 RVA: 0x0013AA40 File Offset: 0x00138C40
		[Conditional("_LOGGING")]
		internal void DumpLeases(Lease[] leases)
		{
			for (int i = 0; i < leases.Length; i++)
			{
			}
		}

		// Token: 0x06005952 RID: 22866 RVA: 0x0013AA5C File Offset: 0x00138C5C
		internal ILease GetLease(MarshalByRefObject obj)
		{
			bool flag = true;
			Identity identity = MarshalByRefObject.GetIdentity(obj, out flag);
			if (identity == null)
			{
				return null;
			}
			return identity.Lease;
		}

		// Token: 0x06005953 RID: 22867 RVA: 0x0013AA80 File Offset: 0x00138C80
		internal void ChangedLeaseTime(Lease lease, DateTime newTime)
		{
			Hashtable obj = this.leaseToTimeTable;
			lock (obj)
			{
				this.leaseToTimeTable[lease] = newTime;
			}
		}

		// Token: 0x06005954 RID: 22868 RVA: 0x0013AACC File Offset: 0x00138CCC
		internal void RegisterSponsorCall(Lease lease, object sponsorId, TimeSpan sponsorshipTimeOut)
		{
			Hashtable obj = this.sponsorTable;
			lock (obj)
			{
				DateTime sponsorWaitTime = DateTime.UtcNow.Add(sponsorshipTimeOut);
				this.sponsorTable[sponsorId] = new LeaseManager.SponsorInfo(lease, sponsorId, sponsorWaitTime);
			}
		}

		// Token: 0x06005955 RID: 22869 RVA: 0x0013AB2C File Offset: 0x00138D2C
		internal void DeleteSponsor(object sponsorId)
		{
			Hashtable obj = this.sponsorTable;
			lock (obj)
			{
				this.sponsorTable.Remove(sponsorId);
			}
		}

		// Token: 0x06005956 RID: 22870 RVA: 0x0013AB74 File Offset: 0x00138D74
		[SecurityCritical]
		private void LeaseTimeAnalyzer(object state)
		{
			DateTime utcNow = DateTime.UtcNow;
			Hashtable obj = this.leaseToTimeTable;
			lock (obj)
			{
				IDictionaryEnumerator enumerator = this.leaseToTimeTable.GetEnumerator();
				while (enumerator.MoveNext())
				{
					DateTime dateTime = (DateTime)enumerator.Value;
					Lease value = (Lease)enumerator.Key;
					if (dateTime.CompareTo(utcNow) < 0)
					{
						this.tempObjects.Add(value);
					}
				}
				for (int i = 0; i < this.tempObjects.Count; i++)
				{
					Lease key = (Lease)this.tempObjects[i];
					this.leaseToTimeTable.Remove(key);
				}
			}
			for (int j = 0; j < this.tempObjects.Count; j++)
			{
				Lease lease = (Lease)this.tempObjects[j];
				if (lease != null)
				{
					lease.LeaseExpired(utcNow);
				}
			}
			this.tempObjects.Clear();
			Hashtable obj2 = this.sponsorTable;
			lock (obj2)
			{
				IDictionaryEnumerator enumerator2 = this.sponsorTable.GetEnumerator();
				while (enumerator2.MoveNext())
				{
					object key2 = enumerator2.Key;
					LeaseManager.SponsorInfo sponsorInfo = (LeaseManager.SponsorInfo)enumerator2.Value;
					if (sponsorInfo.sponsorWaitTime.CompareTo(utcNow) < 0)
					{
						this.tempObjects.Add(sponsorInfo);
					}
				}
				for (int k = 0; k < this.tempObjects.Count; k++)
				{
					LeaseManager.SponsorInfo sponsorInfo2 = (LeaseManager.SponsorInfo)this.tempObjects[k];
					this.sponsorTable.Remove(sponsorInfo2.sponsorId);
				}
			}
			for (int l = 0; l < this.tempObjects.Count; l++)
			{
				LeaseManager.SponsorInfo sponsorInfo3 = (LeaseManager.SponsorInfo)this.tempObjects[l];
				if (sponsorInfo3 != null && sponsorInfo3.lease != null)
				{
					sponsorInfo3.lease.SponsorTimeout(sponsorInfo3.sponsorId);
					this.tempObjects[l] = null;
				}
			}
			this.tempObjects.Clear();
			this.leaseTimer.Change((int)this.pollTime.TotalMilliseconds, -1);
		}

		// Token: 0x040028AA RID: 10410
		private Hashtable leaseToTimeTable = new Hashtable();

		// Token: 0x040028AB RID: 10411
		private Hashtable sponsorTable = new Hashtable();

		// Token: 0x040028AC RID: 10412
		private TimeSpan pollTime;

		// Token: 0x040028AD RID: 10413
		private AutoResetEvent waitHandle;

		// Token: 0x040028AE RID: 10414
		private TimerCallback leaseTimeAnalyzerDelegate;

		// Token: 0x040028AF RID: 10415
		private volatile Timer leaseTimer;

		// Token: 0x040028B0 RID: 10416
		private ArrayList tempObjects = new ArrayList(10);

		// Token: 0x02000C76 RID: 3190
		internal class SponsorInfo
		{
			// Token: 0x060070B7 RID: 28855 RVA: 0x00184A1E File Offset: 0x00182C1E
			internal SponsorInfo(Lease lease, object sponsorId, DateTime sponsorWaitTime)
			{
				this.lease = lease;
				this.sponsorId = sponsorId;
				this.sponsorWaitTime = sponsorWaitTime;
			}

			// Token: 0x04003800 RID: 14336
			internal Lease lease;

			// Token: 0x04003801 RID: 14337
			internal object sponsorId;

			// Token: 0x04003802 RID: 14338
			internal DateTime sponsorWaitTime;
		}
	}
}
