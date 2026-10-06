using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class ChivalrousTag : ConversationTag
{
	public const string Id = "ChivalrousTag";

	public override string StringId => "ChivalrousTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return character.GetTraitLevel(DefaultTraits.Honor) + character.GetTraitLevel(DefaultTraits.Valor) > 0;
	}
}
