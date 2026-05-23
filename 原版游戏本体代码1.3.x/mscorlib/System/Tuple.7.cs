using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System
{
	// Token: 0x02000065 RID: 101
	[__DynamicallyInvokable]
	[Serializable]
	public class Tuple<T1, T2, T3, T4, T5, T6> : IStructuralEquatable, IStructuralComparable, IComparable, ITupleInternal, ITuple
	{
		// Token: 0x17000057 RID: 87
		// (get) Token: 0x060003A4 RID: 932 RVA: 0x000093FA File Offset: 0x000075FA
		[__DynamicallyInvokable]
		public T1 Item1
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_Item1;
			}
		}

		// Token: 0x17000058 RID: 88
		// (get) Token: 0x060003A5 RID: 933 RVA: 0x00009402 File Offset: 0x00007602
		[__DynamicallyInvokable]
		public T2 Item2
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_Item2;
			}
		}

		// Token: 0x17000059 RID: 89
		// (get) Token: 0x060003A6 RID: 934 RVA: 0x0000940A File Offset: 0x0000760A
		[__DynamicallyInvokable]
		public T3 Item3
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_Item3;
			}
		}

		// Token: 0x1700005A RID: 90
		// (get) Token: 0x060003A7 RID: 935 RVA: 0x00009412 File Offset: 0x00007612
		[__DynamicallyInvokable]
		public T4 Item4
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_Item4;
			}
		}

		// Token: 0x1700005B RID: 91
		// (get) Token: 0x060003A8 RID: 936 RVA: 0x0000941A File Offset: 0x0000761A
		[__DynamicallyInvokable]
		public T5 Item5
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_Item5;
			}
		}

		// Token: 0x1700005C RID: 92
		// (get) Token: 0x060003A9 RID: 937 RVA: 0x00009422 File Offset: 0x00007622
		[__DynamicallyInvokable]
		public T6 Item6
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_Item6;
			}
		}

		// Token: 0x060003AA RID: 938 RVA: 0x0000942A File Offset: 0x0000762A
		[__DynamicallyInvokable]
		public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
		{
			this.m_Item1 = item1;
			this.m_Item2 = item2;
			this.m_Item3 = item3;
			this.m_Item4 = item4;
			this.m_Item5 = item5;
			this.m_Item6 = item6;
		}

		// Token: 0x060003AB RID: 939 RVA: 0x0000945F File Offset: 0x0000765F
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return ((IStructuralEquatable)this).Equals(obj, EqualityComparer<object>.Default);
		}

		// Token: 0x060003AC RID: 940 RVA: 0x00009470 File Offset: 0x00007670
		[__DynamicallyInvokable]
		bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
		{
			if (other == null)
			{
				return false;
			}
			Tuple<T1, T2, T3, T4, T5, T6> tuple = other as Tuple<T1, T2, T3, T4, T5, T6>;
			return tuple != null && (comparer.Equals(this.m_Item1, tuple.m_Item1) && comparer.Equals(this.m_Item2, tuple.m_Item2) && comparer.Equals(this.m_Item3, tuple.m_Item3) && comparer.Equals(this.m_Item4, tuple.m_Item4) && comparer.Equals(this.m_Item5, tuple.m_Item5)) && comparer.Equals(this.m_Item6, tuple.m_Item6);
		}

		// Token: 0x060003AD RID: 941 RVA: 0x00009545 File Offset: 0x00007745
		[__DynamicallyInvokable]
		int IComparable.CompareTo(object obj)
		{
			return ((IStructuralComparable)this).CompareTo(obj, Comparer<object>.Default);
		}

		// Token: 0x060003AE RID: 942 RVA: 0x00009554 File Offset: 0x00007754
		[__DynamicallyInvokable]
		int IStructuralComparable.CompareTo(object other, IComparer comparer)
		{
			if (other == null)
			{
				return 1;
			}
			Tuple<T1, T2, T3, T4, T5, T6> tuple = other as Tuple<T1, T2, T3, T4, T5, T6>;
			if (tuple == null)
			{
				throw new ArgumentException(Environment.GetResourceString("ArgumentException_TupleIncorrectType", new object[] { base.GetType().ToString() }), "other");
			}
			int num = comparer.Compare(this.m_Item1, tuple.m_Item1);
			if (num != 0)
			{
				return num;
			}
			num = comparer.Compare(this.m_Item2, tuple.m_Item2);
			if (num != 0)
			{
				return num;
			}
			num = comparer.Compare(this.m_Item3, tuple.m_Item3);
			if (num != 0)
			{
				return num;
			}
			num = comparer.Compare(this.m_Item4, tuple.m_Item4);
			if (num != 0)
			{
				return num;
			}
			num = comparer.Compare(this.m_Item5, tuple.m_Item5);
			if (num != 0)
			{
				return num;
			}
			return comparer.Compare(this.m_Item6, tuple.m_Item6);
		}

		// Token: 0x060003AF RID: 943 RVA: 0x00009661 File Offset: 0x00007861
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return ((IStructuralEquatable)this).GetHashCode(EqualityComparer<object>.Default);
		}

		// Token: 0x060003B0 RID: 944 RVA: 0x00009670 File Offset: 0x00007870
		[__DynamicallyInvokable]
		int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
		{
			return Tuple.CombineHashCodes(comparer.GetHashCode(this.m_Item1), comparer.GetHashCode(this.m_Item2), comparer.GetHashCode(this.m_Item3), comparer.GetHashCode(this.m_Item4), comparer.GetHashCode(this.m_Item5), comparer.GetHashCode(this.m_Item6));
		}

		// Token: 0x060003B1 RID: 945 RVA: 0x000096E8 File Offset: 0x000078E8
		int ITupleInternal.GetHashCode(IEqualityComparer comparer)
		{
			return ((IStructuralEquatable)this).GetHashCode(comparer);
		}

		// Token: 0x060003B2 RID: 946 RVA: 0x000096F4 File Offset: 0x000078F4
		[__DynamicallyInvokable]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("(");
			return ((ITupleInternal)this).ToString(stringBuilder);
		}

		// Token: 0x060003B3 RID: 947 RVA: 0x0000971C File Offset: 0x0000791C
		string ITupleInternal.ToString(StringBuilder sb)
		{
			sb.Append(this.m_Item1);
			sb.Append(", ");
			sb.Append(this.m_Item2);
			sb.Append(", ");
			sb.Append(this.m_Item3);
			sb.Append(", ");
			sb.Append(this.m_Item4);
			sb.Append(", ");
			sb.Append(this.m_Item5);
			sb.Append(", ");
			sb.Append(this.m_Item6);
			sb.Append(")");
			return sb.ToString();
		}

		// Token: 0x1700005D RID: 93
		// (get) Token: 0x060003B4 RID: 948 RVA: 0x000097E3 File Offset: 0x000079E3
		int ITuple.Length
		{
			get
			{
				return 6;
			}
		}

		// Token: 0x1700005E RID: 94
		object ITuple.this[int index]
		{
			get
			{
				switch (index)
				{
				case 0:
					return this.Item1;
				case 1:
					return this.Item2;
				case 2:
					return this.Item3;
				case 3:
					return this.Item4;
				case 4:
					return this.Item5;
				case 5:
					return this.Item6;
				default:
					throw new IndexOutOfRangeException();
				}
			}
		}

		// Token: 0x0400024A RID: 586
		private readonly T1 m_Item1;

		// Token: 0x0400024B RID: 587
		private readonly T2 m_Item2;

		// Token: 0x0400024C RID: 588
		private readonly T3 m_Item3;

		// Token: 0x0400024D RID: 589
		private readonly T4 m_Item4;

		// Token: 0x0400024E RID: 590
		private readonly T5 m_Item5;

		// Token: 0x0400024F RID: 591
		private readonly T6 m_Item6;
	}
}
