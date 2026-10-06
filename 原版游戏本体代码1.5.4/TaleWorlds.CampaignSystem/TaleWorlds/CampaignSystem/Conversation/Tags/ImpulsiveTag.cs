using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class ImpulsiveTag : ConversationTag
{
	public const string Id = "ImpulsiveTag";

	public override string StringId => "ImpulsiveTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.HeroObject.GetTraitLevel(DefaultTraits.Calculating) < 0;
		}
		return false;
	}
}
