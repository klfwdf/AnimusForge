using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class AmoralTag : ConversationTag
{
	public const string Id = "AmoralTag";

	public override string StringId => "AmoralTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return character.GetTraitLevel(DefaultTraits.Honor) + character.GetTraitLevel(DefaultTraits.Mercy) < 0;
	}
}
