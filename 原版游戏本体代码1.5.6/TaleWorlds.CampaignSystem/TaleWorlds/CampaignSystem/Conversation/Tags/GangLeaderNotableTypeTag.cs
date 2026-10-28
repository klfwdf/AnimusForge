namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class GangLeaderNotableTypeTag : ConversationTag
{
	public const string Id = "GangLeaderNotableTypeTag";

	public override string StringId => "GangLeaderNotableTypeTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return character.Occupation == Occupation.GangLeader;
		}
		return false;
	}
}
