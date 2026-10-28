namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class NpcIsLiegeTag : ConversationTag
{
	public const string Id = "NpcIsLiegeTag";

	public override string StringId => "NpcIsLiegeTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.HeroObject.IsKingdomLeader;
		}
		return false;
	}
}
