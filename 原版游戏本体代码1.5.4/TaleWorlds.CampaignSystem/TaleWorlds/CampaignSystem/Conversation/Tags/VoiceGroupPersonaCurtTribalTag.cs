using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class VoiceGroupPersonaCurtTribalTag : ConversationTag
{
	public const string Id = "VoiceGroupPersonaCurtTribalTag";

	public override string StringId => "VoiceGroupPersonaCurtTribalTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.GetPersona() == DefaultTraits.PersonaCurt)
		{
			return ConversationTagHelper.TribalVoiceGroup(character);
		}
		return false;
	}
}
