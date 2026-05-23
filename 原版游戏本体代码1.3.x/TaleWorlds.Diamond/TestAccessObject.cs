using System;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000024 RID: 36
	[Serializable]
	public class TestAccessObject : AccessObject
	{
		// Token: 0x1700002E RID: 46
		// (get) Token: 0x060000C4 RID: 196 RVA: 0x0000344C File Offset: 0x0000164C
		// (set) Token: 0x060000C5 RID: 197 RVA: 0x00003454 File Offset: 0x00001654
		[JsonProperty]
		public string UserName { get; private set; }

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x060000C6 RID: 198 RVA: 0x0000345D File Offset: 0x0000165D
		// (set) Token: 0x060000C7 RID: 199 RVA: 0x00003465 File Offset: 0x00001665
		[JsonProperty]
		public string Password { get; private set; }

		// Token: 0x060000C8 RID: 200 RVA: 0x0000346E File Offset: 0x0000166E
		public TestAccessObject()
		{
		}

		// Token: 0x060000C9 RID: 201 RVA: 0x00003476 File Offset: 0x00001676
		public TestAccessObject(string userName, string password)
		{
			base.Type = "Test";
			this.UserName = userName;
			this.Password = password;
		}
	}
}
