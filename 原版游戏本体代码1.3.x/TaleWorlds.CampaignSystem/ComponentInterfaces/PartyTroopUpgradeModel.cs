using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001EB RID: 491
	public abstract class PartyTroopUpgradeModel : MBGameModel<PartyTroopUpgradeModel>
	{
		// Token: 0x06001ECB RID: 7883
		public abstract bool CanPartyUpgradeTroopToTarget(PartyBase party, CharacterObject character, CharacterObject target);

		// Token: 0x06001ECC RID: 7884
		public abstract bool IsTroopUpgradeable(PartyBase party, CharacterObject character);

		// Token: 0x06001ECD RID: 7885
		public abstract bool DoesPartyHaveRequiredItemsForUpgrade(PartyBase party, CharacterObject upgradeTarget);

		// Token: 0x06001ECE RID: 7886
		public abstract bool DoesPartyHaveRequiredPerksForUpgrade(PartyBase party, CharacterObject character, CharacterObject upgradeTarget, out PerkObject requiredPerk);

		// Token: 0x06001ECF RID: 7887
		public abstract ExplainedNumber GetGoldCostForUpgrade(PartyBase party, CharacterObject characterObject, CharacterObject upgradeTarget);

		// Token: 0x06001ED0 RID: 7888
		public abstract int GetXpCostForUpgrade(PartyBase party, CharacterObject characterObject, CharacterObject upgradeTarget);

		// Token: 0x06001ED1 RID: 7889
		public abstract int GetSkillXpFromUpgradingTroops(PartyBase party, CharacterObject troop, int numberOfTroops);

		// Token: 0x06001ED2 RID: 7890
		public abstract float GetUpgradeChanceForTroopUpgrade(PartyBase party, CharacterObject troop, int upgradeTargetIndex);
	}
}
