using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000F1 RID: 241
	public abstract class ILeaderboardEntriesRetrieveListener : GalaxyTypeAwareListenerLeaderboardEntriesRetrieve
	{
		// Token: 0x060009B8 RID: 2488 RVA: 0x000102F4 File Offset: 0x0000E4F4
		internal ILeaderboardEntriesRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.ILeaderboardEntriesRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			ILeaderboardEntriesRetrieveListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060009B9 RID: 2489 RVA: 0x0001031C File Offset: 0x0000E51C
		public ILeaderboardEntriesRetrieveListener()
			: this(GalaxyInstancePINVOKE.new_ILeaderboardEntriesRetrieveListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x060009BA RID: 2490 RVA: 0x00010340 File Offset: 0x0000E540
		internal static HandleRef getCPtr(ILeaderboardEntriesRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060009BB RID: 2491 RVA: 0x00010360 File Offset: 0x0000E560
		~ILeaderboardEntriesRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x060009BC RID: 2492 RVA: 0x00010390 File Offset: 0x0000E590
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ILeaderboardEntriesRetrieveListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (ILeaderboardEntriesRetrieveListener.listeners.ContainsKey(handle))
					{
						ILeaderboardEntriesRetrieveListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060009BD RID: 2493
		public abstract void OnLeaderboardEntriesRetrieveSuccess(string name, uint entryCount);

		// Token: 0x060009BE RID: 2494
		public abstract void OnLeaderboardEntriesRetrieveFailure(string name, ILeaderboardEntriesRetrieveListener.FailureReason failureReason);

		// Token: 0x060009BF RID: 2495 RVA: 0x00010440 File Offset: 0x0000E640
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnLeaderboardEntriesRetrieveSuccess", ILeaderboardEntriesRetrieveListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new ILeaderboardEntriesRetrieveListener.SwigDelegateILeaderboardEntriesRetrieveListener_0(ILeaderboardEntriesRetrieveListener.SwigDirectorOnLeaderboardEntriesRetrieveSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnLeaderboardEntriesRetrieveFailure", ILeaderboardEntriesRetrieveListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new ILeaderboardEntriesRetrieveListener.SwigDelegateILeaderboardEntriesRetrieveListener_1(ILeaderboardEntriesRetrieveListener.SwigDirectorOnLeaderboardEntriesRetrieveFailure);
			}
			GalaxyInstancePINVOKE.ILeaderboardEntriesRetrieveListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x060009C0 RID: 2496 RVA: 0x000104B4 File Offset: 0x0000E6B4
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(ILeaderboardEntriesRetrieveListener));
		}

		// Token: 0x060009C1 RID: 2497 RVA: 0x000104EA File Offset: 0x0000E6EA
		[MonoPInvokeCallback(typeof(ILeaderboardEntriesRetrieveListener.SwigDelegateILeaderboardEntriesRetrieveListener_0))]
		private static void SwigDirectorOnLeaderboardEntriesRetrieveSuccess(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string name, uint entryCount)
		{
			if (ILeaderboardEntriesRetrieveListener.listeners.ContainsKey(cPtr))
			{
				ILeaderboardEntriesRetrieveListener.listeners[cPtr].OnLeaderboardEntriesRetrieveSuccess(name, entryCount);
			}
		}

		// Token: 0x060009C2 RID: 2498 RVA: 0x0001050E File Offset: 0x0000E70E
		[MonoPInvokeCallback(typeof(ILeaderboardEntriesRetrieveListener.SwigDelegateILeaderboardEntriesRetrieveListener_1))]
		private static void SwigDirectorOnLeaderboardEntriesRetrieveFailure(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string name, int failureReason)
		{
			if (ILeaderboardEntriesRetrieveListener.listeners.ContainsKey(cPtr))
			{
				ILeaderboardEntriesRetrieveListener.listeners[cPtr].OnLeaderboardEntriesRetrieveFailure(name, (ILeaderboardEntriesRetrieveListener.FailureReason)failureReason);
			}
		}

		// Token: 0x04000182 RID: 386
		private static Dictionary<IntPtr, ILeaderboardEntriesRetrieveListener> listeners = new Dictionary<IntPtr, ILeaderboardEntriesRetrieveListener>();

		// Token: 0x04000183 RID: 387
		private HandleRef swigCPtr;

		// Token: 0x04000184 RID: 388
		private ILeaderboardEntriesRetrieveListener.SwigDelegateILeaderboardEntriesRetrieveListener_0 swigDelegate0;

		// Token: 0x04000185 RID: 389
		private ILeaderboardEntriesRetrieveListener.SwigDelegateILeaderboardEntriesRetrieveListener_1 swigDelegate1;

		// Token: 0x04000186 RID: 390
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(string),
			typeof(uint)
		};

		// Token: 0x04000187 RID: 391
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(string),
			typeof(ILeaderboardEntriesRetrieveListener.FailureReason)
		};

		// Token: 0x020000F2 RID: 242
		// (Invoke) Token: 0x060009C5 RID: 2501
		public delegate void SwigDelegateILeaderboardEntriesRetrieveListener_0(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string name, uint entryCount);

		// Token: 0x020000F3 RID: 243
		// (Invoke) Token: 0x060009C9 RID: 2505
		public delegate void SwigDelegateILeaderboardEntriesRetrieveListener_1(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string name, int failureReason);

		// Token: 0x020000F4 RID: 244
		public enum FailureReason
		{
			// Token: 0x04000189 RID: 393
			FAILURE_REASON_UNDEFINED,
			// Token: 0x0400018A RID: 394
			FAILURE_REASON_NOT_FOUND,
			// Token: 0x0400018B RID: 395
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
