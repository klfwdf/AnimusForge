using System;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Security;

namespace System
{
	// Token: 0x020000FD RID: 253
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public struct IntPtr : ISerializable
	{
		// Token: 0x06000F89 RID: 3977 RVA: 0x0002F8AE File Offset: 0x0002DAAE
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal bool IsNull()
		{
			return this.m_value == null;
		}

		// Token: 0x06000F8A RID: 3978 RVA: 0x0002F8BA File Offset: 0x0002DABA
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public IntPtr(int value)
		{
			this.m_value = value;
		}

		// Token: 0x06000F8B RID: 3979 RVA: 0x0002F8C5 File Offset: 0x0002DAC5
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public IntPtr(long value)
		{
			this.m_value = value;
		}

		// Token: 0x06000F8C RID: 3980 RVA: 0x0002F8CF File Offset: 0x0002DACF
		[SecurityCritical]
		[CLSCompliant(false)]
		[ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
		[NonVersionable]
		public unsafe IntPtr(void* value)
		{
			this.m_value = value;
		}

		// Token: 0x06000F8D RID: 3981 RVA: 0x0002F8D8 File Offset: 0x0002DAD8
		[SecurityCritical]
		private IntPtr(SerializationInfo info, StreamingContext context)
		{
			long @int = info.GetInt64("value");
			if (IntPtr.Size == 4 && (@int > 2147483647L || @int < -2147483648L))
			{
				throw new ArgumentException(Environment.GetResourceString("Serialization_InvalidPtrValue"));
			}
			this.m_value = @int;
		}

		// Token: 0x06000F8E RID: 3982 RVA: 0x0002F923 File Offset: 0x0002DB23
		[SecurityCritical]
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			info.AddValue("value", this.m_value);
		}

		// Token: 0x06000F8F RID: 3983 RVA: 0x0002F945 File Offset: 0x0002DB45
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return obj is IntPtr && this.m_value == ((IntPtr)obj).m_value;
		}

		// Token: 0x06000F90 RID: 3984 RVA: 0x0002F964 File Offset: 0x0002DB64
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return this.m_value;
		}

		// Token: 0x06000F91 RID: 3985 RVA: 0x0002F970 File Offset: 0x0002DB70
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public int ToInt32()
		{
			long num = this.m_value;
			return checked((int)num);
		}

		// Token: 0x06000F92 RID: 3986 RVA: 0x0002F987 File Offset: 0x0002DB87
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public long ToInt64()
		{
			return this.m_value;
		}

		// Token: 0x06000F93 RID: 3987 RVA: 0x0002F990 File Offset: 0x0002DB90
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public override string ToString()
		{
			return this.m_value.ToString(CultureInfo.InvariantCulture);
		}

		// Token: 0x06000F94 RID: 3988 RVA: 0x0002F9B4 File Offset: 0x0002DBB4
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public string ToString(string format)
		{
			return this.m_value.ToString(format, CultureInfo.InvariantCulture);
		}

		// Token: 0x06000F95 RID: 3989 RVA: 0x0002F9D6 File Offset: 0x0002DBD6
		[ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
		[NonVersionable]
		public static explicit operator IntPtr(int value)
		{
			return new IntPtr(value);
		}

		// Token: 0x06000F96 RID: 3990 RVA: 0x0002F9DE File Offset: 0x0002DBDE
		[ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
		[NonVersionable]
		public static explicit operator IntPtr(long value)
		{
			return new IntPtr(value);
		}

		// Token: 0x06000F97 RID: 3991 RVA: 0x0002F9E6 File Offset: 0x0002DBE6
		[SecurityCritical]
		[CLSCompliant(false)]
		[ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
		[NonVersionable]
		public unsafe static explicit operator IntPtr(void* value)
		{
			return new IntPtr(value);
		}

		// Token: 0x06000F98 RID: 3992 RVA: 0x0002F9EE File Offset: 0x0002DBEE
		[SecuritySafeCritical]
		[CLSCompliant(false)]
		[NonVersionable]
		public unsafe static explicit operator void*(IntPtr value)
		{
			return value.m_value;
		}

		// Token: 0x06000F99 RID: 3993 RVA: 0x0002F9F8 File Offset: 0x0002DBF8
		[SecuritySafeCritical]
		[NonVersionable]
		public static explicit operator int(IntPtr value)
		{
			long num = value.m_value;
			return checked((int)num);
		}

		// Token: 0x06000F9A RID: 3994 RVA: 0x0002FA10 File Offset: 0x0002DC10
		[SecuritySafeCritical]
		[NonVersionable]
		public static explicit operator long(IntPtr value)
		{
			return value.m_value;
		}

		// Token: 0x06000F9B RID: 3995 RVA: 0x0002FA1A File Offset: 0x0002DC1A
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public static bool operator ==(IntPtr value1, IntPtr value2)
		{
			return value1.m_value == value2.m_value;
		}

		// Token: 0x06000F9C RID: 3996 RVA: 0x0002FA2C File Offset: 0x0002DC2C
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public static bool operator !=(IntPtr value1, IntPtr value2)
		{
			return value1.m_value != value2.m_value;
		}

		// Token: 0x06000F9D RID: 3997 RVA: 0x0002FA41 File Offset: 0x0002DC41
		[ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
		[NonVersionable]
		public static IntPtr Add(IntPtr pointer, int offset)
		{
			return pointer + offset;
		}

		// Token: 0x06000F9E RID: 3998 RVA: 0x0002FA4A File Offset: 0x0002DC4A
		[ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
		[NonVersionable]
		public static IntPtr operator +(IntPtr pointer, int offset)
		{
			return new IntPtr(pointer.ToInt64() + (long)offset);
		}

		// Token: 0x06000F9F RID: 3999 RVA: 0x0002FA5B File Offset: 0x0002DC5B
		[ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
		[NonVersionable]
		public static IntPtr Subtract(IntPtr pointer, int offset)
		{
			return pointer - offset;
		}

		// Token: 0x06000FA0 RID: 4000 RVA: 0x0002FA64 File Offset: 0x0002DC64
		[ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
		[NonVersionable]
		public static IntPtr operator -(IntPtr pointer, int offset)
		{
			return new IntPtr(pointer.ToInt64() - (long)offset);
		}

		// Token: 0x170001B2 RID: 434
		// (get) Token: 0x06000FA1 RID: 4001 RVA: 0x0002FA75 File Offset: 0x0002DC75
		[__DynamicallyInvokable]
		public static int Size
		{
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			[NonVersionable]
			[__DynamicallyInvokable]
			get
			{
				return 8;
			}
		}

		// Token: 0x06000FA2 RID: 4002 RVA: 0x0002FA78 File Offset: 0x0002DC78
		[SecuritySafeCritical]
		[CLSCompliant(false)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[NonVersionable]
		public unsafe void* ToPointer()
		{
			return this.m_value;
		}

		// Token: 0x040005A8 RID: 1448
		[SecurityCritical]
		private unsafe void* m_value;

		// Token: 0x040005A9 RID: 1449
		public static readonly IntPtr Zero;
	}
}
