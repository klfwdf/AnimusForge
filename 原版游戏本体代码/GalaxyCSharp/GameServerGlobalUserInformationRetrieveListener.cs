using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000064 RID: 100
	public abstract class GameServerGlobalUserInformationRetrieveListener : IUserInformationRetrieveListener
	{
		// Token: 0x06000687 RID: 1671 RVA: 0x0000BB10 File Offset: 0x00009D10
		internal GameServerGlobalUserInformationRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GameServerGlobalUserInformationRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			GameServerGlobalUserInformationRetrieveListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000688 RID: 1672 RVA: 0x0000BB38 File Offset: 0x00009D38
		public GameServerGlobalUserInformationRetrieveListener()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Register(GalaxyTypeAwareListenerUserInformationRetrieve.GetListenerType(), this);
			}
		}

		// Token: 0x06000689 RID: 1673 RVA: 0x0000BB5A File Offset: 0x00009D5A
		internal static HandleRef getCPtr(GameServerGlobalUserInformationRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x0600068A RID: 1674 RVA: 0x0000BB78 File Offset: 0x00009D78
		~GameServerGlobalUserInformationRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x0600068B RID: 1675 RVA: 0x0000BBA8 File Offset: 0x00009DA8
		public override void Dispose()
		{
			if (GalaxyInstance.GameServerListenerRegistrar() != null)
			{
				GalaxyInstance.GameServerListenerRegistrar().Unregister(GalaxyTypeAwareListenerUserInformationRetrieve.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GameServerGlobalUserInformationRetrieveListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (GameServerGlobalUserInformationRetrieveListener.listeners.ContainsKey(handle))
					{
						GameServerGlobalUserInformationRetrieveListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000078 RID: 120
		private static Dictionary<IntPtr, GameServerGlobalUserInformationRetrieveListener> listeners = new Dictionary<IntPtr, GameServerGlobalUserInformationRetrieveListener>();

		// Token: 0x04000079 RID: 121
		private HandleRef swigCPtr;
	}
}
