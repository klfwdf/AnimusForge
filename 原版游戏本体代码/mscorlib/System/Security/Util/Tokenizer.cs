using System;
using System.IO;
using System.Text;

namespace System.Security.Util
{
	// Token: 0x02000385 RID: 901
	internal sealed class Tokenizer
	{
		// Token: 0x06002CC4 RID: 11460 RVA: 0x000A8628 File Offset: 0x000A6828
		internal void BasicInitialization()
		{
			this.LineNo = 1;
			this._inProcessingTag = 0;
			this._inSavedCharacter = -1;
			this._inIndex = 0;
			this._inSize = 0;
			this._inNestedSize = 0;
			this._inNestedIndex = 0;
			this._inTokenSource = Tokenizer.TokenSource.Other;
			this._maker = SharedStatics.GetSharedStringMaker();
		}

		// Token: 0x06002CC5 RID: 11461 RVA: 0x000A8678 File Offset: 0x000A6878
		public void Recycle()
		{
			SharedStatics.ReleaseSharedStringMaker(ref this._maker);
		}

		// Token: 0x06002CC6 RID: 11462 RVA: 0x000A8685 File Offset: 0x000A6885
		internal Tokenizer(string input)
		{
			this.BasicInitialization();
			this._inString = input;
			this._inSize = input.Length;
			this._inTokenSource = Tokenizer.TokenSource.String;
		}

		// Token: 0x06002CC7 RID: 11463 RVA: 0x000A86AD File Offset: 0x000A68AD
		internal Tokenizer(string input, string[] searchStrings, string[] replaceStrings)
		{
			this.BasicInitialization();
			this._inString = input;
			this._inSize = this._inString.Length;
			this._inTokenSource = Tokenizer.TokenSource.NestedStrings;
			this._searchStrings = searchStrings;
			this._replaceStrings = replaceStrings;
		}

		// Token: 0x06002CC8 RID: 11464 RVA: 0x000A86E8 File Offset: 0x000A68E8
		internal Tokenizer(byte[] array, Tokenizer.ByteTokenEncoding encoding, int startIndex)
		{
			this.BasicInitialization();
			this._inBytes = array;
			this._inSize = array.Length;
			this._inIndex = startIndex;
			switch (encoding)
			{
			case Tokenizer.ByteTokenEncoding.UnicodeTokens:
				this._inTokenSource = Tokenizer.TokenSource.UnicodeByteArray;
				return;
			case Tokenizer.ByteTokenEncoding.UTF8Tokens:
				this._inTokenSource = Tokenizer.TokenSource.UTF8ByteArray;
				return;
			case Tokenizer.ByteTokenEncoding.ByteTokens:
				this._inTokenSource = Tokenizer.TokenSource.ASCIIByteArray;
				return;
			default:
				throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", new object[] { (int)encoding }));
			}
		}

		// Token: 0x06002CC9 RID: 11465 RVA: 0x000A8762 File Offset: 0x000A6962
		internal Tokenizer(char[] array)
		{
			this.BasicInitialization();
			this._inChars = array;
			this._inSize = array.Length;
			this._inTokenSource = Tokenizer.TokenSource.CharArray;
		}

		// Token: 0x06002CCA RID: 11466 RVA: 0x000A8787 File Offset: 0x000A6987
		internal Tokenizer(StreamReader input)
		{
			this.BasicInitialization();
			this._inTokenReader = new Tokenizer.StreamTokenReader(input);
		}

