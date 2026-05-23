using System;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000020 RID: 32
	[Serializable]
	public class PSAccessObject : AccessObject
	{
		// Token: 0x17000025 RID: 37
		// (get) Token: 0x060000A6 RID: 166 RVA: 0x0000326B File Offset: 0x0000146B
		// (set) Token: 0x060000A7 RID: 167 RVA: 0x00003273 File Offset: 0x00001473
		[JsonProperty]
		public int IssuerId { get; private set; }

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x060000A8 RID: 168 RVA: 0x0000327C File Offset: 0x0000147C
		// (set) Token: 0x060000A9 RID: 169 RVA: 0x00003284 File Offset: 0x00001484
		[JsonProperty]
		public string AuthCode { get; private set; }

		// Token: 0x060000AA RID: 170 RVA: 0x0000328D File Offset: 0x0000148D
		public PSAccessObject()
		{
		}

		// Token: 0x060000AB RID: 171 RVA: 0x00003295 File Offset: 0x00001495
		public PSAccessObject(int issuerId, string authCode)
		{
			base.Type = "PS";
			this.IssuerId = issuerId;
			this.AuthCode = authCode;
		}
	}
}
