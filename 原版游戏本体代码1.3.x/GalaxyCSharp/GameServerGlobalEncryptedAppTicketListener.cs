using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000054 RID: 84
	public abstract class GameServerGlobalEncryptedAppTicketListener : IEncryptedAppTicketListener
	{
		// Token: 0x06000637 RID: 1591 RVA: 0x000081DA File Offset: 0x000063DA
		internal GameServerGlobalEncryptedAppTicketListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GameServerGlobalEncryptedAppTicketListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000638 RID: 1592 RVA: 0x000081F6 File Offset: 0x000063F6
		public GameServerGlobalEncryptedAppTicketListener()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Register(GalaxyTypeAwareListenerEncryptedAppTicket.GetListenerType(), this);
			}
		}

		// Token: 0x06000639 RID: 1593 RVA: 0x00008218 File Offset: 0x00006418
		internal static HandleRef getCPtr(GameServerGlobalEncryptedAppTicketListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600063A RID: 1594 RVA: 0x00008238 File Offset: 0x00006438
		~GameServerGlobalEncryptedAppTicketListener()
		{
			this.Dispose();
		}

		// Token: 0x0600063B RID: 1595 RVA: 0x00008268 File Offset: 0x00006468
		public override void Dispose()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Unregister(GalaxyTypeAwareListenerEncryptedAppTicket.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GameServerGlobalEncryptedAppTicketListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000068 RID: 104
		private HandleRef swigCPtr;
	}
}
