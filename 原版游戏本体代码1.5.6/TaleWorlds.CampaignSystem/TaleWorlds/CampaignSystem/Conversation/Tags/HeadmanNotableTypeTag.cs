namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class HeadmanNotableTypeTag : ConversationTag
{
	public const string Id = "HeadmanNotableTypeTag";

	public override string StringId => "HeadmanNotableTypeTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.Occupation == Occupation.Headman;
		}
		return false;
	}
}
