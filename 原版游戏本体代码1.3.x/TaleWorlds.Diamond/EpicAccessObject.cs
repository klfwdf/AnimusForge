using System;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000007 RID: 7
	[Serializable]
	public class EpicAccessObject : AccessObject
	{
		// Token: 0x06000027 RID: 39 RVA: 0x000026AA File Offset: 0x000008AA
		public EpicAccessObject()
		{
			base.Type = "Epic";
		}

		// Token: 0x0400000A RID: 10
		[JsonProperty]
		public string AccessToken;

		// Token: 0x0400000B RID: 11
		[JsonProperty]
		public string EpicId;
	}
}
