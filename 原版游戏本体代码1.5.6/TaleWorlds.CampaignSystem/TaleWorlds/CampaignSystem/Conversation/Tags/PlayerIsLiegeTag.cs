namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PlayerIsLiegeTag : ConversationTag
{
	public const string Id = "PlayerIsLiegeTag";

	public override string StringId => "PlayerIsLiegeTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero && character.HeroObject.MapFaction.IsKingdomFaction && character.HeroObject.MapFaction == Hero.MainHero.MapFaction)
		{
			return Hero.MainHero.MapFaction.Leader == Hero.MainHero;
		}
		return false;
	}
}
