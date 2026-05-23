using System;

namespace Newtonsoft.Json.Bson
{
	// Token: 0x02000110 RID: 272
	internal class BsonBinary : BsonValue
	{
		// Token: 0x17000281 RID: 641
		// (get) Token: 0x06000DB9 RID: 3513 RVA: 0x00037159 File Offset: 0x00035359
		// (set) Token: 0x06000DBA RID: 3514 RVA: 0x00037161 File Offset: 0x00035361
		public BsonBinaryType BinaryType { get; set; }

		// Token: 0x06000DBB RID: 3515 RVA: 0x0003716A File Offset: 0x0003536A
		public BsonBinary(byte[] value, BsonBinaryType binaryType)
			: base(value, BsonType.Binary)
		{
			this.BinaryType = binaryType;
		}
	}
}
