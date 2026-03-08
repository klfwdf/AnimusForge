using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000101 RID: 257
	public class IListenerRegistrar : IDisposable
	{
		// Token: 0x06000A08 RID: 2568 RVA: 0x00017994 File Offset: 0x00015B94
		internal IListenerRegistrar(IntPtr cPtr, bool cMemoryOwn)
		{
			this.swigCMemOwn = cMemoryOwn;
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000A09 RID: 2569 RVA: 0x000179B0 File Offset: 0x00015BB0
		internal static HandleRef getCPtr(IListenerRegistrar obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000A0A RID: 2570 RVA: 0x000179D0 File Offset: 0x00015BD0
		~IListenerRegistrar()
		{
			this.Dispose();
		}

		// Token: 0x06000A0B RID: 2571 RVA: 0x00017A00 File Offset: 0x00015C00
		public virtual void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IListenerRegistrar(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x06000A0C RID: 2572 RVA: 0x00017A80 File Offset: 0x00015C80
		public virtual void Register(ListenerType listenerType, IGalaxyListener listener)
		{
			GalaxyInstancePINVOKE.IListenerRegistrar_Register(this.swigCPtr, (int)listenerType, IGalaxyListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000A0D RID: 2573 RVA: 0x00017AA4 File Offset: 0x00015CA4
		public virtual void Unregister(ListenerType listenerType, IGalaxyListener listener)
		{
			GalaxyInstancePINVOKE.IListenerRegistrar_Unregister(this.swigCPtr, (int)listenerType, IGalaxyListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x040001A8 RID: 424
		private HandleRef swigCPtr;

		// Token: 0x040001A9 RID: 425
		protected bool swigCMemOwn;
	}
}
