using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000091 RID: 145
	public abstract class GlobalPersonaDataChangedListener : IPersonaDataChangedListener
	{
		// Token: 0x0600076F RID: 1903 RVA: 0x00013897 File Offset: 0x00011A97
		internal GlobalPersonaDataChangedListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalPersonaDataChangedListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000770 RID: 1904 RVA: 0x000138B3 File Offset: 0x00011AB3
		public GlobalPersonaDataChangedListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerPersonaDataChanged.GetListenerType(), this);
			}
		}

		// Token: 0x06000771 RID: 1905 RVA: 0x000138D5 File Offset: 0x00011AD5
		internal static HandleRef getCPtr(GlobalPersonaDataChangedListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000772 RID: 1906 RVA: 0x000138F4 File Offset: 0x00011AF4
		~GlobalPersonaDataChangedListener()
		{
			this.Dispose();
		}

		// Token: 0x06000773 RID: 1907 RVA: 0x00013924 File Offset: 0x00011B24
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerPersonaDataChanged.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalPersonaDataChangedListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000AC RID: 172
		private HandleRef swigCPtr;
	}
}
