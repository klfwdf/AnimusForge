namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class NonCombatantTag : ConversationTag
{
	public const string Id = "NonCombatantTag";

	public override string StringId => "NonCombatantTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.HeroObject.IsNoncombatant;
		}
		return false;
	}
}
