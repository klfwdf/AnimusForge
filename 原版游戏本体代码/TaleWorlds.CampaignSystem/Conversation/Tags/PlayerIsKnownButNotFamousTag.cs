using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000250 RID: 592
	public class PlayerIsKnownButNotFamousTag : ConversationTag
	{
		// Token: 0x1700089E RID: 2206
		// (get) Token: 0x060022D6 RID: 8918 RVA: 0x000986A3 File Offset: 0x000968A3
		public override string StringId
		{
			get
			{
				return "PlayerIsKnownButNotFamousTag";
			}
		}

		// Token: 0x060022D7 RID: 8919 RVA: 0x000986AC File Offset: 0x000968AC
		public override bool IsApplicableTo(CharacterObject character)
		{
			int baseRelation = Campaign.Current.Models.DiplomacyModel.GetBaseRelation(Hero.MainHero, Hero.OneToOneConversationHero);
			if (Hero.OneToOneConversationHero.Clan != null && baseRelation == 0)
			{
				baseRelation = Campaign.Current.Models.DiplomacyModel.GetBaseRelation(Hero.MainHero, Hero.OneToOneConversationHero.Clan.Leader);
			}
			return baseRelation != 0 && Clan.PlayerClan.Renown < 50f && Campaign.Current.ConversationManager.CurrentConversationIsFirst;
		}

		// Token: 0x04000A5C RID: 2652
		public const string Id = "PlayerIsKnownButNotFamousTag";
	}
}
