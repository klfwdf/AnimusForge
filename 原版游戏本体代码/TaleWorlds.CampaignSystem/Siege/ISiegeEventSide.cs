using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Siege
{
	// Token: 0x020002DA RID: 730
	public interface ISiegeEventSide
	{
		// Token: 0x170009B7 RID: 2487
		// (get) Token: 0x060027CC RID: 10188
		SiegeEvent SiegeEvent { get; }

		// Token: 0x060027CD RID: 10189
		IEnumerable<PartyBase> GetInvolvedPartiesForEventType(MapEvent.BattleTypes mapEventType = MapEvent.BattleTypes.Siege);

		// Token: 0x060027CE RID: 10190
		PartyBase GetNextInvolvedPartyForEventType(ref int partyIndex, MapEvent.BattleTypes mapEventType = MapEvent.BattleTypes.Siege);

		// Token: 0x060027CF RID: 10191
		bool HasInvolvedPartyForEventType(PartyBase party, MapEvent.BattleTypes mapEventType = MapEvent.BattleTypes.Siege);

		// Token: 0x170009B8 RID: 2488
		// (get) Token: 0x060027D0 RID: 10192
		SiegeStrategy SiegeStrategy { get; }

		// Token: 0x170009B9 RID: 2489
		// (get) Token: 0x060027D1 RID: 10193
		BattleSideEnum BattleSide { get; }

		// Token: 0x060027D2 RID: 10194
		void OnTroopsKilledOnSide(int killCount);

		// Token: 0x170009BA RID: 2490
		// (get) Token: 0x060027D3 RID: 10195
		int NumberOfTroopsKilledOnSide { get; }

		// Token: 0x170009BB RID: 2491
		// (get) Token: 0x060027D4 RID: 10196
		SiegeEvent.SiegeEnginesContainer SiegeEngines { get; }

		// Token: 0x060027D5 RID: 10197
		void AddSiegeEngineMissile(SiegeEvent.SiegeEngineMissile missile);

		// Token: 0x060027D6 RID: 10198
		void RemoveDeprecatedMissiles();

		// Token: 0x170009BC RID: 2492
		// (get) Token: 0x060027D7 RID: 10199
		MBReadOnlyList<SiegeEvent.SiegeEngineMissile> SiegeEngineMissiles { get; }

		// Token: 0x060027D8 RID: 10200
		void SetSiegeStrategy(SiegeStrategy strategy);

		// Token: 0x060027D9 RID: 10201
		void InitializeSiegeEventSide();

		// Token: 0x060027DA RID: 10202
		void GetAttackTarget(ISiegeEventSide siegeEventSide, SiegeEngineType siegeEngine, int siegeEngineSlot, out SiegeBombardTargets targetType, out int targetIndex);

		// Token: 0x060027DB RID: 10203
		void FinalizeSiegeEvent();
	}
}
