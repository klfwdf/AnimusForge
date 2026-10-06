using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class RogueSkillsTag : ConversationTag
{
	public const string Id = "RogueSkillsTag";

	public override string StringId => "RogueSkillsTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.HeroObject.GetTraitLevel(DefaultTraits.RogueSkills) > 0;
		}
		return false;
	}
}
