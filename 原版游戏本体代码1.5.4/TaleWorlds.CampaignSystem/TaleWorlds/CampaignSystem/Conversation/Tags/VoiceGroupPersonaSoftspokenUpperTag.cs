using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class VoiceGroupPersonaSoftspokenUpperTag : ConversationTag
{
	public const string Id = "VoiceGroupPersonaSoftspokenUpperTag";

	public override string StringId => "VoiceGroupPersonaSoftspokenUpperTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.GetPersona() == DefaultTraits.PersonaSoftspoken)
		{
			return ConversationTagHelper.UsesHighRegister(character);
		}
		return false;
	}
}
