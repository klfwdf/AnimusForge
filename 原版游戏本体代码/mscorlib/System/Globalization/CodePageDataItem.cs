using System;
using System.Security;

namespace System.Globalization
{
	// Token: 0x020003B7 RID: 951
	[Serializable]
	internal class CodePageDataItem
	{
		// Token: 0x06002F4A RID: 12106 RVA: 0x000B5A50 File Offset: 0x000B3C50
		[SecurityCritical]
		internal unsafe CodePageDataItem(int dataIndex)
		{
			this.m_dataIndex = dataIndex;
			this.m_uiFamilyCodePage = (int)EncodingTable.codePageDataPtr[dataIndex].uiFamilyCodePage;
			this.m_flags = EncodingTable.codePageDataPtr[dataIndex].flags;
		}

		// Token: 0x06002F4B RID: 12107 RVA: 0x000B5AA0 File Offset: 0x000B3CA0
		[SecurityCritical]
		internal unsafe static string CreateString(sbyte* pStrings, uint index)
		{
			if (*pStrings == 124)
			{
				int num = 1;
				int num2 = 1;
				for (;;)
				{
					sbyte b = pStrings[num2];
					if (b == 124 || b == 0)
					{
						if (index == 0U)
						{
							break;
						}
						index -= 1U;
						num = num2 + 1;
						if (b == 0)
						{
							goto IL_37;
						}
					}
					num2++;
				}
				return new string(pStrings, num, num2 - num);
				IL_37:
				throw new ArgumentException("pStrings");
			}
			return new string(pStrings);
		}

		// Token: 0x17000667 RID: 1639
		// (get) Token: 0x06002F4C RID: 12108 RVA: 0x000B5AF5 File Offset: 0x000B3CF5
		public unsafe string WebName
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_webName == null)
				{
					this.m_webName = CodePageDataItem.CreateString(EncodingTable.codePageDataPtr[this.m_dataIndex].Names, 0U);
				}
				return this.m_webName;
			}
		}

		// Token: 0x17000668 RID: 1640
		// (get) Token: 0x06002F4D RID: 12109 RVA: 0x000B5B2A File Offset: 0x000B3D2A
		public virtual int UIFamilyCodePage
		{
			get
			{
				return this.m_uiFamilyCodePage;
			}
		}

		// Token: 0x17000669 RID: 1641
		// (get) Token: 0x06002F4E RID: 12110 RVA: 0x000B5B32 File Offset: 0x000B3D32
		public unsafe string HeaderName
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_headerName == null)
				{
					this.m_headerName = CodePageDataItem.CreateString(EncodingTable.codePageDataPtr[this.m_dataIndex].Names, 1U);
				}
				return this.m_headerName;
			}
		}

		// Token: 0x1700066A RID: 1642
		// (get) Token: 0x06002F4F RID: 12111 RVA: 0x000B5B67 File Offset: 0x000B3D67
		public unsafe string BodyName
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_bodyName == null)
				{
					this.m_bodyName = CodePageDataItem.CreateString(EncodingTable.codePageDataPtr[this.m_dataIndex].Names, 2U);
				}
				return this.m_bodyName;
			}
		}

		// Token: 0x1700066B RID: 1643
		// (get) Token: 0x06002F50 RID: 12112 RVA: 0x000B5B9C File Offset: 0x000B3D9C
		public uint Flags
		{
			get
			{
				return this.m_flags;
			}
		}

		// Token: 0x04001410 RID: 5136
		internal int m_dataIndex;

		// Token: 0x04001411 RID: 5137
		internal int m_uiFamilyCodePage;

		// Token: 0x04001412 RID: 5138
		internal string m_webName;

		// Token: 0x04001413 RID: 5139
		internal string m_headerName;

		// Token: 0x04001414 RID: 5140
		internal string m_bodyName;

		// Token: 0x04001415 RID: 5141
		internal uint m_flags;
	}
}
