namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PlayerIsEnemyTag : ConversationTag
{
	public const string Id = "PlayerIsEnemyTag";

	public override string StringId => "PlayerIsEnemyTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero)
		{
			return FactionManager.IsAtWarAgainstFaction(character.HeroObject.MapFaction, Hero.MainHero.MapFaction);
		}
		return false;
	}
}
