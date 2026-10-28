using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class VoiceGroupPersonaIronicLowerTag : ConversationTag
{
	public const string Id = "VoiceGroupPersonaIronicLowerTag";

	public override string StringId => "VoiceGroupPersonaIronicLowerTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.GetPersona() == DefaultTraits.PersonaIronic)
		{
			return ConversationTagHelper.UsesLowRegister(character);
		}
		return false;
	}
}
