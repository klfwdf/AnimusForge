using System.Collections.Generic;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Siege;

public interface ISiegeEventSide
{
	SiegeEvent SiegeEvent { get; }

	SiegeStrategy SiegeStrategy { get; }

	BattleSideEnum BattleSide { get; }

	int NumberOfTroopsKilledOnSide { get; }

	SiegeEvent.SiegeEnginesContainer SiegeEngines { get; }

	MBReadOnlyList<SiegeEvent.SiegeEngineMissile> SiegeEngineMissiles { get; }

	IEnumerable<PartyBase> GetInvolvedPartiesForEventType(MapEvent.BattleTypes mapEventType = MapEvent.BattleTypes.Siege);

	PartyBase GetNextInvolvedPartyForEventType(ref int partyIndex, MapEvent.BattleTypes mapEventType = MapEvent.BattleTypes.Siege);

	bool HasInvolvedPartyForEventType(PartyBase party, MapEvent.BattleTypes mapEventType = MapEvent.BattleTypes.Siege);

	void OnTroopsKilledOnSide(int killCount);

	void AddSiegeEngineMissile(SiegeEvent.SiegeEngineMissile missile);

	void RemoveDeprecatedMissiles();

	void SetSiegeStrategy(SiegeStrategy strategy);

	void InitializeSiegeEventSide();

	void GetAttackTarget(ISiegeEventSide siegeEventSide, SiegeEngineType siegeEngine, int siegeEngineSlot, out SiegeBombardTargets targetType, out int targetIndex);

	void FinalizeSiegeEvent();
}
