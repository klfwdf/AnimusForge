using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000BD RID: 189
	public abstract class IConnectionDataListener : GalaxyTypeAwareListenerConnectionData
	{
		// Token: 0x06000864 RID: 2148 RVA: 0x0000D45C File Offset: 0x0000B65C
		internal IConnectionDataListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IConnectionDataListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IConnectionDataListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000865 RID: 2149 RVA: 0x0000D484 File Offset: 0x0000B684
		public IConnectionDataListener()
			: this(GalaxyInstancePINVOKE.new_IConnectionDataListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000866 RID: 2150 RVA: 0x0000D4A8 File Offset: 0x0000B6A8
		internal static HandleRef getCPtr(IConnectionDataListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000867 RID: 2151 RVA: 0x0000D4C8 File Offset: 0x0000B6C8
		~IConnectionDataListener()
		{
			this.Dispose();
		}

		// Token: 0x06000868 RID: 2152 RVA: 0x0000D4F8 File Offset: 0x0000B6F8
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IConnectionDataListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IConnectionDataListener.listeners.ContainsKey(handle))
					{
						IConnectionDataListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000869 RID: 2153
		public abstract void OnConnectionDataReceived(ulong connectionID, uint dataSize);

		// Token: 0x0600086A RID: 2154 RVA: 0x0000D5A8 File Offset: 0x0000B7A8
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnConnectionDataReceived", IConnectionDataListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IConnectionDataListener.SwigDelegateIConnectionDataListener_0(IConnectionDataListener.SwigDirectorOnConnectionDataReceived);
			}
			GalaxyInstancePINVOKE.IConnectionDataListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x0600086B RID: 2155 RVA: 0x0000D5E4 File Offset: 0x0000B7E4
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IConnectionDataListener));
		}

		// Token: 0x0600086C RID: 2156 RVA: 0x0000D61A File Offset: 0x0000B81A
		[MonoPInvokeCallback(typeof(IConnectionDataListener.SwigDelegateIConnectionDataListener_0))]
		private static void SwigDirectorOnConnectionDataReceived(IntPtr cPtr, ulong connectionID, uint dataSize)
		{
			if (IConnectionDataListener.listeners.ContainsKey(cPtr))
			{
				IConnectionDataListener.listeners[cPtr].OnConnectionDataReceived(connectionID, dataSize);
			}
		}

		// Token: 0x04000108 RID: 264
		private static Dictionary<IntPtr, IConnectionDataListener> listeners = new Dictionary<IntPtr, IConnectionDataListener>();

		// Token: 0x04000109 RID: 265
		private HandleRef swigCPtr;

		// Token: 0x0400010A RID: 266
		private IConnectionDataListener.SwigDelegateIConnectionDataListener_0 swigDelegate0;

		// Token: 0x0400010B RID: 267
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(ulong),
			typeof(uint)
		};

		// Token: 0x020000BE RID: 190
		// (Invoke) Token: 0x0600086F RID: 2159
		public delegate void SwigDelegateIConnectionDataListener_0(IntPtr cPtr, ulong connectionID, uint dataSize);
	}
}
