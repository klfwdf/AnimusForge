using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PersonaIronicTag : ConversationTag
{
	public const string Id = "PersonaIronicTag";

	public override string StringId => "PersonaIronicTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.GetPersona() == DefaultTraits.PersonaIronic;
		}
		return false;
	}
}
