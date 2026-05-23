using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200013A RID: 314
	public abstract class IRichPresenceListener : GalaxyTypeAwareListenerRichPresence
	{
		// Token: 0x06000BAA RID: 2986 RVA: 0x00013D5C File Offset: 0x00011F5C
		internal IRichPresenceListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IRichPresenceListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IRichPresenceListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000BAB RID: 2987 RVA: 0x00013D84 File Offset: 0x00011F84
		public IRichPresenceListener()
			: this(GalaxyInstancePINVOKE.new_IRichPresenceListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000BAC RID: 2988 RVA: 0x00013DA8 File Offset: 0x00011FA8
		internal static HandleRef getCPtr(IRichPresenceListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000BAD RID: 2989 RVA: 0x00013DC8 File Offset: 0x00011FC8
		~IRichPresenceListener()
		{
			this.Dispose();
		}

		// Token: 0x06000BAE RID: 2990 RVA: 0x00013DF8 File Offset: 0x00011FF8
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IRichPresenceListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IRichPresenceListener.listeners.ContainsKey(handle))
					{
						IRichPresenceListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000BAF RID: 2991
		public abstract void OnRichPresenceUpdated(GalaxyID userID);

		// Token: 0x06000BB0 RID: 2992 RVA: 0x00013EA8 File Offset: 0x000120A8
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnRichPresenceUpdated", IRichPresenceListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IRichPresenceListener.SwigDelegateIRichPresenceListener_0(IRichPresenceListener.SwigDirectorOnRichPresenceUpdated);
			}
			GalaxyInstancePINVOKE.IRichPresenceListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x06000BB1 RID: 2993 RVA: 0x00013EE4 File Offset: 0x000120E4
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IRichPresenceListener));
		}

		// Token: 0x06000BB2 RID: 2994 RVA: 0x00013F1A File Offset: 0x0001211A
		[MonoPInvokeCallback(typeof(IRichPresenceListener.SwigDelegateIRichPresenceListener_0))]
		private static void SwigDirectorOnRichPresenceUpdated(IntPtr cPtr, IntPtr userID)
		{
			if (IRichPresenceListener.listeners.ContainsKey(cPtr))
			{
				IRichPresenceListener.listeners[cPtr].OnRichPresenceUpdated(new GalaxyID(new GalaxyID(userID, false).ToUint64()));
			}
		}

		// Token: 0x0400022B RID: 555
		private static Dictionary<IntPtr, IRichPresenceListener> listeners = new Dictionary<IntPtr, IRichPresenceListener>();

		// Token: 0x0400022C RID: 556
		private HandleRef swigCPtr;

		// Token: 0x0400022D RID: 557
		private IRichPresenceListener.SwigDelegateIRichPresenceListener_0 swigDelegate0;

		// Token: 0x0400022E RID: 558
		private static Type[] swigMethodTypes0 = new Type[] { typeof(GalaxyID) };

		// Token: 0x0200013B RID: 315
		// (Invoke) Token: 0x06000BB5 RID: 2997
		public delegate void SwigDelegateIRichPresenceListener_0(IntPtr cPtr, IntPtr userID);
	}
}
