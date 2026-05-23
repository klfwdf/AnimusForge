using System;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000023 RID: 35
	[Serializable]
	public class SteamAccessObject : AccessObject
	{
		// Token: 0x1700002B RID: 43
		// (get) Token: 0x060000BC RID: 188 RVA: 0x000033E9 File Offset: 0x000015E9
		// (set) Token: 0x060000BD RID: 189 RVA: 0x000033F1 File Offset: 0x000015F1
		[JsonProperty]
		public string UserName { get; private set; }

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x060000BE RID: 190 RVA: 0x000033FA File Offset: 0x000015FA
		// (set) Token: 0x060000BF RID: 191 RVA: 0x00003402 File Offset: 0x00001602
		[JsonProperty]
		public string ExternalAccessToken { get; private set; }

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x060000C0 RID: 192 RVA: 0x0000340B File Offset: 0x0000160B
		// (set) Token: 0x060000C1 RID: 193 RVA: 0x00003413 File Offset: 0x00001613
		[JsonProperty]
		public int AppId { get; private set; }

		// Token: 0x060000C2 RID: 194 RVA: 0x0000341C File Offset: 0x0000161C
		public SteamAccessObject()
		{
		}

		// Token: 0x060000C3 RID: 195 RVA: 0x00003424 File Offset: 0x00001624
		public SteamAccessObject(string userName, string externalAccessToken, int appId)
		{
			base.Type = "Steam";
			this.UserName = userName;
			this.ExternalAccessToken = externalAccessToken;
			this.AppId = appId;
		}
	}
}
