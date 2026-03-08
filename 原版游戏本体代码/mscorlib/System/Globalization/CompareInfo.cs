using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using Microsoft.Win32;

namespace System.Globalization
{
	// Token: 0x020003A8 RID: 936
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class CompareInfo : IDeserializationCallback
	{
		// Token: 0x06002E04 RID: 11780 RVA: 0x000B0084 File Offset: 0x000AE284
		internal CompareInfo(CultureInfo culture)
		{
			this.m_name = culture.m_name;
			this.m_sortName = culture.SortName;
			IntPtr handleOrigin;
			this.m_dataHandle = CompareInfo.InternalInitSortHandle(this.m_sortName, out handleOrigin);
			this.m_handleOrigin = handleOrigin;
		}

		// Token: 0x06002E05 RID: 11781 RVA: 0x000B00CC File Offset: 0x000AE2CC
		public static CompareInfo GetCompareInfo(int culture, Assembly assembly)
		{
			if (assembly == null)
			{
				throw new ArgumentNullException("assembly");
			}
			if (assembly != typeof(object).Module.Assembly)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_OnlyMscorlib"));
			}
			return CompareInfo.GetCompareInfo(culture);
		}

		// Token: 0x06002E06 RID: 11782 RVA: 0x000B0120 File Offset: 0x000AE320
		public static CompareInfo GetCompareInfo(string name, Assembly assembly)
		{
			if (name == null || assembly == null)
			{
				throw new ArgumentNullException((name == null) ? "name" : "assembly");
			}
			if (assembly != typeof(object).Module.Assembly)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_OnlyMscorlib"));
			}
			return CompareInfo.GetCompareInfo(name);
		}

		// Token: 0x06002E07 RID: 11783 RVA: 0x000B0180 File Offset: 0x000AE380
		public static CompareInfo GetCompareInfo(int culture)
		{
			if (CultureData.IsCustomCultureId(culture))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_CustomCultureCannotBePassedByNumber", new object[] { "culture" }));
			}
			return CultureInfo.GetCultureInfo(culture).CompareInfo;
		}

		// Token: 0x06002E08 RID: 11784 RVA: 0x000B01B3 File Offset: 0x000AE3B3
		[__DynamicallyInvokable]
		public static CompareInfo GetCompareInfo(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			return CultureInfo.GetCultureInfo(name).CompareInfo;
		}

		// Token: 0x06002E09 RID: 11785 RVA: 0x000B01CE File Offset: 0x000AE3CE
		[ComVisible(false)]
		public static bool IsSortable(char ch)
		{
			return CompareInfo.IsSortable(ch.ToString());
		}

		// Token: 0x06002E0A RID: 11786 RVA: 0x000B01DC File Offset: 0x000AE3DC
		[SecuritySafeCritical]
		[ComVisible(false)]
		public static bool IsSortable(string text)
		{
			if (text == null)
			{
				throw new ArgumentNullException("text");
			}
			if (text.Length == 0)
			{
				return false;
			}
			CompareInfo compareInfo = CultureInfo.InvariantCulture.CompareInfo;
			return CompareInfo.InternalIsSortable(compareInfo.m_dataHandle, compareInfo.m_handleOrigin, compareInfo.m_sortName, text, text.Length);
		}

		// Token: 0x06002E0B RID: 11787 RVA: 0x000B022A File Offset: 0x000AE42A
		[OnDeserializing]
		private void OnDeserializing(StreamingContext ctx)
		{
			this.m_name = null;
		}

		// Token: 0x06002E0C RID: 11788 RVA: 0x000B0234 File Offset: 0x000AE434
		private void OnDeserialized()
		{
			CultureInfo cultureInfo;
			if (this.m_name == null)
			{
				cultureInfo = CultureInfo.GetCultureInfo(this.culture);
				this.m_name = cultureInfo.m_name;
			}
			else
			{
				cultureInfo = CultureInfo.GetCultureInfo(this.m_name);
			}
			this.m_sortName = cultureInfo.SortName;
			IntPtr handleOrigin;
			this.m_dataHandle = CompareInfo.InternalInitSortHandle(this.m_sortName, out handleOrigin);
			this.m_handleOrigin = handleOrigin;
		}

		// Token: 0x06002E0D RID: 11789 RVA: 0x000B0295 File Offset: 0x000AE495
		[OnDeserialized]
		private void OnDeserialized(StreamingContext ctx)
		{
			this.OnDeserialized();
		}

		// Token: 0x06002E0E RID: 11790 RVA: 0x000B029D File Offset: 0x000AE49D
		[OnSerializing]
		private void OnSerializing(StreamingContext ctx)
		{
			this.culture = CultureInfo.GetCultureInfo(this.Name).LCID;
		}

		// Token: 0x06002E0F RID: 11791 RVA: 0x000B02B5 File Offset: 0x000AE4B5
		void IDeserializationCallback.OnDeserialization(object sender)
		{
			this.OnDeserialized();
		}

		// Token: 0x170005FB RID: 1531
		// (get) Token: 0x06002E10 RID: 11792 RVA: 0x000B02BD File Offset: 0x000AE4BD
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public virtual string Name
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.m_name == "zh-CHT" || this.m_name == "zh-CHS")
				{
					return this.m_name;
				}
				return this.m_sortName;
			}
		}

		// Token: 0x06002E11 RID: 11793 RVA: 0x000B02F0 File Offset: 0x000AE4F0
		internal static int GetNativeCompareFlags(CompareOptions options)
		{
			int num = 134217728;
			if ((options & CompareOptions.IgnoreCase) != CompareOptions.None)
			{
				num |= 1;
			}
			if ((options & CompareOptions.IgnoreKanaType) != CompareOptions.None)
			{
				num |= 65536;
			}
			if ((options & CompareOptions.IgnoreNonSpace) != CompareOptions.None)
			{
				num |= 2;
			}
			if ((options & CompareOptions.IgnoreSymbols) != CompareOptions.None)
			{
				num |= 4;
			}
			if ((options & CompareOptions.IgnoreWidth) != CompareOptions.None)
			{
				num |= 131072;
			}
			if ((options & CompareOptions.StringSort) != CompareOptions.None)
			{
				num |= 4096;
			}
			if (options == CompareOptions.Ordinal)
			{
				num = 1073741824;
			}
			return num;
		}

		// Token: 0x06002E12 RID: 11794 RVA: 0x000B0359 File Offset: 0x000AE559
		[__DynamicallyInvokable]
		public virtual int Compare(string string1, string string2)
		{
			return this.Compare(string1, string2, CompareOptions.None);
		}

		// Token: 0x06002E13 RID: 11795 RVA: 0x000B0364 File Offset: 0x000AE564
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public virtual int Compare(string string1, string string2, CompareOptions options)
		{
			if (options == CompareOptions.OrdinalIgnoreCase)
			{
				return string.Compare(string1, string2, StringComparison.OrdinalIgnoreCase);
			}
			if ((options & CompareOptions.Ordinal) != CompareOptions.None)
			{
				if (options != CompareOptions.Ordinal)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_CompareOptionOrdinal"), "options");
				}
				return string.CompareOrdinal(string1, string2);
			}
			else
			{
				if ((options & ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.StringSort)) != CompareOptions.None)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
				}
				if (string1 == null)
				{
					if (string2 == null)
					{
						return 0;
					}
					return -1;
				}
				else
				{
					if (string2 == null)
					{
						return 1;
					}
					return CompareInfo.InternalCompareString(this.m_dataHandle, this.m_handleOrigin, this.m_sortName, string1, 0, string1.Length, string2, 0, string2.Length, CompareInfo.GetNativeCompareFlags(options));
				}
			}
		}

		// Token: 0x06002E14 RID: 11796 RVA: 0x000B040A File Offset: 0x000AE60A
		[__DynamicallyInvokable]
		public virtual int Compare(string string1, int offset1, int length1, string string2, int offset2, int length2)
		{
			return this.Compare(string1, offset1, length1, string2, offset2, length2, CompareOptions.None);
		}

		// Token: 0x06002E15 RID: 11797 RVA: 0x000B041C File Offset: 0x000AE61C
		[__DynamicallyInvokable]
		public virtual int Compare(string string1, int offset1, string string2, int offset2, CompareOptions options)
		{
			return this.Compare(string1, offset1, (string1 == null) ? 0 : (string1.Length - offset1), string2, offset2, (string2 == null) ? 0 : (string2.Length - offset2), options);
		}

		// Token: 0x06002E16 RID: 11798 RVA: 0x000B0448 File Offset: 0x000AE648
		[__DynamicallyInvokable]
		public virtual int Compare(string string1, int offset1, string string2, int offset2)
		{
			return this.Compare(string1, offset1, string2, offset2, CompareOptions.None);
		}

		// Token: 0x06002E17 RID: 11799 RVA: 0x000B0458 File Offset: 0x000AE658
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public virtual int Compare(string string1, int offset1, int length1, string string2, int offset2, int length2, CompareOptions options)
		{
			if (options == CompareOptions.OrdinalIgnoreCase)
			{
				int num = string.Compare(string1, offset1, string2, offset2, (length1 < length2) ? length1 : length2, StringComparison.OrdinalIgnoreCase);
				if (length1 == length2 || num != 0)
				{
					return num;
				}
				if (length1 <= length2)
				{
					return -1;
				}
				return 1;
			}
			else
			{
				if (length1 < 0 || length2 < 0)
				{
					throw new ArgumentOutOfRangeException((length1 < 0) ? "length1" : "length2", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
				}
				if (offset1 < 0 || offset2 < 0)
				{
					throw new ArgumentOutOfRangeException((offset1 < 0) ? "offset1" : "offset2", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
				}
				if (offset1 > ((string1 == null) ? 0 : string1.Length) - length1)
				{
					throw new ArgumentOutOfRangeException("string1", Environment.GetResourceString("ArgumentOutOfRange_OffsetLength"));
				}
				if (offset2 > ((string2 == null) ? 0 : string2.Length) - length2)
				{
					throw new ArgumentOutOfRangeException("string2", Environment.GetResourceString("ArgumentOutOfRange_OffsetLength"));
				}
				if ((options & CompareOptions.Ordinal) != CompareOptions.None)
				{
					if (options != CompareOptions.Ordinal)
					{
						throw new ArgumentException(Environment.GetResourceString("Argument_CompareOptionOrdinal"), "options");
					}
				}
				else if ((options & ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.StringSort)) != CompareOptions.None)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
				}
				if (string1 == null)
				{
					if (string2 == null)
					{
						return 0;
					}
					return -1;
				}
				else
				{
					if (string2 == null)
					{
						return 1;
					}
					if (options == CompareOptions.Ordinal)
					{
						return CompareInfo.CompareOrdinal(string1, offset1, length1, string2, offset2, length2);
					}
					return CompareInfo.InternalCompareString(this.m_dataHandle, this.m_handleOrigin, this.m_sortName, string1, offset1, length1, string2, offset2, length2, CompareInfo.GetNativeCompareFlags(options));
				}
			}
		}

		// Token: 0x06002E18 RID: 11800 RVA: 0x000B05D4 File Offset: 0x000AE7D4
		[SecurityCritical]
		private static int CompareOrdinal(string string1, int offset1, int length1, string string2, int offset2, int length2)
		{
			int num = string.nativeCompareOrdinalEx(string1, offset1, string2, offset2, (length1 < length2) ? length1 : length2);
			if (length1 == length2 || num != 0)
			{
				return num;
			}
			if (length1 <= length2)
			{
				return -1;
			}
			return 1;
		}

		// Token: 0x06002E19 RID: 11801 RVA: 0x000B0608 File Offset: 0x000AE808
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public virtual bool IsPrefix(string source, string prefix, CompareOptions options)
		{
			if (source == null || prefix == null)
			{
				throw new ArgumentNullException((source == null) ? "source" : "prefix", Environment.GetResourceString("ArgumentNull_String"));
			}
			if (prefix.Length == 0)
			{
				return true;
			}
			if (options == CompareOptions.OrdinalIgnoreCase)
			{
				return source.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
			}
			if (options == CompareOptions.Ordinal)
			{
				return source.StartsWith(prefix, StringComparison.Ordinal);
			}
			if ((options & ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth)) != CompareOptions.None)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
			}
			return CompareInfo.InternalFindNLSStringEx(this.m_dataHandle, this.m_handleOrigin, this.m_sortName, CompareInfo.GetNativeCompareFlags(options) | 1048576 | ((source.IsAscii() && prefix.IsAscii()) ? 536870912 : 0), source, source.Length, 0, prefix, prefix.Length) > -1;
		}

		// Token: 0x06002E1A RID: 11802 RVA: 0x000B06D1 File Offset: 0x000AE8D1
		[__DynamicallyInvokable]
		public virtual bool IsPrefix(string source, string prefix)
		{
			return this.IsPrefix(source, prefix, CompareOptions.None);
		}

		// Token: 0x06002E1B RID: 11803 RVA: 0x000B06DC File Offset: 0x000AE8DC
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public virtual bool IsSuffix(string source, string suffix, CompareOptions options)
		{
			if (source == null || suffix == null)
			{
				throw new ArgumentNullException((source == null) ? "source" : "suffix", Environment.GetResourceString("ArgumentNull_String"));
			}
			if (suffix.Length == 0)
			{
				return true;
			}
			if (options == CompareOptions.OrdinalIgnoreCase)
			{
				return source.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
			}
			if (options == CompareOptions.Ordinal)
			{
				return source.EndsWith(suffix, StringComparison.Ordinal);
			}
			if ((options & ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth)) != CompareOptions.None)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
			}
			return CompareInfo.InternalFindNLSStringEx(this.m_dataHandle, this.m_handleOrigin, this.m_sortName, CompareInfo.GetNativeCompareFlags(options) | 2097152 | ((source.IsAscii() && suffix.IsAscii()) ? 536870912 : 0), source, source.Length, source.Length - 1, suffix, suffix.Length) >= 0;
		}

		// Token: 0x06002E1C RID: 11804 RVA: 0x000B07AF File Offset: 0x000AE9AF
		[__DynamicallyInvokable]
		public virtual bool IsSuffix(string source, string suffix)
		{
			return this.IsSuffix(source, suffix, CompareOptions.None);
		}

		// Token: 0x06002E1D RID: 11805 RVA: 0x000B07BA File Offset: 0x000AE9BA
		[__DynamicallyInvokable]
		public virtual int IndexOf(string source, char value)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			return this.IndexOf(source, value, 0, source.Length, CompareOptions.None);
		}

		// Token: 0x06002E1E RID: 11806 RVA: 0x000B07DA File Offset: 0x000AE9DA
		[__DynamicallyInvokable]
		public virtual int IndexOf(string source, string value)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			return this.IndexOf(source, value, 0, source.Length, CompareOptions.None);
		}

		// Token: 0x06002E1F RID: 11807 RVA: 0x000B07FA File Offset: 0x000AE9FA
		[__DynamicallyInvokable]
		public virtual int IndexOf(string source, char value, CompareOptions options)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			return this.IndexOf(source, value, 0, source.Length, options);
		}

		// Token: 0x06002E20 RID: 11808 RVA: 0x000B081A File Offset: 0x000AEA1A
		[__DynamicallyInvokable]
		public virtual int IndexOf(string source, string value, CompareOptions options)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			return this.IndexOf(source, value, 0, source.Length, options);
		}

		// Token: 0x06002E21 RID: 11809 RVA: 0x000B083A File Offset: 0x000AEA3A
		public virtual int IndexOf(string source, char value, int startIndex)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			return this.IndexOf(source, value, startIndex, source.Length - startIndex, CompareOptions.None);
		}

		// Token: 0x06002E22 RID: 11810 RVA: 0x000B085C File Offset: 0x000AEA5C
		public virtual int IndexOf(string source, string value, int startIndex)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			return this.IndexOf(source, value, startIndex, source.Length - startIndex, CompareOptions.None);
		}

		// Token: 0x06002E23 RID: 11811 RVA: 0x000B087E File Offset: 0x000AEA7E
		[__DynamicallyInvokable]
		public virtual int IndexOf(string source, char value, int startIndex, CompareOptions options)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			return this.IndexOf(source, value, startIndex, source.Length - startIndex, options);
		}

		// Token: 0x06002E24 RID: 11812 RVA: 0x000B08A1 File Offset: 0x000AEAA1
		[__DynamicallyInvokable]
		public virtual int IndexOf(string source, string value, int startIndex, CompareOptions options)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			return this.IndexOf(source, value, startIndex, source.Length - startIndex, options);
		}

		// Token: 0x06002E25 RID: 11813 RVA: 0x000B08C4 File Offset: 0x000AEAC4
		[__DynamicallyInvokable]
		public virtual int IndexOf(string source, char value, int startIndex, int count)
		{
			return this.IndexOf(source, value, startIndex, count, CompareOptions.None);
		}

		// Token: 0x06002E26 RID: 11814 RVA: 0x000B08D2 File Offset: 0x000AEAD2
		[__DynamicallyInvokable]
		public virtual int IndexOf(string source, string value, int startIndex, int count)
		{
			return this.IndexOf(source, value, startIndex, count, CompareOptions.None);
		}

		// Token: 0x06002E27 RID: 11815 RVA: 0x000B08E0 File Offset: 0x000AEAE0
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public virtual int IndexOf(string source, char value, int startIndex, int count, CompareOptions options)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			if (startIndex < 0 || startIndex > source.Length)
			{
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (count < 0 || startIndex > source.Length - count)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
			}
			if (options == CompareOptions.OrdinalIgnoreCase)
			{
				return source.IndexOf(value.ToString(), startIndex, count, StringComparison.OrdinalIgnoreCase);
			}
			if ((options & ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth)) != CompareOptions.None && options != CompareOptions.Ordinal)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
			}
			return CompareInfo.InternalFindNLSStringEx(this.m_dataHandle, this.m_handleOrigin, this.m_sortName, CompareInfo.GetNativeCompareFlags(options) | 4194304 | ((source.IsAscii() && value <= '\u007f') ? 536870912 : 0), source, count, startIndex, new string(value, 1), 1);
		}

		// Token: 0x06002E28 RID: 11816 RVA: 0x000B09CC File Offset: 0x000AEBCC
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public virtual int IndexOf(string source, string value, int startIndex, int count, CompareOptions options)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (startIndex > source.Length)
			{
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (source.Length == 0)
			{
				if (value.Length == 0)
				{
					return 0;
				}
				return -1;
			}
			else
			{
				if (startIndex < 0)
				{
					throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
				}
				if (count < 0 || startIndex > source.Length - count)
				{
					throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
				}
				if (options == CompareOptions.OrdinalIgnoreCase)
				{
					return source.IndexOf(value, startIndex, count, StringComparison.OrdinalIgnoreCase);
				}
				if ((options & ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth)) != CompareOptions.None && options != CompareOptions.Ordinal)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
				}
				return CompareInfo.InternalFindNLSStringEx(this.m_dataHandle, this.m_handleOrigin, this.m_sortName, CompareInfo.GetNativeCompareFlags(options) | 4194304 | ((source.IsAscii() && value.IsAscii()) ? 536870912 : 0), source, count, startIndex, value, value.Length);
			}
		}

		// Token: 0x06002E29 RID: 11817 RVA: 0x000B0AE8 File Offset: 0x000AECE8
		[__DynamicallyInvokable]
		public virtual int LastIndexOf(string source, char value)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			return this.LastIndexOf(source, value, source.Length - 1, source.Length, CompareOptions.None);
		}

		// Token: 0x06002E2A RID: 11818 RVA: 0x000B0B0F File Offset: 0x000AED0F
		[__DynamicallyInvokable]
		public virtual int LastIndexOf(string source, string value)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			return this.LastIndexOf(source, value, source.Length - 1, source.Length, CompareOptions.None);
		}

		// Token: 0x06002E2B RID: 11819 RVA: 0x000B0B36 File Offset: 0x000AED36
		[__DynamicallyInvokable]
		public virtual int LastIndexOf(string source, char value, CompareOptions options)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			return this.LastIndexOf(source, value, source.Length - 1, source.Length, options);
		}

		// Token: 0x06002E2C RID: 11820 RVA: 0x000B0B5D File Offset: 0x000AED5D
		[__DynamicallyInvokable]
		public virtual int LastIndexOf(string source, string value, CompareOptions options)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			return this.LastIndexOf(source, value, source.Length - 1, source.Length, options);
		}

		// Token: 0x06002E2D RID: 11821 RVA: 0x000B0B84 File Offset: 0x000AED84
		public virtual int LastIndexOf(string source, char value, int startIndex)
		{
			return this.LastIndexOf(source, value, startIndex, startIndex + 1, CompareOptions.None);
		}

		// Token: 0x06002E2E RID: 11822 RVA: 0x000B0B93 File Offset: 0x000AED93
		public virtual int LastIndexOf(string source, string value, int startIndex)
		{
			return this.LastIndexOf(source, value, startIndex, startIndex + 1, CompareOptions.None);
		}

		// Token: 0x06002E2F RID: 11823 RVA: 0x000B0BA2 File Offset: 0x000AEDA2
		[__DynamicallyInvokable]
		public virtual int LastIndexOf(string source, char value, int startIndex, CompareOptions options)
		{
			return this.LastIndexOf(source, value, startIndex, startIndex + 1, options);
		}

		// Token: 0x06002E30 RID: 11824 RVA: 0x000B0BB2 File Offset: 0x000AEDB2
		[__DynamicallyInvokable]
		public virtual int LastIndexOf(string source, string value, int startIndex, CompareOptions options)
		{
			return this.LastIndexOf(source, value, startIndex, startIndex + 1, options);
		}

		// Token: 0x06002E31 RID: 11825 RVA: 0x000B0BC2 File Offset: 0x000AEDC2
		[__DynamicallyInvokable]
		public virtual int LastIndexOf(string source, char value, int startIndex, int count)
		{
			return this.LastIndexOf(source, value, startIndex, count, CompareOptions.None);
		}

		// Token: 0x06002E32 RID: 11826 RVA: 0x000B0BD0 File Offset: 0x000AEDD0
		[__DynamicallyInvokable]
		public virtual int LastIndexOf(string source, string value, int startIndex, int count)
		{
			return this.LastIndexOf(source, value, startIndex, count, CompareOptions.None);
		}

		// Token: 0x06002E33 RID: 11827 RVA: 0x000B0BE0 File Offset: 0x000AEDE0
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public virtual int LastIndexOf(string source, char value, int startIndex, int count, CompareOptions options)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			if ((options & ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth)) != CompareOptions.None && options != CompareOptions.Ordinal && options != CompareOptions.OrdinalIgnoreCase)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
			}
			if (source.Length == 0 && (startIndex == -1 || startIndex == 0))
			{
				return -1;
			}
			if (startIndex < 0 || startIndex > source.Length)
			{
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			if (startIndex == source.Length)
			{
				startIndex--;
				if (count > 0)
				{
					count--;
				}
			}
			if (count < 0 || startIndex - count + 1 < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
			}
			if (options == CompareOptions.OrdinalIgnoreCase)
			{
				return source.LastIndexOf(value.ToString(), startIndex, count, StringComparison.OrdinalIgnoreCase);
			}
			return CompareInfo.InternalFindNLSStringEx(this.m_dataHandle, this.m_handleOrigin, this.m_sortName, CompareInfo.GetNativeCompareFlags(options) | 8388608 | ((source.IsAscii() && value <= '\u007f') ? 536870912 : 0), source, count, startIndex, new string(value, 1), 1);
		}

		// Token: 0x06002E34 RID: 11828 RVA: 0x000B0CFC File Offset: 0x000AEEFC
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public virtual int LastIndexOf(string source, string value, int startIndex, int count, CompareOptions options)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if ((options & ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth)) != CompareOptions.None && options != CompareOptions.Ordinal && options != CompareOptions.OrdinalIgnoreCase)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
			}
			if (source.Length == 0 && (startIndex == -1 || startIndex == 0))
			{
				if (value.Length != 0)
				{
					return -1;
				}
				return 0;
			}
			else
			{
				if (startIndex < 0 || startIndex > source.Length)
				{
					throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
				}
				if (startIndex == source.Length)
				{
					startIndex--;
					if (count > 0)
					{
						count--;
					}
					if (value.Length == 0 && count >= 0 && startIndex - count + 1 >= 0)
					{
						return startIndex;
					}
				}
				if (count < 0 || startIndex - count + 1 < 0)
				{
					throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
				}
				if (options == CompareOptions.OrdinalIgnoreCase)
				{
					return source.LastIndexOf(value, startIndex, count, StringComparison.OrdinalIgnoreCase);
				}
				return CompareInfo.InternalFindNLSStringEx(this.m_dataHandle, this.m_handleOrigin, this.m_sortName, CompareInfo.GetNativeCompareFlags(options) | 8388608 | ((source.IsAscii() && value.IsAscii()) ? 536870912 : 0), source, count, startIndex, value, value.Length);
			}
		}

		// Token: 0x06002E35 RID: 11829 RVA: 0x000B0E41 File Offset: 0x000AF041
		public virtual SortKey GetSortKey(string source, CompareOptions options)
		{
			return this.CreateSortKey(source, options);
		}

		// Token: 0x06002E36 RID: 11830 RVA: 0x000B0E4B File Offset: 0x000AF04B
		public virtual SortKey GetSortKey(string source)
		{
			return this.CreateSortKey(source, CompareOptions.None);
		}

		// Token: 0x06002E37 RID: 11831 RVA: 0x000B0E58 File Offset: 0x000AF058
		[SecuritySafeCritical]
		private SortKey CreateSortKey(string source, CompareOptions options)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			if ((options & ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.StringSort)) != CompareOptions.None)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
			}
			byte[] array = null;
			if (string.IsNullOrEmpty(source))
			{
				array = EmptyArray<byte>.Value;
				source = "\0";
			}
			int nativeCompareFlags = CompareInfo.GetNativeCompareFlags(options);
			int num = CompareInfo.InternalGetSortKey(this.m_dataHandle, this.m_handleOrigin, this.m_sortName, nativeCompareFlags, source, source.Length, null, 0);
			if (num == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "source");
			}
			if (array == null)
			{
				array = new byte[num];
				num = CompareInfo.InternalGetSortKey(this.m_dataHandle, this.m_handleOrigin, this.m_sortName, nativeCompareFlags, source, source.Length, array, array.Length);
			}
			else
			{
				source = string.Empty;
			}
			return new SortKey(this.Name, source, options, array);
		}

		// Token: 0x06002E38 RID: 11832 RVA: 0x000B0F30 File Offset: 0x000AF130
		[__DynamicallyInvokable]
		public override bool Equals(object value)
		{
			CompareInfo compareInfo = value as CompareInfo;
			return compareInfo != null && this.Name == compareInfo.Name;
		}

		// Token: 0x06002E39 RID: 11833 RVA: 0x000B0F5A File Offset: 0x000AF15A
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}

		// Token: 0x06002E3A RID: 11834 RVA: 0x000B0F67 File Offset: 0x000AF167
		[__DynamicallyInvokable]
		public virtual int GetHashCode(string source, CompareOptions options)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			if (options == CompareOptions.Ordinal)
			{
				return source.GetHashCode();
			}
			if (options == CompareOptions.OrdinalIgnoreCase)
			{
				return TextInfo.GetHashCodeOrdinalIgnoreCase(source);
			}
			return this.GetHashCodeOfString(source, options, false, 0L);
		}

		// Token: 0x06002E3B RID: 11835 RVA: 0x000B0FA0 File Offset: 0x000AF1A0
		internal int GetHashCodeOfString(string source, CompareOptions options)
		{
			return this.GetHashCodeOfString(source, options, false, 0L);
		}

		// Token: 0x06002E3C RID: 11836 RVA: 0x000B0FB0 File Offset: 0x000AF1B0
		[SecuritySafeCritical]
		internal int GetHashCodeOfString(string source, CompareOptions options, bool forceRandomizedHashing, long additionalEntropy)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			if ((options & ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth)) != CompareOptions.None)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
			}
			if (source.Length == 0)
			{
				return 0;
			}
			return CompareInfo.InternalGetGlobalizedHashCode(this.m_dataHandle, this.m_handleOrigin, this.m_sortName, source, source.Length, CompareInfo.GetNativeCompareFlags(options), forceRandomizedHashing, additionalEntropy);
		}

		// Token: 0x06002E3D RID: 11837 RVA: 0x000B1017 File Offset: 0x000AF217
		[__DynamicallyInvokable]
		public override string ToString()
		{
			return "CompareInfo - " + this.Name;
		}

		// Token: 0x170005FC RID: 1532
		// (get) Token: 0x06002E3E RID: 11838 RVA: 0x000B1029 File Offset: 0x000AF229
		public int LCID
		{
			get
			{
				return CultureInfo.GetCultureInfo(this.Name).LCID;
			}
		}

		// Token: 0x06002E3F RID: 11839 RVA: 0x000B103B File Offset: 0x000AF23B
		[SecuritySafeCritical]
		internal static IntPtr InternalInitSortHandle(string localeName, out IntPtr handleOrigin)
		{
			return CompareInfo.NativeInternalInitSortHandle(localeName, out handleOrigin);
		}

		// Token: 0x170005FD RID: 1533
		// (get) Token: 0x06002E40 RID: 11840 RVA: 0x000B1044 File Offset: 0x000AF244
		internal static bool IsLegacy20SortingBehaviorRequested
		{
			get
			{
				return CompareInfo.InternalSortVersion == 4096U;
			}
		}

		// Token: 0x170005FE RID: 1534
		// (get) Token: 0x06002E41 RID: 11841 RVA: 0x000B1052 File Offset: 0x000AF252
		private static uint InternalSortVersion
		{
			[SecuritySafeCritical]
			get
			{
				return CompareInfo.InternalGetSortVersion();
			}
		}

		// Token: 0x170005FF RID: 1535
		// (get) Token: 0x06002E42 RID: 11842 RVA: 0x000B105C File Offset: 0x000AF25C
		public SortVersion Version
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_SortVersion == null)
				{
					Win32Native.NlsVersionInfoEx nlsVersionInfoEx = default(Win32Native.NlsVersionInfoEx);
					nlsVersionInfoEx.dwNLSVersionInfoSize = Marshal.SizeOf(typeof(Win32Native.NlsVersionInfoEx));
					CompareInfo.InternalGetNlsVersionEx(this.m_dataHandle, this.m_handleOrigin, this.m_sortName, ref nlsVersionInfoEx);
					this.m_SortVersion = new SortVersion(nlsVersionInfoEx.dwNLSVersion, (nlsVersionInfoEx.dwEffectiveId != 0) ? nlsVersionInfoEx.dwEffectiveId : this.LCID, nlsVersionInfoEx.guidCustomVersion);
				}
				return this.m_SortVersion;
			}
		}

		// Token: 0x06002E43 RID: 11843
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool InternalGetNlsVersionEx(IntPtr handle, IntPtr handleOrigin, string localeName, ref Win32Native.NlsVersionInfoEx lpNlsVersionInformation);

		// Token: 0x06002E44 RID: 11844
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern uint InternalGetSortVersion();

		// Token: 0x06002E45 RID: 11845
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern IntPtr NativeInternalInitSortHandle(string localeName, out IntPtr handleOrigin);

		// Token: 0x06002E46 RID: 11846
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern int InternalGetGlobalizedHashCode(IntPtr handle, IntPtr handleOrigin, string localeName, string source, int length, int dwFlags, bool forceRandomizedHashing, long additionalEntropy);

		// Token: 0x06002E47 RID: 11847
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool InternalIsSortable(IntPtr handle, IntPtr handleOrigin, string localeName, string source, int length);

		// Token: 0x06002E48 RID: 11848
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern int InternalCompareString(IntPtr handle, IntPtr handleOrigin, string localeName, string string1, int offset1, int length1, string string2, int offset2, int length2, int flags);

		// Token: 0x06002E49 RID: 11849
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern int InternalFindNLSStringEx(IntPtr handle, IntPtr handleOrigin, string localeName, int flags, string source, int sourceCount, int startIndex, string target, int targetCount);

		// Token: 0x06002E4A RID: 11850
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern int InternalGetSortKey(IntPtr handle, IntPtr handleOrigin, string localeName, int flags, string source, int sourceCount, byte[] target, int targetCount);

		// Token: 0x04001318 RID: 4888
		private const CompareOptions ValidIndexMaskOffFlags = ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth);

		// Token: 0x04001319 RID: 4889
		private const CompareOptions ValidCompareMaskOffFlags = ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.StringSort);

		// Token: 0x0400131A RID: 4890
		private const CompareOptions ValidHashCodeOfStringMaskOffFlags = ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth);

		// Token: 0x0400131B RID: 4891
		[OptionalField(VersionAdded = 2)]
		private string m_name;

		// Token: 0x0400131C RID: 4892
		[NonSerialized]
		private string m_sortName;

		// Token: 0x0400131D RID: 4893
		[NonSerialized]
		private IntPtr m_dataHandle;

		// Token: 0x0400131E RID: 4894
		[NonSerialized]
		private IntPtr m_handleOrigin;

		// Token: 0x0400131F RID: 4895
		[OptionalField(VersionAdded = 1)]
		private int win32LCID;

		// Token: 0x04001320 RID: 4896
		private int culture;

		// Token: 0x04001321 RID: 4897
		private const int LINGUISTIC_IGNORECASE = 16;

		// Token: 0x04001322 RID: 4898
		private const int NORM_IGNORECASE = 1;

		// Token: 0x04001323 RID: 4899
		private const int NORM_IGNOREKANATYPE = 65536;

		// Token: 0x04001324 RID: 4900
		private const int LINGUISTIC_IGNOREDIACRITIC = 32;

		// Token: 0x04001325 RID: 4901
		private const int NORM_IGNORENONSPACE = 2;

		// Token: 0x04001326 RID: 4902
		private const int NORM_IGNORESYMBOLS = 4;

		// Token: 0x04001327 RID: 4903
		private const int NORM_IGNOREWIDTH = 131072;

		// Token: 0x04001328 RID: 4904
		private const int SORT_STRINGSORT = 4096;

		// Token: 0x04001329 RID: 4905
		private const int COMPARE_OPTIONS_ORDINAL = 1073741824;

		// Token: 0x0400132A RID: 4906
		internal const int NORM_LINGUISTIC_CASING = 134217728;

		// Token: 0x0400132B RID: 4907
		private const int RESERVED_FIND_ASCII_STRING = 536870912;

		// Token: 0x0400132C RID: 4908
		private const int SORT_VERSION_WHIDBEY = 4096;

		// Token: 0x0400132D RID: 4909
		private const int SORT_VERSION_V4 = 393473;

		// Token: 0x0400132E RID: 4910
		[OptionalField(VersionAdded = 3)]
		private SortVersion m_SortVersion;
	}
}
