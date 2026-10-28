namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class HighRegisterTag : ConversationTag
{
	public const string Id = "HighRegisterTag";

	public override string StringId => "HighRegisterTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return ConversationTagHelper.UsesHighRegister(character);
		}
		return false;
	}
}
