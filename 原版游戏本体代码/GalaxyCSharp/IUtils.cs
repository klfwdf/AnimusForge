using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200016E RID: 366
	public class IUtils : IDisposable
	{
		// Token: 0x06000D49 RID: 3401 RVA: 0x0001AAC7 File Offset: 0x00018CC7
		internal IUtils(IntPtr cPtr, bool cMemoryOwn)
		{
			this.swigCMemOwn = cMemoryOwn;
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000D4A RID: 3402 RVA: 0x0001AAE3 File Offset: 0x00018CE3
		internal static HandleRef getCPtr(IUtils obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000D4B RID: 3403 RVA: 0x0001AB04 File Offset: 0x00018D04
		~IUtils()
		{
			this.Dispose();
		}

		// Token: 0x06000D4C RID: 3404 RVA: 0x0001AB34 File Offset: 0x00018D34
		public virtual void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IUtils(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x06000D4D RID: 3405 RVA: 0x0001ABB4 File Offset: 0x00018DB4
		public virtual void GetImageSize(uint imageID, ref int width, ref int height)
		{
			GalaxyInstancePINVOKE.IUtils_GetImageSize(this.swigCPtr, imageID, ref width, ref height);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000D4E RID: 3406 RVA: 0x0001ABD4 File Offset: 0x00018DD4
		public virtual void GetImageRGBA(uint imageID, byte[] buffer, uint bufferLength)
		{
			GalaxyInstancePINVOKE.IUtils_GetImageRGBA(this.swigCPtr, imageID, buffer, bufferLength);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000D4F RID: 3407 RVA: 0x0001ABF4 File Offset: 0x00018DF4
		public virtual void RegisterForNotification(string type)
		{
			GalaxyInstancePINVOKE.IUtils_RegisterForNotification(this.swigCPtr, type);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000D50 RID: 3408 RVA: 0x0001AC14 File Offset: 0x00018E14
		public virtual uint GetNotification(ulong notificationID, ref bool consumable, ref byte[] type, uint typeLength, byte[] content, uint contentSize)
		{
			uint result = GalaxyInstancePINVOKE.IUtils_GetNotification(this.swigCPtr, notificationID, ref consumable, type, typeLength, content, contentSize);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000D51 RID: 3409 RVA: 0x0001AC48 File Offset: 0x00018E48
		public virtual void ShowOverlayWithWebPage(string url)
		{
			GalaxyInstancePINVOKE.IUtils_ShowOverlayWithWebPage(this.swigCPtr, url);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000D52 RID: 3410 RVA: 0x0001AC68 File Offset: 0x00018E68
		public virtual bool IsOverlayVisible()
		{
			bool result = GalaxyInstancePINVOKE.IUtils_IsOverlayVisible(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000D53 RID: 3411 RVA: 0x0001AC94 File Offset: 0x00018E94
		public virtual OverlayState GetOverlayState()
		{
			OverlayState result = (OverlayState)GalaxyInstancePINVOKE.IUtils_GetOverlayState(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000D54 RID: 3412 RVA: 0x0001ACBE File Offset: 0x00018EBE
		public virtual void DisableOverlayPopups(string popupGroup)
		{
			GalaxyInstancePINVOKE.IUtils_DisableOverlayPopups(this.swigCPtr, popupGroup);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000D55 RID: 3413 RVA: 0x0001ACDC File Offset: 0x00018EDC
		public virtual GogServicesConnectionState GetGogServicesConnectionState()
		{
			GogServicesConnectionState result = (GogServicesConnectionState)GalaxyInstancePINVOKE.IUtils_GetGogServicesConnectionState(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x040002A8 RID: 680
		private HandleRef swigCPtr;

		// Token: 0x040002A9 RID: 681
		protected bool swigCMemOwn;
	}
}
