using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200006D RID: 109
	public abstract class GlobalConnectionDataListener : IConnectionDataListener
	{
		// Token: 0x060006B5 RID: 1717 RVA: 0x0000D66F File Offset: 0x0000B86F
		internal GlobalConnectionDataListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalConnectionDataListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060006B6 RID: 1718 RVA: 0x0000D68B File Offset: 0x0000B88B
		public GlobalConnectionDataListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerConnectionData.GetListenerType(), this);
			}
		}

		// Token: 0x060006B7 RID: 1719 RVA: 0x0000D6AD File Offset: 0x0000B8AD
		internal static HandleRef getCPtr(GlobalConnectionDataListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060006B8 RID: 1720 RVA: 0x0000D6CC File Offset: 0x0000B8CC
		~GlobalConnectionDataListener()
		{
			this.Dispose();
		}

		// Token: 0x060006B9 RID: 1721 RVA: 0x0000D6FC File Offset: 0x0000B8FC
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerConnectionData.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalConnectionDataListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000082 RID: 130
		private HandleRef swigCPtr;
	}
}
