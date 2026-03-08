using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System
{
	// Token: 0x02000066 RID: 102
	[__DynamicallyInvokable]
	[Serializable]
	public class Tuple<T1, T2, T3, T4, T5, T6, T7> : IStructuralEquatable, IStructuralComparable, IComparable, ITupleInternal, ITuple
	{
		// Token: 0x1700005F RID: 95
		// (get) Token: 0x060003B6 RID: 950 RVA: 0x00009862 File Offset: 0x00007A62
		[__DynamicallyInvokable]
		public T1 Item1
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_Item1;
			}
		}

		// Token: 0x17000060 RID: 96
		// (get) Token: 0x060003B7 RID: 951 RVA: 0x0000986A File Offset: 0x00007A6A
		[__DynamicallyInvokable]
		public T2 Item2
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_Item2;
			}
		}

		// Token: 0x17000061 RID: 97
		// (get) Token: 0x060003B8 RID: 952 RVA: 0x00009872 File Offset: 0x00007A72
		[__DynamicallyInvokable]
		public T3 Item3
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_Item3;
			}
		}

		// Token: 0x17000062 RID: 98
		// (get) Token: 0x060003B9 RID: 953 RVA: 0x0000987A File Offset: 0x00007A7A
		[__DynamicallyInvokable]
		public T4 Item4
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_Item4;
			}
		}

		// Token: 0x17000063 RID: 99
		// (get) Token: 0x060003BA RID: 954 RVA: 0x00009882 File Offset: 0x00007A82
		[__DynamicallyInvokable]
		public T5 Item5
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_Item5;
			}
		}

		// Token: 0x17000064 RID: 100
		// (get) Token: 0x060003BB RID: 955 RVA: 0x0000988A File Offset: 0x00007A8A
		[__DynamicallyInvokable]
		public T6 Item6
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_Item6;
			}
		}

		// Token: 0x17000065 RID: 101
		// (get) Token: 0x060003BC RID: 956 RVA: 0x00009892 File Offset: 0x00007A92
		[__DynamicallyInvokable]
		public T7 Item7
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_Item7;
			}
		}

		// Token: 0x060003BD RID: 957 RVA: 0x0000989A File Offset: 0x00007A9A
		[__DynamicallyInvokable]
		public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
		{
			this.m_Item1 = item1;
			this.m_Item2 = item2;
			this.m_Item3 = item3;
			this.m_Item4 = item4;
			this.m_Item5 = item5;
			this.m_Item6 = item6;
			this.m_Item7 = item7;
		}

		// Token: 0x060003BE RID: 958 RVA: 0x000098D7 File Offset: 0x00007AD7
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return ((IStructuralEquatable)this).Equals(obj, EqualityComparer<object>.Default);
		}

		// Token: 0x060003BF RID: 959 RVA: 0x000098E8 File Offset: 0x00007AE8
		[__DynamicallyInvokable]
		bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
		{
			if (other == null)
			{
				return false;
			}
			Tuple<T1, T2, T3, T4, T5, T6, T7> tuple = other as Tuple<T1, T2, T3, T4, T5, T6, T7>;
			return tuple != null && (comparer.Equals(this.m_Item1, tuple.m_Item1) && comparer.Equals(this.m_Item2, tuple.m_Item2) && comparer.Equals(this.m_Item3, tuple.m_Item3) && comparer.Equals(this.m_Item4, tuple.m_Item4) && comparer.Equals(this.m_Item5, tuple.m_Item5) && comparer.Equals(this.m_Item6, tuple.m_Item6)) && comparer.Equals(this.m_Item7, tuple.m_Item7);
		}

		// Token: 0x060003C0 RID: 960 RVA: 0x000099DE File Offset: 0x00007BDE
		[__DynamicallyInvokable]
		int IComparable.CompareTo(object obj)
		{
			return ((IStructuralComparable)this).CompareTo(obj, Comparer<object>.Default);
		}

		// Token: 0x060003C1 RID: 961 RVA: 0x000099EC File Offset: 0x00007BEC
		[__DynamicallyInvokable]
		int IStructuralComparable.CompareTo(object other, IComparer comparer)
		{
			if (other == null)
			{
				return 1;
			}
			Tuple<T1, T2, T3, T4, T5, T6, T7> tuple = other as Tuple<T1, T2, T3, T4, T5, T6, T7>;
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
			num = comparer.Compare(this.m_Item6, tuple.m_Item6);
			if (num != 0)
			{
				return num;
			}
			return comparer.Compare(this.m_Item7, tuple.m_Item7);
		}

		// Token: 0x060003C2 RID: 962 RVA: 0x00009B1B File Offset: 0x00007D1B
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return ((IStructuralEquatable)this).GetHashCode(EqualityComparer<object>.Default);
		}

		// Token: 0x060003C3 RID: 963 RVA: 0x00009B28 File Offset: 0x00007D28
		[__DynamicallyInvokable]
		int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
		{
			return Tuple.CombineHashCodes(comparer.GetHashCode(this.m_Item1), comparer.GetHashCode(this.m_Item2), comparer.GetHashCode(this.m_Item3), comparer.GetHashCode(this.m_Item4), comparer.GetHashCode(this.m_Item5), comparer.GetHashCode(this.m_Item6), comparer.GetHashCode(this.m_Item7));
		}

		// Token: 0x060003C4 RID: 964 RVA: 0x00009BB1 File Offset: 0x00007DB1
		int ITupleInternal.GetHashCode(IEqualityComparer comparer)
		{
			return ((IStructuralEquatable)this).GetHashCode(comparer);
		}

		// Token: 0x060003C5 RID: 965 RVA: 0x00009BBC File Offset: 0x00007DBC
		[__DynamicallyInvokable]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("(");
			return ((ITupleInternal)this).ToString(stringBuilder);
		}

		// Token: 0x060003C6 RID: 966 RVA: 0x00009BE4 File Offset: 0x00007DE4
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
			sb.Append(", ");
			sb.Append(this.m_Item7);
			sb.Append(")");
			return sb.ToString();
		}

		// Token: 0x17000066 RID: 102
		// (get) Token: 0x060003C7 RID: 967 RVA: 0x00009CC9 File Offset: 0x00007EC9
		int ITuple.Length
		{
			get
			{
				return 7;
			}
		}

		// Token: 0x17000067 RID: 103
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
				case 6:
					return this.Item7;
				default:
					throw new IndexOutOfRangeException();
				}
			}
		}

		// Token: 0x04000250 RID: 592
		private readonly T1 m_Item1;

		// Token: 0x04000251 RID: 593
		private readonly T2 m_Item2;

		// Token: 0x04000252 RID: 594
		private readonly T3 m_Item3;

		// Token: 0x04000253 RID: 595
		private readonly T4 m_Item4;

		// Token: 0x04000254 RID: 596
		private readonly T5 m_Item5;

		// Token: 0x04000255 RID: 597
		private readonly T6 m_Item6;

		// Token: 0x04000256 RID: 598
		private readonly T7 m_Item7;
	}
}
