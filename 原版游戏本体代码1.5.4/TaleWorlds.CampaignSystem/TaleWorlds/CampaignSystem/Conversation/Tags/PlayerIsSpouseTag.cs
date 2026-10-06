namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PlayerIsSpouseTag : ConversationTag
{
	public const string Id = "PlayerIsSpouseTag";

	public override string StringId => "PlayerIsSpouseTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return Hero.MainHero.Spouse == character.HeroObject;
		}
		return false;
	}
}
