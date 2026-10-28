using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class VoiceGroupPersonaIronicTribalTag : ConversationTag
{
	public const string Id = "VoiceGroupPersonaIronicTribalTag";

	public override string StringId => "VoiceGroupPersonaIronicTribalTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.GetPersona() == DefaultTraits.PersonaIronic)
		{
			return ConversationTagHelper.TribalVoiceGroup(character);
		}
		return false;
	}
}
