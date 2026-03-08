using System;
using System.Linq;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000269 RID: 617
	public class PlayerBesiegingTag : ConversationTag
	{
		// Token: 0x170008B7 RID: 2231
		// (get) Token: 0x06002321 RID: 8993 RVA: 0x00098D52 File Offset: 0x00096F52
		public override string StringId
		{
			get
			{
				return "PlayerBesiegingTag";
			}
		}

		// Token: 0x06002322 RID: 8994 RVA: 0x00098D5C File Offset: 0x00096F5C
		public override bool IsApplicableTo(CharacterObject character)
		{
			if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.SiegeEvent != null)
			{
				return Settlement.CurrentSettlement.SiegeEvent.BesiegerCamp.GetInvolvedPartiesForEventType(MapEvent.BattleTypes.Siege).Any((PartyBase party) => party.MobileParty == Hero.MainHero.PartyBelongedTo);
			}
			return false;
		}

		// Token: 0x04000A76 RID: 2678
		public const string Id = "PlayerBesiegingTag";
	}
}
