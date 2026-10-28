using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.TroopSuppliers;

public class PartyGroupTroopSupplier : IMissionTroopSupplier
{
	private readonly int _initialTroopCount;

	private int _numAllocated;

	private int _numWounded;

	private int _numKilled;

	private int _numRouted;

	private bool _isPlayerSide;

	private Func<UniqueTroopDescriptor, MapEventParty, bool> _customAllocationConditions;

	private bool _anyTroopRemainsToBeSupplied = true;

	private int _nextTroopRank;

	internal MapEventSide PartyGroup { get; private set; }

	public int NumRemovedTroops => _numWounded + _numKilled + _numRouted;

	public int NumTroopsNotSupplied => _initialTroopCount - _numAllocated;

	public bool AnyTroopRemainsToBeSupplied => _anyTroopRemainsToBeSupplied;

	public PartyGroupTroopSupplier(MapEvent mapEvent, BattleSideEnum side, FlattenedTroopRoster priorTroops = null, Func<UniqueTroopDescriptor, MapEventParty, bool> customAllocationConditions = null)
	{
		_customAllocationConditions = customAllocationConditions;
		PartyGroup = mapEvent.GetMapEventSide(side);
		_isPlayerSide = mapEvent.PlayerSide == side;
		_initialTroopCount = PartyGroup.TroopCount;
		PartyGroup.MakeReadyForMission(priorTroops);
		_nextTroopRank = 0;
	}

	public IEnumerable<IAgentOriginBase> SupplyTroops(int numberToAllocate)
	{
		List<UniqueTroopDescriptor> troopsList = null;
		PartyGroup.AllocateTroops(ref troopsList, numberToAllocate, _customAllocationConditions);
		PartyGroupAgentOrigin[] array = new PartyGroupAgentOrigin[troopsList.Count];
		_numAllocated += troopsList.Count;
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new PartyGroupAgentOrigin(this, troopsList[i], _nextTroopRank++);
		}
		if (array.Length < numberToAllocate)
		{
			_anyTroopRemainsToBeSupplied = false;
		}
		return array;
	}

	public IAgentOriginBase SupplyOneTroop()
	{
		if (PartyGroup.AllocateTroop(_customAllocationConditions, out var troopDescriptor))
		{
			PartyGroupAgentOrigin result = new PartyGroupAgentOrigin(this, troopDescriptor, _nextTroopRank++);
			_anyTroopRemainsToBeSupplied = _anyTroopRemainsToBeSupplied && PartyGroup.HasReadyTroops;
			return result;
		}
		_anyTroopRemainsToBeSupplied = false;
		return null;
	}

	public IEnumerable<IAgentOriginBase> GetAllTroops()
	{
		List<UniqueTroopDescriptor> troopsList = null;
		PartyGroup.GetAllTroops(ref troopsList);
		PartyGroupAgentOrigin[] array = new PartyGroupAgentOrigin[troopsList.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new PartyGroupAgentOrigin(this, troopsList[i], i);
		}
		return array;
	}

	public BasicCharacterObject GetGeneralCharacter()
	{
		return PartyGroup.LeaderParty.General;
	}

	public int GetNumberOfPlayerControllableTroops()
	{
		int num = 0;
		foreach (MapEventParty party2 in PartyGroup.Parties)
		{
			PartyBase party = party2.Party;
			if (PartyBase.IsPartyUnderPlayerCommand(party) || (party.Side == PartyBase.MainParty.Side && PartyGroup.MapEvent.IsPlayerSergeant()))
			{
				num += party.NumberOfHealthyMembers;
			}
		}
		return num;
	}

	public void OnTroopWounded(UniqueTroopDescriptor troopDescriptor)
	{
		_numWounded++;
		PartyGroup.OnTroopWounded(troopDescriptor);
	}

	public void OnTroopKilled(UniqueTroopDescriptor troopDescriptor)
	{
		_numKilled++;
		PartyGroup.OnTroopKilled(troopDescriptor);
	}

	public void OnTroopRouted(UniqueTroopDescriptor troopDescriptor, bool isOrderRetreat)
	{
		_numRouted++;
		PartyGroup.OnTroopRouted(troopDescriptor, isOrderRetreat);
	}

	internal CharacterObject GetTroop(UniqueTroopDescriptor troopDescriptor)
	{
		return PartyGroup.GetAllocatedTroop(troopDescriptor) ?? PartyGroup.GetReadyTroop(troopDescriptor);
	}

	public PartyBase GetParty(UniqueTroopDescriptor troopDescriptor)
	{
		PartyBase partyBase = PartyGroup.GetAllocatedTroopParty(troopDescriptor);
		if (partyBase == null)
		{
			partyBase = PartyGroup.GetReadyTroopParty(troopDescriptor);
		}
		return partyBase;
	}

	public void OnTroopScoreHit(UniqueTroopDescriptor descriptor, BasicCharacterObject attackedCharacter, int damage, bool isFatal, bool isTeamKill, WeaponComponentData attackerWeapon)
	{
		PartyGroup.OnTroopScoreHit(descriptor, (CharacterObject)attackedCharacter, damage, isFatal, isTeamKill, attackerWeapon, isSimulatedHit: false);
	}
}
