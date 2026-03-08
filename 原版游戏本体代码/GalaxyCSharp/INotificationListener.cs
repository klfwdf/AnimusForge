using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000128 RID: 296
	public abstract class INotificationListener : GalaxyTypeAwareListenerNotification
	{
		// Token: 0x06000B42 RID: 2882 RVA: 0x00012810 File Offset: 0x00010A10
		internal INotificationListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.INotificationListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			INotificationListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000B43 RID: 2883 RVA: 0x00012838 File Offset: 0x00010A38
		public INotificationListener()
			: this(GalaxyInstancePINVOKE.new_INotificationListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000B44 RID: 2884 RVA: 0x0001285C File Offset: 0x00010A5C
		internal static HandleRef getCPtr(INotificationListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000B45 RID: 2885 RVA: 0x0001287C File Offset: 0x00010A7C
		~INotificationListener()
		{
			this.Dispose();
		}

		// Token: 0x06000B46 RID: 2886 RVA: 0x000128AC File Offset: 0x00010AAC
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_INotificationListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (INotificationListener.listeners.ContainsKey(handle))
					{
						INotificationListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000B47 RID: 2887
		public abstract void OnNotificationReceived(ulong notificationID, uint typeLength, uint contentSize);

		// Token: 0x06000B48 RID: 2888 RVA: 0x0001295C File Offset: 0x00010B5C
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnNotificationReceived", INotificationListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new INotificationListener.SwigDelegateINotificationListener_0(INotificationListener.SwigDirectorOnNotificationReceived);
			}
			GalaxyInstancePINVOKE.INotificationListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x06000B49 RID: 2889 RVA: 0x00012998 File Offset: 0x00010B98
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(INotificationListener));
		}

		// Token: 0x06000B4A RID: 2890 RVA: 0x000129CE File Offset: 0x00010BCE
		[MonoPInvokeCallback(typeof(INotificationListener.SwigDelegateINotificationListener_0))]
		private static void SwigDirectorOnNotificationReceived(IntPtr cPtr, ulong notificationID, uint typeLength, uint contentSize)
		{
			if (INotificationListener.listeners.ContainsKey(cPtr))
			{
				INotificationListener.listeners[cPtr].OnNotificationReceived(notificationID, typeLength, contentSize);
			}
		}

		// Token: 0x040001FF RID: 511
		private static Dictionary<IntPtr, INotificationListener> listeners = new Dictionary<IntPtr, INotificationListener>();

		// Token: 0x04000200 RID: 512
		private HandleRef swigCPtr;

		// Token: 0x04000201 RID: 513
		private INotificationListener.SwigDelegateINotificationListener_0 swigDelegate0;

		// Token: 0x04000202 RID: 514
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(ulong),
			typeof(uint),
			typeof(uint)
		};

		// Token: 0x02000129 RID: 297
		// (Invoke) Token: 0x06000B4D RID: 2893
		public delegate void SwigDelegateINotificationListener_0(IntPtr cPtr, ulong notificationID, uint typeLength, uint contentSize);
	}
}
