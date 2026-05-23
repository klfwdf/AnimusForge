using System;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond
{
	// Token: 0x0200000B RID: 11
	[Serializable]
	public class GOGAccessObject : AccessObject
	{
		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000031 RID: 49 RVA: 0x00002852 File Offset: 0x00000A52
		// (set) Token: 0x06000032 RID: 50 RVA: 0x0000285A File Offset: 0x00000A5A
		[JsonProperty]
		public ulong GogId { get; set; }

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000033 RID: 51 RVA: 0x00002863 File Offset: 0x00000A63
		// (set) Token: 0x06000034 RID: 52 RVA: 0x0000286B File Offset: 0x00000A6B
		[JsonProperty]
		public ulong OldId { get; set; }

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000035 RID: 53 RVA: 0x00002874 File Offset: 0x00000A74
		// (set) Token: 0x06000036 RID: 54 RVA: 0x0000287C File Offset: 0x00000A7C
		[JsonProperty]
		public string UserName { get; set; }

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000037 RID: 55 RVA: 0x00002885 File Offset: 0x00000A85
		// (set) Token: 0x06000038 RID: 56 RVA: 0x0000288D File Offset: 0x00000A8D
		[JsonProperty]
		public string Ticket { get; set; }

		// Token: 0x06000039 RID: 57 RVA: 0x00002896 File Offset: 0x00000A96
		public GOGAccessObject()
		{
		}

		// Token: 0x0600003A RID: 58 RVA: 0x0000289E File Offset: 0x00000A9E
		public GOGAccessObject(string userName, ulong gogId, ulong oldId, string ticket)
		{
			base.Type = "GOG";
			this.UserName = userName;
			this.GogId = gogId;
			this.Ticket = ticket;
			this.OldId = oldId;
		}
	}
}
