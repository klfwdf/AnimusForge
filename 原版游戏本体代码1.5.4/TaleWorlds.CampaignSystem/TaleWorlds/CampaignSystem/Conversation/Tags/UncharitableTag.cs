using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class UncharitableTag : ConversationTag
{
	public const string Id = "UncharitableTag";

	public override string StringId => "UncharitableTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return character.GetTraitLevel(DefaultTraits.Generosity) + character.GetTraitLevel(DefaultTraits.Mercy) < 0;
	}
}
