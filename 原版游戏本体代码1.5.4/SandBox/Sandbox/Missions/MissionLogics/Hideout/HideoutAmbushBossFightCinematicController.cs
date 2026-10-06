using System;
using System.Collections.Generic;
using SandBox.Objects.Cinematics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics.Hideout;

public class HideoutAmbushBossFightCinematicController : MissionLogic
{
	public delegate void OnInitialFadeOutFinished(ref Agent playerAgent, ref List<Agent> playerCompanions, ref Agent bossAgent, ref List<Agent> bossCompanions, ref float placementPerturbation, ref float placementAngle);

	public delegate void OnHideoutCinematicFinished();

	public readonly struct HideoutCinematicAgentInfo
	{
		public readonly Agent Agent;

		public readonly MatrixFrame InitialFrame;

		public readonly MatrixFrame TargetFrame;

		public readonly HideoutAgentType Type;

		public HideoutCinematicAgentInfo(Agent agent, HideoutAgentType type, in MatrixFrame initialFrame, in MatrixFrame targetFrame)
		{
			Agent = agent;
			InitialFrame = initialFrame;
			TargetFrame = targetFrame;
			Type = type;
		}

		public bool HasReachedTarget(float proximityThreshold = 0.5f)
		{
			return Agent.Position.Distance(TargetFrame.origin) <= proximityThreshold;
		}
	}

	public enum HideoutCinematicState
	{
		None,
		InitialFadeOut,
		PreCinematic,
		Cinematic,
		PostCinematic,
		Completed
	}

	public enum HideoutAgentType
	{
		Player,
		Boss,
		Ally,
		Bandit
	}

	public enum HideoutPreCinematicPhase
	{
		NotStarted,
		InitializeFormations,
		StopFormations,
		InitializeAgents,
		MoveAgents,
		Completed
	}

	public enum HideoutPostCinematicPhase
	{
		NotStarted,
		MoveAgents,
		FinalizeAgents,
		Completed
	}

	private const float AgentTargetProximityThreshold = 0.5f;

	private const float AgentMaxSpeedCinematicOverride = 0.65f;

	public const string HideoutSceneEntityTag = "hideout_boss_fight";

	public const float DefaultTransitionDuration = 0.4f;

	public const float DefaultStateDuration = 0.2f;

	public const float DefaultCinematicDuration = 8f;

	public const float DefaultPlacementPerturbation = 0.25f;

	public const float DefaultPlacementAngle = System.MathF.PI / 15f;

	private OnInitialFadeOutFinished _initialFadeOutFinished;

	private float _cinematicDuration = 8f;

	private float _stateDuration = 0.2f;

	private float _transitionDuration = 0.4f;

	private float _remainingCinematicDuration = 8f;

	private float _remainingStateDuration = 0.2f;

	private float _remainingTransitionDuration = 0.4f;

	private List<Formation> _cachedAgentFormations;

	private List<HideoutCinematicAgentInfo> _hideoutAgentsInfo;

	private HideoutCinematicAgentInfo _bossAgentInfo;

	private HideoutCinematicAgentInfo _playerAgentInfo;

	private bool _isBehaviorInit;

	private HideoutPreCinematicPhase _preCinematicPhase;

	private HideoutPostCinematicPhase _postCinematicPhase;

	private HideoutBossFightBehavior _hideoutBossFightBehavior;

	public HideoutCinematicState State { get; private set; }

	public bool InStateTransition { get; private set; }

	public bool IsCinematicActive => State != HideoutCinematicState.None;

	public float CinematicDuration => _cinematicDuration;

	public float TransitionDuration => _transitionDuration;

	public override MissionBehaviorType BehaviorType => MissionBehaviorType.Logic;

	public event Action OnCinematicFinished;

	public event Action<HideoutCinematicState> OnCinematicStateChanged;

	public event Action<HideoutCinematicState, float> OnCinematicTransition;

	public HideoutAmbushBossFightCinematicController()
	{
		State = HideoutCinematicState.None;
		InStateTransition = false;
		_isBehaviorInit = false;
	}

