using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem;

public class BattleSimulation : IBattleObserver
{
	private enum SimulationState
	{
		Play,
		FastForward,
		Skip,
		Pause
	}

	private readonly MapEvent _mapEvent;

	private bool _isPlayerRetreated;

	private float _numTicks;

	private IBattleObserver _battleObserver;

	public readonly FlattenedTroopRoster[] SelectedTroops = new FlattenedTroopRoster[2];

	private SimulationState _simulationState;

	public bool IsSimulationFinished { get; private set; }

	private bool IsPlayerJoinedBattle => PlayerEncounter.Current.IsJoinedBattle;

	public MapEvent MapEvent => _mapEvent;

	public bool IsPlayerRetreated => _isPlayerRetreated;

	public IBattleObserver BattleObserver
	{
		get
		{
			return _battleObserver;
		}
		set
		{
			_battleObserver = value;
		}
	}

	public List<List<BattleResultPartyData>> Teams { get; private set; }

	public BattleSimulation(FlattenedTroopRoster selectedTroopsForPlayerSide, FlattenedTroopRoster selectedTroopsForOtherSide)
	{
		_mapEvent = PlayerEncounter.Battle ?? PlayerEncounter.StartBattle();
		_mapEvent.IsPlayerSimulation = true;
		_mapEvent.BattleObserver = this;
		SelectedTroops[(int)_mapEvent.PlayerSide] = selectedTroopsForPlayerSide;
		SelectedTroops[(int)_mapEvent.GetOtherSide(_mapEvent.PlayerSide)] = selectedTroopsForOtherSide;
		_mapEvent.GetNumberOfInvolvedMen();
		if (_mapEvent.IsSiegeAssault)
		{
			PlayerSiege.StartPlayerSiege(MobileParty.MainParty.Party.Side, isSimulation: true, _mapEvent.MapEventSettlement);
		}
		List<List<BattleResultPartyData>> list = new List<List<BattleResultPartyData>>
		{
			new List<BattleResultPartyData>(),
			new List<BattleResultPartyData>()
		};
		foreach (PartyBase involvedParty in _mapEvent.InvolvedParties)
		{
			BattleResultPartyData item = default(BattleResultPartyData);
			bool flag = false;
			foreach (BattleResultPartyData item2 in list[(int)involvedParty.Side])
			{
				if (item2.Party == involvedParty)
				{
					flag = true;
					item = item2;
					break;
				}
			}
			if (!flag)
			{
				item = new BattleResultPartyData(involvedParty);
				list[(int)involvedParty.Side].Add(item);
			}
			for (int i = 0; i < involvedParty.MemberRoster.Count; i++)
			{
				TroopRosterElement elementCopyAtIndex = involvedParty.MemberRoster.GetElementCopyAtIndex(i);
				if (!item.Characters.Contains(elementCopyAtIndex.Character))
				{
					item.Characters.Add(elementCopyAtIndex.Character);
				}
			}
		}
		Teams = list;
	}

	public void Play()
	{
		_simulationState = SimulationState.Play;
	}

	public void FastForward()
	{
		_simulationState = SimulationState.FastForward;
	}

	public void Skip()
	{
		_simulationState = SimulationState.Skip;
	}

	public void Pause()
	{
		_simulationState = SimulationState.Pause;
	}

	public void OnFinished()
	{
		foreach (PartyBase involvedParty in _mapEvent.InvolvedParties)
		{
			involvedParty.MemberRoster.RemoveZeroCounts();
		}
		GameMenu.ActivateGameMenu("encounter");
	}

	public void OnPlayerRetreat()
	{
		_isPlayerRetreated = true;
		_mapEvent.CommitXpGains();
		OnFinished();
	}

	public void Tick(float dt)
	{
		if (IsSimulationFinished)
		{
			return;
		}
		if (PlayerEncounter.Current == null)
		{
			Debug.FailedAssert("PlayerEncounter.Current == null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\BattleSimulation.cs", "Tick", 158);
			IsSimulationFinished = true;
			return;
		}
		if (ShouldFinishSimulation())
		{
			IsSimulationFinished = true;
			return;
		}
		if (_simulationState == SimulationState.Skip)
		{
			while (!ShouldFinishSimulation())
			{
				SimulateBattle();
			}
			return;
		}
		if (_simulationState == SimulationState.FastForward)
		{
			dt *= 6f;
		}
		else if (_simulationState == SimulationState.Pause)
		{
			dt = 0f;
		}
		_numTicks += dt;
		while (_numTicks >= 1f && !ShouldFinishSimulation())
		{
			SimulateBattle();
			_numTicks -= 1f;
		}
	}

	public void ResetSimulation()
	{
		MapEvent.SimulateBattleSetup(PlayerEncounter.CurrentBattleSimulation.SelectedTroops);
	}

	public void TroopNumberChanged(BattleSideEnum side, IBattleCombatant battleCombatant, BasicCharacterObject character, int number = 0, int numberKilled = 0, int numberWounded = 0, int numberRouted = 0, int killCount = 0, int numberReadyToUpgrade = 0)
	{
		BattleObserver?.TroopNumberChanged(side, battleCombatant, character, number, numberKilled, numberWounded, numberRouted, killCount, numberReadyToUpgrade);
	}

	public void HeroSkillIncreased(BattleSideEnum side, IBattleCombatant battleCombatant, BasicCharacterObject heroCharacter, SkillObject skill)
	{
		BattleObserver?.HeroSkillIncreased(side, battleCombatant, heroCharacter, skill);
	}

	public void BattleResultsReady()
	{
		BattleObserver?.BattleResultsReady();
	}

	public void TroopSideChanged(BattleSideEnum prevSide, BattleSideEnum newSide, IBattleCombatant battleCombatant, BasicCharacterObject character)
	{
		BattleObserver?.TroopSideChanged(prevSide, newSide, battleCombatant, character);
	}

	private void SimulateBattle()
	{
		_mapEvent.SimulatePlayerEncounterBattle();
	}

	private static bool ShouldFinishSimulation()
	{
		return PlayerEncounter.Battle.HasWinner;
	}
}
