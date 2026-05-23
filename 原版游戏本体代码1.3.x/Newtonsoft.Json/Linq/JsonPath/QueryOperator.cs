using System;

namespace Newtonsoft.Json.Linq.JsonPath
{
	// Token: 0x020000D7 RID: 215
	internal enum QueryOperator
	{
		// Token: 0x040003D7 RID: 983
		None,
		// Token: 0x040003D8 RID: 984
		Equals,
		// Token: 0x040003D9 RID: 985
		NotEquals,
		// Token: 0x040003DA RID: 986
		Exists,
		// Token: 0x040003DB RID: 987
		LessThan,
		// Token: 0x040003DC RID: 988
		LessThanOrEquals,
		// Token: 0x040003DD RID: 989
		GreaterThan,
		// Token: 0x040003DE RID: 990
		GreaterThanOrEquals,
		// Token: 0x040003DF RID: 991
		And,
		// Token: 0x040003E0 RID: 992
		Or,
		// Token: 0x040003E1 RID: 993
		RegexEquals,
		// Token: 0x040003E2 RID: 994
		StrictEquals,
		// Token: 0x040003E3 RID: 995
		StrictNotEquals
	}
}
