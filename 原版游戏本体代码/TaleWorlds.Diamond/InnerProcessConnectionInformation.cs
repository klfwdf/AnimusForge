using System;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000011 RID: 17
	public sealed class InnerProcessConnectionInformation : IConnectionInformation
	{
		// Token: 0x06000055 RID: 85 RVA: 0x00002929 File Offset: 0x00000B29
		string IConnectionInformation.GetAddress(bool isIpv6Compatible)
		{
			return "InnerProcess";
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00002930 File Offset: 0x00000B30
		string IConnectionInformation.GetCountry()
		{
			return "TR";
		}
	}
}