	public void StartCinematic(OnInitialFadeOutFinished initialFadeOutFinished, Action cinematicFinishedCallback, float transitionDuration = 0.4f, float stateDuration = 0.2f, float cinematicDuration = 8f, bool forceDismountAgents = false)
	{
		if (_isBehaviorInit && State == HideoutCinematicState.None)
		{
			OnCinematicFinished += cinematicFinishedCallback;
			_initialFadeOutFinished = initialFadeOutFinished;
			_preCinematicPhase = HideoutPreCinematicPhase.InitializeFormations;
			_postCinematicPhase = HideoutPostCinematicPhase.MoveAgents;
			_transitionDuration = transitionDuration;
			_stateDuration = stateDuration;
			_cinematicDuration = cinematicDuration;
			_remainingCinematicDuration = _cinematicDuration;
			BeginStateTransition(HideoutCinematicState.InitialFadeOut);
		}
		else if (!_isBehaviorInit)
		{
			Debug.FailedAssert("Hideout cinematic controller is not initialized.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\Hideout\\HideoutAmbushBossFightCinematicController.cs", "StartCinematic", 180);
		}
		else if (State != HideoutCinematicState.None)
		{
			Debug.FailedAssert("There is already an ongoing cinematic.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\Hideout\\HideoutAmbushBossFightCinematicController.cs", "StartCinematic", 184);
		}
	}

	public void GetBossStandingEyePosition(out Vec3 eyePosition)
	{
		if (_bossAgentInfo.Agent?.Monster != null)
		{
			eyePosition = _bossAgentInfo.InitialFrame.origin + Vec3.Up * (_bossAgentInfo.Agent.AgentScale * _bossAgentInfo.Agent.Monster.StandingEyeHeight);
			return;
		}
		eyePosition = Vec3.Zero;
		Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\Hideout\\HideoutAmbushBossFightCinematicController.cs", "GetBossStandingEyePosition", 197);
	}

	public void GetPlayerStandingEyePosition(out Vec3 eyePosition)
	{
		if (_playerAgentInfo.Agent?.Monster != null)
		{
			eyePosition = _playerAgentInfo.InitialFrame.origin + Vec3.Up * (_playerAgentInfo.Agent.AgentScale * _playerAgentInfo.Agent.Monster.StandingEyeHeight);
			return;
		}
		eyePosition = Vec3.Zero;
		Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\Hideout\\HideoutAmbushBossFightCinematicController.cs", "GetPlayerStandingEyePosition", 210);
	}

	public MatrixFrame GetBanditsInitialFrame()
	{
		_hideoutBossFightBehavior.GetBanditsInitialFrame(out var frame);
		return frame;
	}

	public void GetScenePrefabParameters(out float innerRadius, out float outerRadius, out float walkDistance)
	{
		innerRadius = 0f;
		outerRadius = 0f;
		walkDistance = 0f;
		if (_hideoutBossFightBehavior != null)
		{
			innerRadius = _hideoutBossFightBehavior.InnerRadius;
			outerRadius = _hideoutBossFightBehavior.OuterRadius;
			walkDistance = _hideoutBossFightBehavior.WalkDistance;
		}
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("hideout_boss_fight");
		_hideoutBossFightBehavior = gameEntity?.GetFirstScriptOfType<HideoutBossFightBehavior>();
		_isBehaviorInit = gameEntity != null && _hideoutBossFightBehavior != null;
	}

	public override void OnMissionTick(float dt)
	{
		if (!_isBehaviorInit || !IsCinematicActive)
		{
			return;
		}
		if (InStateTransition)
		{
			TickStateTransition(dt);
			return;
		}
		switch (State)
		{
		case HideoutCinematicState.InitialFadeOut:
			if (TickInitialFadeOut(dt))
			{
				BeginStateTransition(HideoutCinematicState.PreCinematic);
			}
			break;
		case HideoutCinematicState.PreCinematic:
			if (TickPreCinematic(dt))
			{
				BeginStateTransition(HideoutCinematicState.Cinematic);
			}
			break;
		case HideoutCinematicState.Cinematic:
			if (TickCinematic(dt))
			{
				BeginStateTransition(HideoutCinematicState.PostCinematic);
			}
			break;
		case HideoutCinematicState.PostCinematic:
			if (TickPostCinematic(dt))
			{
				BeginStateTransition(HideoutCinematicState.Completed);
			}
			break;
		case HideoutCinematicState.Completed:
			this.OnCinematicFinished?.Invoke();
			this.OnCinematicFinished = null;
			this.OnCinematicStateChanged = null;
			this.OnCinematicTransition = null;
			State = HideoutCinematicState.None;
			break;
		}
	}

