using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200015A RID: 346
	public class IUnauthorizedAccessError : IError
	{
		// Token: 0x06000CAD RID: 3245 RVA: 0x0001A051 File Offset: 0x00018251
		internal IUnauthorizedAccessError(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IUnauthorizedAccessError_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000CAE RID: 3246 RVA: 0x0001A06D File Offset: 0x0001826D
		internal static HandleRef getCPtr(IUnauthorizedAccessError obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000CAF RID: 3247 RVA: 0x0001A08C File Offset: 0x0001828C
		~IUnauthorizedAccessError()
		{
			this.Dispose();
		}

		// Token: 0x06000CB0 RID: 3248 RVA: 0x0001A0BC File Offset: 0x000182BC
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IUnauthorizedAccessError(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0400027C RID: 636
		private HandleRef swigCPtr;
	}
}
