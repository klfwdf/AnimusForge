using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000156 RID: 342
	public abstract class ITelemetryEventSendListener : GalaxyTypeAwareListenerTelemetryEventSend
	{
		// Token: 0x06000C99 RID: 3225 RVA: 0x0000B480 File Offset: 0x00009680
		internal ITelemetryEventSendListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.ITelemetryEventSendListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			ITelemetryEventSendListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000C9A RID: 3226 RVA: 0x0000B4A8 File Offset: 0x000096A8
		public ITelemetryEventSendListener()
			: this(GalaxyInstancePINVOKE.new_ITelemetryEventSendListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000C9B RID: 3227 RVA: 0x0000B4CC File Offset: 0x000096CC
		internal static HandleRef getCPtr(ITelemetryEventSendListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000C9C RID: 3228 RVA: 0x0000B4EC File Offset: 0x000096EC
		~ITelemetryEventSendListener()
		{
			this.Dispose();
		}

		// Token: 0x06000C9D RID: 3229 RVA: 0x0000B51C File Offset: 0x0000971C
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ITelemetryEventSendListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (ITelemetryEventSendListener.listeners.ContainsKey(handle))
					{
						ITelemetryEventSendListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000C9E RID: 3230
		public abstract void OnTelemetryEventSendSuccess(string eventType, uint sentEventIndex);

		// Token: 0x06000C9F RID: 3231
		public abstract void OnTelemetryEventSendFailure(string eventType, uint sentEventIndex, ITelemetryEventSendListener.FailureReason failureReason);

		// Token: 0x06000CA0 RID: 3232 RVA: 0x0000B5CC File Offset: 0x000097CC
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnTelemetryEventSendSuccess", ITelemetryEventSendListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new ITelemetryEventSendListener.SwigDelegateITelemetryEventSendListener_0(ITelemetryEventSendListener.SwigDirectorOnTelemetryEventSendSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnTelemetryEventSendFailure", ITelemetryEventSendListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new ITelemetryEventSendListener.SwigDelegateITelemetryEventSendListener_1(ITelemetryEventSendListener.SwigDirectorOnTelemetryEventSendFailure);
			}
			GalaxyInstancePINVOKE.ITelemetryEventSendListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x06000CA1 RID: 3233 RVA: 0x0000B640 File Offset: 0x00009840
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(ITelemetryEventSendListener));
		}

		// Token: 0x06000CA2 RID: 3234 RVA: 0x0000B676 File Offset: 0x00009876
		[MonoPInvokeCallback(typeof(ITelemetryEventSendListener.SwigDelegateITelemetryEventSendListener_0))]
		private static void SwigDirectorOnTelemetryEventSendSuccess(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string eventType, uint sentEventIndex)
		{
			if (ITelemetryEventSendListener.listeners.ContainsKey(cPtr))
			{
				ITelemetryEventSendListener.listeners[cPtr].OnTelemetryEventSendSuccess(eventType, sentEventIndex);
			}
		}

		// Token: 0x06000CA3 RID: 3235 RVA: 0x0000B69A File Offset: 0x0000989A
		[MonoPInvokeCallback(typeof(ITelemetryEventSendListener.SwigDelegateITelemetryEventSendListener_1))]
		private static void SwigDirectorOnTelemetryEventSendFailure(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string eventType, uint sentEventIndex, int failureReason)
		{
			if (ITelemetryEventSendListener.listeners.ContainsKey(cPtr))
			{
				ITelemetryEventSendListener.listeners[cPtr].OnTelemetryEventSendFailure(eventType, sentEventIndex, (ITelemetryEventSendListener.FailureReason)failureReason);
			}
		}

		// Token: 0x0400026E RID: 622
		private static Dictionary<IntPtr, ITelemetryEventSendListener> listeners = new Dictionary<IntPtr, ITelemetryEventSendListener>();

		// Token: 0x0400026F RID: 623
		private HandleRef swigCPtr;

		// Token: 0x04000270 RID: 624
		private ITelemetryEventSendListener.SwigDelegateITelemetryEventSendListener_0 swigDelegate0;

		// Token: 0x04000271 RID: 625
		private ITelemetryEventSendListener.SwigDelegateITelemetryEventSendListener_1 swigDelegate1;

		// Token: 0x04000272 RID: 626
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(string),
			typeof(uint)
		};

		// Token: 0x04000273 RID: 627
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(string),
			typeof(uint),
			typeof(ITelemetryEventSendListener.FailureReason)
		};

		// Token: 0x02000157 RID: 343
		// (Invoke) Token: 0x06000CA6 RID: 3238
		public delegate void SwigDelegateITelemetryEventSendListener_0(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string eventType, uint sentEventIndex);

		// Token: 0x02000158 RID: 344
		// (Invoke) Token: 0x06000CAA RID: 3242
		public delegate void SwigDelegateITelemetryEventSendListener_1(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string eventType, uint sentEventIndex, int failureReason);

		// Token: 0x02000159 RID: 345
		public enum FailureReason
		{
			// Token: 0x04000275 RID: 629
			FAILURE_REASON_UNDEFINED,
			// Token: 0x04000276 RID: 630
			FAILURE_REASON_CLIENT_FORBIDDEN,
			// Token: 0x04000277 RID: 631
			FAILURE_REASON_INVALID_DATA,
			// Token: 0x04000278 RID: 632
			FAILURE_REASON_CONNECTION_FAILURE,
			// Token: 0x04000279 RID: 633
			FAILURE_REASON_NO_SAMPLING_CLASS_IN_CONFIG,
			// Token: 0x0400027A RID: 634
			FAILURE_REASON_SAMPLING_CLASS_FIELD_MISSING,
			// Token: 0x0400027B RID: 635
			FAILURE_REASON_EVENT_SAMPLED_OUT
		}
	}
}
