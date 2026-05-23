namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PlayerIsMotherTag : ConversationTag
{
	public const string Id = "PlayerIsMotherTag";

	public override string StringId => "PlayerIsMotherTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.HeroObject.Mother == Hero.MainHero;
		}
		return false;
	}
}
