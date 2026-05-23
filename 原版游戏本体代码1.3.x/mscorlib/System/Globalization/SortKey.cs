using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Globalization
{
	// Token: 0x020003D0 RID: 976
	[ComVisible(true)]
	[Serializable]
	public class SortKey
	{
		// Token: 0x06003161 RID: 12641 RVA: 0x000BDB14 File Offset: 0x000BBD14
		internal SortKey(string localeName, string str, CompareOptions options, byte[] keyData)
		{
			this.m_KeyData = keyData;
			this.localeName = localeName;
			this.options = options;
			this.m_String = str;
		}

		// Token: 0x06003162 RID: 12642 RVA: 0x000BDB39 File Offset: 0x000BBD39
		[OnSerializing]
		private void OnSerializing(StreamingContext context)
		{
			if (this.win32LCID == 0)
			{
				this.win32LCID = CultureInfo.GetCultureInfo(this.localeName).LCID;
			}
		}

		// Token: 0x06003163 RID: 12643 RVA: 0x000BDB59 File Offset: 0x000BBD59
		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			if (string.IsNullOrEmpty(this.localeName) && this.win32LCID != 0)
			{
				this.localeName = CultureInfo.GetCultureInfo(this.win32LCID).Name;
			}
		}

		// Token: 0x170006E7 RID: 1767
		// (get) Token: 0x06003164 RID: 12644 RVA: 0x000BDB86 File Offset: 0x000BBD86
		public virtual string OriginalString
		{
			get
			{
				return this.m_String;
			}
		}

		// Token: 0x170006E8 RID: 1768
		// (get) Token: 0x06003165 RID: 12645 RVA: 0x000BDB8E File Offset: 0x000BBD8E
		public virtual byte[] KeyData
		{
			get
			{
				return (byte[])this.m_KeyData.Clone();
			}
		}

		// Token: 0x06003166 RID: 12646 RVA: 0x000BDBA0 File Offset: 0x000BBDA0
		public static int Compare(SortKey sortkey1, SortKey sortkey2)
		{
			if (sortkey1 == null || sortkey2 == null)
			{
				throw new ArgumentNullException((sortkey1 == null) ? "sortkey1" : "sortkey2");
			}
			byte[] keyData = sortkey1.m_KeyData;
			byte[] keyData2 = sortkey2.m_KeyData;
			if (keyData.Length == 0)
			{
				if (keyData2.Length == 0)
				{
					return 0;
				}
				return -1;
			}
			else
			{
				if (keyData2.Length == 0)
				{
					return 1;
				}
				int num = ((keyData.Length < keyData2.Length) ? keyData.Length : keyData2.Length);
				for (int i = 0; i < num; i++)
				{
					if (keyData[i] > keyData2[i])
					{
						return 1;
					}
					if (keyData[i] < keyData2[i])
					{
						return -1;
					}
				}
				return 0;
			}
		}

		// Token: 0x06003167 RID: 12647 RVA: 0x000BDC1C File Offset: 0x000BBE1C
		public override bool Equals(object value)
		{
			SortKey sortKey = value as SortKey;
			return sortKey != null && SortKey.Compare(this, sortKey) == 0;
		}

		// Token: 0x06003168 RID: 12648 RVA: 0x000BDC3F File Offset: 0x000BBE3F
		public override int GetHashCode()
		{
			return CompareInfo.GetCompareInfo(this.localeName).GetHashCodeOfString(this.m_String, this.options);
		}

		// Token: 0x06003169 RID: 12649 RVA: 0x000BDC60 File Offset: 0x000BBE60
		public override string ToString()
		{
			return string.Concat(new string[]
			{
				"SortKey - ",
				this.localeName,
				", ",
				this.options.ToString(),
				", ",
				this.m_String
			});
		}

		// Token: 0x0400151A RID: 5402
		[OptionalField(VersionAdded = 3)]
		internal string localeName;

		// Token: 0x0400151B RID: 5403
		[OptionalField(VersionAdded = 1)]
		internal int win32LCID;

		// Token: 0x0400151C RID: 5404
		internal CompareOptions options;

		// Token: 0x0400151D RID: 5405
		internal string m_String;

		// Token: 0x0400151E RID: 5406
		internal byte[] m_KeyData;
	}
}
