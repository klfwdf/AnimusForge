using System;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001E0 RID: 480
	public abstract class MilitaryPowerModel : MBGameModel<MilitaryPowerModel>
	{
		// Token: 0x06001E78 RID: 7800
		public abstract float GetTroopPower(CharacterObject troop, BattleSideEnum side, MapEvent.PowerCalculationContext context, float leaderModifier);

		// Token: 0x06001E79 RID: 7801
		public abstract float GetPowerOfParty(PartyBase party, BattleSideEnum side, MapEvent.PowerCalculationContext context);

		// Token: 0x06001E7A RID: 7802
		public abstract float GetContextModifier(CharacterObject troop, BattleSideEnum battleSideEnum, MapEvent.PowerCalculationContext context);

		// Token: 0x06001E7B RID: 7803
		public abstract float GetContextModifier(Ship ship, BattleSideEnum battleSideEnum, MapEvent.PowerCalculationContext context);

		// Token: 0x06001E7C RID: 7804
		public abstract MapEvent.PowerCalculationContext GetContextForPosition(CampaignVec2 position);

		// Token: 0x06001E7D RID: 7805
		public abstract float GetDefaultTroopPower(CharacterObject troop);

		// Token: 0x06001E7E RID: 7806
		public abstract float GetPowerModifierOfHero(Hero leaderHero);
	}
}