	private void TickStateTransition(float dt)
	{
		_remainingTransitionDuration -= dt;
		if (_remainingTransitionDuration <= 0f)
		{
			InStateTransition = false;
			this.OnCinematicStateChanged?.Invoke(State);
			_remainingStateDuration = _stateDuration;
		}
	}

	private bool TickInitialFadeOut(float dt)
	{
		_remainingStateDuration -= dt;
		if (_remainingStateDuration <= 0f)
		{
			Agent playerAgent = null;
			Agent bossAgent = null;
			List<Agent> playerCompanions = null;
			List<Agent> bossCompanions = null;
			float placementPerturbation = 0.25f;
			float placementAngle = System.MathF.PI / 15f;
			_initialFadeOutFinished?.Invoke(ref playerAgent, ref playerCompanions, ref bossAgent, ref bossCompanions, ref placementPerturbation, ref placementAngle);
			ComputeAgentFrames(playerAgent, playerCompanions, bossAgent, bossCompanions, placementPerturbation, placementAngle);
		}
		return _remainingStateDuration <= 0f;
	}

	private bool TickPreCinematic(float dt)
	{
		Scene scene = base.Mission.Scene;
		_remainingStateDuration -= dt;
		switch (_preCinematicPhase)
		{
		case HideoutPreCinematicPhase.InitializeFormations:
		{
			_playerAgentInfo.Agent.Controller = AgentControllerType.AI;
			bool isTeleportingAgents2 = base.Mission.IsTeleportingAgents;
			base.Mission.IsTeleportingAgents = true;
			_hideoutBossFightBehavior.GetAlliesInitialFrame(out var frame);
			foreach (Formation item in base.Mission.Teams.Attacker.FormationsIncludingEmpty)
			{
				if (item.CountOfUnits > 0)
				{
					WorldPosition position = new WorldPosition(scene, frame.origin);
					item.SetMovementOrder(MovementOrder.MovementOrderMove(position));
				}
			}
			_hideoutBossFightBehavior.GetBanditsInitialFrame(out var frame2);
			foreach (Formation item2 in base.Mission.Teams.Defender.FormationsIncludingEmpty)
			{
				if (item2.CountOfUnits > 0)
				{
					WorldPosition position2 = new WorldPosition(scene, frame2.origin);
					item2.SetMovementOrder(MovementOrder.MovementOrderMove(position2));
				}
			}
			foreach (HideoutCinematicAgentInfo item3 in _hideoutAgentsInfo)
			{
				Agent agent3 = item3.Agent;
				agent3.SetMovementDirection((agent3.LookDirection = item3.InitialFrame.rotation.f).AsVec2.Normalized());
			}
			base.Mission.IsTeleportingAgents = isTeleportingAgents2;
			_preCinematicPhase = HideoutPreCinematicPhase.StopFormations;
			break;
		}
		case HideoutPreCinematicPhase.StopFormations:
			foreach (Formation item4 in base.Mission.Teams.Attacker.FormationsIncludingEmpty)
			{
				if (item4.CountOfUnits > 0)
				{
					item4.SetMovementOrder(MovementOrder.MovementOrderStop);
				}
			}
			foreach (Formation item5 in base.Mission.Teams.Defender.FormationsIncludingEmpty)
			{
				if (item5.CountOfUnits > 0)
				{
					item5.SetMovementOrder(MovementOrder.MovementOrderStop);
				}
			}
			_preCinematicPhase = HideoutPreCinematicPhase.InitializeAgents;
			break;
		case HideoutPreCinematicPhase.InitializeAgents:
		{
			bool isTeleportingAgents = base.Mission.IsTeleportingAgents;
			base.Mission.IsTeleportingAgents = true;
			_cachedAgentFormations = new List<Formation>();
			foreach (HideoutCinematicAgentInfo item6 in _hideoutAgentsInfo)
			{
				Agent agent2 = item6.Agent;
				_cachedAgentFormations.Add(agent2.Formation);
				agent2.Formation = null;
				MatrixFrame initialFrame = item6.InitialFrame;
				WorldPosition worldPosition = new WorldPosition(scene, initialFrame.origin);
				Vec3 f = initialFrame.rotation.f;
				agent2.TeleportToPosition(worldPosition.GetGroundVec3());
				agent2.LookDirection = f;
				agent2.SetMovementDirection(f.AsVec2.Normalized());
			}
			base.Mission.IsTeleportingAgents = isTeleportingAgents;
			_preCinematicPhase = HideoutPreCinematicPhase.MoveAgents;
			break;
		}
		case HideoutPreCinematicPhase.MoveAgents:
			foreach (HideoutCinematicAgentInfo item7 in _hideoutAgentsInfo)
			{
				Agent agent = item7.Agent;
				MatrixFrame targetFrame = item7.TargetFrame;
				WorldPosition scriptedPosition = new WorldPosition(scene, targetFrame.origin);
				agent.SetMaximumSpeedLimit(0.65f, isMultiplier: false);
				agent.SetScriptedPositionAndDirection(ref scriptedPosition, targetFrame.rotation.f.AsVec2.RotationInRadians, addHumanLikeDelay: true);
			}
			_preCinematicPhase = HideoutPreCinematicPhase.Completed;
			break;
		}
		if (_preCinematicPhase == HideoutPreCinematicPhase.Completed)
		{
			return _remainingStateDuration <= 0f;
		}
		return false;
	}

