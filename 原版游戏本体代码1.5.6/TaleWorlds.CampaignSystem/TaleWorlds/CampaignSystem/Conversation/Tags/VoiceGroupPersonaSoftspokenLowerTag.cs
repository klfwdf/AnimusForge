using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class VoiceGroupPersonaSoftspokenLowerTag : ConversationTag
{
	public const string Id = "VoiceGroupPersonaSoftspokenLowerTag";

	public override string StringId => "VoiceGroupPersonaSoftspokenLowerTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.GetPersona() == DefaultTraits.PersonaSoftspoken)
		{
			return ConversationTagHelper.UsesLowRegister(character);
		}
		return false;
	}
}
