using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000005 RID: 5
	public class GalaxyID : IDisposable
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		internal GalaxyID(IntPtr cPtr, bool cMemoryOwn)
		{
			this.swigCMemOwn = cMemoryOwn;
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000002 RID: 2 RVA: 0x0000206C File Offset: 0x0000026C
		public GalaxyID()
			: this(GalaxyInstancePINVOKE.new_GalaxyID__SWIG_0(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000003 RID: 3 RVA: 0x0000208A File Offset: 0x0000028A
		public GalaxyID(ulong _value)
			: this(GalaxyInstancePINVOKE.new_GalaxyID__SWIG_1(_value), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000004 RID: 4 RVA: 0x000020A9 File Offset: 0x000002A9
		public GalaxyID(GalaxyID galaxyID)
			: this(GalaxyInstancePINVOKE.new_GalaxyID__SWIG_2(GalaxyID.getCPtr(galaxyID)), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000005 RID: 5 RVA: 0x000020CD File Offset: 0x000002CD
		internal static HandleRef getCPtr(GalaxyID obj)
		{
			return (!(obj == null)) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000006 RID: 6 RVA: 0x000020F4 File Offset: 0x000002F4
		~GalaxyID()
		{
			this.Dispose();
		}

		// Token: 0x06000007 RID: 7 RVA: 0x00002124 File Offset: 0x00000324
		public virtual void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_GalaxyID(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x06000008 RID: 8 RVA: 0x000021A4 File Offset: 0x000003A4
		public static bool operator ==(GalaxyID other1, GalaxyID other2)
		{
			return (other1 == null && other2 == null) || (other1 != null && other2 != null && other1.operator_equals(other2));
		}

		// Token: 0x06000009 RID: 9 RVA: 0x000021C9 File Offset: 0x000003C9
		public static bool operator !=(GalaxyID other1, GalaxyID other2)
		{
			return (other1 != null || other2 != null) && (other1 == null || other2 == null || other1.operator_not_equals(other2));
		}

		// Token: 0x0600000A RID: 10 RVA: 0x000021EE File Offset: 0x000003EE
		public static bool operator <(GalaxyID other1, GalaxyID other2)
		{
			return other1.operator_less(other2);
		}

		// Token: 0x0600000B RID: 11 RVA: 0x000021F7 File Offset: 0x000003F7
		public static bool operator >(GalaxyID other1, GalaxyID other2)
		{
			return !other1.operator_less(other2) && !other1.operator_equals(other2);
		}

		// Token: 0x0600000C RID: 12 RVA: 0x00002212 File Offset: 0x00000412
		public override int GetHashCode()
		{
			return (int)this.ToUint64();
		}

		// Token: 0x0600000D RID: 13 RVA: 0x0000221C File Offset: 0x0000041C
		public override bool Equals(object obj)
		{
			GalaxyID galaxyID = obj as GalaxyID;
			return !(galaxyID == null) && this.operator_equals(galaxyID);
		}

		// Token: 0x0600000E RID: 14 RVA: 0x00002248 File Offset: 0x00000448
		public override string ToString()
		{
			return this.ToUint64().ToString();
		}

		// Token: 0x0600000F RID: 15 RVA: 0x0000226C File Offset: 0x0000046C
		public static GalaxyID FromRealID(GalaxyID.IDType type, ulong value)
		{
			GalaxyID result = new GalaxyID(GalaxyInstancePINVOKE.GalaxyID_FromRealID((int)type, value), true);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000010 RID: 16 RVA: 0x00002298 File Offset: 0x00000498
		private GalaxyID operator_assign(GalaxyID other)
		{
			GalaxyID result = new GalaxyID(GalaxyInstancePINVOKE.GalaxyID_operator_assign(this.swigCPtr, GalaxyID.getCPtr(other)), false);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000011 RID: 17 RVA: 0x000022D0 File Offset: 0x000004D0
		private bool operator_less(GalaxyID other)
		{
			bool result = GalaxyInstancePINVOKE.GalaxyID_operator_less(this.swigCPtr, GalaxyID.getCPtr(other));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00002300 File Offset: 0x00000500
		private bool operator_equals(GalaxyID other)
		{
			bool result = GalaxyInstancePINVOKE.GalaxyID_operator_equals(this.swigCPtr, GalaxyID.getCPtr(other));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000013 RID: 19 RVA: 0x00002330 File Offset: 0x00000530
		private bool operator_not_equals(GalaxyID other)
		{
			bool result = GalaxyInstancePINVOKE.GalaxyID_operator_not_equals(this.swigCPtr, GalaxyID.getCPtr(other));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000014 RID: 20 RVA: 0x00002360 File Offset: 0x00000560
		public bool IsValid()
		{
			bool result = GalaxyInstancePINVOKE.GalaxyID_IsValid(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000015 RID: 21 RVA: 0x0000238C File Offset: 0x0000058C
		public ulong ToUint64()
		{
			ulong result = GalaxyInstancePINVOKE.GalaxyID_ToUint64(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000016 RID: 22 RVA: 0x000023B8 File Offset: 0x000005B8
		public ulong GetRealID()
		{
			ulong result = GalaxyInstancePINVOKE.GalaxyID_GetRealID(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000017 RID: 23 RVA: 0x000023E4 File Offset: 0x000005E4
		public GalaxyID.IDType GetIDType()
		{
			GalaxyID.IDType result = (GalaxyID.IDType)GalaxyInstancePINVOKE.GalaxyID_GetIDType(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400000E RID: 14
		private HandleRef swigCPtr;

		// Token: 0x0400000F RID: 15
		protected bool swigCMemOwn;

		// Token: 0x04000010 RID: 16
		public static readonly ulong UNASSIGNED_VALUE = GalaxyInstancePINVOKE.GalaxyID_UNASSIGNED_VALUE_get();

		// Token: 0x02000006 RID: 6
		public enum IDType
		{
			// Token: 0x04000012 RID: 18
			ID_TYPE_UNASSIGNED,
			// Token: 0x04000013 RID: 19
			ID_TYPE_LOBBY,
			// Token: 0x04000014 RID: 20
			ID_TYPE_USER
		}
	}
}
