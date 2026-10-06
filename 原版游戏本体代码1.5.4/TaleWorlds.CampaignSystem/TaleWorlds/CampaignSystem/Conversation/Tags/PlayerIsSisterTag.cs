using System.Linq;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PlayerIsSisterTag : ConversationTag
{
	public const string Id = "PlayerIsSisterTag";

	public override string StringId => "PlayerIsSisterTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (Hero.MainHero.IsFemale && character.IsHero)
		{
			return character.HeroObject.Siblings.Contains(Hero.MainHero);
		}
		return false;
	}
}
