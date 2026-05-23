using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public abstract class TeamAIComponent
{
	protected class TacticOption
	{
		public string Id { get; private set; }

		public Lazy<TacticComponent> Tactic { get; private set; }

		public float Weight { get; set; }

		public TacticOption(string id, Lazy<TacticComponent> tactic, float weight)
		{
			Id = id;
			Tactic = tactic;
			Weight = weight;
		}
	}

	public delegate void TacticalDecisionDelegate(in TacticalDecision decision);

	public TacticalDecisionDelegate OnNotifyTacticalDecision;

	public const int BattleTokenForceSize = 10;

	private readonly List<TacticComponent> _availableTactics;

	private static bool _retreatScriptActive;

	protected readonly Mission Mission;

	protected readonly Team Team;

	private readonly Timer _thinkTimer;

	private readonly Timer _applyTimer;

	private TacticComponent _currentTactic;

	public List<TacticalPosition> TacticalPositions;

	public List<TacticalRegion> TacticalRegions;

	private readonly MBList<StrategicArea> _strategicAreas;

	private readonly float _occasionalTickTime;

	private MissionTime _nextTacticChooseTime;

	private MissionTime _nextOccasionalTickTime;

	public MBReadOnlyList<StrategicArea> StrategicAreas => _strategicAreas;

	public bool HasStrategicAreas => !_strategicAreas.IsEmpty();

	public bool IsDefenseApplicable { get; private set; }

	public bool GetIsFirstTacticChosen { get; private set; }

	protected TacticComponent CurrentTactic
	{
		get
		{
			return _currentTactic;
		}
		private set
		{
			_currentTactic?.OnCancel();
			_currentTactic = value;
			if (_currentTactic != null)
			{
				_currentTactic.OnApply();
				_currentTactic.TickOccasionally();
			}
		}
	}

	protected TeamAIComponent(Mission currentMission, Team currentTeam, float thinkTimerTime, float applyTimerTime)
	{
		Mission = currentMission;
		Team = currentTeam;
		_thinkTimer = new Timer(Mission.CurrentTime, thinkTimerTime);
		_applyTimer = new Timer(Mission.CurrentTime, applyTimerTime);
		_occasionalTickTime = applyTimerTime;
		_availableTactics = new List<TacticComponent>();
		TacticalPositions = currentMission.ActiveMissionObjects.FindAllWithType<TacticalPosition>().ToList();
		TacticalRegions = currentMission.ActiveMissionObjects.FindAllWithType<TacticalRegion>().ToList();
		_strategicAreas = (from amo in currentMission.ActiveMissionObjects
			where amo is StrategicArea { IsActive: not false } strategicArea && strategicArea.IsUsableBy(Team.Side)
			select amo as StrategicArea).ToMBList();
	}

	public void AddStrategicArea(StrategicArea strategicArea)
	{
		_strategicAreas.Add(strategicArea);
	}

	public void RemoveStrategicArea(StrategicArea strategicArea)
	{
		if (Team.DetachmentManager.ContainsDetachment(strategicArea))
		{
			Team.DetachmentManager.DestroyDetachment(strategicArea);
		}
		_strategicAreas.Remove(strategicArea);
	}

	public void RemoveAllStrategicAreas()
	{
		foreach (StrategicArea strategicArea in _strategicAreas)
		{
			if (Team.DetachmentManager.ContainsDetachment(strategicArea))
			{
				Team.DetachmentManager.DestroyDetachment(strategicArea);
			}
		}
		_strategicAreas.Clear();
	}

	public void AddTacticOption(TacticComponent tacticOption)
	{
		_availableTactics.Add(tacticOption);
	}

	public void RemoveTacticOption(Type tacticType)
	{
		_availableTactics.RemoveAll((TacticComponent at) => tacticType == at.GetType());
	}

	public void ClearTacticOptions()
	{
		_availableTactics.Clear();
	}

	[Conditional("DEBUG")]
	public void AssertTeam(Team team)
	{
	}

	public void NotifyTacticalDecision(in TacticalDecision decision)
	{
		OnNotifyTacticalDecision?.Invoke(in decision);
	}

	public virtual void OnDeploymentFinished()
	{
	}

	public virtual void OnFormationFrameChanged(Agent agent, bool isFrameEnabled, WorldPosition frame)
	{
	}

	public virtual void OnMissionEnded()
	{
		MBDebug.Print("Mission end received by teamAI");
		foreach (Formation item in Team.FormationsIncludingSpecialAndEmpty)
		{
			if (item.CountOfUnits <= 0)
			{
				continue;
			}
			foreach (UsableMachine item2 in item.GetUsedMachines().ToList())
			{
				item.StopUsingMachine(item2);
			}
		}
	}

	public void ResetTacticalPositions()
	{
		TacticalPositions = Mission.ActiveMissionObjects.FindAllWithType<TacticalPosition>().ToList();
		TacticalRegions = Mission.ActiveMissionObjects.FindAllWithType<TacticalRegion>().ToList();
	}

	public void ResetTactic(bool keepCurrentTactic = true)
	{
		if (!keepCurrentTactic)
		{
			CurrentTactic = null;
		}
		_thinkTimer.Reset(Mission.CurrentTime);
		_applyTimer.Reset(Mission.CurrentTime);
		MakeDecision();
		TickOccasionally();
	}

	protected internal virtual void Tick(float dt)
	{
		if (Team.BodyGuardFormation != null && Team.BodyGuardFormation.CountOfUnits > 0 && (Team.GeneralsFormation == null || Team.GeneralsFormation.CountOfUnits == 0))
		{
			Team.BodyGuardFormation.AI.ResetBehaviorWeights();
			Team.BodyGuardFormation.AI.SetBehaviorWeight<BehaviorCharge>(1f);
		}
		if (_nextTacticChooseTime.IsPast)
		{
			MakeDecision();
			_nextTacticChooseTime = MissionTime.SecondsFromNow(5f);
		}
		if (_nextOccasionalTickTime.IsPast)
		{
			TickOccasionally();
			_nextOccasionalTickTime = MissionTime.SecondsFromNow(_occasionalTickTime);
		}
	}

	public void CheckIsDefenseApplicable()
	{
		if (Team.Side != BattleSideEnum.Defender)
		{
			IsDefenseApplicable = false;
			return;
		}
		int memberCount = Team.QuerySystem.MemberCount;
		float maxUnderRangedAttackRatio = Team.QuerySystem.MaxUnderRangedAttackRatio;
		float num = (float)memberCount * maxUnderRangedAttackRatio;
		int deathByRangedCount = Team.QuerySystem.DeathByRangedCount;
		int deathCount = Team.QuerySystem.DeathCount;
		float num2 = MBMath.ClampFloat((num + (float)deathByRangedCount) / (float)(memberCount + deathCount), 0.05f, 1f);
		int enemyUnitCount = Team.QuerySystem.EnemyUnitCount;
		float num3 = 0f;
		int num4 = 0;
		int num5 = 0;
		foreach (Team team in Mission.Teams)
		{
			if (Team.IsEnemyOf(team))
			{
				TeamQuerySystem querySystem = team.QuerySystem;
				num4 += querySystem.DeathByRangedCount;
				num5 += querySystem.DeathCount;
				num3 += ((enemyUnitCount == 0) ? 0f : (querySystem.MaxUnderRangedAttackRatio * ((float)querySystem.MemberCount / (float)((enemyUnitCount <= 0) ? 1 : enemyUnitCount))));
			}
		}
		float num6 = (float)enemyUnitCount * num3;
		int num7 = enemyUnitCount + num5;
		float num8 = MBMath.ClampFloat((num6 + (float)num4) / (float)((num7 <= 0) ? 1 : num7), 0.05f, 1f);
		float num9 = TaleWorlds.Library.MathF.Pow(num2 / num8, 3f * (Team.QuerySystem.EnemyRangedRatio + Team.QuerySystem.EnemyRangedCavalryRatio));
		IsDefenseApplicable = num9 <= 1.5f;
	}

	public void OnTacticAppliedForFirstTime()
	{
		GetIsFirstTacticChosen = false;
	}

	private void MakeDecision()
	{
		List<TacticComponent> availableTactics = _availableTactics;
		if ((Mission.CurrentState != Mission.State.Continuing && availableTactics.Count == 0) || !Team.HasAnyFormationsIncludingSpecialThatIsNotEmpty())
		{
			return;
		}
		bool flag = true;
		foreach (Team team in Mission.Teams)
		{
			if (team.IsEnemyOf(Team) && team.HasAnyFormationsIncludingSpecialThatIsNotEmpty())
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			if (Mission.MissionEnded)
			{
				return;
			}
			if (!(CurrentTactic is TacticCharge))
			{
				foreach (TacticComponent item in availableTactics)
				{
					if (item is TacticCharge)
					{
						if (CurrentTactic == null)
						{
							GetIsFirstTacticChosen = true;
						}
						CurrentTactic = item;
						break;
					}
				}
				if (!(CurrentTactic is TacticCharge))
				{
					if (CurrentTactic == null)
					{
						GetIsFirstTacticChosen = true;
					}
					CurrentTactic = availableTactics.FirstOrDefault();
				}
			}
		}
		CheckIsDefenseApplicable();
		TacticComponent tacticComponent = TaleWorlds.Core.Extensions.MaxBy(availableTactics, (TacticComponent to) => to.GetTacticWeight() * ((to == _currentTactic) ? 1.5f : 1f));
		bool flag2 = false;
		if (CurrentTactic == null)
		{
			flag2 = true;
		}
		else if (CurrentTactic != tacticComponent)
		{
			if (!CurrentTactic.ResetTacticalPositions())
			{
				flag2 = true;
			}
			else
			{
				float tacticWeight = tacticComponent.GetTacticWeight();
				float num = CurrentTactic.GetTacticWeight() * 1.5f;
				if (tacticWeight > num)
				{
					flag2 = true;
				}
			}
		}
		if (flag2)
		{
			if (CurrentTactic == null)
			{
				GetIsFirstTacticChosen = true;
			}
			CurrentTactic = tacticComponent;
			if (Mission.Current.MainAgent != null && Team.GeneralAgent != null && Team.IsPlayerTeam && Team.IsPlayerSergeant)
			{
				string name = tacticComponent.GetType().Name;
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_team_ai_tactic_text", name), 4000, Team.GeneralAgent.Character);
			}
		}
	}

	public virtual void TickOccasionally()
	{
		if (Mission.Current.AllowAiTicking && Team.HasBots)
		{
			CurrentTactic?.TickOccasionally();
		}
	}

	public bool IsCurrentTactic(TacticComponent tactic)
	{
		return tactic == CurrentTactic;
	}

	[Conditional("DEBUG")]
	protected virtual void DebugTick(float dt)
	{
		if (MBDebug.IsDisplayingHighLevelAI)
		{
			_ = CurrentTactic;
			if (Input.DebugInput.IsHotKeyPressed("UsableMachineAiBaseHotkeyRetreatScriptActive"))
			{
				_retreatScriptActive = true;
			}
			else if (Input.DebugInput.IsHotKeyPressed("UsableMachineAiBaseHotkeyRetreatScriptPassive"))
			{
				_retreatScriptActive = false;
			}
			_ = _retreatScriptActive;
		}
	}

	public abstract void OnUnitAddedToFormationForTheFirstTime(Formation formation);

	protected internal virtual void CreateMissionSpecificBehaviors()
	{
	}

	protected internal virtual void InitializeDetachments(Mission mission)
	{
		Mission.GetMissionBehavior<DeploymentHandler>()?.InitializeDeploymentPoints();
	}
}
