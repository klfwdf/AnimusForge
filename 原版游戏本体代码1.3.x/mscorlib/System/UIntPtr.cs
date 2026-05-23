using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Security;

namespace System
{
	// Token: 0x02000153 RID: 339
	[CLSCompliant(false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public struct UIntPtr : ISerializable
	{
		// Token: 0x06001541 RID: 5441 RVA: 0x0003E456 File Offset: 0x0003C656
		[SecuritySafeCritical]
		[NonVersionable]
		[__DynamicallyInvokable]
		public UIntPtr(uint value)
		{
			this.m_value = value;
		}

		// Token: 0x06001542 RID: 5442 RVA: 0x0003E460 File Offset: 0x0003C660
		[SecuritySafeCritical]
		[NonVersionable]
		[__DynamicallyInvokable]
		public UIntPtr(ulong value)
		{
			this.m_value = value;
		}

		// Token: 0x06001543 RID: 5443 RVA: 0x0003E46A File Offset: 0x0003C66A
		[SecurityCritical]
		[CLSCompliant(false)]
		[NonVersionable]
		public unsafe UIntPtr(void* value)
		{
			this.m_value = value;
		}

		// Token: 0x06001544 RID: 5444 RVA: 0x0003E474 File Offset: 0x0003C674
		[SecurityCritical]
		private UIntPtr(SerializationInfo info, StreamingContext context)
		{
			ulong @uint = info.GetUInt64("value");
			if (UIntPtr.Size == 4 && @uint > (ulong)(-1))
			{
				throw new ArgumentException(Environment.GetResourceString("Serialization_InvalidPtrValue"));
			}
			this.m_value = @uint;
		}

		// Token: 0x06001545 RID: 5445 RVA: 0x0003E4B2 File Offset: 0x0003C6B2
		[SecurityCritical]
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			info.AddValue("value", this.m_value);
		}

		// Token: 0x06001546 RID: 5446 RVA: 0x0003E4D4 File Offset: 0x0003C6D4
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return obj is UIntPtr && this.m_value == ((UIntPtr)obj).m_value;
		}

		// Token: 0x06001547 RID: 5447 RVA: 0x0003E4F3 File Offset: 0x0003C6F3
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return this.m_value & int.MaxValue;
		}

		// Token: 0x06001548 RID: 5448 RVA: 0x0003E503 File Offset: 0x0003C703
		[SecuritySafeCritical]
		[NonVersionable]
		[__DynamicallyInvokable]
		public uint ToUInt32()
		{
			return this.m_value;
		}

		// Token: 0x06001549 RID: 5449 RVA: 0x0003E50C File Offset: 0x0003C70C
		[SecuritySafeCritical]
		[NonVersionable]
		[__DynamicallyInvokable]
		public ulong ToUInt64()
		{
			return this.m_value;
		}

		// Token: 0x0600154A RID: 5450 RVA: 0x0003E518 File Offset: 0x0003C718
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public override string ToString()
		{
			return this.m_value.ToString(CultureInfo.InvariantCulture);
		}

		// Token: 0x0600154B RID: 5451 RVA: 0x0003E539 File Offset: 0x0003C739
		[NonVersionable]
		public static explicit operator UIntPtr(uint value)
		{
			return new UIntPtr(value);
		}

		// Token: 0x0600154C RID: 5452 RVA: 0x0003E541 File Offset: 0x0003C741
		[NonVersionable]
		public static explicit operator UIntPtr(ulong value)
		{
			return new UIntPtr(value);
		}

		// Token: 0x0600154D RID: 5453 RVA: 0x0003E549 File Offset: 0x0003C749
		[SecuritySafeCritical]
		[NonVersionable]
		public static explicit operator uint(UIntPtr value)
		{
			return value.m_value;
		}

		// Token: 0x0600154E RID: 5454 RVA: 0x0003E553 File Offset: 0x0003C753
		[SecuritySafeCritical]
		[NonVersionable]
		public static explicit operator ulong(UIntPtr value)
		{
			return value.m_value;
		}

		// Token: 0x0600154F RID: 5455 RVA: 0x0003E55D File Offset: 0x0003C75D
		[SecurityCritical]
		[CLSCompliant(false)]
		[NonVersionable]
		public unsafe static explicit operator UIntPtr(void* value)
		{
			return new UIntPtr(value);
		}

		// Token: 0x06001550 RID: 5456 RVA: 0x0003E565 File Offset: 0x0003C765
		[SecurityCritical]
		[CLSCompliant(false)]
		[NonVersionable]
		public unsafe static explicit operator void*(UIntPtr value)
		{
			return value.m_value;
		}

		// Token: 0x06001551 RID: 5457 RVA: 0x0003E56E File Offset: 0x0003C76E
		[SecuritySafeCritical]
		[NonVersionable]
		[__DynamicallyInvokable]
		public static bool operator ==(UIntPtr value1, UIntPtr value2)
		{
			return value1.m_value == value2.m_value;
		}

		// Token: 0x06001552 RID: 5458 RVA: 0x0003E580 File Offset: 0x0003C780
		[SecuritySafeCritical]
		[NonVersionable]
		[__DynamicallyInvokable]
		public static bool operator !=(UIntPtr value1, UIntPtr value2)
		{
			return value1.m_value != value2.m_value;
		}

		// Token: 0x06001553 RID: 5459 RVA: 0x0003E595 File Offset: 0x0003C795
		[NonVersionable]
		public static UIntPtr Add(UIntPtr pointer, int offset)
		{
			return pointer + offset;
		}

		// Token: 0x06001554 RID: 5460 RVA: 0x0003E59E File Offset: 0x0003C79E
		[NonVersionable]
		public static UIntPtr operator +(UIntPtr pointer, int offset)
		{
			return new UIntPtr(pointer.ToUInt64() + (ulong)((long)offset));
		}

		// Token: 0x06001555 RID: 5461 RVA: 0x0003E5AF File Offset: 0x0003C7AF
		[NonVersionable]
		public static UIntPtr Subtract(UIntPtr pointer, int offset)
		{
			return pointer - offset;
		}

		// Token: 0x06001556 RID: 5462 RVA: 0x0003E5B8 File Offset: 0x0003C7B8
		[NonVersionable]
		public static UIntPtr operator -(UIntPtr pointer, int offset)
		{
			return new UIntPtr(pointer.ToUInt64() - (ulong)((long)offset));
		}

		// Token: 0x1700025F RID: 607
		// (get) Token: 0x06001557 RID: 5463 RVA: 0x0003E5C9 File Offset: 0x0003C7C9
		[__DynamicallyInvokable]
		public static int Size
		{
			[NonVersionable]
			[__DynamicallyInvokable]
			get
			{
				return 8;
			}
		}

		// Token: 0x06001558 RID: 5464 RVA: 0x0003E5CC File Offset: 0x0003C7CC
		[SecuritySafeCritical]
		[CLSCompliant(false)]
		[NonVersionable]
		public unsafe void* ToPointer()
		{
			return this.m_value;
		}

		// Token: 0x040006FA RID: 1786
		[SecurityCritical]
		private unsafe void* m_value;

		// Token: 0x040006FB RID: 1787
		public static readonly UIntPtr Zero;
	}
}
