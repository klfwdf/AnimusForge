using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;

namespace System.Globalization
{
	// Token: 0x020003D4 RID: 980
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class TextInfo : ICloneable, IDeserializationCallback
	{
		// Token: 0x170006F4 RID: 1780
		// (get) Token: 0x060031A1 RID: 12705 RVA: 0x000BE545 File Offset: 0x000BC745
		internal static TextInfo Invariant
		{
			get
			{
				if (TextInfo.s_Invariant == null)
				{
					TextInfo.s_Invariant = new TextInfo(CultureData.Invariant);
				}
				return TextInfo.s_Invariant;
			}
		}

		// Token: 0x060031A2 RID: 12706 RVA: 0x000BE568 File Offset: 0x000BC768
		internal TextInfo(CultureData cultureData)
		{
			this.m_cultureData = cultureData;
			this.m_cultureName = this.m_cultureData.CultureName;
			this.m_textInfoName = this.m_cultureData.STEXTINFO;
			IntPtr handleOrigin;
			this.m_dataHandle = CompareInfo.InternalInitSortHandle(this.m_textInfoName, out handleOrigin);
			this.m_handleOrigin = handleOrigin;
		}

		// Token: 0x060031A3 RID: 12707 RVA: 0x000BE5BE File Offset: 0x000BC7BE
		[OnDeserializing]
		private void OnDeserializing(StreamingContext ctx)
		{
			this.m_cultureData = null;
			this.m_cultureName = null;
		}

		// Token: 0x060031A4 RID: 12708 RVA: 0x000BE5D0 File Offset: 0x000BC7D0
		private void OnDeserialized()
		{
			if (this.m_cultureData == null)
			{
				if (this.m_cultureName == null)
				{
					if (this.customCultureName != null)
					{
						this.m_cultureName = this.customCultureName;
					}
					else if (this.m_win32LangID == 0)
					{
						this.m_cultureName = "ar-SA";
					}
					else
					{
						this.m_cultureName = CultureInfo.GetCultureInfo(this.m_win32LangID).m_cultureData.CultureName;
					}
				}
				this.m_cultureData = CultureInfo.GetCultureInfo(this.m_cultureName).m_cultureData;
				this.m_textInfoName = this.m_cultureData.STEXTINFO;
				IntPtr handleOrigin;
				this.m_dataHandle = CompareInfo.InternalInitSortHandle(this.m_textInfoName, out handleOrigin);
				this.m_handleOrigin = handleOrigin;
			}
		}

		// Token: 0x060031A5 RID: 12709 RVA: 0x000BE677 File Offset: 0x000BC877
		[OnDeserialized]
		private void OnDeserialized(StreamingContext ctx)
		{
			this.OnDeserialized();
		}

		// Token: 0x060031A6 RID: 12710 RVA: 0x000BE67F File Offset: 0x000BC87F
		[OnSerializing]
		private void OnSerializing(StreamingContext ctx)
		{
			this.m_useUserOverride = false;
			this.customCultureName = this.m_cultureName;
			this.m_win32LangID = CultureInfo.GetCultureInfo(this.m_cultureName).LCID;
		}

		// Token: 0x060031A7 RID: 12711 RVA: 0x000BE6AA File Offset: 0x000BC8AA
		internal static int GetHashCodeOrdinalIgnoreCase(string s)
		{
			return TextInfo.GetHashCodeOrdinalIgnoreCase(s, false, 0L);
		}

		// Token: 0x060031A8 RID: 12712 RVA: 0x000BE6B5 File Offset: 0x000BC8B5
		internal static int GetHashCodeOrdinalIgnoreCase(string s, bool forceRandomizedHashing, long additionalEntropy)
		{
			return TextInfo.Invariant.GetCaseInsensitiveHashCode(s, forceRandomizedHashing, additionalEntropy);
		}

		// Token: 0x060031A9 RID: 12713 RVA: 0x000BE6C4 File Offset: 0x000BC8C4
		[SecuritySafeCritical]
		internal static bool TryFastFindStringOrdinalIgnoreCase(int searchFlags, string source, int startIndex, string value, int count, ref int foundIndex)
		{
			return TextInfo.InternalTryFindStringOrdinalIgnoreCase(searchFlags, source, count, startIndex, value, value.Length, ref foundIndex);
		}

		// Token: 0x060031AA RID: 12714 RVA: 0x000BE6D9 File Offset: 0x000BC8D9
		[SecuritySafeCritical]
		internal static int CompareOrdinalIgnoreCase(string str1, string str2)
		{
			return TextInfo.InternalCompareStringOrdinalIgnoreCase(str1, 0, str2, 0, str1.Length, str2.Length);
		}

		// Token: 0x060031AB RID: 12715 RVA: 0x000BE6F0 File Offset: 0x000BC8F0
		[SecuritySafeCritical]
		internal static int CompareOrdinalIgnoreCaseEx(string strA, int indexA, string strB, int indexB, int lengthA, int lengthB)
		{
			return TextInfo.InternalCompareStringOrdinalIgnoreCase(strA, indexA, strB, indexB, lengthA, lengthB);
		}

		// Token: 0x060031AC RID: 12716 RVA: 0x000BE700 File Offset: 0x000BC900
		internal static int IndexOfStringOrdinalIgnoreCase(string source, string value, int startIndex, int count)
		{
			if (source.Length == 0 && value.Length == 0)
			{
				return 0;
			}
			int result = -1;
			if (TextInfo.TryFastFindStringOrdinalIgnoreCase(4194304, source, startIndex, value, count, ref result))
			{
				return result;
			}
			int num = startIndex + count;
			int num2 = num - value.Length;
			while (startIndex <= num2)
			{
				if (TextInfo.CompareOrdinalIgnoreCaseEx(source, startIndex, value, 0, value.Length, value.Length) == 0)
				{
					return startIndex;
				}
				startIndex++;
			}
			return -1;
		}

		// Token: 0x060031AD RID: 12717 RVA: 0x000BE768 File Offset: 0x000BC968
		internal static int LastIndexOfStringOrdinalIgnoreCase(string source, string value, int startIndex, int count)
		{
			if (value.Length == 0)
			{
				return startIndex;
			}
			int result = -1;
			if (TextInfo.TryFastFindStringOrdinalIgnoreCase(8388608, source, startIndex, value, count, ref result))
			{
				return result;
			}
			int num = startIndex - count + 1;
			if (value.Length > 0)
			{
				startIndex -= value.Length - 1;
			}
			while (startIndex >= num)
			{
				if (TextInfo.CompareOrdinalIgnoreCaseEx(source, startIndex, value, 0, value.Length, value.Length) == 0)
				{
					return startIndex;
				}
				startIndex--;
			}
			return -1;
		}

		// Token: 0x170006F5 RID: 1781
		// (get) Token: 0x060031AE RID: 12718 RVA: 0x000BE7D5 File Offset: 0x000BC9D5
		public virtual int ANSICodePage
		{
			get
			{
				return this.m_cultureData.IDEFAULTANSICODEPAGE;
			}
		}

		// Token: 0x170006F6 RID: 1782
		// (get) Token: 0x060031AF RID: 12719 RVA: 0x000BE7E2 File Offset: 0x000BC9E2
		public virtual int OEMCodePage
		{
			get
			{
				return this.m_cultureData.IDEFAULTOEMCODEPAGE;
			}
		}

		// Token: 0x170006F7 RID: 1783
		// (get) Token: 0x060031B0 RID: 12720 RVA: 0x000BE7EF File Offset: 0x000BC9EF
		public virtual int MacCodePage
		{
			get
			{
				return this.m_cultureData.IDEFAULTMACCODEPAGE;
			}
		}

		// Token: 0x170006F8 RID: 1784
		// (get) Token: 0x060031B1 RID: 12721 RVA: 0x000BE7FC File Offset: 0x000BC9FC
		public virtual int EBCDICCodePage
		{
			get
			{
				return this.m_cultureData.IDEFAULTEBCDICCODEPAGE;
			}
		}

		// Token: 0x170006F9 RID: 1785
		// (get) Token: 0x060031B2 RID: 12722 RVA: 0x000BE809 File Offset: 0x000BCA09
		[ComVisible(false)]
		public int LCID
		{
			get
			{
				return CultureInfo.GetCultureInfo(this.m_textInfoName).LCID;
			}
		}

		// Token: 0x170006FA RID: 1786
		// (get) Token: 0x060031B3 RID: 12723 RVA: 0x000BE81B File Offset: 0x000BCA1B
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public string CultureName
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_textInfoName;
			}
		}

		// Token: 0x170006FB RID: 1787
		// (get) Token: 0x060031B4 RID: 12724 RVA: 0x000BE823 File Offset: 0x000BCA23
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public bool IsReadOnly
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_isReadOnly;
			}
		}

		// Token: 0x060031B5 RID: 12725 RVA: 0x000BE82C File Offset: 0x000BCA2C
		[ComVisible(false)]
		public virtual object Clone()
		{
			object obj = base.MemberwiseClone();
			((TextInfo)obj).SetReadOnlyState(false);
			return obj;
		}

		// Token: 0x060031B6 RID: 12726 RVA: 0x000BE850 File Offset: 0x000BCA50
		[ComVisible(false)]
		public static TextInfo ReadOnly(TextInfo textInfo)
		{
			if (textInfo == null)
			{
				throw new ArgumentNullException("textInfo");
			}
			if (textInfo.IsReadOnly)
			{
				return textInfo;
			}
			TextInfo textInfo2 = (TextInfo)textInfo.MemberwiseClone();
			textInfo2.SetReadOnlyState(true);
			return textInfo2;
		}

		// Token: 0x060031B7 RID: 12727 RVA: 0x000BE889 File Offset: 0x000BCA89
		private void VerifyWritable()
		{
			if (this.m_isReadOnly)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
			}
		}

		// Token: 0x060031B8 RID: 12728 RVA: 0x000BE8A3 File Offset: 0x000BCAA3
		internal void SetReadOnlyState(bool readOnly)
		{
			this.m_isReadOnly = readOnly;
		}

		// Token: 0x170006FC RID: 1788
		// (get) Token: 0x060031B9 RID: 12729 RVA: 0x000BE8AC File Offset: 0x000BCAAC
		// (set) Token: 0x060031BA RID: 12730 RVA: 0x000BE8CD File Offset: 0x000BCACD
		[__DynamicallyInvokable]
		public virtual string ListSeparator
		{
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			get
			{
				if (this.m_listSeparator == null)
				{
					this.m_listSeparator = this.m_cultureData.SLIST;
				}
				return this.m_listSeparator;
			}
			[ComVisible(false)]
			[__DynamicallyInvokable]
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
				}
				this.VerifyWritable();
				this.m_listSeparator = value;
			}
		}

		// Token: 0x060031BB RID: 12731 RVA: 0x000BE8F4 File Offset: 0x000BCAF4
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public virtual char ToLower(char c)
		{
			if (TextInfo.IsAscii(c) && this.IsAsciiCasingSameAsInvariant)
			{
				return TextInfo.ToLowerAsciiInvariant(c);
			}
			return TextInfo.InternalChangeCaseChar(this.m_dataHandle, this.m_handleOrigin, this.m_textInfoName, c, false);
		}

		// Token: 0x060031BC RID: 12732 RVA: 0x000BE926 File Offset: 0x000BCB26
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public virtual string ToLower(string str)
		{
			if (str == null)
			{
				throw new ArgumentNullException("str");
			}
			return TextInfo.InternalChangeCaseString(this.m_dataHandle, this.m_handleOrigin, this.m_textInfoName, str, false);
		}

		// Token: 0x060031BD RID: 12733 RVA: 0x000BE94F File Offset: 0x000BCB4F
		private static char ToLowerAsciiInvariant(char c)
		{
			if ('A' <= c && c <= 'Z')
			{
				c |= ' ';
			}
			return c;
		}

		// Token: 0x060031BE RID: 12734 RVA: 0x000BE963 File Offset: 0x000BCB63
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public virtual char ToUpper(char c)
		{
			if (TextInfo.IsAscii(c) && this.IsAsciiCasingSameAsInvariant)
			{
				return TextInfo.ToUpperAsciiInvariant(c);
			}
			return TextInfo.InternalChangeCaseChar(this.m_dataHandle, this.m_handleOrigin, this.m_textInfoName, c, true);
		}

		// Token: 0x060031BF RID: 12735 RVA: 0x000BE995 File Offset: 0x000BCB95
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public virtual string ToUpper(string str)
		{
			if (str == null)
			{
				throw new ArgumentNullException("str");
			}
			return TextInfo.InternalChangeCaseString(this.m_dataHandle, this.m_handleOrigin, this.m_textInfoName, str, true);
		}

		// Token: 0x060031C0 RID: 12736 RVA: 0x000BE9BE File Offset: 0x000BCBBE
		private static char ToUpperAsciiInvariant(char c)
		{
			if ('a' <= c && c <= 'z')
			{
				c = (char)((int)c & -33);
			}
			return c;
		}

		// Token: 0x060031C1 RID: 12737 RVA: 0x000BE9D2 File Offset: 0x000BCBD2
		private static bool IsAscii(char c)
		{
			return c < '\u0080';
		}

		// Token: 0x170006FD RID: 1789
		// (get) Token: 0x060031C2 RID: 12738 RVA: 0x000BE9DC File Offset: 0x000BCBDC
		private bool IsAsciiCasingSameAsInvariant
		{
			get
			{
				if (this.m_IsAsciiCasingSameAsInvariant == TextInfo.Tristate.NotInitialized)
				{
					this.m_IsAsciiCasingSameAsInvariant = ((CultureInfo.GetCultureInfo(this.m_textInfoName).CompareInfo.Compare("abcdefghijklmnopqrstuvwxyz", "ABCDEFGHIJKLMNOPQRSTUVWXYZ", CompareOptions.IgnoreCase) == 0) ? TextInfo.Tristate.True : TextInfo.Tristate.False);
				}
				return this.m_IsAsciiCasingSameAsInvariant == TextInfo.Tristate.True;
			}
		}

		// Token: 0x060031C3 RID: 12739 RVA: 0x000BEA1C File Offset: 0x000BCC1C
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			TextInfo textInfo = obj as TextInfo;
			return textInfo != null && this.CultureName.Equals(textInfo.CultureName);
		}

		// Token: 0x060031C4 RID: 12740 RVA: 0x000BEA46 File Offset: 0x000BCC46
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return this.CultureName.GetHashCode();
		}

		// Token: 0x060031C5 RID: 12741 RVA: 0x000BEA53 File Offset: 0x000BCC53
		[__DynamicallyInvokable]
		public override string ToString()
		{
			return "TextInfo - " + this.m_cultureData.CultureName;
		}

		// Token: 0x060031C6 RID: 12742 RVA: 0x000BEA6C File Offset: 0x000BCC6C
		public string ToTitleCase(string str)
		{
			if (str == null)
			{
				throw new ArgumentNullException("str");
			}
			if (str.Length == 0)
			{
				return str;
			}
			StringBuilder stringBuilder = new StringBuilder();
			string text = null;
			for (int i = 0; i < str.Length; i++)
			{
				int num;
				UnicodeCategory unicodeCategory = CharUnicodeInfo.InternalGetUnicodeCategory(str, i, out num);
				if (char.CheckLetter(unicodeCategory))
				{
					i = this.AddTitlecaseLetter(ref stringBuilder, ref str, i, num) + 1;
					int num2 = i;
					bool flag = unicodeCategory == UnicodeCategory.LowercaseLetter;
					while (i < str.Length)
					{
						unicodeCategory = CharUnicodeInfo.InternalGetUnicodeCategory(str, i, out num);
						if (TextInfo.IsLetterCategory(unicodeCategory))
						{
							if (unicodeCategory == UnicodeCategory.LowercaseLetter)
							{
								flag = true;
							}
							i += num;
						}
						else if (str[i] == '\'')
						{
							i++;
							if (flag)
							{
								if (text == null)
								{
									text = this.ToLower(str);
								}
								stringBuilder.Append(text, num2, i - num2);
							}
							else
							{
								stringBuilder.Append(str, num2, i - num2);
							}
							num2 = i;
							flag = true;
						}
						else
						{
							if (TextInfo.IsWordSeparator(unicodeCategory))
							{
								break;
							}
							i += num;
						}
					}
					int num3 = i - num2;
					if (num3 > 0)
					{
						if (flag)
						{
							if (text == null)
							{
								text = this.ToLower(str);
							}
							stringBuilder.Append(text, num2, num3);
						}
						else
						{
							stringBuilder.Append(str, num2, num3);
						}
					}
					if (i < str.Length)
					{
						i = TextInfo.AddNonLetter(ref stringBuilder, ref str, i, num);
					}
				}
				else
				{
					i = TextInfo.AddNonLetter(ref stringBuilder, ref str, i, num);
				}
			}
			return stringBuilder.ToString();
		}

		// Token: 0x060031C7 RID: 12743 RVA: 0x000BEBB9 File Offset: 0x000BCDB9
		private static int AddNonLetter(ref StringBuilder result, ref string input, int inputIndex, int charLen)
		{
			if (charLen == 2)
			{
				result.Append(input[inputIndex++]);
				result.Append(input[inputIndex]);
			}
			else
			{
				result.Append(input[inputIndex]);
			}
			return inputIndex;
		}

		// Token: 0x060031C8 RID: 12744 RVA: 0x000BEBF8 File Offset: 0x000BCDF8
		private int AddTitlecaseLetter(ref StringBuilder result, ref string input, int inputIndex, int charLen)
		{
			if (charLen == 2)
			{
				result.Append(this.ToUpper(input.Substring(inputIndex, charLen)));
				inputIndex++;
			}
			else
			{
				char c = input[inputIndex];
				switch (c)
				{
				case 'Ǆ':
				case 'ǅ':
				case 'ǆ':
					result.Append('ǅ');
					break;
				case 'Ǉ':
				case 'ǈ':
				case 'ǉ':
					result.Append('ǈ');
					break;
				case 'Ǌ':
				case 'ǋ':
				case 'ǌ':
					result.Append('ǋ');
					break;
				default:
					switch (c)
					{
					case 'Ǳ':
					case 'ǲ':
					case 'ǳ':
						result.Append('ǲ');
						break;
					default:
						result.Append(this.ToUpper(input[inputIndex]));
						break;
					}
					break;
				}
			}
			return inputIndex;
		}

		// Token: 0x060031C9 RID: 12745 RVA: 0x000BECD2 File Offset: 0x000BCED2
		private static bool IsWordSeparator(UnicodeCategory category)
		{
			return (536672256 & (1 << (int)category)) != 0;
		}

		// Token: 0x060031CA RID: 12746 RVA: 0x000BECE3 File Offset: 0x000BCEE3
		private static bool IsLetterCategory(UnicodeCategory uc)
		{
			return uc == UnicodeCategory.UppercaseLetter || uc == UnicodeCategory.LowercaseLetter || uc == UnicodeCategory.TitlecaseLetter || uc == UnicodeCategory.ModifierLetter || uc == UnicodeCategory.OtherLetter;
		}

		// Token: 0x170006FE RID: 1790
		// (get) Token: 0x060031CB RID: 12747 RVA: 0x000BECFA File Offset: 0x000BCEFA
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public bool IsRightToLeft
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_cultureData.IsRightToLeft;
			}
		}

		// Token: 0x060031CC RID: 12748 RVA: 0x000BED07 File Offset: 0x000BCF07
		void IDeserializationCallback.OnDeserialization(object sender)
		{
			this.OnDeserialized();
		}

		// Token: 0x060031CD RID: 12749 RVA: 0x000BED0F File Offset: 0x000BCF0F
		[SecuritySafeCritical]
		internal int GetCaseInsensitiveHashCode(string str)
		{
			return this.GetCaseInsensitiveHashCode(str, false, 0L);
		}

		// Token: 0x060031CE RID: 12750 RVA: 0x000BED1B File Offset: 0x000BCF1B
		[SecuritySafeCritical]
		internal int GetCaseInsensitiveHashCode(string str, bool forceRandomizedHashing, long additionalEntropy)
		{
			if (str == null)
			{
				throw new ArgumentNullException("str");
			}
			return TextInfo.InternalGetCaseInsHash(this.m_dataHandle, this.m_handleOrigin, this.m_textInfoName, str, forceRandomizedHashing, additionalEntropy);
		}

		// Token: 0x060031CF RID: 12751
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern char InternalChangeCaseChar(IntPtr handle, IntPtr handleOrigin, string localeName, char ch, bool isToUpper);

		// Token: 0x060031D0 RID: 12752
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern string InternalChangeCaseString(IntPtr handle, IntPtr handleOrigin, string localeName, string str, bool isToUpper);

		// Token: 0x060031D1 RID: 12753
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern int InternalGetCaseInsHash(IntPtr handle, IntPtr handleOrigin, string localeName, string str, bool forceRandomizedHashing, long additionalEntropy);

		// Token: 0x060031D2 RID: 12754
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern int InternalCompareStringOrdinalIgnoreCase(string string1, int index1, string string2, int index2, int length1, int length2);

		// Token: 0x060031D3 RID: 12755
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool InternalTryFindStringOrdinalIgnoreCase(int searchFlags, string source, int sourceCount, int startIndex, string target, int targetCount, ref int foundIndex);

		// Token: 0x0400152F RID: 5423
		[OptionalField(VersionAdded = 2)]
		private string m_listSeparator;

		// Token: 0x04001530 RID: 5424
		[OptionalField(VersionAdded = 2)]
		private bool m_isReadOnly;

		// Token: 0x04001531 RID: 5425
		[OptionalField(VersionAdded = 3)]
		private string m_cultureName;

		// Token: 0x04001532 RID: 5426
		[NonSerialized]
		private CultureData m_cultureData;

		// Token: 0x04001533 RID: 5427
		[NonSerialized]
		private string m_textInfoName;

		// Token: 0x04001534 RID: 5428
		[NonSerialized]
		private IntPtr m_dataHandle;

		// Token: 0x04001535 RID: 5429
		[NonSerialized]
		private IntPtr m_handleOrigin;

		// Token: 0x04001536 RID: 5430
		[NonSerialized]
		private TextInfo.Tristate m_IsAsciiCasingSameAsInvariant;

		// Token: 0x04001537 RID: 5431
		internal static volatile TextInfo s_Invariant;

		// Token: 0x04001538 RID: 5432
		[OptionalField(VersionAdded = 2)]
		private string customCultureName;

		// Token: 0x04001539 RID: 5433
		[OptionalField(VersionAdded = 1)]
		internal int m_nDataItem;

		// Token: 0x0400153A RID: 5434
		[OptionalField(VersionAdded = 1)]
		internal bool m_useUserOverride;

		// Token: 0x0400153B RID: 5435
		[OptionalField(VersionAdded = 1)]
		internal int m_win32LangID;

		// Token: 0x0400153C RID: 5436
		private const int wordSeparatorMask = 536672256;

		// Token: 0x02000B72 RID: 2930
		private enum Tristate : byte
		{
			// Token: 0x04003476 RID: 13430
			NotInitialized,
			// Token: 0x04003477 RID: 13431
			True,
			// Token: 0x04003478 RID: 13432
			False
		}
	}
}
