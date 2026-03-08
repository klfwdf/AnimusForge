using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000063 RID: 99
	public abstract class GameServerGlobalTelemetryEventSendListener : ITelemetryEventSendListener
	{
		// Token: 0x06000682 RID: 1666 RVA: 0x0000B72E File Offset: 0x0000992E
		internal GameServerGlobalTelemetryEventSendListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GameServerGlobalTelemetryEventSendListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000683 RID: 1667 RVA: 0x0000B74A File Offset: 0x0000994A
		public GameServerGlobalTelemetryEventSendListener()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Register(GalaxyTypeAwareListenerTelemetryEventSend.GetListenerType(), this);
			}
		}

		// Token: 0x06000684 RID: 1668 RVA: 0x0000B76C File Offset: 0x0000996C
		internal static HandleRef getCPtr(GameServerGlobalTelemetryEventSendListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000685 RID: 1669 RVA: 0x0000B78C File Offset: 0x0000998C
		~GameServerGlobalTelemetryEventSendListener()
		{
			this.Dispose();
		}

		// Token: 0x06000686 RID: 1670 RVA: 0x0000B7BC File Offset: 0x000099BC
		public override void Dispose()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Unregister(GalaxyTypeAwareListenerTelemetryEventSend.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GameServerGlobalTelemetryEventSendListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000077 RID: 119
		private HandleRef swigCPtr;
	}
}
