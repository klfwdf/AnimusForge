namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class CombatantTag : ConversationTag
{
	public const string Id = "CombatantTag";

	public override string StringId => "CombatantTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return !character.HeroObject.IsNoncombatant;
		}
		return true;
	}
}
