using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000BA RID: 186
	public abstract class IConnectionCloseListener : GalaxyTypeAwareListenerConnectionClose
	{
		// Token: 0x06000856 RID: 2134 RVA: 0x0000D11C File Offset: 0x0000B31C
		internal IConnectionCloseListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IConnectionCloseListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IConnectionCloseListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000857 RID: 2135 RVA: 0x0000D144 File Offset: 0x0000B344
		public IConnectionCloseListener()
			: this(GalaxyInstancePINVOKE.new_IConnectionCloseListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000858 RID: 2136 RVA: 0x0000D168 File Offset: 0x0000B368
		internal static HandleRef getCPtr(IConnectionCloseListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000859 RID: 2137 RVA: 0x0000D188 File Offset: 0x0000B388
		~IConnectionCloseListener()
		{
			this.Dispose();
		}

		// Token: 0x0600085A RID: 2138 RVA: 0x0000D1B8 File Offset: 0x0000B3B8
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IConnectionCloseListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IConnectionCloseListener.listeners.ContainsKey(handle))
					{
						IConnectionCloseListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x0600085B RID: 2139
		public abstract void OnConnectionClosed(ulong connectionID, IConnectionCloseListener.CloseReason closeReason);

		// Token: 0x0600085C RID: 2140 RVA: 0x0000D268 File Offset: 0x0000B468
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnConnectionClosed", IConnectionCloseListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IConnectionCloseListener.SwigDelegateIConnectionCloseListener_0(IConnectionCloseListener.SwigDirectorOnConnectionClosed);
			}
			GalaxyInstancePINVOKE.IConnectionCloseListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x0600085D RID: 2141 RVA: 0x0000D2A4 File Offset: 0x0000B4A4
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IConnectionCloseListener));
		}

		// Token: 0x0600085E RID: 2142 RVA: 0x0000D2DA File Offset: 0x0000B4DA
		[MonoPInvokeCallback(typeof(IConnectionCloseListener.SwigDelegateIConnectionCloseListener_0))]
		private static void SwigDirectorOnConnectionClosed(IntPtr cPtr, ulong connectionID, int closeReason)
		{
			if (IConnectionCloseListener.listeners.ContainsKey(cPtr))
			{
				IConnectionCloseListener.listeners[cPtr].OnConnectionClosed(connectionID, (IConnectionCloseListener.CloseReason)closeReason);
			}
		}

		// Token: 0x04000102 RID: 258
		private static Dictionary<IntPtr, IConnectionCloseListener> listeners = new Dictionary<IntPtr, IConnectionCloseListener>();

		// Token: 0x04000103 RID: 259
		private HandleRef swigCPtr;

		// Token: 0x04000104 RID: 260
		private IConnectionCloseListener.SwigDelegateIConnectionCloseListener_0 swigDelegate0;

		// Token: 0x04000105 RID: 261
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(ulong),
			typeof(IConnectionCloseListener.CloseReason)
		};

		// Token: 0x020000BB RID: 187
		// (Invoke) Token: 0x06000861 RID: 2145
		public delegate void SwigDelegateIConnectionCloseListener_0(IntPtr cPtr, ulong connectionID, int closeReason);

		// Token: 0x020000BC RID: 188
		public enum CloseReason
		{
			// Token: 0x04000107 RID: 263
			CLOSE_REASON_UNDEFINED
		}
	}
}
