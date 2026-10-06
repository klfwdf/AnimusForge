using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class VoiceGroupPersonaCurtUpperTag : ConversationTag
{
	public const string Id = "VoiceGroupPersonaCurtUpperTag";

	public override string StringId => "VoiceGroupPersonaCurtUpperTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.GetPersona() == DefaultTraits.PersonaCurt)
		{
			return ConversationTagHelper.UsesHighRegister(character);
		}
		return false;
	}
}
