namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class WandererTag : ConversationTag
{
	public const string Id = "WandererTag";

	public override string StringId => "WandererTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.HeroObject.IsWanderer;
		}
		return false;
	}
}
