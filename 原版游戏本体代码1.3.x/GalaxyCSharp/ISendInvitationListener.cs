using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000141 RID: 321
	public abstract class ISendInvitationListener : GalaxyTypeAwareListenerSendInvitation
	{
		// Token: 0x06000BD0 RID: 3024 RVA: 0x000141CC File Offset: 0x000123CC
		internal ISendInvitationListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.ISendInvitationListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			ISendInvitationListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000BD1 RID: 3025 RVA: 0x000141F4 File Offset: 0x000123F4
		public ISendInvitationListener()
			: this(GalaxyInstancePINVOKE.new_ISendInvitationListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000BD2 RID: 3026 RVA: 0x00014218 File Offset: 0x00012418
		internal static HandleRef getCPtr(ISendInvitationListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000BD3 RID: 3027 RVA: 0x00014238 File Offset: 0x00012438
		~ISendInvitationListener()
		{
			this.Dispose();
		}

		// Token: 0x06000BD4 RID: 3028 RVA: 0x00014268 File Offset: 0x00012468
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ISendInvitationListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (ISendInvitationListener.listeners.ContainsKey(handle))
					{
						ISendInvitationListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000BD5 RID: 3029
		public abstract void OnInvitationSendSuccess(GalaxyID userID, string connectionString);

		// Token: 0x06000BD6 RID: 3030
		public abstract void OnInvitationSendFailure(GalaxyID userID, string connectionString, ISendInvitationListener.FailureReason failureReason);

		// Token: 0x06000BD7 RID: 3031 RVA: 0x00014318 File Offset: 0x00012518
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnInvitationSendSuccess", ISendInvitationListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new ISendInvitationListener.SwigDelegateISendInvitationListener_0(ISendInvitationListener.SwigDirectorOnInvitationSendSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnInvitationSendFailure", ISendInvitationListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new ISendInvitationListener.SwigDelegateISendInvitationListener_1(ISendInvitationListener.SwigDirectorOnInvitationSendFailure);
			}
			GalaxyInstancePINVOKE.ISendInvitationListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x06000BD8 RID: 3032 RVA: 0x0001438C File Offset: 0x0001258C
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(ISendInvitationListener));
		}

		// Token: 0x06000BD9 RID: 3033 RVA: 0x000143C2 File Offset: 0x000125C2
		[MonoPInvokeCallback(typeof(ISendInvitationListener.SwigDelegateISendInvitationListener_0))]
		private static void SwigDirectorOnInvitationSendSuccess(IntPtr cPtr, IntPtr userID, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string connectionString)
		{
			if (ISendInvitationListener.listeners.ContainsKey(cPtr))
			{
				ISendInvitationListener.listeners[cPtr].OnInvitationSendSuccess(new GalaxyID(new GalaxyID(userID, false).ToUint64()), connectionString);
			}
		}

		// Token: 0x06000BDA RID: 3034 RVA: 0x000143F6 File Offset: 0x000125F6
		[MonoPInvokeCallback(typeof(ISendInvitationListener.SwigDelegateISendInvitationListener_1))]
		private static void SwigDirectorOnInvitationSendFailure(IntPtr cPtr, IntPtr userID, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string connectionString, int failureReason)
		{
			if (ISendInvitationListener.listeners.ContainsKey(cPtr))
			{
				ISendInvitationListener.listeners[cPtr].OnInvitationSendFailure(new GalaxyID(new GalaxyID(userID, false).ToUint64()), connectionString, (ISendInvitationListener.FailureReason)failureReason);
			}
		}

		// Token: 0x04000239 RID: 569
		private static Dictionary<IntPtr, ISendInvitationListener> listeners = new Dictionary<IntPtr, ISendInvitationListener>();

		// Token: 0x0400023A RID: 570
		private HandleRef swigCPtr;

		// Token: 0x0400023B RID: 571
		private ISendInvitationListener.SwigDelegateISendInvitationListener_0 swigDelegate0;

		// Token: 0x0400023C RID: 572
		private ISendInvitationListener.SwigDelegateISendInvitationListener_1 swigDelegate1;

		// Token: 0x0400023D RID: 573
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(GalaxyID),
			typeof(string)
		};

		// Token: 0x0400023E RID: 574
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(GalaxyID),
			typeof(string),
			typeof(ISendInvitationListener.FailureReason)
		};

		// Token: 0x02000142 RID: 322
		// (Invoke) Token: 0x06000BDD RID: 3037
		public delegate void SwigDelegateISendInvitationListener_0(IntPtr cPtr, IntPtr userID, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string connectionString);

		// Token: 0x02000143 RID: 323
		// (Invoke) Token: 0x06000BE1 RID: 3041
		public delegate void SwigDelegateISendInvitationListener_1(IntPtr cPtr, IntPtr userID, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string connectionString, int failureReason);

		// Token: 0x02000144 RID: 324
		public enum FailureReason
		{
			// Token: 0x04000240 RID: 576
			FAILURE_REASON_UNDEFINED,
			// Token: 0x04000241 RID: 577
			FAILURE_REASON_USER_DOES_NOT_EXIST,
			// Token: 0x04000242 RID: 578
			FAILURE_REASON_RECEIVER_DOES_NOT_ALLOW_INVITING,
			// Token: 0x04000243 RID: 579
			FAILURE_REASON_SENDER_DOES_NOT_ALLOW_INVITING,
			// Token: 0x04000244 RID: 580
			FAILURE_REASON_RECEIVER_BLOCKED,
			// Token: 0x04000245 RID: 581
			FAILURE_REASON_SENDER_BLOCKED,
			// Token: 0x04000246 RID: 582
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
