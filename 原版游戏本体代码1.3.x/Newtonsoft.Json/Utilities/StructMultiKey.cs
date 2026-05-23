using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Utilities
{
	// Token: 0x0200006E RID: 110
	[NullableContext(1)]
	[Nullable(0)]
	internal readonly struct StructMultiKey<[Nullable(2)] T1, [Nullable(2)] T2> : IEquatable<StructMultiKey<T1, T2>>
	{
		// Token: 0x060005F8 RID: 1528 RVA: 0x000198C1 File Offset: 0x00017AC1
		public StructMultiKey(T1 v1, T2 v2)
		{
			this.Value1 = v1;
			this.Value2 = v2;
		}

		// Token: 0x060005F9 RID: 1529 RVA: 0x000198D4 File Offset: 0x00017AD4
		public override int GetHashCode()
		{
			T1 value = this.Value1;
			int num = ((value != null) ? value.GetHashCode() : 0);
			T2 value2 = this.Value2;
			return num ^ ((value2 != null) ? value2.GetHashCode() : 0);
		}

		// Token: 0x060005FA RID: 1530 RVA: 0x0001992C File Offset: 0x00017B2C
		[NullableContext(2)]
		public override bool Equals(object obj)
		{
			if (obj is StructMultiKey<T1, T2>)
			{
				StructMultiKey<T1, T2> other = (StructMultiKey<T1, T2>)obj;
				return this.Equals(other);
			}
			return false;
		}

		// Token: 0x060005FB RID: 1531 RVA: 0x00019953 File Offset: 0x00017B53
		public bool Equals([Nullable(new byte[] { 0, 1, 1 })] StructMultiKey<T1, T2> other)
		{
			return object.Equals(this.Value1, other.Value1) && object.Equals(this.Value2, other.Value2);
		}

		// Token: 0x04000226 RID: 550
		public readonly T1 Value1;

		// Token: 0x04000227 RID: 551
		public readonly T2 Value2;
	}
}
