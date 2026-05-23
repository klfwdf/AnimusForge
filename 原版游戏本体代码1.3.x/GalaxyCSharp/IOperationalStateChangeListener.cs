using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x0200012A RID: 298
	public abstract class IOperationalStateChangeListener : GalaxyTypeAwareListenerOperationalStateChange
	{
		// Token: 0x06000B50 RID: 2896 RVA: 0x0000A9E0 File Offset: 0x00008BE0
		internal IOperationalStateChangeListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IOperationalStateChangeListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IOperationalStateChangeListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000B51 RID: 2897 RVA: 0x0000AA08 File Offset: 0x00008C08
		public IOperationalStateChangeListener()
			: this(GalaxyInstancePINVOKE.new_IOperationalStateChangeListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000B52 RID: 2898 RVA: 0x0000AA2C File Offset: 0x00008C2C
		internal static HandleRef getCPtr(IOperationalStateChangeListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000B53 RID: 2899 RVA: 0x0000AA4C File Offset: 0x00008C4C
		~IOperationalStateChangeListener()
		{
			this.Dispose();
		}

		// Token: 0x06000B54 RID: 2900 RVA: 0x0000AA7C File Offset: 0x00008C7C
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IOperationalStateChangeListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IOperationalStateChangeListener.listeners.ContainsKey(handle))
					{
						IOperationalStateChangeListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000B55 RID: 2901
		public abstract void OnOperationalStateChanged(uint operationalState);

		// Token: 0x06000B56 RID: 2902 RVA: 0x0000AB2C File Offset: 0x00008D2C
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnOperationalStateChanged", IOperationalStateChangeListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IOperationalStateChangeListener.SwigDelegateIOperationalStateChangeListener_0(IOperationalStateChangeListener.SwigDirectorOnOperationalStateChanged);
			}
			GalaxyInstancePINVOKE.IOperationalStateChangeListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x06000B57 RID: 2903 RVA: 0x0000AB68 File Offset: 0x00008D68
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IOperationalStateChangeListener));
		}

		// Token: 0x06000B58 RID: 2904 RVA: 0x0000AB9E File Offset: 0x00008D9E
		[MonoPInvokeCallback(typeof(IOperationalStateChangeListener.SwigDelegateIOperationalStateChangeListener_0))]
		private static void SwigDirectorOnOperationalStateChanged(IntPtr cPtr, uint operationalState)
		{
			if (IOperationalStateChangeListener.listeners.ContainsKey(cPtr))
			{
				IOperationalStateChangeListener.listeners[cPtr].OnOperationalStateChanged(operationalState);
			}
		}

		// Token: 0x04000203 RID: 515
		private static Dictionary<IntPtr, IOperationalStateChangeListener> listeners = new Dictionary<IntPtr, IOperationalStateChangeListener>();

		// Token: 0x04000204 RID: 516
		private HandleRef swigCPtr;

		// Token: 0x04000205 RID: 517
		private IOperationalStateChangeListener.SwigDelegateIOperationalStateChangeListener_0 swigDelegate0;

		// Token: 0x04000206 RID: 518
		private static Type[] swigMethodTypes0 = new Type[] { typeof(uint) };

		// Token: 0x0200012B RID: 299
		// (Invoke) Token: 0x06000B5B RID: 2907
		public delegate void SwigDelegateIOperationalStateChangeListener_0(IntPtr cPtr, uint operationalState);

		// Token: 0x0200012C RID: 300
		public enum OperationalState
		{
			// Token: 0x04000208 RID: 520
			OPERATIONAL_STATE_SIGNED_IN = 1,
			// Token: 0x04000209 RID: 521
			OPERATIONAL_STATE_LOGGED_ON
		}
	}
}
