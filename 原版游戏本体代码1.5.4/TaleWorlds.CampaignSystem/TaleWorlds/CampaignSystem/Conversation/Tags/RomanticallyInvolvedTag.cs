namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class RomanticallyInvolvedTag : ConversationTag
{
	public const string Id = "RomanticallyInvolvedTag";

	public override string StringId => "RomanticallyInvolvedTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return Romance.GetRomanticLevel(character.HeroObject, CharacterObject.PlayerCharacter.HeroObject) >= Romance.RomanceLevelEnum.CourtshipStarted;
		}
		return false;
	}
}
