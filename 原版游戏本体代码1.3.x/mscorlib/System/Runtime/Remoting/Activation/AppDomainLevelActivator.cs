using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System.Runtime.Remoting.Activation
{
	// Token: 0x02000894 RID: 2196
	[Serializable]
	internal class AppDomainLevelActivator : IActivator
	{
		// Token: 0x06005D11 RID: 23825 RVA: 0x00146942 File Offset: 0x00144B42
		internal AppDomainLevelActivator(string remActivatorURL)
		{
			this.m_RemActivatorURL = remActivatorURL;
		}

		// Token: 0x06005D12 RID: 23826 RVA: 0x00146951 File Offset: 0x00144B51
		internal AppDomainLevelActivator(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			this.m_NextActivator = (IActivator)info.GetValue("m_NextActivator", typeof(IActivator));
		}

		// Token: 0x17001000 RID: 4096
		// (get) Token: 0x06005D13 RID: 23827 RVA: 0x00146987 File Offset: 0x00144B87
		// (set) Token: 0x06005D14 RID: 23828 RVA: 0x0014698F File Offset: 0x00144B8F
		public virtual IActivator NextActivator
		{
			[SecurityCritical]
			get
			{
				return this.m_NextActivator;
			}
			[SecurityCritical]
			set
			{
				this.m_NextActivator = value;
			}
		}

		// Token: 0x17001001 RID: 4097
		// (get) Token: 0x06005D15 RID: 23829 RVA: 0x00146998 File Offset: 0x00144B98
		public virtual ActivatorLevel Level
		{
			[SecurityCritical]
			get
			{
				return ActivatorLevel.AppDomain;
			}
		}

		// Token: 0x06005D16 RID: 23830 RVA: 0x0014699C File Offset: 0x00144B9C
		[SecurityCritical]
		[ComVisible(true)]
		public virtual IConstructionReturnMessage Activate(IConstructionCallMessage ctorMsg)
		{
			ctorMsg.Activator = this.m_NextActivator;
			return ActivationServices.GetActivator().Activate(ctorMsg);
		}

		// Token: 0x040029EF RID: 10735
		private IActivator m_NextActivator;

		// Token: 0x040029F0 RID: 10736
		private string m_RemActivatorURL;
	}
}
