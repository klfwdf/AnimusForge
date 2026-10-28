using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class VoiceGroupPersonaIronicUpperTag : ConversationTag
{
	public const string Id = "VoiceGroupPersonaIronicUpperTag";

	public override string StringId => "VoiceGroupPersonaIronicUpperTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.GetPersona() == DefaultTraits.PersonaIronic)
		{
			return ConversationTagHelper.UsesHighRegister(character);
		}
		return false;
	}
}
