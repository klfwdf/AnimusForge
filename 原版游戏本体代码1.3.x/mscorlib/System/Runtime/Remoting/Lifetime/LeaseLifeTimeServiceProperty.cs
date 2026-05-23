using System;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting.Lifetime
{
	// Token: 0x02000825 RID: 2085
	[Serializable]
	internal class LeaseLifeTimeServiceProperty : IContextProperty, IContributeObjectSink
	{
		// Token: 0x17000ED5 RID: 3797
		// (get) Token: 0x06005968 RID: 22888 RVA: 0x0013B0F5 File Offset: 0x001392F5
		public string Name
		{
			[SecurityCritical]
			get
			{
				return "LeaseLifeTimeServiceProperty";
			}
		}

		// Token: 0x06005969 RID: 22889 RVA: 0x0013B0FC File Offset: 0x001392FC
		[SecurityCritical]
		public bool IsNewContextOK(Context newCtx)
		{
			return true;
		}

		// Token: 0x0600596A RID: 22890 RVA: 0x0013B0FF File Offset: 0x001392FF
		[SecurityCritical]
		public void Freeze(Context newContext)
		{
		}

		// Token: 0x0600596B RID: 22891 RVA: 0x0013B104 File Offset: 0x00139304
		[SecurityCritical]
		public IMessageSink GetObjectSink(MarshalByRefObject obj, IMessageSink nextSink)
		{
			bool flag;
			ServerIdentity serverIdentity = (ServerIdentity)MarshalByRefObject.GetIdentity(obj, out flag);
			if (serverIdentity.IsSingleCall())
			{
				return nextSink;
			}
			object obj2 = obj.InitializeLifetimeService();
			if (obj2 == null)
			{
				return nextSink;
			}
			if (!(obj2 is ILease))
			{
				throw new RemotingException(Environment.GetResourceString("Remoting_Lifetime_ILeaseReturn", new object[] { obj2 }));
			}
			ILease lease = (ILease)obj2;
			if (lease.InitialLeaseTime.CompareTo(TimeSpan.Zero) <= 0)
			{
				if (lease is Lease)
				{
					((Lease)lease).Remove();
				}
				return nextSink;
			}
			Lease lease2 = null;
			ServerIdentity obj3 = serverIdentity;
			lock (obj3)
			{
				if (serverIdentity.Lease != null)
				{
					lease2 = serverIdentity.Lease;
					lease2.Renew(lease2.InitialLeaseTime);
				}
				else
				{
					if (!(lease is Lease))
					{
						lease2 = (Lease)LifetimeServices.GetLeaseInitial(obj);
						if (lease2.CurrentState == LeaseState.Initial)
						{
							lease2.InitialLeaseTime = lease.InitialLeaseTime;
							lease2.RenewOnCallTime = lease.RenewOnCallTime;
							lease2.SponsorshipTimeout = lease.SponsorshipTimeout;
						}
					}
					else
					{
						lease2 = (Lease)lease;
					}
					serverIdentity.Lease = lease2;
					if (serverIdentity.ObjectRef != null)
					{
						lease2.ActivateLease();
					}
				}
			}
			if (lease2.RenewOnCallTime > TimeSpan.Zero)
			{
				return new LeaseSink(lease2, nextSink);
			}
			return nextSink;
		}
	}
}
