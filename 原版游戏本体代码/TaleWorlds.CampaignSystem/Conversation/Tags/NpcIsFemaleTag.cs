using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000253 RID: 595
	public class NpcIsFemaleTag : ConversationTag
	{
		// Token: 0x170008A1 RID: 2209
		// (get) Token: 0x060022DF RID: 8927 RVA: 0x0009879E File Offset: 0x0009699E
		public override string StringId
		{
			get
			{
				return "NpcIsFemaleTag";
			}
		}

		// Token: 0x060022E0 RID: 8928 RVA: 0x000987A5 File Offset: 0x000969A5
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsFemale;
		}

		// Token: 0x04000A5F RID: 2655
		public const string Id = "NpcIsFemaleTag";
	}
}
