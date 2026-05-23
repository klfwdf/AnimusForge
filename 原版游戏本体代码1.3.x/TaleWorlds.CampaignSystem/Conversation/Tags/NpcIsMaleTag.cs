using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000254 RID: 596
	public class NpcIsMaleTag : ConversationTag
	{
		// Token: 0x170008A2 RID: 2210
		// (get) Token: 0x060022E2 RID: 8930 RVA: 0x000987B5 File Offset: 0x000969B5
		public override string StringId
		{
			get
			{
				return "NpcIsMaleTag";
			}
		}

		// Token: 0x060022E3 RID: 8931 RVA: 0x000987BC File Offset: 0x000969BC
		public override bool IsApplicableTo(CharacterObject character)
		{
			return !character.IsFemale;
		}

		// Token: 0x04000A60 RID: 2656
		public const string Id = "NpcIsMaleTag";
	}
}