		// Token: 0x06002CCB RID: 11467 RVA: 0x000A87A4 File Offset: 0x000A69A4
		internal void ChangeFormat(Encoding encoding)
		{
			if (encoding == null)
			{
				return;
			}
			Tokenizer.TokenSource inTokenSource = this._inTokenSource;
			if (inTokenSource > Tokenizer.TokenSource.ASCIIByteArray)
			{
				if (inTokenSource - Tokenizer.TokenSource.CharArray <= 2)
				{
					return;
				}
			}
			else
			{
				if (encoding == Encoding.Unicode)
				{
					this._inTokenSource = Tokenizer.TokenSource.UnicodeByteArray;
					return;
				}
				if (encoding == Encoding.UTF8)
				{
					this._inTokenSource = Tokenizer.TokenSource.UTF8ByteArray;
					return;
				}
				if (encoding == Encoding.ASCII)
				{
					this._inTokenSource = Tokenizer.TokenSource.ASCIIByteArray;
					return;
				}
			}
			Tokenizer.TokenSource inTokenSource2 = this._inTokenSource;
			Stream stream;
			if (inTokenSource2 > Tokenizer.TokenSource.ASCIIByteArray)
			{
				if (inTokenSource2 - Tokenizer.TokenSource.CharArray <= 2)
				{
					return;
				}
				Tokenizer.StreamTokenReader streamTokenReader = this._inTokenReader as Tokenizer.StreamTokenReader;
				if (streamTokenReader == null)
				{
					return;
				}
				stream = streamTokenReader._in.BaseStream;
				string s = new string(' ', streamTokenReader.NumCharEncountered);
				stream.Position = (long)streamTokenReader._in.CurrentEncoding.GetByteCount(s);
			}
			else
			{
				stream = new MemoryStream(this._inBytes, this._inIndex, this._inSize - this._inIndex);
			}
			this._inTokenReader = new Tokenizer.StreamTokenReader(new StreamReader(stream, encoding));
			this._inTokenSource = Tokenizer.TokenSource.Other;
		}

