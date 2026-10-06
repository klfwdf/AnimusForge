using System.Linq;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PlayerIsBrotherTag : ConversationTag
{
	public const string Id = "PlayerIsBrotherTag";

	public override string StringId => "PlayerIsBrotherTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (!Hero.MainHero.IsFemale && character.IsHero)
		{
			return character.HeroObject.Siblings.Contains(Hero.MainHero);
		}
		return false;
	}
}
