using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System.Runtime.Remoting.Activation
{
	// Token: 0x02000895 RID: 2197
	[Serializable]
	internal class ContextLevelActivator : IActivator
	{
		// Token: 0x06005D17 RID: 23831 RVA: 0x001469B5 File Offset: 0x00144BB5
		internal ContextLevelActivator()
		{
			this.m_NextActivator = null;
		}

		// Token: 0x06005D18 RID: 23832 RVA: 0x001469C4 File Offset: 0x00144BC4
		internal ContextLevelActivator(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			this.m_NextActivator = (IActivator)info.GetValue("m_NextActivator", typeof(IActivator));
		}

		// Token: 0x17001002 RID: 4098
		// (get) Token: 0x06005D19 RID: 23833 RVA: 0x001469FA File Offset: 0x00144BFA
		// (set) Token: 0x06005D1A RID: 23834 RVA: 0x00146A02 File Offset: 0x00144C02
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

		// Token: 0x17001003 RID: 4099
		// (get) Token: 0x06005D1B RID: 23835 RVA: 0x00146A0B File Offset: 0x00144C0B
		public virtual ActivatorLevel Level
		{
			[SecurityCritical]
			get
			{
				return ActivatorLevel.Context;
			}
		}

		// Token: 0x06005D1C RID: 23836 RVA: 0x00146A0E File Offset: 0x00144C0E
		[SecurityCritical]
		[ComVisible(true)]
		public virtual IConstructionReturnMessage Activate(IConstructionCallMessage ctorMsg)
		{
			ctorMsg.Activator = ctorMsg.Activator.NextActivator;
			return ActivationServices.DoCrossContextActivation(ctorMsg);
		}

		// Token: 0x040029F1 RID: 10737
		private IActivator m_NextActivator;
	}
}
