using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000246 RID: 582
	public class PlayerIsAffiliatedTag : ConversationTag
	{
		// Token: 0x17000894 RID: 2196
		// (get) Token: 0x060022B8 RID: 8888 RVA: 0x0009844E File Offset: 0x0009664E
		public override string StringId
		{
			get
			{
				return "PlayerIsAffiliatedTag";
			}
		}

		// Token: 0x060022B9 RID: 8889 RVA: 0x00098455 File Offset: 0x00096655
		public override bool IsApplicableTo(CharacterObject character)
		{
			return Hero.MainHero.MapFaction.IsKingdomFaction;
		}

		// Token: 0x04000A52 RID: 2642
		public const string Id = "PlayerIsAffiliatedTag";
	}
}
