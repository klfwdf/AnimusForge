using System;
using System.Text;

namespace System.Globalization
{
	// Token: 0x020003DF RID: 991
	internal class HebrewNumber
	{
		// Token: 0x060032E0 RID: 13024 RVA: 0x000C445B File Offset: 0x000C265B
		private HebrewNumber()
		{
		}

		// Token: 0x060032E1 RID: 13025 RVA: 0x000C4464 File Offset: 0x000C2664
		internal static string ToString(int Number)
		{
			char c = '\0';
			StringBuilder stringBuilder = new StringBuilder();
			if (Number > 5000)
			{
				Number -= 5000;
			}
			int num = Number / 100;
			if (num > 0)
			{
				Number -= num * 100;
				for (int i = 0; i < num / 4; i++)
				{
					stringBuilder.Append('ת');
				}
				int num2 = num % 4;
				if (num2 > 0)
				{
					stringBuilder.Append((char)(1510 + num2));
				}
			}
			int num3 = Number / 10;
			Number %= 10;
			switch (num3)
			{
			case 0:
				c = '\0';
				break;
			case 1:
				c = 'י';
				break;
			case 2:
				c = 'כ';
				break;
			case 3:
				c = 'ל';
				break;
			case 4:
				c = 'מ';
				break;
			case 5:
				c = 'נ';
				break;
			case 6:
				c = 'ס';
				break;
			case 7:
				c = 'ע';
				break;
			case 8:
				c = 'פ';
				break;
			case 9:
				c = 'צ';
				break;
			}
			char c2 = (char)((Number > 0) ? (1488 + Number - 1) : 0);
			if (c2 == 'ה' && c == 'י')
			{
				c2 = 'ו';
				c = 'ט';
			}
			if (c2 == 'ו' && c == 'י')
			{
				c2 = 'ז';
				c = 'ט';
			}
			if (c != '\0')
			{
				stringBuilder.Append(c);
			}
			if (c2 != '\0')
			{
				stringBuilder.Append(c2);
			}
			if (stringBuilder.Length > 1)
			{
				stringBuilder.Insert(stringBuilder.Length - 1, '"');
			}
			else
			{
				stringBuilder.Append('\'');
			}
			return stringBuilder.ToString();
		}

		// Token: 0x060032E2 RID: 13026 RVA: 0x000C45F0 File Offset: 0x000C27F0
		internal static HebrewNumberParsingState ParseByChar(char ch, ref HebrewNumberParsingContext context)
		{
			HebrewNumber.HebrewToken hebrewToken;
			if (ch == '\'')
			{
				hebrewToken = HebrewNumber.HebrewToken.SingleQuote;
			}
			else if (ch == '"')
			{
				hebrewToken = HebrewNumber.HebrewToken.DoubleQuote;
			}
			else
			{
				int num = (int)(ch - 'א');
				if (num < 0 || num >= HebrewNumber.HebrewValues.Length)
				{
					return HebrewNumberParsingState.NotHebrewDigit;
				}
				hebrewToken = HebrewNumber.HebrewValues[num].token;
				if (hebrewToken == HebrewNumber.HebrewToken.Invalid)
				{
					return HebrewNumberParsingState.NotHebrewDigit;
				}
				context.result += HebrewNumber.HebrewValues[num].value;
			}
			context.state = HebrewNumber.NumberPasingState[(int)context.state][(int)hebrewToken];
			if (context.state == HebrewNumber.HS._err)
			{
				return HebrewNumberParsingState.InvalidHebrewNumber;
			}
			if (context.state == HebrewNumber.HS.END)
			{
				return HebrewNumberParsingState.FoundEndOfHebrewNumber;
			}
			return HebrewNumberParsingState.ContinueParsing;
		}

		// Token: 0x060032E3 RID: 13027 RVA: 0x000C467F File Offset: 0x000C287F
		internal static bool IsDigit(char ch)
		{
			if (ch >= 'א' && ch <= HebrewNumber.maxHebrewNumberCh)
			{
				return HebrewNumber.HebrewValues[(int)(ch - 'א')].value >= 0;
			}
			return ch == '\'' || ch == '"';
		}

