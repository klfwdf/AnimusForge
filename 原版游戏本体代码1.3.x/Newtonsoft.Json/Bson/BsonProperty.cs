using System;

namespace Newtonsoft.Json.Bson
{
	// Token: 0x02000112 RID: 274
	internal class BsonProperty
	{
		// Token: 0x17000285 RID: 645
		// (get) Token: 0x06000DC2 RID: 3522 RVA: 0x000371C3 File Offset: 0x000353C3
		// (set) Token: 0x06000DC3 RID: 3523 RVA: 0x000371CB File Offset: 0x000353CB
		public BsonString Name { get; set; }

		// Token: 0x17000286 RID: 646
		// (get) Token: 0x06000DC4 RID: 3524 RVA: 0x000371D4 File Offset: 0x000353D4
		// (set) Token: 0x06000DC5 RID: 3525 RVA: 0x000371DC File Offset: 0x000353DC
		public BsonToken Value { get; set; }
	}
}
