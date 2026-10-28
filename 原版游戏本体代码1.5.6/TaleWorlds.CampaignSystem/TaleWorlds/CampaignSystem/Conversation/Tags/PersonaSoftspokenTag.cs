using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PersonaSoftspokenTag : ConversationTag
{
	public const string Id = "PersonaSoftspokenTag";

	public override string StringId => "PersonaSoftspokenTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.GetPersona() == DefaultTraits.PersonaSoftspoken;
		}
		return false;
	}
}
