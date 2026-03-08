using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Versioning;
using System.Security;

namespace System
{
	// Token: 0x0200010B RID: 267
	[__DynamicallyInvokable]
	public static class Math
	{
		// Token: 0x06000FFE RID: 4094
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern double Acos(double d);

		// Token: 0x06000FFF RID: 4095
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern double Asin(double d);

		// Token: 0x06001000 RID: 4096
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern double Atan(double d);

		// Token: 0x06001001 RID: 4097
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern double Atan2(double y, double x);

		// Token: 0x06001002 RID: 4098 RVA: 0x00030B79 File Offset: 0x0002ED79
		[__DynamicallyInvokable]
		public static decimal Ceiling(decimal d)
		{
			return decimal.Ceiling(d);
		}

		// Token: 0x06001003 RID: 4099
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern double Ceiling(double a);

		// Token: 0x06001004 RID: 4100
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern double Cos(double d);

		// Token: 0x06001005 RID: 4101
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern double Cosh(double value);

		// Token: 0x06001006 RID: 4102 RVA: 0x00030B81 File Offset: 0x0002ED81
		[__DynamicallyInvokable]
		public static decimal Floor(decimal d)
		{
			return decimal.Floor(d);
		}

		// Token: 0x06001007 RID: 4103
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern double Floor(double d);

		// Token: 0x06001008 RID: 4104 RVA: 0x00030B8C File Offset: 0x0002ED8C
		[SecuritySafeCritical]
		private unsafe static double InternalRound(double value, int digits, MidpointRounding mode)
		{
			if (Math.Abs(value) < Math.doubleRoundLimit)
			{
				double num = Math.roundPower10Double[digits];
				value *= num;
				if (mode == MidpointRounding.AwayFromZero)
				{
					double value2 = Math.SplitFractionDouble(&value);
					if (Math.Abs(value2) >= 0.5)
					{
						value += (double)Math.Sign(value2);
					}
				}
				else
				{
					value = Math.Round(value);
				}
				value /= num;
			}
			return value;
		}

		// Token: 0x06001009 RID: 4105 RVA: 0x00030BEC File Offset: 0x0002EDEC
		[SecuritySafeCritical]
		private unsafe static double InternalTruncate(double d)
		{
			Math.SplitFractionDouble(&d);
			return d;
		}

		// Token: 0x0600100A RID: 4106
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern double Sin(double a);

		// Token: 0x0600100B RID: 4107
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern double Tan(double a);

		// Token: 0x0600100C RID: 4108
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern double Sinh(double value);

		// Token: 0x0600100D RID: 4109
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern double Tanh(double value);

		// Token: 0x0600100E RID: 4110
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern double Round(double a);

		// Token: 0x0600100F RID: 4111 RVA: 0x00030BF8 File Offset: 0x0002EDF8
		[__DynamicallyInvokable]
		public static double Round(double value, int digits)
		{
			if (digits < 0 || digits > 15)
			{
				throw new ArgumentOutOfRangeException("digits", Environment.GetResourceString("ArgumentOutOfRange_RoundingDigits"));
			}
			return Math.InternalRound(value, digits, MidpointRounding.ToEven);
		}

		// Token: 0x06001010 RID: 4112 RVA: 0x00030C20 File Offset: 0x0002EE20
		[__DynamicallyInvokable]
		public static double Round(double value, MidpointRounding mode)
		{
			return Math.Round(value, 0, mode);
		}

		// Token: 0x06001011 RID: 4113 RVA: 0x00030C2C File Offset: 0x0002EE2C
		[__DynamicallyInvokable]
		public static double Round(double value, int digits, MidpointRounding mode)
		{
			if (digits < 0 || digits > 15)
			{
				throw new ArgumentOutOfRangeException("digits", Environment.GetResourceString("ArgumentOutOfRange_RoundingDigits"));
			}
			if (mode < MidpointRounding.ToEven || mode > MidpointRounding.AwayFromZero)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidEnumValue", new object[] { mode, "MidpointRounding" }), "mode");
			}
			return Math.InternalRound(value, digits, mode);
		}