		// Token: 0x06002CCC RID: 11468 RVA: 0x000A8890 File Offset: 0x000A6A90
		internal void GetTokens(TokenizerStream stream, int maxNum, bool endAfterKet)
		{
			while (maxNum == -1 || stream.GetTokenCount() < maxNum)
			{
				int num = 0;
				bool flag = false;
				bool flag2 = false;
				Tokenizer.StringMaker maker = this._maker;
				maker._outStringBuilder = null;
				maker._outIndex = 0;
				int num2;
				for (;;)
				{
					if (this._inSavedCharacter != -1)
					{
						num2 = this._inSavedCharacter;
						this._inSavedCharacter = -1;
					}
					else
					{
						switch (this._inTokenSource)
						{
						case Tokenizer.TokenSource.UnicodeByteArray:
							if (this._inIndex + 1 >= this._inSize)
							{
								goto Block_3;
							}
							num2 = ((int)this._inBytes[this._inIndex + 1] << 8) + (int)this._inBytes[this._inIndex];
							this._inIndex += 2;
							break;
						case Tokenizer.TokenSource.UTF8ByteArray:
						{
							if (this._inIndex >= this._inSize)
							{
								goto Block_4;
							}
							byte[] inBytes = this._inBytes;
							int num3 = this._inIndex;
							this._inIndex = num3 + 1;
							num2 = inBytes[num3];
							if ((num2 & 128) != 0)
							{
								switch ((num2 & 240) >> 4)
								{
								case 8:
								case 9:
								case 10:
								case 11:
									goto IL_12D;
								case 12:
								case 13:
									num2 &= 31;
									num = 2;
									break;
								case 14:
									num2 &= 15;
									num = 3;
									break;
								case 15:
									goto IL_14B;
								}
								if (this._inIndex >= this._inSize)
								{
									goto Block_7;
								}
								byte[] inBytes2 = this._inBytes;
								num3 = this._inIndex;
								this._inIndex = num3 + 1;
								byte b = inBytes2[num3];
								if ((b & 192) != 128)
								{
									goto Block_8;
								}
								num2 = (num2 << 6) | (int)(b & 63);
								if (num != 2)
								{
									if (this._inIndex >= this._inSize)
									{
										goto Block_10;
									}
									byte[] inBytes3 = this._inBytes;
									num3 = this._inIndex;
									this._inIndex = num3 + 1;
									b = inBytes3[num3];
									if ((b & 192) != 128)
									{
										goto Block_11;
									}
									num2 = (num2 << 6) | (int)(b & 63);
								}
							}
							break;
						}
						case Tokenizer.TokenSource.ASCIIByteArray:
						{
							if (this._inIndex >= this._inSize)
							{
								goto Block_12;
							}
							byte[] inBytes4 = this._inBytes;
							int num3 = this._inIndex;
							this._inIndex = num3 + 1;
							num2 = inBytes4[num3];
							break;
						}
						case Tokenizer.TokenSource.CharArray:
						{
							if (this._inIndex >= this._inSize)
							{
								goto Block_13;
							}
							char[] inChars = this._inChars;
							int num3 = this._inIndex;
							this._inIndex = num3 + 1;
							num2 = inChars[num3];
							break;
						}
						case Tokenizer.TokenSource.String:
						{
							if (this._inIndex >= this._inSize)
							{
								goto Block_14;
							}
							string inString = this._inString;
							int num3 = this._inIndex;
							this._inIndex = num3 + 1;
							num2 = (int)inString[num3];
							break;
						}
						case Tokenizer.TokenSource.NestedStrings:
						{
							int num3;
							if (this._inNestedSize != 0)
							{
								if (this._inNestedIndex < this._inNestedSize)
								{
									string inNestedString = this._inNestedString;
									num3 = this._inNestedIndex;
									this._inNestedIndex = num3 + 1;
									num2 = (int)inNestedString[num3];
									break;
								}
								this._inNestedSize = 0;
							}
							if (this._inIndex >= this._inSize)
							{
								goto Block_17;
							}
							string inString2 = this._inString;
							num3 = this._inIndex;
							this._inIndex = num3 + 1;
							num2 = (int)inString2[num3];
							if (num2 == 123)
							{
								for (int i = 0; i < this._searchStrings.Length; i++)
								{
									if (string.Compare(this._searchStrings[i], 0, this._inString, this._inIndex - 1, this._searchStrings[i].Length, StringComparison.Ordinal) == 0)
									{
										this._inNestedString = this._replaceStrings[i];
										this._inNestedSize = this._inNestedString.Length;
										this._inNestedIndex = 1;
										num2 = (int)this._inNestedString[0];
										this._inIndex += this._searchStrings[i].Length - 1;
										break;
									}
								}
							}
							break;
						}
						default:
							num2 = this._inTokenReader.Read();
							if (num2 == -1)
							{
								goto Block_21;
							}
							break;
						}
					}
					if (!flag)
					{
						if (num2 <= 34)
						{
							switch (num2)
							{
							case 9:
							case 13:
								continue;
							case 10:
								this.LineNo++;
								continue;
							case 11:
							case 12:
								break;
							default:
								switch (num2)
								{
								case 32:
									continue;
								case 33:
									if (this._inProcessingTag != 0)
									{
										goto Block_32;
									}
									break;
								case 34:
									flag = true;
									flag2 = true;
									continue;
								}
								break;
							}
						}
						else if (num2 != 45)
						{
							if (num2 != 47)
							{
								switch (num2)
								{
								case 60:
									goto IL_48A;
								case 61:
									goto IL_4C0;
								case 62:
									goto IL_4A4;
								case 63:
									if (this._inProcessingTag != 0)
									{
										goto Block_31;
									}
									break;
								}
							}
							else if (this._inProcessingTag != 0)
							{
								goto Block_30;
							}
						}
						else if (this._inProcessingTag != 0)
						{
							goto Block_33;
						}
					}
					else if (num2 <= 34)
					{
						switch (num2)
						{
						case 9:
						case 13:
							break;
						case 10:
							this.LineNo++;
							if (!flag2)
							{
								goto Block_46;
							}
							goto IL_62F;
						case 11:
						case 12:
							goto IL_62F;
						default:
							if (num2 != 32)
							{
								if (num2 != 34)
								{
									goto IL_62F;
								}
								if (flag2)
								{
									goto Block_44;
								}
								goto IL_62F;
							}
							break;
						}
						if (!flag2)
						{
							goto Block_45;
						}
					}
					else
					{
						if (num2 != 47)
						{
							if (num2 != 60)
							{
								if (num2 - 61 > 1)
								{
									goto IL_62F;
								}
							}
							else
							{
								if (!flag2)
								{
									goto Block_41;
								}
								goto IL_62F;
							}
						}
						if (!flag2 && this._inProcessingTag != 0)
						{
							goto Block_43;
						}
					}
					IL_62F:
					flag = true;
					if (maker._outIndex < 512)
					{
						char[] outChars = maker._outChars;
						Tokenizer.StringMaker stringMaker = maker;
						int num3 = stringMaker._outIndex;
						stringMaker._outIndex = num3 + 1;
						outChars[num3] = (ushort)num2;
					}
					else
					{
						if (maker._outStringBuilder == null)
						{
							maker._outStringBuilder = new StringBuilder();
						}
						maker._outStringBuilder.Append(maker._outChars, 0, 512);
						maker._outChars[0] = (char)num2;
						maker._outIndex = 1;
					}
				}
				IL_48A:
				this._inProcessingTag++;
				stream.AddToken(0);
				continue;
				Block_3:
				stream.AddToken(-1);
				return;
				IL_4A4:
				this._inProcessingTag--;
				stream.AddToken(1);
				if (endAfterKet)
				{
					return;
				}
				continue;
				IL_4C0:
				stream.AddToken(4);
				continue;
				Block_30:
				stream.AddToken(2);
				continue;
				Block_31:
				stream.AddToken(5);
				continue;
				Block_32:
				stream.AddToken(6);
				continue;
				Block_33:
				stream.AddToken(7);
				continue;
				Block_41:
				this._inSavedCharacter = num2;
				stream.AddToken(3);
				stream.AddString(this.GetStringToken());
				continue;
				Block_43:
				this._inSavedCharacter = num2;
				stream.AddToken(3);
				stream.AddString(this.GetStringToken());
				continue;
				Block_44:
				stream.AddToken(3);
				stream.AddString(this.GetStringToken());
				continue;
				Block_45:
				stream.AddToken(3);
				stream.AddString(this.GetStringToken());
				continue;
				Block_46:
				stream.AddToken(3);
				stream.AddString(this.GetStringToken());
				continue;
				Block_4:
				stream.AddToken(-1);
				return;
				IL_12D:
				throw new XmlSyntaxException(this.LineNo);
				IL_14B:
				throw new XmlSyntaxException(this.LineNo);
				Block_7:
				throw new XmlSyntaxException(this.LineNo, Environment.GetResourceString("XMLSyntax_UnexpectedEndOfFile"));
				Block_8:
				throw new XmlSyntaxException(this.LineNo);
				Block_10:
				throw new XmlSyntaxException(this.LineNo, Environment.GetResourceString("XMLSyntax_UnexpectedEndOfFile"));
				Block_11:
				throw new XmlSyntaxException(this.LineNo);
				Block_12:
				stream.AddToken(-1);
				return;
				Block_13:
				stream.AddToken(-1);
				return;
				Block_14:
				stream.AddToken(-1);
				return;
				Block_17:
				stream.AddToken(-1);
				return;
				Block_21:
				stream.AddToken(-1);
				return;
			}
		}

