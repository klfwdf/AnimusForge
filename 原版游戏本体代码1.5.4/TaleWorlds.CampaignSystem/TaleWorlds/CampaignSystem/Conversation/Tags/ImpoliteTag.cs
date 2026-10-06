using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class ImpoliteTag : ConversationTag
{
	public const string Id = "ImpoliteTag";

	public override string StringId => "ImpoliteTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (!character.IsHero)
		{
			return false;
		}
		int heroRelation = CharacterRelationManager.GetHeroRelation(character.HeroObject, Hero.MainHero);
		if ((character.HeroObject.IsLord || character.HeroObject.IsMerchant || character.HeroObject.IsGangLeader) && Clan.PlayerClan.Renown < 100f && heroRelation < 1)
		{
			return character.GetTraitLevel(DefaultTraits.Mercy) + character.GetTraitLevel(DefaultTraits.Generosity) < 0;
		}
		return false;
	}
}
