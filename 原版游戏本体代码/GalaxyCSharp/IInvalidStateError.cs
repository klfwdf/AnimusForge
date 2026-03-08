using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000F0 RID: 240
	public class IInvalidStateError : IError
	{
		// Token: 0x060009B4 RID: 2484 RVA: 0x000178A0 File Offset: 0x00015AA0
		internal IInvalidStateError(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IInvalidStateError_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060009B5 RID: 2485 RVA: 0x000178BC File Offset: 0x00015ABC
		internal static HandleRef getCPtr(IInvalidStateError obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060009B6 RID: 2486 RVA: 0x000178DC File Offset: 0x00015ADC
		~IInvalidStateError()
		{
			this.Dispose();
		}

		// Token: 0x060009B7 RID: 2487 RVA: 0x0001790C File Offset: 0x00015B0C
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IInvalidStateError(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000181 RID: 385
		private HandleRef swigCPtr;
	}
}
