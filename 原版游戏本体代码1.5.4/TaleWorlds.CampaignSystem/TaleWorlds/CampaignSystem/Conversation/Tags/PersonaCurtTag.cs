using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PersonaCurtTag : ConversationTag
{
	public const string Id = "PersonaCurtTag";

	public override string StringId => "PersonaCurtTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.GetPersona() == DefaultTraits.PersonaCurt;
		}
		return false;
	}
}
