using System;

namespace Newtonsoft.Json.Bson
{
	// Token: 0x02000109 RID: 265
	internal abstract class BsonToken
	{
		// Token: 0x17000277 RID: 631
		// (get) Token: 0x06000D9D RID: 3485
		public abstract BsonType Type { get; }

		// Token: 0x17000278 RID: 632
		// (get) Token: 0x06000D9E RID: 3486 RVA: 0x00036FE6 File Offset: 0x000351E6
		// (set) Token: 0x06000D9F RID: 3487 RVA: 0x00036FEE File Offset: 0x000351EE
		public BsonToken Parent { get; set; }

		// Token: 0x17000279 RID: 633
		// (get) Token: 0x06000DA0 RID: 3488 RVA: 0x00036FF7 File Offset: 0x000351F7
		// (set) Token: 0x06000DA1 RID: 3489 RVA: 0x00036FFF File Offset: 0x000351FF
		public int CalculatedSize { get; set; }
	}
}
