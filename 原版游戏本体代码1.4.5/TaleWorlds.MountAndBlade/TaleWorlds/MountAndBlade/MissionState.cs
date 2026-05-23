using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromClient;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;

namespace TaleWorlds.MountAndBlade;

public class MissionState : GameState
{
	private const int MissionFastForwardSpeedMultiplier = 10;

	private bool _missionInitializing;

	private int _tickCountBeforeLoad;

	public static bool RecordMission;

	public float MissionReplayStartTime;

	public float MissionEndTime;

	private bool _isDelayedDisconnecting;

	private int _missionTickCount;

	public IMissionSystemHandler Handler { get; set; }

	public static MissionState Current { get; private set; }

	public Mission CurrentMission { get; private set; }

	public string MissionName { get; private set; }

	public bool FirstMissionTickAfterLoading { get; private set; }

	public bool Paused { get; set; }

	protected override void OnInitialize()
	{
		base.OnInitialize();
		Current = this;
		FirstMissionTickAfterLoading = true;
		LoadingWindow.EnableGlobalLoadingWindow();
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		CurrentMission.OnMissionStateFinalize(CurrentMission.NeedsMemoryCleanup);
		CurrentMission = null;
		Current = null;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		CurrentMission.OnMissionStateActivate();
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		CurrentMission.OnMissionStateDeactivate();
	}

	protected override void OnIdleTick(float dt)
	{
		base.OnIdleTick(dt);
		if (CurrentMission != null && CurrentMission.CurrentState == Mission.State.Continuing)
		{
			CurrentMission.IdleTick(dt);
		}
	}

	protected override void OnTick(float realDt)
	{
		base.OnTick(realDt);
		if (_isDelayedDisconnecting && CurrentMission != null && CurrentMission.CurrentState == Mission.State.Continuing)
		{
			BannerlordNetwork.EndMultiplayerLobbyMission();
		}
		if (CurrentMission == null)
		{
			return;
		}
		if (CurrentMission.CurrentState == Mission.State.NewlyCreated || CurrentMission.CurrentState == Mission.State.Initializing)
		{
			if (CurrentMission.CurrentState == Mission.State.NewlyCreated)
			{
				CurrentMission.ClearUnreferencedResources(CurrentMission.NeedsMemoryCleanup);
			}
			TickLoading(realDt);
		}
		else if (CurrentMission.CurrentState == Mission.State.Continuing || CurrentMission.MissionEnded)
		{
			if (MissionReplayStartTime != 0f)
			{
				CurrentMission.SkipForwardMissionReplay(MissionReplayStartTime, 0.033f);
				MissionReplayStartTime = 0f;
			}
			bool flag = false;
			if (MissionEndTime != 0f && CurrentMission.CurrentTime > MissionEndTime)
			{
				CurrentMission.EndMission();
				flag = true;
			}
			if (!flag && (Handler == null || Handler.RenderIsReady()))
			{
				TickMission(realDt);
			}
			if (flag && MBEditor._isEditorMissionOn)
			{
				MBEditor.LeaveEditMissionMode();
				TickMission(realDt);
			}
		}
		if (CurrentMission.CurrentState == Mission.State.Over)
		{
			if (MBGameManager.Current.IsEnding)
			{
				Game.Current.GameStateManager.CleanStates();
			}
			else
			{
				Game.Current.GameStateManager.PopState();
			}
		}
	}

