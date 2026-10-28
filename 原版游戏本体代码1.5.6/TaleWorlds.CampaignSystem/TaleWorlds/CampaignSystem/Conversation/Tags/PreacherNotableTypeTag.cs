namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PreacherNotableTypeTag : ConversationTag
{
	public const string Id = "PreacherNotableTypeTag";

	public override string StringId => "PreacherNotableTypeTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.Occupation == Occupation.Preacher;
		}
		return false;
	}
}
