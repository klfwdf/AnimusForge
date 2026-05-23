using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PersonaEarnestTag : ConversationTag
{
	public const string Id = "PersonaEarnestTag";

	public override string StringId => "PersonaEarnestTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.GetPersona() == DefaultTraits.PersonaEarnest;
		}
		return false;
	}
}
