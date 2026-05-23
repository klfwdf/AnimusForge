using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000C8 RID: 200
	public class IError : IDisposable
	{
		// Token: 0x060008A7 RID: 2215 RVA: 0x00016B36 File Offset: 0x00014D36
		internal IError(IntPtr cPtr, bool cMemoryOwn)
		{
			this.swigCMemOwn = cMemoryOwn;
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060008A8 RID: 2216 RVA: 0x00016B52 File Offset: 0x00014D52
		internal static HandleRef getCPtr(IError obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060008A9 RID: 2217 RVA: 0x00016B70 File Offset: 0x00014D70
		~IError()
		{
			this.Dispose();
		}

		// Token: 0x060008AA RID: 2218 RVA: 0x00016BA0 File Offset: 0x00014DA0
		public virtual void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IError(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x060008AB RID: 2219 RVA: 0x00016C20 File Offset: 0x00014E20
		public override string ToString()
		{
			return string.Format("{0}: {1} ({2})", this.GetName(), this.GetMsg(), this.GetErrorType().ToString());
		}

		// Token: 0x060008AC RID: 2220 RVA: 0x00016C58 File Offset: 0x00014E58
		public virtual string GetName()
		{
			string result = GalaxyInstancePINVOKE.IError_GetName(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x060008AD RID: 2221 RVA: 0x00016C84 File Offset: 0x00014E84
		public virtual string GetMsg()
		{
			string result = GalaxyInstancePINVOKE.IError_GetMsg(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x060008AE RID: 2222 RVA: 0x00016CB0 File Offset: 0x00014EB0
		public virtual IError.Type GetErrorType()
		{
			IError.Type result = (IError.Type)GalaxyInstancePINVOKE.IError_GetErrorType(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x04000121 RID: 289
		private HandleRef swigCPtr;

		// Token: 0x04000122 RID: 290
		protected bool swigCMemOwn;

		// Token: 0x020000C9 RID: 201
		public enum Type
		{
			// Token: 0x04000124 RID: 292
			UNAUTHORIZED_ACCESS,
			// Token: 0x04000125 RID: 293
			INVALID_ARGUMENT,
			// Token: 0x04000126 RID: 294
			INVALID_STATE,
			// Token: 0x04000127 RID: 295
			RUNTIME_ERROR
		}
	}
}
