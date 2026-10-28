using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class VoiceGroupPersonaSoftspokenTribalTag : ConversationTag
{
	public const string Id = "VoiceGroupPersonaSoftspokenTribalTag";

	public override string StringId => "VoiceGroupPersonaSoftspokenTribalTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.GetPersona() == DefaultTraits.PersonaSoftspoken)
		{
			return ConversationTagHelper.TribalVoiceGroup(character);
		}
		return false;
	}
}
