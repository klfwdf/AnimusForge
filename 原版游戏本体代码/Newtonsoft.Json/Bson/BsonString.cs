using System;

namespace Newtonsoft.Json.Bson
{
	// Token: 0x0200010F RID: 271
	internal class BsonString : BsonValue
	{
		// Token: 0x1700027F RID: 639
		// (get) Token: 0x06000DB5 RID: 3509 RVA: 0x0003712F File Offset: 0x0003532F
		// (set) Token: 0x06000DB6 RID: 3510 RVA: 0x00037137 File Offset: 0x00035337
		public int ByteCount { get; set; }

		// Token: 0x17000280 RID: 640
		// (get) Token: 0x06000DB7 RID: 3511 RVA: 0x00037140 File Offset: 0x00035340
		public bool IncludeLength { get; }

		// Token: 0x06000DB8 RID: 3512 RVA: 0x00037148 File Offset: 0x00035348
		public BsonString(object value, bool includeLength)
			: base(value, BsonType.String)
		{
			this.IncludeLength = includeLength;
		}
	}
}
