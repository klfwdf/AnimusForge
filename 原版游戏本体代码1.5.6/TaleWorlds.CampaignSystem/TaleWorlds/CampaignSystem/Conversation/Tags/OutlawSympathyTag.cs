using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class OutlawSympathyTag : ConversationTag
{
	public const string Id = "OutlawSympathyTag";

	public override string StringId => "OutlawSympathyTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero && character.HeroObject.IsWanderer)
		{
			return character.HeroObject.GetTraitLevel(DefaultTraits.RogueSkills) > 0;
		}
		return false;
	}
}