	private bool TickCinematic(float dt)
	{
		_remainingCinematicDuration -= dt;
		_remainingStateDuration -= dt;
		if (_remainingCinematicDuration <= 0f && _remainingStateDuration <= 0f)
		{
			return true;
		}
		return false;
	}

	private bool TickPostCinematic(float dt)
	{
		_remainingStateDuration -= dt;
		switch (_postCinematicPhase)
		{
		case HideoutPostCinematicPhase.MoveAgents:
		{
			int num = 0;
			foreach (HideoutCinematicAgentInfo item in _hideoutAgentsInfo)
			{
				Agent agent2 = item.Agent;
				if (!item.HasReachedTarget())
				{
					MatrixFrame targetFrame = item.TargetFrame;
					agent2.TeleportToPosition(new WorldPosition(base.Mission.Scene, targetFrame.origin).GetGroundVec3());
					agent2.SetMovementDirection(targetFrame.rotation.f.AsVec2.Normalized());
				}
				agent2.Formation = _cachedAgentFormations[num];
				num++;
			}
			_postCinematicPhase = HideoutPostCinematicPhase.FinalizeAgents;
			break;
		}
		case HideoutPostCinematicPhase.FinalizeAgents:
			foreach (HideoutCinematicAgentInfo item2 in _hideoutAgentsInfo)
			{
				Agent agent = item2.Agent;
				agent.DisableScriptedMovement();
				agent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
			}
			_postCinematicPhase = HideoutPostCinematicPhase.Completed;
			break;
		}
		if (_postCinematicPhase == HideoutPostCinematicPhase.Completed)
		{
			return _remainingStateDuration <= 0f;
		}
		return false;
	}

	private void BeginStateTransition(HideoutCinematicState nextState)
	{
		State = nextState;
		_remainingTransitionDuration = _transitionDuration;
		InStateTransition = true;
		this.OnCinematicTransition?.Invoke(State, _remainingTransitionDuration);
	}

	private void ComputeAgentFrames(Agent playerAgent, List<Agent> playerCompanions, Agent bossAgent, List<Agent> bossCompanions, float placementPerturbation, float placementAngle)
	{
		_hideoutAgentsInfo = new List<HideoutCinematicAgentInfo>();
		_hideoutBossFightBehavior.GetPlayerFrames(out var initialFrame, out var targetFrame, placementPerturbation);
		_playerAgentInfo = new HideoutCinematicAgentInfo(playerAgent, HideoutAgentType.Player, in initialFrame, in targetFrame);
		_hideoutAgentsInfo.Add(_playerAgentInfo);
		GetAllyFrames(out var initialFrames, out var targetFrames, _playerAgentInfo.InitialFrame, _playerAgentInfo.TargetFrame, playerCompanions.Count, placementAngle);
		for (int i = 0; i < playerCompanions.Count; i++)
		{
			initialFrame = initialFrames[i];
			targetFrame = targetFrames[i];
			_hideoutAgentsInfo.Add(new HideoutCinematicAgentInfo(playerCompanions[i], HideoutAgentType.Ally, in initialFrame, in targetFrame));
		}
		_hideoutBossFightBehavior.GetBossFrames(out initialFrame, out targetFrame, placementPerturbation);
		_bossAgentInfo = new HideoutCinematicAgentInfo(bossAgent, HideoutAgentType.Boss, in initialFrame, in targetFrame);
		_hideoutAgentsInfo.Add(_bossAgentInfo);
		GetBanditFrames(out initialFrames, out targetFrames, _bossAgentInfo.InitialFrame, _bossAgentInfo.TargetFrame, bossCompanions.Count, placementAngle);
		for (int j = 0; j < bossCompanions.Count; j++)
		{
			initialFrame = initialFrames[j];
			targetFrame = targetFrames[j];
			_hideoutAgentsInfo.Add(new HideoutCinematicAgentInfo(bossCompanions[j], HideoutAgentType.Bandit, in initialFrame, in targetFrame));
		}
	}

