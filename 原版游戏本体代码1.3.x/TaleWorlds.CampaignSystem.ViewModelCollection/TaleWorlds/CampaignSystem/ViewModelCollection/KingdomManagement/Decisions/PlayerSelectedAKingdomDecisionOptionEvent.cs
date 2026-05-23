using System;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions
{
	// Token: 0x02000076 RID: 118
	public class PlayerSelectedAKingdomDecisionOptionEvent : EventBase
	{
		// Token: 0x170002E5 RID: 741
		// (get) Token: 0x060009B0 RID: 2480 RVA: 0x0002A5C8 File Offset: 0x000287C8
		// (set) Token: 0x060009B1 RID: 2481 RVA: 0x0002A5D0 File Offset: 0x000287D0
		public DecisionOutcome Option { get; private set; }

		// Token: 0x060009B2 RID: 2482 RVA: 0x0002A5D9 File Offset: 0x000287D9
		public PlayerSelectedAKingdomDecisionOptionEvent(DecisionOutcome option)
		{
			this.Option = option;
		}
	}
}
