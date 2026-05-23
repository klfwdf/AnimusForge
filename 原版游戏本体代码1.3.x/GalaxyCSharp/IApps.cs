using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Galaxy.Api
{
	// Token: 0x020000A5 RID: 165
	public class IApps : IDisposable
	{
		// Token: 0x060007D9 RID: 2009 RVA: 0x000163D0 File Offset: 0x000145D0
		internal IApps(IntPtr cPtr, bool cMemoryOwn)
		{
			this.swigCMemOwn = cMemoryOwn;
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060007DA RID: 2010 RVA: 0x000163EC File Offset: 0x000145EC
		internal static HandleRef getCPtr(IApps obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060007DB RID: 2011 RVA: 0x0001640C File Offset: 0x0001460C
		~IApps()
		{
			this.Dispose();
		}

		// Token: 0x060007DC RID: 2012 RVA: 0x0001643C File Offset: 0x0001463C
		public virtual void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IApps(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x060007DD RID: 2013 RVA: 0x000164BC File Offset: 0x000146BC
		public virtual bool IsDlcInstalled(ulong productID)
		{
			bool result = GalaxyInstancePINVOKE.IApps_IsDlcInstalled(this.swigCPtr, productID);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x060007DE RID: 2014 RVA: 0x000164E8 File Offset: 0x000146E8
		public virtual string GetCurrentGameLanguage(ulong productID)
		{
			string result = GalaxyInstancePINVOKE.IApps_GetCurrentGameLanguage__SWIG_0(this.swigCPtr, productID);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x060007DF RID: 2015 RVA: 0x00016514 File Offset: 0x00014714
		public virtual string GetCurrentGameLanguage()
		{
			string result = GalaxyInstancePINVOKE.IApps_GetCurrentGameLanguage__SWIG_1(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x060007E0 RID: 2016 RVA: 0x00016540 File Offset: 0x00014740
		public virtual void GetCurrentGameLanguageCopy(out string buffer, uint bufferLength, ulong productID)
		{
			byte[] array = new byte[bufferLength];
			try
			{
				GalaxyInstancePINVOKE.IApps_GetCurrentGameLanguageCopy__SWIG_0(this.swigCPtr, array, bufferLength, productID);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
			}
			finally
			{
				buffer = Encoding.UTF8.GetString(array);
			}
		}

		// Token: 0x060007E1 RID: 2017 RVA: 0x00016598 File Offset: 0x00014798
		public virtual void GetCurrentGameLanguageCopy(out string buffer, uint bufferLength)
		{
			byte[] array = new byte[bufferLength];
			try
			{
				GalaxyInstancePINVOKE.IApps_GetCurrentGameLanguageCopy__SWIG_1(this.swigCPtr, array, bufferLength);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
			}
			finally
			{
				buffer = Encoding.UTF8.GetString(array);
			}
		}

		// Token: 0x040000CB RID: 203
		private HandleRef swigCPtr;

		// Token: 0x040000CC RID: 204
		protected bool swigCMemOwn;
	}
}
