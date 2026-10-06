using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PlayerBesiegingTag : ConversationTag
{
	public const string Id = "PlayerBesiegingTag";

	public override string StringId => "PlayerBesiegingTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.SiegeEvent != null)
		{
			return Settlement.CurrentSettlement.SiegeEvent.BesiegerCamp.GetInvolvedPartiesForEventType().Any((PartyBase party) => party.MobileParty == Hero.MainHero.PartyBelongedTo);
		}
		return false;
	}
}