	private void TickMission(float realDt)
	{
		if (FirstMissionTickAfterLoading && CurrentMission != null && CurrentMission.CurrentState == Mission.State.Continuing && GameNetwork.IsClient)
		{
			int currentBattleIndex = GameNetwork.GetNetworkComponent<BaseNetworkComponentData>().CurrentBattleIndex;
			MBDebug.Print($"Client: I finished loading battle with index: {currentBattleIndex}. Sending confirmation to server.", 0, Debug.DebugColor.White, 17179869184uL);
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new FinishedLoading(currentBattleIndex));
			GameNetwork.EndModuleEventAsClient();
			GameNetwork.SyncRelevantGameOptionsToServer();
		}
		Handler?.BeforeMissionTick(CurrentMission, realDt);
		CurrentMission.PauseAITick = false;
		if (GameNetwork.IsSessionActive && CurrentMission.ClearSceneTimerElapsedTime < 0f)
		{
			CurrentMission.PauseAITick = true;
		}
		float num = realDt;
		if (Paused || MBCommon.IsPaused)
		{
			num = 0f;
		}
		else if (CurrentMission.FixedDeltaTimeMode)
		{
			num = CurrentMission.FixedDeltaTime;
		}
		if (!GameNetwork.IsSessionActive)
		{
			CurrentMission.UpdateSceneTimeSpeed();
			float timeSpeed = CurrentMission.Scene.TimeSpeed;
			num *= timeSpeed;
		}
		if (CurrentMission.ClearSceneTimerElapsedTime < -0.3f && !GameNetwork.IsClientOrReplay)
		{
			CurrentMission.ClearAgentActions();
		}
		if (CurrentMission.CurrentState == Mission.State.Continuing || CurrentMission.MissionEnded)
		{
			if (CurrentMission.IsFastForward)
			{
				float num2 = num * 9f;
				while (num2 > 1E-06f)
				{
					if (num2 > 0.1f)
					{
						TickMissionAux(0.1f, 0.1f, updateCamera: false, asyncAITick: false);
						if (CurrentMission.CurrentState == Mission.State.Over)
						{
							break;
						}
						num2 -= 0.1f;
					}
					else
					{
						if (num2 > 0.0033333334f)
						{
							TickMissionAux(num2, num2, updateCamera: false, asyncAITick: false);
						}
						num2 = 0f;
					}
				}
				if (CurrentMission.CurrentState != Mission.State.Over)
				{
					TickMissionAux(num, realDt, updateCamera: true, asyncAITick: false);
				}
			}
			else
			{
				TickMissionAux(num, realDt, updateCamera: true, asyncAITick: true);
			}
		}
		if (Handler != null)
		{
			Handler.AfterMissionTick(CurrentMission, realDt);
		}
		FirstMissionTickAfterLoading = false;
		_missionTickCount++;
	}

	private void TickMissionAux(float dt, float realDt, bool updateCamera, bool asyncAITick)
	{
		CurrentMission.Tick(dt);
		if (_missionTickCount > 2)
		{
			CurrentMission.OnTick(dt, realDt, updateCamera, asyncAITick);
		}
	}

	private void TickLoading(float realDt)
	{
		_tickCountBeforeLoad++;
		if (!_missionInitializing && _tickCountBeforeLoad > 0)
		{
			LoadMission();
			Utilities.SetLoadingScreenPercentage(0.01f);
		}
		else if (_missionInitializing && CurrentMission.IsLoadingFinished)
		{
			FinishMissionLoading();
		}
	}

	private void LoadMission()
	{
		foreach (MissionBehavior missionBehavior in CurrentMission.MissionBehaviors)
		{
			missionBehavior.OnMissionScreenPreLoad();
		}
		Utilities.ClearOldResourcesAndObjects();
		_missionInitializing = true;
		CurrentMission.Initialize();
	}

	private void CreateMission(MissionInitializerRecord rec, bool needsMemoryCleanup)
	{
		CurrentMission = new Mission(rec, this, needsMemoryCleanup);
	}

	protected Mission HandleOpenNew(string missionName, MissionInitializerRecord rec, InitializeMissionBehaviorsDelegate handler, bool addDefaultMissionBehaviors, bool needsMemoryCleanup)
	{
		MissionName = missionName;
		CreateMission(rec, needsMemoryCleanup);
		IEnumerable<MissionBehavior> source = handler(CurrentMission);
		source = source.Where((MissionBehavior behavior) => behavior != null);
		if (addDefaultMissionBehaviors)
		{
			source = AddDefaultMissionBehaviorsTo(CurrentMission, source);
		}
		foreach (MissionBehavior item in source)
		{
			item.OnAfterMissionCreated();
		}
		AddBehaviorsToMission(source);
		if (Handler != null)
		{
			source = new MissionBehavior[0];
			source = Handler.OnAddBehaviors(source, CurrentMission, missionName, addDefaultMissionBehaviors);
			AddBehaviorsToMission(source);
		}
		if (GameNetwork.IsDedicatedServer)
		{
			GameNetwork.SetServerFrameRate(Module.CurrentModule.StartupInfo.ServerTickRate);
		}
		return CurrentMission;
	}

	private void AddBehaviorsToMission(IEnumerable<MissionBehavior> behaviors)
	{
		MissionLogic[] logicBehaviors = (from behavior in behaviors.OfType<MissionLogic>()
			where !(behavior is MissionNetwork)
			select behavior).ToArray();
		MissionBehavior[] otherBehaviors = behaviors.Where((MissionBehavior behavior) => behavior != null && !(behavior is MissionNetwork) && !(behavior is MissionLogic)).ToArray();
		MissionNetwork[] networkBehaviors = behaviors.OfType<MissionNetwork>().ToArray();
		CurrentMission.InitializeStartingBehaviors(logicBehaviors, otherBehaviors, networkBehaviors);
	}

	protected static bool IsRecordingActive()
	{
		if (GameNetwork.IsServer)
		{
			return MultiplayerOptions.OptionType.EnableMissionRecording.GetBoolValue();
		}
		if (RecordMission)
		{
			return Game.Current.GameType.IsCoreOnlyGameMode;
		}
		return false;
	}

	public static Mission OpenNew(string missionName, MissionInitializerRecord rec, InitializeMissionBehaviorsDelegate handler, bool addDefaultMissionBehaviors = true, bool needsMemoryCleanup = true)
	{
		Debug.Print("Opening new mission " + missionName + " " + rec.SceneLevels + ".\n");
		if (!GameNetwork.IsClientOrReplay && !GameNetwork.IsServer)
		{
			MBCommon.CurrentGameType = (IsRecordingActive() ? MBCommon.GameType.SingleRecord : MBCommon.GameType.Single);
		}
		Game.Current.OnMissionIsStarting(missionName, rec);
		MissionState missionState = Game.Current.GameStateManager.CreateState<MissionState>();
		Mission result = missionState.HandleOpenNew(missionName, rec, handler, addDefaultMissionBehaviors, needsMemoryCleanup);
		Game.Current.GameStateManager.PushState(missionState);
		return result;
	}

	private static IEnumerable<MissionBehavior> AddDefaultMissionBehaviorsTo(Mission mission, IEnumerable<MissionBehavior> behaviors)
	{
		List<MissionBehavior> list = new List<MissionBehavior>();
		if (GameNetwork.IsSessionActive || GameNetwork.IsReplay)
		{
			list.Add(new MissionNetworkComponent());
		}
		if (IsRecordingActive() && !GameNetwork.IsReplay)
		{
			list.Add(new RecordMissionLogic());
		}
		list.Add(new BasicMissionHandler());
		list.Add(new CasualtyHandler());
		list.Add(new AgentCommonAILogic());
		return list.Concat(behaviors);
	}

	private void FinishMissionLoading()
	{
		_missionInitializing = false;
		CurrentMission.Scene.SetOwnerThread();
		Utilities.SetLoadingScreenPercentage(0.4f);
		for (int i = 0; i < 2; i++)
		{
			CurrentMission.Tick(0.001f);
		}
		Utilities.SetLoadingScreenPercentage(0.42f);
		Handler?.OnMissionAfterStarting(CurrentMission);
		Utilities.SetLoadingScreenPercentage(0.48f);
		CurrentMission.AfterStart();
		Utilities.SetLoadingScreenPercentage(0.56f);
		Handler?.OnMissionLoadingFinished(CurrentMission);
		Utilities.SetLoadingScreenPercentage(0.62f);
		CurrentMission.Scene.ResumeLoadingRenderings();
	}

	public void BeginDelayedDisconnectFromMission()
	{
		_isDelayedDisconnecting = true;
	}
}
