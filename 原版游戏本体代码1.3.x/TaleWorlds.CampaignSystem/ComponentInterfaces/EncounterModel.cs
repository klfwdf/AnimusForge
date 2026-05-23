using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x02000186 RID: 390
	public abstract class EncounterModel : MBGameModel<EncounterModel>
	{
		// Token: 0x170006F1 RID: 1777
		// (get) Token: 0x06001BA8 RID: 7080
		public abstract float NeededMaximumDistanceForEncounteringMobileParty { get; }

		// Token: 0x170006F2 RID: 1778
		// (get) Token: 0x06001BA9 RID: 7081
		public abstract float MaximumAllowedDistanceForEncounteringMobilePartyInArmy { get; }

		// Token: 0x170006F3 RID: 1779
		// (get) Token: 0x06001BAA RID: 7082
		public abstract float NeededMaximumDistanceForEncounteringTown { get; }

		// Token: 0x170006F4 RID: 1780
		// (get) Token: 0x06001BAB RID: 7083
		public abstract float NeededMaximumDistanceForEncounteringBlockade { get; }

		// Token: 0x170006F5 RID: 1781
		// (get) Token: 0x06001BAC RID: 7084
		public abstract float NeededMaximumDistanceForEncounteringVillage { get; }

		// Token: 0x170006F6 RID: 1782
		// (get) Token: 0x06001BAD RID: 7085
		public abstract float GetEncounterJoiningRadius { get; }

		// Token: 0x170006F7 RID: 1783
		// (get) Token: 0x06001BAE RID: 7086
		public abstract float GetSettlementBeingNearFieldBattleRadius { get; }

		// Token: 0x170006F8 RID: 1784
		// (get) Token: 0x06001BAF RID: 7087
		public abstract float PlayerParleyDistance { get; }

		// Token: 0x06001BB0 RID: 7088
		public abstract bool IsEncounterExemptFromHostileActions(PartyBase side1, PartyBase side2);

		// Token: 0x06001BB1 RID: 7089
		public abstract bool CanMainHeroDoParleyWithParty(PartyBase partyBase, out TextObject explanation);

		// Token: 0x06001BB2 RID: 7090
		public abstract Hero GetLeaderOfSiegeEvent(SiegeEvent siegeEvent, BattleSideEnum side);

		// Token: 0x06001BB3 RID: 7091
		public abstract Hero GetLeaderOfMapEvent(MapEvent mapEvent, BattleSideEnum side);

		// Token: 0x06001BB4 RID: 7092
		public abstract int GetCharacterSergeantScore(Hero hero);

		// Token: 0x06001BB5 RID: 7093
		public abstract IEnumerable<PartyBase> GetDefenderPartiesOfSettlement(Settlement settlement, MapEvent.BattleTypes mapEventType);

		// Token: 0x06001BB6 RID: 7094
		public abstract PartyBase GetNextDefenderPartyOfSettlement(Settlement settlement, ref int partyIndex, MapEvent.BattleTypes mapEventType);

		// Token: 0x06001BB7 RID: 7095
		public abstract MapEventComponent CreateMapEventComponentForEncounter(PartyBase attackerParty, PartyBase defenderParty, MapEvent.BattleTypes battleType);

		// Token: 0x06001BB8 RID: 7096
		public abstract ExplainedNumber GetBribeChance(MobileParty defenderParty, MobileParty attackerParty);

		// Token: 0x06001BB9 RID: 7097
		public abstract float GetSurrenderChance(MobileParty defenderParty, MobileParty attackerParty);

		// Token: 0x06001BBA RID: 7098
		public abstract float GetMapEventSideRunAwayChance(MapEventSide mapEventside);

		// Token: 0x06001BBB RID: 7099
		public abstract void FindNonAttachedNpcPartiesWhoWillJoinPlayerEncounter(List<MobileParty> partiesToJoinPlayerSide, List<MobileParty> partiesToJoinEnemySide);

		// Token: 0x06001BBC RID: 7100
		public abstract bool CanPlayerForceBanditsToJoin(out TextObject explanation);

		// Token: 0x06001BBD RID: 7101
		public abstract bool IsPartyUnderPlayerCommand(PartyBase party);
	}
}
