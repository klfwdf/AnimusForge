using System;

namespace System.Text
{
	// Token: 0x02000A74 RID: 2676
	[Serializable]
	public sealed class EncodingInfo
	{
		// Token: 0x06006879 RID: 26745 RVA: 0x00160BC6 File Offset: 0x0015EDC6
		internal EncodingInfo(int codePage, string name, string displayName)
		{
			this.iCodePage = codePage;
			this.strEncodingName = name;
			this.strDisplayName = displayName;
		}

		// Token: 0x170011DC RID: 4572
		// (get) Token: 0x0600687A RID: 26746 RVA: 0x00160BE3 File Offset: 0x0015EDE3
		public int CodePage
		{
			get
			{
				return this.iCodePage;
			}
		}

		// Token: 0x170011DD RID: 4573
		// (get) Token: 0x0600687B RID: 26747 RVA: 0x00160BEB File Offset: 0x0015EDEB
		public string Name
		{
			get
			{
				return this.strEncodingName;
			}
		}

		// Token: 0x170011DE RID: 4574
		// (get) Token: 0x0600687C RID: 26748 RVA: 0x00160BF3 File Offset: 0x0015EDF3
		public string DisplayName
		{
			get
			{
				return this.strDisplayName;
			}
		}

		// Token: 0x0600687D RID: 26749 RVA: 0x00160BFB File Offset: 0x0015EDFB
		public Encoding GetEncoding()
		{
			return Encoding.GetEncoding(this.iCodePage);
		}

		// Token: 0x0600687E RID: 26750 RVA: 0x00160C08 File Offset: 0x0015EE08
		public override bool Equals(object value)
		{
			EncodingInfo encodingInfo = value as EncodingInfo;
			return encodingInfo != null && this.CodePage == encodingInfo.CodePage;
		}

		// Token: 0x0600687F RID: 26751 RVA: 0x00160C2F File Offset: 0x0015EE2F
		public override int GetHashCode()
		{
			return this.CodePage;
		}

		// Token: 0x04002EB6 RID: 11958
		private int iCodePage;

		// Token: 0x04002EB7 RID: 11959
		private string strEncodingName;

		// Token: 0x04002EB8 RID: 11960
		private string strDisplayName;
	}
}
