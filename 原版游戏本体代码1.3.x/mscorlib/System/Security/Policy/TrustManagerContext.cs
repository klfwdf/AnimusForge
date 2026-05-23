using System;
using System.Runtime.InteropServices;

namespace System.Security.Policy
{
	// Token: 0x0200035D RID: 861
	[ComVisible(true)]
	public class TrustManagerContext
	{
		// Token: 0x06002A7D RID: 10877 RVA: 0x0009D282 File Offset: 0x0009B482
		public TrustManagerContext()
			: this(TrustManagerUIContext.Run)
		{
		}

		// Token: 0x06002A7E RID: 10878 RVA: 0x0009D28B File Offset: 0x0009B48B
		public TrustManagerContext(TrustManagerUIContext uiContext)
		{
			this.m_ignorePersistedDecision = false;
			this.m_uiContext = uiContext;
			this.m_keepAlive = false;
			this.m_persist = true;
		}

		// Token: 0x170005A4 RID: 1444
		// (get) Token: 0x06002A7F RID: 10879 RVA: 0x0009D2AF File Offset: 0x0009B4AF
		// (set) Token: 0x06002A80 RID: 10880 RVA: 0x0009D2B7 File Offset: 0x0009B4B7
		public virtual TrustManagerUIContext UIContext
		{
			get
			{
				return this.m_uiContext;
			}
			set
			{
				this.m_uiContext = value;
			}
		}

		// Token: 0x170005A5 RID: 1445
		// (get) Token: 0x06002A81 RID: 10881 RVA: 0x0009D2C0 File Offset: 0x0009B4C0
		// (set) Token: 0x06002A82 RID: 10882 RVA: 0x0009D2C8 File Offset: 0x0009B4C8
		public virtual bool NoPrompt
		{
			get
			{
				return this.m_noPrompt;
			}
			set
			{
				this.m_noPrompt = value;
			}
		}

		// Token: 0x170005A6 RID: 1446
		// (get) Token: 0x06002A83 RID: 10883 RVA: 0x0009D2D1 File Offset: 0x0009B4D1
		// (set) Token: 0x06002A84 RID: 10884 RVA: 0x0009D2D9 File Offset: 0x0009B4D9
		public virtual bool IgnorePersistedDecision
		{
			get
			{
				return this.m_ignorePersistedDecision;
			}
			set
			{
				this.m_ignorePersistedDecision = value;
			}
		}

		// Token: 0x170005A7 RID: 1447
		// (get) Token: 0x06002A85 RID: 10885 RVA: 0x0009D2E2 File Offset: 0x0009B4E2
		// (set) Token: 0x06002A86 RID: 10886 RVA: 0x0009D2EA File Offset: 0x0009B4EA
		public virtual bool KeepAlive
		{
			get
			{
				return this.m_keepAlive;
			}
			set
			{
				this.m_keepAlive = value;
			}
		}

		// Token: 0x170005A8 RID: 1448
		// (get) Token: 0x06002A87 RID: 10887 RVA: 0x0009D2F3 File Offset: 0x0009B4F3
		// (set) Token: 0x06002A88 RID: 10888 RVA: 0x0009D2FB File Offset: 0x0009B4FB
		public virtual bool Persist
		{
			get
			{
				return this.m_persist;
			}
			set
			{
				this.m_persist = value;
			}
		}

		// Token: 0x170005A9 RID: 1449
		// (get) Token: 0x06002A89 RID: 10889 RVA: 0x0009D304 File Offset: 0x0009B504
		// (set) Token: 0x06002A8A RID: 10890 RVA: 0x0009D30C File Offset: 0x0009B50C
		public virtual ApplicationIdentity PreviousApplicationIdentity
		{
			get
			{
				return this.m_appId;
			}
			set
			{
				this.m_appId = value;
			}
		}

		// Token: 0x04001148 RID: 4424
		private bool m_ignorePersistedDecision;

		// Token: 0x04001149 RID: 4425
		private TrustManagerUIContext m_uiContext;

		// Token: 0x0400114A RID: 4426
		private bool m_noPrompt;

		// Token: 0x0400114B RID: 4427
		private bool m_keepAlive;

		// Token: 0x0400114C RID: 4428
		private bool m_persist;

		// Token: 0x0400114D RID: 4429
		private ApplicationIdentity m_appId;
	}
}
