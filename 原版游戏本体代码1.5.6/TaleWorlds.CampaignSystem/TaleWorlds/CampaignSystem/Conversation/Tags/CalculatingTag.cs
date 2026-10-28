using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class CalculatingTag : ConversationTag
{
	public const string Id = "CalculatingTag";

	public override string StringId => "CalculatingTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.HeroObject.GetTraitLevel(DefaultTraits.Calculating) > 0;
		}
		return false;
	}
}
