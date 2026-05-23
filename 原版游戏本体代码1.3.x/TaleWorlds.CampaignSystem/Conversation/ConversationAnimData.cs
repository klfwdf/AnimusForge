using System;
using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Conversation
{
	// Token: 0x0200022F RID: 559
	public class ConversationAnimData
	{
		// Token: 0x060021E4 RID: 8676 RVA: 0x00094C9D File Offset: 0x00092E9D
		public ConversationAnimData()
		{
			this.Reactions = new Dictionary<string, string>();
		}

		// Token: 0x040009DE RID: 2526
		[SaveableField(0)]
		public string IdleAnimStart;

		// Token: 0x040009DF RID: 2527
		[SaveableField(1)]
		public string IdleAnimLoop;

		// Token: 0x040009E0 RID: 2528
		[SaveableField(2)]
		public int FamilyType;

		// Token: 0x040009E1 RID: 2529
		[SaveableField(3)]
		public int MountFamilyType;

		// Token: 0x040009E2 RID: 2530
		[SaveableField(4)]
		public Dictionary<string, string> Reactions;
	}
}
