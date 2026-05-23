using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000140 RID: 320
	public class IRuntimeError : IError
	{
		// Token: 0x06000BCC RID: 3020 RVA: 0x00018D0A File Offset: 0x00016F0A
		internal IRuntimeError(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IRuntimeError_SWIGUpcast(cPtr), cMemoryOwn)
		{
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000BCD RID: 3021 RVA: 0x00018D26 File Offset: 0x00016F26
		internal static HandleRef getCPtr(IRuntimeError obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000BCE RID: 3022 RVA: 0x00018D44 File Offset: 0x00016F44
		~IRuntimeError()
		{
			this.Dispose();
		}

		// Token: 0x06000BCF RID: 3023 RVA: 0x00018D74 File Offset: 0x00016F74
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IRuntimeError(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x04000238 RID: 568
		private HandleRef swigCPtr;
	}
}
