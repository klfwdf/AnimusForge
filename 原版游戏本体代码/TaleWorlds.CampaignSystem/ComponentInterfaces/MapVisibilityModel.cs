using System;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x0200018A RID: 394
	public abstract class MapVisibilityModel : MBGameModel<MapVisibilityModel>
	{
		// Token: 0x06001BDD RID: 7133
		public abstract float MaximumSeeingRange();

		// Token: 0x06001BDE RID: 7134
		public abstract float GetPartySpottingRangeBase(MobileParty party);

		// Token: 0x06001BDF RID: 7135
		public abstract ExplainedNumber GetPartySpottingRange(MobileParty party, bool includeDescriptions = false);

		// Token: 0x06001BE0 RID: 7136
		public abstract float GetPartyRelativeInspectionRange(IMapPoint party);

		// Token: 0x06001BE1 RID: 7137
		public abstract float GetPartySpottingDifficulty(MobileParty spotterParty, MobileParty party);

		// Token: 0x06001BE2 RID: 7138
		public abstract float GetHideoutSpottingDistance();
	}
}
