using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class MercyTag : ConversationTag
{
	public const string Id = "MercyTag";

	public override string StringId => "MercyTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.HeroObject.GetTraitLevel(DefaultTraits.Mercy) > 0;
		}
		return false;
	}
}
