using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000F9 RID: 249
	public abstract class ILeaderboardScoreUpdateListener : GalaxyTypeAwareListenerLeaderboardScoreUpdate
	{
		// Token: 0x060009E0 RID: 2528 RVA: 0x00010A84 File Offset: 0x0000EC84
		internal ILeaderboardScoreUpdateListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.ILeaderboardScoreUpdateListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			ILeaderboardScoreUpdateListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060009E1 RID: 2529 RVA: 0x00010AAC File Offset: 0x0000ECAC
		public ILeaderboardScoreUpdateListener()
			: this(GalaxyInstancePINVOKE.new_ILeaderboardScoreUpdateListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x060009E2 RID: 2530 RVA: 0x00010AD0 File Offset: 0x0000ECD0
		internal static HandleRef getCPtr(ILeaderboardScoreUpdateListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060009E3 RID: 2531 RVA: 0x00010AF0 File Offset: 0x0000ECF0
		~ILeaderboardScoreUpdateListener()
		{
			this.Dispose();
		}

		// Token: 0x060009E4 RID: 2532 RVA: 0x00010B20 File Offset: 0x0000ED20
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ILeaderboardScoreUpdateListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (ILeaderboardScoreUpdateListener.listeners.ContainsKey(handle))
					{
						ILeaderboardScoreUpdateListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060009E5 RID: 2533
		public abstract void OnLeaderboardScoreUpdateSuccess(string name, int score, uint oldRank, uint newRank);

		// Token: 0x060009E6 RID: 2534
		public abstract void OnLeaderboardScoreUpdateFailure(string name, int score, ILeaderboardScoreUpdateListener.FailureReason failureReason);

		// Token: 0x060009E7 RID: 2535 RVA: 0x00010BD0 File Offset: 0x0000EDD0
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnLeaderboardScoreUpdateSuccess", ILeaderboardScoreUpdateListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new ILeaderboardScoreUpdateListener.SwigDelegateILeaderboardScoreUpdateListener_0(ILeaderboardScoreUpdateListener.SwigDirectorOnLeaderboardScoreUpdateSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnLeaderboardScoreUpdateFailure", ILeaderboardScoreUpdateListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new ILeaderboardScoreUpdateListener.SwigDelegateILeaderboardScoreUpdateListener_1(ILeaderboardScoreUpdateListener.SwigDirectorOnLeaderboardScoreUpdateFailure);
			}
			GalaxyInstancePINVOKE.ILeaderboardScoreUpdateListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x060009E8 RID: 2536 RVA: 0x00010C44 File Offset: 0x0000EE44
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(ILeaderboardScoreUpdateListener));
		}

		// Token: 0x060009E9 RID: 2537 RVA: 0x00010C7A File Offset: 0x0000EE7A
		[MonoPInvokeCallback(typeof(ILeaderboardScoreUpdateListener.SwigDelegateILeaderboardScoreUpdateListener_0))]
		private static void SwigDirectorOnLeaderboardScoreUpdateSuccess(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string name, int score, uint oldRank, uint newRank)
		{
			if (ILeaderboardScoreUpdateListener.listeners.ContainsKey(cPtr))
			{
				ILeaderboardScoreUpdateListener.listeners[cPtr].OnLeaderboardScoreUpdateSuccess(name, score, oldRank, newRank);
			}
		}

		// Token: 0x060009EA RID: 2538 RVA: 0x00010CA1 File Offset: 0x0000EEA1
		[MonoPInvokeCallback(typeof(ILeaderboardScoreUpdateListener.SwigDelegateILeaderboardScoreUpdateListener_1))]
		private static void SwigDirectorOnLeaderboardScoreUpdateFailure(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string name, int score, int failureReason)
		{
			if (ILeaderboardScoreUpdateListener.listeners.ContainsKey(cPtr))
			{
				ILeaderboardScoreUpdateListener.listeners[cPtr].OnLeaderboardScoreUpdateFailure(name, score, (ILeaderboardScoreUpdateListener.FailureReason)failureReason);
			}
		}

		// Token: 0x04000195 RID: 405
		private static Dictionary<IntPtr, ILeaderboardScoreUpdateListener> listeners = new Dictionary<IntPtr, ILeaderboardScoreUpdateListener>();

		// Token: 0x04000196 RID: 406
		private HandleRef swigCPtr;

		// Token: 0x04000197 RID: 407
		private ILeaderboardScoreUpdateListener.SwigDelegateILeaderboardScoreUpdateListener_0 swigDelegate0;

		// Token: 0x04000198 RID: 408
		private ILeaderboardScoreUpdateListener.SwigDelegateILeaderboardScoreUpdateListener_1 swigDelegate1;

		// Token: 0x04000199 RID: 409
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(string),
			typeof(int),
			typeof(uint),
			typeof(uint)
		};

		// Token: 0x0400019A RID: 410
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(string),
			typeof(int),
			typeof(ILeaderboardScoreUpdateListener.FailureReason)
		};

		// Token: 0x020000FA RID: 250
		// (Invoke) Token: 0x060009ED RID: 2541
		public delegate void SwigDelegateILeaderboardScoreUpdateListener_0(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string name, int score, uint oldRank, uint newRank);

		// Token: 0x020000FB RID: 251
		// (Invoke) Token: 0x060009F1 RID: 2545
		public delegate void SwigDelegateILeaderboardScoreUpdateListener_1(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string name, int score, int failureReason);

		// Token: 0x020000FC RID: 252
		public enum FailureReason
		{
			// Token: 0x0400019C RID: 412
			FAILURE_REASON_UNDEFINED,
			// Token: 0x0400019D RID: 413
			FAILURE_REASON_NO_IMPROVEMENT,
			// Token: 0x0400019E RID: 414
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
