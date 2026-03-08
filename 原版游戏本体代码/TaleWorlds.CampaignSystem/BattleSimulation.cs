using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200002B RID: 43
	public class BattleSimulation : IBattleObserver
	{
		// Token: 0x17000019 RID: 25
		// (get) Token: 0x060001D9 RID: 473 RVA: 0x000134DB File Offset: 0x000116DB
		// (set) Token: 0x060001DA RID: 474 RVA: 0x000134E3 File Offset: 0x000116E3
		public bool IsSimulationFinished { get; private set; }

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x060001DB RID: 475 RVA: 0x000134EC File Offset: 0x000116EC
		private bool IsPlayerJoinedBattle
		{
			get
			{
				return PlayerEncounter.Current.IsJoinedBattle;
			}
		}

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x060001DC RID: 476 RVA: 0x000134F8 File Offset: 0x000116F8
		public MapEvent MapEvent
		{
			get
			{
				return this._mapEvent;
			}
		}

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x060001DD RID: 477 RVA: 0x00013500 File Offset: 0x00011700
		public bool IsPlayerRetreated
		{
			get
			{
				return this._isPlayerRetreated;
			}
		}

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x060001DE RID: 478 RVA: 0x00013508 File Offset: 0x00011708
		// (set) Token: 0x060001DF RID: 479 RVA: 0x00013510 File Offset: 0x00011710
		public IBattleObserver BattleObserver
		{
			get
			{
				return this._battleObserver;
			}
			set
			{
				this._battleObserver = value;
			}
		}

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x060001E0 RID: 480 RVA: 0x00013519 File Offset: 0x00011719
		// (set) Token: 0x060001E1 RID: 481 RVA: 0x00013521 File Offset: 0x00011721
		public List<List<BattleResultPartyData>> Teams { get; private set; }

		// Token: 0x060001E2 RID: 482 RVA: 0x0001352C File Offset: 0x0001172C
		public BattleSimulation(FlattenedTroopRoster selectedTroopsForPlayerSide, FlattenedTroopRoster selectedTroopsForOtherSide)
		{
			this._mapEvent = PlayerEncounter.Battle ?? PlayerEncounter.StartBattle();
			this._mapEvent.IsPlayerSimulation = true;
			this._mapEvent.BattleObserver = this;
			this.SelectedTroops[(int)this._mapEvent.PlayerSide] = selectedTroopsForPlayerSide;
			this.SelectedTroops[(int)this._mapEvent.GetOtherSide(this._mapEvent.PlayerSide)] = selectedTroopsForOtherSide;
			this._mapEvent.GetNumberOfInvolvedMen();
			if (this._mapEvent.IsSiegeAssault)
			{
				PlayerSiege.StartPlayerSiege(MobileParty.MainParty.Party.Side, true, this._mapEvent.MapEventSettlement);
			}
			List<List<BattleResultPartyData>> list = new List<List<BattleResultPartyData>>
			{
				new List<BattleResultPartyData>(),
				new List<BattleResultPartyData>()
			};
			foreach (PartyBase partyBase in this._mapEvent.InvolvedParties)
			{
				BattleResultPartyData battleResultPartyData = default(BattleResultPartyData);
				bool flag = false;
				foreach (BattleResultPartyData battleResultPartyData2 in list[(int)partyBase.Side])
				{
					if (battleResultPartyData2.Party == partyBase)
					{
						flag = true;
						battleResultPartyData = battleResultPartyData2;
						break;
					}
				}
				if (!flag)
				{
					battleResultPartyData = new BattleResultPartyData(partyBase);
					list[(int)partyBase.Side].Add(battleResultPartyData);
				}
				for (int i = 0; i < partyBase.MemberRoster.Count; i++)
				{
					TroopRosterElement elementCopyAtIndex = partyBase.MemberRoster.GetElementCopyAtIndex(i);
					if (!battleResultPartyData.Characters.Contains(elementCopyAtIndex.Character))
					{
						battleResultPartyData.Characters.Add(elementCopyAtIndex.Character);
					}
				}
			}
			this.Teams = list;
		}

		// Token: 0x060001E3 RID: 483 RVA: 0x00013718 File Offset: 0x00011918
		public void Play()
		{
			this._simulationState = BattleSimulation.SimulationState.Play;
		}

		// Token: 0x060001E4 RID: 484 RVA: 0x00013721 File Offset: 0x00011921
		public void FastForward()
		{
			this._simulationState = BattleSimulation.SimulationState.FastForward;
		}

		// Token: 0x060001E5 RID: 485 RVA: 0x0001372A File Offset: 0x0001192A
		public void Skip()
		{
			this._simulationState = BattleSimulation.SimulationState.Skip;
		}

		// Token: 0x060001E6 RID: 486 RVA: 0x00013733 File Offset: 0x00011933
		public void Pause()
		{
			this._simulationState = BattleSimulation.SimulationState.Pause;
		}

		// Token: 0x060001E7 RID: 487 RVA: 0x0001373C File Offset: 0x0001193C
		public void OnFinished()
		{
			foreach (PartyBase partyBase in this._mapEvent.InvolvedParties)
			{
				partyBase.MemberRoster.RemoveZeroCounts();
			}
			GameMenu.ActivateGameMenu("encounter");
		}

		// Token: 0x060001E8 RID: 488 RVA: 0x0001379C File Offset: 0x0001199C
		public void OnPlayerRetreat()
		{
			this._isPlayerRetreated = true;
			this._mapEvent.AttackerSide.CommitXpGains();
			this._mapEvent.DefenderSide.CommitXpGains();
			this.OnFinished();
		}

		// Token: 0x060001E9 RID: 489 RVA: 0x000137CC File Offset: 0x000119CC
		public void Tick(float dt)
		{
			if (this.IsSimulationFinished)
			{
				return;
			}
			if (PlayerEncounter.Current == null)
			{
				Debug.FailedAssert("PlayerEncounter.Current == null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\BattleSimulation.cs", "Tick", 160);
				this.IsSimulationFinished = true;
				return;
			}
			if (BattleSimulation.ShouldFinishSimulation())
			{
				this.IsSimulationFinished = true;
				return;
			}
			if (this._simulationState == BattleSimulation.SimulationState.Skip)
			{
				while (!BattleSimulation.ShouldFinishSimulation())
				{
					this.SimulateBattle();
				}
				return;
			}
			if (this._simulationState == BattleSimulation.SimulationState.FastForward)
			{
				dt *= 6f;
			}
			else if (this._simulationState == BattleSimulation.SimulationState.Pause)
			{
				dt = 0f;
			}
			this._numTicks += dt;
			while (this._numTicks >= 1f && !BattleSimulation.ShouldFinishSimulation())
			{
				this.SimulateBattle();
				this._numTicks -= 1f;
			}
		}

		// Token: 0x060001EA RID: 490 RVA: 0x00013892 File Offset: 0x00011A92
		public void ResetSimulation()
		{
			this.MapEvent.SimulateBattleSetup(PlayerEncounter.CurrentBattleSimulation.SelectedTroops);
		}

		// Token: 0x060001EB RID: 491 RVA: 0x000138AC File Offset: 0x00011AAC
		public void TroopNumberChanged(BattleSideEnum side, IBattleCombatant battleCombatant, BasicCharacterObject character, int number = 0, int numberKilled = 0, int numberWounded = 0, int numberRouted = 0, int killCount = 0, int numberReadyToUpgrade = 0)
		{
			IBattleObserver battleObserver = this.BattleObserver;
			if (battleObserver == null)
			{
				return;
			}
			battleObserver.TroopNumberChanged(side, battleCombatant, character, number, numberKilled, numberWounded, numberRouted, killCount, numberReadyToUpgrade);
		}

		// Token: 0x060001EC RID: 492 RVA: 0x000138D8 File Offset: 0x00011AD8
		public void HeroSkillIncreased(BattleSideEnum side, IBattleCombatant battleCombatant, BasicCharacterObject heroCharacter, SkillObject skill)
		{
			IBattleObserver battleObserver = this.BattleObserver;
			if (battleObserver == null)
			{
				return;
			}
			battleObserver.HeroSkillIncreased(side, battleCombatant, heroCharacter, skill);
		}

		// Token: 0x060001ED RID: 493 RVA: 0x000138EF File Offset: 0x00011AEF
		public void BattleResultsReady()
		{
			IBattleObserver battleObserver = this.BattleObserver;
			if (battleObserver == null)
			{
				return;
			}
			battleObserver.BattleResultsReady();
		}

		// Token: 0x060001EE RID: 494 RVA: 0x00013901 File Offset: 0x00011B01
		public void TroopSideChanged(BattleSideEnum prevSide, BattleSideEnum newSide, IBattleCombatant battleCombatant, BasicCharacterObject character)
		{
			IBattleObserver battleObserver = this.BattleObserver;
			if (battleObserver == null)
			{
				return;
			}
			battleObserver.TroopSideChanged(prevSide, newSide, battleCombatant, character);
		}

		// Token: 0x060001EF RID: 495 RVA: 0x00013918 File Offset: 0x00011B18
		private void SimulateBattle()
		{
			this._mapEvent.SimulatePlayerEncounterBattle();
		}

		// Token: 0x060001F0 RID: 496 RVA: 0x00013925 File Offset: 0x00011B25
		private static bool ShouldFinishSimulation()
		{
			return PlayerEncounter.Battle.HasWinner;
		}

		// Token: 0x0400001F RID: 31
		private readonly MapEvent _mapEvent;

		// Token: 0x04000020 RID: 32
		private bool _isPlayerRetreated;

		// Token: 0x04000021 RID: 33
		private float _numTicks;

		// Token: 0x04000022 RID: 34
		private IBattleObserver _battleObserver;

		// Token: 0x04000024 RID: 36
		public readonly FlattenedTroopRoster[] SelectedTroops = new FlattenedTroopRoster[2];

		// Token: 0x04000025 RID: 37
		private BattleSimulation.SimulationState _simulationState;

		// Token: 0x020004F4 RID: 1268
		private enum SimulationState
		{
			// Token: 0x0400151C RID: 5404
			Play,
			// Token: 0x0400151D RID: 5405
			FastForward,
			// Token: 0x0400151E RID: 5406
			Skip,
			// Token: 0x0400151F RID: 5407
			Pause
		}
	}
}