		// Token: 0x06002CCD RID: 11469 RVA: 0x000A8F5A File Offset: 0x000A715A
		private string GetStringToken()
		{
			return this._maker.MakeString();
		}

		// Token: 0x040011FA RID: 4602
		internal const byte bra = 0;

		// Token: 0x040011FB RID: 4603
		internal const byte ket = 1;

		// Token: 0x040011FC RID: 4604
		internal const byte slash = 2;

		// Token: 0x040011FD RID: 4605
		internal const byte cstr = 3;

		// Token: 0x040011FE RID: 4606
		internal const byte equals = 4;

		// Token: 0x040011FF RID: 4607
		internal const byte quest = 5;

		// Token: 0x04001200 RID: 4608
		internal const byte bang = 6;

		// Token: 0x04001201 RID: 4609
		internal const byte dash = 7;

		// Token: 0x04001202 RID: 4610
		internal const int intOpenBracket = 60;

		// Token: 0x04001203 RID: 4611
		internal const int intCloseBracket = 62;

		// Token: 0x04001204 RID: 4612
		internal const int intSlash = 47;

		// Token: 0x04001205 RID: 4613
		internal const int intEquals = 61;

		// Token: 0x04001206 RID: 4614
		internal const int intQuote = 34;

		// Token: 0x04001207 RID: 4615
		internal const int intQuest = 63;

