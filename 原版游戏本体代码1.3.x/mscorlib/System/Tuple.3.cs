using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System
{
	// Token: 0x02000061 RID: 97
	[__DynamicallyInvokable]
	[Serializable]
	public class Tuple<T1, T2> : IStructuralEquatable, IStructuralComparable, IComparable, ITupleInternal, ITuple
	{
		// Token: 0x17000041 RID: 65
		// (get) Token: 0x06000366 RID: 870 RVA: 0x00008845 File Offset: 0x00006A45
		[__DynamicallyInvokable]
		public T1 Item1
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_Item1;
			}
		}

		// Token: 0x17000042 RID: 66
		// (get) Token: 0x06000367 RID: 871 RVA: 0x0000884D File Offset: 0x00006A4D
		[__DynamicallyInvokable]
		public T2 Item2
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_Item2;
			}
		}

		// Token: 0x06000368 RID: 872 RVA: 0x00008855 File Offset: 0x00006A55
		[__DynamicallyInvokable]
		public Tuple(T1 item1, T2 item2)
		{
			this.m_Item1 = item1;
			this.m_Item2 = item2;
		}

		// Token: 0x06000369 RID: 873 RVA: 0x0000886B File Offset: 0x00006A6B
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return ((IStructuralEquatable)this).Equals(obj, EqualityComparer<object>.Default);
		}

		// Token: 0x0600036A RID: 874 RVA: 0x0000887C File Offset: 0x00006A7C
		[__DynamicallyInvokable]
		bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
		{
			if (other == null)
			{
				return false;
			}
			Tuple<T1, T2> tuple = other as Tuple<T1, T2>;
			return tuple != null && comparer.Equals(this.m_Item1, tuple.m_Item1) && comparer.Equals(this.m_Item2, tuple.m_Item2);
		}

		// Token: 0x0600036B RID: 875 RVA: 0x000088D6 File Offset: 0x00006AD6
		[__DynamicallyInvokable]
		int IComparable.CompareTo(object obj)
		{
			return ((IStructuralComparable)this).CompareTo(obj, Comparer<object>.Default);
		}

		// Token: 0x0600036C RID: 876 RVA: 0x000088E4 File Offset: 0x00006AE4
		[__DynamicallyInvokable]
		int IStructuralComparable.CompareTo(object other, IComparer comparer)
		{
			if (other == null)
			{
				return 1;
			}
			Tuple<T1, T2> tuple = other as Tuple<T1, T2>;
			if (tuple == null)
			{
				throw new ArgumentException(Environment.GetResourceString("ArgumentException_TupleIncorrectType", new object[] { base.GetType().ToString() }), "other");
			}
			int num = comparer.Compare(this.m_Item1, tuple.m_Item1);
			if (num != 0)
			{
				return num;
			}
			return comparer.Compare(this.m_Item2, tuple.m_Item2);
		}

		// Token: 0x0600036D RID: 877 RVA: 0x00008969 File Offset: 0x00006B69
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return ((IStructuralEquatable)this).GetHashCode(EqualityComparer<object>.Default);
		}

		// Token: 0x0600036E RID: 878 RVA: 0x00008976 File Offset: 0x00006B76
		[__DynamicallyInvokable]
		int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
		{
			return Tuple.CombineHashCodes(comparer.GetHashCode(this.m_Item1), comparer.GetHashCode(this.m_Item2));
		}

		// Token: 0x0600036F RID: 879 RVA: 0x0000899F File Offset: 0x00006B9F
		int ITupleInternal.GetHashCode(IEqualityComparer comparer)
		{
			return ((IStructuralEquatable)this).GetHashCode(comparer);
		}

		// Token: 0x06000370 RID: 880 RVA: 0x000089A8 File Offset: 0x00006BA8
		[__DynamicallyInvokable]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("(");
			return ((ITupleInternal)this).ToString(stringBuilder);
		}

		// Token: 0x06000371 RID: 881 RVA: 0x000089D0 File Offset: 0x00006BD0
		string ITupleInternal.ToString(StringBuilder sb)
		{
			sb.Append(this.m_Item1);
			sb.Append(", ");
			sb.Append(this.m_Item2);
			sb.Append(")");
			return sb.ToString();
		}

		// Token: 0x17000043 RID: 67
		// (get) Token: 0x06000372 RID: 882 RVA: 0x00008A1F File Offset: 0x00006C1F
		int ITuple.Length
		{
			get
			{
				return 2;
			}
		}

		// Token: 0x17000044 RID: 68
		object ITuple.this[int index]
		{
			get
			{
				if (index == 0)
				{
					return this.Item1;
				}
				if (index != 1)
				{
					throw new IndexOutOfRangeException();
				}
				return this.Item2;
			}
		}

		// Token: 0x0400023C RID: 572
		private readonly T1 m_Item1;

		// Token: 0x0400023D RID: 573
		private readonly T2 m_Item2;
	}
}
