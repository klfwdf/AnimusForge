using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class DeviousTag : ConversationTag
{
	public const string Id = "DeviousTag";

	public override string StringId => "DeviousTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.HeroObject.GetTraitLevel(DefaultTraits.Honor) < 0;
		}
		return false;
	}
}
