using System;

namespace Newtonsoft.Json.Bson
{
	// Token: 0x0200010D RID: 269
	internal class BsonValue : BsonToken
	{
		// Token: 0x06000DB0 RID: 3504 RVA: 0x000370E2 File Offset: 0x000352E2
		public BsonValue(object value, BsonType type)
		{
			this._value = value;
			this._type = type;
		}

		// Token: 0x1700027D RID: 637
		// (get) Token: 0x06000DB1 RID: 3505 RVA: 0x000370F8 File Offset: 0x000352F8
		public object Value
		{
			get
			{
				return this._value;
			}
		}

		// Token: 0x1700027E RID: 638
		// (get) Token: 0x06000DB2 RID: 3506 RVA: 0x00037100 File Offset: 0x00035300
		public override BsonType Type
		{
			get
			{
				return this._type;
			}
		}

		// Token: 0x04000441 RID: 1089
		private readonly object _value;

		// Token: 0x04000442 RID: 1090
		private readonly BsonType _type;
	}
}
