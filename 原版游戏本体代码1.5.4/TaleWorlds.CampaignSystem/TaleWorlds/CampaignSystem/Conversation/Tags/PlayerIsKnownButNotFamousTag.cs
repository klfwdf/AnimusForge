namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PlayerIsKnownButNotFamousTag : ConversationTag
{
	public const string Id = "PlayerIsKnownButNotFamousTag";

	public override string StringId => "PlayerIsKnownButNotFamousTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		int baseRelation = Campaign.Current.Models.DiplomacyModel.GetBaseRelation(Hero.MainHero, Hero.OneToOneConversationHero);
		if (Hero.OneToOneConversationHero.Clan != null && baseRelation == 0)
		{
			baseRelation = Campaign.Current.Models.DiplomacyModel.GetBaseRelation(Hero.MainHero, Hero.OneToOneConversationHero.Clan.Leader);
		}
		if (baseRelation != 0 && Clan.PlayerClan.Renown < 50f)
		{
			return Campaign.Current.ConversationManager.CurrentConversationIsFirst;
		}
		return false;
	}
}
