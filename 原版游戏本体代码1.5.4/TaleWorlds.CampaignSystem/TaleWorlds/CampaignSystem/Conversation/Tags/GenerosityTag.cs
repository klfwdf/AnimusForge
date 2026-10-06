using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class GenerosityTag : ConversationTag
{
	public const string Id = "GenerosityTag";

	public override string StringId => "GenerosityTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.HeroObject.GetTraitLevel(DefaultTraits.Generosity) > 0;
		}
		return false;
	}
}
