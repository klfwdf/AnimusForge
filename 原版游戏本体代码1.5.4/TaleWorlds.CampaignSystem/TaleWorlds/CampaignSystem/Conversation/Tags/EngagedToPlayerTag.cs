namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class EngagedToPlayerTag : ConversationTag
{
	public const string Id = "EngagedToPlayerTag";

	public override string StringId => "EngagedToPlayerTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return Romance.GetRomanticLevel(character.HeroObject, Hero.MainHero) == Romance.RomanceLevelEnum.CoupleAgreedOnMarriage;
		}
		return false;
	}
}
