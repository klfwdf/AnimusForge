using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000275 RID: 629
	public class EmpireTag : ConversationTag
	{
		// Token: 0x170008C3 RID: 2243
		// (get) Token: 0x06002345 RID: 9029 RVA: 0x00098FEF File Offset: 0x000971EF
		public override string StringId
		{
			get
			{
				return "EmpireTag";
			}
		}

		// Token: 0x06002346 RID: 9030 RVA: 0x00098FF6 File Offset: 0x000971F6
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.Culture.StringId == "empire";
		}

		// Token: 0x04000A82 RID: 2690
		public const string Id = "EmpireTag";
	}
}
