using System;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond
{
	// Token: 0x0200000A RID: 10
	[Serializable]
	public class GDKAccessObject : AccessObject
	{
		// Token: 0x06000030 RID: 48 RVA: 0x0000283F File Offset: 0x00000A3F
		public GDKAccessObject()
		{
			base.Type = "GDK";
		}

		// Token: 0x0400000D RID: 13
		[JsonProperty]
		public string Id;

		// Token: 0x0400000E RID: 14
		[JsonProperty]
		public string Token;
	}
}
