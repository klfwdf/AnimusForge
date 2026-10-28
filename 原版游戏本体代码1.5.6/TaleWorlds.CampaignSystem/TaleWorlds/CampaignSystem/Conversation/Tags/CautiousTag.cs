using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class CautiousTag : ConversationTag
{
	public const string Id = "CautiousTag";

	public override string StringId => "CautiousTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.HeroObject.GetTraitLevel(DefaultTraits.Valor) < 0;
		}
		return false;
	}
}
