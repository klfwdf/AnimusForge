namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PlayerIsSonTag : ConversationTag
{
	public const string Id = "PlayerIsSonTag";

	public override string StringId => "PlayerIsSonTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero && !Hero.MainHero.IsFemale)
		{
			if (Hero.MainHero.Father != character.HeroObject)
			{
				return Hero.MainHero.Mother == character.HeroObject;
			}
			return true;
		}
		return false;
	}
}
