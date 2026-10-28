using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class VoiceGroupPersonaEarnestUpperTag : ConversationTag
{
	public const string Id = "VoiceGroupPersonaEarnestUpperTag";

	public override string StringId => "VoiceGroupPersonaEarnestUpperTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.GetPersona() == DefaultTraits.PersonaEarnest)
		{
			return ConversationTagHelper.UsesHighRegister(character);
		}
		return false;
	}
}