		// Token: 0x04001693 RID: 5779
		private static HebrewNumber.HebrewValue[] HebrewValues = new HebrewNumber.HebrewValue[]
		{
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit1, 1),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit1, 2),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit1, 3),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit1, 4),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit1, 5),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit6_7, 6),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit6_7, 7),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit1, 8),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit9, 9),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit10, 10),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Invalid, -1),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit10, 20),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit10, 30),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Invalid, -1),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit10, 40),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Invalid, -1),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit10, 50),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit10, 60),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit10, 70),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Invalid, -1),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit10, 80),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Invalid, -1),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit10, 90),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit100, 100),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit200_300, 200),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit200_300, 300),
			new HebrewNumber.HebrewValue(HebrewNumber.HebrewToken.Digit400, 400)
		};

		// Token: 0x04001694 RID: 5780
		private const int minHebrewNumberCh = 1488;

		// Token: 0x04001695 RID: 5781
		private static char maxHebrewNumberCh = (char)(1488 + HebrewNumber.HebrewValues.Length - 1);

		// Token: 0x04001696 RID: 5782
		private static readonly HebrewNumber.HS[][] NumberPasingState = new HebrewNumber.HS[][]
		{
			new HebrewNumber.HS[]
			{
				HebrewNumber.HS.S400,
				HebrewNumber.HS.X00,
				HebrewNumber.HS.X00,
				HebrewNumber.HS.X0,
				HebrewNumber.HS.X,
				HebrewNumber.HS.X,
				HebrewNumber.HS.X,
				HebrewNumber.HS.S9,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err
			},
			new HebrewNumber.HS[]
			{
				HebrewNumber.HS.S400_400,
				HebrewNumber.HS.S400_X00,
				HebrewNumber.HS.S400_X00,
				HebrewNumber.HS.S400_X0,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS.X00_S9,
				HebrewNumber.HS.END,
				HebrewNumber.HS.S400_DQ
			},
			new HebrewNumber.HS[]
			{
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS.S400_400_100,
				HebrewNumber.HS.S400_X0,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS.X00_S9,
				HebrewNumber.HS._err,
				HebrewNumber.HS.S400_400_DQ
			},
			new HebrewNumber.HS[]
			{
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS.S400_X00_X0,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS.X00_S9,
				HebrewNumber.HS._err,
				HebrewNumber.HS.X00_DQ
			},
			new HebrewNumber.HS[]
			{
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS.X0_DQ
			},
			new HebrewNumber.HS[]
			{
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS.END,
				HebrewNumber.HS.END,
				HebrewNumber.HS.END,
				HebrewNumber.HS.END,
				HebrewNumber.HS.END,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err
			},
			new HebrewNumber.HS[]
			{
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS.X0_DQ
			},
			new HebrewNumber.HS[]
			{
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS.END,
				HebrewNumber.HS.END,
				HebrewNumber.HS.END,
				HebrewNumber.HS.END,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err
			},
			new HebrewNumber.HS[]
			{
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS.END,
				HebrewNumber.HS._err
			},
			new HebrewNumber.HS[]
			{
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS.END,
				HebrewNumber.HS.X0_DQ
			},
			new HebrewNumber.HS[]
			{
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS.S400_X0,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS.X00_S9,
				HebrewNumber.HS.END,
				HebrewNumber.HS.X00_DQ
			},
			new HebrewNumber.HS[]
			{
				HebrewNumber.HS.END,
				HebrewNumber.HS.END,
				HebrewNumber.HS.END,
				HebrewNumber.HS.END,
				HebrewNumber.HS.END,
				HebrewNumber.HS.END,
				HebrewNumber.HS.END,
				HebrewNumber.HS.END,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err
			},
			new HebrewNumber.HS[]
			{
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS.END,
				HebrewNumber.HS.END,
				HebrewNumber.HS.END,
				HebrewNumber.HS.END,
				HebrewNumber.HS.END,
				HebrewNumber.HS.END,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err
			},
			new HebrewNumber.HS[]
			{
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS.S400_X00_X0,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS.X00_S9,
				HebrewNumber.HS._err,
				HebrewNumber.HS.X00_DQ
			},
			new HebrewNumber.HS[]
			{
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS.END,
				HebrewNumber.HS.S9_DQ
			},
			new HebrewNumber.HS[]
			{
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS.S9_DQ
			},
			new HebrewNumber.HS[]
			{
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS.END,
				HebrewNumber.HS.END,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err,
				HebrewNumber.HS._err
			}
		};

		// Token: 0x02000B7E RID: 2942
		private enum HebrewToken
		{
			// Token: 0x040034B9 RID: 13497
			Invalid = -1,
			// Token: 0x040034BA RID: 13498
			Digit400,
			// Token: 0x040034BB RID: 13499
			Digit200_300,
			// Token: 0x040034BC RID: 13500
			Digit100,
			// Token: 0x040034BD RID: 13501
			Digit10,
			// Token: 0x040034BE RID: 13502
			Digit1,
			// Token: 0x040034BF RID: 13503
			Digit6_7,
			// Token: 0x040034C0 RID: 13504
			Digit7,
			// Token: 0x040034C1 RID: 13505
			Digit9,
			// Token: 0x040034C2 RID: 13506
			SingleQuote,
			// Token: 0x040034C3 RID: 13507
			DoubleQuote
		}

		// Token: 0x02000B7F RID: 2943
		private class HebrewValue
		{
			// Token: 0x06006C5C RID: 27740 RVA: 0x00177533 File Offset: 0x00175733
			internal HebrewValue(HebrewNumber.HebrewToken token, int value)
			{
				this.token = token;
				this.value = value;
			}

			// Token: 0x040034C4 RID: 13508
			internal HebrewNumber.HebrewToken token;

			// Token: 0x040034C5 RID: 13509
			internal int value;
		}

		// Token: 0x02000B80 RID: 2944
		internal enum HS
		{
			// Token: 0x040034C7 RID: 13511
			_err = -1,
			// Token: 0x040034C8 RID: 13512
			Start,
			// Token: 0x040034C9 RID: 13513
			S400,
			// Token: 0x040034CA RID: 13514
			S400_400,
			// Token: 0x040034CB RID: 13515
			S400_X00,
			// Token: 0x040034CC RID: 13516
			S400_X0,
			// Token: 0x040034CD RID: 13517
			X00_DQ,
			// Token: 0x040034CE RID: 13518
			S400_X00_X0,
			// Token: 0x040034CF RID: 13519
			X0_DQ,
			// Token: 0x040034D0 RID: 13520
			X,
			// Token: 0x040034D1 RID: 13521
			X0,
			// Token: 0x040034D2 RID: 13522
			X00,
			// Token: 0x040034D3 RID: 13523
			S400_DQ,
			// Token: 0x040034D4 RID: 13524
			S400_400_DQ,
			// Token: 0x040034D5 RID: 13525
			S400_400_100,
			// Token: 0x040034D6 RID: 13526
			S9,
			// Token: 0x040034D7 RID: 13527
			X00_S9,
			// Token: 0x040034D8 RID: 13528
			S9_DQ,
			// Token: 0x040034D9 RID: 13529
			END = 100
		}
	}
}
