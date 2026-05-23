using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class HonorTag : ConversationTag
{
	public const string Id = "HonorTag";

	public override string StringId => "HonorTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.HeroObject.GetTraitLevel(DefaultTraits.Honor) > 0;
		}
		return false;
	}
}
