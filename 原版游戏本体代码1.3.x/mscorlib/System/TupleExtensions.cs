using System;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x02000072 RID: 114
	public static class TupleExtensions
	{
		// Token: 0x06000473 RID: 1139 RVA: 0x0000E67C File Offset: 0x0000C87C
		public static void Deconstruct<T1>(this Tuple<T1> value, out T1 item1)
		{
			item1 = value.Item1;
		}

		// Token: 0x06000474 RID: 1140 RVA: 0x0000E68A File Offset: 0x0000C88A
		public static void Deconstruct<T1, T2>(this Tuple<T1, T2> value, out T1 item1, out T2 item2)
		{
			item1 = value.Item1;
			item2 = value.Item2;
		}

		// Token: 0x06000475 RID: 1141 RVA: 0x0000E6A4 File Offset: 0x0000C8A4
		public static void Deconstruct<T1, T2, T3>(this Tuple<T1, T2, T3> value, out T1 item1, out T2 item2, out T3 item3)
		{
			item1 = value.Item1;
			item2 = value.Item2;
			item3 = value.Item3;
		}

		// Token: 0x06000476 RID: 1142 RVA: 0x0000E6CA File Offset: 0x0000C8CA
		public static void Deconstruct<T1, T2, T3, T4>(this Tuple<T1, T2, T3, T4> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4)
		{
			item1 = value.Item1;
			item2 = value.Item2;
			item3 = value.Item3;
			item4 = value.Item4;
		}

		// Token: 0x06000477 RID: 1143 RVA: 0x0000E6FD File Offset: 0x0000C8FD
		public static void Deconstruct<T1, T2, T3, T4, T5>(this Tuple<T1, T2, T3, T4, T5> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5)
		{
			item1 = value.Item1;
			item2 = value.Item2;
			item3 = value.Item3;
			item4 = value.Item4;
			item5 = value.Item5;
		}

		// Token: 0x06000478 RID: 1144 RVA: 0x0000E740 File Offset: 0x0000C940
		public static void Deconstruct<T1, T2, T3, T4, T5, T6>(this Tuple<T1, T2, T3, T4, T5, T6> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6)
		{
			item1 = value.Item1;
			item2 = value.Item2;
			item3 = value.Item3;
			item4 = value.Item4;
			item5 = value.Item5;
			item6 = value.Item6;
		}

		// Token: 0x06000479 RID: 1145 RVA: 0x0000E798 File Offset: 0x0000C998
		public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7>(this Tuple<T1, T2, T3, T4, T5, T6, T7> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7)
		{
			item1 = value.Item1;
			item2 = value.Item2;
			item3 = value.Item3;
			item4 = value.Item4;
			item5 = value.Item5;
			item6 = value.Item6;
			item7 = value.Item7;
		}

		// Token: 0x0600047A RID: 1146 RVA: 0x0000E800 File Offset: 0x0000CA00
		public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8)
		{
			item1 = value.Item1;
			item2 = value.Item2;
			item3 = value.Item3;
			item4 = value.Item4;
			item5 = value.Item5;
			item6 = value.Item6;
			item7 = value.Item7;
			item8 = value.Rest.Item1;
		}

		// Token: 0x0600047B RID: 1147 RVA: 0x0000E878 File Offset: 0x0000CA78
		public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9)
		{
			item1 = value.Item1;
			item2 = value.Item2;
			item3 = value.Item3;
			item4 = value.Item4;
			item5 = value.Item5;
			item6 = value.Item6;
			item7 = value.Item7;
			item8 = value.Rest.Item1;
			item9 = value.Rest.Item2;
		}

		// Token: 0x0600047C RID: 1148 RVA: 0x0000E904 File Offset: 0x0000CB04
		public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10)
		{
			item1 = value.Item1;
			item2 = value.Item2;
			item3 = value.Item3;
			item4 = value.Item4;
			item5 = value.Item5;
			item6 = value.Item6;
			item7 = value.Item7;
			item8 = value.Rest.Item1;
			item9 = value.Rest.Item2;
			item10 = value.Rest.Item3;
		}

		// Token: 0x0600047D RID: 1149 RVA: 0x0000E9A0 File Offset: 0x0000CBA0
		public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11)
		{
			item1 = value.Item1;
			item2 = value.Item2;
			item3 = value.Item3;
			item4 = value.Item4;
			item5 = value.Item5;
			item6 = value.Item6;
			item7 = value.Item7;
			item8 = value.Rest.Item1;
			item9 = value.Rest.Item2;
			item10 = value.Rest.Item3;
			item11 = value.Rest.Item4;
		}

		// Token: 0x0600047E RID: 1150 RVA: 0x0000EA50 File Offset: 0x0000CC50
		public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12)
		{
			item1 = value.Item1;
			item2 = value.Item2;
			item3 = value.Item3;
			item4 = value.Item4;
			item5 = value.Item5;
			item6 = value.Item6;
			item7 = value.Item7;
			item8 = value.Rest.Item1;
			item9 = value.Rest.Item2;
			item10 = value.Rest.Item3;
			item11 = value.Rest.Item4;
			item12 = value.Rest.Item5;
		}

		// Token: 0x0600047F RID: 1151 RVA: 0x0000EB10 File Offset: 0x0000CD10
		public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13)
		{
			item1 = value.Item1;
			item2 = value.Item2;
			item3 = value.Item3;
			item4 = value.Item4;
			item5 = value.Item5;
			item6 = value.Item6;
			item7 = value.Item7;
			item8 = value.Rest.Item1;
			item9 = value.Rest.Item2;
			item10 = value.Rest.Item3;
			item11 = value.Rest.Item4;
			item12 = value.Rest.Item5;
			item13 = value.Rest.Item6;
		}

		// Token: 0x06000480 RID: 1152 RVA: 0x0000EBE4 File Offset: 0x0000CDE4
		public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14)
		{
			item1 = value.Item1;
			item2 = value.Item2;
			item3 = value.Item3;
			item4 = value.Item4;
			item5 = value.Item5;
			item6 = value.Item6;
			item7 = value.Item7;
			item8 = value.Rest.Item1;
			item9 = value.Rest.Item2;
			item10 = value.Rest.Item3;
			item11 = value.Rest.Item4;
			item12 = value.Rest.Item5;
			item13 = value.Rest.Item6;
			item14 = value.Rest.Item7;
		}

		// Token: 0x06000481 RID: 1153 RVA: 0x0000ECC8 File Offset: 0x0000CEC8
		public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15>>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15)
		{
			item1 = value.Item1;
			item2 = value.Item2;
			item3 = value.Item3;
			item4 = value.Item4;
			item5 = value.Item5;
			item6 = value.Item6;
			item7 = value.Item7;
			item8 = value.Rest.Item1;
			item9 = value.Rest.Item2;
			item10 = value.Rest.Item3;
			item11 = value.Rest.Item4;
			item12 = value.Rest.Item5;
			item13 = value.Rest.Item6;
			item14 = value.Rest.Item7;
			item15 = value.Rest.Rest.Item1;
		}

		// Token: 0x06000482 RID: 1154 RVA: 0x0000EDC4 File Offset: 0x0000CFC4
		public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16>>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16)
		{
			item1 = value.Item1;
			item2 = value.Item2;
			item3 = value.Item3;
			item4 = value.Item4;
			item5 = value.Item5;
			item6 = value.Item6;
			item7 = value.Item7;
			item8 = value.Rest.Item1;
			item9 = value.Rest.Item2;
			item10 = value.Rest.Item3;
			item11 = value.Rest.Item4;
			item12 = value.Rest.Item5;
			item13 = value.Rest.Item6;
			item14 = value.Rest.Item7;
			item15 = value.Rest.Rest.Item1;
			item16 = value.Rest.Rest.Item2;
		}

		// Token: 0x06000483 RID: 1155 RVA: 0x0000EED8 File Offset: 0x0000D0D8
		public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17>>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16, out T17 item17)
		{
			item1 = value.Item1;
			item2 = value.Item2;
			item3 = value.Item3;
			item4 = value.Item4;
			item5 = value.Item5;
			item6 = value.Item6;
			item7 = value.Item7;
			item8 = value.Rest.Item1;
			item9 = value.Rest.Item2;
			item10 = value.Rest.Item3;
			item11 = value.Rest.Item4;
			item12 = value.Rest.Item5;
			item13 = value.Rest.Item6;
			item14 = value.Rest.Item7;
			item15 = value.Rest.Rest.Item1;
			item16 = value.Rest.Rest.Item2;
			item17 = value.Rest.Rest.Item3;
		}

		// Token: 0x06000484 RID: 1156 RVA: 0x0000F000 File Offset: 0x0000D200
		public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18>>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16, out T17 item17, out T18 item18)
		{
			item1 = value.Item1;
			item2 = value.Item2;
			item3 = value.Item3;
			item4 = value.Item4;
			item5 = value.Item5;
			item6 = value.Item6;
			item7 = value.Item7;
			item8 = value.Rest.Item1;
			item9 = value.Rest.Item2;
			item10 = value.Rest.Item3;
			item11 = value.Rest.Item4;
			item12 = value.Rest.Item5;
			item13 = value.Rest.Item6;
			item14 = value.Rest.Item7;
			item15 = value.Rest.Rest.Item1;
			item16 = value.Rest.Rest.Item2;
			item17 = value.Rest.Rest.Item3;
			item18 = value.Rest.Rest.Item4;
		}

		// Token: 0x06000485 RID: 1157 RVA: 0x0000F140 File Offset: 0x0000D340
		public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19>>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16, out T17 item17, out T18 item18, out T19 item19)
		{
			item1 = value.Item1;
			item2 = value.Item2;
			item3 = value.Item3;
			item4 = value.Item4;
			item5 = value.Item5;
			item6 = value.Item6;
			item7 = value.Item7;
			item8 = value.Rest.Item1;
			item9 = value.Rest.Item2;
			item10 = value.Rest.Item3;
			item11 = value.Rest.Item4;
			item12 = value.Rest.Item5;
			item13 = value.Rest.Item6;
			item14 = value.Rest.Item7;
			item15 = value.Rest.Rest.Item1;
			item16 = value.Rest.Rest.Item2;
			item17 = value.Rest.Rest.Item3;
			item18 = value.Rest.Rest.Item4;
			item19 = value.Rest.Rest.Item5;
		}

		// Token: 0x06000486 RID: 1158 RVA: 0x0000F298 File Offset: 0x0000D498
		public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19, T20>>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16, out T17 item17, out T18 item18, out T19 item19, out T20 item20)
		{
			item1 = value.Item1;
			item2 = value.Item2;
			item3 = value.Item3;
			item4 = value.Item4;
			item5 = value.Item5;
			item6 = value.Item6;
			item7 = value.Item7;
			item8 = value.Rest.Item1;
			item9 = value.Rest.Item2;
			item10 = value.Rest.Item3;
			item11 = value.Rest.Item4;
			item12 = value.Rest.Item5;
			item13 = value.Rest.Item6;
			item14 = value.Rest.Item7;
			item15 = value.Rest.Rest.Item1;
			item16 = value.Rest.Rest.Item2;
			item17 = value.Rest.Rest.Item3;
			item18 = value.Rest.Rest.Item4;
			item19 = value.Rest.Rest.Item5;
			item20 = value.Rest.Rest.Item6;
		}

		// Token: 0x06000487 RID: 1159 RVA: 0x0000F408 File Offset: 0x0000D608
		public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19, T20, T21>>> value, out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16, out T17 item17, out T18 item18, out T19 item19, out T20 item20, out T21 item21)
		{
			item1 = value.Item1;
			item2 = value.Item2;
			item3 = value.Item3;
			item4 = value.Item4;
			item5 = value.Item5;
			item6 = value.Item6;
			item7 = value.Item7;
			item8 = value.Rest.Item1;
			item9 = value.Rest.Item2;
			item10 = value.Rest.Item3;
			item11 = value.Rest.Item4;
			item12 = value.Rest.Item5;
			item13 = value.Rest.Item6;
			item14 = value.Rest.Item7;
			item15 = value.Rest.Rest.Item1;
			item16 = value.Rest.Rest.Item2;
			item17 = value.Rest.Rest.Item3;
			item18 = value.Rest.Rest.Item4;
			item19 = value.Rest.Rest.Item5;
			item20 = value.Rest.Rest.Item6;
			item21 = value.Rest.Rest.Item7;
		}

		// Token: 0x06000488 RID: 1160 RVA: 0x0000F58C File Offset: 0x0000D78C
		public static ValueTuple<T1> ToValueTuple<T1>(this Tuple<T1> value)
		{
			return ValueTuple.Create<T1>(value.Item1);
		}

		// Token: 0x06000489 RID: 1161 RVA: 0x0000F599 File Offset: 0x0000D799
		public static ValueTuple<T1, T2> ToValueTuple<T1, T2>(this Tuple<T1, T2> value)
		{
			return ValueTuple.Create<T1, T2>(value.Item1, value.Item2);
		}

		// Token: 0x0600048A RID: 1162 RVA: 0x0000F5AC File Offset: 0x0000D7AC
		public static ValueTuple<T1, T2, T3> ToValueTuple<T1, T2, T3>(this Tuple<T1, T2, T3> value)
		{
			return ValueTuple.Create<T1, T2, T3>(value.Item1, value.Item2, value.Item3);
		}

		// Token: 0x0600048B RID: 1163 RVA: 0x0000F5C5 File Offset: 0x0000D7C5
		public static ValueTuple<T1, T2, T3, T4> ToValueTuple<T1, T2, T3, T4>(this Tuple<T1, T2, T3, T4> value)
		{
			return ValueTuple.Create<T1, T2, T3, T4>(value.Item1, value.Item2, value.Item3, value.Item4);
		}

		// Token: 0x0600048C RID: 1164 RVA: 0x0000F5E4 File Offset: 0x0000D7E4
		public static ValueTuple<T1, T2, T3, T4, T5> ToValueTuple<T1, T2, T3, T4, T5>(this Tuple<T1, T2, T3, T4, T5> value)
		{
			return ValueTuple.Create<T1, T2, T3, T4, T5>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5);
		}

		// Token: 0x0600048D RID: 1165 RVA: 0x0000F609 File Offset: 0x0000D809
		public static ValueTuple<T1, T2, T3, T4, T5, T6> ToValueTuple<T1, T2, T3, T4, T5, T6>(this Tuple<T1, T2, T3, T4, T5, T6> value)
		{
			return ValueTuple.Create<T1, T2, T3, T4, T5, T6>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6);
		}

		// Token: 0x0600048E RID: 1166 RVA: 0x0000F634 File Offset: 0x0000D834
		public static ValueTuple<T1, T2, T3, T4, T5, T6, T7> ToValueTuple<T1, T2, T3, T4, T5, T6, T7>(this Tuple<T1, T2, T3, T4, T5, T6, T7> value)
		{
			return ValueTuple.Create<T1, T2, T3, T4, T5, T6, T7>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7);
		}

		// Token: 0x0600048F RID: 1167 RVA: 0x0000F668 File Offset: 0x0000D868
		public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>> value)
		{
			return TupleExtensions.CreateLong<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, ValueTuple.Create<T8>(value.Rest.Item1));
		}

		// Token: 0x06000490 RID: 1168 RVA: 0x0000F6B4 File Offset: 0x0000D8B4
		public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>> value)
		{
			return TupleExtensions.CreateLong<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, ValueTuple.Create<T8, T9>(value.Rest.Item1, value.Rest.Item2));
		}

		// Token: 0x06000491 RID: 1169 RVA: 0x0000F70C File Offset: 0x0000D90C
		public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>> value)
		{
			return TupleExtensions.CreateLong<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, ValueTuple.Create<T8, T9, T10>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3));
		}

		// Token: 0x06000492 RID: 1170 RVA: 0x0000F770 File Offset: 0x0000D970
		public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11>> value)
		{
			return TupleExtensions.CreateLong<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, ValueTuple.Create<T8, T9, T10, T11>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4));
		}

		// Token: 0x06000493 RID: 1171 RVA: 0x0000F7E0 File Offset: 0x0000D9E0
		public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12>> value)
		{
			return TupleExtensions.CreateLong<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, ValueTuple.Create<T8, T9, T10, T11, T12>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5));
		}

		// Token: 0x06000494 RID: 1172 RVA: 0x0000F858 File Offset: 0x0000DA58
		public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13>> value)
		{
			return TupleExtensions.CreateLong<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, ValueTuple.Create<T8, T9, T10, T11, T12, T13>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5, value.Rest.Item6));
		}

		// Token: 0x06000495 RID: 1173 RVA: 0x0000F8DC File Offset: 0x0000DADC
		public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14>> value)
		{
			return TupleExtensions.CreateLong<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, ValueTuple.Create<T8, T9, T10, T11, T12, T13, T14>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5, value.Rest.Item6, value.Rest.Item7));
		}

		// Token: 0x06000496 RID: 1174 RVA: 0x0000F96C File Offset: 0x0000DB6C
		public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15>>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15>>> value)
		{
			return TupleExtensions.CreateLong<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15>>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, TupleExtensions.CreateLong<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15>>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5, value.Rest.Item6, value.Rest.Item7, ValueTuple.Create<T15>(value.Rest.Rest.Item1)));
		}

		// Token: 0x06000497 RID: 1175 RVA: 0x0000FA10 File Offset: 0x0000DC10
		public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16>>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16>>> value)
		{
			return TupleExtensions.CreateLong<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16>>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, TupleExtensions.CreateLong<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16>>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5, value.Rest.Item6, value.Rest.Item7, ValueTuple.Create<T15, T16>(value.Rest.Rest.Item1, value.Rest.Rest.Item2)));
		}

		// Token: 0x06000498 RID: 1176 RVA: 0x0000FAC4 File Offset: 0x0000DCC4
		public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17>>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17>>> value)
		{
			return TupleExtensions.CreateLong<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17>>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, TupleExtensions.CreateLong<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17>>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5, value.Rest.Item6, value.Rest.Item7, ValueTuple.Create<T15, T16, T17>(value.Rest.Rest.Item1, value.Rest.Rest.Item2, value.Rest.Rest.Item3)));
		}

		// Token: 0x06000499 RID: 1177 RVA: 0x0000FB88 File Offset: 0x0000DD88
		public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18>>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18>>> value)
		{
			return TupleExtensions.CreateLong<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18>>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, TupleExtensions.CreateLong<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18>>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5, value.Rest.Item6, value.Rest.Item7, ValueTuple.Create<T15, T16, T17, T18>(value.Rest.Rest.Item1, value.Rest.Rest.Item2, value.Rest.Rest.Item3, value.Rest.Rest.Item4)));
		}

		// Token: 0x0600049A RID: 1178 RVA: 0x0000FC5C File Offset: 0x0000DE5C
		public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19>>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19>>> value)
		{
			return TupleExtensions.CreateLong<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19>>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, TupleExtensions.CreateLong<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19>>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5, value.Rest.Item6, value.Rest.Item7, ValueTuple.Create<T15, T16, T17, T18, T19>(value.Rest.Rest.Item1, value.Rest.Rest.Item2, value.Rest.Rest.Item3, value.Rest.Rest.Item4, value.Rest.Rest.Item5)));
		}

		// Token: 0x0600049B RID: 1179 RVA: 0x0000FD40 File Offset: 0x0000DF40
		public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19, T20>>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19, T20>>> value)
		{
			return TupleExtensions.CreateLong<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19, T20>>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, TupleExtensions.CreateLong<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19, T20>>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5, value.Rest.Item6, value.Rest.Item7, ValueTuple.Create<T15, T16, T17, T18, T19, T20>(value.Rest.Rest.Item1, value.Rest.Rest.Item2, value.Rest.Rest.Item3, value.Rest.Rest.Item4, value.Rest.Rest.Item5, value.Rest.Rest.Item6)));
		}

		// Token: 0x0600049C RID: 1180 RVA: 0x0000FE34 File Offset: 0x0000E034
		public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19, T20, T21>>> ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19, T20, T21>>> value)
		{
			return TupleExtensions.CreateLong<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19, T20, T21>>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, TupleExtensions.CreateLong<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19, T20, T21>>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5, value.Rest.Item6, value.Rest.Item7, ValueTuple.Create<T15, T16, T17, T18, T19, T20, T21>(value.Rest.Rest.Item1, value.Rest.Rest.Item2, value.Rest.Rest.Item3, value.Rest.Rest.Item4, value.Rest.Rest.Item5, value.Rest.Rest.Item6, value.Rest.Rest.Item7)));
		}

		// Token: 0x0600049D RID: 1181 RVA: 0x0000FF37 File Offset: 0x0000E137
		public static Tuple<T1> ToTuple<T1>(this ValueTuple<T1> value)
		{
			return Tuple.Create<T1>(value.Item1);
		}

		// Token: 0x0600049E RID: 1182 RVA: 0x0000FF44 File Offset: 0x0000E144
		public static Tuple<T1, T2> ToTuple<T1, T2>(this ValueTuple<T1, T2> value)
		{
			return Tuple.Create<T1, T2>(value.Item1, value.Item2);
		}

		// Token: 0x0600049F RID: 1183 RVA: 0x0000FF57 File Offset: 0x0000E157
		public static Tuple<T1, T2, T3> ToTuple<T1, T2, T3>(this ValueTuple<T1, T2, T3> value)
		{
			return Tuple.Create<T1, T2, T3>(value.Item1, value.Item2, value.Item3);
		}

		// Token: 0x060004A0 RID: 1184 RVA: 0x0000FF70 File Offset: 0x0000E170
		public static Tuple<T1, T2, T3, T4> ToTuple<T1, T2, T3, T4>(this ValueTuple<T1, T2, T3, T4> value)
		{
			return Tuple.Create<T1, T2, T3, T4>(value.Item1, value.Item2, value.Item3, value.Item4);
		}

		// Token: 0x060004A1 RID: 1185 RVA: 0x0000FF8F File Offset: 0x0000E18F
		public static Tuple<T1, T2, T3, T4, T5> ToTuple<T1, T2, T3, T4, T5>(this ValueTuple<T1, T2, T3, T4, T5> value)
		{
			return Tuple.Create<T1, T2, T3, T4, T5>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5);
		}

		// Token: 0x060004A2 RID: 1186 RVA: 0x0000FFB4 File Offset: 0x0000E1B4
		public static Tuple<T1, T2, T3, T4, T5, T6> ToTuple<T1, T2, T3, T4, T5, T6>(this ValueTuple<T1, T2, T3, T4, T5, T6> value)
		{
			return Tuple.Create<T1, T2, T3, T4, T5, T6>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6);
		}

		// Token: 0x060004A3 RID: 1187 RVA: 0x0000FFDF File Offset: 0x0000E1DF
		public static Tuple<T1, T2, T3, T4, T5, T6, T7> ToTuple<T1, T2, T3, T4, T5, T6, T7>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7> value)
		{
			return Tuple.Create<T1, T2, T3, T4, T5, T6, T7>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7);
		}

		// Token: 0x060004A4 RID: 1188 RVA: 0x00010010 File Offset: 0x0000E210
		public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>> value)
		{
			return TupleExtensions.CreateLongRef<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, Tuple.Create<T8>(value.Rest.Item1));
		}

		// Token: 0x060004A5 RID: 1189 RVA: 0x0001005C File Offset: 0x0000E25C
		public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>> value)
		{
			return TupleExtensions.CreateLongRef<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, Tuple.Create<T8, T9>(value.Rest.Item1, value.Rest.Item2));
		}

		// Token: 0x060004A6 RID: 1190 RVA: 0x000100B4 File Offset: 0x0000E2B4
		public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>> value)
		{
			return TupleExtensions.CreateLongRef<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, Tuple.Create<T8, T9, T10>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3));
		}

		// Token: 0x060004A7 RID: 1191 RVA: 0x00010118 File Offset: 0x0000E318
		public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11>> value)
		{
			return TupleExtensions.CreateLongRef<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, Tuple.Create<T8, T9, T10, T11>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4));
		}

		// Token: 0x060004A8 RID: 1192 RVA: 0x00010188 File Offset: 0x0000E388
		public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12>> value)
		{
			return TupleExtensions.CreateLongRef<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, Tuple.Create<T8, T9, T10, T11, T12>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5));
		}

		// Token: 0x060004A9 RID: 1193 RVA: 0x00010200 File Offset: 0x0000E400
		public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13>> value)
		{
			return TupleExtensions.CreateLongRef<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, Tuple.Create<T8, T9, T10, T11, T12, T13>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5, value.Rest.Item6));
		}

		// Token: 0x060004AA RID: 1194 RVA: 0x00010284 File Offset: 0x0000E484
		public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14>> value)
		{
			return TupleExtensions.CreateLongRef<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, Tuple.Create<T8, T9, T10, T11, T12, T13, T14>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5, value.Rest.Item6, value.Rest.Item7));
		}

		// Token: 0x060004AB RID: 1195 RVA: 0x00010314 File Offset: 0x0000E514
		public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15>>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15>>> value)
		{
			return TupleExtensions.CreateLongRef<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15>>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, TupleExtensions.CreateLongRef<T8, T9, T10, T11, T12, T13, T14, Tuple<T15>>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5, value.Rest.Item6, value.Rest.Item7, Tuple.Create<T15>(value.Rest.Rest.Item1)));
		}

		// Token: 0x060004AC RID: 1196 RVA: 0x000103B8 File Offset: 0x0000E5B8
		public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16>>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16>>> value)
		{
			return TupleExtensions.CreateLongRef<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16>>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, TupleExtensions.CreateLongRef<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16>>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5, value.Rest.Item6, value.Rest.Item7, Tuple.Create<T15, T16>(value.Rest.Rest.Item1, value.Rest.Rest.Item2)));
		}

		// Token: 0x060004AD RID: 1197 RVA: 0x0001046C File Offset: 0x0000E66C
		public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17>>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17>>> value)
		{
			return TupleExtensions.CreateLongRef<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17>>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, TupleExtensions.CreateLongRef<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17>>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5, value.Rest.Item6, value.Rest.Item7, Tuple.Create<T15, T16, T17>(value.Rest.Rest.Item1, value.Rest.Rest.Item2, value.Rest.Rest.Item3)));
		}

		// Token: 0x060004AE RID: 1198 RVA: 0x00010530 File Offset: 0x0000E730
		public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18>>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18>>> value)
		{
			return TupleExtensions.CreateLongRef<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18>>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, TupleExtensions.CreateLongRef<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18>>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5, value.Rest.Item6, value.Rest.Item7, Tuple.Create<T15, T16, T17, T18>(value.Rest.Rest.Item1, value.Rest.Rest.Item2, value.Rest.Rest.Item3, value.Rest.Rest.Item4)));
		}

		// Token: 0x060004AF RID: 1199 RVA: 0x00010604 File Offset: 0x0000E804
		public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19>>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19>>> value)
		{
			return TupleExtensions.CreateLongRef<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19>>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, TupleExtensions.CreateLongRef<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19>>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5, value.Rest.Item6, value.Rest.Item7, Tuple.Create<T15, T16, T17, T18, T19>(value.Rest.Rest.Item1, value.Rest.Rest.Item2, value.Rest.Rest.Item3, value.Rest.Rest.Item4, value.Rest.Rest.Item5)));
		}

		// Token: 0x060004B0 RID: 1200 RVA: 0x000106E8 File Offset: 0x0000E8E8
		public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19, T20>>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19, T20>>> value)
		{
			return TupleExtensions.CreateLongRef<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19, T20>>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, TupleExtensions.CreateLongRef<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19, T20>>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5, value.Rest.Item6, value.Rest.Item7, Tuple.Create<T15, T16, T17, T18, T19, T20>(value.Rest.Rest.Item1, value.Rest.Rest.Item2, value.Rest.Rest.Item3, value.Rest.Rest.Item4, value.Rest.Rest.Item5, value.Rest.Rest.Item6)));
		}

		// Token: 0x060004B1 RID: 1201 RVA: 0x000107DC File Offset: 0x0000E9DC
		public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19, T20, T21>>> ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19, T20, T21>>> value)
		{
			return TupleExtensions.CreateLongRef<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19, T20, T21>>>(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7, TupleExtensions.CreateLongRef<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19, T20, T21>>(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5, value.Rest.Item6, value.Rest.Item7, Tuple.Create<T15, T16, T17, T18, T19, T20, T21>(value.Rest.Rest.Item1, value.Rest.Rest.Item2, value.Rest.Rest.Item3, value.Rest.Rest.Item4, value.Rest.Rest.Item5, value.Rest.Rest.Item6, value.Rest.Rest.Item7)));
		}

		// Token: 0x060004B2 RID: 1202 RVA: 0x000108DF File Offset: 0x0000EADF
		private static ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> CreateLong<T1, T2, T3, T4, T5, T6, T7, TRest>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest) where TRest : struct, ITuple
		{
			return new ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, rest);
		}

		// Token: 0x060004B3 RID: 1203 RVA: 0x000108F2 File Offset: 0x0000EAF2
		private static Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> CreateLongRef<T1, T2, T3, T4, T5, T6, T7, TRest>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest) where TRest : ITuple
		{
			return new Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, rest);
		}
	}
}
