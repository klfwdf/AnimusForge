using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class CruelTag : ConversationTag
{
	public const string Id = "CruelTag";

	public override string StringId => "CruelTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.HeroObject.GetTraitLevel(DefaultTraits.Mercy) < 0;
		}
		return false;
	}
}
