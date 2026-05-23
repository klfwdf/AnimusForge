using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x0200006A RID: 106
	[Serializable]
	public struct ValueTuple<T1> : IEquatable<ValueTuple<T1>>, IStructuralEquatable, IStructuralComparable, IComparable, IComparable<ValueTuple<T1>>, IValueTupleInternal, ITuple
	{
		// Token: 0x060003FC RID: 1020 RVA: 0x0000A6F1 File Offset: 0x000088F1
		public ValueTuple(T1 item1)
		{
			this.Item1 = item1;
		}

		// Token: 0x060003FD RID: 1021 RVA: 0x0000A6FA File Offset: 0x000088FA
		public override bool Equals(object obj)
		{
			return obj is ValueTuple<T1> && this.Equals((ValueTuple<T1>)obj);
		}

		// Token: 0x060003FE RID: 1022 RVA: 0x0000A712 File Offset: 0x00008912
		public bool Equals(ValueTuple<T1> other)
		{
			return EqualityComparer<T1>.Default.Equals(this.Item1, other.Item1);
		}

		// Token: 0x060003FF RID: 1023 RVA: 0x0000A72C File Offset: 0x0000892C
		bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
		{
			if (other == null || !(other is ValueTuple<T1>))
			{
				return false;
			}
			ValueTuple<T1> valueTuple = (ValueTuple<T1>)other;
			return comparer.Equals(this.Item1, valueTuple.Item1);
		}

		// Token: 0x06000400 RID: 1024 RVA: 0x0000A76C File Offset: 0x0000896C
		int IComparable.CompareTo(object other)
		{
			if (other == null)
			{
				return 1;
			}
			if (!(other is ValueTuple<T1>))
			{
				throw new ArgumentException(Environment.GetResourceString("ArgumentException_ValueTupleIncorrectType", new object[] { base.GetType().ToString() }), "other");
			}
			ValueTuple<T1> valueTuple = (ValueTuple<T1>)other;
			return Comparer<T1>.Default.Compare(this.Item1, valueTuple.Item1);
		}

		// Token: 0x06000401 RID: 1025 RVA: 0x0000A7D6 File Offset: 0x000089D6
		public int CompareTo(ValueTuple<T1> other)
		{
			return Comparer<T1>.Default.Compare(this.Item1, other.Item1);
		}

		// Token: 0x06000402 RID: 1026 RVA: 0x0000A7F0 File Offset: 0x000089F0
		int IStructuralComparable.CompareTo(object other, IComparer comparer)
		{
			if (other == null)
			{
				return 1;
			}
			if (!(other is ValueTuple<T1>))
			{
				throw new ArgumentException(Environment.GetResourceString("ArgumentException_ValueTupleIncorrectType", new object[] { base.GetType().ToString() }), "other");
			}
			ValueTuple<T1> valueTuple = (ValueTuple<T1>)other;
			return comparer.Compare(this.Item1, valueTuple.Item1);
		}

		// Token: 0x06000403 RID: 1027 RVA: 0x0000A860 File Offset: 0x00008A60
		public override int GetHashCode()
		{
			return EqualityComparer<T1>.Default.GetHashCode(this.Item1);
		}

		// Token: 0x06000404 RID: 1028 RVA: 0x0000A872 File Offset: 0x00008A72
		int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
		{
			return comparer.GetHashCode(this.Item1);
		}

		// Token: 0x06000405 RID: 1029 RVA: 0x0000A885 File Offset: 0x00008A85
		int IValueTupleInternal.GetHashCode(IEqualityComparer comparer)
		{
			return comparer.GetHashCode(this.Item1);
		}

		// Token: 0x06000406 RID: 1030 RVA: 0x0000A898 File Offset: 0x00008A98
		public override string ToString()
		{
			string str = "(";
			ref T1 ptr = ref this.Item1;
			T1 t = default(T1);
			string str2;
			if (t == null)
			{
				t = this.Item1;
				ptr = ref t;
				if (t == null)
				{
					str2 = null;
					goto IL_3A;
				}
			}
			str2 = ptr.ToString();
			IL_3A:
			return str + str2 + ")";
		}

		// Token: 0x06000407 RID: 1031 RVA: 0x0000A8EC File Offset: 0x00008AEC
		string IValueTupleInternal.ToStringEnd()
		{
			ref T1 ptr = ref this.Item1;
			T1 t = default(T1);
			string str;
			if (t == null)
			{
				t = this.Item1;
				ptr = ref t;
				if (t == null)
				{
					str = null;
					goto IL_35;
				}
			}
			str = ptr.ToString();
			IL_35:
			return str + ")";
		}

		// Token: 0x17000074 RID: 116
		// (get) Token: 0x06000408 RID: 1032 RVA: 0x0000A938 File Offset: 0x00008B38
		int ITuple.Length
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x17000075 RID: 117
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

		// Token: 0x0400025F RID: 607
		public T1 Item1;
	}
}
