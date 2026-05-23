using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class ValorTag : ConversationTag
{
	public const string Id = "ValorTag";

	public override string StringId => "ValorTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.HeroObject.GetTraitLevel(DefaultTraits.Valor) > 0;
		}
		return false;
	}
}
