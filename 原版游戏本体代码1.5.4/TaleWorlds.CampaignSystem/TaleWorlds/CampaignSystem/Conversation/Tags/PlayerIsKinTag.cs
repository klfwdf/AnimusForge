using System.Linq;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PlayerIsKinTag : ConversationTag
{
	public const string Id = "PlayerIsKinTag";

	public override string StringId => "PlayerIsKinTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			if (!character.HeroObject.Siblings.Contains(Hero.MainHero) && character.HeroObject.Mother != Hero.MainHero && character.HeroObject.Father != Hero.MainHero)
			{
				return character.HeroObject.Spouse == Hero.MainHero;
			}
			return true;
		}
		return false;
	}
}
