using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class WaryTag : ConversationTag
{
	public const string Id = "WaryTag";

	public override string StringId => "WaryTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (character.IsHero && character.HeroObject.MapFaction != Hero.MainHero.MapFaction && (Settlement.CurrentSettlement == null || Settlement.CurrentSettlement.SiegeEvent != null))
		{
			if (!Campaign.Current.ConversationManager.CurrentConversationIsFirst)
			{
				return FactionManager.IsAtWarAgainstFaction(character.HeroObject.MapFaction, Hero.MainHero.MapFaction);
			}
			return true;
		}
		return false;
	}
}
