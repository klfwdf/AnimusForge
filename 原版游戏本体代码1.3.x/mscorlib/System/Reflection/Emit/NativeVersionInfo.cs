using System;

namespace System.Reflection.Emit
{
	// Token: 0x0200062D RID: 1581
	internal class NativeVersionInfo
	{
		// Token: 0x0600498D RID: 18829 RVA: 0x0010A968 File Offset: 0x00108B68
		internal NativeVersionInfo()
		{
			this.m_strDescription = null;
			this.m_strCompany = null;
			this.m_strTitle = null;
			this.m_strCopyright = null;
			this.m_strTrademark = null;
			this.m_strProduct = null;
			this.m_strProductVersion = null;
			this.m_strFileVersion = null;
			this.m_lcid = -1;
		}

		// Token: 0x04001E74 RID: 7796
		internal string m_strDescription;

		// Token: 0x04001E75 RID: 7797
		internal string m_strCompany;

		// Token: 0x04001E76 RID: 7798
		internal string m_strTitle;

		// Token: 0x04001E77 RID: 7799
		internal string m_strCopyright;

		// Token: 0x04001E78 RID: 7800
		internal string m_strTrademark;

		// Token: 0x04001E79 RID: 7801
		internal string m_strProduct;

		// Token: 0x04001E7A RID: 7802
		internal string m_strProductVersion;

		// Token: 0x04001E7B RID: 7803
		internal string m_strFileVersion;

		// Token: 0x04001E7C RID: 7804
		internal int m_lcid;
	}
}
