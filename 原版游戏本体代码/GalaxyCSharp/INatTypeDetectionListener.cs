using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000121 RID: 289
	public abstract class INatTypeDetectionListener : GalaxyTypeAwareListenerNatTypeDetection
	{
		// Token: 0x06000AF9 RID: 2809 RVA: 0x0001234C File Offset: 0x0001054C
		internal INatTypeDetectionListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.INatTypeDetectionListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			INatTypeDetectionListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000AFA RID: 2810 RVA: 0x00012374 File Offset: 0x00010574
		public INatTypeDetectionListener()
			: this(GalaxyInstancePINVOKE.new_INatTypeDetectionListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000AFB RID: 2811 RVA: 0x00012398 File Offset: 0x00010598
		internal static HandleRef getCPtr(INatTypeDetectionListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000AFC RID: 2812 RVA: 0x000123B8 File Offset: 0x000105B8
		~INatTypeDetectionListener()
		{
			this.Dispose();
		}

		// Token: 0x06000AFD RID: 2813 RVA: 0x000123E8 File Offset: 0x000105E8
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_INatTypeDetectionListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (INatTypeDetectionListener.listeners.ContainsKey(handle))
					{
						INatTypeDetectionListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000AFE RID: 2814
		public abstract void OnNatTypeDetectionSuccess(NatType natType);

		// Token: 0x06000AFF RID: 2815
		public abstract void OnNatTypeDetectionFailure();

		// Token: 0x06000B00 RID: 2816 RVA: 0x00012498 File Offset: 0x00010698
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnNatTypeDetectionSuccess", INatTypeDetectionListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new INatTypeDetectionListener.SwigDelegateINatTypeDetectionListener_0(INatTypeDetectionListener.SwigDirectorOnNatTypeDetectionSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnNatTypeDetectionFailure", INatTypeDetectionListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new INatTypeDetectionListener.SwigDelegateINatTypeDetectionListener_1(INatTypeDetectionListener.SwigDirectorOnNatTypeDetectionFailure);
			}
			GalaxyInstancePINVOKE.INatTypeDetectionListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x06000B01 RID: 2817 RVA: 0x0001250C File Offset: 0x0001070C
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(INatTypeDetectionListener));
		}

		// Token: 0x06000B02 RID: 2818 RVA: 0x00012542 File Offset: 0x00010742
		[MonoPInvokeCallback(typeof(INatTypeDetectionListener.SwigDelegateINatTypeDetectionListener_0))]
		private static void SwigDirectorOnNatTypeDetectionSuccess(IntPtr cPtr, int natType)
		{
			if (INatTypeDetectionListener.listeners.ContainsKey(cPtr))
			{
				INatTypeDetectionListener.listeners[cPtr].OnNatTypeDetectionSuccess((NatType)natType);
			}
		}

		// Token: 0x06000B03 RID: 2819 RVA: 0x00012565 File Offset: 0x00010765
		[MonoPInvokeCallback(typeof(INatTypeDetectionListener.SwigDelegateINatTypeDetectionListener_1))]
		private static void SwigDirectorOnNatTypeDetectionFailure(IntPtr cPtr)
		{
			if (INatTypeDetectionListener.listeners.ContainsKey(cPtr))
			{
				INatTypeDetectionListener.listeners[cPtr].OnNatTypeDetectionFailure();
			}
		}

		// Token: 0x040001F1 RID: 497
		private static Dictionary<IntPtr, INatTypeDetectionListener> listeners = new Dictionary<IntPtr, INatTypeDetectionListener>();

		// Token: 0x040001F2 RID: 498
		private HandleRef swigCPtr;

		// Token: 0x040001F3 RID: 499
		private INatTypeDetectionListener.SwigDelegateINatTypeDetectionListener_0 swigDelegate0;

		// Token: 0x040001F4 RID: 500
		private INatTypeDetectionListener.SwigDelegateINatTypeDetectionListener_1 swigDelegate1;

		// Token: 0x040001F5 RID: 501
		private static Type[] swigMethodTypes0 = new Type[] { typeof(NatType) };

		// Token: 0x040001F6 RID: 502
		private static Type[] swigMethodTypes1 = new Type[0];

		// Token: 0x02000122 RID: 290
		// (Invoke) Token: 0x06000B06 RID: 2822
		public delegate void SwigDelegateINatTypeDetectionListener_0(IntPtr cPtr, int natType);

		// Token: 0x02000123 RID: 291
		// (Invoke) Token: 0x06000B0A RID: 2826
		public delegate void SwigDelegateINatTypeDetectionListener_1(IntPtr cPtr);
	}
}
