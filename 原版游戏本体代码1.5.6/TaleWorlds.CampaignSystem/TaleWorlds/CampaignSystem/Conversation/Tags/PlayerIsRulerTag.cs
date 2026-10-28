namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PlayerIsRulerTag : ConversationTag
{
	public const string Id = "PlayerIsRulerTag";

	public override string StringId => "PlayerIsRulerTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return Hero.MainHero.Clan.Leader == Hero.MainHero;
	}
}
