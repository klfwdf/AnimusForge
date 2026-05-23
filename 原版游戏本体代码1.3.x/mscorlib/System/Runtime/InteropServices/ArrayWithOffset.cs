using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200090E RID: 2318
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public struct ArrayWithOffset
	{
		// Token: 0x06005FE5 RID: 24549 RVA: 0x0014B610 File Offset: 0x00149810
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public ArrayWithOffset(object array, int offset)
		{
			this.m_array = array;
			this.m_offset = offset;
			this.m_count = 0;
			this.m_count = this.CalculateCount();
		}

		// Token: 0x06005FE6 RID: 24550 RVA: 0x0014B633 File Offset: 0x00149833
		[__DynamicallyInvokable]
		public object GetArray()
		{
			return this.m_array;
		}

		// Token: 0x06005FE7 RID: 24551 RVA: 0x0014B63B File Offset: 0x0014983B
		[__DynamicallyInvokable]
		public int GetOffset()
		{
			return this.m_offset;
		}

		// Token: 0x06005FE8 RID: 24552 RVA: 0x0014B643 File Offset: 0x00149843
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return this.m_count + this.m_offset;
		}

		// Token: 0x06005FE9 RID: 24553 RVA: 0x0014B652 File Offset: 0x00149852
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return obj is ArrayWithOffset && this.Equals((ArrayWithOffset)obj);
		}

		// Token: 0x06005FEA RID: 24554 RVA: 0x0014B66A File Offset: 0x0014986A
		[__DynamicallyInvokable]
		public bool Equals(ArrayWithOffset obj)
		{
			return obj.m_array == this.m_array && obj.m_offset == this.m_offset && obj.m_count == this.m_count;
		}

		// Token: 0x06005FEB RID: 24555 RVA: 0x0014B698 File Offset: 0x00149898
		[__DynamicallyInvokable]
		public static bool operator ==(ArrayWithOffset a, ArrayWithOffset b)
		{
			return a.Equals(b);
		}

		// Token: 0x06005FEC RID: 24556 RVA: 0x0014B6A2 File Offset: 0x001498A2
		[__DynamicallyInvokable]
		public static bool operator !=(ArrayWithOffset a, ArrayWithOffset b)
		{
			return !(a == b);
		}

		// Token: 0x06005FED RID: 24557
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern int CalculateCount();

		// Token: 0x04002A5B RID: 10843
		private object m_array;

		// Token: 0x04002A5C RID: 10844
		private int m_offset;

		// Token: 0x04002A5D RID: 10845
		private int m_count;
	}
}
