using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200024F RID: 591
	public class PlayerIsFamousTag : ConversationTag
	{
		// Token: 0x1700089D RID: 2205
		// (get) Token: 0x060022D3 RID: 8915 RVA: 0x0009867E File Offset: 0x0009687E
		public override string StringId
		{
			get
			{
				return "PlayerIsFamousTag";
			}
		}

		// Token: 0x060022D4 RID: 8916 RVA: 0x00098685 File Offset: 0x00096885
		public override bool IsApplicableTo(CharacterObject character)
		{
			return Clan.PlayerClan.Renown >= 50f;
		}

		// Token: 0x04000A5B RID: 2651
		public const string Id = "PlayerIsFamousTag";
	}
}
