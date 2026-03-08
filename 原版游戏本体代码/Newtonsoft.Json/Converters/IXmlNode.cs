using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml;

namespace Newtonsoft.Json.Converters
{
	// Token: 0x020000F9 RID: 249
	[NullableContext(2)]
	internal interface IXmlNode
	{
		// Token: 0x17000238 RID: 568
		// (get) Token: 0x06000CE6 RID: 3302
		XmlNodeType NodeType { get; }

		// Token: 0x17000239 RID: 569
		// (get) Token: 0x06000CE7 RID: 3303
		string LocalName { get; }

		// Token: 0x1700023A RID: 570
		// (get) Token: 0x06000CE8 RID: 3304
		[Nullable(1)]
		List<IXmlNode> ChildNodes
		{
			[NullableContext(1)]
			get;
		}

		// Token: 0x1700023B RID: 571
		// (get) Token: 0x06000CE9 RID: 3305
		[Nullable(1)]
		List<IXmlNode> Attributes
		{
			[NullableContext(1)]
			get;
		}

		// Token: 0x1700023C RID: 572
		// (get) Token: 0x06000CEA RID: 3306
		IXmlNode ParentNode { get; }

		// Token: 0x1700023D RID: 573
		// (get) Token: 0x06000CEB RID: 3307
		// (set) Token: 0x06000CEC RID: 3308
		string Value { get; set; }

		// Token: 0x06000CED RID: 3309
		[NullableContext(1)]
		IXmlNode AppendChild(IXmlNode newChild);

		// Token: 0x1700023E RID: 574
		// (get) Token: 0x06000CEE RID: 3310
		string NamespaceUri { get; }

		// Token: 0x1700023F RID: 575
		// (get) Token: 0x06000CEF RID: 3311
		object WrappedNode { get; }
	}
}
