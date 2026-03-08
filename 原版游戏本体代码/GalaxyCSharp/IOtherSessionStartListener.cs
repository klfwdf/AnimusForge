using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200012D RID: 301
	public abstract class IOtherSessionStartListener : GalaxyTypeAwareListenerOtherSessionStart
	{
		// Token: 0x06000B5E RID: 2910 RVA: 0x00012C8C File Offset: 0x00010E8C
		internal IOtherSessionStartListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IOtherSessionStartListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IOtherSessionStartListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000B5F RID: 2911 RVA: 0x00012CB4 File Offset: 0x00010EB4
		public IOtherSessionStartListener()
			: this(GalaxyInstancePINVOKE.new_IOtherSessionStartListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000B60 RID: 2912 RVA: 0x00012CD8 File Offset: 0x00010ED8
		internal static HandleRef getCPtr(IOtherSessionStartListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000B61 RID: 2913 RVA: 0x00012CF8 File Offset: 0x00010EF8
		~IOtherSessionStartListener()
		{
			this.Dispose();
		}

		// Token: 0x06000B62 RID: 2914 RVA: 0x00012D28 File Offset: 0x00010F28
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IOtherSessionStartListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IOtherSessionStartListener.listeners.ContainsKey(handle))
					{
						IOtherSessionStartListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000B63 RID: 2915
		public abstract void OnOtherSessionStarted();

		// Token: 0x06000B64 RID: 2916 RVA: 0x00012DD8 File Offset: 0x00010FD8
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnOtherSessionStarted", IOtherSessionStartListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IOtherSessionStartListener.SwigDelegateIOtherSessionStartListener_0(IOtherSessionStartListener.SwigDirectorOnOtherSessionStarted);
			}
			GalaxyInstancePINVOKE.IOtherSessionStartListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x06000B65 RID: 2917 RVA: 0x00012E14 File Offset: 0x00011014
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IOtherSessionStartListener));
		}

		// Token: 0x06000B66 RID: 2918 RVA: 0x00012E4A File Offset: 0x0001104A
		[MonoPInvokeCallback(typeof(IOtherSessionStartListener.SwigDelegateIOtherSessionStartListener_0))]
		private static void SwigDirectorOnOtherSessionStarted(IntPtr cPtr)
		{
			if (IOtherSessionStartListener.listeners.ContainsKey(cPtr))
			{
				IOtherSessionStartListener.listeners[cPtr].OnOtherSessionStarted();
			}
		}

		// Token: 0x0400020A RID: 522
		private static Dictionary<IntPtr, IOtherSessionStartListener> listeners = new Dictionary<IntPtr, IOtherSessionStartListener>();

		// Token: 0x0400020B RID: 523
		private HandleRef swigCPtr;

		// Token: 0x0400020C RID: 524
		private IOtherSessionStartListener.SwigDelegateIOtherSessionStartListener_0 swigDelegate0;

		// Token: 0x0400020D RID: 525
		private static Type[] swigMethodTypes0 = new Type[0];

		// Token: 0x0200012E RID: 302
		// (Invoke) Token: 0x06000B69 RID: 2921
		public delegate void SwigDelegateIOtherSessionStartListener_0(IntPtr cPtr);
	}
}
