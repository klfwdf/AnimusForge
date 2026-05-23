using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Galaxy.Api
{
	// Token: 0x02000155 RID: 341
	public class ITelemetry : IDisposable
	{
		// Token: 0x06000C85 RID: 3205 RVA: 0x00019CF3 File Offset: 0x00017EF3
		internal ITelemetry(IntPtr cPtr, bool cMemoryOwn)
		{
			this.swigCMemOwn = cMemoryOwn;
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000C86 RID: 3206 RVA: 0x00019D0F File Offset: 0x00017F0F
		internal static HandleRef getCPtr(ITelemetry obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000C87 RID: 3207 RVA: 0x00019D30 File Offset: 0x00017F30
		~ITelemetry()
		{
			this.Dispose();
		}

		// Token: 0x06000C88 RID: 3208 RVA: 0x00019D60 File Offset: 0x00017F60
		public virtual void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_ITelemetry(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x06000C89 RID: 3209 RVA: 0x00019DE0 File Offset: 0x00017FE0
		public virtual void AddStringParam(string name, string value)
		{
			GalaxyInstancePINVOKE.ITelemetry_AddStringParam(this.swigCPtr, name, value);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C8A RID: 3210 RVA: 0x00019DFF File Offset: 0x00017FFF
		public virtual void AddIntParam(string name, int value)
		{
			GalaxyInstancePINVOKE.ITelemetry_AddIntParam(this.swigCPtr, name, value);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C8B RID: 3211 RVA: 0x00019E1E File Offset: 0x0001801E
		public virtual void AddFloatParam(string name, double value)
		{
			GalaxyInstancePINVOKE.ITelemetry_AddFloatParam(this.swigCPtr, name, value);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C8C RID: 3212 RVA: 0x00019E3D File Offset: 0x0001803D
		public virtual void AddBoolParam(string name, bool value)
		{
			GalaxyInstancePINVOKE.ITelemetry_AddBoolParam(this.swigCPtr, name, value);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C8D RID: 3213 RVA: 0x00019E5C File Offset: 0x0001805C
		public virtual void AddObjectParam(string name)
		{
			GalaxyInstancePINVOKE.ITelemetry_AddObjectParam(this.swigCPtr, name);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C8E RID: 3214 RVA: 0x00019E7A File Offset: 0x0001807A
		public virtual void AddArrayParam(string name)
		{
			GalaxyInstancePINVOKE.ITelemetry_AddArrayParam(this.swigCPtr, name);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C8F RID: 3215 RVA: 0x00019E98 File Offset: 0x00018098
		public virtual void CloseParam()
		{
			GalaxyInstancePINVOKE.ITelemetry_CloseParam(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C90 RID: 3216 RVA: 0x00019EB5 File Offset: 0x000180B5
		public virtual void ClearParams()
		{
			GalaxyInstancePINVOKE.ITelemetry_ClearParams(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C91 RID: 3217 RVA: 0x00019ED2 File Offset: 0x000180D2
		public virtual void SetSamplingClass(string name)
		{
			GalaxyInstancePINVOKE.ITelemetry_SetSamplingClass(this.swigCPtr, name);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C92 RID: 3218 RVA: 0x00019EF0 File Offset: 0x000180F0
		public virtual uint SendTelemetryEvent(string eventType, ITelemetryEventSendListener listener)
		{
			uint result = GalaxyInstancePINVOKE.ITelemetry_SendTelemetryEvent__SWIG_0(this.swigCPtr, eventType, ITelemetryEventSendListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C93 RID: 3219 RVA: 0x00019F24 File Offset: 0x00018124
		public virtual uint SendTelemetryEvent(string eventType)
		{
			uint result = GalaxyInstancePINVOKE.ITelemetry_SendTelemetryEvent__SWIG_1(this.swigCPtr, eventType);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C94 RID: 3220 RVA: 0x00019F50 File Offset: 0x00018150
		public virtual uint SendAnonymousTelemetryEvent(string eventType, ITelemetryEventSendListener listener)
		{
			uint result = GalaxyInstancePINVOKE.ITelemetry_SendAnonymousTelemetryEvent__SWIG_0(this.swigCPtr, eventType, ITelemetryEventSendListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C95 RID: 3221 RVA: 0x00019F84 File Offset: 0x00018184
		public virtual uint SendAnonymousTelemetryEvent(string eventType)
		{
			uint result = GalaxyInstancePINVOKE.ITelemetry_SendAnonymousTelemetryEvent__SWIG_1(this.swigCPtr, eventType);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C96 RID: 3222 RVA: 0x00019FB0 File Offset: 0x000181B0
		public virtual string GetVisitID()
		{
			string result = GalaxyInstancePINVOKE.ITelemetry_GetVisitID(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C97 RID: 3223 RVA: 0x00019FDC File Offset: 0x000181DC
		public virtual void GetVisitIDCopy(out string buffer, uint bufferLength)
		{
			byte[] array = new byte[bufferLength];
			try
			{
				GalaxyInstancePINVOKE.ITelemetry_GetVisitIDCopy(this.swigCPtr, array, bufferLength);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
			}
			finally
			{
				buffer = Encoding.UTF8.GetString(array);
			}
		}

		// Token: 0x06000C98 RID: 3224 RVA: 0x0001A034 File Offset: 0x00018234
		public virtual void ResetVisitID()
		{
			GalaxyInstancePINVOKE.ITelemetry_ResetVisitID(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0400026C RID: 620
		private HandleRef swigCPtr;

		// Token: 0x0400026D RID: 621
		protected bool swigCMemOwn;
	}
}
