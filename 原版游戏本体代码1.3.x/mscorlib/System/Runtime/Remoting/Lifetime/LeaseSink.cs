using System;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting.Lifetime
{
	// Token: 0x02000821 RID: 2081
	internal class LeaseSink : IMessageSink
	{
		// Token: 0x06005946 RID: 22854 RVA: 0x0013A813 File Offset: 0x00138A13
		public LeaseSink(Lease lease, IMessageSink nextSink)
		{
			this.lease = lease;
			this.nextSink = nextSink;
		}

		// Token: 0x06005947 RID: 22855 RVA: 0x0013A829 File Offset: 0x00138A29
		[SecurityCritical]
		public IMessage SyncProcessMessage(IMessage msg)
		{
			this.lease.RenewOnCall();
			return this.nextSink.SyncProcessMessage(msg);
		}

		// Token: 0x06005948 RID: 22856 RVA: 0x0013A842 File Offset: 0x00138A42
		[SecurityCritical]
		public IMessageCtrl AsyncProcessMessage(IMessage msg, IMessageSink replySink)
		{
			this.lease.RenewOnCall();
			return this.nextSink.AsyncProcessMessage(msg, replySink);
		}

		// Token: 0x17000ECF RID: 3791
		// (get) Token: 0x06005949 RID: 22857 RVA: 0x0013A85C File Offset: 0x00138A5C
		public IMessageSink NextSink
		{
			[SecurityCritical]
			get
			{
				return this.nextSink;
			}
		}

		// Token: 0x040028A8 RID: 10408
		private Lease lease;

		// Token: 0x040028A9 RID: 10409
		private IMessageSink nextSink;
	}
}
