using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200015E RID: 350
	public abstract class IUserFindListener : GalaxyTypeAwareListenerUserFind
	{
		// Token: 0x06000CF9 RID: 3321 RVA: 0x000156C4 File Offset: 0x000138C4
		internal IUserFindListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IUserFindListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IUserFindListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000CFA RID: 3322 RVA: 0x000156EC File Offset: 0x000138EC
		public IUserFindListener()
			: this(GalaxyInstancePINVOKE.new_IUserFindListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000CFB RID: 3323 RVA: 0x00015710 File Offset: 0x00013910
		internal static HandleRef getCPtr(IUserFindListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000CFC RID: 3324 RVA: 0x00015730 File Offset: 0x00013930
		~IUserFindListener()
		{
			this.Dispose();
		}

		// Token: 0x06000CFD RID: 3325 RVA: 0x00015760 File Offset: 0x00013960
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IUserFindListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IUserFindListener.listeners.ContainsKey(handle))
					{
						IUserFindListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000CFE RID: 3326
		public abstract void OnUserFindSuccess(string userSpecifier, GalaxyID userID);

		// Token: 0x06000CFF RID: 3327
		public abstract void OnUserFindFailure(string userSpecifier, IUserFindListener.FailureReason failureReason);

		// Token: 0x06000D00 RID: 3328 RVA: 0x00015810 File Offset: 0x00013A10
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnUserFindSuccess", IUserFindListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IUserFindListener.SwigDelegateIUserFindListener_0(IUserFindListener.SwigDirectorOnUserFindSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnUserFindFailure", IUserFindListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new IUserFindListener.SwigDelegateIUserFindListener_1(IUserFindListener.SwigDirectorOnUserFindFailure);
			}
			GalaxyInstancePINVOKE.IUserFindListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x06000D01 RID: 3329 RVA: 0x00015884 File Offset: 0x00013A84
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IUserFindListener));
		}

		// Token: 0x06000D02 RID: 3330 RVA: 0x000158BA File Offset: 0x00013ABA
		[MonoPInvokeCallback(typeof(IUserFindListener.SwigDelegateIUserFindListener_0))]
		private static void SwigDirectorOnUserFindSuccess(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string userSpecifier, IntPtr userID)
		{
			if (IUserFindListener.listeners.ContainsKey(cPtr))
			{
				IUserFindListener.listeners[cPtr].OnUserFindSuccess(userSpecifier, new GalaxyID(new GalaxyID(userID, false).ToUint64()));
			}
		}

		// Token: 0x06000D03 RID: 3331 RVA: 0x000158EE File Offset: 0x00013AEE
		[MonoPInvokeCallback(typeof(IUserFindListener.SwigDelegateIUserFindListener_1))]
		private static void SwigDirectorOnUserFindFailure(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string userSpecifier, int failureReason)
		{
			if (IUserFindListener.listeners.ContainsKey(cPtr))
			{
				IUserFindListener.listeners[cPtr].OnUserFindFailure(userSpecifier, (IUserFindListener.FailureReason)failureReason);
			}
		}

		// Token: 0x04000283 RID: 643
		private static Dictionary<IntPtr, IUserFindListener> listeners = new Dictionary<IntPtr, IUserFindListener>();

		// Token: 0x04000284 RID: 644
		private HandleRef swigCPtr;

		// Token: 0x04000285 RID: 645
		private IUserFindListener.SwigDelegateIUserFindListener_0 swigDelegate0;

		// Token: 0x04000286 RID: 646
		private IUserFindListener.SwigDelegateIUserFindListener_1 swigDelegate1;

		// Token: 0x04000287 RID: 647
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(string),
			typeof(GalaxyID)
		};

		// Token: 0x04000288 RID: 648
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(string),
			typeof(IUserFindListener.FailureReason)
		};

		// Token: 0x0200015F RID: 351
		// (Invoke) Token: 0x06000D06 RID: 3334
		public delegate void SwigDelegateIUserFindListener_0(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string userSpecifier, IntPtr userID);

		// Token: 0x02000160 RID: 352
		// (Invoke) Token: 0x06000D0A RID: 3338
		public delegate void SwigDelegateIUserFindListener_1(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string userSpecifier, int failureReason);

		// Token: 0x02000161 RID: 353
		public enum FailureReason
		{
			// Token: 0x0400028A RID: 650
			FAILURE_REASON_UNDEFINED,
			// Token: 0x0400028B RID: 651
			FAILURE_REASON_USER_NOT_FOUND,
			// Token: 0x0400028C RID: 652
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
