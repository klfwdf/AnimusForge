using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Globalization
{
	// Token: 0x020003A6 RID: 934
	[__DynamicallyInvokable]
	public static class CharUnicodeInfo
	{
		// Token: 0x06002DEC RID: 11756 RVA: 0x000AFC54 File Offset: 0x000ADE54
		[SecuritySafeCritical]
		private unsafe static bool InitTable()
		{
			byte* globalizationResourceBytePtr = GlobalizationAssembly.GetGlobalizationResourceBytePtr(typeof(CharUnicodeInfo).Assembly, "charinfo.nlp");
			CharUnicodeInfo.UnicodeDataHeader* ptr = (CharUnicodeInfo.UnicodeDataHeader*)globalizationResourceBytePtr;
			CharUnicodeInfo.s_pCategoryLevel1Index = (ushort*)(globalizationResourceBytePtr + ptr->OffsetToCategoriesIndex);
			CharUnicodeInfo.s_pCategoriesValue = globalizationResourceBytePtr + ptr->OffsetToCategoriesValue;
			CharUnicodeInfo.s_pNumericLevel1Index = (ushort*)(globalizationResourceBytePtr + ptr->OffsetToNumbericIndex);
			CharUnicodeInfo.s_pNumericValues = globalizationResourceBytePtr + ptr->OffsetToNumbericValue;
			CharUnicodeInfo.s_pDigitValues = (CharUnicodeInfo.DigitValues*)(globalizationResourceBytePtr + ptr->OffsetToDigitValue);
			return true;
		}

		// Token: 0x06002DED RID: 11757 RVA: 0x000AFCC4 File Offset: 0x000ADEC4
		internal static int InternalConvertToUtf32(string s, int index)
		{
			if (index < s.Length - 1)
			{
				int num = (int)(s[index] - '\ud800');
				if (num >= 0 && num <= 1023)
				{
					int num2 = (int)(s[index + 1] - '\udc00');
					if (num2 >= 0 && num2 <= 1023)
					{
						return num * 1024 + num2 + 65536;
					}
				}
			}
			return (int)s[index];
		}

		// Token: 0x06002DEE RID: 11758 RVA: 0x000AFD2C File Offset: 0x000ADF2C
		internal static int InternalConvertToUtf32(string s, int index, out int charLength)
		{
			charLength = 1;
			if (index < s.Length - 1)
			{
				int num = (int)(s[index] - '\ud800');
				if (num >= 0 && num <= 1023)
				{
					int num2 = (int)(s[index + 1] - '\udc00');
					if (num2 >= 0 && num2 <= 1023)
					{
						charLength++;
						return num * 1024 + num2 + 65536;
					}
				}
			}
			return (int)s[index];
		}

		// Token: 0x06002DEF RID: 11759 RVA: 0x000AFD9C File Offset: 0x000ADF9C
		internal static bool IsWhiteSpace(string s, int index)
		{
			UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(s, index);
			return unicodeCategory - UnicodeCategory.SpaceSeparator <= 2;
		}

		// Token: 0x06002DF0 RID: 11760 RVA: 0x000AFDBC File Offset: 0x000ADFBC
		internal static bool IsWhiteSpace(char c)
		{
			UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
			return unicodeCategory - UnicodeCategory.SpaceSeparator <= 2;
		}

		// Token: 0x06002DF1 RID: 11761 RVA: 0x000AFDDC File Offset: 0x000ADFDC
		[SecuritySafeCritical]
		internal unsafe static double InternalGetNumericValue(int ch)
		{
			ushort num = CharUnicodeInfo.s_pNumericLevel1Index[ch >> 8];
			num = CharUnicodeInfo.s_pNumericLevel1Index[(int)num + ((ch >> 4) & 15)];
			byte* ptr = (byte*)(CharUnicodeInfo.s_pNumericLevel1Index + num);
			byte* ptr2 = CharUnicodeInfo.s_pNumericValues + ptr[ch & 15] * 8;
			if (ptr2 % 8L != null)
			{
				double result;
				byte* dest = (byte*)(&result);
				Buffer.Memcpy(dest, ptr2, 8);
				return result;
			}
			return *(double*)(CharUnicodeInfo.s_pNumericValues + (IntPtr)ptr[ch & 15] * 8);
		}

		// Token: 0x06002DF2 RID: 11762 RVA: 0x000AFE50 File Offset: 0x000AE050
		[SecuritySafeCritical]
		internal unsafe static CharUnicodeInfo.DigitValues* InternalGetDigitValues(int ch)
		{
			ushort num = CharUnicodeInfo.s_pNumericLevel1Index[ch >> 8];
			num = CharUnicodeInfo.s_pNumericLevel1Index[(int)num + ((ch >> 4) & 15)];
			byte* ptr = (byte*)(CharUnicodeInfo.s_pNumericLevel1Index + num);
			return CharUnicodeInfo.s_pDigitValues + ptr[ch & 15];
		}

		// Token: 0x06002DF3 RID: 11763 RVA: 0x000AFEA0 File Offset: 0x000AE0A0
		[SecuritySafeCritical]
		internal unsafe static sbyte InternalGetDecimalDigitValue(int ch)
		{
			return CharUnicodeInfo.InternalGetDigitValues(ch)->decimalDigit;
		}

		// Token: 0x06002DF4 RID: 11764 RVA: 0x000AFEAD File Offset: 0x000AE0AD
		[SecuritySafeCritical]
		internal unsafe static sbyte InternalGetDigitValue(int ch)
		{
			return CharUnicodeInfo.InternalGetDigitValues(ch)->digit;
		}

		// Token: 0x06002DF5 RID: 11765 RVA: 0x000AFEBA File Offset: 0x000AE0BA
		[__DynamicallyInvokable]
		public static double GetNumericValue(char ch)
		{
			return CharUnicodeInfo.InternalGetNumericValue((int)ch);
		}

		// Token: 0x06002DF6 RID: 11766 RVA: 0x000AFEC2 File Offset: 0x000AE0C2
		[__DynamicallyInvokable]
		public static double GetNumericValue(string s, int index)
		{
			if (s == null)
			{
				throw new ArgumentNullException("s");
			}
			if (index < 0 || index >= s.Length)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			return CharUnicodeInfo.InternalGetNumericValue(CharUnicodeInfo.InternalConvertToUtf32(s, index));
		}

		// Token: 0x06002DF7 RID: 11767 RVA: 0x000AFF00 File Offset: 0x000AE100
		public static int GetDecimalDigitValue(char ch)
		{
			return (int)CharUnicodeInfo.InternalGetDecimalDigitValue((int)ch);
		}

		// Token: 0x06002DF8 RID: 11768 RVA: 0x000AFF08 File Offset: 0x000AE108
		public static int GetDecimalDigitValue(string s, int index)
		{
			if (s == null)
			{
				throw new ArgumentNullException("s");
			}
			if (index < 0 || index >= s.Length)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			return (int)CharUnicodeInfo.InternalGetDecimalDigitValue(CharUnicodeInfo.InternalConvertToUtf32(s, index));
		}

		// Token: 0x06002DF9 RID: 11769 RVA: 0x000AFF46 File Offset: 0x000AE146
		public static int GetDigitValue(char ch)
		{
			return (int)CharUnicodeInfo.InternalGetDigitValue((int)ch);
		}

		// Token: 0x06002DFA RID: 11770 RVA: 0x000AFF4E File Offset: 0x000AE14E
		public static int GetDigitValue(string s, int index)
		{
			if (s == null)
			{
				throw new ArgumentNullException("s");
			}
			if (index < 0 || index >= s.Length)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			return (int)CharUnicodeInfo.InternalGetDigitValue(CharUnicodeInfo.InternalConvertToUtf32(s, index));
		}

		// Token: 0x06002DFB RID: 11771 RVA: 0x000AFF8C File Offset: 0x000AE18C
		[__DynamicallyInvokable]
		public static UnicodeCategory GetUnicodeCategory(char ch)
		{
			return CharUnicodeInfo.InternalGetUnicodeCategory((int)ch);
		}

		// Token: 0x06002DFC RID: 11772 RVA: 0x000AFF94 File Offset: 0x000AE194
		[__DynamicallyInvokable]
		public static UnicodeCategory GetUnicodeCategory(string s, int index)
		{
			if (s == null)
			{
				throw new ArgumentNullException("s");
			}
			if (index >= s.Length)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			return CharUnicodeInfo.InternalGetUnicodeCategory(s, index);
		}

		// Token: 0x06002DFD RID: 11773 RVA: 0x000AFFBF File Offset: 0x000AE1BF
		internal static UnicodeCategory InternalGetUnicodeCategory(int ch)
		{
			return (UnicodeCategory)CharUnicodeInfo.InternalGetCategoryValue(ch, 0);
		}

		// Token: 0x06002DFE RID: 11774 RVA: 0x000AFFC8 File Offset: 0x000AE1C8
		[SecuritySafeCritical]
		internal unsafe static byte InternalGetCategoryValue(int ch, int offset)
		{
			ushort num = CharUnicodeInfo.s_pCategoryLevel1Index[ch >> 8];
			num = CharUnicodeInfo.s_pCategoryLevel1Index[(int)num + ((ch >> 4) & 15)];
			byte* ptr = (byte*)(CharUnicodeInfo.s_pCategoryLevel1Index + num);
			byte b = ptr[ch & 15];
			return CharUnicodeInfo.s_pCategoriesValue[(int)(b * 2) + offset];
		}

		// Token: 0x06002DFF RID: 11775 RVA: 0x000B0018 File Offset: 0x000AE218
		internal static BidiCategory GetBidiCategory(string s, int index)
		{
			if (s == null)
			{
				throw new ArgumentNullException("s");
			}
			if (index >= s.Length)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			return (BidiCategory)CharUnicodeInfo.InternalGetCategoryValue(CharUnicodeInfo.InternalConvertToUtf32(s, index), 1);
		}

		// Token: 0x06002E00 RID: 11776 RVA: 0x000B0049 File Offset: 0x000AE249
		internal static UnicodeCategory InternalGetUnicodeCategory(string value, int index)
		{
			return CharUnicodeInfo.InternalGetUnicodeCategory(CharUnicodeInfo.InternalConvertToUtf32(value, index));
		}

		// Token: 0x06002E01 RID: 11777 RVA: 0x000B0057 File Offset: 0x000AE257
		internal static UnicodeCategory InternalGetUnicodeCategory(string str, int index, out int charLength)
		{
			return CharUnicodeInfo.InternalGetUnicodeCategory(CharUnicodeInfo.InternalConvertToUtf32(str, index, out charLength));
		}

		// Token: 0x06002E02 RID: 11778 RVA: 0x000B0066 File Offset: 0x000AE266
		internal static bool IsCombiningCategory(UnicodeCategory uc)
		{
			return uc == UnicodeCategory.NonSpacingMark || uc == UnicodeCategory.SpacingCombiningMark || uc == UnicodeCategory.EnclosingMark;
		}

		// Token: 0x04001300 RID: 4864
		internal const char HIGH_SURROGATE_START = '\ud800';

		// Token: 0x04001301 RID: 4865
		internal const char HIGH_SURROGATE_END = '\udbff';

		// Token: 0x04001302 RID: 4866
		internal const char LOW_SURROGATE_START = '\udc00';

		// Token: 0x04001303 RID: 4867
		internal const char LOW_SURROGATE_END = '\udfff';

		// Token: 0x04001304 RID: 4868
		internal const int UNICODE_CATEGORY_OFFSET = 0;

		// Token: 0x04001305 RID: 4869
		internal const int BIDI_CATEGORY_OFFSET = 1;

		// Token: 0x04001306 RID: 4870
		private static bool s_initialized = CharUnicodeInfo.InitTable();

		// Token: 0x04001307 RID: 4871
		[SecurityCritical]
		private unsafe static ushort* s_pCategoryLevel1Index;

		// Token: 0x04001308 RID: 4872
		[SecurityCritical]
		private unsafe static byte* s_pCategoriesValue;

		// Token: 0x04001309 RID: 4873
		[SecurityCritical]
		private unsafe static ushort* s_pNumericLevel1Index;

		// Token: 0x0400130A RID: 4874
		[SecurityCritical]
		private unsafe static byte* s_pNumericValues;

		// Token: 0x0400130B RID: 4875
		[SecurityCritical]
		private unsafe static CharUnicodeInfo.DigitValues* s_pDigitValues;

		// Token: 0x0400130C RID: 4876
		internal const string UNICODE_INFO_FILE_NAME = "charinfo.nlp";

		// Token: 0x0400130D RID: 4877
		internal const int UNICODE_PLANE01_START = 65536;

		// Token: 0x02000B6B RID: 2923
		[StructLayout(LayoutKind.Explicit)]
		internal struct UnicodeDataHeader
		{
			// Token: 0x04003458 RID: 13400
			[FieldOffset(0)]
			internal char TableName;

			// Token: 0x04003459 RID: 13401
			[FieldOffset(32)]
			internal ushort version;

			// Token: 0x0400345A RID: 13402
			[FieldOffset(40)]
			internal uint OffsetToCategoriesIndex;

			// Token: 0x0400345B RID: 13403
			[FieldOffset(44)]
			internal uint OffsetToCategoriesValue;

			// Token: 0x0400345C RID: 13404
			[FieldOffset(48)]
			internal uint OffsetToNumbericIndex;

			// Token: 0x0400345D RID: 13405
			[FieldOffset(52)]
			internal uint OffsetToDigitValue;

			// Token: 0x0400345E RID: 13406
			[FieldOffset(56)]
			internal uint OffsetToNumbericValue;
		}

		// Token: 0x02000B6C RID: 2924
		[StructLayout(LayoutKind.Sequential, Pack = 2)]
		internal struct DigitValues
		{
			// Token: 0x0400345F RID: 13407
			internal sbyte decimalDigit;

			// Token: 0x04003460 RID: 13408
			internal sbyte digit;
		}
	}
}
