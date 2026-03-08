using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Converters
{
	// Token: 0x020000F8 RID: 248
	[NullableContext(1)]
	internal interface IXmlElement : IXmlNode
	{
		// Token: 0x06000CE3 RID: 3299
		void SetAttributeNode(IXmlNode attribute);

		// Token: 0x06000CE4 RID: 3300
		[return: Nullable(2)]
		string GetPrefixOfNamespace(string namespaceUri);

		// Token: 0x17000237 RID: 567
		// (get) Token: 0x06000CE5 RID: 3301
		bool IsEmpty { get; }
	}
}
