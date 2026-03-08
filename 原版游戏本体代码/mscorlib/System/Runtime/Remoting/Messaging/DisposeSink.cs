using System;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000886 RID: 2182
	internal class DisposeSink : IMessageSink
	{
		// Token: 0x06005C99 RID: 23705 RVA: 0x00144E9B File Offset: 0x0014309B
		internal DisposeSink(IDisposable iDis, IMessageSink replySink)
		{
			this._iDis = iDis;
			this._replySink = replySink;
		}

		// Token: 0x06005C9A RID: 23706 RVA: 0x00144EB4 File Offset: 0x001430B4
		[SecurityCritical]
		public virtual IMessage SyncProcessMessage(IMessage reqMsg)
		{
			IMessage result = null;
			try
			{
				if (this._replySink != null)
				{
					result = this._replySink.SyncProcessMessage(reqMsg);
				}
			}
			finally
			{
				this._iDis.Dispose();
			}
			return result;
		}

		// Token: 0x06005C9B RID: 23707 RVA: 0x00144EF8 File Offset: 0x001430F8
		[SecurityCritical]
		public virtual IMessageCtrl AsyncProcessMessage(IMessage reqMsg, IMessageSink replySink)
		{
			throw new NotSupportedException();
		}

		// Token: 0x17000FE8 RID: 4072
		// (get) Token: 0x06005C9C RID: 23708 RVA: 0x00144EFF File Offset: 0x001430FF
		public IMessageSink NextSink
		{
			[SecurityCritical]
			get
			{
				return this._replySink;
			}
		}

		// Token: 0x040029D0 RID: 10704
		private IDisposable _iDis;

		// Token: 0x040029D1 RID: 10705
		private IMessageSink _replySink;
	}
}