		// Token: 0x04001208 RID: 4616
		internal const int intBang = 33;

		// Token: 0x04001209 RID: 4617
		internal const int intDash = 45;

		// Token: 0x0400120A RID: 4618
		internal const int intTab = 9;

		// Token: 0x0400120B RID: 4619
		internal const int intCR = 13;

		// Token: 0x0400120C RID: 4620
		internal const int intLF = 10;

		// Token: 0x0400120D RID: 4621
		internal const int intSpace = 32;

		// Token: 0x0400120E RID: 4622
		public int LineNo;

		// Token: 0x0400120F RID: 4623
		private int _inProcessingTag;

		// Token: 0x04001210 RID: 4624
		private byte[] _inBytes;

		// Token: 0x04001211 RID: 4625
		private char[] _inChars;

		// Token: 0x04001212 RID: 4626
		private string _inString;

		// Token: 0x04001213 RID: 4627
		private int _inIndex;

		// Token: 0x04001214 RID: 4628
		private int _inSize;

		// Token: 0x04001215 RID: 4629
		private int _inSavedCharacter;

		// Token: 0x04001216 RID: 4630
		private Tokenizer.TokenSource _inTokenSource;

		// Token: 0x04001217 RID: 4631
		private Tokenizer.ITokenReader _inTokenReader;

		// Token: 0x04001218 RID: 4632
		private Tokenizer.StringMaker _maker;

		// Token: 0x04001219 RID: 4633
		private string[] _searchStrings;

		// Token: 0x0400121A RID: 4634
		private string[] _replaceStrings;

		// Token: 0x0400121B RID: 4635
		private int _inNestedIndex;

		// Token: 0x0400121C RID: 4636
		private int _inNestedSize;

		// Token: 0x0400121D RID: 4637
		private string _inNestedString;

		// Token: 0x02000B5F RID: 2911
		private enum TokenSource
		{
			// Token: 0x04003430 RID: 13360
			UnicodeByteArray,
			// Token: 0x04003431 RID: 13361
			UTF8ByteArray,
			// Token: 0x04003432 RID: 13362
			ASCIIByteArray,
			// Token: 0x04003433 RID: 13363
			CharArray,
			// Token: 0x04003434 RID: 13364
			String,
			// Token: 0x04003435 RID: 13365
			NestedStrings,
			// Token: 0x04003436 RID: 13366
			Other
		}

		// Token: 0x02000B60 RID: 2912
		internal enum ByteTokenEncoding
		{
			// Token: 0x04003438 RID: 13368
			UnicodeTokens,
			// Token: 0x04003439 RID: 13369
			UTF8Tokens,
			// Token: 0x0400343A RID: 13370
			ByteTokens
		}

		// Token: 0x02000B61 RID: 2913
		[Serializable]
		internal sealed class StringMaker
		{
			// Token: 0x06006BF9 RID: 27641 RVA: 0x00175C1C File Offset: 0x00173E1C
			private static uint HashString(string str)
			{
				uint num = 0U;
				int length = str.Length;
				for (int i = 0; i < length; i++)
				{
					num = (num << 3) ^ (uint)str[i] ^ (num >> 29);
				}
				return num;
			}

			// Token: 0x06006BFA RID: 27642 RVA: 0x00175C50 File Offset: 0x00173E50
			private static uint HashCharArray(char[] a, int l)
			{
				uint num = 0U;
				for (int i = 0; i < l; i++)
				{
					num = (num << 3) ^ (uint)a[i] ^ (num >> 29);
				}
				return num;
			}

