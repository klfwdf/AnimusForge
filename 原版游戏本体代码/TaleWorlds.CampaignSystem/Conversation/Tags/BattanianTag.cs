using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000276 RID: 630
	public class BattanianTag : ConversationTag
	{
		// Token: 0x170008C4 RID: 2244
		// (get) Token: 0x06002348 RID: 9032 RVA: 0x00099015 File Offset: 0x00097215
		public override string StringId
		{
			get
			{
				return "BattanianTag";
			}
		}

		// Token: 0x06002349 RID: 9033 RVA: 0x0009901C File Offset: 0x0009721C
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.Culture.StringId == "battania";
		}

		// Token: 0x04000A83 RID: 2691
		public const string Id = "BattanianTag";
	}
}
