using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Converters
{
	// Token: 0x020000F6 RID: 246
	[NullableContext(2)]
	internal interface IXmlDeclaration : IXmlNode
	{
		// Token: 0x17000230 RID: 560
		// (get) Token: 0x06000CDA RID: 3290
		string Version { get; }

		// Token: 0x17000231 RID: 561
		// (get) Token: 0x06000CDB RID: 3291
		// (set) Token: 0x06000CDC RID: 3292
		string Encoding { get; set; }

		// Token: 0x17000232 RID: 562
		// (get) Token: 0x06000CDD RID: 3293
		// (set) Token: 0x06000CDE RID: 3294
		string Standalone { get; set; }
	}
}
