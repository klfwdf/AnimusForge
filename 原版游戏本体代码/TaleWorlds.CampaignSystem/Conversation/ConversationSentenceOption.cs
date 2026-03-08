using System;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Conversation
{
	// Token: 0x02000236 RID: 566
	public struct ConversationSentenceOption
	{
		// Token: 0x04000A1F RID: 2591
		public int SentenceNo;

		// Token: 0x04000A20 RID: 2592
		public string Id;

		// Token: 0x04000A21 RID: 2593
		public object RepeatObject;

		// Token: 0x04000A22 RID: 2594
		public TextObject Text;

		// Token: 0x04000A23 RID: 2595
		public string DebugInfo;

		// Token: 0x04000A24 RID: 2596
		public bool IsClickable;

		// Token: 0x04000A25 RID: 2597
		public bool HasPersuasion;

		// Token: 0x04000A26 RID: 2598
		public string SkillName;

		// Token: 0x04000A27 RID: 2599
		public string TraitName;

		// Token: 0x04000A28 RID: 2600
		public bool IsSpecial;

		// Token: 0x04000A29 RID: 2601
		public bool IsUsedOnce;

		// Token: 0x04000A2A RID: 2602
		public TextObject HintText;

		// Token: 0x04000A2B RID: 2603
		public PersuasionOptionArgs PersuationOptionArgs;
	}
}
