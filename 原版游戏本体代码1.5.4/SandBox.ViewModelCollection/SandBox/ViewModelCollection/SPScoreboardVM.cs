using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.BattleScore;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.BattleScore;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;

namespace SandBox.ViewModelCollection;

public class SPScoreboardVM : ScoreboardBaseVM, IBattleObserver
{
	private readonly BattleSimulation _battleSimulation;

	private SallyOutEndLogic _sallyOutEndLogic;

	private static readonly TextObject _renownStr = new TextObject("{=eiWQoW9j}You gained {A0} renown.");

	private static readonly TextObject _influenceStr = new TextObject("{=5zeL8sa9}You gained {A0} influence.");

	private static readonly TextObject _moraleStr = new TextObject("{=WAKz9xX8}You gained {A0} morale.");

	private static readonly TextObject _lootStr = new TextObject("{=xu5NA6AW}You earned {A0}% of the loot.");

	private static readonly TextObject _deadLordStr = new TextObject("{=gDKhs4lD}{A0} has died on the battlefield.");

	private static readonly TextObject _figureheadStr = new TextObject("{=ANoYN1yZ}You unlocked the {A0} figurehead.");

	private float _moraleUpdateTimer = 1f;

	private float _missionEndScoreboardDelayTimer;

	private MBBindingList<BattleResultVM> _battleResults;

	private bool _isPlayerDefendingSiege
	{
		get
		{
			Mission current = Mission.Current;
			if (current != null && current.IsSiegeBattle)
			{
				return Mission.Current.PlayerTeam.IsDefender;
			}
			return false;
		}
	}

	[DataSourceProperty]
	public override MBBindingList<BattleResultVM> BattleResults
	{
		get
		{
			return _battleResults;
		}
		set
		{
			if (value != _battleResults)
			{
				_battleResults = value;
				OnPropertyChangedWithValue(value, "BattleResults");
			}
		}
	}

	public static SPScoreboardVM CreateSimulation(BattleSimulation simulation)
	{
		return new SPScoreboardVM(new SandboxSimulationBattleScoreContext(simulation), simulation);
	}

	public static SPScoreboardVM CreateMission(Mission mission)
	{
		return new SPScoreboardVM(new SandboxMissionBattleScoreContext(mission), null);
	}

	public static SPScoreboardVM CreateCustom(BattleScoreContext battleScoreContext, BattleSimulation simulation = null)
	{
		return new SPScoreboardVM(battleScoreContext, simulation);
	}

	public SPScoreboardVM(BattleScoreContext scoreboardContext, BattleSimulation simulation)
		: base(scoreboardContext)
	{
		_battleSimulation = simulation;
		BattleResults = new MBBindingList<BattleResultVM>();
	}

	protected override void UpdateQuitText()
	{
		if (base.IsOver)
		{
			base.QuitText = GameTexts.FindText("str_done").ToString();
		}
		else if (base.IsMainCharacterDead && !base.IsSimulation)
		{
			base.QuitText = GameTexts.FindText("str_end_battle").ToString();
		}
		else if (_isPlayerDefendingSiege)
		{
			base.QuitText = GameTexts.FindText("str_surrender").ToString();
		}
		else
		{
			base.QuitText = GameTexts.FindText("str_retreat").ToString();
		}
	}

