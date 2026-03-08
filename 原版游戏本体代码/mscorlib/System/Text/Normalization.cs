using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Text
{
	// Token: 0x02000A7C RID: 2684
	internal class Normalization
	{
		// Token: 0x060068B6 RID: 26806 RVA: 0x00162308 File Offset: 0x00160508
		[SecurityCritical]
		private unsafe static void InitializeForm(NormalizationForm form, string strDataFile)
		{
			byte* ptr = null;
			if (!Environment.IsWindows8OrAbove)
			{
				if (strDataFile == null)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidNormalizationForm"));
				}
				ptr = GlobalizationAssembly.GetGlobalizationResourceBytePtr(typeof(Normalization).Assembly, strDataFile);
				if (ptr == null)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidNormalizationForm"));
				}
			}
			Normalization.nativeNormalizationInitNormalization(form, ptr);
		}

		// Token: 0x060068B7 RID: 26807 RVA: 0x00162364 File Offset: 0x00160564
		[SecurityCritical]
		private static void EnsureInitialized(NormalizationForm form)
		{
			if (form <= (NormalizationForm)13)
			{
				switch (form)
				{
				case NormalizationForm.FormC:
					if (Normalization.NFC)
					{
						return;
					}
					Normalization.InitializeForm(form, "normnfc.nlp");
					Normalization.NFC = true;
					return;
				case NormalizationForm.FormD:
					if (Normalization.NFD)
					{
						return;
					}
					Normalization.InitializeForm(form, "normnfd.nlp");
					Normalization.NFD = true;
					return;
				case (NormalizationForm)3:
				case (NormalizationForm)4:
					break;
				case NormalizationForm.FormKC:
					if (Normalization.NFKC)
					{
						return;
					}
					Normalization.InitializeForm(form, "normnfkc.nlp");
					Normalization.NFKC = true;
					return;
				case NormalizationForm.FormKD:
					if (Normalization.NFKD)
					{
						return;
					}
					Normalization.InitializeForm(form, "normnfkd.nlp");
					Normalization.NFKD = true;
					return;
				default:
					if (form == (NormalizationForm)13)
					{
						if (Normalization.IDNA)
						{
							return;
						}
						Normalization.InitializeForm(form, "normidna.nlp");
						Normalization.IDNA = true;
						return;
					}
					break;
				}
			}
			else
			{
				switch (form)
				{
				case (NormalizationForm)257:
					if (Normalization.NFCDisallowUnassigned)
					{
						return;
					}
					Normalization.InitializeForm(form, "normnfc.nlp");
					Normalization.NFCDisallowUnassigned = true;
					return;
				case (NormalizationForm)258:
					if (Normalization.NFDDisallowUnassigned)
					{
						return;
					}
					Normalization.InitializeForm(form, "normnfd.nlp");
					Normalization.NFDDisallowUnassigned = true;
					return;
				case (NormalizationForm)259:
				case (NormalizationForm)260:
					break;
				case (NormalizationForm)261:
					if (Normalization.NFKCDisallowUnassigned)
					{
						return;
					}
					Normalization.InitializeForm(form, "normnfkc.nlp");
					Normalization.NFKCDisallowUnassigned = true;
					return;
				case (NormalizationForm)262:
					if (Normalization.NFKDDisallowUnassigned)
					{
						return;
					}
					Normalization.InitializeForm(form, "normnfkd.nlp");
					Normalization.NFKDDisallowUnassigned = true;
					return;
				default:
					if (form == (NormalizationForm)269)
					{
						if (Normalization.IDNADisallowUnassigned)
						{
							return;
						}
						Normalization.InitializeForm(form, "normidna.nlp");
						Normalization.IDNADisallowUnassigned = true;
						return;
					}
					break;
				}
			}
			if (Normalization.Other)
			{
				return;
			}
			Normalization.InitializeForm(form, null);
			Normalization.Other = true;
		}

		// Token: 0x060068B8 RID: 26808 RVA: 0x00162520 File Offset: 0x00160720
		[SecurityCritical]
		internal static bool IsNormalized(string strInput, NormalizationForm normForm)
		{
			Normalization.EnsureInitialized(normForm);
			int num = 0;
			bool result = Normalization.nativeNormalizationIsNormalizedString(normForm, ref num, strInput, strInput.Length);
			if (num <= 8)
			{
				if (num == 0)
				{
					return result;
				}
				if (num == 8)
				{
					throw new OutOfMemoryException(Environment.GetResourceString("Arg_OutOfMemoryException"));
				}
			}
			else if (num == 87 || num == 1113)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex"), "strInput");
			}
			throw new InvalidOperationException(Environment.GetResourceString("UnknownError_Num", new object[] { num }));
		}

		// Token: 0x060068B9 RID: 26809 RVA: 0x001625A4 File Offset: 0x001607A4
		[SecurityCritical]
		internal static string Normalize(string strInput, NormalizationForm normForm)
		{
			Normalization.EnsureInitialized(normForm);
			int num = 0;
			int num2 = Normalization.nativeNormalizationNormalizeString(normForm, ref num, strInput, strInput.Length, null, 0);
			if (num != 0)
			{
				if (num == 87)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex"), "strInput");
				}
				if (num == 8)
				{
					throw new OutOfMemoryException(Environment.GetResourceString("Arg_OutOfMemoryException"));
				}
				throw new InvalidOperationException(Environment.GetResourceString("UnknownError_Num", new object[] { num }));
			}
			else
			{
				if (num2 == 0)
				{
					return string.Empty;
				}
				char[] array;
				for (;;)
				{
					array = new char[num2];
					num2 = Normalization.nativeNormalizationNormalizeString(normForm, ref num, strInput, strInput.Length, array, array.Length);
					if (num == 0)
					{
						goto IL_103;
					}
					if (num <= 87)
					{
						break;
					}
					if (num != 122)
					{
						goto Block_9;
					}
				}
				if (num == 8)
				{
					throw new OutOfMemoryException(Environment.GetResourceString("Arg_OutOfMemoryException"));
				}
				if (num != 87)
				{
					goto IL_E4;
				}
				goto IL_B0;
				Block_9:
				if (num != 1113)
				{
					goto IL_E4;
				}
				IL_B0:
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequence", new object[] { num2 }), "strInput");
				IL_E4:
				throw new InvalidOperationException(Environment.GetResourceString("UnknownError_Num", new object[] { num }));
				IL_103:
				return new string(array, 0, num2);
			}
		}

		// Token: 0x060068BA RID: 26810
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern int nativeNormalizationNormalizeString(NormalizationForm normForm, ref int iError, string lpSrcString, int cwSrcLength, char[] lpDstString, int cwDstLength);

		// Token: 0x060068BB RID: 26811
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool nativeNormalizationIsNormalizedString(NormalizationForm normForm, ref int iError, string lpString, int cwLength);

		// Token: 0x060068BC RID: 26812
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private unsafe static extern void nativeNormalizationInitNormalization(NormalizationForm normForm, byte* pTableData);

		// Token: 0x04002EED RID: 12013
		private static volatile bool NFC;

		// Token: 0x04002EEE RID: 12014
		private static volatile bool NFD;

		// Token: 0x04002EEF RID: 12015
		private static volatile bool NFKC;

		// Token: 0x04002EF0 RID: 12016
		private static volatile bool NFKD;

		// Token: 0x04002EF1 RID: 12017
		private static volatile bool IDNA;

		// Token: 0x04002EF2 RID: 12018
		private static volatile bool NFCDisallowUnassigned;

		// Token: 0x04002EF3 RID: 12019
		private static volatile bool NFDDisallowUnassigned;

		// Token: 0x04002EF4 RID: 12020
		private static volatile bool NFKCDisallowUnassigned;

		// Token: 0x04002EF5 RID: 12021
		private static volatile bool NFKDDisallowUnassigned;

		// Token: 0x04002EF6 RID: 12022
		private static volatile bool IDNADisallowUnassigned;

		// Token: 0x04002EF7 RID: 12023
		private static volatile bool Other;

		// Token: 0x04002EF8 RID: 12024
		private const int ERROR_SUCCESS = 0;

		// Token: 0x04002EF9 RID: 12025
		private const int ERROR_NOT_ENOUGH_MEMORY = 8;

		// Token: 0x04002EFA RID: 12026
		private const int ERROR_INVALID_PARAMETER = 87;

		// Token: 0x04002EFB RID: 12027
		private const int ERROR_INSUFFICIENT_BUFFER = 122;

		// Token: 0x04002EFC RID: 12028
		private const int ERROR_NO_UNICODE_TRANSLATION = 1113;
	}
}
