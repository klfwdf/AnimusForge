using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Activation
{
	// Token: 0x02000896 RID: 2198
	[Serializable]
	internal class ConstructionLevelActivator : IActivator
	{
		// Token: 0x06005D1D RID: 23837 RVA: 0x00146A27 File Offset: 0x00144C27
		internal ConstructionLevelActivator()
		{
		}

		// Token: 0x17001004 RID: 4100
		// (get) Token: 0x06005D1E RID: 23838 RVA: 0x00146A2F File Offset: 0x00144C2F
		// (set) Token: 0x06005D1F RID: 23839 RVA: 0x00146A32 File Offset: 0x00144C32
		public virtual IActivator NextActivator
		{
			[SecurityCritical]
			get
			{
				return null;
			}
			[SecurityCritical]
			set
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x17001005 RID: 4101
		// (get) Token: 0x06005D20 RID: 23840 RVA: 0x00146A39 File Offset: 0x00144C39
		public virtual ActivatorLevel Level
		{
			[SecurityCritical]
			get
			{
				return ActivatorLevel.Construction;
			}
		}

		// Token: 0x06005D21 RID: 23841 RVA: 0x00146A3C File Offset: 0x00144C3C
		[SecurityCritical]
		[ComVisible(true)]
		public virtual IConstructionReturnMessage Activate(IConstructionCallMessage ctorMsg)
		{
			ctorMsg.Activator = ctorMsg.Activator.NextActivator;
			return ActivationServices.DoServerContextActivation(ctorMsg);
		}
	}
}
