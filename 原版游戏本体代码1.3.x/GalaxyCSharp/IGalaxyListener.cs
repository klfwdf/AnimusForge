using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000E8 RID: 232
	public class IGalaxyListener : IDisposable
	{
		// Token: 0x06000980 RID: 2432 RVA: 0x00002FF6 File Offset: 0x000011F6
		internal IGalaxyListener(IntPtr cPtr, bool cMemoryOwn)
		{
			this.swigCMemOwn = cMemoryOwn;
			IGalaxyListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000981 RID: 2433 RVA: 0x0000301E File Offset: 0x0000121E
		public IGalaxyListener()
			: this(GalaxyInstancePINVOKE.new_IGalaxyListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000982 RID: 2434 RVA: 0x0000303C File Offset: 0x0000123C
		internal static HandleRef getCPtr(IGalaxyListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000983 RID: 2435 RVA: 0x0000305C File Offset: 0x0000125C
		~IGalaxyListener()
		{
			this.Dispose();
		}

		// Token: 0x06000984 RID: 2436 RVA: 0x0000308C File Offset: 0x0000128C
		public virtual void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IGalaxyListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IGalaxyListener.listeners.ContainsKey(handle))
					{
						IGalaxyListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x04000171 RID: 369
		private static Dictionary<IntPtr, IGalaxyListener> listeners = new Dictionary<IntPtr, IGalaxyListener>();

		// Token: 0x04000172 RID: 370
		private HandleRef swigCPtr;

		// Token: 0x04000173 RID: 371
		protected bool swigCMemOwn;
	}
}
