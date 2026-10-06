namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class NpcIsNobleTag : ConversationTag
{
	public const string Id = "NpcIsNobleTag";

	public override string StringId => "NpcIsNobleTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		Hero heroObject = character.HeroObject;
		if (heroObject == null)
		{
			return false;
		}
		return heroObject.Clan?.IsNoble == true;
	}
}
