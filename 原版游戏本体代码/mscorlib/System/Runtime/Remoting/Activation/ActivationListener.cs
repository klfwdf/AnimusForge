using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Remoting.Activation
{
	// Token: 0x02000893 RID: 2195
	internal class ActivationListener : MarshalByRefObject, IActivator
	{
		// Token: 0x06005D0B RID: 23819 RVA: 0x00146884 File Offset: 0x00144A84
		[SecurityCritical]
		public override object InitializeLifetimeService()
		{
			return null;
		}

		// Token: 0x17000FFE RID: 4094
		// (get) Token: 0x06005D0C RID: 23820 RVA: 0x00146887 File Offset: 0x00144A87
		// (set) Token: 0x06005D0D RID: 23821 RVA: 0x0014688A File Offset: 0x00144A8A
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

		// Token: 0x17000FFF RID: 4095
		// (get) Token: 0x06005D0E RID: 23822 RVA: 0x00146891 File Offset: 0x00144A91
		public virtual ActivatorLevel Level
		{
			[SecurityCritical]
			get
			{
				return ActivatorLevel.AppDomain;
			}
		}

		// Token: 0x06005D0F RID: 23823 RVA: 0x00146898 File Offset: 0x00144A98
		[SecurityCritical]
		[ComVisible(true)]
		public virtual IConstructionReturnMessage Activate(IConstructionCallMessage ctorMsg)
		{
			if (ctorMsg == null || RemotingServices.IsTransparentProxy(ctorMsg))
			{
				throw new ArgumentNullException("ctorMsg");
			}
			ctorMsg.Properties["Permission"] = "allowed";
			string activationTypeName = ctorMsg.ActivationTypeName;
			if (!RemotingConfigHandler.IsActivationAllowed(activationTypeName))
			{
				throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Activation_PermissionDenied"), ctorMsg.ActivationTypeName));
			}
			Type activationType = ctorMsg.ActivationType;
			if (activationType == null)
			{
				throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_BadType"), ctorMsg.ActivationTypeName));
			}
			return ActivationServices.GetActivator().Activate(ctorMsg);
		}
	}
}
