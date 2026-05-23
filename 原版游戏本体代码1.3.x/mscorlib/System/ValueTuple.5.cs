using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x0200006D RID: 109
	[Serializable]
	[StructLayout(LayoutKind.Auto)]
	public struct ValueTuple<T1, T2, T3, T4> : IEquatable<ValueTuple<T1, T2, T3, T4>>, IStructuralEquatable, IStructuralComparable, IComparable, IComparable<ValueTuple<T1, T2, T3, T4>>, IValueTupleInternal, ITuple
	{
		// Token: 0x06000428 RID: 1064 RVA: 0x0000B1DA File Offset: 0x000093DA
		public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4)
		{
			this.Item1 = item1;
			this.Item2 = item2;
			this.Item3 = item3;
			this.Item4 = item4;
		}

		// Token: 0x06000429 RID: 1065 RVA: 0x0000B1F9 File Offset: 0x000093F9
		public override bool Equals(object obj)
		{
			return obj is ValueTuple<T1, T2, T3, T4> && this.Equals((ValueTuple<T1, T2, T3, T4>)obj);
		}

		// Token: 0x0600042A RID: 1066 RVA: 0x0000B214 File Offset: 0x00009414
		public bool Equals(ValueTuple<T1, T2, T3, T4> other)
		{
			return EqualityComparer<T1>.Default.Equals(this.Item1, other.Item1) && EqualityComparer<T2>.Default.Equals(this.Item2, other.Item2) && EqualityComparer<T3>.Default.Equals(this.Item3, other.Item3) && EqualityComparer<T4>.Default.Equals(this.Item4, other.Item4);
		}

		// Token: 0x0600042B RID: 1067 RVA: 0x0000B284 File Offset: 0x00009484
		bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
		{
			if (other == null || !(other is ValueTuple<T1, T2, T3, T4>))
			{
				return false;
			}
			ValueTuple<T1, T2, T3, T4> valueTuple = (ValueTuple<T1, T2, T3, T4>)other;
			return comparer.Equals(this.Item1, valueTuple.Item1) && comparer.Equals(this.Item2, valueTuple.Item2) && comparer.Equals(this.Item3, valueTuple.Item3) && comparer.Equals(this.Item4, valueTuple.Item4);
		}

		// Token: 0x0600042C RID: 1068 RVA: 0x0000B320 File Offset: 0x00009520
		int IComparable.CompareTo(object other)
		{
			if (other == null)
			{
				return 1;
			}
			if (!(other is ValueTuple<T1, T2, T3, T4>))
			{
				throw new ArgumentException(Environment.GetResourceString("ArgumentException_ValueTupleIncorrectType", new object[] { base.GetType().ToString() }), "other");
			}
			return this.CompareTo((ValueTuple<T1, T2, T3, T4>)other);
		}

		// Token: 0x0600042D RID: 1069 RVA: 0x0000B37C File Offset: 0x0000957C
		public int CompareTo(ValueTuple<T1, T2, T3, T4> other)
		{
			int num = Comparer<T1>.Default.Compare(this.Item1, other.Item1);
			if (num != 0)
			{
				return num;
			}
			num = Comparer<T2>.Default.Compare(this.Item2, other.Item2);
			if (num != 0)
			{
				return num;
			}
			num = Comparer<T3>.Default.Compare(this.Item3, other.Item3);
			if (num != 0)
			{
				return num;
			}
			return Comparer<T4>.Default.Compare(this.Item4, other.Item4);
		}

		// Token: 0x0600042E RID: 1070 RVA: 0x0000B3F4 File Offset: 0x000095F4
		int IStructuralComparable.CompareTo(object other, IComparer comparer)
		{
			if (other == null)
			{
				return 1;
			}
			if (!(other is ValueTuple<T1, T2, T3, T4>))
			{
				throw new ArgumentException(Environment.GetResourceString("ArgumentException_ValueTupleIncorrectType", new object[] { base.GetType().ToString() }), "other");
			}
			ValueTuple<T1, T2, T3, T4> valueTuple = (ValueTuple<T1, T2, T3, T4>)other;
			int num = comparer.Compare(this.Item1, valueTuple.Item1);
			if (num != 0)
			{
				return num;
			}
			num = comparer.Compare(this.Item2, valueTuple.Item2);
			if (num != 0)
			{
				return num;
			}
			num = comparer.Compare(this.Item3, valueTuple.Item3);
			if (num != 0)
			{
				return num;
			}
			return comparer.Compare(this.Item4, valueTuple.Item4);
		}

		// Token: 0x0600042F RID: 1071 RVA: 0x0000B4CC File Offset: 0x000096CC
		public override int GetHashCode()
		{
			return ValueTuple.CombineHashCodes(EqualityComparer<T1>.Default.GetHashCode(this.Item1), EqualityComparer<T2>.Default.GetHashCode(this.Item2), EqualityComparer<T3>.Default.GetHashCode(this.Item3), EqualityComparer<T4>.Default.GetHashCode(this.Item4));
		}

		// Token: 0x06000430 RID: 1072 RVA: 0x0000B51E File Offset: 0x0000971E
		int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
		{
			return this.GetHashCodeCore(comparer);
		}

		// Token: 0x06000431 RID: 1073 RVA: 0x0000B528 File Offset: 0x00009728
		private int GetHashCodeCore(IEqualityComparer comparer)
		{
			return ValueTuple.CombineHashCodes(comparer.GetHashCode(this.Item1), comparer.GetHashCode(this.Item2), comparer.GetHashCode(this.Item3), comparer.GetHashCode(this.Item4));
		}

		// Token: 0x06000432 RID: 1074 RVA: 0x0000B57E File Offset: 0x0000977E
		int IValueTupleInternal.GetHashCode(IEqualityComparer comparer)
		{
			return this.GetHashCodeCore(comparer);
		}

		// Token: 0x06000433 RID: 1075 RVA: 0x0000B588 File Offset: 0x00009788
		public override string ToString()
		{
			string[] array = new string[9];
			array[0] = "(";
			int num = 1;
			ref T1 ptr = ref this.Item1;
			T1 t = default(T1);
			string text;
			if (t == null)
			{
				t = this.Item1;
				ptr = ref t;
				if (t == null)
				{
					text = null;
					goto IL_46;
				}
			}
			text = ptr.ToString();
			IL_46:
			array[num] = text;
			array[2] = ", ";
			int num2 = 3;
			ref T2 ptr2 = ref this.Item2;
			T2 t2 = default(T2);
			string text2;
			if (t2 == null)
			{
				t2 = this.Item2;
				ptr2 = ref t2;
				if (t2 == null)
				{
					text2 = null;
					goto IL_86;
				}
			}
			text2 = ptr2.ToString();
			IL_86:
			array[num2] = text2;
			array[4] = ", ";
			int num3 = 5;
			ref T3 ptr3 = ref this.Item3;
			T3 t3 = default(T3);
			string text3;
			if (t3 == null)
			{
				t3 = this.Item3;
				ptr3 = ref t3;
				if (t3 == null)
				{
					text3 = null;
					goto IL_C6;
				}
			}
			text3 = ptr3.ToString();
			IL_C6:
			array[num3] = text3;
			array[6] = ", ";
			int num4 = 7;
			ref T4 ptr4 = ref this.Item4;
			T4 t4 = default(T4);
			string text4;
			if (t4 == null)
			{
				t4 = this.Item4;
				ptr4 = ref t4;
				if (t4 == null)
				{
					text4 = null;
					goto IL_106;
				}
			}
			text4 = ptr4.ToString();
			IL_106:
			array[num4] = text4;
			array[8] = ")";
			return string.Concat(array);
		}

		// Token: 0x06000434 RID: 1076 RVA: 0x0000B6AC File Offset: 0x000098AC
		string IValueTupleInternal.ToStringEnd()
		{
			string[] array = new string[8];
			int num = 0;
			ref T1 ptr = ref this.Item1;
			T1 t = default(T1);
			string text;
			if (t == null)
			{
				t = this.Item1;
				ptr = ref t;
				if (t == null)
				{
					text = null;
					goto IL_3D;
				}
			}
			text = ptr.ToString();
			IL_3D:
			array[num] = text;
			array[1] = ", ";
			int num2 = 2;
			ref T2 ptr2 = ref this.Item2;
			T2 t2 = default(T2);
			string text2;
			if (t2 == null)
			{
				t2 = this.Item2;
				ptr2 = ref t2;
				if (t2 == null)
				{
					text2 = null;
					goto IL_7D;
				}
			}
			text2 = ptr2.ToString();
			IL_7D:
			array[num2] = text2;
			array[3] = ", ";
			int num3 = 4;
			ref T3 ptr3 = ref this.Item3;
			T3 t3 = default(T3);
			string text3;
			if (t3 == null)
			{
				t3 = this.Item3;
				ptr3 = ref t3;
				if (t3 == null)
				{
					text3 = null;
					goto IL_BD;
				}
			}
			text3 = ptr3.ToString();
			IL_BD:
			array[num3] = text3;
			array[5] = ", ";
			int num4 = 6;
			ref T4 ptr4 = ref this.Item4;
			T4 t4 = default(T4);
			string text4;
			if (t4 == null)
			{
				t4 = this.Item4;
				ptr4 = ref t4;
				if (t4 == null)
				{
					text4 = null;
					goto IL_FD;
				}
			}
			text4 = ptr4.ToString();
			IL_FD:
			array[num4] = text4;
			array[7] = ")";
			return string.Concat(array);
		}

		// Token: 0x1700007A RID: 122
		// (get) Token: 0x06000435 RID: 1077 RVA: 0x0000B7C4 File Offset: 0x000099C4
		int ITuple.Length
		{
			get
			{
				return 4;
			}
		}

		// Token: 0x1700007B RID: 123
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
				default:
					throw new IndexOutOfRangeException();
				}
			}
		}

		// Token: 0x04000265 RID: 613
		public T1 Item1;

		// Token: 0x04000266 RID: 614
		public T2 Item2;

		// Token: 0x04000267 RID: 615
		public T3 Item3;

		// Token: 0x04000268 RID: 616
		public T4 Item4;
	}
}
