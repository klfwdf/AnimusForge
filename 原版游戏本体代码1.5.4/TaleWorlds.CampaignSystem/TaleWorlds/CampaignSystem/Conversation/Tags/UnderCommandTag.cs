using Helpers;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class UnderCommandTag : ConversationTag
{
	public const string Id = "UnderCommandTag";

	public override string StringId => "UnderCommandTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero && character.HeroObject.Spouse != Hero.MainHero)
		{
			return HeroHelper.UnderPlayerCommand(character.HeroObject);
		}
		return false;
	}
}
