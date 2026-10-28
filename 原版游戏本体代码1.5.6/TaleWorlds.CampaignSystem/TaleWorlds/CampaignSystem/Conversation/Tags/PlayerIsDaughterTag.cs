namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PlayerIsDaughterTag : ConversationTag
{
	public const string Id = "PlayerIsDaughterTag";

	public override string StringId => "PlayerIsDaughterTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero && Hero.MainHero.IsFemale)
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
