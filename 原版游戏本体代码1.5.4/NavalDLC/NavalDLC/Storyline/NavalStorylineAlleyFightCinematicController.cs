using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Hints;
using TaleWorlds.MountAndBlade.Missions.MissionLogics;

namespace NavalDLC.Storyline;

public class NavalStorylineAlleyFightCinematicController : MissionLogic
{
	public enum NavalAlleyFightCinematicState
	{
		Ready,
		InitialFadeOut,
		BlackScreen,
		InitialFadeIn,
		FirstCamera,
		FinalCamera,
		Completed
	}

	private class ConversationLine
	{
		public TextObject Line;

		public CharacterObject Speaker;

		public MBInformationManager.DialogNotificationHandle Handle;

		public ConversationLine(TextObject line, CharacterObject speaker)
		{
			Line = line;
			Speaker = speaker;
		}
	}

	private const float CinematicTriggerRadius = 3f;

	private const float FadeDuration = 0.75f;

	private const float BlackScreenDuration = 0.25f;

	private const float FirstCameraDuration = 10f;

	private const int SkipHotKey = 14;

	private bool _isMissionInitialized;

	private List<GameEntity> _entities = new List<GameEntity>();

	private GameEntity _currentCameraEntity;

	private GameEntity _cameraEntity;

	private GameEntity _cameraEntity2;

	private GameEntity _cinematicTriggerZone;

	private NavalAlleyFightCinematicState _currentCinematicState;

	private float _cinematicTimer;

	private NavalStorylineAlleyFightMissionController _missionController;

	private MissionHintLogic _missionHintLogic;

	private List<ConversationLine> _allLines;

	private CharacterObject _enemyCharacterObject;

	private bool _isPostFightConversationQueued;

	private float _postFightDialogueFadeTimer;

	private bool _isConversationSetup;

	private const float PostFightDialogueFadeOutDuration = 0.75f;

	private const float PostFightDialogueBlackDuration = 1f;

	private const float PostFightDialogueFadeInDuration = 0.75f;

	private TextObject SkipHintText => new TextObject("{=FiSENWMB}Skip Cinematic");

	public event Action<NavalAlleyFightCinematicState> OnCinematicStateChanged;

	public event Action<float, float, float> OnFightEndedEvent;

	public event Action<Vec3> OnConversationSetupEvent;

	public override void OnMissionTick(float dt)
	{
		if (!_isMissionInitialized)
		{
			Initialize();
		}
		TickCinematic(dt);
		if (_isPostFightConversationQueued)
		{
			_postFightDialogueFadeTimer += dt;
			if (!_isConversationSetup && _postFightDialogueFadeTimer >= 0.75f)
			{
				_isConversationSetup = true;
				_missionController.SetupConversation();
			}
			if (_postFightDialogueFadeTimer >= 1.75f)
			{
				_isPostFightConversationQueued = false;
				_missionController.StartPostFightConversation();
			}
		}
	}

	private void Initialize()
	{
		_isMissionInitialized = true;
		UpdateEntityReferences();
		_missionController = base.Mission.GetMissionBehavior<NavalStorylineAlleyFightMissionController>();
		_missionHintLogic = base.Mission.GetMissionBehavior<MissionHintLogic>();
		_cinematicTriggerZone = _entities.FirstOrDefault((GameEntity t) => t.HasTag("trigger_cutscene"));
		_cameraEntity = _entities.FirstOrDefault((GameEntity t) => t.HasTag("sp_camera"));
		_cameraEntity2 = _entities.FirstOrDefault((GameEntity t) => t.HasTag("sp_camera_2"));
		_currentCameraEntity = _cameraEntity;
		SoundManager.SetListenerFrame(_currentCameraEntity.GetGlobalFrame(), _currentCameraEntity.GlobalPosition);
		_enemyCharacterObject = _missionController.GetEnemyCharacterObject();
		_allLines = new List<ConversationLine>
		{
			new ConversationLine(new TextObject("{=4nAQl8Vx}Listen, you lot, I'm in a bit of a hurry. If I give you a penny each will you stop pestering me?"), NavalStorylineData.Gunnar.CharacterObject),
			new ConversationLine(new TextObject("{=p7Gxhb6O}You're Gunnar of Lagshofn, aren't you? We've got a message from the Sea Hounds for you."), _enemyCharacterObject),
			new ConversationLine(new TextObject("{=G6NrtQuF}You’ve got a message from those curs? Out with it, then. What’s your message?"), NavalStorylineData.Gunnar.CharacterObject),
			new ConversationLine(new TextObject("{=OMpfszRu}The message... the message is that you will die, you damn fool."), _enemyCharacterObject),
			new ConversationLine(new TextObject("{=qtz4B25N}And how should I die, then? Of old age, while you three work up the courage to attack a wizened graybeard? Go on, you've delivered your message, now scamper off."), NavalStorylineData.Gunnar.CharacterObject),
			new ConversationLine(new TextObject("{=Nmv85ZfP}We’ll send you down to the Pale One right now. Kill him, boys!"), _enemyCharacterObject)
		};
	}

	private void UpdateEntityReferences()
	{
		base.Mission.Scene.GetEntities(ref _entities);
	}

	public void GetCameraFrame(out Vec3 position, out Vec3 forward)
	{
		if (!_isMissionInitialized)
		{
			Initialize();
		}
		position = _currentCameraEntity.GlobalPosition;
		forward = _currentCameraEntity.GetGlobalFrame().rotation.f;
	}

	public float GetFadeDuration()
	{
		return 0.75f;
	}

