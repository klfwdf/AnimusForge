namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class NoConflictTag : ConversationTag
{
	public const string Id = "NoConflictTag";

	public override string StringId => "NoConflictTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		bool num = new HostileRelationshipTag().IsApplicableTo(character);
		bool flag = new PlayerIsEnemyTag().IsApplicableTo(character);
		if (!num)
		{
			return !flag;
		}
		return false;
	}
}
