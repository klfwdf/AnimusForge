using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000125 RID: 293
	public abstract class INetworkingListener : GalaxyTypeAwareListenerNetworking
	{
		// Token: 0x06000B1F RID: 2847 RVA: 0x0000A6A0 File Offset: 0x000088A0
		internal INetworkingListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.INetworkingListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			INetworkingListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000B20 RID: 2848 RVA: 0x0000A6C8 File Offset: 0x000088C8
		public INetworkingListener()
			: this(GalaxyInstancePINVOKE.new_INetworkingListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000B21 RID: 2849 RVA: 0x0000A6EC File Offset: 0x000088EC
		internal static HandleRef getCPtr(INetworkingListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000B22 RID: 2850 RVA: 0x0000A70C File Offset: 0x0000890C
		~INetworkingListener()
		{
			this.Dispose();
		}

		// Token: 0x06000B23 RID: 2851 RVA: 0x0000A73C File Offset: 0x0000893C
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_INetworkingListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (INetworkingListener.listeners.ContainsKey(handle))
					{
						INetworkingListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000B24 RID: 2852
		public abstract void OnP2PPacketAvailable(uint msgSize, byte channel);

		// Token: 0x06000B25 RID: 2853 RVA: 0x0000A7EC File Offset: 0x000089EC
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnP2PPacketAvailable", INetworkingListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new INetworkingListener.SwigDelegateINetworkingListener_0(INetworkingListener.SwigDirectorOnP2PPacketAvailable);
			}
			GalaxyInstancePINVOKE.INetworkingListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x06000B26 RID: 2854 RVA: 0x0000A828 File Offset: 0x00008A28
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(INetworkingListener));
		}

		// Token: 0x06000B27 RID: 2855 RVA: 0x0000A85E File Offset: 0x00008A5E
		[MonoPInvokeCallback(typeof(INetworkingListener.SwigDelegateINetworkingListener_0))]
		private static void SwigDirectorOnP2PPacketAvailable(IntPtr cPtr, uint msgSize, byte channel)
		{
			if (INetworkingListener.listeners.ContainsKey(cPtr))
			{
				INetworkingListener.listeners[cPtr].OnP2PPacketAvailable(msgSize, channel);
			}
		}

		// Token: 0x040001F9 RID: 505
		private static Dictionary<IntPtr, INetworkingListener> listeners = new Dictionary<IntPtr, INetworkingListener>();

		// Token: 0x040001FA RID: 506
		private HandleRef swigCPtr;

		// Token: 0x040001FB RID: 507
		private INetworkingListener.SwigDelegateINetworkingListener_0 swigDelegate0;

		// Token: 0x040001FC RID: 508
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(uint),
			typeof(byte)
		};

		// Token: 0x02000126 RID: 294
		// (Invoke) Token: 0x06000B2A RID: 2858
		public delegate void SwigDelegateINetworkingListener_0(IntPtr cPtr, uint msgSize, byte channel);
	}
}