			// Token: 0x06006BFB RID: 27643 RVA: 0x00175C79 File Offset: 0x00173E79
			public StringMaker()
			{
				this.cStringsMax = 2048U;
				this.cStringsUsed = 0U;
				this.aStrings = new string[this.cStringsMax];
				this._outChars = new char[512];
			}

			// Token: 0x06006BFC RID: 27644 RVA: 0x00175CB4 File Offset: 0x00173EB4
			private bool CompareStringAndChars(string str, char[] a, int l)
			{
				if (str.Length != l)
				{
					return false;
				}
				for (int i = 0; i < l; i++)
				{
					if (a[i] != str[i])
					{
						return false;
					}
				}
				return true;
			}

			// Token: 0x06006BFD RID: 27645 RVA: 0x00175CE8 File Offset: 0x00173EE8
			public string MakeString()
			{
				char[] outChars = this._outChars;
				int outIndex = this._outIndex;
				if (this._outStringBuilder != null)
				{
					this._outStringBuilder.Append(this._outChars, 0, this._outIndex);
					return this._outStringBuilder.ToString();
				}
				uint num3;
				if (this.cStringsUsed > this.cStringsMax / 4U * 3U)
				{
					uint num = this.cStringsMax * 2U;
					string[] array = new string[num];
					int num2 = 0;
					while ((long)num2 < (long)((ulong)this.cStringsMax))
					{
						if (this.aStrings[num2] != null)
						{
							num3 = Tokenizer.StringMaker.HashString(this.aStrings[num2]) % num;
							while (array[(int)num3] != null)
							{
								if ((num3 += 1U) >= num)
								{
									num3 = 0U;
								}
							}
							array[(int)num3] = this.aStrings[num2];
						}
						num2++;
					}
					this.cStringsMax = num;
					this.aStrings = array;
				}
				num3 = Tokenizer.StringMaker.HashCharArray(outChars, outIndex) % this.cStringsMax;
				string text;
				while ((text = this.aStrings[(int)num3]) != null)
				{
					if (this.CompareStringAndChars(text, outChars, outIndex))
					{
						return text;
					}
					if ((num3 += 1U) >= this.cStringsMax)
					{
						num3 = 0U;
					}
				}
				text = new string(outChars, 0, outIndex);
				this.aStrings[(int)num3] = text;
				this.cStringsUsed += 1U;
				return text;
			}

			// Token: 0x0400343B RID: 13371
			private string[] aStrings;

			// Token: 0x0400343C RID: 13372
			private uint cStringsMax;

			// Token: 0x0400343D RID: 13373
			private uint cStringsUsed;

			// Token: 0x0400343E RID: 13374
			public StringBuilder _outStringBuilder;

			// Token: 0x0400343F RID: 13375
			public char[] _outChars;

			// Token: 0x04003440 RID: 13376
			public int _outIndex;

			// Token: 0x04003441 RID: 13377
			public const int outMaxSize = 512;
		}

		// Token: 0x02000B62 RID: 2914
		internal interface ITokenReader
		{
			// Token: 0x06006BFE RID: 27646
			int Read();
		}

		// Token: 0x02000B63 RID: 2915
		internal class StreamTokenReader : Tokenizer.ITokenReader
		{
			// Token: 0x06006BFF RID: 27647 RVA: 0x00175E13 File Offset: 0x00174013
			internal StreamTokenReader(StreamReader input)
			{
				this._in = input;
				this._numCharRead = 0;
			}

			// Token: 0x06006C00 RID: 27648 RVA: 0x00175E2C File Offset: 0x0017402C
			public virtual int Read()
			{
				int num = this._in.Read();
				if (num != -1)
				{
					this._numCharRead++;
				}
				return num;
			}

			// Token: 0x1700123A RID: 4666
			// (get) Token: 0x06006C01 RID: 27649 RVA: 0x00175E58 File Offset: 0x00174058
			internal int NumCharEncountered
			{
				get
				{
					return this._numCharRead;
				}
			}

			// Token: 0x04003442 RID: 13378
			internal StreamReader _in;

			// Token: 0x04003443 RID: 13379
			internal int _numCharRead;
		}
	}
}
