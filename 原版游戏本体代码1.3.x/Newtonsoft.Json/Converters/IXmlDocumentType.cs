using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Converters
{
	// Token: 0x020000F7 RID: 247
	[NullableContext(2)]
	internal interface IXmlDocumentType : IXmlNode
	{
		// Token: 0x17000233 RID: 563
		// (get) Token: 0x06000CDF RID: 3295
		[Nullable(1)]
		string Name
		{
			[NullableContext(1)]
			get;
		}

		// Token: 0x17000234 RID: 564
		// (get) Token: 0x06000CE0 RID: 3296
		string System { get; }

		// Token: 0x17000235 RID: 565
		// (get) Token: 0x06000CE1 RID: 3297
		string Public { get; }

		// Token: 0x17000236 RID: 566
		// (get) Token: 0x06000CE2 RID: 3298
		string InternalSubset { get; }
	}
}
