using System;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting
{
	// Token: 0x020007BB RID: 1979
	[Serializable]
	internal sealed class EnvoyInfo : IEnvoyInfo
	{
		// Token: 0x0600559F RID: 21919 RVA: 0x00130164 File Offset: 0x0012E364
		[SecurityCritical]
		internal static IEnvoyInfo CreateEnvoyInfo(ServerIdentity serverID)
		{
			IEnvoyInfo result = null;
			if (serverID != null)
			{
				if (serverID.EnvoyChain == null)
				{
					serverID.RaceSetEnvoyChain(serverID.ServerContext.CreateEnvoyChain(serverID.TPOrObject));
				}
				if (!(serverID.EnvoyChain is EnvoyTerminatorSink))
				{
					result = new EnvoyInfo(serverID.EnvoyChain);
				}
			}
			return result;
		}

		// Token: 0x060055A0 RID: 21920 RVA: 0x001301B2 File Offset: 0x0012E3B2
		[SecurityCritical]
		private EnvoyInfo(IMessageSink sinks)
		{
			this.EnvoySinks = sinks;
		}

		// Token: 0x17000E1D RID: 3613
		// (get) Token: 0x060055A1 RID: 21921 RVA: 0x001301C1 File Offset: 0x0012E3C1
		// (set) Token: 0x060055A2 RID: 21922 RVA: 0x001301C9 File Offset: 0x0012E3C9
		public IMessageSink EnvoySinks
		{
			[SecurityCritical]
			get
			{
				return this.envoySinks;
			}
			[SecurityCritical]
			set
			{
				this.envoySinks = value;
			}
		}

		// Token: 0x0400276A RID: 10090
		private IMessageSink envoySinks;
	}
}
