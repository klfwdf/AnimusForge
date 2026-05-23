using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000149 RID: 329
	public abstract class ISharedFileDownloadListener : GalaxyTypeAwareListenerSharedFileDownload
	{
		// Token: 0x06000BF8 RID: 3064 RVA: 0x000149E0 File Offset: 0x00012BE0
		internal ISharedFileDownloadListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.ISharedFileDownloadListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			ISharedFileDownloadListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000BF9 RID: 3065 RVA: 0x00014A08 File Offset: 0x00012C08
		public ISharedFileDownloadListener()
			: this(GalaxyInstancePINVOKE.new_ISharedFileDownloadListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000BFA RID: 3066 RVA: 0x00014A2C File Offset: 0x00012C2C
		internal static HandleRef getCPtr(ISharedFileDownloadListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000BFB RID: 3067 RVA: 0x00014A4C File Offset: 0x00012C4C
		~ISharedFileDownloadListener()
		{
			this.Dispose();
		}

		// Token: 0x06000BFC RID: 3068 RVA: 0x00014A7C File Offset: 0x00012C7C
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ISharedFileDownloadListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (ISharedFileDownloadListener.listeners.ContainsKey(handle))
					{
						ISharedFileDownloadListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000BFD RID: 3069
		public abstract void OnSharedFileDownloadSuccess(ulong sharedFileID, string fileName);

		// Token: 0x06000BFE RID: 3070
		public abstract void OnSharedFileDownloadFailure(ulong sharedFileID, ISharedFileDownloadListener.FailureReason failureReason);

		// Token: 0x06000BFF RID: 3071 RVA: 0x00014B2C File Offset: 0x00012D2C
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnSharedFileDownloadSuccess", ISharedFileDownloadListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new ISharedFileDownloadListener.SwigDelegateISharedFileDownloadListener_0(ISharedFileDownloadListener.SwigDirectorOnSharedFileDownloadSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnSharedFileDownloadFailure", ISharedFileDownloadListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new ISharedFileDownloadListener.SwigDelegateISharedFileDownloadListener_1(ISharedFileDownloadListener.SwigDirectorOnSharedFileDownloadFailure);
			}
			GalaxyInstancePINVOKE.ISharedFileDownloadListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x06000C00 RID: 3072 RVA: 0x00014BA0 File Offset: 0x00012DA0
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(ISharedFileDownloadListener));
		}

		// Token: 0x06000C01 RID: 3073 RVA: 0x00014BD6 File Offset: 0x00012DD6
		[MonoPInvokeCallback(typeof(ISharedFileDownloadListener.SwigDelegateISharedFileDownloadListener_0))]
		private static void SwigDirectorOnSharedFileDownloadSuccess(IntPtr cPtr, ulong sharedFileID, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string fileName)
		{
			if (ISharedFileDownloadListener.listeners.ContainsKey(cPtr))
			{
				ISharedFileDownloadListener.listeners[cPtr].OnSharedFileDownloadSuccess(sharedFileID, fileName);
			}
		}

		// Token: 0x06000C02 RID: 3074 RVA: 0x00014BFA File Offset: 0x00012DFA
		[MonoPInvokeCallback(typeof(ISharedFileDownloadListener.SwigDelegateISharedFileDownloadListener_1))]
		private static void SwigDirectorOnSharedFileDownloadFailure(IntPtr cPtr, ulong sharedFileID, int failureReason)
		{
			if (ISharedFileDownloadListener.listeners.ContainsKey(cPtr))
			{
				ISharedFileDownloadListener.listeners[cPtr].OnSharedFileDownloadFailure(sharedFileID, (ISharedFileDownloadListener.FailureReason)failureReason);
			}
		}

		// Token: 0x04000250 RID: 592
		private static Dictionary<IntPtr, ISharedFileDownloadListener> listeners = new Dictionary<IntPtr, ISharedFileDownloadListener>();

		// Token: 0x04000251 RID: 593
		private HandleRef swigCPtr;

		// Token: 0x04000252 RID: 594
		private ISharedFileDownloadListener.SwigDelegateISharedFileDownloadListener_0 swigDelegate0;

		// Token: 0x04000253 RID: 595
		private ISharedFileDownloadListener.SwigDelegateISharedFileDownloadListener_1 swigDelegate1;

		// Token: 0x04000254 RID: 596
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(ulong),
			typeof(string)
		};

		// Token: 0x04000255 RID: 597
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(ulong),
			typeof(ISharedFileDownloadListener.FailureReason)
		};

		// Token: 0x0200014A RID: 330
		// (Invoke) Token: 0x06000C05 RID: 3077
		public delegate void SwigDelegateISharedFileDownloadListener_0(IntPtr cPtr, ulong sharedFileID, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string fileName);

		// Token: 0x0200014B RID: 331
		// (Invoke) Token: 0x06000C09 RID: 3081
		public delegate void SwigDelegateISharedFileDownloadListener_1(IntPtr cPtr, ulong sharedFileID, int failureReason);

		// Token: 0x0200014C RID: 332
		public enum FailureReason
		{
			// Token: 0x04000257 RID: 599
			FAILURE_REASON_UNDEFINED,
			// Token: 0x04000258 RID: 600
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
