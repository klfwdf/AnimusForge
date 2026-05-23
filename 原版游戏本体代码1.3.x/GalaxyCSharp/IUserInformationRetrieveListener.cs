using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000162 RID: 354
	public abstract class IUserInformationRetrieveListener : GalaxyTypeAwareListenerUserInformationRetrieve
	{
		// Token: 0x06000D0D RID: 3341 RVA: 0x0000B85C File Offset: 0x00009A5C
		internal IUserInformationRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IUserInformationRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IUserInformationRetrieveListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000D0E RID: 3342 RVA: 0x0000B884 File Offset: 0x00009A84
		public IUserInformationRetrieveListener()
			: this(GalaxyInstancePINVOKE.new_IUserInformationRetrieveListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000D0F RID: 3343 RVA: 0x0000B8A8 File Offset: 0x00009AA8
		internal static HandleRef getCPtr(IUserInformationRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000D10 RID: 3344 RVA: 0x0000B8C8 File Offset: 0x00009AC8
		~IUserInformationRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x06000D11 RID: 3345 RVA: 0x0000B8F8 File Offset: 0x00009AF8
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IUserInformationRetrieveListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IUserInformationRetrieveListener.listeners.ContainsKey(handle))
					{
						IUserInformationRetrieveListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000D12 RID: 3346
		public abstract void OnUserInformationRetrieveSuccess(GalaxyID userID);

		// Token: 0x06000D13 RID: 3347
		public abstract void OnUserInformationRetrieveFailure(GalaxyID userID, IUserInformationRetrieveListener.FailureReason failureReason);

		// Token: 0x06000D14 RID: 3348 RVA: 0x0000B9A8 File Offset: 0x00009BA8
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnUserInformationRetrieveSuccess", IUserInformationRetrieveListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IUserInformationRetrieveListener.SwigDelegateIUserInformationRetrieveListener_0(IUserInformationRetrieveListener.SwigDirectorOnUserInformationRetrieveSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnUserInformationRetrieveFailure", IUserInformationRetrieveListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new IUserInformationRetrieveListener.SwigDelegateIUserInformationRetrieveListener_1(IUserInformationRetrieveListener.SwigDirectorOnUserInformationRetrieveFailure);
			}
			GalaxyInstancePINVOKE.IUserInformationRetrieveListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x06000D15 RID: 3349 RVA: 0x0000BA1C File Offset: 0x00009C1C
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IUserInformationRetrieveListener));
		}

		// Token: 0x06000D16 RID: 3350 RVA: 0x0000BA52 File Offset: 0x00009C52
		[MonoPInvokeCallback(typeof(IUserInformationRetrieveListener.SwigDelegateIUserInformationRetrieveListener_0))]
		private static void SwigDirectorOnUserInformationRetrieveSuccess(IntPtr cPtr, IntPtr userID)
		{
			if (IUserInformationRetrieveListener.listeners.ContainsKey(cPtr))
			{
				IUserInformationRetrieveListener.listeners[cPtr].OnUserInformationRetrieveSuccess(new GalaxyID(new GalaxyID(userID, false).ToUint64()));
			}
		}

		// Token: 0x06000D17 RID: 3351 RVA: 0x0000BA85 File Offset: 0x00009C85
		[MonoPInvokeCallback(typeof(IUserInformationRetrieveListener.SwigDelegateIUserInformationRetrieveListener_1))]
		private static void SwigDirectorOnUserInformationRetrieveFailure(IntPtr cPtr, IntPtr userID, int failureReason)
		{
			if (IUserInformationRetrieveListener.listeners.ContainsKey(cPtr))
			{
				IUserInformationRetrieveListener.listeners[cPtr].OnUserInformationRetrieveFailure(new GalaxyID(new GalaxyID(userID, false).ToUint64()), (IUserInformationRetrieveListener.FailureReason)failureReason);
			}
		}

		// Token: 0x0400028D RID: 653
		private static Dictionary<IntPtr, IUserInformationRetrieveListener> listeners = new Dictionary<IntPtr, IUserInformationRetrieveListener>();

		// Token: 0x0400028E RID: 654
		private HandleRef swigCPtr;

		// Token: 0x0400028F RID: 655
		private IUserInformationRetrieveListener.SwigDelegateIUserInformationRetrieveListener_0 swigDelegate0;

		// Token: 0x04000290 RID: 656
		private IUserInformationRetrieveListener.SwigDelegateIUserInformationRetrieveListener_1 swigDelegate1;

		// Token: 0x04000291 RID: 657
		private static Type[] swigMethodTypes0 = new Type[] { typeof(GalaxyID) };

		// Token: 0x04000292 RID: 658
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(GalaxyID),
			typeof(IUserInformationRetrieveListener.FailureReason)
		};

		// Token: 0x02000163 RID: 355
		// (Invoke) Token: 0x06000D1A RID: 3354
		public delegate void SwigDelegateIUserInformationRetrieveListener_0(IntPtr cPtr, IntPtr userID);

		// Token: 0x02000164 RID: 356
		// (Invoke) Token: 0x06000D1E RID: 3358
		public delegate void SwigDelegateIUserInformationRetrieveListener_1(IntPtr cPtr, IntPtr userID, int failureReason);

		// Token: 0x02000165 RID: 357
		public enum FailureReason
		{
			// Token: 0x04000294 RID: 660
			FAILURE_REASON_UNDEFINED,
			// Token: 0x04000295 RID: 661
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
