using System;
using System.IO;
using System.Resources;

namespace System.Reflection.Emit
{
	// Token: 0x0200062C RID: 1580
	internal class ResWriterData
	{
		// Token: 0x0600498C RID: 18828 RVA: 0x0010A929 File Offset: 0x00108B29
		internal ResWriterData(ResourceWriter resWriter, Stream memoryStream, string strName, string strFileName, string strFullFileName, ResourceAttributes attribute)
		{
			this.m_resWriter = resWriter;
			this.m_memoryStream = memoryStream;
			this.m_strName = strName;
			this.m_strFileName = strFileName;
			this.m_strFullFileName = strFullFileName;
			this.m_nextResWriter = null;
			this.m_attribute = attribute;
		}

		// Token: 0x04001E6D RID: 7789
		internal ResourceWriter m_resWriter;

		// Token: 0x04001E6E RID: 7790
		internal string m_strName;

		// Token: 0x04001E6F RID: 7791
		internal string m_strFileName;

		// Token: 0x04001E70 RID: 7792
		internal string m_strFullFileName;

		// Token: 0x04001E71 RID: 7793
		internal Stream m_memoryStream;

		// Token: 0x04001E72 RID: 7794
		internal ResWriterData m_nextResWriter;

		// Token: 0x04001E73 RID: 7795
		internal ResourceAttributes m_attribute;
	}
}
