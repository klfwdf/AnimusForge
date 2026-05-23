using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200016A RID: 362
	public abstract class IUserTimePlayedRetrieveListener : GalaxyTypeAwareListenerUserTimePlayedRetrieve
	{
		// Token: 0x06000D35 RID: 3381 RVA: 0x00015FF0 File Offset: 0x000141F0
		internal IUserTimePlayedRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IUserTimePlayedRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IUserTimePlayedRetrieveListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000D36 RID: 3382 RVA: 0x00016018 File Offset: 0x00014218
		public IUserTimePlayedRetrieveListener()
			: this(GalaxyInstancePINVOKE.new_IUserTimePlayedRetrieveListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000D37 RID: 3383 RVA: 0x0001603C File Offset: 0x0001423C
		internal static HandleRef getCPtr(IUserTimePlayedRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000D38 RID: 3384 RVA: 0x0001605C File Offset: 0x0001425C
		~IUserTimePlayedRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x06000D39 RID: 3385 RVA: 0x0001608C File Offset: 0x0001428C
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IUserTimePlayedRetrieveListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IUserTimePlayedRetrieveListener.listeners.ContainsKey(handle))
					{
						IUserTimePlayedRetrieveListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000D3A RID: 3386
		public abstract void OnUserTimePlayedRetrieveSuccess(GalaxyID userID);

		// Token: 0x06000D3B RID: 3387
		public abstract void OnUserTimePlayedRetrieveFailure(GalaxyID userID, IUserTimePlayedRetrieveListener.FailureReason failureReason);

		// Token: 0x06000D3C RID: 3388 RVA: 0x0001613C File Offset: 0x0001433C
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnUserTimePlayedRetrieveSuccess", IUserTimePlayedRetrieveListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IUserTimePlayedRetrieveListener.SwigDelegateIUserTimePlayedRetrieveListener_0(IUserTimePlayedRetrieveListener.SwigDirectorOnUserTimePlayedRetrieveSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnUserTimePlayedRetrieveFailure", IUserTimePlayedRetrieveListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new IUserTimePlayedRetrieveListener.SwigDelegateIUserTimePlayedRetrieveListener_1(IUserTimePlayedRetrieveListener.SwigDirectorOnUserTimePlayedRetrieveFailure);
			}
			GalaxyInstancePINVOKE.IUserTimePlayedRetrieveListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x06000D3D RID: 3389 RVA: 0x000161B0 File Offset: 0x000143B0
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IUserTimePlayedRetrieveListener));
		}

		// Token: 0x06000D3E RID: 3390 RVA: 0x000161E6 File Offset: 0x000143E6
		[MonoPInvokeCallback(typeof(IUserTimePlayedRetrieveListener.SwigDelegateIUserTimePlayedRetrieveListener_0))]
		private static void SwigDirectorOnUserTimePlayedRetrieveSuccess(IntPtr cPtr, IntPtr userID)
		{
			if (IUserTimePlayedRetrieveListener.listeners.ContainsKey(cPtr))
			{
				IUserTimePlayedRetrieveListener.listeners[cPtr].OnUserTimePlayedRetrieveSuccess(new GalaxyID(new GalaxyID(userID, false).ToUint64()));
			}
		}

		// Token: 0x06000D3F RID: 3391 RVA: 0x00016219 File Offset: 0x00014419
		[MonoPInvokeCallback(typeof(IUserTimePlayedRetrieveListener.SwigDelegateIUserTimePlayedRetrieveListener_1))]
		private static void SwigDirectorOnUserTimePlayedRetrieveFailure(IntPtr cPtr, IntPtr userID, int failureReason)
		{
			if (IUserTimePlayedRetrieveListener.listeners.ContainsKey(cPtr))
			{
				IUserTimePlayedRetrieveListener.listeners[cPtr].OnUserTimePlayedRetrieveFailure(new GalaxyID(new GalaxyID(userID, false).ToUint64()), (IUserTimePlayedRetrieveListener.FailureReason)failureReason);
			}
		}

		// Token: 0x0400029F RID: 671
		private static Dictionary<IntPtr, IUserTimePlayedRetrieveListener> listeners = new Dictionary<IntPtr, IUserTimePlayedRetrieveListener>();

		// Token: 0x040002A0 RID: 672
		private HandleRef swigCPtr;

		// Token: 0x040002A1 RID: 673
		private IUserTimePlayedRetrieveListener.SwigDelegateIUserTimePlayedRetrieveListener_0 swigDelegate0;

		// Token: 0x040002A2 RID: 674
		private IUserTimePlayedRetrieveListener.SwigDelegateIUserTimePlayedRetrieveListener_1 swigDelegate1;

		// Token: 0x040002A3 RID: 675
		private static Type[] swigMethodTypes0 = new Type[] { typeof(GalaxyID) };

		// Token: 0x040002A4 RID: 676
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(GalaxyID),
			typeof(IUserTimePlayedRetrieveListener.FailureReason)
		};

		// Token: 0x0200016B RID: 363
		// (Invoke) Token: 0x06000D42 RID: 3394
		public delegate void SwigDelegateIUserTimePlayedRetrieveListener_0(IntPtr cPtr, IntPtr userID);

		// Token: 0x0200016C RID: 364
		// (Invoke) Token: 0x06000D46 RID: 3398
		public delegate void SwigDelegateIUserTimePlayedRetrieveListener_1(IntPtr cPtr, IntPtr userID, int failureReason);

		// Token: 0x0200016D RID: 365
		public enum FailureReason
		{
			// Token: 0x040002A6 RID: 678
			FAILURE_REASON_UNDEFINED,
			// Token: 0x040002A7 RID: 679
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