	public override void Initialize(IMissionScreen missionScreen, Mission mission, Action releaseSimulationSources, Action<bool> onToggle)
	{
		base.Initialize(missionScreen, mission, releaseSimulationSources, onToggle);
		base.Attackers = new SPScoreboardSideVM(GameTexts.FindText("str_battle_result_side", "attacker"), ScoreboardContext.GetAttackerBanner(), isSimulation: true, PlayerEncounter.PlayerIsAttacker);
		base.Defenders = new SPScoreboardSideVM(GameTexts.FindText("str_battle_result_side", "defender"), ScoreboardContext.GetDefenderBanner(), isSimulation: true, !PlayerEncounter.PlayerIsAttacker);
		if (_battleSimulation != null)
		{
			PlayerSide = (PlayerEncounter.PlayerIsAttacker ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
			base.IsSimulation = true;
			base.IsMainCharacterDead = true;
			base.ShowScoreboard = true;
			foreach (UniqueTroopDescriptor troop in _battleSimulation.MapEvent.AttackerSide.GetTroops())
			{
				PartyBase allocatedTroopParty = _battleSimulation.MapEvent.AttackerSide.GetAllocatedTroopParty(troop);
				CharacterObject allocatedTroop = _battleSimulation.MapEvent.AttackerSide.GetAllocatedTroop(troop);
				GetSide(allocatedTroopParty.Side).UpdateScores(allocatedTroopParty, allocatedTroopParty?.Owner == Hero.MainHero, allocatedTroop, 1, 0, 0, 0, 0, 0);
			}
			foreach (UniqueTroopDescriptor troop2 in _battleSimulation.MapEvent.DefenderSide.GetTroops())
			{
				PartyBase allocatedTroopParty2 = _battleSimulation.MapEvent.DefenderSide.GetAllocatedTroopParty(troop2);
				CharacterObject allocatedTroop2 = _battleSimulation.MapEvent.DefenderSide.GetAllocatedTroop(troop2);
				GetSide(allocatedTroopParty2.Side).UpdateScores(allocatedTroopParty2, allocatedTroopParty2?.Owner == Hero.MainHero, allocatedTroop2, 1, 0, 0, 0, 0, 0);
			}
			_battleSimulation.BattleObserver = this;
			base.PowerComparer.Update(base.Defenders.CurrentPower, base.Attackers.CurrentPower, base.Defenders.CurrentPower, base.Attackers.CurrentPower);
		}
		else
		{
			base.IsSimulation = false;
			if (Campaign.Current != null)
			{
				if (PlayerEncounter.Battle != null)
				{
					PlayerSide = (PlayerEncounter.PlayerIsAttacker ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
				}
				else
				{
					PlayerSide = BattleSideEnum.Defender;
				}
			}
			else
			{
				Debug.FailedAssert("SPScoreboard on CustomBattle", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\SPScoreboardVM.cs", "Initialize", 134);
			}
			BattleObserverMissionLogic missionBehavior = _mission.GetMissionBehavior<BattleObserverMissionLogic>();
			if (missionBehavior != null)
			{
				missionBehavior.SetObserver(this);
			}
			else
			{
				Debug.FailedAssert("SPScoreboard on CustomBattle", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\SPScoreboardVM.cs", "Initialize", 159);
			}
		}
		string defenderColor;
		string attackerColor;
		if (MobileParty.MainParty.MapEvent != null)
		{
			defenderColor = ((!(MobileParty.MainParty.MapEvent.DefenderSide.LeaderParty?.MapFaction is Kingdom)) ? Color.FromUint(MobileParty.MainParty.MapEvent.DefenderSide.LeaderParty.MapFaction?.Banner.GetPrimaryColor() ?? 0).ToString() : Color.FromUint(((Kingdom)MobileParty.MainParty.MapEvent.DefenderSide.LeaderParty.MapFaction).PrimaryBannerColor).ToString());
			attackerColor = ((!(MobileParty.MainParty.MapEvent.AttackerSide.LeaderParty?.MapFaction is Kingdom)) ? Color.FromUint(MobileParty.MainParty.MapEvent.AttackerSide.LeaderParty.MapFaction?.Banner.GetPrimaryColor() ?? 0).ToString() : Color.FromUint(((Kingdom)MobileParty.MainParty.MapEvent.AttackerSide.LeaderParty.MapFaction).PrimaryBannerColor).ToString());
		}
		else
		{
			attackerColor = Color.FromUint(Mission.Current.Teams.Attacker.Color).ToString();
			defenderColor = Color.FromUint(Mission.Current.Teams.Defender.Color).ToString();
		}
		base.PowerComparer.SetColors(defenderColor, attackerColor);
		base.MissionTimeInSeconds = -1;
	}

	protected override void OnTick(float dt)
	{
		if (!base.IsSimulation)
		{
			if (_sallyOutEndLogic == null)
			{
				_sallyOutEndLogic = _mission?.GetMissionBehavior<SallyOutEndLogic>();
			}
			if (!base.IsOver)
			{
				Mission mission = _mission;
				if (mission == null || !mission.IsMissionEnding)
				{
					BattleEndLogic battleEndLogic = _battleEndLogic;
					if (battleEndLogic == null || !battleEndLogic.IsEnemySideRetreating)
					{
						SallyOutEndLogic sallyOutEndLogic = _sallyOutEndLogic;
						if (sallyOutEndLogic == null || !sallyOutEndLogic.IsSallyOutOver)
						{
							goto IL_0092;
						}
					}
				}
				if (_missionEndScoreboardDelayTimer < 1.5f)
				{
					_missionEndScoreboardDelayTimer += dt;
				}
				else
				{
					OnBattleOver();
				}
			}
		}
		goto IL_0092;
		IL_0092:
		if (!base.IsSimulation && !base.IsOver)
		{
			base.MissionTimeInSeconds = (int)Mission.Current.CurrentTime;
		}
		_moraleUpdateTimer += dt;
		if (_moraleUpdateTimer > 1f)
		{
			if (base.IsSimulation)
			{
				base.Attackers.Morale = MobileParty.MainParty.MapEvent.AttackerSide.GetSideMorale();
				base.Defenders.Morale = MobileParty.MainParty.MapEvent.DefenderSide.GetSideMorale();
			}
			else
			{
				base.Attackers.Morale = GetBattleMoraleOfSide(BattleSideEnum.Attacker);
				base.Defenders.Morale = GetBattleMoraleOfSide(BattleSideEnum.Defender);
			}
			_moraleUpdateTimer = 0f;
		}
	}

	public override void ExecutePlayAction()
	{
		if (base.IsSimulation)
		{
			_battleSimulation.Play();
		}
	}

	public override void ExecuteFastForwardAction()
	{
		if (base.IsSimulation)
		{
			base.IsPaused = false;
			if (!base.IsFastForwarding)
			{
				_battleSimulation.Play();
			}
			else
			{
				_battleSimulation.FastForward();
			}
		}
		else
		{
			Mission.Current.SetFastForwardingFromUI(base.IsFastForwarding);
		}
	}

	public override void ExecutePauseSimulationAction()
	{
		if (base.IsSimulation)
		{
			base.IsFastForwarding = false;
			if (!base.IsPaused)
			{
				_battleSimulation.Play();
			}
			else
			{
				_battleSimulation.Pause();
			}
		}
	}

	public override void ExecuteEndSimulationAction()
	{
		if (base.IsSimulation)
		{
			base.IsPaused = false;
			base.IsFastForwarding = false;
			_battleSimulation.Skip();
		}
	}

	public override void ExecuteQuitAction()
	{
		OnExitBattle();
	}

	private void GetBattleRewards(bool playerVictory)
	{
		BattleResults.Clear();
		if (playerVictory && PlayerEncounter.Current != null)
		{
			PlayerEncounter.Current.GetBattleRewards(out var renownExplained, out var influenceExplained, out var moraleExplained, out var playerEarnedLootRate, out var playerEarnedFigurehead);
			float resultNumber = renownExplained.ResultNumber;
			float resultNumber2 = influenceExplained.ResultNumber;
			float resultNumber3 = moraleExplained.ResultNumber;
			if (resultNumber > 0.1f)
			{
				BattleResults.Add(new BattleResultVM(_renownStr.Format(resultNumber), () => SandBoxUIHelper.GetExplainedNumberTooltip(ref renownExplained)));
			}
			if (resultNumber2 > 0.1f)
			{
				BattleResults.Add(new BattleResultVM(_influenceStr.Format(resultNumber2), () => SandBoxUIHelper.GetExplainedNumberTooltip(ref influenceExplained)));
			}
			if (resultNumber3 > 0.1f || resultNumber3 < -0.1f)
			{
				BattleResults.Add(new BattleResultVM(_moraleStr.Format(resultNumber3), () => SandBoxUIHelper.GetExplainedNumberTooltip(ref moraleExplained)));
			}
			int num = ((PlayerSide == BattleSideEnum.Attacker) ? base.Attackers.Parties.Count : base.Defenders.Parties.Count);
			if (playerEarnedLootRate > 0.1f && num > 1)
			{
				playerEarnedLootRate *= 100f;
				BattleResults.Add(new BattleResultVM(_lootStr.Format(playerEarnedLootRate), () => SandBoxUIHelper.GetBattleLootAwardTooltip(playerEarnedLootRate)));
			}
			if (playerEarnedFigurehead != null)
			{
				if (playerEarnedFigurehead?.Name != null)
				{
					BattleResults.Add(new BattleResultVM(_figureheadStr.SetTextVariable("A0", playerEarnedFigurehead.Name?.ToString() ?? "").ToString(), () => SandBoxUIHelper.GetFigureheadTooltip(playerEarnedFigurehead)));
				}
				else
				{
					Debug.FailedAssert("Battle rewards contain an invalid figurehead (null or name missing)", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\SPScoreboardVM.cs", "GetBattleRewards", 361);
				}
			}
		}
		foreach (SPScoreboardPartyVM party in base.Defenders.Parties)
		{
			foreach (SPScoreboardUnitVM item in party.Members.Where((SPScoreboardUnitVM member) => member.IsHero && member.Score.Dead > 0))
			{
				if (item.Character == null)
				{
					Debug.FailedAssert("Scoreboard has a member element without a character", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\SPScoreboardVM.cs", "GetBattleRewards", 378);
					continue;
				}
				BattleResults.Add(new BattleResultVM(_deadLordStr.SetTextVariable("A0", item.Character.Name).ToString(), () => new List<TooltipProperty>(), SandBoxUIHelper.GetCharacterCode(item.Character as CharacterObject)));
			}
		}
		foreach (SPScoreboardPartyVM party2 in base.Attackers.Parties)
		{
			foreach (SPScoreboardUnitVM item2 in party2.Members.Where((SPScoreboardUnitVM member) => member.IsHero && member.Score.Dead > 0))
			{
				if (item2.Character == null)
				{
					Debug.FailedAssert("Scoreboard has a member element without a character", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\SPScoreboardVM.cs", "GetBattleRewards", 395);
					continue;
				}
				BattleResults.Add(new BattleResultVM(_deadLordStr.SetTextVariable("A0", item2.Character.Name).ToString(), () => new List<TooltipProperty>(), SandBoxUIHelper.GetCharacterCode(item2.Character as CharacterObject)));
			}
		}
	}

	private void UpdateSimulationResult(bool playerVictory)
	{
		if (base.IsSimulation)
		{
			if (playerVictory)
			{
				if (PlayerEncounter.Battle.PartiesOnSide(PlayerSide).Sum((MapEventParty x) => x.Party.NumberOfHealthyMembers) < 70)
				{
					base.SimulationResult = "SimulationVictorySmall";
				}
				else
				{
					base.SimulationResult = "SimulationVictoryLarge";
				}
			}
			else
			{
				base.SimulationResult = "SimulationDefeat";
			}
		}
		else
		{
			base.SimulationResult = "NotSimulation";
		}
	}

	public void OnBattleOver()
	{
		BattleResultType battleResultType = BattleResultType.NotOver;
		if (PlayerEncounter.IsActive && PlayerEncounter.Battle != null)
		{
			base.IsOver = true;
			bool playerVictory = false;
			if (PlayerEncounter.WinningSide == PlayerSide)
			{
				battleResultType = BattleResultType.Victory;
				playerVictory = true;
			}
			else
			{
				CampaignBattleResult campaignBattleResult = PlayerEncounter.CampaignBattleResult;
				battleResultType = ((campaignBattleResult != null && campaignBattleResult.EnemyPulledBack) ? BattleResultType.Retreat : BattleResultType.Defeat);
			}
			GetBattleRewards(playerVictory);
		}
		else
		{
			Mission current = Mission.Current;
			if (current != null && current.MissionEnded)
			{
				base.IsOver = true;
				battleResultType = (((Mission.Current.HasMissionBehavior<SallyOutEndLogic>() && !Mission.Current.MissionResult.BattleResolved) || Mission.Current.MissionResult.PlayerVictory) ? BattleResultType.Victory : ((Mission.Current.MissionResult.BattleState == BattleState.DefenderPullBack && Mission.Current.PlayerTeam.Side == BattleSideEnum.Attacker) ? BattleResultType.Retreat : BattleResultType.Defeat));
			}
			else
			{
				BattleEndLogic battleEndLogic = _battleEndLogic;
				if (battleEndLogic != null && battleEndLogic.IsEnemySideRetreating)
				{
					base.IsOver = true;
				}
			}
		}
		switch (battleResultType)
		{
		case BattleResultType.Defeat:
			base.BattleResult = GameTexts.FindText("str_defeat").ToString();
			base.BattleResultIndex = (int)battleResultType;
			break;
		case BattleResultType.Victory:
			if (PlayerEncounter.Battle != null && PlayerEncounter.Battle.EndedByRetreat)
			{
				base.BattleResult = ((PlayerEncounter.Battle.RetreatingSide == BattleSideEnum.Attacker) ? GameTexts.FindText("str_attackers_retreated").ToString() : GameTexts.FindText("str_defenders_retreated").ToString());
			}
			else
			{
				base.BattleResult = GameTexts.FindText("str_victory").ToString();
			}
			base.BattleResultIndex = (int)battleResultType;
			break;
		case BattleResultType.Retreat:
			base.BattleResult = GameTexts.FindText("str_battle_result_retreat").ToString();
			base.BattleResultIndex = (int)battleResultType;
			break;
		}
		if (battleResultType != BattleResultType.NotOver)
		{
			UpdateSimulationResult(battleResultType == BattleResultType.Victory || battleResultType == BattleResultType.Retreat);
		}
	}

	public void OnExitBattle()
	{
		if (base.IsSimulation)
		{
			if (_battleSimulation.IsSimulationFinished)
			{
				_releaseSimulationSources();
				_battleSimulation.OnFinished();
				return;
			}
			Game.Current.GameStateManager.RegisterActiveStateDisableRequest(this);
			InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_order_Retreat").ToString(), GameTexts.FindText("str_retreat_question").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_ok").ToString(), GameTexts.FindText("str_cancel").ToString(), delegate
			{
				Game.Current.GameStateManager.UnregisterActiveStateDisableRequest(this);
				_releaseSimulationSources();
				_battleSimulation.OnPlayerRetreat();
			}, delegate
			{
				Game.Current.GameStateManager.UnregisterActiveStateDisableRequest(this);
			}));
			return;
		}
		BattleEndLogic missionBehavior = _mission.GetMissionBehavior<BattleEndLogic>();
		BasicMissionHandler missionBehavior2 = _mission.GetMissionBehavior<BasicMissionHandler>();
		BattleEndLogic.ExitResult exitResult = (BattleEndLogic.ExitResult)(((int?)missionBehavior?.TryExit()) ?? ((!_mission.MissionEnded) ? 1 : 3));
		switch (exitResult)
		{
		case BattleEndLogic.ExitResult.NeedsPlayerConfirmation:
		case BattleEndLogic.ExitResult.SurrenderSiege:
			OnToggle(obj: false);
			missionBehavior2.CreateWarningWidgetForResult(exitResult);
			return;
		case BattleEndLogic.ExitResult.False:
			InformationManager.ShowInquiry(_retreatInquiryData);
			return;
		}
		if (missionBehavior == null && exitResult == BattleEndLogic.ExitResult.True)
		{
			_mission.EndMission();
		}
	}

	public void TroopNumberChanged(BattleSideEnum side, IBattleCombatant battleCombatant, BasicCharacterObject character, int number = 0, int numberDead = 0, int numberWounded = 0, int numberRouted = 0, int numberKilled = 0, int numberReadyToUpgrade = 0)
	{
		bool isPlayerParty = (battleCombatant as PartyBase)?.Owner == Hero.MainHero;
		GetSide(side).UpdateScores(battleCombatant, isPlayerParty, character, number, numberDead, numberWounded, numberRouted, numberKilled, numberReadyToUpgrade);
		base.PowerComparer.Update(base.Defenders.CurrentPower, base.Attackers.CurrentPower, base.Defenders.InitialPower, base.Attackers.InitialPower);
	}

	public void HeroSkillIncreased(BattleSideEnum side, IBattleCombatant battleCombatant, BasicCharacterObject heroCharacter, SkillObject upgradedSkill)
	{
		bool isPlayerParty = (battleCombatant as PartyBase)?.Owner == Hero.MainHero;
		GetSide(side).UpdateHeroSkills(battleCombatant, isPlayerParty, heroCharacter, upgradedSkill);
	}

	public void BattleResultsReady()
	{
		if (!base.IsOver)
		{
			OnBattleOver();
		}
	}

	public void TroopSideChanged(BattleSideEnum prevSide, BattleSideEnum newSide, IBattleCombatant battleCombatant, BasicCharacterObject character)
	{
		SPScoreboardStatsVM scoreToBringOver = GetSide(prevSide).RemoveTroop(battleCombatant, character);
		GetSide(newSide).GetPartyAddIfNotExists(battleCombatant, (battleCombatant as PartyBase)?.Owner == Hero.MainHero);
		GetSide(newSide).AddTroop(battleCombatant, character, scoreToBringOver);
	}
}