	public float GetBlackScreenDuration()
	{
		return 0.25f;
	}

	private void SetCinematicState(NavalAlleyFightCinematicState newState)
	{
		_cinematicTimer = 0f;
		_currentCinematicState = newState;
		this.OnCinematicStateChanged(_currentCinematicState);
		if (newState == NavalAlleyFightCinematicState.FirstCamera)
		{
			ShowSkipCinematicHintText();
		}
	}

	private void TickCinematic(float dt)
	{
		if (_currentCinematicState == NavalAlleyFightCinematicState.Completed)
		{
			return;
		}
		if (_currentCinematicState == NavalAlleyFightCinematicState.Ready && Agent.Main != null && _cinematicTriggerZone.GlobalPosition.DistanceSquared(Agent.Main.Position) <= 9f)
		{
			if (Mission.Current.CameraIsFirstPerson)
			{
				Mission.Current.CameraIsFirstPerson = false;
			}
			_missionController.OnCinematicStarted();
			SetCinematicState(NavalAlleyFightCinematicState.InitialFadeOut);
		}
		_cinematicTimer += dt;
		if (_currentCinematicState == NavalAlleyFightCinematicState.InitialFadeOut && _cinematicTimer >= 0.75f)
		{
			SetCinematicState(NavalAlleyFightCinematicState.BlackScreen);
		}
		if (_currentCinematicState == NavalAlleyFightCinematicState.BlackScreen)
		{
			if (_cinematicTimer >= 0.25f)
			{
				ActivatePlayerEavesdropAnimation();
				SetCinematicState(NavalAlleyFightCinematicState.InitialFadeIn);
			}
		}
		else if (_currentCinematicState == NavalAlleyFightCinematicState.InitialFadeIn)
		{
			if (_cinematicTimer >= 0.75f)
			{
				foreach (ConversationLine allLine in _allLines)
				{
					MBInformationManager.DialogNotificationHandle handle = CampaignInformationManager.AddDialogLine(allLine.Line, allLine.Speaker, allLine.Speaker.FirstCivilianEquipment, 0, MBInformationManager.NotificationPriority.Highest);
					allLine.Handle = handle;
				}
				SetCinematicState(NavalAlleyFightCinematicState.FirstCamera);
			}
		}
		else if (_currentCinematicState == NavalAlleyFightCinematicState.FirstCamera)
		{
			if (_cinematicTimer >= 10f)
			{
				_currentCameraEntity = _cameraEntity2;
				SetCinematicState(NavalAlleyFightCinematicState.FinalCamera);
				SoundManager.SetListenerFrame(_currentCameraEntity.GetGlobalFrame(), _currentCameraEntity.GlobalPosition);
			}
		}
		else if (_currentCinematicState == NavalAlleyFightCinematicState.FinalCamera && _allLines.TrueForAll((ConversationLine x) => CampaignInformationManager.GetStatusOfDialogNotification(x.Handle) == MBInformationManager.NotificationStatus.Inactive))
		{
			FinishCinematic();
		}
		HandleSkipCinematic();
	}

	private void ActivatePlayerEavesdropAnimation()
	{
		if (Agent.Main.GetCurrentAction(0) != ActionIndexCache.act_cutscene_npc_argue_player_1)
		{
			Agent.Main.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
			Agent.Main.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
			Agent.Main.SetActionChannel(0, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL, 0f, 1f, 0f);
			Agent.Main.SetActionChannel(1, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL, 0f, 1f, 0f);
			GameEntity gameEntity = _entities.FirstOrDefault((GameEntity t) => t.HasTag("sp_player_wait"));
			Agent.Main.TeleportToPosition(gameEntity.GlobalPosition);
			Vec3 f = gameEntity.GetGlobalFrame().rotation.f;
			Agent.Main.LookDirection = f;
			this.OnConversationSetupEvent(f);
			Agent.Main.SetActionChannel(0, in ActionIndexCache.act_cutscene_npc_argue_player_1, ignorePriority: true, (AnimFlags)0uL, 0f, 1f, 0f);
		}
	}

	private void FinishCinematic()
	{
		SetCinematicState(NavalAlleyFightCinematicState.Completed);
		_missionController.StartFight();
		Agent.Main.SetActionChannel(0, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL);
		_missionHintLogic.Clear();
	}

	private void HandleSkipCinematic()
	{
		if ((_currentCinematicState != NavalAlleyFightCinematicState.FirstCamera && _currentCinematicState != NavalAlleyFightCinematicState.FinalCamera) || !Mission.Current.InputManager.IsGameKeyDown(14) || !_allLines.Any((ConversationLine x) => CampaignInformationManager.GetStatusOfDialogNotification(x.Handle) != MBInformationManager.NotificationStatus.Inactive))
		{
			return;
		}
		foreach (ConversationLine allLine in _allLines)
		{
			CampaignInformationManager.ClearDialogNotification(allLine.Handle, fadeOut: false);
		}
		FinishCinematic();
	}

	public void OnFightEnded()
	{
		_isPostFightConversationQueued = true;
		this.OnFightEndedEvent(0.75f, 1f, 0.75f);
	}

	private void ShowSkipCinematicHintText()
	{
		if (_missionHintLogic.ActiveHint != null)
		{
			_missionHintLogic.Clear();
		}
		MissionHint activeHint = MissionHint.CreateWithKeyAndAction(SkipHintText, HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 14));
		_missionHintLogic.SetActiveHint(activeHint);
	}

	public void OnConversationSetup(Vec3 direction)
	{
		this.OnConversationSetupEvent(direction);
	}
}
