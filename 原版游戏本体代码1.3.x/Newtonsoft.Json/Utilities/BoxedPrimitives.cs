using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Utilities
{
	// Token: 0x02000043 RID: 67
	[NullableContext(1)]
	[Nullable(0)]
	internal static class BoxedPrimitives
	{
		// Token: 0x06000439 RID: 1081 RVA: 0x000107C3 File Offset: 0x0000E9C3
		internal static object Get(bool value)
		{
			if (!value)
			{
				return BoxedPrimitives.BooleanFalse;
			}
			return BoxedPrimitives.BooleanTrue;
		}

		// Token: 0x0600043A RID: 1082 RVA: 0x000107D4 File Offset: 0x0000E9D4
		internal static object Get(int value)
		{
			object result;
			switch (value)
			{
			case -1:
				result = BoxedPrimitives.Int32_M1;
				break;
			case 0:
				result = BoxedPrimitives.Int32_0;
				break;
			case 1:
				result = BoxedPrimitives.Int32_1;
				break;
			case 2:
				result = BoxedPrimitives.Int32_2;
				break;
			case 3:
				result = BoxedPrimitives.Int32_3;
				break;
			case 4:
				result = BoxedPrimitives.Int32_4;
				break;
			case 5:
				result = BoxedPrimitives.Int32_5;
				break;
			case 6:
				result = BoxedPrimitives.Int32_6;
				break;
			case 7:
				result = BoxedPrimitives.Int32_7;
				break;
			case 8:
				result = BoxedPrimitives.Int32_8;
				break;
			default:
				result = value;
				break;
			}
			return result;
		}

		// Token: 0x0600043B RID: 1083 RVA: 0x0001086C File Offset: 0x0000EA6C
		internal static object Get(long value)
		{
			long num = value - -1L;
			if (num <= 9L)
			{
				switch ((uint)num)
				{
				case 0U:
					return BoxedPrimitives.Int64_M1;
				case 1U:
					return BoxedPrimitives.Int64_0;
				case 2U:
					return BoxedPrimitives.Int64_1;
				case 3U:
					return BoxedPrimitives.Int64_2;
				case 4U:
					return BoxedPrimitives.Int64_3;
				case 5U:
					return BoxedPrimitives.Int64_4;
				case 6U:
					return BoxedPrimitives.Int64_5;
				case 7U:
					return BoxedPrimitives.Int64_6;
				case 8U:
					return BoxedPrimitives.Int64_7;
				case 9U:
					return BoxedPrimitives.Int64_8;
				}
			}
			return value;
		}

		// Token: 0x0600043C RID: 1084 RVA: 0x00010911 File Offset: 0x0000EB11
		internal static object Get(decimal value)
		{
			return value;
		}

		// Token: 0x0600043D RID: 1085 RVA: 0x0001091C File Offset: 0x0000EB1C
		internal static object Get(double value)
		{
			if (value == 0.0)
			{
				if (!double.IsNegativeInfinity(1.0 / value))
				{
					return BoxedPrimitives.DoubleZero;
				}
				return BoxedPrimitives.DoubleNegativeZero;
			}
			else if (double.IsInfinity(value))
			{
				if (!double.IsPositiveInfinity(value))
				{
					return BoxedPrimitives.DoubleNegativeInfinity;
				}
				return BoxedPrimitives.DoublePositiveInfinity;
			}
			else
			{
				if (double.IsNaN(value))
				{
					return BoxedPrimitives.DoubleNaN;
				}
				return value;
			}
		}

		// Token: 0x04000158 RID: 344
		internal static readonly object BooleanTrue = true;

		// Token: 0x04000159 RID: 345
		internal static readonly object BooleanFalse = false;

		// Token: 0x0400015A RID: 346
		internal static readonly object Int32_M1 = -1;

		// Token: 0x0400015B RID: 347
		internal static readonly object Int32_0 = 0;

		// Token: 0x0400015C RID: 348
		internal static readonly object Int32_1 = 1;

		// Token: 0x0400015D RID: 349
		internal static readonly object Int32_2 = 2;

		// Token: 0x0400015E RID: 350
		internal static readonly object Int32_3 = 3;

		// Token: 0x0400015F RID: 351
		internal static readonly object Int32_4 = 4;

		// Token: 0x04000160 RID: 352
		internal static readonly object Int32_5 = 5;

		// Token: 0x04000161 RID: 353
		internal static readonly object Int32_6 = 6;

		// Token: 0x04000162 RID: 354
		internal static readonly object Int32_7 = 7;

		// Token: 0x04000163 RID: 355
		internal static readonly object Int32_8 = 8;

		// Token: 0x04000164 RID: 356
		internal static readonly object Int64_M1 = -1L;

		// Token: 0x04000165 RID: 357
		internal static readonly object Int64_0 = 0L;

		// Token: 0x04000166 RID: 358
		internal static readonly object Int64_1 = 1L;

		// Token: 0x04000167 RID: 359
		internal static readonly object Int64_2 = 2L;

		// Token: 0x04000168 RID: 360
		internal static readonly object Int64_3 = 3L;

		// Token: 0x04000169 RID: 361
		internal static readonly object Int64_4 = 4L;

		// Token: 0x0400016A RID: 362
		internal static readonly object Int64_5 = 5L;

		// Token: 0x0400016B RID: 363
		internal static readonly object Int64_6 = 6L;

		// Token: 0x0400016C RID: 364
		internal static readonly object Int64_7 = 7L;

		// Token: 0x0400016D RID: 365
		internal static readonly object Int64_8 = 8L;

		// Token: 0x0400016E RID: 366
		internal static readonly object DoubleNaN = double.NaN;

		// Token: 0x0400016F RID: 367
		internal static readonly object DoublePositiveInfinity = double.PositiveInfinity;

		// Token: 0x04000170 RID: 368
		internal static readonly object DoubleNegativeInfinity = double.NegativeInfinity;

		// Token: 0x04000171 RID: 369
		internal static readonly object DoubleZero = 0.0;

		// Token: 0x04000172 RID: 370
		internal static readonly object DoubleNegativeZero = -0.0;
	}
}
