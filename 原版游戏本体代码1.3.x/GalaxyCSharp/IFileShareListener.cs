using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000CA RID: 202
	public abstract class IFileShareListener : GalaxyTypeAwareListenerFileShare
	{
		// Token: 0x060008AF RID: 2223 RVA: 0x0000DC98 File Offset: 0x0000BE98
		internal IFileShareListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IFileShareListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IFileShareListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060008B0 RID: 2224 RVA: 0x0000DCC0 File Offset: 0x0000BEC0
		public IFileShareListener()
			: this(GalaxyInstancePINVOKE.new_IFileShareListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x060008B1 RID: 2225 RVA: 0x0000DCE4 File Offset: 0x0000BEE4
		internal static HandleRef getCPtr(IFileShareListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060008B2 RID: 2226 RVA: 0x0000DD04 File Offset: 0x0000BF04
		~IFileShareListener()
		{
			this.Dispose();
		}

		// Token: 0x060008B3 RID: 2227 RVA: 0x0000DD34 File Offset: 0x0000BF34
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IFileShareListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IFileShareListener.listeners.ContainsKey(handle))
					{
						IFileShareListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060008B4 RID: 2228
		public abstract void OnFileShareSuccess(string fileName, ulong sharedFileID);

		// Token: 0x060008B5 RID: 2229
		public abstract void OnFileShareFailure(string fileName, IFileShareListener.FailureReason failureReason);

		// Token: 0x060008B6 RID: 2230 RVA: 0x0000DDE4 File Offset: 0x0000BFE4
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnFileShareSuccess", IFileShareListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IFileShareListener.SwigDelegateIFileShareListener_0(IFileShareListener.SwigDirectorOnFileShareSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnFileShareFailure", IFileShareListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new IFileShareListener.SwigDelegateIFileShareListener_1(IFileShareListener.SwigDirectorOnFileShareFailure);
			}
			GalaxyInstancePINVOKE.IFileShareListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x060008B7 RID: 2231 RVA: 0x0000DE58 File Offset: 0x0000C058
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IFileShareListener));
		}

		// Token: 0x060008B8 RID: 2232 RVA: 0x0000DE8E File Offset: 0x0000C08E
		[MonoPInvokeCallback(typeof(IFileShareListener.SwigDelegateIFileShareListener_0))]
		private static void SwigDirectorOnFileShareSuccess(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string fileName, ulong sharedFileID)
		{
			if (IFileShareListener.listeners.ContainsKey(cPtr))
			{
				IFileShareListener.listeners[cPtr].OnFileShareSuccess(fileName, sharedFileID);
			}
		}

		// Token: 0x060008B9 RID: 2233 RVA: 0x0000DEB2 File Offset: 0x0000C0B2
		[MonoPInvokeCallback(typeof(IFileShareListener.SwigDelegateIFileShareListener_1))]
		private static void SwigDirectorOnFileShareFailure(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string fileName, int failureReason)
		{
			if (IFileShareListener.listeners.ContainsKey(cPtr))
			{
				IFileShareListener.listeners[cPtr].OnFileShareFailure(fileName, (IFileShareListener.FailureReason)failureReason);
			}
		}

		// Token: 0x04000128 RID: 296
		private static Dictionary<IntPtr, IFileShareListener> listeners = new Dictionary<IntPtr, IFileShareListener>();

		// Token: 0x04000129 RID: 297
		private HandleRef swigCPtr;

		// Token: 0x0400012A RID: 298
		private IFileShareListener.SwigDelegateIFileShareListener_0 swigDelegate0;

		// Token: 0x0400012B RID: 299
		private IFileShareListener.SwigDelegateIFileShareListener_1 swigDelegate1;

		// Token: 0x0400012C RID: 300
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(string),
			typeof(ulong)
		};

		// Token: 0x0400012D RID: 301
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(string),
			typeof(IFileShareListener.FailureReason)
		};

		// Token: 0x020000CB RID: 203
		// (Invoke) Token: 0x060008BC RID: 2236
		public delegate void SwigDelegateIFileShareListener_0(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string fileName, ulong sharedFileID);

		// Token: 0x020000CC RID: 204
		// (Invoke) Token: 0x060008C0 RID: 2240
		public delegate void SwigDelegateIFileShareListener_1(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string fileName, int failureReason);

		// Token: 0x020000CD RID: 205
		public enum FailureReason
		{
			// Token: 0x0400012F RID: 303
			FAILURE_REASON_UNDEFINED,
			// Token: 0x04000130 RID: 304
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
