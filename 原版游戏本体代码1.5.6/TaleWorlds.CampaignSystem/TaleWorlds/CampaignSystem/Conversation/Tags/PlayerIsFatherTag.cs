namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PlayerIsFatherTag : ConversationTag
{
	public const string Id = "PlayerIsFatherTag";

	public override string StringId => "PlayerIsFatherTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.HeroObject.Father == Hero.MainHero;
		}
		return false;
	}
}