		// Token: 0x06001012 RID: 4114 RVA: 0x00030C93 File Offset: 0x0002EE93
		[__DynamicallyInvokable]
		public static decimal Round(decimal d)
		{
			return decimal.Round(d, 0);
		}

		// Token: 0x06001013 RID: 4115 RVA: 0x00030C9C File Offset: 0x0002EE9C
		[__DynamicallyInvokable]
		public static decimal Round(decimal d, int decimals)
		{
			return decimal.Round(d, decimals);
		}

		// Token: 0x06001014 RID: 4116 RVA: 0x00030CA5 File Offset: 0x0002EEA5
		[__DynamicallyInvokable]
		public static decimal Round(decimal d, MidpointRounding mode)
		{
			return decimal.Round(d, 0, mode);
		}

		// Token: 0x06001015 RID: 4117 RVA: 0x00030CAF File Offset: 0x0002EEAF
		[__DynamicallyInvokable]
		public static decimal Round(decimal d, int decimals, MidpointRounding mode)
		{
			return decimal.Round(d, decimals, mode);
		}

		// Token: 0x06001016 RID: 4118
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private unsafe static extern double SplitFractionDouble(double* value);

		// Token: 0x06001017 RID: 4119 RVA: 0x00030CB9 File Offset: 0x0002EEB9
		[__DynamicallyInvokable]
		public static decimal Truncate(decimal d)
		{
			return decimal.Truncate(d);
		}

		// Token: 0x06001018 RID: 4120 RVA: 0x00030CC1 File Offset: 0x0002EEC1
		[__DynamicallyInvokable]
		public static double Truncate(double d)
		{
			return Math.InternalTruncate(d);
		}

		// Token: 0x06001019 RID: 4121
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern double Sqrt(double d);

		// Token: 0x0600101A RID: 4122
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern double Log(double d);

		// Token: 0x0600101B RID: 4123
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern double Log10(double d);

		// Token: 0x0600101C RID: 4124
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern double Exp(double d);

		// Token: 0x0600101D RID: 4125
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern double Pow(double x, double y);

		// Token: 0x0600101E RID: 4126 RVA: 0x00030CCC File Offset: 0x0002EECC
		[__DynamicallyInvokable]
		public static double IEEERemainder(double x, double y)
		{
			if (double.IsNaN(x))
			{
				return x;
			}
			if (double.IsNaN(y))
			{
				return y;
			}
			double num = x % y;
			if (double.IsNaN(num))
			{
				return double.NaN;
			}
			if (num == 0.0 && double.IsNegative(x))
			{
				return double.NegativeZero;
			}
			double num2 = num - Math.Abs(y) * (double)Math.Sign(x);
			if (Math.Abs(num2) == Math.Abs(num))
			{
				double num3 = x / y;
				double value = Math.Round(num3);
				if (Math.Abs(value) > Math.Abs(num3))
				{
					return num2;
				}
				return num;
			}
			else
			{
				if (Math.Abs(num2) < Math.Abs(num))
				{
					return num2;
				}
				return num;
			}
		}

		// Token: 0x0600101F RID: 4127 RVA: 0x00030D6A File Offset: 0x0002EF6A
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static sbyte Abs(sbyte value)
		{
			if (value >= 0)
			{
				return value;
			}
			return Math.AbsHelper(value);
		}

