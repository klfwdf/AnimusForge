using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200011F RID: 287
	public class ILogger : IDisposable
	{
		// Token: 0x06000ABA RID: 2746 RVA: 0x00017AC8 File Offset: 0x00015CC8
		internal ILogger(IntPtr cPtr, bool cMemoryOwn)
		{
			this.swigCMemOwn = cMemoryOwn;
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000ABB RID: 2747 RVA: 0x00017AE4 File Offset: 0x00015CE4
		internal static HandleRef getCPtr(ILogger obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000ABC RID: 2748 RVA: 0x00017B04 File Offset: 0x00015D04
		~ILogger()
		{
			this.Dispose();
		}

		// Token: 0x06000ABD RID: 2749 RVA: 0x00017B34 File Offset: 0x00015D34
		public virtual void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ILogger(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x06000ABE RID: 2750 RVA: 0x00017BB4 File Offset: 0x00015DB4
		public virtual void Trace(string format)
		{
			GalaxyInstancePINVOKE.ILogger_Trace(this.swigCPtr, format);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000ABF RID: 2751 RVA: 0x00017BD2 File Offset: 0x00015DD2
		public virtual void Debug(string format)
		{
			GalaxyInstancePINVOKE.ILogger_Debug(this.swigCPtr, format);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AC0 RID: 2752 RVA: 0x00017BF0 File Offset: 0x00015DF0
		public virtual void Info(string format)
		{
			GalaxyInstancePINVOKE.ILogger_Info(this.swigCPtr, format);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AC1 RID: 2753 RVA: 0x00017C0E File Offset: 0x00015E0E
		public virtual void Warning(string format)
		{
			GalaxyInstancePINVOKE.ILogger_Warning(this.swigCPtr, format);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AC2 RID: 2754 RVA: 0x00017C2C File Offset: 0x00015E2C
		public virtual void Error(string format)
		{
			GalaxyInstancePINVOKE.ILogger_Error(this.swigCPtr, format);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AC3 RID: 2755 RVA: 0x00017C4A File Offset: 0x00015E4A
		public virtual void Fatal(string format)
		{
			GalaxyInstancePINVOKE.ILogger_Fatal(this.swigCPtr, format);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x040001ED RID: 493
		private HandleRef swigCPtr;

		// Token: 0x040001EE RID: 494
		protected bool swigCMemOwn;
	}
}
