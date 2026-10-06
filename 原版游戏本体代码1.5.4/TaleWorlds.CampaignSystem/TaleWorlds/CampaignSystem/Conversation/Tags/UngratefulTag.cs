using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class UngratefulTag : ConversationTag
{
	public const string Id = "UngratefulTag";

	public override string StringId => "UngratefulTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.HeroObject.GetTraitLevel(DefaultTraits.Generosity) < 0;
		}
		return false;
	}
}