		// Token: 0x06001020 RID: 4128 RVA: 0x00030D78 File Offset: 0x0002EF78
		private static sbyte AbsHelper(sbyte value)
		{
			if (value == -128)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_NegateTwosCompNum"));
			}
			return -value;
		}

		// Token: 0x06001021 RID: 4129 RVA: 0x00030D92 File Offset: 0x0002EF92
		[__DynamicallyInvokable]
		public static short Abs(short value)
		{
			if (value >= 0)
			{
				return value;
			}
			return Math.AbsHelper(value);
		}

		// Token: 0x06001022 RID: 4130 RVA: 0x00030DA0 File Offset: 0x0002EFA0
		private static short AbsHelper(short value)
		{
			if (value == -32768)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_NegateTwosCompNum"));
			}
			return -value;
		}

		// Token: 0x06001023 RID: 4131 RVA: 0x00030DBD File Offset: 0x0002EFBD
		[__DynamicallyInvokable]
		public static int Abs(int value)
		{
			if (value >= 0)
			{
				return value;
			}
			return Math.AbsHelper(value);
		}

		// Token: 0x06001024 RID: 4132 RVA: 0x00030DCB File Offset: 0x0002EFCB
		private static int AbsHelper(int value)
		{
			if (value == -2147483648)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_NegateTwosCompNum"));
			}
			return -value;
		}

		// Token: 0x06001025 RID: 4133 RVA: 0x00030DE7 File Offset: 0x0002EFE7
		[__DynamicallyInvokable]
		public static long Abs(long value)
		{
			if (value >= 0L)
			{
				return value;
			}
			return Math.AbsHelper(value);
		}

		// Token: 0x06001026 RID: 4134 RVA: 0x00030DF6 File Offset: 0x0002EFF6
		private static long AbsHelper(long value)
		{
			if (value == -9223372036854775808L)
			{
				throw new OverflowException(Environment.GetResourceString("Overflow_NegateTwosCompNum"));
			}
			return -value;
		}

		// Token: 0x06001027 RID: 4135
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern float Abs(float value);

		// Token: 0x06001028 RID: 4136
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern double Abs(double value);

		// Token: 0x06001029 RID: 4137 RVA: 0x00030E16 File Offset: 0x0002F016
		[__DynamicallyInvokable]
		public static decimal Abs(decimal value)
		{
			return decimal.Abs(value);
		}

		// Token: 0x0600102A RID: 4138 RVA: 0x00030E1E File Offset: 0x0002F01E
		[CLSCompliant(false)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public static sbyte Max(sbyte val1, sbyte val2)
		{
			if (val1 < val2)
			{
				return val2;
			}
			return val1;
		}

		// Token: 0x0600102B RID: 4139 RVA: 0x00030E27 File Offset: 0x0002F027
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public static byte Max(byte val1, byte val2)
		{
			if (val1 < val2)
			{
				return val2;
			}
			return val1;
		}

		// Token: 0x0600102C RID: 4140 RVA: 0x00030E30 File Offset: 0x0002F030
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public static short Max(short val1, short val2)
		{
			if (val1 < val2)
			{
				return val2;
			}
			return val1;
		}

		// Token: 0x0600102D RID: 4141 RVA: 0x00030E39 File Offset: 0x0002F039
		[CLSCompliant(false)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public static ushort Max(ushort val1, ushort val2)
		{
			if (val1 < val2)
			{
				return val2;
			}
			return val1;
		}

		// Token: 0x0600102E RID: 4142 RVA: 0x00030E42 File Offset: 0x0002F042
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public static int Max(int val1, int val2)
		{
			if (val1 < val2)
			{
				return val2;
			}
			return val1;
		}

		// Token: 0x0600102F RID: 4143 RVA: 0x00030E4B File Offset: 0x0002F04B
		[CLSCompliant(false)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public static uint Max(uint val1, uint val2)
		{
			if (val1 < val2)
			{
				return val2;
			}
			return val1;
		}

		// Token: 0x06001030 RID: 4144 RVA: 0x00030E54 File Offset: 0x0002F054
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public static long Max(long val1, long val2)
		{
			if (val1 < val2)
			{
				return val2;
			}
			return val1;
		}

		// Token: 0x06001031 RID: 4145 RVA: 0x00030E5D File Offset: 0x0002F05D
		[CLSCompliant(false)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public static ulong Max(ulong val1, ulong val2)
		{
			if (val1 < val2)
			{
				return val2;
			}
			return val1;
		}

		// Token: 0x06001032 RID: 4146 RVA: 0x00030E66 File Offset: 0x0002F066
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static float Max(float val1, float val2)
		{
			if (val1 > val2)
			{
				return val1;
			}
			if (float.IsNaN(val1))
			{
				return val1;
			}
			return val2;
		}

		// Token: 0x06001033 RID: 4147 RVA: 0x00030E79 File Offset: 0x0002F079
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static double Max(double val1, double val2)
		{
			if (val1 > val2)
			{
				return val1;
			}
			if (double.IsNaN(val1))
			{
				return val1;
			}
			return val2;
		}

		// Token: 0x06001034 RID: 4148 RVA: 0x00030E8C File Offset: 0x0002F08C
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static decimal Max(decimal val1, decimal val2)
		{
			return decimal.Max(val1, val2);
		}

		// Token: 0x06001035 RID: 4149 RVA: 0x00030E95 File Offset: 0x0002F095
		[CLSCompliant(false)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public static sbyte Min(sbyte val1, sbyte val2)
		{
			if (val1 > val2)
			{
				return val2;
			}
			return val1;
		}

		// Token: 0x06001036 RID: 4150 RVA: 0x00030E9E File Offset: 0x0002F09E
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public static byte Min(byte val1, byte val2)
		{
			if (val1 > val2)
			{
				return val2;
			}
			return val1;
		}

		// Token: 0x06001037 RID: 4151 RVA: 0x00030EA7 File Offset: 0x0002F0A7
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public static short Min(short val1, short val2)
		{
			if (val1 > val2)
			{
				return val2;
			}
			return val1;
		}

		// Token: 0x06001038 RID: 4152 RVA: 0x00030EB0 File Offset: 0x0002F0B0
		[CLSCompliant(false)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public static ushort Min(ushort val1, ushort val2)
		{
			if (val1 > val2)
			{
				return val2;
			}
			return val1;
		}

		// Token: 0x06001039 RID: 4153 RVA: 0x00030EB9 File Offset: 0x0002F0B9
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public static int Min(int val1, int val2)
		{
			if (val1 > val2)
			{
				return val2;
			}
			return val1;
		}

		// Token: 0x0600103A RID: 4154 RVA: 0x00030EC2 File Offset: 0x0002F0C2
		[CLSCompliant(false)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public static uint Min(uint val1, uint val2)
		{
			if (val1 > val2)
			{
				return val2;
			}
			return val1;
		}

		// Token: 0x0600103B RID: 4155 RVA: 0x00030ECB File Offset: 0x0002F0CB
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public static long Min(long val1, long val2)
		{
			if (val1 > val2)
			{
				return val2;
			}
			return val1;
		}

		// Token: 0x0600103C RID: 4156 RVA: 0x00030ED4 File Offset: 0x0002F0D4
		[CLSCompliant(false)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[NonVersionable]
		[__DynamicallyInvokable]
		public static ulong Min(ulong val1, ulong val2)
		{
			if (val1 > val2)
			{
				return val2;
			}
			return val1;
		}

		// Token: 0x0600103D RID: 4157 RVA: 0x00030EDD File Offset: 0x0002F0DD
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static float Min(float val1, float val2)
		{
			if (val1 < val2)
			{
				return val1;
			}
			if (float.IsNaN(val1))
			{
				return val1;
			}
			return val2;
		}

		// Token: 0x0600103E RID: 4158 RVA: 0x00030EF0 File Offset: 0x0002F0F0
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static double Min(double val1, double val2)
		{
			if (val1 < val2)
			{
				return val1;
			}
			if (double.IsNaN(val1))
			{
				return val1;
			}
			return val2;
		}

		// Token: 0x0600103F RID: 4159 RVA: 0x00030F03 File Offset: 0x0002F103
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static decimal Min(decimal val1, decimal val2)
		{
			return decimal.Min(val1, val2);
		}

		// Token: 0x06001040 RID: 4160 RVA: 0x00030F0C File Offset: 0x0002F10C
		[__DynamicallyInvokable]
		public static double Log(double a, double newBase)
		{
			if (double.IsNaN(a))
			{
				return a;
			}
			if (double.IsNaN(newBase))
			{
				return newBase;
			}
			if (newBase == 1.0)
			{
				return double.NaN;
			}
			if (a != 1.0 && (newBase == 0.0 || double.IsPositiveInfinity(newBase)))
			{
				return double.NaN;
			}
			return Math.Log(a) / Math.Log(newBase);
		}

		// Token: 0x06001041 RID: 4161 RVA: 0x00030F7A File Offset: 0x0002F17A
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static int Sign(sbyte value)
		{
			if (value < 0)
			{
				return -1;
			}
			if (value > 0)
			{
				return 1;
			}
			return 0;
		}

		// Token: 0x06001042 RID: 4162 RVA: 0x00030F89 File Offset: 0x0002F189
		[__DynamicallyInvokable]
		public static int Sign(short value)
		{
			if (value < 0)
			{
				return -1;
			}
			if (value > 0)
			{
				return 1;
			}
			return 0;
		}

		// Token: 0x06001043 RID: 4163 RVA: 0x00030F98 File Offset: 0x0002F198
		[__DynamicallyInvokable]
		public static int Sign(int value)
		{
			if (value < 0)
			{
				return -1;
			}
			if (value > 0)
			{
				return 1;
			}
			return 0;
		}

		// Token: 0x06001044 RID: 4164 RVA: 0x00030FA7 File Offset: 0x0002F1A7
		[__DynamicallyInvokable]
		public static int Sign(long value)
		{
			if (value < 0L)
			{
				return -1;
			}
			if (value > 0L)
			{
				return 1;
			}
			return 0;
		}

		// Token: 0x06001045 RID: 4165 RVA: 0x00030FB8 File Offset: 0x0002F1B8
		[__DynamicallyInvokable]
		public static int Sign(float value)
		{
			if (value < 0f)
			{
				return -1;
			}
			if (value > 0f)
			{
				return 1;
			}
			if (value == 0f)
			{
				return 0;
			}
			throw new ArithmeticException(Environment.GetResourceString("Arithmetic_NaN"));
		}

		// Token: 0x06001046 RID: 4166 RVA: 0x00030FE7 File Offset: 0x0002F1E7
		[__DynamicallyInvokable]
		public static int Sign(double value)
		{
			if (value < 0.0)
			{
				return -1;
			}
			if (value > 0.0)
			{
				return 1;
			}
			if (value == 0.0)
			{
				return 0;
			}
			throw new ArithmeticException(Environment.GetResourceString("Arithmetic_NaN"));
		}

		// Token: 0x06001047 RID: 4167 RVA: 0x00031022 File Offset: 0x0002F222
		[__DynamicallyInvokable]
		public static int Sign(decimal value)
		{
			if (value < 0m)
			{
				return -1;
			}
			if (value > 0m)
			{
				return 1;
			}
			return 0;
		}

		// Token: 0x06001048 RID: 4168 RVA: 0x00031043 File Offset: 0x0002F243
		public static long BigMul(int a, int b)
		{
			return (long)a * (long)b;
		}

		// Token: 0x06001049 RID: 4169 RVA: 0x0003104A File Offset: 0x0002F24A
		public static int DivRem(int a, int b, out int result)
		{
			result = a % b;
			return a / b;
		}

		// Token: 0x0600104A RID: 4170 RVA: 0x00031054 File Offset: 0x0002F254
		public static long DivRem(long a, long b, out long result)
		{
			result = a % b;
			return a / b;
		}

		// Token: 0x040005BB RID: 1467
		private static double doubleRoundLimit = 10000000000000000.0;

		// Token: 0x040005BC RID: 1468
		private const int maxRoundingDigits = 15;

		// Token: 0x040005BD RID: 1469
		private static double[] roundPower10Double = new double[]
		{
			1.0, 10.0, 100.0, 1000.0, 10000.0, 100000.0, 1000000.0, 10000000.0, 100000000.0, 1000000000.0,
			10000000000.0, 100000000000.0, 1000000000000.0, 10000000000000.0, 100000000000000.0, 1000000000000000.0
		};

		// Token: 0x040005BE RID: 1470
		[__DynamicallyInvokable]
		public const double PI = 3.141592653589793;

		// Token: 0x040005BF RID: 1471
		[__DynamicallyInvokable]
		public const double E = 2.718281828459045;
	}
}
