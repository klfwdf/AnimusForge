namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class TribalRegisterTag : ConversationTag
{
	public const string Id = "TribalRegisterTag";

	public override string StringId => "TribalRegisterTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (!ConversationTagHelper.UsesHighRegister(character))
		{
			return !ConversationTagHelper.UsesLowRegister(character);
		}
		return false;
	}
}
