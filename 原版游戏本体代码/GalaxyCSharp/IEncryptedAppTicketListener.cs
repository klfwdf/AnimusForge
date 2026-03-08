using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000C4 RID: 196
	public abstract class IEncryptedAppTicketListener : GalaxyTypeAwareListenerEncryptedAppTicket
	{
		// Token: 0x06000893 RID: 2195 RVA: 0x00007F50 File Offset: 0x00006150
		internal IEncryptedAppTicketListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IEncryptedAppTicketListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IEncryptedAppTicketListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000894 RID: 2196 RVA: 0x00007F78 File Offset: 0x00006178
		public IEncryptedAppTicketListener()
			: this(GalaxyInstancePINVOKE.new_IEncryptedAppTicketListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000895 RID: 2197 RVA: 0x00007F9C File Offset: 0x0000619C
		internal static HandleRef getCPtr(IEncryptedAppTicketListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000896 RID: 2198 RVA: 0x00007FBC File Offset: 0x000061BC
		~IEncryptedAppTicketListener()
		{
			this.Dispose();
		}

		// Token: 0x06000897 RID: 2199 RVA: 0x00007FEC File Offset: 0x000061EC
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IEncryptedAppTicketListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IEncryptedAppTicketListener.listeners.ContainsKey(handle))
					{
						IEncryptedAppTicketListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000898 RID: 2200
		public abstract void OnEncryptedAppTicketRetrieveSuccess();

		// Token: 0x06000899 RID: 2201 RVA: 0x0000809C File Offset: 0x0000629C
		public virtual void OnEncryptedAppTicketRetrieveFailure(IEncryptedAppTicketListener.FailureReason failureReason)
		{
			GalaxyInstancePINVOKE.IEncryptedAppTicketListener_OnEncryptedAppTicketRetrieveFailure(this.swigCPtr, (int)failureReason);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600089A RID: 2202 RVA: 0x000080BC File Offset: 0x000062BC
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnEncryptedAppTicketRetrieveSuccess", IEncryptedAppTicketListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IEncryptedAppTicketListener.SwigDelegateIEncryptedAppTicketListener_0(IEncryptedAppTicketListener.SwigDirectorOnEncryptedAppTicketRetrieveSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnEncryptedAppTicketRetrieveFailure", IEncryptedAppTicketListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new IEncryptedAppTicketListener.SwigDelegateIEncryptedAppTicketListener_1(IEncryptedAppTicketListener.SwigDirectorOnEncryptedAppTicketRetrieveFailure);
			}
			GalaxyInstancePINVOKE.IEncryptedAppTicketListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x0600089B RID: 2203 RVA: 0x00008130 File Offset: 0x00006330
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IEncryptedAppTicketListener));
		}

		// Token: 0x0600089C RID: 2204 RVA: 0x00008166 File Offset: 0x00006366
		[MonoPInvokeCallback(typeof(IEncryptedAppTicketListener.SwigDelegateIEncryptedAppTicketListener_0))]
		private static void SwigDirectorOnEncryptedAppTicketRetrieveSuccess(IntPtr cPtr)
		{
			if (IEncryptedAppTicketListener.listeners.ContainsKey(cPtr))
			{
				IEncryptedAppTicketListener.listeners[cPtr].OnEncryptedAppTicketRetrieveSuccess();
			}
		}

		// Token: 0x0600089D RID: 2205 RVA: 0x00008188 File Offset: 0x00006388
		[MonoPInvokeCallback(typeof(IEncryptedAppTicketListener.SwigDelegateIEncryptedAppTicketListener_1))]
		private static void SwigDirectorOnEncryptedAppTicketRetrieveFailure(IntPtr cPtr, int failureReason)
		{
			if (IEncryptedAppTicketListener.listeners.ContainsKey(cPtr))
			{
				IEncryptedAppTicketListener.listeners[cPtr].OnEncryptedAppTicketRetrieveFailure((IEncryptedAppTicketListener.FailureReason)failureReason);
			}
		}

		// Token: 0x04000118 RID: 280
		private static Dictionary<IntPtr, IEncryptedAppTicketListener> listeners = new Dictionary<IntPtr, IEncryptedAppTicketListener>();

		// Token: 0x04000119 RID: 281
		private HandleRef swigCPtr;

		// Token: 0x0400011A RID: 282
		private IEncryptedAppTicketListener.SwigDelegateIEncryptedAppTicketListener_0 swigDelegate0;

		// Token: 0x0400011B RID: 283
		private IEncryptedAppTicketListener.SwigDelegateIEncryptedAppTicketListener_1 swigDelegate1;

		// Token: 0x0400011C RID: 284
		private static Type[] swigMethodTypes0 = new Type[0];

		// Token: 0x0400011D RID: 285
		private static Type[] swigMethodTypes1 = new Type[] { typeof(IEncryptedAppTicketListener.FailureReason) };

		// Token: 0x020000C5 RID: 197
		// (Invoke) Token: 0x060008A0 RID: 2208
		public delegate void SwigDelegateIEncryptedAppTicketListener_0(IntPtr cPtr);

		// Token: 0x020000C6 RID: 198
		// (Invoke) Token: 0x060008A4 RID: 2212
		public delegate void SwigDelegateIEncryptedAppTicketListener_1(IntPtr cPtr, int failureReason);

		// Token: 0x020000C7 RID: 199
		public enum FailureReason
		{
			// Token: 0x0400011F RID: 287
			FAILURE_REASON_UNDEFINED,
			// Token: 0x04000120 RID: 288
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
