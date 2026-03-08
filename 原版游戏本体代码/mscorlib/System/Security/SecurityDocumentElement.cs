using System;

namespace System.Security
{
	// Token: 0x020001BE RID: 446
	[Serializable]
	internal sealed class SecurityDocumentElement : ISecurityElementFactory
	{
		// Token: 0x06001BFD RID: 7165 RVA: 0x00060761 File Offset: 0x0005E961
		internal SecurityDocumentElement(SecurityDocument document, int position)
		{
			this.m_document = document;
			this.m_position = position;
		}

		// Token: 0x06001BFE RID: 7166 RVA: 0x00060777 File Offset: 0x0005E977
		SecurityElement ISecurityElementFactory.CreateSecurityElement()
		{
			return this.m_document.GetElement(this.m_position, true);
		}

		// Token: 0x06001BFF RID: 7167 RVA: 0x0006078B File Offset: 0x0005E98B
		object ISecurityElementFactory.Copy()
		{
			return new SecurityDocumentElement(this.m_document, this.m_position);
		}

		// Token: 0x06001C00 RID: 7168 RVA: 0x0006079E File Offset: 0x0005E99E
		string ISecurityElementFactory.GetTag()
		{
			return this.m_document.GetTagForElement(this.m_position);
		}

		// Token: 0x06001C01 RID: 7169 RVA: 0x000607B1 File Offset: 0x0005E9B1
		string ISecurityElementFactory.Attribute(string attributeName)
		{
			return this.m_document.GetAttributeForElement(this.m_position, attributeName);
		}

		// Token: 0x040009B7 RID: 2487
		private int m_position;

		// Token: 0x040009B8 RID: 2488
		private SecurityDocument m_document;
	}
}
