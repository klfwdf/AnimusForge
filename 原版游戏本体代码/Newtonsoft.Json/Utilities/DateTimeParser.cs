using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Utilities
{
	// Token: 0x0200004C RID: 76
	[NullableContext(1)]
	[Nullable(0)]
	internal struct DateTimeParser
	{
		// Token: 0x06000485 RID: 1157 RVA: 0x0001279E File Offset: 0x0001099E
		public bool Parse(char[] text, int startIndex, int length)
		{
			this._text = text;
			this._end = startIndex + length;
			return this.ParseDate(startIndex) && this.ParseChar(DateTimeParser.Lzyyyy_MM_dd + startIndex, 'T') && this.ParseTimeAndZoneAndWhitespace(DateTimeParser.Lzyyyy_MM_ddT + startIndex);
		}

		// Token: 0x06000486 RID: 1158 RVA: 0x000127DC File Offset: 0x000109DC
		private bool ParseDate(int start)
		{
			return this.Parse4Digit(start, out this.Year) && 1 <= this.Year && this.ParseChar(start + DateTimeParser.Lzyyyy, '-') && this.Parse2Digit(start + DateTimeParser.Lzyyyy_, out this.Month) && 1 <= this.Month && this.Month <= 12 && this.ParseChar(start + DateTimeParser.Lzyyyy_MM, '-') && this.Parse2Digit(start + DateTimeParser.Lzyyyy_MM_, out this.Day) && 1 <= this.Day && this.Day <= DateTime.DaysInMonth(this.Year, this.Month);
		}

		// Token: 0x06000487 RID: 1159 RVA: 0x0001288D File Offset: 0x00010A8D
		private bool ParseTimeAndZoneAndWhitespace(int start)
		{
			return this.ParseTime(ref start) && this.ParseZone(start);
		}

		// Token: 0x06000488 RID: 1160 RVA: 0x000128A4 File Offset: 0x00010AA4
		private bool ParseTime(ref int start)
		{
			if (!this.Parse2Digit(start, out this.Hour) || this.Hour > 24 || !this.ParseChar(start + DateTimeParser.LzHH, ':') || !this.Parse2Digit(start + DateTimeParser.LzHH_, out this.Minute) || this.Minute >= 60 || !this.ParseChar(start + DateTimeParser.LzHH_mm, ':') || !this.Parse2Digit(start + DateTimeParser.LzHH_mm_, out this.Second) || this.Second >= 60 || (this.Hour == 24 && (this.Minute != 0 || this.Second != 0)))
			{
				return false;
			}
			start += DateTimeParser.LzHH_mm_ss;
			if (this.ParseChar(start, '.'))
			{
				this.Fraction = 0;
				int num = 0;
				for (;;)
				{
					int num2 = start + 1;
					start = num2;
					if (num2 >= this._end || num >= 7)
					{
						break;
					}
					int num3 = (int)(this._text[start] - '0');
					if (num3 < 0 || num3 > 9)
					{
						break;
					}
					this.Fraction = this.Fraction * 10 + num3;
					num++;
				}
				if (num < 7)
				{
					if (num == 0)
					{
						return false;
					}
					this.Fraction *= DateTimeParser.Power10[7 - num];
				}
				if (this.Hour == 24 && this.Fraction != 0)
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000489 RID: 1161 RVA: 0x000129E4 File Offset: 0x00010BE4
		private bool ParseZone(int start)
		{
			if (start < this._end)
			{
				char c = this._text[start];
				if (c == 'Z' || c == 'z')
				{
					this.Zone = ParserTimeZone.Utc;
					start++;
				}
				else
				{
					if (start + 2 < this._end && this.Parse2Digit(start + DateTimeParser.Lz_, out this.ZoneHour) && this.ZoneHour <= 99)
					{
						if (c != '+')
						{
							if (c == '-')
							{
								this.Zone = ParserTimeZone.LocalWestOfUtc;
								start += DateTimeParser.Lz_zz;
							}
						}
						else
						{
							this.Zone = ParserTimeZone.LocalEastOfUtc;
							start += DateTimeParser.Lz_zz;
						}
					}
					if (start < this._end)
					{
						if (this.ParseChar(start, ':'))
						{
							start++;
							if (start + 1 < this._end && this.Parse2Digit(start, out this.ZoneMinute) && this.ZoneMinute <= 99)
							{
								start += 2;
							}
						}
						else if (start + 1 < this._end && this.Parse2Digit(start, out this.ZoneMinute) && this.ZoneMinute <= 99)
						{
							start += 2;
						}
					}
				}
			}
			return start == this._end;
		}

		// Token: 0x0600048A RID: 1162 RVA: 0x00012AF0 File Offset: 0x00010CF0
		private bool Parse4Digit(int start, out int num)
		{
			if (start + 3 < this._end)
			{
				int num2 = (int)(this._text[start] - '0');
				int num3 = (int)(this._text[start + 1] - '0');
				int num4 = (int)(this._text[start + 2] - '0');
				int num5 = (int)(this._text[start + 3] - '0');
				if (0 <= num2 && num2 < 10 && 0 <= num3 && num3 < 10 && 0 <= num4 && num4 < 10 && 0 <= num5 && num5 < 10)
				{
					num = ((num2 * 10 + num3) * 10 + num4) * 10 + num5;
					return true;
				}
			}
			num = 0;
			return false;
		}

		// Token: 0x0600048B RID: 1163 RVA: 0x00012B7C File Offset: 0x00010D7C
		private bool Parse2Digit(int start, out int num)
		{
			if (start + 1 < this._end)
			{
				int num2 = (int)(this._text[start] - '0');
				int num3 = (int)(this._text[start + 1] - '0');
				if (0 <= num2 && num2 < 10 && 0 <= num3 && num3 < 10)
				{
					num = num2 * 10 + num3;
					return true;
				}
			}
			num = 0;
			return false;
		}

		// Token: 0x0600048C RID: 1164 RVA: 0x00012BCE File Offset: 0x00010DCE
		private bool ParseChar(int start, char ch)
		{
			return start < this._end && this._text[start] == ch;
		}

		// Token: 0x040001B0 RID: 432
		public int Year;

		// Token: 0x040001B1 RID: 433
		public int Month;

		// Token: 0x040001B2 RID: 434
		public int Day;

		// Token: 0x040001B3 RID: 435
		public int Hour;

		// Token: 0x040001B4 RID: 436
		public int Minute;

		// Token: 0x040001B5 RID: 437
		public int Second;

		// Token: 0x040001B6 RID: 438
		public int Fraction;

		// Token: 0x040001B7 RID: 439
		public int ZoneHour;

		// Token: 0x040001B8 RID: 440
		public int ZoneMinute;

		// Token: 0x040001B9 RID: 441
		public ParserTimeZone Zone;

		// Token: 0x040001BA RID: 442
		private char[] _text;

		// Token: 0x040001BB RID: 443
		private int _end;

		// Token: 0x040001BC RID: 444
		private static readonly int[] Power10 = new int[] { -1, 10, 100, 1000, 10000, 100000, 1000000 };

		// Token: 0x040001BD RID: 445
		private static readonly int Lzyyyy = "yyyy".Length;

		// Token: 0x040001BE RID: 446
		private static readonly int Lzyyyy_ = "yyyy-".Length;

		// Token: 0x040001BF RID: 447
		private static readonly int Lzyyyy_MM = "yyyy-MM".Length;

		// Token: 0x040001C0 RID: 448
		private static readonly int Lzyyyy_MM_ = "yyyy-MM-".Length;

		// Token: 0x040001C1 RID: 449
		private static readonly int Lzyyyy_MM_dd = "yyyy-MM-dd".Length;

		// Token: 0x040001C2 RID: 450
		private static readonly int Lzyyyy_MM_ddT = "yyyy-MM-ddT".Length;

		// Token: 0x040001C3 RID: 451
		private static readonly int LzHH = "HH".Length;

		// Token: 0x040001C4 RID: 452
		private static readonly int LzHH_ = "HH:".Length;

		// Token: 0x040001C5 RID: 453
		private static readonly int LzHH_mm = "HH:mm".Length;

		// Token: 0x040001C6 RID: 454
		private static readonly int LzHH_mm_ = "HH:mm:".Length;

		// Token: 0x040001C7 RID: 455
		private static readonly int LzHH_mm_ss = "HH:mm:ss".Length;

		// Token: 0x040001C8 RID: 456
		private static readonly int Lz_ = "-".Length;

		// Token: 0x040001C9 RID: 457
		private static readonly int Lz_zz = "-zz".Length;

		// Token: 0x040001CA RID: 458
		private const short MaxFractionDigits = 7;
	}
}
