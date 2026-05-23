using System;

namespace System.Globalization
{
	// Token: 0x020003B0 RID: 944
	internal class TokenHashValue
	{
		// Token: 0x06002F30 RID: 12080 RVA: 0x000B51CA File Offset: 0x000B33CA
		internal TokenHashValue(string tokenString, TokenType tokenType, int tokenValue)
		{
			this.tokenString = tokenString;
			this.tokenType = tokenType;
			this.tokenValue = tokenValue;
		}

		// Token: 0x040013D0 RID: 5072
		internal string tokenString;

		// Token: 0x040013D1 RID: 5073
		internal TokenType tokenType;

		// Token: 0x040013D2 RID: 5074
		internal int tokenValue;
	}
}
