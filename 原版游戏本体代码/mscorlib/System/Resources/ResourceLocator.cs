using System;

namespace System.Resources
{
	// Token: 0x02000398 RID: 920
	internal struct ResourceLocator
	{
		// Token: 0x06002D44 RID: 11588 RVA: 0x000ABBC7 File Offset: 0x000A9DC7
		internal ResourceLocator(int dataPos, object value)
		{
			this._dataPos = dataPos;
			this._value = value;
		}

		// Token: 0x170005ED RID: 1517
		// (get) Token: 0x06002D45 RID: 11589 RVA: 0x000ABBD7 File Offset: 0x000A9DD7
		internal int DataPosition
		{
			get
			{
				return this._dataPos;
			}
		}

		// Token: 0x170005EE RID: 1518
		// (get) Token: 0x06002D46 RID: 11590 RVA: 0x000ABBDF File Offset: 0x000A9DDF
		// (set) Token: 0x06002D47 RID: 11591 RVA: 0x000ABBE7 File Offset: 0x000A9DE7
		internal object Value
		{
			get
			{
				return this._value;
			}
			set
			{
				this._value = value;
			}
		}

		// Token: 0x06002D48 RID: 11592 RVA: 0x000ABBF0 File Offset: 0x000A9DF0
		internal static bool CanCache(ResourceTypeCode value)
		{
			return value <= ResourceTypeCode.TimeSpan;
		}

		// Token: 0x04001258 RID: 4696
		internal object _value;

		// Token: 0x04001259 RID: 4697
		internal int _dataPos;
	}
}
