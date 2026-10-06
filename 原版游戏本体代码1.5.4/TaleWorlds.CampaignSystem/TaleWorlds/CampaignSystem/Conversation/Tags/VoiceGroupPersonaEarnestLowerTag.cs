using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class VoiceGroupPersonaEarnestLowerTag : ConversationTag
{
	public const string Id = "VoiceGroupPersonaEarnestLowerTag";

	public override string StringId => "VoiceGroupPersonaEarnestLowerTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.GetPersona() == DefaultTraits.PersonaEarnest)
		{
			return ConversationTagHelper.UsesLowRegister(character);
		}
		return false;
	}
}
