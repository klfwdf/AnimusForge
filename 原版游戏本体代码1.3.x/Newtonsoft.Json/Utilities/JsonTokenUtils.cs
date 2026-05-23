using System;

namespace Newtonsoft.Json.Utilities
{
	// Token: 0x02000060 RID: 96
	internal static class JsonTokenUtils
	{
		// Token: 0x06000562 RID: 1378 RVA: 0x0001773E File Offset: 0x0001593E
		internal static bool IsEndToken(JsonToken token)
		{
			return token - JsonToken.EndObject <= 2;
		}

		// Token: 0x06000563 RID: 1379 RVA: 0x0001774A File Offset: 0x0001594A
		internal static bool IsStartToken(JsonToken token)
		{
			return token - JsonToken.StartObject <= 2;
		}

		// Token: 0x06000564 RID: 1380 RVA: 0x00017755 File Offset: 0x00015955
		internal static bool IsPrimitiveToken(JsonToken token)
		{
			return token - JsonToken.Integer <= 5 || token - JsonToken.Date <= 1;
		}
	}
}
