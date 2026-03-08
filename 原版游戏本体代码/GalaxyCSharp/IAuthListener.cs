using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000A6 RID: 166
	public abstract class IAuthListener : GalaxyTypeAwareListenerAuth
	{
		// Token: 0x060007E2 RID: 2018 RVA: 0x00007B60 File Offset: 0x00005D60
		internal IAuthListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IAuthListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IAuthListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060007E3 RID: 2019 RVA: 0x00007B88 File Offset: 0x00005D88
		public IAuthListener()
			: this(GalaxyInstancePINVOKE.new_IAuthListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x060007E4 RID: 2020 RVA: 0x00007BAC File Offset: 0x00005DAC
		internal static HandleRef getCPtr(IAuthListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060007E5 RID: 2021 RVA: 0x00007BCC File Offset: 0x00005DCC
		~IAuthListener()
		{
			this.Dispose();
		}

		// Token: 0x060007E6 RID: 2022 RVA: 0x00007BFC File Offset: 0x00005DFC
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IAuthListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IAuthListener.listeners.ContainsKey(handle))
					{
						IAuthListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060007E7 RID: 2023
		public abstract void OnAuthSuccess();

		// Token: 0x060007E8 RID: 2024
		public abstract void OnAuthFailure(IAuthListener.FailureReason failureReason);

		// Token: 0x060007E9 RID: 2025
		public abstract void OnAuthLost();

		// Token: 0x060007EA RID: 2026 RVA: 0x00007CAC File Offset: 0x00005EAC
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnAuthSuccess", IAuthListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IAuthListener.SwigDelegateIAuthListener_0(IAuthListener.SwigDirectorOnAuthSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnAuthFailure", IAuthListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new IAuthListener.SwigDelegateIAuthListener_1(IAuthListener.SwigDirectorOnAuthFailure);
			}
			if (this.SwigDerivedClassHasMethod("OnAuthLost", IAuthListener.swigMethodTypes2))
			{
				this.swigDelegate2 = new IAuthListener.SwigDelegateIAuthListener_2(IAuthListener.SwigDirectorOnAuthLost);
			}
			GalaxyInstancePINVOKE.IAuthListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1, this.swigDelegate2);
		}

		// Token: 0x060007EB RID: 2027 RVA: 0x00007D4C File Offset: 0x00005F4C
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IAuthListener));
		}

		// Token: 0x060007EC RID: 2028 RVA: 0x00007D82 File Offset: 0x00005F82
		[MonoPInvokeCallback(typeof(IAuthListener.SwigDelegateIAuthListener_0))]
		private static void SwigDirectorOnAuthSuccess(IntPtr cPtr)
		{
			if (IAuthListener.listeners.ContainsKey(cPtr))
			{
				IAuthListener.listeners[cPtr].OnAuthSuccess();
			}
		}

		// Token: 0x060007ED RID: 2029 RVA: 0x00007DA4 File Offset: 0x00005FA4
		[MonoPInvokeCallback(typeof(IAuthListener.SwigDelegateIAuthListener_1))]
		private static void SwigDirectorOnAuthFailure(IntPtr cPtr, int failureReason)
		{
			if (IAuthListener.listeners.ContainsKey(cPtr))
			{
				IAuthListener.listeners[cPtr].OnAuthFailure((IAuthListener.FailureReason)failureReason);
			}
		}

		// Token: 0x060007EE RID: 2030 RVA: 0x00007DC7 File Offset: 0x00005FC7
		[MonoPInvokeCallback(typeof(IAuthListener.SwigDelegateIAuthListener_2))]
		private static void SwigDirectorOnAuthLost(IntPtr cPtr)
		{
			if (IAuthListener.listeners.ContainsKey(cPtr))
			{
				IAuthListener.listeners[cPtr].OnAuthLost();
			}
		}

		// Token: 0x040000CD RID: 205
		private static Dictionary<IntPtr, IAuthListener> listeners = new Dictionary<IntPtr, IAuthListener>();

		// Token: 0x040000CE RID: 206
		private HandleRef swigCPtr;

		// Token: 0x040000CF RID: 207
		private IAuthListener.SwigDelegateIAuthListener_0 swigDelegate0;

		// Token: 0x040000D0 RID: 208
		private IAuthListener.SwigDelegateIAuthListener_1 swigDelegate1;

		// Token: 0x040000D1 RID: 209
		private IAuthListener.SwigDelegateIAuthListener_2 swigDelegate2;

		// Token: 0x040000D2 RID: 210
		private static Type[] swigMethodTypes0 = new Type[0];

		// Token: 0x040000D3 RID: 211
		private static Type[] swigMethodTypes1 = new Type[] { typeof(IAuthListener.FailureReason) };

		// Token: 0x040000D4 RID: 212
		private static Type[] swigMethodTypes2 = new Type[0];

		// Token: 0x020000A7 RID: 167
		// (Invoke) Token: 0x060007F1 RID: 2033
		public delegate void SwigDelegateIAuthListener_0(IntPtr cPtr);

		// Token: 0x020000A8 RID: 168
		// (Invoke) Token: 0x060007F5 RID: 2037
		public delegate void SwigDelegateIAuthListener_1(IntPtr cPtr, int failureReason);

		// Token: 0x020000A9 RID: 169
		// (Invoke) Token: 0x060007F9 RID: 2041
		public delegate void SwigDelegateIAuthListener_2(IntPtr cPtr);

		// Token: 0x020000AA RID: 170
		public enum FailureReason
		{
			// Token: 0x040000D6 RID: 214
			FAILURE_REASON_UNDEFINED,
			// Token: 0x040000D7 RID: 215
			FAILURE_REASON_GALAXY_SERVICE_NOT_AVAILABLE,
			// Token: 0x040000D8 RID: 216
			FAILURE_REASON_GALAXY_SERVICE_NOT_SIGNED_IN,
			// Token: 0x040000D9 RID: 217
			FAILURE_REASON_CONNECTION_FAILURE,
			// Token: 0x040000DA RID: 218
			FAILURE_REASON_NO_LICENSE,
			// Token: 0x040000DB RID: 219
			FAILURE_REASON_INVALID_CREDENTIALS,
			// Token: 0x040000DC RID: 220
			FAILURE_REASON_GALAXY_NOT_INITIALIZED,
			// Token: 0x040000DD RID: 221
			FAILURE_REASON_EXTERNAL_SERVICE_FAILURE
		}
	}
}
