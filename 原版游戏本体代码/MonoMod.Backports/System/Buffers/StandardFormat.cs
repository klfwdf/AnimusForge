using System;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
	// Token: 0x02000034 RID: 52
	public readonly struct StandardFormat : IEquatable<StandardFormat>
	{
		// Token: 0x1700002B RID: 43
		// (get) Token: 0x06000205 RID: 517 RVA: 0x0000B1B2 File Offset: 0x000093B2
		public char Symbol
		{
			get
			{
				return (char)this._format;
			}
		}

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x06000206 RID: 518 RVA: 0x0000B1BA File Offset: 0x000093BA
		public byte Precision
		{
			get
			{
				return this._precision;
			}
		}

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x06000207 RID: 519 RVA: 0x0000B1C2 File Offset: 0x000093C2
		public bool HasPrecision
		{
			get
			{
				return this._precision != byte.MaxValue;
			}
		}

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x06000208 RID: 520 RVA: 0x0000B1D4 File Offset: 0x000093D4
		public bool IsDefault
		{
			get
			{
				return this._format == 0 && this._precision == 0;
			}
		}

		// Token: 0x06000209 RID: 521 RVA: 0x0000B1E9 File Offset: 0x000093E9
		public StandardFormat(char symbol, byte precision = 255)
		{
			if (precision != 255 && precision > 99)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_PrecisionTooLarge();
			}
			if (symbol != (char)((byte)symbol))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_SymbolDoesNotFit();
			}
			this._format = (byte)symbol;
			this._precision = precision;
		}

		// Token: 0x0600020A RID: 522 RVA: 0x0000B216 File Offset: 0x00009416
		public static implicit operator StandardFormat(char symbol)
		{
			return new StandardFormat(symbol, byte.MaxValue);
		}

		// Token: 0x0600020B RID: 523 RVA: 0x0000B224 File Offset: 0x00009424
		public unsafe static StandardFormat Parse(ReadOnlySpan<char> format)
		{
			if (format.Length == 0)
			{
				return default(StandardFormat);
			}
			char symbol = (char)(*format[0]);
			byte precision;
			if (format.Length == 1)
			{
				precision = byte.MaxValue;
			}
			else
			{
				uint num = 0U;
				for (int i = 1; i < format.Length; i++)
				{
					uint num2 = (uint)(*format[i] - 48);
					if (num2 > 9U)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(32, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Cannot parse precision (max is ");
						defaultInterpolatedStringHandler.AppendFormatted<byte>(99);
						defaultInterpolatedStringHandler.AppendLiteral(")");
						throw new FormatException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					num = num * 10U + num2;
					if (num > 99U)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(37, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Precision is larger than the maximum ");
						defaultInterpolatedStringHandler.AppendFormatted<byte>(99);
						throw new FormatException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
				}
				precision = (byte)num;
			}
			return new StandardFormat(symbol, precision);
		}

		// Token: 0x0600020C RID: 524 RVA: 0x0000B314 File Offset: 0x00009514
		[NullableContext(2)]
		public static StandardFormat Parse(string format)
		{
			if (format != null)
			{
				return StandardFormat.Parse(format.AsSpan());
			}
			return default(StandardFormat);
		}

		// Token: 0x0600020D RID: 525 RVA: 0x0000B33C File Offset: 0x0000953C
		[NullableContext(2)]
		public override bool Equals(object obj)
		{
			if (obj is StandardFormat)
			{
				StandardFormat other = (StandardFormat)obj;
				return this.Equals(other);
			}
			return false;
		}

		// Token: 0x0600020E RID: 526 RVA: 0x0000B364 File Offset: 0x00009564
		public override int GetHashCode()
		{
			return this._format.GetHashCode() ^ this._precision.GetHashCode();
		}

		// Token: 0x0600020F RID: 527 RVA: 0x0000B38E File Offset: 0x0000958E
		public bool Equals(StandardFormat other)
		{
			return this._format == other._format && this._precision == other._precision;
		}

		// Token: 0x06000210 RID: 528 RVA: 0x0000B3B0 File Offset: 0x000095B0
		[NullableContext(1)]
		public unsafe override string ToString()
		{
			char* ptr = stackalloc char[(UIntPtr)8];
			int length = 0;
			char symbol = this.Symbol;
			if (symbol != '\0')
			{
				ptr[(IntPtr)(length++) * 2] = symbol;
				byte b = this.Precision;
				if (b != 255)
				{
					if (b >= 100)
					{
						ptr[(IntPtr)(length++) * 2] = (char)(48 + b / 100 % 10);
						b %= 100;
					}
					if (b >= 10)
					{
						ptr[(IntPtr)(length++) * 2] = (char)(48 + b / 10 % 10);
						b %= 10;
					}
					ptr[(IntPtr)(length++) * 2] = (char)(48 + b);
				}
			}
			return new string(ptr, 0, length);
		}

		// Token: 0x06000211 RID: 529 RVA: 0x0000B443 File Offset: 0x00009643
		public static bool operator ==(StandardFormat left, StandardFormat right)
		{
			return left.Equals(right);
		}

		// Token: 0x06000212 RID: 530 RVA: 0x0000B44D File Offset: 0x0000964D
		public static bool operator !=(StandardFormat left, StandardFormat right)
		{
			return !left.Equals(right);
		}

		// Token: 0x0400006B RID: 107
		public const byte NoPrecision = 255;

		// Token: 0x0400006C RID: 108
		public const byte MaxPrecision = 99;

		// Token: 0x0400006D RID: 109
		private readonly byte _format;

		// Token: 0x0400006E RID: 110
		private readonly byte _precision;
	}
}
