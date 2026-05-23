using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000136 RID: 310
	public abstract class IRichPresenceChangeListener : GalaxyTypeAwareListenerRichPresenceChange
	{
		// Token: 0x06000B96 RID: 2966 RVA: 0x000139C4 File Offset: 0x00011BC4
		internal IRichPresenceChangeListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IRichPresenceChangeListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IRichPresenceChangeListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000B97 RID: 2967 RVA: 0x000139EC File Offset: 0x00011BEC
		public IRichPresenceChangeListener()
			: this(GalaxyInstancePINVOKE.new_IRichPresenceChangeListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000B98 RID: 2968 RVA: 0x00013A10 File Offset: 0x00011C10
		internal static HandleRef getCPtr(IRichPresenceChangeListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000B99 RID: 2969 RVA: 0x00013A30 File Offset: 0x00011C30
		~IRichPresenceChangeListener()
		{
			this.Dispose();
		}

		// Token: 0x06000B9A RID: 2970 RVA: 0x00013A60 File Offset: 0x00011C60
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IRichPresenceChangeListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IRichPresenceChangeListener.listeners.ContainsKey(handle))
					{
						IRichPresenceChangeListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000B9B RID: 2971
		public abstract void OnRichPresenceChangeSuccess();

		// Token: 0x06000B9C RID: 2972
		public abstract void OnRichPresenceChangeFailure(IRichPresenceChangeListener.FailureReason failureReason);

		// Token: 0x06000B9D RID: 2973 RVA: 0x00013B10 File Offset: 0x00011D10
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnRichPresenceChangeSuccess", IRichPresenceChangeListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IRichPresenceChangeListener.SwigDelegateIRichPresenceChangeListener_0(IRichPresenceChangeListener.SwigDirectorOnRichPresenceChangeSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnRichPresenceChangeFailure", IRichPresenceChangeListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new IRichPresenceChangeListener.SwigDelegateIRichPresenceChangeListener_1(IRichPresenceChangeListener.SwigDirectorOnRichPresenceChangeFailure);
			}
			GalaxyInstancePINVOKE.IRichPresenceChangeListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x06000B9E RID: 2974 RVA: 0x00013B84 File Offset: 0x00011D84
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IRichPresenceChangeListener));
		}

		// Token: 0x06000B9F RID: 2975 RVA: 0x00013BBA File Offset: 0x00011DBA
		[MonoPInvokeCallback(typeof(IRichPresenceChangeListener.SwigDelegateIRichPresenceChangeListener_0))]
		private static void SwigDirectorOnRichPresenceChangeSuccess(IntPtr cPtr)
		{
			if (IRichPresenceChangeListener.listeners.ContainsKey(cPtr))
			{
				IRichPresenceChangeListener.listeners[cPtr].OnRichPresenceChangeSuccess();
			}
		}

		// Token: 0x06000BA0 RID: 2976 RVA: 0x00013BDC File Offset: 0x00011DDC
		[MonoPInvokeCallback(typeof(IRichPresenceChangeListener.SwigDelegateIRichPresenceChangeListener_1))]
		private static void SwigDirectorOnRichPresenceChangeFailure(IntPtr cPtr, int failureReason)
		{
			if (IRichPresenceChangeListener.listeners.ContainsKey(cPtr))
			{
				IRichPresenceChangeListener.listeners[cPtr].OnRichPresenceChangeFailure((IRichPresenceChangeListener.FailureReason)failureReason);
			}
		}

		// Token: 0x04000222 RID: 546
		private static Dictionary<IntPtr, IRichPresenceChangeListener> listeners = new Dictionary<IntPtr, IRichPresenceChangeListener>();

		// Token: 0x04000223 RID: 547
		private HandleRef swigCPtr;

		// Token: 0x04000224 RID: 548
		private IRichPresenceChangeListener.SwigDelegateIRichPresenceChangeListener_0 swigDelegate0;

		// Token: 0x04000225 RID: 549
		private IRichPresenceChangeListener.SwigDelegateIRichPresenceChangeListener_1 swigDelegate1;

		// Token: 0x04000226 RID: 550
		private static Type[] swigMethodTypes0 = new Type[0];

		// Token: 0x04000227 RID: 551
		private static Type[] swigMethodTypes1 = new Type[] { typeof(IRichPresenceChangeListener.FailureReason) };

		// Token: 0x02000137 RID: 311
		// (Invoke) Token: 0x06000BA3 RID: 2979
		public delegate void SwigDelegateIRichPresenceChangeListener_0(IntPtr cPtr);

		// Token: 0x02000138 RID: 312
		// (Invoke) Token: 0x06000BA7 RID: 2983
		public delegate void SwigDelegateIRichPresenceChangeListener_1(IntPtr cPtr, int failureReason);

		// Token: 0x02000139 RID: 313
		public enum FailureReason
		{
			// Token: 0x04000229 RID: 553
			FAILURE_REASON_UNDEFINED,
			// Token: 0x0400022A RID: 554
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
