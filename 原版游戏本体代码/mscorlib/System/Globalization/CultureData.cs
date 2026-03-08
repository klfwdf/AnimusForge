using System;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace System.Globalization
{
	// Token: 0x020003DC RID: 988
	[FriendAccessAllowed]
	internal class CultureData
	{
		// Token: 0x17000723 RID: 1827
		// (get) Token: 0x06003255 RID: 12885 RVA: 0x000C193C File Offset: 0x000BFB3C
		private static Dictionary<string, string> RegionNames
		{
			get
			{
				if (CultureData.s_RegionNames == null)
				{
					Dictionary<string, string> dictionary = new Dictionary<string, string>
					{
						{ "029", "en-029" },
						{ "AE", "ar-AE" },
						{ "AF", "prs-AF" },
						{ "AL", "sq-AL" },
						{ "AM", "hy-AM" },
						{ "AR", "es-AR" },
						{ "AT", "de-AT" },
						{ "AU", "en-AU" },
						{ "AZ", "az-Cyrl-AZ" },
						{ "BA", "bs-Latn-BA" },
						{ "BD", "bn-BD" },
						{ "BE", "nl-BE" },
						{ "BG", "bg-BG" },
						{ "BH", "ar-BH" },
						{ "BN", "ms-BN" },
						{ "BO", "es-BO" },
						{ "BR", "pt-BR" },
						{ "BY", "be-BY" },
						{ "BZ", "en-BZ" },
						{ "CA", "en-CA" },
						{ "CH", "it-CH" },
						{ "CL", "es-CL" },
						{ "CN", "zh-CN" },
						{ "CO", "es-CO" },
						{ "CR", "es-CR" },
						{ "CS", "sr-Cyrl-CS" },
						{ "CZ", "cs-CZ" },
						{ "DE", "de-DE" },
						{ "DK", "da-DK" },
						{ "DO", "es-DO" },
						{ "DZ", "ar-DZ" },
						{ "EC", "es-EC" },
						{ "EE", "et-EE" },
						{ "EG", "ar-EG" },
						{ "ES", "es-ES" },
						{ "ET", "am-ET" },
						{ "FI", "fi-FI" },
						{ "FO", "fo-FO" },
						{ "FR", "fr-FR" },
						{ "GB", "en-GB" },
						{ "GE", "ka-GE" },
						{ "GL", "kl-GL" },
						{ "GR", "el-GR" },
						{ "GT", "es-GT" },
						{ "HK", "zh-HK" },
						{ "HN", "es-HN" },
						{ "HR", "hr-HR" },
						{ "HU", "hu-HU" },
						{ "ID", "id-ID" },
						{ "IE", "en-IE" },
						{ "IL", "he-IL" },
						{ "IN", "hi-IN" },
						{ "IQ", "ar-IQ" },
						{ "IR", "fa-IR" },
						{ "IS", "is-IS" },
						{ "IT", "it-IT" },
						{ "IV", "" },
						{ "JM", "en-JM" },
						{ "JO", "ar-JO" },
						{ "JP", "ja-JP" },
						{ "KE", "sw-KE" },
						{ "KG", "ky-KG" },
						{ "KH", "km-KH" },
						{ "KR", "ko-KR" },
						{ "KW", "ar-KW" },
						{ "KZ", "kk-KZ" },
						{ "LA", "lo-LA" },
						{ "LB", "ar-LB" },
						{ "LI", "de-LI" },
						{ "LK", "si-LK" },
						{ "LT", "lt-LT" },
						{ "LU", "lb-LU" },
						{ "LV", "lv-LV" },
						{ "LY", "ar-LY" },
						{ "MA", "ar-MA" },
						{ "MC", "fr-MC" },
						{ "ME", "sr-Latn-ME" },
						{ "MK", "mk-MK" },
						{ "MN", "mn-MN" },
						{ "MO", "zh-MO" },
						{ "MT", "mt-MT" },
						{ "MV", "dv-MV" },
						{ "MX", "es-MX" },
						{ "MY", "ms-MY" },
						{ "NG", "ig-NG" },
						{ "NI", "es-NI" },
						{ "NL", "nl-NL" },
						{ "NO", "nn-NO" },
						{ "NP", "ne-NP" },
						{ "NZ", "en-NZ" },
						{ "OM", "ar-OM" },
						{ "PA", "es-PA" },
						{ "PE", "es-PE" },
						{ "PH", "en-PH" },
						{ "PK", "ur-PK" },
						{ "PL", "pl-PL" },
						{ "PR", "es-PR" },
						{ "PT", "pt-PT" },
						{ "PY", "es-PY" },
						{ "QA", "ar-QA" },
						{ "RO", "ro-RO" },
						{ "RS", "sr-Latn-RS" },
						{ "RU", "ru-RU" },
						{ "RW", "rw-RW" },
						{ "SA", "ar-SA" },
						{ "SE", "sv-SE" },
						{ "SG", "zh-SG" },
						{ "SI", "sl-SI" },
						{ "SK", "sk-SK" },
						{ "SN", "wo-SN" },
						{ "SV", "es-SV" },
						{ "SY", "ar-SY" },
						{ "TH", "th-TH" },
						{ "TJ", "tg-Cyrl-TJ" },
						{ "TM", "tk-TM" },
						{ "TN", "ar-TN" },
						{ "TR", "tr-TR" },
						{ "TT", "en-TT" },
						{ "TW", "zh-TW" },
						{ "UA", "uk-UA" },
						{ "US", "en-US" },
						{ "UY", "es-UY" },
						{ "UZ", "uz-Cyrl-UZ" },
						{ "VE", "es-VE" },
						{ "VN", "vi-VN" },
						{ "YE", "ar-YE" },
						{ "ZA", "af-ZA" },
						{ "ZW", "en-ZW" }
					};
					CultureData.s_RegionNames = dictionary;
				}
				return CultureData.s_RegionNames;
			}
		}

		// Token: 0x17000724 RID: 1828
		// (get) Token: 0x06003256 RID: 12886 RVA: 0x000C216C File Offset: 0x000C036C
		internal static CultureData Invariant
		{
			get
			{
				if (CultureData.s_Invariant == null)
				{
					CultureData cultureData = new CultureData();
					cultureData.bUseOverrides = false;
					cultureData.sRealName = "";
					CultureData.nativeInitCultureData(cultureData);
					cultureData.bUseOverrides = false;
					cultureData.sRealName = "";
					cultureData.sWindowsName = "";
					cultureData.sName = "";
					cultureData.sParent = "";
					cultureData.bNeutral = false;
					cultureData.bFramework = true;
					cultureData.sEnglishDisplayName = "Invariant Language (Invariant Country)";
					cultureData.sNativeDisplayName = "Invariant Language (Invariant Country)";
					cultureData.sSpecificCulture = "";
					cultureData.sISO639Language = "iv";
					cultureData.sLocalizedLanguage = "Invariant Language";
					cultureData.sEnglishLanguage = "Invariant Language";
					cultureData.sNativeLanguage = "Invariant Language";
					cultureData.sRegionName = "IV";
					cultureData.iGeoId = 244;
					cultureData.sEnglishCountry = "Invariant Country";
					cultureData.sNativeCountry = "Invariant Country";
					cultureData.sISO3166CountryName = "IV";
					cultureData.sPositiveSign = "+";
					cultureData.sNegativeSign = "-";
					cultureData.saNativeDigits = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
					cultureData.iDigitSubstitution = 1;
					cultureData.iLeadingZeros = 1;
					cultureData.iDigits = 2;
					cultureData.iNegativeNumber = 1;
					cultureData.waGrouping = new int[] { 3 };
					cultureData.sDecimalSeparator = ".";
					cultureData.sThousandSeparator = ",";
					cultureData.sNaN = "NaN";
					cultureData.sPositiveInfinity = "Infinity";
					cultureData.sNegativeInfinity = "-Infinity";
					cultureData.iNegativePercent = 0;
					cultureData.iPositivePercent = 0;
					cultureData.sPercent = "%";
					cultureData.sPerMille = "‰";
					cultureData.sCurrency = "¤";
					cultureData.sIntlMonetarySymbol = "XDR";
					cultureData.sEnglishCurrency = "International Monetary Fund";
					cultureData.sNativeCurrency = "International Monetary Fund";
					cultureData.iCurrencyDigits = 2;
					cultureData.iCurrency = 0;
					cultureData.iNegativeCurrency = 0;
					cultureData.waMonetaryGrouping = new int[] { 3 };
					cultureData.sMonetaryDecimal = ".";
					cultureData.sMonetaryThousand = ",";
					cultureData.iMeasure = 0;
					cultureData.sListSeparator = ",";
					cultureData.sAM1159 = "AM";
					cultureData.sPM2359 = "PM";
					cultureData.saLongTimes = new string[] { "HH:mm:ss" };
					cultureData.saShortTimes = new string[] { "HH:mm", "hh:mm tt", "H:mm", "h:mm tt" };
					cultureData.saDurationFormats = new string[] { "HH:mm:ss" };
					cultureData.iFirstDayOfWeek = 0;
					cultureData.iFirstWeekOfYear = 0;
					cultureData.waCalendars = new int[] { 1 };
					cultureData.calendars = new CalendarData[23];
					cultureData.calendars[0] = CalendarData.Invariant;
					cultureData.iReadingLayout = 0;
					cultureData.sTextInfo = "";
					cultureData.sCompareInfo = "";
					cultureData.sScripts = "Latn;";
					cultureData.iLanguage = 127;
					cultureData.iDefaultAnsiCodePage = 1252;
					cultureData.iDefaultOemCodePage = 437;
					cultureData.iDefaultMacCodePage = 10000;
					cultureData.iDefaultEbcdicCodePage = 37;
					cultureData.sAbbrevLang = "IVL";
					cultureData.sAbbrevCountry = "IVC";
					cultureData.sISO639Language2 = "ivl";
					cultureData.sISO3166CountryName2 = "ivc";
					cultureData.iInputLanguageHandle = 127;
					cultureData.sConsoleFallbackName = "";
					cultureData.sKeyboardsToInstall = "0409:00000409";
					CultureData.s_Invariant = cultureData;
				}
				return CultureData.s_Invariant;
			}
		}

		// Token: 0x06003257 RID: 12887 RVA: 0x000C2539 File Offset: 0x000C0739
		[SecurityCritical]
		private static bool IsResourcePresent(string resourceKey)
		{
			if (CultureData.MscorlibResourceSet == null)
			{
				CultureData.MscorlibResourceSet = new ResourceSet(typeof(Environment).Assembly.GetManifestResourceStream("mscorlib.resources"));
			}
			return CultureData.MscorlibResourceSet.GetString(resourceKey) != null;
		}

		// Token: 0x06003258 RID: 12888 RVA: 0x000C257C File Offset: 0x000C077C
		[FriendAccessAllowed]
		internal static CultureData GetCultureData(string cultureName, bool useUserOverride)
		{
			if (string.IsNullOrEmpty(cultureName))
			{
				return CultureData.Invariant;
			}
			if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
			{
				if (cultureName.Equals("iw", StringComparison.OrdinalIgnoreCase))
				{
					cultureName = "he";
				}
				else if (cultureName.Equals("tl", StringComparison.OrdinalIgnoreCase))
				{
					cultureName = "fil";
				}
				else if (cultureName.Equals("english", StringComparison.OrdinalIgnoreCase))
				{
					cultureName = "en";
				}
			}
			string key = CultureData.AnsiToLower(useUserOverride ? cultureName : (cultureName + "*"));
			Dictionary<string, CultureData> dictionary = CultureData.s_cachedCultures;
			if (dictionary == null)
			{
				dictionary = new Dictionary<string, CultureData>();
			}
			else
			{
				object syncRoot = ((ICollection)dictionary).SyncRoot;
				CultureData cultureData;
				lock (syncRoot)
				{
					dictionary.TryGetValue(key, out cultureData);
				}
				if (cultureData != null)
				{
					return cultureData;
				}
			}
			CultureData cultureData2 = CultureData.CreateCultureData(cultureName, useUserOverride);
			if (cultureData2 == null)
			{
				return null;
			}
			object syncRoot2 = ((ICollection)dictionary).SyncRoot;
			lock (syncRoot2)
			{
				dictionary[key] = cultureData2;
			}
			CultureData.s_cachedCultures = dictionary;
			return cultureData2;
		}

		// Token: 0x06003259 RID: 12889 RVA: 0x000C2698 File Offset: 0x000C0898
		private static CultureData CreateCultureData(string cultureName, bool useUserOverride)
		{
			CultureData cultureData = new CultureData();
			cultureData.bUseOverrides = useUserOverride;
			cultureData.sRealName = cultureName;
			if (!cultureData.InitCultureData() && !cultureData.InitCompatibilityCultureData() && !cultureData.InitLegacyAlternateSortData())
			{
				return null;
			}
			return cultureData;
		}

		// Token: 0x0600325A RID: 12890 RVA: 0x000C26D4 File Offset: 0x000C08D4
		private bool InitCultureData()
		{
			if (!CultureData.nativeInitCultureData(this))
			{
				return false;
			}
			if (CultureInfo.IsTaiwanSku)
			{
				this.TreatTaiwanParentChainAsHavingTaiwanAsSpecific();
			}
			return true;
		}

		// Token: 0x0600325B RID: 12891 RVA: 0x000C26F0 File Offset: 0x000C08F0
		[SecuritySafeCritical]
		private void TreatTaiwanParentChainAsHavingTaiwanAsSpecific()
		{
			if (this.IsNeutralInParentChainOfTaiwan() && CultureData.IsOsPriorToWin7() && !this.IsReplacementCulture)
			{
				string text = this.SNATIVELANGUAGE;
				text = this.SENGLISHLANGUAGE;
				text = this.SLOCALIZEDLANGUAGE;
				text = this.STEXTINFO;
				text = this.SCOMPAREINFO;
				text = this.FONTSIGNATURE;
				int num = this.IDEFAULTANSICODEPAGE;
				num = this.IDEFAULTOEMCODEPAGE;
				num = this.IDEFAULTMACCODEPAGE;
				this.sSpecificCulture = "zh-TW";
				this.sWindowsName = "zh-TW";
			}
		}

		// Token: 0x0600325C RID: 12892 RVA: 0x000C2769 File Offset: 0x000C0969
		private bool IsNeutralInParentChainOfTaiwan()
		{
			return this.sRealName == "zh" || this.sRealName == "zh-Hant";
		}

		// Token: 0x0600325D RID: 12893 RVA: 0x000C278F File Offset: 0x000C098F
		private static bool IsOsPriorToWin7()
		{
			return Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version < CultureData.s_win7Version;
		}

		// Token: 0x0600325E RID: 12894 RVA: 0x000C27B4 File Offset: 0x000C09B4
		private static bool IsOsWin7OrPrior()
		{
			return Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version < new Version(6, 2);
		}

		// Token: 0x0600325F RID: 12895 RVA: 0x000C27DC File Offset: 0x000C09DC
		private bool InitCompatibilityCultureData()
		{
			string testString = this.sRealName;
			string a = CultureData.AnsiToLower(testString);
			string text;
			string text2;
			if (!(a == "zh-chs"))
			{
				if (!(a == "zh-cht"))
				{
					return false;
				}
				text = "zh-Hant";
				text2 = "zh-CHT";
			}
			else
			{
				text = "zh-Hans";
				text2 = "zh-CHS";
			}
			this.sRealName = text;
			if (!this.InitCultureData())
			{
				return false;
			}
			this.sName = text2;
			this.sParent = text;
			this.bFramework = true;
			return true;
		}

		// Token: 0x06003260 RID: 12896 RVA: 0x000C2858 File Offset: 0x000C0A58
		private bool InitLegacyAlternateSortData()
		{
			if (!CompareInfo.IsLegacy20SortingBehaviorRequested)
			{
				return false;
			}
			string testString = this.sRealName;
			string a = CultureData.AnsiToLower(testString);
			if (!(a == "ko-kr_unicod"))
			{
				if (!(a == "ja-jp_unicod"))
				{
					if (!(a == "zh-hk_stroke"))
					{
						return false;
					}
					testString = "zh-HK_stroke";
					this.sRealName = "zh-HK";
					this.iLanguage = 134148;
				}
				else
				{
					testString = "ja-JP_unicod";
					this.sRealName = "ja-JP";
					this.iLanguage = 66577;
				}
			}
			else
			{
				testString = "ko-KR_unicod";
				this.sRealName = "ko-KR";
				this.iLanguage = 66578;
			}
			if (!CultureData.nativeInitCultureData(this))
			{
				return false;
			}
			this.sRealName = testString;
			this.sCompareInfo = testString;
			this.bFramework = true;
			return true;
		}

		// Token: 0x06003261 RID: 12897 RVA: 0x000C2924 File Offset: 0x000C0B24
		[SecurityCritical]
		internal static CultureData GetCultureDataForRegion(string cultureName, bool useUserOverride)
		{
			if (string.IsNullOrEmpty(cultureName))
			{
				return CultureData.Invariant;
			}
			CultureData cultureData = CultureData.GetCultureData(cultureName, useUserOverride);
			if (cultureData != null && !cultureData.IsNeutralCulture)
			{
				return cultureData;
			}
			CultureData cultureData2 = cultureData;
			string key = CultureData.AnsiToLower(useUserOverride ? cultureName : (cultureName + "*"));
			Dictionary<string, CultureData> dictionary = CultureData.s_cachedRegions;
			if (dictionary == null)
			{
				dictionary = new Dictionary<string, CultureData>();
			}
			else
			{
				object syncRoot = ((ICollection)dictionary).SyncRoot;
				lock (syncRoot)
				{
					dictionary.TryGetValue(key, out cultureData);
				}
				if (cultureData != null)
				{
					return cultureData;
				}
			}
			try
			{
				RegistryKey registryKey = Registry.LocalMachine.InternalOpenSubKey(CultureData.s_RegionKey, false);
				if (registryKey != null)
				{
					try
					{
						object obj = registryKey.InternalGetValue(cultureName, null, false, false);
						if (obj != null)
						{
							string cultureName2 = obj.ToString();
							cultureData = CultureData.GetCultureData(cultureName2, useUserOverride);
						}
					}
					finally
					{
						registryKey.Close();
					}
				}
			}
			catch (ObjectDisposedException)
			{
			}
			catch (ArgumentException)
			{
			}
			if ((cultureData == null || cultureData.IsNeutralCulture) && CultureData.RegionNames.ContainsKey(cultureName))
			{
				cultureData = CultureData.GetCultureData(CultureData.RegionNames[cultureName], useUserOverride);
			}
			if (cultureData == null || cultureData.IsNeutralCulture)
			{
				CultureInfo[] array = CultureData.SpecificCultures;
				for (int i = 0; i < array.Length; i++)
				{
					if (string.Compare(array[i].m_cultureData.SREGIONNAME, cultureName, StringComparison.OrdinalIgnoreCase) == 0)
					{
						cultureData = array[i].m_cultureData;
						break;
					}
				}
			}
			if (cultureData != null && !cultureData.IsNeutralCulture)
			{
				object syncRoot2 = ((ICollection)dictionary).SyncRoot;
				lock (syncRoot2)
				{
					dictionary[key] = cultureData;
				}
				CultureData.s_cachedRegions = dictionary;
			}
			else
			{
				cultureData = cultureData2;
			}
			return cultureData;
		}

		// Token: 0x06003262 RID: 12898
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern string LCIDToLocaleName(int lcid);

		// Token: 0x06003263 RID: 12899 RVA: 0x000C2AF4 File Offset: 0x000C0CF4
		internal static CultureData GetCultureData(int culture, bool bUseUserOverride)
		{
			string text = null;
			CultureData cultureData = null;
			if (CompareInfo.IsLegacy20SortingBehaviorRequested)
			{
				if (culture != 66577)
				{
					if (culture != 66578)
					{
						if (culture == 134148)
						{
							text = "zh-HK_stroke";
						}
					}
					else
					{
						text = "ko-KR_unicod";
					}
				}
				else
				{
					text = "ja-JP_unicod";
				}
			}
			if (text == null)
			{
				text = CultureData.LCIDToLocaleName(culture);
			}
			if (string.IsNullOrEmpty(text))
			{
				if (culture == 127)
				{
					return CultureData.Invariant;
				}
			}
			else
			{
				if (!(text == "zh-Hans"))
				{
					if (text == "zh-Hant")
					{
						text = "zh-CHT";
					}
				}
				else
				{
					text = "zh-CHS";
				}
				cultureData = CultureData.GetCultureData(text, bUseUserOverride);
			}
			if (cultureData == null)
			{
				throw new CultureNotFoundException("culture", culture, Environment.GetResourceString("Argument_CultureNotSupported"));
			}
			return cultureData;
		}

		// Token: 0x06003264 RID: 12900 RVA: 0x000C2BA5 File Offset: 0x000C0DA5
		internal static void ClearCachedData()
		{
			CultureData.s_cachedCultures = null;
			CultureData.s_cachedRegions = null;
			CultureData.s_replacementCultureNames = null;
		}

		// Token: 0x06003265 RID: 12901 RVA: 0x000C2BC0 File Offset: 0x000C0DC0
		[SecuritySafeCritical]
		internal static CultureInfo[] GetCultures(CultureTypes types)
		{
			if (types <= (CultureTypes)0 || (types & ~(CultureTypes.NeutralCultures | CultureTypes.SpecificCultures | CultureTypes.InstalledWin32Cultures | CultureTypes.UserCustomCulture | CultureTypes.ReplacementCultures | CultureTypes.WindowsOnlyCultures | CultureTypes.FrameworkCultures)) != (CultureTypes)0)
			{
				throw new ArgumentOutOfRangeException("types", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), CultureTypes.NeutralCultures, CultureTypes.FrameworkCultures));
			}
			if ((types & CultureTypes.WindowsOnlyCultures) != (CultureTypes)0)
			{
				types &= ~CultureTypes.WindowsOnlyCultures;
			}
			string[] array = null;
			if (CultureData.nativeEnumCultureNames((int)types, JitHelpers.GetObjectHandleOnStack<string[]>(ref array)) == 0)
			{
				return new CultureInfo[0];
			}
			int num = array.Length;
			if ((types & (CultureTypes.NeutralCultures | CultureTypes.FrameworkCultures)) != (CultureTypes)0)
			{
				num += 2;
			}
			CultureInfo[] array2 = new CultureInfo[num];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i] = new CultureInfo(array[i]);
			}
			if ((types & (CultureTypes.NeutralCultures | CultureTypes.FrameworkCultures)) != (CultureTypes)0)
			{
				array2[array.Length] = new CultureInfo("zh-CHS");
				array2[array.Length + 1] = new CultureInfo("zh-CHT");
			}
			return array2;
		}

		// Token: 0x17000725 RID: 1829
		// (get) Token: 0x06003266 RID: 12902 RVA: 0x000C2C7C File Offset: 0x000C0E7C
		private static CultureInfo[] SpecificCultures
		{
			get
			{
				if (CultureData.specificCultures == null)
				{
					CultureData.specificCultures = CultureData.GetCultures(CultureTypes.SpecificCultures);
				}
				return CultureData.specificCultures;
			}
		}

		// Token: 0x17000726 RID: 1830
		// (get) Token: 0x06003267 RID: 12903 RVA: 0x000C2C9B File Offset: 0x000C0E9B
		internal bool IsReplacementCulture
		{
			get
			{
				return CultureData.IsReplacementCultureName(this.SNAME);
			}
		}

		// Token: 0x06003268 RID: 12904 RVA: 0x000C2CA8 File Offset: 0x000C0EA8
		[SecuritySafeCritical]
		private static bool IsReplacementCultureName(string name)
		{
			string[] array = CultureData.s_replacementCultureNames;
			if (array == null)
			{
				if (CultureData.nativeEnumCultureNames(16, JitHelpers.GetObjectHandleOnStack<string[]>(ref array)) == 0)
				{
					return false;
				}
				Array.Sort<string>(array);
				CultureData.s_replacementCultureNames = array;
			}
			return Array.BinarySearch<string>(array, name) >= 0;
		}

		// Token: 0x17000727 RID: 1831
		// (get) Token: 0x06003269 RID: 12905 RVA: 0x000C2CF0 File Offset: 0x000C0EF0
		internal string CultureName
		{
			get
			{
				string a = this.sName;
				if (a == "zh-CHS" || a == "zh-CHT")
				{
					return this.sName;
				}
				return this.sRealName;
			}
		}

		// Token: 0x17000728 RID: 1832
		// (get) Token: 0x0600326A RID: 12906 RVA: 0x000C2D2B File Offset: 0x000C0F2B
		internal bool UseUserOverride
		{
			get
			{
				return this.bUseOverrides;
			}
		}

		// Token: 0x17000729 RID: 1833
		// (get) Token: 0x0600326B RID: 12907 RVA: 0x000C2D33 File Offset: 0x000C0F33
		internal string SNAME
		{
			get
			{
				if (this.sName == null)
				{
					this.sName = string.Empty;
				}
				return this.sName;
			}
		}

		// Token: 0x1700072A RID: 1834
		// (get) Token: 0x0600326C RID: 12908 RVA: 0x000C2D50 File Offset: 0x000C0F50
		internal string SPARENT
		{
			[SecurityCritical]
			get
			{
				if (this.sParent == null)
				{
					this.sParent = this.DoGetLocaleInfo(this.sRealName, 109U);
					string a = this.sParent;
					if (!(a == "zh-Hans"))
					{
						if (a == "zh-Hant")
						{
							this.sParent = "zh-CHT";
						}
					}
					else
					{
						this.sParent = "zh-CHS";
					}
				}
				return this.sParent;
			}
		}

		// Token: 0x1700072B RID: 1835
		// (get) Token: 0x0600326D RID: 12909 RVA: 0x000C2DBC File Offset: 0x000C0FBC
		internal string SLOCALIZEDDISPLAYNAME
		{
			[SecurityCritical]
			get
			{
				if (this.sLocalizedDisplayName == null)
				{
					string text = "Globalization.ci_" + this.sName;
					if (CultureData.IsResourcePresent(text))
					{
						this.sLocalizedDisplayName = Environment.GetResourceString(text);
					}
					if (string.IsNullOrEmpty(this.sLocalizedDisplayName))
					{
						if (this.IsNeutralCulture)
						{
							this.sLocalizedDisplayName = this.SLOCALIZEDLANGUAGE;
						}
						else
						{
							if (CultureInfo.UserDefaultUICulture.Name.Equals(Thread.CurrentThread.CurrentUICulture.Name))
							{
								this.sLocalizedDisplayName = this.DoGetLocaleInfo(2U);
							}
							if (string.IsNullOrEmpty(this.sLocalizedDisplayName))
							{
								this.sLocalizedDisplayName = this.SNATIVEDISPLAYNAME;
							}
						}
					}
				}
				return this.sLocalizedDisplayName;
			}
		}

		// Token: 0x1700072C RID: 1836
		// (get) Token: 0x0600326E RID: 12910 RVA: 0x000C2E68 File Offset: 0x000C1068
		internal string SENGDISPLAYNAME
		{
			[SecurityCritical]
			get
			{
				if (this.sEnglishDisplayName == null)
				{
					if (this.IsNeutralCulture)
					{
						this.sEnglishDisplayName = this.SENGLISHLANGUAGE;
						string a = this.sName;
						if (a == "zh-CHS" || a == "zh-CHT")
						{
							this.sEnglishDisplayName += " Legacy";
						}
					}
					else
					{
						this.sEnglishDisplayName = this.DoGetLocaleInfo(114U);
						if (string.IsNullOrEmpty(this.sEnglishDisplayName))
						{
							if (this.SENGLISHLANGUAGE.EndsWith(')'))
							{
								this.sEnglishDisplayName = this.SENGLISHLANGUAGE.Substring(0, this.sEnglishLanguage.Length - 1) + ", " + this.SENGCOUNTRY + ")";
							}
							else
							{
								this.sEnglishDisplayName = this.SENGLISHLANGUAGE + " (" + this.SENGCOUNTRY + ")";
							}
						}
					}
				}
				return this.sEnglishDisplayName;
			}
		}

		// Token: 0x1700072D RID: 1837
		// (get) Token: 0x0600326F RID: 12911 RVA: 0x000C2F5C File Offset: 0x000C115C
		internal string SNATIVEDISPLAYNAME
		{
			[SecurityCritical]
			get
			{
				if (this.sNativeDisplayName == null)
				{
					if (this.IsNeutralCulture)
					{
						this.sNativeDisplayName = this.SNATIVELANGUAGE;
						string a = this.sName;
						if (!(a == "zh-CHS"))
						{
							if (a == "zh-CHT")
							{
								this.sNativeDisplayName += " 舊版";
							}
						}
						else
						{
							this.sNativeDisplayName += " 旧版";
						}
					}
					else
					{
						if (this.IsIncorrectNativeLanguageForSinhala())
						{
							this.sNativeDisplayName = "ස\u0dd2\u0d82හල (ශ\u0dca\u200dර\u0dd3 ල\u0d82ක\u0dcf)";
						}
						else
						{
							this.sNativeDisplayName = this.DoGetLocaleInfo(115U);
						}
						if (string.IsNullOrEmpty(this.sNativeDisplayName))
						{
							this.sNativeDisplayName = this.SNATIVELANGUAGE + " (" + this.SNATIVECOUNTRY + ")";
						}
					}
				}
				return this.sNativeDisplayName;
			}
		}

		// Token: 0x1700072E RID: 1838
		// (get) Token: 0x06003270 RID: 12912 RVA: 0x000C3035 File Offset: 0x000C1235
		internal string SSPECIFICCULTURE
		{
			get
			{
				return this.sSpecificCulture;
			}
		}

		// Token: 0x1700072F RID: 1839
		// (get) Token: 0x06003271 RID: 12913 RVA: 0x000C303D File Offset: 0x000C123D
		internal string SISO639LANGNAME
		{
			[SecurityCritical]
			get
			{
				if (this.sISO639Language == null)
				{
					this.sISO639Language = this.DoGetLocaleInfo(89U);
				}
				return this.sISO639Language;
			}
		}

		// Token: 0x17000730 RID: 1840
		// (get) Token: 0x06003272 RID: 12914 RVA: 0x000C305B File Offset: 0x000C125B
		internal string SISO639LANGNAME2
		{
			[SecurityCritical]
			get
			{
				if (this.sISO639Language2 == null)
				{
					this.sISO639Language2 = this.DoGetLocaleInfo(103U);
				}
				return this.sISO639Language2;
			}
		}

		// Token: 0x17000731 RID: 1841
		// (get) Token: 0x06003273 RID: 12915 RVA: 0x000C3079 File Offset: 0x000C1279
		internal string SABBREVLANGNAME
		{
			[SecurityCritical]
			get
			{
				if (this.sAbbrevLang == null)
				{
					this.sAbbrevLang = this.DoGetLocaleInfo(3U);
				}
				return this.sAbbrevLang;
			}
		}

		// Token: 0x17000732 RID: 1842
		// (get) Token: 0x06003274 RID: 12916 RVA: 0x000C3098 File Offset: 0x000C1298
		internal string SLOCALIZEDLANGUAGE
		{
			[SecurityCritical]
			get
			{
				if (this.sLocalizedLanguage == null)
				{
					if (CultureInfo.UserDefaultUICulture.Name.Equals(Thread.CurrentThread.CurrentUICulture.Name))
					{
						this.sLocalizedLanguage = this.DoGetLocaleInfo(111U);
					}
					if (string.IsNullOrEmpty(this.sLocalizedLanguage))
					{
						this.sLocalizedLanguage = this.SNATIVELANGUAGE;
					}
				}
				return this.sLocalizedLanguage;
			}
		}

		// Token: 0x17000733 RID: 1843
		// (get) Token: 0x06003275 RID: 12917 RVA: 0x000C30FA File Offset: 0x000C12FA
		internal string SENGLISHLANGUAGE
		{
			[SecurityCritical]
			get
			{
				if (this.sEnglishLanguage == null)
				{
					this.sEnglishLanguage = this.DoGetLocaleInfo(4097U);
				}
				return this.sEnglishLanguage;
			}
		}

		// Token: 0x17000734 RID: 1844
		// (get) Token: 0x06003276 RID: 12918 RVA: 0x000C311B File Offset: 0x000C131B
		internal string SNATIVELANGUAGE
		{
			[SecurityCritical]
			get
			{
				if (this.sNativeLanguage == null)
				{
					if (this.IsIncorrectNativeLanguageForSinhala())
					{
						this.sNativeLanguage = "ස\u0dd2\u0d82හල";
					}
					else
					{
						this.sNativeLanguage = this.DoGetLocaleInfo(4U);
					}
				}
				return this.sNativeLanguage;
			}
		}

		// Token: 0x06003277 RID: 12919 RVA: 0x000C314D File Offset: 0x000C134D
		private bool IsIncorrectNativeLanguageForSinhala()
		{
			return CultureData.IsOsWin7OrPrior() && (this.sName == "si-LK" || this.sName == "si") && !this.IsReplacementCulture;
		}

		// Token: 0x17000735 RID: 1845
		// (get) Token: 0x06003278 RID: 12920 RVA: 0x000C3185 File Offset: 0x000C1385
		internal string SREGIONNAME
		{
			[SecurityCritical]
			get
			{
				if (this.sRegionName == null)
				{
					this.sRegionName = this.DoGetLocaleInfo(90U);
				}
				return this.sRegionName;
			}
		}

		// Token: 0x17000736 RID: 1846
		// (get) Token: 0x06003279 RID: 12921 RVA: 0x000C31A3 File Offset: 0x000C13A3
		internal int ICOUNTRY
		{
			get
			{
				return this.DoGetLocaleInfoInt(5U);
			}
		}

		// Token: 0x17000737 RID: 1847
		// (get) Token: 0x0600327A RID: 12922 RVA: 0x000C31AC File Offset: 0x000C13AC
		internal int IGEOID
		{
			get
			{
				if (this.iGeoId == -1)
				{
					this.iGeoId = this.DoGetLocaleInfoInt(91U);
				}
				return this.iGeoId;
			}
		}

		// Token: 0x17000738 RID: 1848
		// (get) Token: 0x0600327B RID: 12923 RVA: 0x000C31CC File Offset: 0x000C13CC
		internal string SLOCALIZEDCOUNTRY
		{
			[SecurityCritical]
			get
			{
				if (this.sLocalizedCountry == null)
				{
					string text = "Globalization.ri_" + this.SREGIONNAME;
					if (CultureData.IsResourcePresent(text))
					{
						this.sLocalizedCountry = Environment.GetResourceString(text);
					}
					if (string.IsNullOrEmpty(this.sLocalizedCountry))
					{
						if (CultureInfo.UserDefaultUICulture.Name.Equals(Thread.CurrentThread.CurrentUICulture.Name))
						{
							this.sLocalizedCountry = this.DoGetLocaleInfo(6U);
						}
						if (string.IsNullOrEmpty(this.sLocalizedDisplayName))
						{
							this.sLocalizedCountry = this.SNATIVECOUNTRY;
						}
					}
				}
				return this.sLocalizedCountry;
			}
		}

		// Token: 0x17000739 RID: 1849
		// (get) Token: 0x0600327C RID: 12924 RVA: 0x000C325F File Offset: 0x000C145F
		internal string SENGCOUNTRY
		{
			[SecurityCritical]
			get
			{
				if (this.sEnglishCountry == null)
				{
					this.sEnglishCountry = this.DoGetLocaleInfo(4098U);
				}
				return this.sEnglishCountry;
			}
		}

		// Token: 0x1700073A RID: 1850
		// (get) Token: 0x0600327D RID: 12925 RVA: 0x000C3280 File Offset: 0x000C1480
		internal string SNATIVECOUNTRY
		{
			[SecurityCritical]
			get
			{
				if (this.sNativeCountry == null)
				{
					this.sNativeCountry = this.DoGetLocaleInfo(8U);
				}
				return this.sNativeCountry;
			}
		}

		// Token: 0x1700073B RID: 1851
		// (get) Token: 0x0600327E RID: 12926 RVA: 0x000C329D File Offset: 0x000C149D
		internal string SISO3166CTRYNAME
		{
			[SecurityCritical]
			get
			{
				if (this.sISO3166CountryName == null)
				{
					this.sISO3166CountryName = this.DoGetLocaleInfo(90U);
				}
				return this.sISO3166CountryName;
			}
		}

		// Token: 0x1700073C RID: 1852
		// (get) Token: 0x0600327F RID: 12927 RVA: 0x000C32BB File Offset: 0x000C14BB
		internal string SISO3166CTRYNAME2
		{
			[SecurityCritical]
			get
			{
				if (this.sISO3166CountryName2 == null)
				{
					this.sISO3166CountryName2 = this.DoGetLocaleInfo(104U);
				}
				return this.sISO3166CountryName2;
			}
		}

		// Token: 0x1700073D RID: 1853
		// (get) Token: 0x06003280 RID: 12928 RVA: 0x000C32D9 File Offset: 0x000C14D9
		internal string SABBREVCTRYNAME
		{
			[SecurityCritical]
			get
			{
				if (this.sAbbrevCountry == null)
				{
					this.sAbbrevCountry = this.DoGetLocaleInfo(7U);
				}
				return this.sAbbrevCountry;
			}
		}

		// Token: 0x1700073E RID: 1854
		// (get) Token: 0x06003281 RID: 12929 RVA: 0x000C32F6 File Offset: 0x000C14F6
		private int IDEFAULTCOUNTRY
		{
			get
			{
				return this.DoGetLocaleInfoInt(10U);
			}
		}

		// Token: 0x1700073F RID: 1855
		// (get) Token: 0x06003282 RID: 12930 RVA: 0x000C3300 File Offset: 0x000C1500
		internal int IINPUTLANGUAGEHANDLE
		{
			get
			{
				if (this.iInputLanguageHandle == -1)
				{
					if (this.IsSupplementalCustomCulture)
					{
						this.iInputLanguageHandle = 1033;
					}
					else
					{
						this.iInputLanguageHandle = this.ILANGUAGE;
					}
				}
				return this.iInputLanguageHandle;
			}
		}

		// Token: 0x17000740 RID: 1856
		// (get) Token: 0x06003283 RID: 12931 RVA: 0x000C3334 File Offset: 0x000C1534
		internal string SCONSOLEFALLBACKNAME
		{
			[SecurityCritical]
			get
			{
				if (this.sConsoleFallbackName == null)
				{
					string a = this.DoGetLocaleInfo(110U);
					if (a == "es-ES_tradnl")
					{
						a = "es-ES";
					}
					this.sConsoleFallbackName = a;
				}
				return this.sConsoleFallbackName;
			}
		}

		// Token: 0x17000741 RID: 1857
		// (get) Token: 0x06003284 RID: 12932 RVA: 0x000C3372 File Offset: 0x000C1572
		private bool ILEADINGZEROS
		{
			get
			{
				return this.DoGetLocaleInfoInt(18U) == 1;
			}
		}

		// Token: 0x17000742 RID: 1858
		// (get) Token: 0x06003285 RID: 12933 RVA: 0x000C337F File Offset: 0x000C157F
		internal int[] WAGROUPING
		{
			[SecurityCritical]
			get
			{
				if (this.waGrouping == null || this.UseUserOverride)
				{
					this.waGrouping = CultureData.ConvertWin32GroupString(this.DoGetLocaleInfo(16U));
				}
				return this.waGrouping;
			}
		}

		// Token: 0x17000743 RID: 1859
		// (get) Token: 0x06003286 RID: 12934 RVA: 0x000C33AA File Offset: 0x000C15AA
		internal string SNAN
		{
			[SecurityCritical]
			get
			{
				if (this.sNaN == null)
				{
					this.sNaN = this.DoGetLocaleInfo(105U);
				}
				return this.sNaN;
			}
		}

		// Token: 0x17000744 RID: 1860
		// (get) Token: 0x06003287 RID: 12935 RVA: 0x000C33C8 File Offset: 0x000C15C8
		internal string SPOSINFINITY
		{
			[SecurityCritical]
			get
			{
				if (this.sPositiveInfinity == null)
				{
					this.sPositiveInfinity = this.DoGetLocaleInfo(106U);
				}
				return this.sPositiveInfinity;
			}
		}

		// Token: 0x17000745 RID: 1861
		// (get) Token: 0x06003288 RID: 12936 RVA: 0x000C33E6 File Offset: 0x000C15E6
		internal string SNEGINFINITY
		{
			[SecurityCritical]
			get
			{
				if (this.sNegativeInfinity == null)
				{
					this.sNegativeInfinity = this.DoGetLocaleInfo(107U);
				}
				return this.sNegativeInfinity;
			}
		}

		// Token: 0x17000746 RID: 1862
		// (get) Token: 0x06003289 RID: 12937 RVA: 0x000C3404 File Offset: 0x000C1604
		internal int INEGATIVEPERCENT
		{
			get
			{
				if (this.iNegativePercent == -1)
				{
					this.iNegativePercent = this.DoGetLocaleInfoInt(116U);
				}
				return this.iNegativePercent;
			}
		}

		// Token: 0x17000747 RID: 1863
		// (get) Token: 0x0600328A RID: 12938 RVA: 0x000C3423 File Offset: 0x000C1623
		internal int IPOSITIVEPERCENT
		{
			get
			{
				if (this.iPositivePercent == -1)
				{
					this.iPositivePercent = this.DoGetLocaleInfoInt(117U);
				}
				return this.iPositivePercent;
			}
		}

		// Token: 0x17000748 RID: 1864
		// (get) Token: 0x0600328B RID: 12939 RVA: 0x000C3442 File Offset: 0x000C1642
		internal string SPERCENT
		{
			[SecurityCritical]
			get
			{
				if (this.sPercent == null)
				{
					this.sPercent = this.DoGetLocaleInfo(118U);
				}
				return this.sPercent;
			}
		}

		// Token: 0x17000749 RID: 1865
		// (get) Token: 0x0600328C RID: 12940 RVA: 0x000C3460 File Offset: 0x000C1660
		internal string SPERMILLE
		{
			[SecurityCritical]
			get
			{
				if (this.sPerMille == null)
				{
					this.sPerMille = this.DoGetLocaleInfo(119U);
				}
				return this.sPerMille;
			}
		}

		// Token: 0x1700074A RID: 1866
		// (get) Token: 0x0600328D RID: 12941 RVA: 0x000C347E File Offset: 0x000C167E
		internal string SCURRENCY
		{
			[SecurityCritical]
			get
			{
				if (this.sCurrency == null || this.UseUserOverride)
				{
					this.sCurrency = this.DoGetLocaleInfo(20U);
				}
				return this.sCurrency;
			}
		}

		// Token: 0x1700074B RID: 1867
		// (get) Token: 0x0600328E RID: 12942 RVA: 0x000C34A4 File Offset: 0x000C16A4
		internal string SINTLSYMBOL
		{
			[SecurityCritical]
			get
			{
				if (this.sIntlMonetarySymbol == null)
				{
					this.sIntlMonetarySymbol = this.DoGetLocaleInfo(21U);
				}
				return this.sIntlMonetarySymbol;
			}
		}

		// Token: 0x1700074C RID: 1868
		// (get) Token: 0x0600328F RID: 12943 RVA: 0x000C34C2 File Offset: 0x000C16C2
		internal string SENGLISHCURRENCY
		{
			[SecurityCritical]
			get
			{
				if (this.sEnglishCurrency == null)
				{
					this.sEnglishCurrency = this.DoGetLocaleInfo(4103U);
				}
				return this.sEnglishCurrency;
			}
		}

		// Token: 0x1700074D RID: 1869
		// (get) Token: 0x06003290 RID: 12944 RVA: 0x000C34E3 File Offset: 0x000C16E3
		internal string SNATIVECURRENCY
		{
			[SecurityCritical]
			get
			{
				if (this.sNativeCurrency == null)
				{
					this.sNativeCurrency = this.DoGetLocaleInfo(4104U);
				}
				return this.sNativeCurrency;
			}
		}

		// Token: 0x1700074E RID: 1870
		// (get) Token: 0x06003291 RID: 12945 RVA: 0x000C3504 File Offset: 0x000C1704
		internal int[] WAMONGROUPING
		{
			[SecurityCritical]
			get
			{
				if (this.waMonetaryGrouping == null || this.UseUserOverride)
				{
					this.waMonetaryGrouping = CultureData.ConvertWin32GroupString(this.DoGetLocaleInfo(24U));
				}
				return this.waMonetaryGrouping;
			}
		}

		// Token: 0x1700074F RID: 1871
		// (get) Token: 0x06003292 RID: 12946 RVA: 0x000C352F File Offset: 0x000C172F
		internal int IMEASURE
		{
			get
			{
				if (this.iMeasure == -1 || this.UseUserOverride)
				{
					this.iMeasure = this.DoGetLocaleInfoInt(13U);
				}
				return this.iMeasure;
			}
		}

		// Token: 0x17000750 RID: 1872
		// (get) Token: 0x06003293 RID: 12947 RVA: 0x000C3556 File Offset: 0x000C1756
		internal string SLIST
		{
			[SecurityCritical]
			get
			{
				if (this.sListSeparator == null || this.UseUserOverride)
				{
					this.sListSeparator = this.DoGetLocaleInfo(12U);
				}
				return this.sListSeparator;
			}
		}

		// Token: 0x17000751 RID: 1873
		// (get) Token: 0x06003294 RID: 12948 RVA: 0x000C357C File Offset: 0x000C177C
		private int IPAPERSIZE
		{
			get
			{
				return this.DoGetLocaleInfoInt(4106U);
			}
		}

		// Token: 0x17000752 RID: 1874
		// (get) Token: 0x06003295 RID: 12949 RVA: 0x000C3589 File Offset: 0x000C1789
		internal string SAM1159
		{
			[SecurityCritical]
			get
			{
				if (this.sAM1159 == null || this.UseUserOverride)
				{
					this.sAM1159 = this.DoGetLocaleInfo(40U);
				}
				return this.sAM1159;
			}
		}

		// Token: 0x17000753 RID: 1875
		// (get) Token: 0x06003296 RID: 12950 RVA: 0x000C35AF File Offset: 0x000C17AF
		internal string SPM2359
		{
			[SecurityCritical]
			get
			{
				if (this.sPM2359 == null || this.UseUserOverride)
				{
					this.sPM2359 = this.DoGetLocaleInfo(41U);
				}
				return this.sPM2359;
			}
		}

		// Token: 0x17000754 RID: 1876
		// (get) Token: 0x06003297 RID: 12951 RVA: 0x000C35D8 File Offset: 0x000C17D8
		internal string[] LongTimes
		{
			get
			{
				if (this.saLongTimes == null || this.UseUserOverride)
				{
					string[] array = this.DoEnumTimeFormats();
					if (array == null || array.Length == 0)
					{
						this.saLongTimes = CultureData.Invariant.saLongTimes;
					}
					else
					{
						this.saLongTimes = array;
					}
				}
				return this.saLongTimes;
			}
		}

		// Token: 0x17000755 RID: 1877
		// (get) Token: 0x06003298 RID: 12952 RVA: 0x000C362C File Offset: 0x000C182C
		internal string[] ShortTimes
		{
			get
			{
				if (this.saShortTimes == null || this.UseUserOverride)
				{
					string[] array = this.DoEnumShortTimeFormats();
					if (array == null || array.Length == 0)
					{
						array = this.DeriveShortTimesFromLong();
					}
					this.saShortTimes = array;
				}
				return this.saShortTimes;
			}
		}

		// Token: 0x06003299 RID: 12953 RVA: 0x000C3674 File Offset: 0x000C1874
		private string[] DeriveShortTimesFromLong()
		{
			string[] array = new string[this.LongTimes.Length];
			for (int i = 0; i < this.LongTimes.Length; i++)
			{
				array[i] = CultureData.StripSecondsFromPattern(this.LongTimes[i]);
			}
			return array;
		}

		// Token: 0x0600329A RID: 12954 RVA: 0x000C36B4 File Offset: 0x000C18B4
		private static string StripSecondsFromPattern(string time)
		{
			bool flag = false;
			int num = -1;
			for (int i = 0; i < time.Length; i++)
			{
				if (time[i] == '\'')
				{
					flag = !flag;
				}
				else if (time[i] == '\\')
				{
					i++;
				}
				else if (!flag)
				{
					char c = time[i];
					if (c <= 'h')
					{
						if (c != 'H' && c != 'h')
						{
							goto IL_D8;
						}
					}
					else if (c != 'm')
					{
						if (c == 's')
						{
							if (i - num <= 4 && i - num > 1 && time[num + 1] != '\'' && time[i - 1] != '\'' && num >= 0)
							{
								i = num + 1;
							}
							bool flag2;
							int indexOfNextTokenAfterSeconds = CultureData.GetIndexOfNextTokenAfterSeconds(time, i, out flag2);
							StringBuilder stringBuilder = new StringBuilder(time.Substring(0, i));
							if (flag2)
							{
								stringBuilder.Append(' ');
							}
							stringBuilder.Append(time.Substring(indexOfNextTokenAfterSeconds));
							time = stringBuilder.ToString();
							goto IL_D8;
						}
						goto IL_D8;
					}
					num = i;
				}
				IL_D8:;
			}
			return time;
		}

		// Token: 0x0600329B RID: 12955 RVA: 0x000C37AC File Offset: 0x000C19AC
		private static int GetIndexOfNextTokenAfterSeconds(string time, int index, out bool containsSpace)
		{
			bool flag = false;
			containsSpace = false;
			while (index < time.Length)
			{
				char c = time[index];
				if (c <= 'H')
				{
					if (c != ' ')
					{
						if (c != '\'')
						{
							if (c == 'H')
							{
								goto IL_63;
							}
						}
						else
						{
							flag = !flag;
						}
					}
					else
					{
						containsSpace = true;
					}
				}
				else if (c <= 'h')
				{
					if (c != '\\')
					{
						if (c == 'h')
						{
							goto IL_63;
						}
					}
					else
					{
						index++;
						if (time[index] == ' ')
						{
							containsSpace = true;
						}
					}
				}
				else if (c == 'm' || c == 't')
				{
					goto IL_63;
				}
				IL_68:
				index++;
				continue;
				IL_63:
				if (!flag)
				{
					return index;
				}
				goto IL_68;
			}
			containsSpace = false;
			return index;
		}

		// Token: 0x17000756 RID: 1878
		// (get) Token: 0x0600329C RID: 12956 RVA: 0x000C3834 File Offset: 0x000C1A34
		internal string[] SADURATION
		{
			[SecurityCritical]
			get
			{
				if (this.saDurationFormats == null)
				{
					string str = this.DoGetLocaleInfo(93U);
					this.saDurationFormats = new string[] { CultureData.ReescapeWin32String(str) };
				}
				return this.saDurationFormats;
			}
		}

		// Token: 0x17000757 RID: 1879
		// (get) Token: 0x0600329D RID: 12957 RVA: 0x000C3873 File Offset: 0x000C1A73
		internal int IFIRSTDAYOFWEEK
		{
			get
			{
				if (this.iFirstDayOfWeek == -1 || this.UseUserOverride)
				{
					this.iFirstDayOfWeek = CultureData.ConvertFirstDayOfWeekMonToSun(this.DoGetLocaleInfoInt(4108U));
				}
				return this.iFirstDayOfWeek;
			}
		}

		// Token: 0x17000758 RID: 1880
		// (get) Token: 0x0600329E RID: 12958 RVA: 0x000C38A2 File Offset: 0x000C1AA2
		internal int IFIRSTWEEKOFYEAR
		{
			get
			{
				if (this.iFirstWeekOfYear == -1 || this.UseUserOverride)
				{
					this.iFirstWeekOfYear = this.DoGetLocaleInfoInt(4109U);
				}
				return this.iFirstWeekOfYear;
			}
		}

		// Token: 0x0600329F RID: 12959 RVA: 0x000C38CC File Offset: 0x000C1ACC
		internal string[] ShortDates(int calendarId)
		{
			return this.GetCalendar(calendarId).saShortDates;
		}

		// Token: 0x060032A0 RID: 12960 RVA: 0x000C38DA File Offset: 0x000C1ADA
		internal string[] LongDates(int calendarId)
		{
			return this.GetCalendar(calendarId).saLongDates;
		}

		// Token: 0x060032A1 RID: 12961 RVA: 0x000C38E8 File Offset: 0x000C1AE8
		internal string[] YearMonths(int calendarId)
		{
			return this.GetCalendar(calendarId).saYearMonths;
		}

		// Token: 0x060032A2 RID: 12962 RVA: 0x000C38F6 File Offset: 0x000C1AF6
		internal string[] DayNames(int calendarId)
		{
			return this.GetCalendar(calendarId).saDayNames;
		}

		// Token: 0x060032A3 RID: 12963 RVA: 0x000C3904 File Offset: 0x000C1B04
		internal string[] AbbreviatedDayNames(int calendarId)
		{
			return this.GetCalendar(calendarId).saAbbrevDayNames;
		}

		// Token: 0x060032A4 RID: 12964 RVA: 0x000C3912 File Offset: 0x000C1B12
		internal string[] SuperShortDayNames(int calendarId)
		{
			return this.GetCalendar(calendarId).saSuperShortDayNames;
		}

		// Token: 0x060032A5 RID: 12965 RVA: 0x000C3920 File Offset: 0x000C1B20
		internal string[] MonthNames(int calendarId)
		{
			return this.GetCalendar(calendarId).saMonthNames;
		}

		// Token: 0x060032A6 RID: 12966 RVA: 0x000C392E File Offset: 0x000C1B2E
		internal string[] GenitiveMonthNames(int calendarId)
		{
			return this.GetCalendar(calendarId).saMonthGenitiveNames;
		}

		// Token: 0x060032A7 RID: 12967 RVA: 0x000C393C File Offset: 0x000C1B3C
		internal string[] AbbreviatedMonthNames(int calendarId)
		{
			return this.GetCalendar(calendarId).saAbbrevMonthNames;
		}

		// Token: 0x060032A8 RID: 12968 RVA: 0x000C394A File Offset: 0x000C1B4A
		internal string[] AbbreviatedGenitiveMonthNames(int calendarId)
		{
			return this.GetCalendar(calendarId).saAbbrevMonthGenitiveNames;
		}

		// Token: 0x060032A9 RID: 12969 RVA: 0x000C3958 File Offset: 0x000C1B58
		internal string[] LeapYearMonthNames(int calendarId)
		{
			return this.GetCalendar(calendarId).saLeapYearMonthNames;
		}

		// Token: 0x060032AA RID: 12970 RVA: 0x000C3966 File Offset: 0x000C1B66
		internal string MonthDay(int calendarId)
		{
			return this.GetCalendar(calendarId).sMonthDay;
		}

		// Token: 0x17000759 RID: 1881
		// (get) Token: 0x060032AB RID: 12971 RVA: 0x000C3974 File Offset: 0x000C1B74
		internal int[] CalendarIds
		{
			get
			{
				if (this.waCalendars == null)
				{
					int[] array = new int[23];
					int num = CalendarData.nativeGetCalendars(this.sWindowsName, this.bUseOverrides, array);
					if (num == 0)
					{
						this.waCalendars = CultureData.Invariant.waCalendars;
					}
					else
					{
						if (this.sWindowsName == "zh-TW")
						{
							bool flag = false;
							for (int i = 0; i < num; i++)
							{
								if (array[i] == 4)
								{
									flag = true;
									break;
								}
							}
							if (!flag)
							{
								num++;
								Array.Copy(array, 1, array, 2, 21);
								array[1] = 4;
							}
						}
						int[] destinationArray = new int[num];
						Array.Copy(array, destinationArray, num);
						this.waCalendars = destinationArray;
					}
				}
				return this.waCalendars;
			}
		}

		// Token: 0x060032AC RID: 12972 RVA: 0x000C3A27 File Offset: 0x000C1C27
		internal string CalendarName(int calendarId)
		{
			return this.GetCalendar(calendarId).sNativeName;
		}

		// Token: 0x060032AD RID: 12973 RVA: 0x000C3A38 File Offset: 0x000C1C38
		internal CalendarData GetCalendar(int calendarId)
		{
			int num = calendarId - 1;
			if (this.calendars == null)
			{
				this.calendars = new CalendarData[23];
			}
			CalendarData calendarData = this.calendars[num];
			if (calendarData == null || this.UseUserOverride)
			{
				calendarData = new CalendarData(this.sWindowsName, calendarId, this.UseUserOverride);
				if (CultureData.IsOsWin7OrPrior() && !this.IsSupplementalCustomCulture && !this.IsReplacementCulture)
				{
					calendarData.FixupWin7MonthDaySemicolonBug();
				}
				this.calendars[num] = calendarData;
			}
			return calendarData;
		}

		// Token: 0x060032AE RID: 12974 RVA: 0x000C3AAC File Offset: 0x000C1CAC
		internal int CurrentEra(int calendarId)
		{
			return this.GetCalendar(calendarId).iCurrentEra;
		}

		// Token: 0x1700075A RID: 1882
		// (get) Token: 0x060032AF RID: 12975 RVA: 0x000C3ABA File Offset: 0x000C1CBA
		internal bool IsRightToLeft
		{
			get
			{
				return this.IREADINGLAYOUT == 1;
			}
		}

		// Token: 0x1700075B RID: 1883
		// (get) Token: 0x060032B0 RID: 12976 RVA: 0x000C3AC5 File Offset: 0x000C1CC5
		private int IREADINGLAYOUT
		{
			get
			{
				if (this.iReadingLayout == -1)
				{
					this.iReadingLayout = this.DoGetLocaleInfoInt(112U);
				}
				return this.iReadingLayout;
			}
		}

		// Token: 0x1700075C RID: 1884
		// (get) Token: 0x060032B1 RID: 12977 RVA: 0x000C3AE4 File Offset: 0x000C1CE4
		internal string STEXTINFO
		{
			[SecuritySafeCritical]
			get
			{
				if (this.sTextInfo == null)
				{
					if (this.IsNeutralCulture || this.IsSupplementalCustomCulture)
					{
						string cultureName = this.DoGetLocaleInfo(123U);
						this.sTextInfo = CultureData.GetCultureData(cultureName, this.bUseOverrides).SNAME;
					}
					if (this.sTextInfo == null)
					{
						this.sTextInfo = this.SNAME;
					}
				}
				return this.sTextInfo;
			}
		}

		// Token: 0x1700075D RID: 1885
		// (get) Token: 0x060032B2 RID: 12978 RVA: 0x000C3B43 File Offset: 0x000C1D43
		internal string SCOMPAREINFO
		{
			[SecuritySafeCritical]
			get
			{
				if (this.sCompareInfo == null)
				{
					if (this.IsSupplementalCustomCulture)
					{
						this.sCompareInfo = this.DoGetLocaleInfo(123U);
					}
					if (this.sCompareInfo == null)
					{
						this.sCompareInfo = this.sWindowsName;
					}
				}
				return this.sCompareInfo;
			}
		}

		// Token: 0x1700075E RID: 1886
		// (get) Token: 0x060032B3 RID: 12979 RVA: 0x000C3B7D File Offset: 0x000C1D7D
		internal bool IsSupplementalCustomCulture
		{
			get
			{
				return CultureData.IsCustomCultureId(this.ILANGUAGE);
			}
		}

		// Token: 0x1700075F RID: 1887
		// (get) Token: 0x060032B4 RID: 12980 RVA: 0x000C3B8A File Offset: 0x000C1D8A
		private string SSCRIPTS
		{
			[SecuritySafeCritical]
			get
			{
				if (this.sScripts == null)
				{
					this.sScripts = this.DoGetLocaleInfo(108U);
				}
				return this.sScripts;
			}
		}

		// Token: 0x17000760 RID: 1888
		// (get) Token: 0x060032B5 RID: 12981 RVA: 0x000C3BA8 File Offset: 0x000C1DA8
		private string SOPENTYPELANGUAGETAG
		{
			[SecuritySafeCritical]
			get
			{
				return this.DoGetLocaleInfo(122U);
			}
		}

		// Token: 0x17000761 RID: 1889
		// (get) Token: 0x060032B6 RID: 12982 RVA: 0x000C3BB2 File Offset: 0x000C1DB2
		private string FONTSIGNATURE
		{
			[SecuritySafeCritical]
			get
			{
				if (this.fontSignature == null)
				{
					this.fontSignature = this.DoGetLocaleInfo(88U);
				}
				return this.fontSignature;
			}
		}

		// Token: 0x17000762 RID: 1890
		// (get) Token: 0x060032B7 RID: 12983 RVA: 0x000C3BD0 File Offset: 0x000C1DD0
		private string SKEYBOARDSTOINSTALL
		{
			[SecuritySafeCritical]
			get
			{
				return this.DoGetLocaleInfo(94U);
			}
		}

		// Token: 0x17000763 RID: 1891
		// (get) Token: 0x060032B8 RID: 12984 RVA: 0x000C3BDA File Offset: 0x000C1DDA
		internal int IDEFAULTANSICODEPAGE
		{
			get
			{
				if (this.iDefaultAnsiCodePage == -1)
				{
					this.iDefaultAnsiCodePage = this.DoGetLocaleInfoInt(4100U);
				}
				return this.iDefaultAnsiCodePage;
			}
		}

		// Token: 0x17000764 RID: 1892
		// (get) Token: 0x060032B9 RID: 12985 RVA: 0x000C3BFC File Offset: 0x000C1DFC
		internal int IDEFAULTOEMCODEPAGE
		{
			get
			{
				if (this.iDefaultOemCodePage == -1)
				{
					this.iDefaultOemCodePage = this.DoGetLocaleInfoInt(11U);
				}
				return this.iDefaultOemCodePage;
			}
		}

		// Token: 0x17000765 RID: 1893
		// (get) Token: 0x060032BA RID: 12986 RVA: 0x000C3C1B File Offset: 0x000C1E1B
		internal int IDEFAULTMACCODEPAGE
		{
			get
			{
				if (this.iDefaultMacCodePage == -1)
				{
					this.iDefaultMacCodePage = this.DoGetLocaleInfoInt(4113U);
				}
				return this.iDefaultMacCodePage;
			}
		}

		// Token: 0x17000766 RID: 1894
		// (get) Token: 0x060032BB RID: 12987 RVA: 0x000C3C3D File Offset: 0x000C1E3D
		internal int IDEFAULTEBCDICCODEPAGE
		{
			get
			{
				if (this.iDefaultEbcdicCodePage == -1)
				{
					this.iDefaultEbcdicCodePage = this.DoGetLocaleInfoInt(4114U);
				}
				return this.iDefaultEbcdicCodePage;
			}
		}

		// Token: 0x060032BC RID: 12988
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern int LocaleNameToLCID(string localeName);

		// Token: 0x17000767 RID: 1895
		// (get) Token: 0x060032BD RID: 12989 RVA: 0x000C3C5F File Offset: 0x000C1E5F
		internal int ILANGUAGE
		{
			get
			{
				if (this.iLanguage == 0)
				{
					this.iLanguage = CultureData.LocaleNameToLCID(this.sRealName);
				}
				return this.iLanguage;
			}
		}

		// Token: 0x17000768 RID: 1896
		// (get) Token: 0x060032BE RID: 12990 RVA: 0x000C3C80 File Offset: 0x000C1E80
		internal bool IsWin32Installed
		{
			get
			{
				return this.bWin32Installed;
			}
		}

		// Token: 0x17000769 RID: 1897
		// (get) Token: 0x060032BF RID: 12991 RVA: 0x000C3C88 File Offset: 0x000C1E88
		internal bool IsFramework
		{
			get
			{
				return this.bFramework;
			}
		}

		// Token: 0x1700076A RID: 1898
		// (get) Token: 0x060032C0 RID: 12992 RVA: 0x000C3C90 File Offset: 0x000C1E90
		internal bool IsNeutralCulture
		{
			get
			{
				return this.bNeutral;
			}
		}

		// Token: 0x1700076B RID: 1899
		// (get) Token: 0x060032C1 RID: 12993 RVA: 0x000C3C98 File Offset: 0x000C1E98
		internal bool IsInvariantCulture
		{
			get
			{
				return string.IsNullOrEmpty(this.SNAME);
			}
		}

		// Token: 0x1700076C RID: 1900
		// (get) Token: 0x060032C2 RID: 12994 RVA: 0x000C3CA8 File Offset: 0x000C1EA8
		internal Calendar DefaultCalendar
		{
			get
			{
				int num = this.DoGetLocaleInfoInt(4105U);
				if (num == 0)
				{
					num = this.CalendarIds[0];
				}
				return CultureInfo.GetCalendarInstance(num);
			}
		}

		// Token: 0x060032C3 RID: 12995 RVA: 0x000C3CD3 File Offset: 0x000C1ED3
		internal string[] EraNames(int calendarId)
		{
			return this.GetCalendar(calendarId).saEraNames;
		}

		// Token: 0x060032C4 RID: 12996 RVA: 0x000C3CE1 File Offset: 0x000C1EE1
		internal string[] AbbrevEraNames(int calendarId)
		{
			return this.GetCalendar(calendarId).saAbbrevEraNames;
		}

		// Token: 0x060032C5 RID: 12997 RVA: 0x000C3CEF File Offset: 0x000C1EEF
		internal string[] AbbreviatedEnglishEraNames(int calendarId)
		{
			return this.GetCalendar(calendarId).saAbbrevEnglishEraNames;
		}

		// Token: 0x1700076D RID: 1901
		// (get) Token: 0x060032C6 RID: 12998 RVA: 0x000C3D00 File Offset: 0x000C1F00
		internal string TimeSeparator
		{
			[SecuritySafeCritical]
			get
			{
				if (this.sTimeSeparator == null || this.UseUserOverride)
				{
					string text = CultureData.ReescapeWin32String(this.DoGetLocaleInfo(4099U));
					if (string.IsNullOrEmpty(text))
					{
						text = this.LongTimes[0];
					}
					this.sTimeSeparator = CultureData.GetTimeSeparator(text);
				}
				return this.sTimeSeparator;
			}
		}

		// Token: 0x060032C7 RID: 12999 RVA: 0x000C3D51 File Offset: 0x000C1F51
		internal string DateSeparator(int calendarId)
		{
			if (calendarId == 3 && !AppContextSwitches.EnforceLegacyJapaneseDateParsing)
			{
				return "/";
			}
			return CultureData.GetDateSeparator(this.ShortDates(calendarId)[0]);
		}

		// Token: 0x060032C8 RID: 13000 RVA: 0x000C3D74 File Offset: 0x000C1F74
		private static string UnescapeNlsString(string str, int start, int end)
		{
			StringBuilder stringBuilder = null;
			int num = start;
			while (num < str.Length && num <= end)
			{
				char c = str[num];
				if (c != '\'')
				{
					if (c != '\\')
					{
						if (stringBuilder != null)
						{
							stringBuilder.Append(str[num]);
						}
					}
					else
					{
						if (stringBuilder == null)
						{
							stringBuilder = new StringBuilder(str, start, num - start, str.Length);
						}
						num++;
						if (num < str.Length)
						{
							stringBuilder.Append(str[num]);
						}
					}
				}
				else if (stringBuilder == null)
				{
					stringBuilder = new StringBuilder(str, start, num - start, str.Length);
				}
				num++;
			}
			if (stringBuilder == null)
			{
				return str.Substring(start, end - start + 1);
			}
			return stringBuilder.ToString();
		}

		// Token: 0x060032C9 RID: 13001 RVA: 0x000C3E1C File Offset: 0x000C201C
		internal static string ReescapeWin32String(string str)
		{
			if (str == null)
			{
				return null;
			}
			StringBuilder stringBuilder = null;
			bool flag = false;
			int i = 0;
			while (i < str.Length)
			{
				if (str[i] == '\'')
				{
					if (!flag)
					{
						flag = true;
						goto IL_91;
					}
					if (i + 1 >= str.Length || str[i + 1] != '\'')
					{
						flag = false;
						goto IL_91;
					}
					if (stringBuilder == null)
					{
						stringBuilder = new StringBuilder(str, 0, i, str.Length * 2);
					}
					stringBuilder.Append("\\'");
					i++;
				}
				else
				{
					if (str[i] != '\\')
					{
						goto IL_91;
					}
					if (stringBuilder == null)
					{
						stringBuilder = new StringBuilder(str, 0, i, str.Length * 2);
					}
					stringBuilder.Append("\\\\");
				}
				IL_A2:
				i++;
				continue;
				IL_91:
				if (stringBuilder != null)
				{
					stringBuilder.Append(str[i]);
					goto IL_A2;
				}
				goto IL_A2;
			}
			if (stringBuilder == null)
			{
				return str;
			}
			return stringBuilder.ToString();
		}

		// Token: 0x060032CA RID: 13002 RVA: 0x000C3EE8 File Offset: 0x000C20E8
		internal static string[] ReescapeWin32Strings(string[] array)
		{
			if (array != null)
			{
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = CultureData.ReescapeWin32String(array[i]);
				}
			}
			return array;
		}

		// Token: 0x060032CB RID: 13003 RVA: 0x000C3F12 File Offset: 0x000C2112
		private static string GetTimeSeparator(string format)
		{
			return CultureData.GetSeparator(format, "Hhms");
		}

		// Token: 0x060032CC RID: 13004 RVA: 0x000C3F1F File Offset: 0x000C211F
		private static string GetDateSeparator(string format)
		{
			return CultureData.GetSeparator(format, "dyM");
		}

		// Token: 0x060032CD RID: 13005 RVA: 0x000C3F2C File Offset: 0x000C212C
		private static string GetSeparator(string format, string timeParts)
		{
			int num = CultureData.IndexOfTimePart(format, 0, timeParts);
			if (num != -1)
			{
				char c = format[num];
				do
				{
					num++;
				}
				while (num < format.Length && format[num] == c);
				int num2 = num;
				if (num2 < format.Length)
				{
					int num3 = CultureData.IndexOfTimePart(format, num2, timeParts);
					if (num3 != -1)
					{
						return CultureData.UnescapeNlsString(format, num2, num3 - 1);
					}
				}
			}
			return string.Empty;
		}

		// Token: 0x060032CE RID: 13006 RVA: 0x000C3F90 File Offset: 0x000C2190
		private static int IndexOfTimePart(string format, int startIndex, string timeParts)
		{
			bool flag = false;
			for (int i = startIndex; i < format.Length; i++)
			{
				if (!flag && timeParts.IndexOf(format[i]) != -1)
				{
					return i;
				}
				char c = format[i];
				if (c != '\'')
				{
					if (c == '\\' && i + 1 < format.Length)
					{
						i++;
						char c2 = format[i];
						if (c2 != '\'' && c2 != '\\')
						{
							i--;
						}
					}
				}
				else
				{
					flag = !flag;
				}
			}
			return -1;
		}

		// Token: 0x060032CF RID: 13007 RVA: 0x000C4004 File Offset: 0x000C2204
		[SecurityCritical]
		private string DoGetLocaleInfo(uint lctype)
		{
			return this.DoGetLocaleInfo(this.sWindowsName, lctype);
		}

		// Token: 0x060032D0 RID: 13008 RVA: 0x000C4014 File Offset: 0x000C2214
		[SecurityCritical]
		private string DoGetLocaleInfo(string localeName, uint lctype)
		{
			if (!this.UseUserOverride)
			{
				lctype |= 2147483648U;
			}
			string text = CultureInfo.nativeGetLocaleInfoEx(localeName, lctype);
			if (text == null)
			{
				text = string.Empty;
			}
			return text;
		}

		// Token: 0x060032D1 RID: 13009 RVA: 0x000C4044 File Offset: 0x000C2244
		private int DoGetLocaleInfoInt(uint lctype)
		{
			if (!this.UseUserOverride)
			{
				lctype |= 2147483648U;
			}
			return CultureInfo.nativeGetLocaleInfoExInt(this.sWindowsName, lctype);
		}

		// Token: 0x060032D2 RID: 13010 RVA: 0x000C4070 File Offset: 0x000C2270
		private string[] DoEnumTimeFormats()
		{
			return CultureData.ReescapeWin32Strings(CultureData.nativeEnumTimeFormats(this.sWindowsName, 0U, this.UseUserOverride));
		}

		// Token: 0x060032D3 RID: 13011 RVA: 0x000C4098 File Offset: 0x000C2298
		private string[] DoEnumShortTimeFormats()
		{
			return CultureData.ReescapeWin32Strings(CultureData.nativeEnumTimeFormats(this.sWindowsName, 2U, this.UseUserOverride));
		}

		// Token: 0x060032D4 RID: 13012 RVA: 0x000C40BE File Offset: 0x000C22BE
		internal static bool IsCustomCultureId(int cultureId)
		{
			return cultureId == 3072 || cultureId == 4096;
		}

		// Token: 0x060032D5 RID: 13013 RVA: 0x000C40D4 File Offset: 0x000C22D4
		[SecurityCritical]
		internal void GetNFIValues(NumberFormatInfo nfi)
		{
			if (this.IsInvariantCulture)
			{
				nfi.positiveSign = this.sPositiveSign;
				nfi.negativeSign = this.sNegativeSign;
				nfi.nativeDigits = this.saNativeDigits;
				nfi.digitSubstitution = this.iDigitSubstitution;
				nfi.numberGroupSeparator = this.sThousandSeparator;
				nfi.numberDecimalSeparator = this.sDecimalSeparator;
				nfi.numberDecimalDigits = this.iDigits;
				nfi.numberNegativePattern = this.iNegativeNumber;
				nfi.currencySymbol = this.sCurrency;
				nfi.currencyGroupSeparator = this.sMonetaryThousand;
				nfi.currencyDecimalSeparator = this.sMonetaryDecimal;
				nfi.currencyDecimalDigits = this.iCurrencyDigits;
				nfi.currencyNegativePattern = this.iNegativeCurrency;
				nfi.currencyPositivePattern = this.iCurrency;
			}
			else
			{
				CultureData.nativeGetNumberFormatInfoValues(this.sWindowsName, nfi, this.UseUserOverride);
			}
			nfi.numberGroupSizes = this.WAGROUPING;
			nfi.currencyGroupSizes = this.WAMONGROUPING;
			nfi.percentNegativePattern = this.INEGATIVEPERCENT;
			nfi.percentPositivePattern = this.IPOSITIVEPERCENT;
			nfi.percentSymbol = this.SPERCENT;
			nfi.perMilleSymbol = this.SPERMILLE;
			nfi.negativeInfinitySymbol = this.SNEGINFINITY;
			nfi.positiveInfinitySymbol = this.SPOSINFINITY;
			nfi.nanSymbol = this.SNAN;
			nfi.percentDecimalDigits = nfi.numberDecimalDigits;
			nfi.percentDecimalSeparator = nfi.numberDecimalSeparator;
			nfi.percentGroupSizes = nfi.numberGroupSizes;
			nfi.percentGroupSeparator = nfi.numberGroupSeparator;
			if (nfi.positiveSign == null || nfi.positiveSign.Length == 0)
			{
				nfi.positiveSign = "+";
			}
			if (nfi.currencyDecimalSeparator == null || nfi.currencyDecimalSeparator.Length == 0)
			{
				nfi.currencyDecimalSeparator = nfi.numberDecimalSeparator;
			}
			if (932 == this.IDEFAULTANSICODEPAGE || 949 == this.IDEFAULTANSICODEPAGE)
			{
				nfi.ansiCurrencySymbol = "\\";
			}
		}

		// Token: 0x060032D6 RID: 13014 RVA: 0x000C42AB File Offset: 0x000C24AB
		private static int ConvertFirstDayOfWeekMonToSun(int iTemp)
		{
			iTemp++;
			if (iTemp > 6)
			{
				iTemp = 0;
			}
			return iTemp;
		}

		// Token: 0x060032D7 RID: 13015 RVA: 0x000C42BC File Offset: 0x000C24BC
		internal static string AnsiToLower(string testString)
		{
			StringBuilder stringBuilder = new StringBuilder(testString.Length);
			foreach (char c in testString)
			{
				stringBuilder.Append((c <= 'Z' && c >= 'A') ? (c - 'A' + 'a') : c);
			}
			return stringBuilder.ToString();
		}

		// Token: 0x060032D8 RID: 13016 RVA: 0x000C4310 File Offset: 0x000C2510
		private static int[] ConvertWin32GroupString(string win32Str)
		{
			if (win32Str == null || win32Str.Length == 0)
			{
				return new int[] { 3 };
			}
			if (win32Str[0] == '0')
			{
				return new int[1];
			}
			int[] array;
			if (win32Str[win32Str.Length - 1] == '0')
			{
				array = new int[win32Str.Length / 2];
			}
			else
			{
				array = new int[win32Str.Length / 2 + 2];
				array[array.Length - 1] = 0;
			}
			int num = 0;
			int num2 = 0;
			while (num < win32Str.Length && num2 < array.Length)
			{
				if (win32Str[num] < '1' || win32Str[num] > '9')
				{
					return new int[] { 3 };
				}
				array[num2] = (int)(win32Str[num] - '0');
				num += 2;
				num2++;
			}
			return array;
		}

		// Token: 0x060032D9 RID: 13017
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool nativeInitCultureData(CultureData cultureData);

		// Token: 0x060032DA RID: 13018
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool nativeGetNumberFormatInfoValues(string localeName, NumberFormatInfo nfi, bool useUserOverride);

		// Token: 0x060032DB RID: 13019
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern string[] nativeEnumTimeFormats(string localeName, uint dwFlags, bool useUserOverride);

		// Token: 0x060032DC RID: 13020
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern int nativeEnumCultureNames(int cultureTypes, ObjectHandleOnStack retStringArray);

		// Token: 0x040015A3 RID: 5539
		private const int undef = -1;

		// Token: 0x040015A4 RID: 5540
		private string sRealName;

		// Token: 0x040015A5 RID: 5541
		private string sWindowsName;

		// Token: 0x040015A6 RID: 5542
		private string sName;

		// Token: 0x040015A7 RID: 5543
		private string sParent;

		// Token: 0x040015A8 RID: 5544
		private string sLocalizedDisplayName;

		// Token: 0x040015A9 RID: 5545
		private string sEnglishDisplayName;

		// Token: 0x040015AA RID: 5546
		private string sNativeDisplayName;

		// Token: 0x040015AB RID: 5547
		private string sSpecificCulture;

		// Token: 0x040015AC RID: 5548
		private string sISO639Language;

		// Token: 0x040015AD RID: 5549
		private string sLocalizedLanguage;

		// Token: 0x040015AE RID: 5550
		private string sEnglishLanguage;

		// Token: 0x040015AF RID: 5551
		private string sNativeLanguage;

		// Token: 0x040015B0 RID: 5552
		private string sRegionName;

		// Token: 0x040015B1 RID: 5553
		private int iGeoId = -1;

		// Token: 0x040015B2 RID: 5554
		private string sLocalizedCountry;

		// Token: 0x040015B3 RID: 5555
		private string sEnglishCountry;

		// Token: 0x040015B4 RID: 5556
		private string sNativeCountry;

		// Token: 0x040015B5 RID: 5557
		private string sISO3166CountryName;

		// Token: 0x040015B6 RID: 5558
		private string sPositiveSign;

		// Token: 0x040015B7 RID: 5559
		private string sNegativeSign;

		// Token: 0x040015B8 RID: 5560
		private string[] saNativeDigits;

		// Token: 0x040015B9 RID: 5561
		private int iDigitSubstitution;

		// Token: 0x040015BA RID: 5562
		private int iLeadingZeros;

		// Token: 0x040015BB RID: 5563
		private int iDigits;

		// Token: 0x040015BC RID: 5564
		private int iNegativeNumber;

		// Token: 0x040015BD RID: 5565
		private int[] waGrouping;

		// Token: 0x040015BE RID: 5566
		private string sDecimalSeparator;

		// Token: 0x040015BF RID: 5567
		private string sThousandSeparator;

		// Token: 0x040015C0 RID: 5568
		private string sNaN;

		// Token: 0x040015C1 RID: 5569
		private string sPositiveInfinity;

		// Token: 0x040015C2 RID: 5570
		private string sNegativeInfinity;

		// Token: 0x040015C3 RID: 5571
		private int iNegativePercent = -1;

		// Token: 0x040015C4 RID: 5572
		private int iPositivePercent = -1;

		// Token: 0x040015C5 RID: 5573
		private string sPercent;

		// Token: 0x040015C6 RID: 5574
		private string sPerMille;

		// Token: 0x040015C7 RID: 5575
		private string sCurrency;

		// Token: 0x040015C8 RID: 5576
		private string sIntlMonetarySymbol;

		// Token: 0x040015C9 RID: 5577
		private string sEnglishCurrency;

		// Token: 0x040015CA RID: 5578
		private string sNativeCurrency;

		// Token: 0x040015CB RID: 5579
		private int iCurrencyDigits;

		// Token: 0x040015CC RID: 5580
		private int iCurrency;

		// Token: 0x040015CD RID: 5581
		private int iNegativeCurrency;

		// Token: 0x040015CE RID: 5582
		private int[] waMonetaryGrouping;

		// Token: 0x040015CF RID: 5583
		private string sMonetaryDecimal;

		// Token: 0x040015D0 RID: 5584
		private string sMonetaryThousand;

		// Token: 0x040015D1 RID: 5585
		private int iMeasure = -1;

		// Token: 0x040015D2 RID: 5586
		private string sListSeparator;

		// Token: 0x040015D3 RID: 5587
		private string sAM1159;

		// Token: 0x040015D4 RID: 5588
		private string sPM2359;

		// Token: 0x040015D5 RID: 5589
		private string sTimeSeparator;

		// Token: 0x040015D6 RID: 5590
		private volatile string[] saLongTimes;

		// Token: 0x040015D7 RID: 5591
		private volatile string[] saShortTimes;

		// Token: 0x040015D8 RID: 5592
		private volatile string[] saDurationFormats;

		// Token: 0x040015D9 RID: 5593
		private int iFirstDayOfWeek = -1;

		// Token: 0x040015DA RID: 5594
		private int iFirstWeekOfYear = -1;

		// Token: 0x040015DB RID: 5595
		private volatile int[] waCalendars;

		// Token: 0x040015DC RID: 5596
		private CalendarData[] calendars;

		// Token: 0x040015DD RID: 5597
		private int iReadingLayout = -1;

		// Token: 0x040015DE RID: 5598
		private string sTextInfo;

		// Token: 0x040015DF RID: 5599
		private string sCompareInfo;

		// Token: 0x040015E0 RID: 5600
		private string sScripts;

		// Token: 0x040015E1 RID: 5601
		private int iDefaultAnsiCodePage = -1;

		// Token: 0x040015E2 RID: 5602
		private int iDefaultOemCodePage = -1;

		// Token: 0x040015E3 RID: 5603
		private int iDefaultMacCodePage = -1;

		// Token: 0x040015E4 RID: 5604
		private int iDefaultEbcdicCodePage = -1;

		// Token: 0x040015E5 RID: 5605
		private int iLanguage;

		// Token: 0x040015E6 RID: 5606
		private string sAbbrevLang;

		// Token: 0x040015E7 RID: 5607
		private string sAbbrevCountry;

		// Token: 0x040015E8 RID: 5608
		private string sISO639Language2;

		// Token: 0x040015E9 RID: 5609
		private string sISO3166CountryName2;

		// Token: 0x040015EA RID: 5610
		private int iInputLanguageHandle = -1;

		// Token: 0x040015EB RID: 5611
		private string sConsoleFallbackName;

		// Token: 0x040015EC RID: 5612
		private string sKeyboardsToInstall;

		// Token: 0x040015ED RID: 5613
		private string fontSignature;

		// Token: 0x040015EE RID: 5614
		private bool bUseOverrides;

		// Token: 0x040015EF RID: 5615
		private bool bNeutral;

		// Token: 0x040015F0 RID: 5616
		private bool bWin32Installed;

		// Token: 0x040015F1 RID: 5617
		private bool bFramework;

		// Token: 0x040015F2 RID: 5618
		private static volatile Dictionary<string, string> s_RegionNames;

		// Token: 0x040015F3 RID: 5619
		private static volatile CultureData s_Invariant;

		// Token: 0x040015F4 RID: 5620
		internal static volatile ResourceSet MscorlibResourceSet;

		// Token: 0x040015F5 RID: 5621
		private static volatile Dictionary<string, CultureData> s_cachedCultures;

		// Token: 0x040015F6 RID: 5622
		private static readonly Version s_win7Version = new Version(6, 1);

		// Token: 0x040015F7 RID: 5623
		private static string s_RegionKey = "System\\CurrentControlSet\\Control\\Nls\\RegionMapping";

		// Token: 0x040015F8 RID: 5624
		private static volatile Dictionary<string, CultureData> s_cachedRegions;

		// Token: 0x040015F9 RID: 5625
		internal static volatile CultureInfo[] specificCultures;

		// Token: 0x040015FA RID: 5626
		internal static volatile string[] s_replacementCultureNames;

		// Token: 0x040015FB RID: 5627
		private const uint LOCALE_NOUSEROVERRIDE = 2147483648U;

		// Token: 0x040015FC RID: 5628
		private const uint LOCALE_RETURN_NUMBER = 536870912U;

		// Token: 0x040015FD RID: 5629
		private const uint LOCALE_RETURN_GENITIVE_NAMES = 268435456U;

		// Token: 0x040015FE RID: 5630
		private const uint LOCALE_SLOCALIZEDDISPLAYNAME = 2U;

		// Token: 0x040015FF RID: 5631
		private const uint LOCALE_SENGLISHDISPLAYNAME = 114U;

		// Token: 0x04001600 RID: 5632
		private const uint LOCALE_SNATIVEDISPLAYNAME = 115U;

		// Token: 0x04001601 RID: 5633
		private const uint LOCALE_SLOCALIZEDLANGUAGENAME = 111U;

		// Token: 0x04001602 RID: 5634
		private const uint LOCALE_SENGLISHLANGUAGENAME = 4097U;

		// Token: 0x04001603 RID: 5635
		private const uint LOCALE_SNATIVELANGUAGENAME = 4U;

		// Token: 0x04001604 RID: 5636
		private const uint LOCALE_SLOCALIZEDCOUNTRYNAME = 6U;

		// Token: 0x04001605 RID: 5637
		private const uint LOCALE_SENGLISHCOUNTRYNAME = 4098U;

		// Token: 0x04001606 RID: 5638
		private const uint LOCALE_SNATIVECOUNTRYNAME = 8U;

		// Token: 0x04001607 RID: 5639
		private const uint LOCALE_SABBREVLANGNAME = 3U;

		// Token: 0x04001608 RID: 5640
		private const uint LOCALE_ICOUNTRY = 5U;

		// Token: 0x04001609 RID: 5641
		private const uint LOCALE_SABBREVCTRYNAME = 7U;

		// Token: 0x0400160A RID: 5642
		private const uint LOCALE_IGEOID = 91U;

		// Token: 0x0400160B RID: 5643
		private const uint LOCALE_IDEFAULTLANGUAGE = 9U;

		// Token: 0x0400160C RID: 5644
		private const uint LOCALE_IDEFAULTCOUNTRY = 10U;

		// Token: 0x0400160D RID: 5645
		private const uint LOCALE_IDEFAULTCODEPAGE = 11U;

		// Token: 0x0400160E RID: 5646
		private const uint LOCALE_IDEFAULTANSICODEPAGE = 4100U;

		// Token: 0x0400160F RID: 5647
		private const uint LOCALE_IDEFAULTMACCODEPAGE = 4113U;

		// Token: 0x04001610 RID: 5648
		private const uint LOCALE_SLIST = 12U;

		// Token: 0x04001611 RID: 5649
		private const uint LOCALE_IMEASURE = 13U;

		// Token: 0x04001612 RID: 5650
		private const uint LOCALE_SDECIMAL = 14U;

		// Token: 0x04001613 RID: 5651
		private const uint LOCALE_STHOUSAND = 15U;

		// Token: 0x04001614 RID: 5652
		private const uint LOCALE_SGROUPING = 16U;

		// Token: 0x04001615 RID: 5653
		private const uint LOCALE_IDIGITS = 17U;

		// Token: 0x04001616 RID: 5654
		private const uint LOCALE_ILZERO = 18U;

		// Token: 0x04001617 RID: 5655
		private const uint LOCALE_INEGNUMBER = 4112U;

		// Token: 0x04001618 RID: 5656
		private const uint LOCALE_SNATIVEDIGITS = 19U;

		// Token: 0x04001619 RID: 5657
		private const uint LOCALE_SCURRENCY = 20U;

		// Token: 0x0400161A RID: 5658
		private const uint LOCALE_SINTLSYMBOL = 21U;

		// Token: 0x0400161B RID: 5659
		private const uint LOCALE_SMONDECIMALSEP = 22U;

		// Token: 0x0400161C RID: 5660
		private const uint LOCALE_SMONTHOUSANDSEP = 23U;

		// Token: 0x0400161D RID: 5661
		private const uint LOCALE_SMONGROUPING = 24U;

		// Token: 0x0400161E RID: 5662
		private const uint LOCALE_ICURRDIGITS = 25U;

		// Token: 0x0400161F RID: 5663
		private const uint LOCALE_IINTLCURRDIGITS = 26U;

		// Token: 0x04001620 RID: 5664
		private const uint LOCALE_ICURRENCY = 27U;

		// Token: 0x04001621 RID: 5665
		private const uint LOCALE_INEGCURR = 28U;

		// Token: 0x04001622 RID: 5666
		private const uint LOCALE_SDATE = 29U;

		// Token: 0x04001623 RID: 5667
		private const uint LOCALE_STIME = 30U;

		// Token: 0x04001624 RID: 5668
		private const uint LOCALE_SSHORTDATE = 31U;

		// Token: 0x04001625 RID: 5669
		private const uint LOCALE_SLONGDATE = 32U;

		// Token: 0x04001626 RID: 5670
		private const uint LOCALE_STIMEFORMAT = 4099U;

		// Token: 0x04001627 RID: 5671
		private const uint LOCALE_IDATE = 33U;

		// Token: 0x04001628 RID: 5672
		private const uint LOCALE_ILDATE = 34U;

		// Token: 0x04001629 RID: 5673
		private const uint LOCALE_ITIME = 35U;

		// Token: 0x0400162A RID: 5674
		private const uint LOCALE_ITIMEMARKPOSN = 4101U;

		// Token: 0x0400162B RID: 5675
		private const uint LOCALE_ICENTURY = 36U;

		// Token: 0x0400162C RID: 5676
		private const uint LOCALE_ITLZERO = 37U;

		// Token: 0x0400162D RID: 5677
		private const uint LOCALE_IDAYLZERO = 38U;

		// Token: 0x0400162E RID: 5678
		private const uint LOCALE_IMONLZERO = 39U;

		// Token: 0x0400162F RID: 5679
		private const uint LOCALE_S1159 = 40U;

		// Token: 0x04001630 RID: 5680
		private const uint LOCALE_S2359 = 41U;

		// Token: 0x04001631 RID: 5681
		private const uint LOCALE_ICALENDARTYPE = 4105U;

		// Token: 0x04001632 RID: 5682
		private const uint LOCALE_IOPTIONALCALENDAR = 4107U;

		// Token: 0x04001633 RID: 5683
		private const uint LOCALE_IFIRSTDAYOFWEEK = 4108U;

		// Token: 0x04001634 RID: 5684
		private const uint LOCALE_IFIRSTWEEKOFYEAR = 4109U;

		// Token: 0x04001635 RID: 5685
		private const uint LOCALE_SDAYNAME1 = 42U;

		// Token: 0x04001636 RID: 5686
		private const uint LOCALE_SDAYNAME2 = 43U;

		// Token: 0x04001637 RID: 5687
		private const uint LOCALE_SDAYNAME3 = 44U;

		// Token: 0x04001638 RID: 5688
		private const uint LOCALE_SDAYNAME4 = 45U;

		// Token: 0x04001639 RID: 5689
		private const uint LOCALE_SDAYNAME5 = 46U;

		// Token: 0x0400163A RID: 5690
		private const uint LOCALE_SDAYNAME6 = 47U;

		// Token: 0x0400163B RID: 5691
		private const uint LOCALE_SDAYNAME7 = 48U;

		// Token: 0x0400163C RID: 5692
		private const uint LOCALE_SABBREVDAYNAME1 = 49U;

		// Token: 0x0400163D RID: 5693
		private const uint LOCALE_SABBREVDAYNAME2 = 50U;

		// Token: 0x0400163E RID: 5694
		private const uint LOCALE_SABBREVDAYNAME3 = 51U;

		// Token: 0x0400163F RID: 5695
		private const uint LOCALE_SABBREVDAYNAME4 = 52U;

		// Token: 0x04001640 RID: 5696
		private const uint LOCALE_SABBREVDAYNAME5 = 53U;

		// Token: 0x04001641 RID: 5697
		private const uint LOCALE_SABBREVDAYNAME6 = 54U;

		// Token: 0x04001642 RID: 5698
		private const uint LOCALE_SABBREVDAYNAME7 = 55U;

		// Token: 0x04001643 RID: 5699
		private const uint LOCALE_SMONTHNAME1 = 56U;

		// Token: 0x04001644 RID: 5700
		private const uint LOCALE_SMONTHNAME2 = 57U;

		// Token: 0x04001645 RID: 5701
		private const uint LOCALE_SMONTHNAME3 = 58U;

		// Token: 0x04001646 RID: 5702
		private const uint LOCALE_SMONTHNAME4 = 59U;

		// Token: 0x04001647 RID: 5703
		private const uint LOCALE_SMONTHNAME5 = 60U;

		// Token: 0x04001648 RID: 5704
		private const uint LOCALE_SMONTHNAME6 = 61U;

		// Token: 0x04001649 RID: 5705
		private const uint LOCALE_SMONTHNAME7 = 62U;

		// Token: 0x0400164A RID: 5706
		private const uint LOCALE_SMONTHNAME8 = 63U;

		// Token: 0x0400164B RID: 5707
		private const uint LOCALE_SMONTHNAME9 = 64U;

		// Token: 0x0400164C RID: 5708
		private const uint LOCALE_SMONTHNAME10 = 65U;

		// Token: 0x0400164D RID: 5709
		private const uint LOCALE_SMONTHNAME11 = 66U;

		// Token: 0x0400164E RID: 5710
		private const uint LOCALE_SMONTHNAME12 = 67U;

		// Token: 0x0400164F RID: 5711
		private const uint LOCALE_SMONTHNAME13 = 4110U;

		// Token: 0x04001650 RID: 5712
		private const uint LOCALE_SABBREVMONTHNAME1 = 68U;

		// Token: 0x04001651 RID: 5713
		private const uint LOCALE_SABBREVMONTHNAME2 = 69U;

		// Token: 0x04001652 RID: 5714
		private const uint LOCALE_SABBREVMONTHNAME3 = 70U;

		// Token: 0x04001653 RID: 5715
		private const uint LOCALE_SABBREVMONTHNAME4 = 71U;

		// Token: 0x04001654 RID: 5716
		private const uint LOCALE_SABBREVMONTHNAME5 = 72U;

		// Token: 0x04001655 RID: 5717
		private const uint LOCALE_SABBREVMONTHNAME6 = 73U;

		// Token: 0x04001656 RID: 5718
		private const uint LOCALE_SABBREVMONTHNAME7 = 74U;

		// Token: 0x04001657 RID: 5719
		private const uint LOCALE_SABBREVMONTHNAME8 = 75U;

		// Token: 0x04001658 RID: 5720
		private const uint LOCALE_SABBREVMONTHNAME9 = 76U;

		// Token: 0x04001659 RID: 5721
		private const uint LOCALE_SABBREVMONTHNAME10 = 77U;

		// Token: 0x0400165A RID: 5722
		private const uint LOCALE_SABBREVMONTHNAME11 = 78U;

		// Token: 0x0400165B RID: 5723
		private const uint LOCALE_SABBREVMONTHNAME12 = 79U;

		// Token: 0x0400165C RID: 5724
		private const uint LOCALE_SABBREVMONTHNAME13 = 4111U;

		// Token: 0x0400165D RID: 5725
		private const uint LOCALE_SPOSITIVESIGN = 80U;

		// Token: 0x0400165E RID: 5726
		private const uint LOCALE_SNEGATIVESIGN = 81U;

		// Token: 0x0400165F RID: 5727
		private const uint LOCALE_IPOSSIGNPOSN = 82U;

		// Token: 0x04001660 RID: 5728
		private const uint LOCALE_INEGSIGNPOSN = 83U;

		// Token: 0x04001661 RID: 5729
		private const uint LOCALE_IPOSSYMPRECEDES = 84U;

		// Token: 0x04001662 RID: 5730
		private const uint LOCALE_IPOSSEPBYSPACE = 85U;

		// Token: 0x04001663 RID: 5731
		private const uint LOCALE_INEGSYMPRECEDES = 86U;

		// Token: 0x04001664 RID: 5732
		private const uint LOCALE_INEGSEPBYSPACE = 87U;

		// Token: 0x04001665 RID: 5733
		private const uint LOCALE_FONTSIGNATURE = 88U;

		// Token: 0x04001666 RID: 5734
		private const uint LOCALE_SISO639LANGNAME = 89U;

		// Token: 0x04001667 RID: 5735
		private const uint LOCALE_SISO3166CTRYNAME = 90U;

		// Token: 0x04001668 RID: 5736
		private const uint LOCALE_IDEFAULTEBCDICCODEPAGE = 4114U;

		// Token: 0x04001669 RID: 5737
		private const uint LOCALE_IPAPERSIZE = 4106U;

		// Token: 0x0400166A RID: 5738
		private const uint LOCALE_SENGCURRNAME = 4103U;

		// Token: 0x0400166B RID: 5739
		private const uint LOCALE_SNATIVECURRNAME = 4104U;

		// Token: 0x0400166C RID: 5740
		private const uint LOCALE_SYEARMONTH = 4102U;

		// Token: 0x0400166D RID: 5741
		private const uint LOCALE_SSORTNAME = 4115U;

		// Token: 0x0400166E RID: 5742
		private const uint LOCALE_IDIGITSUBSTITUTION = 4116U;

		// Token: 0x0400166F RID: 5743
		private const uint LOCALE_SNAME = 92U;

		// Token: 0x04001670 RID: 5744
		private const uint LOCALE_SDURATION = 93U;

		// Token: 0x04001671 RID: 5745
		private const uint LOCALE_SKEYBOARDSTOINSTALL = 94U;

		// Token: 0x04001672 RID: 5746
		private const uint LOCALE_SSHORTESTDAYNAME1 = 96U;

		// Token: 0x04001673 RID: 5747
		private const uint LOCALE_SSHORTESTDAYNAME2 = 97U;

		// Token: 0x04001674 RID: 5748
		private const uint LOCALE_SSHORTESTDAYNAME3 = 98U;

		// Token: 0x04001675 RID: 5749
		private const uint LOCALE_SSHORTESTDAYNAME4 = 99U;

		// Token: 0x04001676 RID: 5750
		private const uint LOCALE_SSHORTESTDAYNAME5 = 100U;

		// Token: 0x04001677 RID: 5751
		private const uint LOCALE_SSHORTESTDAYNAME6 = 101U;

		// Token: 0x04001678 RID: 5752
		private const uint LOCALE_SSHORTESTDAYNAME7 = 102U;

		// Token: 0x04001679 RID: 5753
		private const uint LOCALE_SISO639LANGNAME2 = 103U;

		// Token: 0x0400167A RID: 5754
		private const uint LOCALE_SISO3166CTRYNAME2 = 104U;

		// Token: 0x0400167B RID: 5755
		private const uint LOCALE_SNAN = 105U;

		// Token: 0x0400167C RID: 5756
		private const uint LOCALE_SPOSINFINITY = 106U;

		// Token: 0x0400167D RID: 5757
		private const uint LOCALE_SNEGINFINITY = 107U;

		// Token: 0x0400167E RID: 5758
		private const uint LOCALE_SSCRIPTS = 108U;

		// Token: 0x0400167F RID: 5759
		private const uint LOCALE_SPARENT = 109U;

		// Token: 0x04001680 RID: 5760
		private const uint LOCALE_SCONSOLEFALLBACKNAME = 110U;

		// Token: 0x04001681 RID: 5761
		private const uint LOCALE_IREADINGLAYOUT = 112U;

		// Token: 0x04001682 RID: 5762
		private const uint LOCALE_INEUTRAL = 113U;

		// Token: 0x04001683 RID: 5763
		private const uint LOCALE_INEGATIVEPERCENT = 116U;

		// Token: 0x04001684 RID: 5764
		private const uint LOCALE_IPOSITIVEPERCENT = 117U;

		// Token: 0x04001685 RID: 5765
		private const uint LOCALE_SPERCENT = 118U;

		// Token: 0x04001686 RID: 5766
		private const uint LOCALE_SPERMILLE = 119U;

		// Token: 0x04001687 RID: 5767
		private const uint LOCALE_SMONTHDAY = 120U;

		// Token: 0x04001688 RID: 5768
		private const uint LOCALE_SSHORTTIME = 121U;

		// Token: 0x04001689 RID: 5769
		private const uint LOCALE_SOPENTYPELANGUAGETAG = 122U;

		// Token: 0x0400168A RID: 5770
		private const uint LOCALE_SSORTLOCALE = 123U;

		// Token: 0x0400168B RID: 5771
		internal const uint TIME_NOSECONDS = 2U;
	}
}
