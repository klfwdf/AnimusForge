using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000BF RID: 191
	public abstract class IConnectionOpenListener : GalaxyTypeAwareListenerConnectionOpen
	{
		// Token: 0x06000872 RID: 2162 RVA: 0x0000D79C File Offset: 0x0000B99C
		internal IConnectionOpenListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IConnectionOpenListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IConnectionOpenListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000873 RID: 2163 RVA: 0x0000D7C4 File Offset: 0x0000B9C4
		public IConnectionOpenListener()
			: this(GalaxyInstancePINVOKE.new_IConnectionOpenListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000874 RID: 2164 RVA: 0x0000D7E8 File Offset: 0x0000B9E8
		internal static HandleRef getCPtr(IConnectionOpenListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000875 RID: 2165 RVA: 0x0000D808 File Offset: 0x0000BA08
		~IConnectionOpenListener()
		{
			this.Dispose();
		}

		// Token: 0x06000876 RID: 2166 RVA: 0x0000D838 File Offset: 0x0000BA38
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IConnectionOpenListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IConnectionOpenListener.listeners.ContainsKey(handle))
					{
						IConnectionOpenListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000877 RID: 2167
		public abstract void OnConnectionOpenSuccess(string connectionString, ulong connectionID);

		// Token: 0x06000878 RID: 2168
		public abstract void OnConnectionOpenFailure(string connectionString, IConnectionOpenListener.FailureReason failureReason);

		// Token: 0x06000879 RID: 2169 RVA: 0x0000D8E8 File Offset: 0x0000BAE8
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnConnectionOpenSuccess", IConnectionOpenListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IConnectionOpenListener.SwigDelegateIConnectionOpenListener_0(IConnectionOpenListener.SwigDirectorOnConnectionOpenSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnConnectionOpenFailure", IConnectionOpenListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new IConnectionOpenListener.SwigDelegateIConnectionOpenListener_1(IConnectionOpenListener.SwigDirectorOnConnectionOpenFailure);
			}
			GalaxyInstancePINVOKE.IConnectionOpenListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x0600087A RID: 2170 RVA: 0x0000D95C File Offset: 0x0000BB5C
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IConnectionOpenListener));
		}

		// Token: 0x0600087B RID: 2171 RVA: 0x0000D992 File Offset: 0x0000BB92
		[MonoPInvokeCallback(typeof(IConnectionOpenListener.SwigDelegateIConnectionOpenListener_0))]
		private static void SwigDirectorOnConnectionOpenSuccess(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string connectionString, ulong connectionID)
		{
			if (IConnectionOpenListener.listeners.ContainsKey(cPtr))
			{
				IConnectionOpenListener.listeners[cPtr].OnConnectionOpenSuccess(connectionString, connectionID);
			}
		}

		// Token: 0x0600087C RID: 2172 RVA: 0x0000D9B6 File Offset: 0x0000BBB6
		[MonoPInvokeCallback(typeof(IConnectionOpenListener.SwigDelegateIConnectionOpenListener_1))]
		private static void SwigDirectorOnConnectionOpenFailure(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string connectionString, int failureReason)
		{
			if (IConnectionOpenListener.listeners.ContainsKey(cPtr))
			{
				IConnectionOpenListener.listeners[cPtr].OnConnectionOpenFailure(connectionString, (IConnectionOpenListener.FailureReason)failureReason);
			}
		}

		// Token: 0x0400010C RID: 268
		private static Dictionary<IntPtr, IConnectionOpenListener> listeners = new Dictionary<IntPtr, IConnectionOpenListener>();

		// Token: 0x0400010D RID: 269
		private HandleRef swigCPtr;

		// Token: 0x0400010E RID: 270
		private IConnectionOpenListener.SwigDelegateIConnectionOpenListener_0 swigDelegate0;

		// Token: 0x0400010F RID: 271
		private IConnectionOpenListener.SwigDelegateIConnectionOpenListener_1 swigDelegate1;

		// Token: 0x04000110 RID: 272
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(string),
			typeof(ulong)
		};

		// Token: 0x04000111 RID: 273
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(string),
			typeof(IConnectionOpenListener.FailureReason)
		};

		// Token: 0x020000C0 RID: 192
		// (Invoke) Token: 0x0600087F RID: 2175
		public delegate void SwigDelegateIConnectionOpenListener_0(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string connectionString, ulong connectionID);

		// Token: 0x020000C1 RID: 193
		// (Invoke) Token: 0x06000883 RID: 2179
		public delegate void SwigDelegateIConnectionOpenListener_1(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string connectionString, int failureReason);

		// Token: 0x020000C2 RID: 194
		public enum FailureReason
		{
			// Token: 0x04000113 RID: 275
			FAILURE_REASON_UNDEFINED,
			// Token: 0x04000114 RID: 276
			FAILURE_REASON_CONNECTION_FAILURE,
			// Token: 0x04000115 RID: 277
			FAILURE_REASON_UNAUTHORIZED
		}
	}
}
