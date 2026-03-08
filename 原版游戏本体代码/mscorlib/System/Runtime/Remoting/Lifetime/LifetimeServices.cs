using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace System.Runtime.Remoting.Lifetime
{
	// Token: 0x02000824 RID: 2084
	[SecurityCritical]
	[ComVisible(true)]
	public sealed class LifetimeServices
	{
		// Token: 0x06005957 RID: 22871 RVA: 0x0013ADBC File Offset: 0x00138FBC
		private static TimeSpan GetTimeSpan(ref long ticks)
		{
			return TimeSpan.FromTicks(Volatile.Read(ref ticks));
		}

		// Token: 0x06005958 RID: 22872 RVA: 0x0013ADC9 File Offset: 0x00138FC9
		private static void SetTimeSpan(ref long ticks, TimeSpan value)
		{
			Volatile.Write(ref ticks, value.Ticks);
		}

		// Token: 0x17000ED0 RID: 3792
		// (get) Token: 0x06005959 RID: 22873 RVA: 0x0013ADD8 File Offset: 0x00138FD8
		private static object LifetimeSyncObject
		{
			get
			{
				if (LifetimeServices.s_LifetimeSyncObject == null)
				{
					object value = new object();
					Interlocked.CompareExchange(ref LifetimeServices.s_LifetimeSyncObject, value, null);
				}
				return LifetimeServices.s_LifetimeSyncObject;
			}
		}

		// Token: 0x0600595A RID: 22874 RVA: 0x0013AE04 File Offset: 0x00139004
		[Obsolete("Do not create instances of the LifetimeServices class.  Call the static methods directly on this type instead", true)]
		public LifetimeServices()
		{
		}

		// Token: 0x17000ED1 RID: 3793
		// (get) Token: 0x0600595B RID: 22875 RVA: 0x0013AE0C File Offset: 0x0013900C
		// (set) Token: 0x0600595C RID: 22876 RVA: 0x0013AE18 File Offset: 0x00139018
		public static TimeSpan LeaseTime
		{
			get
			{
				return LifetimeServices.GetTimeSpan(ref LifetimeServices.s_leaseTimeTicks);
			}
			[SecurityCritical]
			[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
			set
			{
				object lifetimeSyncObject = LifetimeServices.LifetimeSyncObject;
				lock (lifetimeSyncObject)
				{
					if (LifetimeServices.s_isLeaseTime)
					{
						throw new RemotingException(Environment.GetResourceString("Remoting_Lifetime_SetOnce", new object[] { "LeaseTime" }));
					}
					LifetimeServices.SetTimeSpan(ref LifetimeServices.s_leaseTimeTicks, value);
					LifetimeServices.s_isLeaseTime = true;
				}
			}
		}

		// Token: 0x17000ED2 RID: 3794
		// (get) Token: 0x0600595D RID: 22877 RVA: 0x0013AE88 File Offset: 0x00139088
		// (set) Token: 0x0600595E RID: 22878 RVA: 0x0013AE94 File Offset: 0x00139094
		public static TimeSpan RenewOnCallTime
		{
			get
			{
				return LifetimeServices.GetTimeSpan(ref LifetimeServices.s_renewOnCallTimeTicks);
			}
			[SecurityCritical]
			[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
			set
			{
				object lifetimeSyncObject = LifetimeServices.LifetimeSyncObject;
				lock (lifetimeSyncObject)
				{
					if (LifetimeServices.s_isRenewOnCallTime)
					{
						throw new RemotingException(Environment.GetResourceString("Remoting_Lifetime_SetOnce", new object[] { "RenewOnCallTime" }));
					}
					LifetimeServices.SetTimeSpan(ref LifetimeServices.s_renewOnCallTimeTicks, value);
					LifetimeServices.s_isRenewOnCallTime = true;
				}
			}
		}

		// Token: 0x17000ED3 RID: 3795
		// (get) Token: 0x0600595F RID: 22879 RVA: 0x0013AF04 File Offset: 0x00139104
		// (set) Token: 0x06005960 RID: 22880 RVA: 0x0013AF10 File Offset: 0x00139110
		public static TimeSpan SponsorshipTimeout
		{
			get
			{
				return LifetimeServices.GetTimeSpan(ref LifetimeServices.s_sponsorshipTimeoutTicks);
			}
			[SecurityCritical]
			[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
			set
			{
				object lifetimeSyncObject = LifetimeServices.LifetimeSyncObject;
				lock (lifetimeSyncObject)
				{
					if (LifetimeServices.s_isSponsorshipTimeout)
					{
						throw new RemotingException(Environment.GetResourceString("Remoting_Lifetime_SetOnce", new object[] { "SponsorshipTimeout" }));
					}
					LifetimeServices.SetTimeSpan(ref LifetimeServices.s_sponsorshipTimeoutTicks, value);
					LifetimeServices.s_isSponsorshipTimeout = true;
				}
			}
		}

		// Token: 0x17000ED4 RID: 3796
		// (get) Token: 0x06005961 RID: 22881 RVA: 0x0013AF80 File Offset: 0x00139180
		// (set) Token: 0x06005962 RID: 22882 RVA: 0x0013AF8C File Offset: 0x0013918C
		public static TimeSpan LeaseManagerPollTime
		{
			get
			{
				return LifetimeServices.GetTimeSpan(ref LifetimeServices.s_pollTimeTicks);
			}
			[SecurityCritical]
			[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
			set
			{
				object lifetimeSyncObject = LifetimeServices.LifetimeSyncObject;
				lock (lifetimeSyncObject)
				{
					LifetimeServices.SetTimeSpan(ref LifetimeServices.s_pollTimeTicks, value);
					if (LeaseManager.IsInitialized())
					{
						LeaseManager.GetLeaseManager().ChangePollTime(value);
					}
				}
			}
		}

		// Token: 0x06005963 RID: 22883 RVA: 0x0013AFE4 File Offset: 0x001391E4
		[SecurityCritical]
		internal static ILease GetLeaseInitial(MarshalByRefObject obj)
		{
			LeaseManager leaseManager = LeaseManager.GetLeaseManager(LifetimeServices.LeaseManagerPollTime);
			ILease lease = leaseManager.GetLease(obj);
			if (lease == null)
			{
				lease = LifetimeServices.CreateLease(obj);
			}
			return lease;
		}

		// Token: 0x06005964 RID: 22884 RVA: 0x0013B014 File Offset: 0x00139214
		[SecurityCritical]
		internal static ILease GetLease(MarshalByRefObject obj)
		{
			LeaseManager leaseManager = LeaseManager.GetLeaseManager(LifetimeServices.LeaseManagerPollTime);
			return leaseManager.GetLease(obj);
		}

		// Token: 0x06005965 RID: 22885 RVA: 0x0013B037 File Offset: 0x00139237
		[SecurityCritical]
		internal static ILease CreateLease(MarshalByRefObject obj)
		{
			return LifetimeServices.CreateLease(LifetimeServices.LeaseTime, LifetimeServices.RenewOnCallTime, LifetimeServices.SponsorshipTimeout, obj);
		}

		// Token: 0x06005966 RID: 22886 RVA: 0x0013B04E File Offset: 0x0013924E
		[SecurityCritical]
		internal static ILease CreateLease(TimeSpan leaseTime, TimeSpan renewOnCallTime, TimeSpan sponsorshipTimeout, MarshalByRefObject obj)
		{
			LeaseManager.GetLeaseManager(LifetimeServices.LeaseManagerPollTime);
			return new Lease(leaseTime, renewOnCallTime, sponsorshipTimeout, obj);
		}

		// Token: 0x040028B7 RID: 10423
		private static bool s_isLeaseTime = false;

		// Token: 0x040028B8 RID: 10424
		private static bool s_isRenewOnCallTime = false;

		// Token: 0x040028B9 RID: 10425
		private static bool s_isSponsorshipTimeout = false;

		// Token: 0x040028BA RID: 10426
		private static long s_leaseTimeTicks = TimeSpan.FromMinutes(5.0).Ticks;

		// Token: 0x040028BB RID: 10427
		private static long s_renewOnCallTimeTicks = TimeSpan.FromMinutes(2.0).Ticks;

		// Token: 0x040028BC RID: 10428
		private static long s_sponsorshipTimeoutTicks = TimeSpan.FromMinutes(2.0).Ticks;

		// Token: 0x040028BD RID: 10429
		private static long s_pollTimeTicks = TimeSpan.FromMilliseconds(10000.0).Ticks;

		// Token: 0x040028BE RID: 10430
		private static object s_LifetimeSyncObject = null;
	}
}