	public void GetAllyFrames(out List<MatrixFrame> initialFrames, out List<MatrixFrame> targetFrames, MatrixFrame initialPlayerFrame, MatrixFrame targetPlayerFrame, int agentCount, float agentOffsetAngle)
	{
		initialFrames = new List<MatrixFrame>();
		targetFrames = new List<MatrixFrame>();
		MatrixFrame[] array = new MatrixFrame[GetSpineTroopCount(agentCount)];
		for (int i = 0; i < array.Length; i++)
		{
			int num = i + 1;
			array[i] = new MatrixFrame(in initialPlayerFrame.rotation, new Vec3(initialPlayerFrame.origin.x, initialPlayerFrame.origin.y - 1.3f * (float)num, initialPlayerFrame.origin.z));
		}
		for (int j = 0; j < array.Length; j++)
		{
			int num2 = j + 1;
			initialFrames.Add(array[j]);
			int num3 = num2;
			int num4 = num2;
			for (int k = 0; k < num3; k++)
			{
				initialFrames.Add(new MatrixFrame(in array[j].rotation, new Vec3(array[j].origin.x - 1f * (float)(k + 1), array[j].origin.y, array[j].origin.z)));
			}
			for (int l = 0; l < num4; l++)
			{
				initialFrames.Add(new MatrixFrame(in array[j].rotation, new Vec3(array[j].origin.x + 1f * (float)(l + 1), array[j].origin.y, array[j].origin.z)));
			}
		}
		foreach (MatrixFrame initialFrame in initialFrames)
		{
			MatrixFrame current = initialFrame;
			targetFrames.Add(new MatrixFrame(in current.rotation, new Vec3(current.origin.x, current.origin.y - 0.5f, current.origin.z)));
		}
	}

	public int GetSpineTroopCount(int totalTroopCount)
	{
		if (totalTroopCount <= 0)
		{
			return 1;
		}
		int num = -totalTroopCount;
		int num2 = TaleWorlds.Library.MathF.Ceiling((-2f + TaleWorlds.Library.MathF.Sqrt(4 - 4 * num)) / 2f);
		if (num2 < 1)
		{
			num2 = 1;
		}
		return num2;
	}

	public void GetBanditFrames(out List<MatrixFrame> initialFrames, out List<MatrixFrame> targetFrames, MatrixFrame initialBossFrame, MatrixFrame targetBossFrame, int agentCount, float agentOffsetAngle)
	{
		initialFrames = new List<MatrixFrame>();
		targetFrames = new List<MatrixFrame>();
		MatrixFrame[] array = new MatrixFrame[GetSpineTroopCount(agentCount)];
		for (int i = 0; i < array.Length; i++)
		{
			int num = i + 1;
			array[i] = new MatrixFrame(in initialBossFrame.rotation, new Vec3(initialBossFrame.origin.x, initialBossFrame.origin.y + 1.2f * (float)num, initialBossFrame.origin.z));
		}
		for (int j = 0; j < array.Length; j++)
		{
			int num2 = j + 1;
			initialFrames.Add(array[j]);
			int num3 = num2;
			int num4 = num2;
			for (int k = 0; k < num3; k++)
			{
				initialFrames.Add(new MatrixFrame(in array[j].rotation, new Vec3(array[j].origin.x - 1f * (float)(k + 1), array[j].origin.y, array[j].origin.z)));
			}
			for (int l = 0; l < num4; l++)
			{
				initialFrames.Add(new MatrixFrame(in array[j].rotation, new Vec3(array[j].origin.x + 1f * (float)(l + 1), array[j].origin.y, array[j].origin.z)));
			}
		}
		foreach (MatrixFrame initialFrame in initialFrames)
		{
			MatrixFrame current = initialFrame;
			targetFrames.Add(new MatrixFrame(in current.rotation, new Vec3(current.origin.x, current.origin.y - 0.5f, current.origin.z)));
		}
	}
}
