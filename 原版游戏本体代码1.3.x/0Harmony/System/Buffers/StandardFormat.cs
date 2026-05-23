using System;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
	// Token: 0x0200049D RID: 1181
	internal readonly struct StandardFormat : IEquatable<StandardFormat>
	{
		// Token: 0x170005D1 RID: 1489
		// (get) Token: 0x06001A67 RID: 6759 RVA: 0x000563C7 File Offset: 0x000545C7
		public char Symbol
		{
			get
			{
				return (char)this._format;
			}
		}

		// Token: 0x170005D2 RID: 1490
		// (get) Token: 0x06001A68 RID: 6760 RVA: 0x000563CF File Offset: 0x000545CF
		public byte Precision
		{
			get
			{
				return this._precision;
			}
		}

		// Token: 0x170005D3 RID: 1491
		// (get) Token: 0x06001A69 RID: 6761 RVA: 0x000563D7 File Offset: 0x000545D7
		public bool HasPrecision
		{
			get
			{
				return this._precision != byte.MaxValue;
			}
		}

		// Token: 0x170005D4 RID: 1492
		// (get) Token: 0x06001A6A RID: 6762 RVA: 0x000563E9 File Offset: 0x000545E9
		public bool IsDefault
		{
			get
			{
				return this._format == 0 && this._precision == 0;
			}
		}

		// Token: 0x06001A6B RID: 6763 RVA: 0x000563FE File Offset: 0x000545FE
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

		// Token: 0x06001A6C RID: 6764 RVA: 0x0005642B File Offset: 0x0005462B
		public static implicit operator StandardFormat(char symbol)
		{
			return new StandardFormat(symbol, byte.MaxValue);
		}

		// Token: 0x06001A6D RID: 6765 RVA: 0x00056438 File Offset: 0x00054638
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
				uint parsedPrecision = 0U;
				for (int srcIndex = 1; srcIndex < format.Length; srcIndex++)
				{
					uint digit = (uint)(*format[srcIndex] - 48);
					if (digit > 9U)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(32, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Cannot parse precision (max is ");
						defaultInterpolatedStringHandler.AppendFormatted<byte>(99);
						defaultInterpolatedStringHandler.AppendLiteral(")");
						throw new FormatException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					parsedPrecision = parsedPrecision * 10U + digit;
					if (parsedPrecision > 99U)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(37, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Precision is larger than the maximum ");
						defaultInterpolatedStringHandler.AppendFormatted<byte>(99);
						throw new FormatException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
				}
				precision = (byte)parsedPrecision;
			}
			return new StandardFormat(symbol, precision);
		}

		// Token: 0x06001A6E RID: 6766 RVA: 0x00056528 File Offset: 0x00054728
		[NullableContext(2)]
		public static StandardFormat Parse(string format)
		{
			if (format != null)
			{
				return StandardFormat.Parse(format.AsSpan());
			}
			return default(StandardFormat);
		}

		// Token: 0x06001A6F RID: 6767 RVA: 0x00056550 File Offset: 0x00054750
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

		// Token: 0x06001A70 RID: 6768 RVA: 0x00056578 File Offset: 0x00054778
		public override int GetHashCode()
		{
			return this._format.GetHashCode() ^ this._precision.GetHashCode();
		}

		// Token: 0x06001A71 RID: 6769 RVA: 0x000565A2 File Offset: 0x000547A2
		public bool Equals(StandardFormat other)
		{
			return this._format == other._format && this._precision == other._precision;
		}

		// Token: 0x06001A72 RID: 6770 RVA: 0x000565C4 File Offset: 0x000547C4
		[NullableContext(1)]
		public unsafe override string ToString()
		{
			char* pBuffer = stackalloc char[(UIntPtr)8];
			int dstIndex = 0;
			char symbol = this.Symbol;
			if (symbol != '\0')
			{
				pBuffer[(IntPtr)(dstIndex++) * 2] = symbol;
				byte precision = this.Precision;
				if (precision != 255)
				{
					if (precision >= 100)
					{
						pBuffer[(IntPtr)(dstIndex++) * 2] = (char)(48 + precision / 100 % 10);
						precision %= 100;
					}
					if (precision >= 10)
					{
						pBuffer[(IntPtr)(dstIndex++) * 2] = (char)(48 + precision / 10 % 10);
						precision %= 10;
					}
					pBuffer[(IntPtr)(dstIndex++) * 2] = (char)(48 + precision);
				}
			}
			return new string(pBuffer, 0, dstIndex);
		}

		// Token: 0x06001A73 RID: 6771 RVA: 0x00056657 File Offset: 0x00054857
		public static bool operator ==(StandardFormat left, StandardFormat right)
		{
			return left.Equals(right);
		}

		// Token: 0x06001A74 RID: 6772 RVA: 0x00056661 File Offset: 0x00054861
		public static bool operator !=(StandardFormat left, StandardFormat right)
		{
			return !left.Equals(right);
		}

		// Token: 0x040010F9 RID: 4345
		public const byte NoPrecision = 255;

		// Token: 0x040010FA RID: 4346
		public const byte MaxPrecision = 99;

		// Token: 0x040010FB RID: 4347
		private readonly byte _format;

		// Token: 0x040010FC RID: 4348
		private readonly byte _precision;
	}
}
