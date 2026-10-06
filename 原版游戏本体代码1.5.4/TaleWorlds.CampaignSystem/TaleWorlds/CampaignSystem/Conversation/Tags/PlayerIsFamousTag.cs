namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PlayerIsFamousTag : ConversationTag
{
	public const string Id = "PlayerIsFamousTag";

	public override string StringId => "PlayerIsFamousTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return Clan.PlayerClan.Renown >= 50f;
	}
}
