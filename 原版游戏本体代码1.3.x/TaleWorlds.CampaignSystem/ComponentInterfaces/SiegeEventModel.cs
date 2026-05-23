using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001E3 RID: 483
	public abstract class SiegeEventModel : MBGameModel<SiegeEventModel>
	{
		// Token: 0x06001E8E RID: 7822
		public abstract int GetSiegeEngineDestructionCasualties(SiegeEvent siegeEvent, BattleSideEnum side, SiegeEngineType destroyedSiegeEngine);

		// Token: 0x06001E8F RID: 7823
		public abstract float GetCasualtyChance(MobileParty siegeParty, SiegeEvent siegeEvent, BattleSideEnum side);

		// Token: 0x06001E90 RID: 7824
		public abstract int GetColleteralDamageCasualties(SiegeEngineType attackerSiegeEngine, MobileParty attackerParty);

		// Token: 0x06001E91 RID: 7825
		public abstract float GetSiegeEngineHitChance(SiegeEngineType siegeEngineType, BattleSideEnum battleSide, SiegeBombardTargets target, Town town);

		// Token: 0x06001E92 RID: 7826
		public abstract string GetSiegeEngineMapPrefabName(SiegeEngineType siegeEngineType, int wallLevel, BattleSideEnum side);

		// Token: 0x06001E93 RID: 7827
		public abstract string GetSiegeEngineMapProjectilePrefabName(SiegeEngineType siegeEngineType);

		// Token: 0x06001E94 RID: 7828
		public abstract string GetSiegeEngineMapReloadAnimationName(SiegeEngineType siegeEngineType, BattleSideEnum side);

		// Token: 0x06001E95 RID: 7829
		public abstract string GetSiegeEngineMapFireAnimationName(SiegeEngineType siegeEngineType, BattleSideEnum side);

		// Token: 0x06001E96 RID: 7830
		public abstract sbyte GetSiegeEngineMapProjectileBoneIndex(SiegeEngineType siegeEngineType, BattleSideEnum side);

		// Token: 0x06001E97 RID: 7831
		public abstract float GetSiegeStrategyScore(SiegeEvent siege, BattleSideEnum side, SiegeStrategy strategy);

		// Token: 0x06001E98 RID: 7832
		public abstract float GetConstructionProgressPerHour(SiegeEngineType type, SiegeEvent siegeEvent, ISiegeEventSide side);

		// Token: 0x06001E99 RID: 7833
		public abstract MobileParty GetEffectiveSiegePartyForSide(SiegeEvent siegeEvent, BattleSideEnum side);

		// Token: 0x06001E9A RID: 7834
		public abstract float GetAvailableManDayPower(ISiegeEventSide side);

		// Token: 0x06001E9B RID: 7835
		public abstract IEnumerable<SiegeEngineType> GetAvailableAttackerRangedSiegeEngines(PartyBase party);

		// Token: 0x06001E9C RID: 7836
		public abstract IEnumerable<SiegeEngineType> GetAvailableDefenderSiegeEngines(PartyBase party);

		// Token: 0x06001E9D RID: 7837
		public abstract IEnumerable<SiegeEngineType> GetAvailableAttackerRamSiegeEngines(PartyBase party);

		// Token: 0x06001E9E RID: 7838
		public abstract IEnumerable<SiegeEngineType> GetAvailableAttackerTowerSiegeEngines(PartyBase party);

		// Token: 0x06001E9F RID: 7839
		public abstract IEnumerable<SiegeEngineType> GetPrebuiltSiegeEnginesOfSettlement(Settlement settlement);

		// Token: 0x06001EA0 RID: 7840
		public abstract IEnumerable<SiegeEngineType> GetPrebuiltSiegeEnginesOfSiegeCamp(BesiegerCamp camp);

		// Token: 0x06001EA1 RID: 7841
		public abstract float GetSiegeEngineHitPoints(SiegeEvent siegeEvent, SiegeEngineType siegeEngine, BattleSideEnum battleSide);

		// Token: 0x06001EA2 RID: 7842
		public abstract int GetRangedSiegeEngineReloadTime(SiegeEvent siegeEvent, BattleSideEnum side, SiegeEngineType siegeEngine);

		// Token: 0x06001EA3 RID: 7843
		public abstract float GetSiegeEngineDamage(SiegeEvent siegeEvent, BattleSideEnum battleSide, SiegeEngineType siegeEngine, SiegeBombardTargets target);

		// Token: 0x06001EA4 RID: 7844
		public abstract FlattenedTroopRoster GetPriorityTroopsForSallyOutAmbush();
	}
}
