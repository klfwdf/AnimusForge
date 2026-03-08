using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System
{
	// Token: 0x02000060 RID: 96
	[__DynamicallyInvokable]
	[Serializable]
	public class Tuple<T1> : IStructuralEquatable, IStructuralComparable, IComparable, ITupleInternal, ITuple
	{
		// Token: 0x1700003E RID: 62
		// (get) Token: 0x06000359 RID: 857 RVA: 0x000086E6 File Offset: 0x000068E6
		[__DynamicallyInvokable]
		public T1 Item1
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_Item1;
			}
		}

		// Token: 0x0600035A RID: 858 RVA: 0x000086EE File Offset: 0x000068EE
		[__DynamicallyInvokable]
		public Tuple(T1 item1)
		{
			this.m_Item1 = item1;
		}

		// Token: 0x0600035B RID: 859 RVA: 0x000086FD File Offset: 0x000068FD
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return ((IStructuralEquatable)this).Equals(obj, EqualityComparer<object>.Default);
		}

		// Token: 0x0600035C RID: 860 RVA: 0x0000870C File Offset: 0x0000690C
		[__DynamicallyInvokable]
		bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
		{
			if (other == null)
			{
				return false;
			}
			Tuple<T1> tuple = other as Tuple<T1>;
			return tuple != null && comparer.Equals(this.m_Item1, tuple.m_Item1);
		}

		// Token: 0x0600035D RID: 861 RVA: 0x00008746 File Offset: 0x00006946
		[__DynamicallyInvokable]
		int IComparable.CompareTo(object obj)
		{
			return ((IStructuralComparable)this).CompareTo(obj, Comparer<object>.Default);
		}

		// Token: 0x0600035E RID: 862 RVA: 0x00008754 File Offset: 0x00006954
		[__DynamicallyInvokable]
		int IStructuralComparable.CompareTo(object other, IComparer comparer)
		{
			if (other == null)
			{
				return 1;
			}
			Tuple<T1> tuple = other as Tuple<T1>;
			if (tuple == null)
			{
				throw new ArgumentException(Environment.GetResourceString("ArgumentException_TupleIncorrectType", new object[] { base.GetType().ToString() }), "other");
			}
			return comparer.Compare(this.m_Item1, tuple.m_Item1);
		}

		// Token: 0x0600035F RID: 863 RVA: 0x000087B5 File Offset: 0x000069B5
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return ((IStructuralEquatable)this).GetHashCode(EqualityComparer<object>.Default);
		}

		// Token: 0x06000360 RID: 864 RVA: 0x000087C2 File Offset: 0x000069C2
		[__DynamicallyInvokable]
		int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
		{
			return comparer.GetHashCode(this.m_Item1);
		}

		// Token: 0x06000361 RID: 865 RVA: 0x000087D5 File Offset: 0x000069D5
		int ITupleInternal.GetHashCode(IEqualityComparer comparer)
		{
			return ((IStructuralEquatable)this).GetHashCode(comparer);
		}

		// Token: 0x06000362 RID: 866 RVA: 0x000087E0 File Offset: 0x000069E0
		[__DynamicallyInvokable]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("(");
			return ((ITupleInternal)this).ToString(stringBuilder);
		}

		// Token: 0x06000363 RID: 867 RVA: 0x00008806 File Offset: 0x00006A06
		string ITupleInternal.ToString(StringBuilder sb)
		{
			sb.Append(this.m_Item1);
			sb.Append(")");
			return sb.ToString();
		}

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x06000364 RID: 868 RVA: 0x0000882C File Offset: 0x00006A2C
		int ITuple.Length
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x17000040 RID: 64
		object ITuple.this[int index]
		{
			get
			{
				if (index != 0)
				{
					throw new IndexOutOfRangeException();
				}
				return this.Item1;
			}
		}

		// Token: 0x0400023B RID: 571
		private readonly T1 m_Item1;
	}
}
