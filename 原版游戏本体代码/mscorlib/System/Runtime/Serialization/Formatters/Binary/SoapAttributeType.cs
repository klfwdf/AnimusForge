using System;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000778 RID: 1912
	[Serializable]
	internal enum SoapAttributeType
	{
		// Token: 0x04002576 RID: 9590
		None,
		// Token: 0x04002577 RID: 9591
		SchemaType,
		// Token: 0x04002578 RID: 9592
		Embedded,
		// Token: 0x04002579 RID: 9593
		XmlElement = 4,
		// Token: 0x0400257A RID: 9594
		XmlAttribute = 8
	}
}
