using System;

namespace Newtonsoft.Json.Bson
{
	// Token: 0x0200010E RID: 270
	internal class BsonBoolean : BsonValue
	{
		// Token: 0x06000DB3 RID: 3507 RVA: 0x00037108 File Offset: 0x00035308
		private BsonBoolean(bool value)
			: base(value, BsonType.Boolean)
		{
		}

		// Token: 0x04000443 RID: 1091
		public static readonly BsonBoolean False = new BsonBoolean(false);

		// Token: 0x04000444 RID: 1092
		public static readonly BsonBoolean True = new BsonBoolean(true);
	}
}
