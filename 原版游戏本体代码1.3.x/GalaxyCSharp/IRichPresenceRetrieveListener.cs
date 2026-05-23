using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200013C RID: 316
	public abstract class IRichPresenceRetrieveListener : GalaxyTypeAwareListenerRichPresenceRetrieve
	{
		// Token: 0x06000BB8 RID: 3000 RVA: 0x0000AD14 File Offset: 0x00008F14
		internal IRichPresenceRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IRichPresenceRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IRichPresenceRetrieveListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000BB9 RID: 3001 RVA: 0x0000AD3C File Offset: 0x00008F3C
		public IRichPresenceRetrieveListener()
			: this(GalaxyInstancePINVOKE.new_IRichPresenceRetrieveListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000BBA RID: 3002 RVA: 0x0000AD60 File Offset: 0x00008F60
		internal static HandleRef getCPtr(IRichPresenceRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000BBB RID: 3003 RVA: 0x0000AD80 File Offset: 0x00008F80
		~IRichPresenceRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x06000BBC RID: 3004 RVA: 0x0000ADB0 File Offset: 0x00008FB0
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IRichPresenceRetrieveListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IRichPresenceRetrieveListener.listeners.ContainsKey(handle))
					{
						IRichPresenceRetrieveListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000BBD RID: 3005 RVA: 0x0000AE60 File Offset: 0x00009060
		public virtual void OnRichPresenceRetrieveSuccess(GalaxyID userID)
		{
			GalaxyInstancePINVOKE.IRichPresenceRetrieveListener_OnRichPresenceRetrieveSuccess(this.swigCPtr, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000BBE RID: 3006 RVA: 0x0000AE83 File Offset: 0x00009083
		public virtual void OnRichPresenceRetrieveFailure(GalaxyID userID, IRichPresenceRetrieveListener.FailureReason failureReason)
		{
			GalaxyInstancePINVOKE.IRichPresenceRetrieveListener_OnRichPresenceRetrieveFailure(this.swigCPtr, GalaxyID.getCPtr(userID), (int)failureReason);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000BBF RID: 3007 RVA: 0x0000AEA8 File Offset: 0x000090A8
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnRichPresenceRetrieveSuccess", IRichPresenceRetrieveListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IRichPresenceRetrieveListener.SwigDelegateIRichPresenceRetrieveListener_0(IRichPresenceRetrieveListener.SwigDirectorOnRichPresenceRetrieveSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnRichPresenceRetrieveFailure", IRichPresenceRetrieveListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new IRichPresenceRetrieveListener.SwigDelegateIRichPresenceRetrieveListener_1(IRichPresenceRetrieveListener.SwigDirectorOnRichPresenceRetrieveFailure);
			}
			GalaxyInstancePINVOKE.IRichPresenceRetrieveListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x06000BC0 RID: 3008 RVA: 0x0000AF1C File Offset: 0x0000911C
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IRichPresenceRetrieveListener));
		}

		// Token: 0x06000BC1 RID: 3009 RVA: 0x0000AF52 File Offset: 0x00009152
		[MonoPInvokeCallback(typeof(IRichPresenceRetrieveListener.SwigDelegateIRichPresenceRetrieveListener_0))]
		private static void SwigDirectorOnRichPresenceRetrieveSuccess(IntPtr cPtr, IntPtr userID)
		{
			if (IRichPresenceRetrieveListener.listeners.ContainsKey(cPtr))
			{
				IRichPresenceRetrieveListener.listeners[cPtr].OnRichPresenceRetrieveSuccess(new GalaxyID(new GalaxyID(userID, false).ToUint64()));
			}
		}

		// Token: 0x06000BC2 RID: 3010 RVA: 0x0000AF85 File Offset: 0x00009185
		[MonoPInvokeCallback(typeof(IRichPresenceRetrieveListener.SwigDelegateIRichPresenceRetrieveListener_1))]
		private static void SwigDirectorOnRichPresenceRetrieveFailure(IntPtr cPtr, IntPtr userID, int failureReason)
		{
			if (IRichPresenceRetrieveListener.listeners.ContainsKey(cPtr))
			{
				IRichPresenceRetrieveListener.listeners[cPtr].OnRichPresenceRetrieveFailure(new GalaxyID(new GalaxyID(userID, false).ToUint64()), (IRichPresenceRetrieveListener.FailureReason)failureReason);
			}
		}

		// Token: 0x0400022F RID: 559
		private static Dictionary<IntPtr, IRichPresenceRetrieveListener> listeners = new Dictionary<IntPtr, IRichPresenceRetrieveListener>();

		// Token: 0x04000230 RID: 560
		private HandleRef swigCPtr;

		// Token: 0x04000231 RID: 561
		private IRichPresenceRetrieveListener.SwigDelegateIRichPresenceRetrieveListener_0 swigDelegate0;

		// Token: 0x04000232 RID: 562
		private IRichPresenceRetrieveListener.SwigDelegateIRichPresenceRetrieveListener_1 swigDelegate1;

		// Token: 0x04000233 RID: 563
		private static Type[] swigMethodTypes0 = new Type[] { typeof(GalaxyID) };

		// Token: 0x04000234 RID: 564
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(GalaxyID),
			typeof(IRichPresenceRetrieveListener.FailureReason)
		};

		// Token: 0x0200013D RID: 317
		// (Invoke) Token: 0x06000BC5 RID: 3013
		public delegate void SwigDelegateIRichPresenceRetrieveListener_0(IntPtr cPtr, IntPtr userID);

		// Token: 0x0200013E RID: 318
		// (Invoke) Token: 0x06000BC9 RID: 3017
		public delegate void SwigDelegateIRichPresenceRetrieveListener_1(IntPtr cPtr, IntPtr userID, int failureReason);

		// Token: 0x0200013F RID: 319
		public enum FailureReason
		{
			// Token: 0x04000236 RID: 566
			FAILURE_REASON_UNDEFINED,
			// Token: 0x04000237 RID: 567
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
