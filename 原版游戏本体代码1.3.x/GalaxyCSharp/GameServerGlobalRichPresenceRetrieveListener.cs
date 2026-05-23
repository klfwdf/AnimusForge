using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000061 RID: 97
	public abstract class GameServerGlobalRichPresenceRetrieveListener : IRichPresenceRetrieveListener
	{
		// Token: 0x06000678 RID: 1656 RVA: 0x0000B010 File Offset: 0x00009210
		internal GameServerGlobalRichPresenceRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GameServerGlobalRichPresenceRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000679 RID: 1657 RVA: 0x0000B02C File Offset: 0x0000922C
		public GameServerGlobalRichPresenceRetrieveListener()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Register(GalaxyTypeAwareListenerRichPresenceRetrieve.GetListenerType(), this);
			}
		}

		// Token: 0x0600067A RID: 1658 RVA: 0x0000B04E File Offset: 0x0000924E
		internal static HandleRef getCPtr(GameServerGlobalRichPresenceRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600067B RID: 1659 RVA: 0x0000B06C File Offset: 0x0000926C
		~GameServerGlobalRichPresenceRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x0600067C RID: 1660 RVA: 0x0000B09C File Offset: 0x0000929C
		public override void Dispose()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Unregister(GalaxyTypeAwareListenerRichPresenceRetrieve.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GameServerGlobalRichPresenceRetrieveListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000075 RID: 117
		private HandleRef swigCPtr;
	}
}
