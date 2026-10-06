using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class VoiceGroupPersonaEarnestTribalTag : ConversationTag
{
	public const string Id = "VoiceGroupPersonaEarnestTribalTag";

	public override string StringId => "VoiceGroupPersonaEarnestTribalTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.GetPersona() == DefaultTraits.PersonaEarnest)
		{
			return ConversationTagHelper.TribalVoiceGroup(character);
		}
		return false;
	}
}
