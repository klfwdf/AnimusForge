using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class VoiceGroupPersonaCurtLowerTag : ConversationTag
{
	public const string Id = "VoiceGroupPersonaCurtLowerTag";

	public override string StringId => "VoiceGroupPersonaCurtLowerTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.GetPersona() == DefaultTraits.PersonaCurt)
		{
			return ConversationTagHelper.UsesLowRegister(character);
		}
		return false;
	}
}
