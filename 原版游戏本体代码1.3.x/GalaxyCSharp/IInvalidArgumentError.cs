using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000EF RID: 239
	public class IInvalidArgumentError : IError
	{
		// Token: 0x060009B0 RID: 2480 RVA: 0x000177AC File Offset: 0x000159AC
		internal IInvalidArgumentError(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IInvalidArgumentError_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060009B1 RID: 2481 RVA: 0x000177C8 File Offset: 0x000159C8
		internal static HandleRef getCPtr(IInvalidArgumentError obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060009B2 RID: 2482 RVA: 0x000177E8 File Offset: 0x000159E8
		~IInvalidArgumentError()
		{
			this.Dispose();
		}

		// Token: 0x060009B3 RID: 2483 RVA: 0x00017818 File Offset: 0x00015A18
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IInvalidArgumentError(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000180 RID: 384
		private HandleRef swigCPtr;
	}
}
