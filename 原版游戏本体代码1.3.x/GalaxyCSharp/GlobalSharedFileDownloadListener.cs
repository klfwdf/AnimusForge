using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000097 RID: 151
	public abstract class GlobalSharedFileDownloadListener : ISharedFileDownloadListener
	{
		// Token: 0x0600078F RID: 1935 RVA: 0x00014C81 File Offset: 0x00012E81
		internal GlobalSharedFileDownloadListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.GlobalSharedFileDownloadListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000790 RID: 1936 RVA: 0x00014C9D File Offset: 0x00012E9D
		public GlobalSharedFileDownloadListener()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Register(GalaxyTypeAwareListenerSharedFileDownload.GetListenerType(), this);
			}
		}

		// Token: 0x06000791 RID: 1937 RVA: 0x00014CBF File Offset: 0x00012EBF
		internal static HandleRef getCPtr(GlobalSharedFileDownloadListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000792 RID: 1938 RVA: 0x00014CE0 File Offset: 0x00012EE0
		~GlobalSharedFileDownloadListener()
		{
			this.Dispose();
		}

		// Token: 0x06000793 RID: 1939 RVA: 0x00014D10 File Offset: 0x00012F10
		public override void Dispose()
		{
			if (GalaxyInstance.ListenerRegistrar() != null)
			{
				GalaxyInstance.ListenerRegistrar().Unregister(GalaxyTypeAwareListenerSharedFileDownload.GetListenerType(), this);
			}
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GlobalSharedFileDownloadListener(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x040000B4 RID: 180
		private HandleRef swigCPtr;
	}
}
