namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class LowRegisterTag : ConversationTag
{
	public const string Id = "LowRegisterTag";

	public override string StringId => "LowRegisterTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero && !ConversationTagHelper.UsesHighRegister(character))
		{
			return ConversationTagHelper.UsesLowRegister(character);
		}
		return false;
	}
}
