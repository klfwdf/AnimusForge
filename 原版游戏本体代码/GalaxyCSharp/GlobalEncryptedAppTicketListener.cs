using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200006F RID: 111
	public abstract class GlobalEncryptedAppTicketListener : IEncryptedAppTicketListener
	{
		// Token: 0x060006BF RID: 1727 RVA: 0x0000DB6C File Offset: 0x0000BD6C
		internal GlobalEncryptedAppTicketListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalEncryptedAppTicketListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060006C0 RID: 1728 RVA: 0x0000DB88 File Offset: 0x0000BD88
		public GlobalEncryptedAppTicketListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerEncryptedAppTicket.GetListenerType(), this);
			}
		}

		// Token: 0x060006C1 RID: 1729 RVA: 0x0000DBAA File Offset: 0x0000BDAA
		internal static HandleRef getCPtr(GlobalEncryptedAppTicketListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060006C2 RID: 1730 RVA: 0x0000DBC8 File Offset: 0x0000BDC8
		~GlobalEncryptedAppTicketListener()
		{
			this.Dispose();
		}

		// Token: 0x060006C3 RID: 1731 RVA: 0x0000DBF8 File Offset: 0x0000BDF8
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerEncryptedAppTicket.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalEncryptedAppTicketListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000084 RID: 132
		private HandleRef swigCPtr;
	}
}
