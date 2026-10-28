namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class AnyNotableTypeTag : ConversationTag
{
	public const string Id = "AnyNotableTypeTag";

	public override string StringId => "AnyNotableTypeTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.HeroObject.IsNotable;
		}
		return false;
	}
}
