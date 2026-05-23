using System;
using System.Collections;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x0200088D RID: 2189
	internal class IllogicalCallContext
	{
		// Token: 0x17000FED RID: 4077
		// (get) Token: 0x06005CBA RID: 23738 RVA: 0x001452F4 File Offset: 0x001434F4
		private Hashtable Datastore
		{
			get
			{
				if (this.m_Datastore == null)
				{
					this.m_Datastore = new Hashtable();
				}
				return this.m_Datastore;
			}
		}

		// Token: 0x17000FEE RID: 4078
		// (get) Token: 0x06005CBB RID: 23739 RVA: 0x0014530F File Offset: 0x0014350F
		// (set) Token: 0x06005CBC RID: 23740 RVA: 0x00145317 File Offset: 0x00143517
		internal object HostContext
		{
			get
			{
				return this.m_HostContext;
			}
			set
			{
				this.m_HostContext = value;
			}
		}

		// Token: 0x17000FEF RID: 4079
		// (get) Token: 0x06005CBD RID: 23741 RVA: 0x00145320 File Offset: 0x00143520
		internal bool HasUserData
		{
			get
			{
				return this.m_Datastore != null && this.m_Datastore.Count > 0;
			}
		}

		// Token: 0x06005CBE RID: 23742 RVA: 0x0014533A File Offset: 0x0014353A
		public void FreeNamedDataSlot(string name)
		{
			this.Datastore.Remove(name);
		}

		// Token: 0x06005CBF RID: 23743 RVA: 0x00145348 File Offset: 0x00143548
		public object GetData(string name)
		{
			return this.Datastore[name];
		}

		// Token: 0x06005CC0 RID: 23744 RVA: 0x00145356 File Offset: 0x00143556
		public void SetData(string name, object data)
		{
			this.Datastore[name] = data;
		}

		// Token: 0x06005CC1 RID: 23745 RVA: 0x00145368 File Offset: 0x00143568
		public IllogicalCallContext CreateCopy()
		{
			IllogicalCallContext illogicalCallContext = new IllogicalCallContext();
			illogicalCallContext.HostContext = this.HostContext;
			if (this.HasUserData)
			{
				IDictionaryEnumerator enumerator = this.m_Datastore.GetEnumerator();
				while (enumerator.MoveNext())
				{
					illogicalCallContext.Datastore[(string)enumerator.Key] = enumerator.Value;
				}
			}
			return illogicalCallContext;
		}

		// Token: 0x040029D8 RID: 10712
		private Hashtable m_Datastore;

		// Token: 0x040029D9 RID: 10713
		private object m_HostContext;

		// Token: 0x02000C7C RID: 3196
		internal struct Reader
		{
			// Token: 0x060070C2 RID: 28866 RVA: 0x00184CD4 File Offset: 0x00182ED4
			public Reader(IllogicalCallContext ctx)
			{
				this.m_ctx = ctx;
			}

			// Token: 0x17001356 RID: 4950
			// (get) Token: 0x060070C3 RID: 28867 RVA: 0x00184CDD File Offset: 0x00182EDD
			public bool IsNull
			{
				get
				{
					return this.m_ctx == null;
				}
			}

			// Token: 0x060070C4 RID: 28868 RVA: 0x00184CE8 File Offset: 0x00182EE8
			[SecurityCritical]
			public object GetData(string name)
			{
				if (!this.IsNull)
				{
					return this.m_ctx.GetData(name);
				}
				return null;
			}

			// Token: 0x17001357 RID: 4951
			// (get) Token: 0x060070C5 RID: 28869 RVA: 0x00184D00 File Offset: 0x00182F00
			public object HostContext
			{
				get
				{
					if (!this.IsNull)
					{
						return this.m_ctx.HostContext;
					}
					return null;
				}
			}

			// Token: 0x0400380E RID: 14350
			private IllogicalCallContext m_ctx;
		}
	}
}
