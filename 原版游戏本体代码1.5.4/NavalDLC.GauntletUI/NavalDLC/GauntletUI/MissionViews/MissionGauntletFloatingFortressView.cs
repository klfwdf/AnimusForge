using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.Objects.UsableMachines;
using NavalDLC.Storyline;
using NavalDLC.View.MissionViews;
using SandBox.Objects.AreaMarkers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Hints;
using TaleWorlds.MountAndBlade.Missions.MissionLogics;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace NavalDLC.GauntletUI.MissionViews;

[OverrideView(typeof(FloatingFortressView))]
public class MissionGauntletFloatingFortressView : FloatingFortressView
{
	private abstract class Keyframe<T>
	{
		public float Time { get; set; }

		public T Value { get; set; }

		public Keyframe(float time, T value)
		{
			Time = time;
			Value = value;
		}
	}

	private abstract class Track<TKeyframe, TValue> where TKeyframe : Keyframe<TValue>
	{
		protected readonly List<TKeyframe> Keyframes = new List<TKeyframe>();

		private int _lastKeyframeIndex;

		public void AddKeyframe(TKeyframe keyframe)
		{
			Keyframes.Add(keyframe);
			Keyframes.Sort((TKeyframe a, TKeyframe b) => a.Time.CompareTo(b.Time));
		}

		public void RemoveKeyframe(TKeyframe keyframe)
		{
			Keyframes.Remove(keyframe);
		}

		public void ClearKeyframes()
		{
			Keyframes.Clear();
			_lastKeyframeIndex = 0;
		}

		public bool IsCompleted(float time)
		{
			if (Keyframes.Count != 0)
			{
				return Keyframes.Last().Time <= time;
			}
			return true;
		}

		public abstract TValue Evaluate(float time);

		protected (TKeyframe prev, TKeyframe next, float t) GetKeyframesAtTime(float time)
		{
			if (Keyframes.Count == 0)
			{
				return (prev: null, next: null, t: 0f);
			}
			if (time <= Keyframes[0].Time)
			{
				return (prev: Keyframes[0], next: Keyframes[0], t: 0f);
			}
			if (time >= Keyframes[Keyframes.Count - 1].Time)
			{
				return (prev: Keyframes[Keyframes.Count - 1], next: Keyframes[Keyframes.Count - 1], t: 1f);
			}
			int num = Math.Max(0, Math.Min(_lastKeyframeIndex, Keyframes.Count - 2));
			if (Keyframes[num].Time > time)
			{
				for (int num2 = num; num2 >= 0; num2--)
				{
					if (Keyframes[num2].Time <= time && Keyframes[num2 + 1].Time > time)
					{
						_lastKeyframeIndex = num2;
						float item = (time - Keyframes[num2].Time) / (Keyframes[num2 + 1].Time - Keyframes[num2].Time);
						return (prev: Keyframes[num2], next: Keyframes[num2 + 1], t: item);
					}
				}
			}
			else
			{
				for (int i = num; i < Keyframes.Count - 1; i++)
				{
					if (Keyframes[i].Time <= time && Keyframes[i + 1].Time > time)
					{
						_lastKeyframeIndex = i;
						float item2 = (time - Keyframes[i].Time) / (Keyframes[i + 1].Time - Keyframes[i].Time);
						return (prev: Keyframes[i], next: Keyframes[i + 1], t: item2);
					}
				}
			}
			return (prev: Keyframes[0], next: Keyframes[0], t: 0f);
		}
	}

	private class MatrixFrameKeyFrame : Keyframe<MatrixFrame>
	{
		public MatrixFrameKeyFrame(float time, MatrixFrame value)
			: base(time, value)
		{
		}
	}

	private class MatrixFrameTrack : Track<MatrixFrameKeyFrame, MatrixFrame>
	{
		public override MatrixFrame Evaluate(float time)
		{
			var (matrixFrameKeyFrame, matrixFrameKeyFrame2, num) = GetKeyframesAtTime(time);
			if (matrixFrameKeyFrame == null || matrixFrameKeyFrame2 == null)
			{
				return MatrixFrame.Zero;
			}
			if (matrixFrameKeyFrame == matrixFrameKeyFrame2)
			{
				return matrixFrameKeyFrame.Value;
			}
			return MatrixFrame.Lerp(matrixFrameKeyFrame.Value, matrixFrameKeyFrame2.Value, num * num * (3f - 2f * num));
		}
	}

	private class EventKeyframe : Keyframe<Action>
	{
		public EventKeyframe(float time, Action value)
			: base(time, value)
		{
		}
	}

	private class EventTrack : Track<EventKeyframe, Action>
	{
		private readonly HashSet<EventKeyframe> _triggeredEvents = new HashSet<EventKeyframe>();

		private float _lastEvaluatedTime = -0f;

		public override Action Evaluate(float time)
		{
			if (time < _lastEvaluatedTime)
			{
				_triggeredEvents.RemoveWhere((EventKeyframe e) => e.Time > time);
			}
			_lastEvaluatedTime = time;
			foreach (EventKeyframe keyframe in Keyframes)
			{
				if (keyframe.Time <= time && _triggeredEvents.Add(keyframe))
				{
					keyframe.Value?.Invoke();
				}
			}
			return null;
		}
	}

	private enum FadeOutReason
	{
		Initialize,
		BallistaCinematicEnded,
		PhaseOneCompleted
	}

	private const float EarliestSkipTime = 2.5f;

	private const float FadeOutTransitionTime = 1.5f;

	private readonly Dictionary<DestructableComponent, AnimatedBasicAreaIndicator> _markerByBallista = new Dictionary<DestructableComponent, AnimatedBasicAreaIndicator>();

	private bool _canInvokeFadeOutEvent = true;

	private FadeOutReason _fadeOutReason;

	private float _initialFadeOutWaitTime = 2f;

	private bool _isInitialized;

	private bool _isPhaseOneCompleted;

	private bool _isShowingBallistaHint;

	private bool _hasUsedBallista;

	private bool _willFadeOutForPhaseOneCompletion;

	private float _remainingTimeForPhaseOneFadeOut = 1.5f;

	private Camera _cinematicCamera;

	private bool _shouldTickCinematic;

	private float _cinematicElapsedTime;

	private MatrixFrameTrack _cinematicCameraTrack;

	private EventTrack _cinematicEventTrack;

	private FloatingFortressSetPieceBattleMissionController _controller;

	private MissionHintLogic _hintLogic;

	private NavalShipsLogic _navalShipsLogic;

	private MissionMainAgentController _missionMainAgentController;

	private MissionGauntletShipControlView _shipControlView;

	private MissionGauntletShipControlView.ShipControlFeatureFlags _suspendedFeatures;

	public bool AreMarkersDirty { get; private set; }

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (!_isInitialized)
		{
			InitializeView();
			_fadeOutReason = FadeOutReason.Initialize;
			_canInvokeFadeOutEvent = true;
			ScreenFadeController.BeginFadeOut(0f);
			_isInitialized = true;
		}
		if (!Mission.Current.Scene.IsLoadingFinished())
		{
			return;
		}
		MissionShip missionShip = Agent.Main?.GetComponent<AgentNavalComponent>().FormationShip;
		_hasUsedBallista = _hasUsedBallista || (missionShip != null && missionShip.ShipSiegeWeapon?.PlayerForceUse == true);
		if (_isShowingBallistaHint && _hintLogic.ActiveHint != null && (_hasUsedBallista || _navalShipsLogic.PlayerControlledShip == null))
		{
			_isShowingBallistaHint = false;
			_hintLogic.Clear();
		}
		if (_initialFadeOutWaitTime > 0f)
		{
			_initialFadeOutWaitTime -= dt;
			return;
		}
		if (_controller.IsPhaseOneCompleted && !_isPhaseOneCompleted)
		{
			_isPhaseOneCompleted = true;
			OnPhaseOneCompleted();
		}
		if (_willFadeOutForPhaseOneCompletion)
		{
			_remainingTimeForPhaseOneFadeOut -= dt;
			if (_remainingTimeForPhaseOneFadeOut <= 0f)
			{
				_fadeOutReason = FadeOutReason.PhaseOneCompleted;
				ScreenFadeController.BeginFadeOutAndIn(0.1f, 0.75f, 0.75f);
				_canInvokeFadeOutEvent = true;
				_willFadeOutForPhaseOneCompletion = false;
			}
		}
		foreach (MissionShip item in _controller.EnemyShipsOrdered)
		{
			if (item.ShipSiegeWeapon != null)
			{
				RangedSiegeWeapon shipSiegeWeapon = item.ShipSiegeWeapon;
				if (!shipSiegeWeapon.IsDestroyed && !_markerByBallista.ContainsKey(shipSiegeWeapon.DestructionComponent))
				{
					shipSiegeWeapon.DestructionComponent.OnDestroyed += OnBallistaDestroyed;
					GameEntity gameEntity = GameEntity.CreateEmpty(base.Mission.Scene);
					gameEntity.WeakEntity.SetGlobalPosition(shipSiegeWeapon.GameEntity.GlobalPosition);
					AnimatedBasicAreaIndicator value = AddMarker(gameEntity.WeakEntity, new TextObject("{=cn28TEkM}Target"), "quest", 1.5f);
					_markerByBallista.Add(shipSiegeWeapon.DestructionComponent, value);
					AreMarkersDirty = true;
				}
			}
		}
		foreach (KeyValuePair<DestructableComponent, AnimatedBasicAreaIndicator> markerByBallistum in _markerByBallista)
		{
			markerByBallistum.Value.GameEntity.SetGlobalPosition(markerByBallistum.Key.GameEntity.GlobalPosition);
		}
		if (ScreenFadeController.IsFadedOut && _canInvokeFadeOutEvent)
		{
			if (_controller.IsStartedFromCheckpoint)
			{
				_fadeOutReason = FadeOutReason.PhaseOneCompleted;
				ScreenFadeController.BeginFadeIn(1f);
			}
			if (_fadeOutReason == FadeOutReason.Initialize)
			{
				_cinematicCamera = Camera.CreateCamera();
				_cinematicCamera.SetFovHorizontal(base.MissionScreen.CombatCamera.HorizontalFov, base.MissionScreen.CombatCamera.GetAspectRatio(), base.MissionScreen.CombatCamera.Near, base.MissionScreen.CombatCamera.Far);
				_cinematicCamera.Frame = base.MissionScreen.CombatCamera.Frame;
				base.MissionScreen.CustomCamera = _cinematicCamera;
				ScreenFadeController.BeginFadeIn(1f);
				_shouldTickCinematic = true;
				MissionHint activeHint = MissionHint.CreateWithKeyAndAction(new TextObject("{=FiSENWMB}Skip Cinematic"), HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 14));
				_hintLogic.SetActiveHint(activeHint);
				_missionMainAgentController.Disable();
				_suspendedFeatures = _shipControlView.SuspendedFeatures;
				_shipControlView.SuspendFeature(~_suspendedFeatures);
			}
			else if (_fadeOutReason == FadeOutReason.BallistaCinematicEnded)
			{
				base.MissionScreen.CustomCamera = null;
				_cinematicCamera?.ReleaseCamera();
				_cinematicCamera = null;
				_shouldTickCinematic = false;
				if (!_controller.IsPhaseOneCompleted && !_controller.IsStartedFromCheckpoint && !_isShowingBallistaHint && !_hasUsedBallista && missionShip != null)
				{
					if (Agent.Main != null)
					{
						ShipControllerMachine shipControllerMachine = missionShip.ShipControllerMachine;
						Agent.Main.UseGameObject(shipControllerMachine.PilotStandingPoint);
					}
					_missionMainAgentController.Enable();
					_shipControlView.ResumeFeature(~_suspendedFeatures);
					_hintLogic.Clear();
					_isShowingBallistaHint = true;
					MissionHint activeHint2 = MissionHint.CreateWithKeyAndAction(new TextObject("{=aTEkCItM}Control Ballista"), HotKeyManager.GetHotKeyId("NavalShipControlsHotKeyCategory", 115));
					_hintLogic.SetActiveHint(activeHint2);
				}
			}
			_controller.OnViewFadeOut((int)_fadeOutReason);
			_canInvokeFadeOutEvent = false;
		}
		if (!ScreenFadeController.IsFadeActive && !_canInvokeFadeOutEvent)
		{
			_canInvokeFadeOutEvent = true;
		}
	}

	private void OnBallistaDestroyed(DestructableComponent target, Agent attackerAgent, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, int inflictedDamage)
	{
		if (_markerByBallista.TryGetValue(target, out var value))
		{
			value.SetIsActive(isActive: false);
			AreMarkersDirty = true;
		}
	}

	public override void OnFixedMissionTick(float fixedDt)
	{
		if (_shouldTickCinematic && !Game.Current.GameStateManager.ActiveStateDisabledByUser)
		{
			_cinematicElapsedTime += fixedDt;
			_cinematicCamera.Frame = _cinematicCameraTrack.Evaluate(_cinematicElapsedTime);
			_cinematicEventTrack.Evaluate(_cinematicElapsedTime);
			if ((Mission.Current.InputManager.IsGameKeyDown(14) && _cinematicElapsedTime >= 2.5f) || (_cinematicCameraTrack.IsCompleted(_cinematicElapsedTime) && _cinematicEventTrack.IsCompleted(_cinematicElapsedTime)))
			{
				_shouldTickCinematic = false;
				_hintLogic.Clear();
				CampaignInformationManager.ClearAllDialogNotifications(fadeOut: true);
				_fadeOutReason = FadeOutReason.BallistaCinematicEnded;
				ScreenFadeController.BeginFadeOutAndIn();
				_canInvokeFadeOutEvent = true;
			}
		}
	}

	private void InitializeView()
	{
		_controller = base.Mission.GetMissionBehavior<FloatingFortressSetPieceBattleMissionController>();
		_hintLogic = base.Mission.GetMissionBehavior<MissionHintLogic>();
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		_missionMainAgentController = base.Mission.GetMissionBehavior<MissionMainAgentController>();
		_shipControlView = base.Mission.GetMissionBehavior<MissionGauntletShipControlView>();
		InitializeCinematicKeyframes();
	}

	private void InitializeCinematicKeyframes()
	{
		MatrixFrame globalFrame = base.Mission.Scene.FindEntityWithTag("sp_camera_initial").GetGlobalFrame();
		MatrixFrame globalFrame2 = base.Mission.Scene.FindEntityWithTag("sp_camera_1").GetGlobalFrame();
		MatrixFrame globalFrame3 = base.Mission.Scene.FindEntityWithTag("sp_camera_1a").GetGlobalFrame();
		MatrixFrame globalFrame4 = base.Mission.Scene.FindEntityWithTag("sp_camera_2").GetGlobalFrame();
		MatrixFrame globalFrame5 = base.Mission.Scene.FindEntityWithTag("sp_camera_2a").GetGlobalFrame();
		MatrixFrame globalFrame6 = base.Mission.Scene.FindEntityWithTag("sp_camera_3").GetGlobalFrame();
		MatrixFrame globalFrame7 = base.Mission.Scene.FindEntityWithTag("sp_camera_3a").GetGlobalFrame();
		MatrixFrame globalFrame8 = base.Mission.Scene.FindEntityWithTag("sp_camera_4").GetGlobalFrame();
		MatrixFrame globalFrame9 = base.Mission.Scene.FindEntityWithTag("sp_camera_4a").GetGlobalFrame();
		MatrixFrame globalFrame10 = base.Mission.Scene.FindEntityWithTag("sp_camera_5").GetGlobalFrame();
		MatrixFrame globalFrame11 = base.Mission.Scene.FindEntityWithTag("sp_camera_5a").GetGlobalFrame();
		TextObject dialogueText1 = new TextObject("{=VUWTon9z}Have a good look at Crusas's floating fortress before we attack. It's formidable, but it's not going anywhere.");
		TextObject dialogueText2 = new TextObject("{=0JjVa9p9}He has no less than eight ships lashed together. They mount four heavy mangonels - big ones. Most ships would tip over from the recoil if they weren't chained to each other.");
		TextObject dialogueText3 = new TextObject("{=4Bhb39KH}One is on the roundship, which is the fortress's keep, as it were.");
		TextObject dialogueText4 = new TextObject("{=MTJMs4A7}Another three are on cogs - one is to the northwest.");
		TextObject dialogueText5 = new TextObject("{=ObjIiR2M}The others are to the northeast and southeast.");
		TextObject dialogueText6 = new TextObject("{=mVa3D9xf}You must steer the Wasp to take out those mangonels. You need direct hits - but don’t get too close, as their decks are packed with archers. ");
		TextObject dialogueText7 = new TextObject("{=afb9bd35}Also, keep moving. One or two hits could shatter our timbers or set us alight and make an end of us.");
		TextObject dialogueText8 = new TextObject("{=NIlRAHPb}We're right behind you, brother. Let's take this vile toad of a merchant down!");
		_cinematicCameraTrack = new MatrixFrameTrack();
		_cinematicEventTrack = new EventTrack();
		float num = 0f;
		_cinematicCameraTrack.AddKeyframe(new MatrixFrameKeyFrame(num, globalFrame));
		_cinematicEventTrack.AddKeyframe(new EventKeyframe(num, delegate
		{
			CampaignInformationManager.ClearAllDialogNotifications(fadeOut: true);
			CampaignInformationManager.AddDialogLine(dialogueText1, NavalStorylineData.Bjolgur.CharacterObject);
		}));
		num += 10f;
		_cinematicEventTrack.AddKeyframe(new EventKeyframe(num, delegate
		{
			CampaignInformationManager.ClearAllDialogNotifications(fadeOut: true);
			CampaignInformationManager.AddDialogLine(dialogueText2, NavalStorylineData.Bjolgur.CharacterObject);
		}));
		num += 15f;
		_cinematicCameraTrack.AddKeyframe(new MatrixFrameKeyFrame(num, globalFrame2));
		_cinematicEventTrack.AddKeyframe(new EventKeyframe(num, delegate
		{
			CampaignInformationManager.ClearAllDialogNotifications(fadeOut: true);
			CampaignInformationManager.AddDialogLine(dialogueText3, NavalStorylineData.Bjolgur.CharacterObject);
		}));
		num += 6f;
		_cinematicCameraTrack.AddKeyframe(new MatrixFrameKeyFrame(num, globalFrame3));
		_cinematicEventTrack.AddKeyframe(new EventKeyframe(num, delegate
		{
			CampaignInformationManager.ClearAllDialogNotifications(fadeOut: true);
			CampaignInformationManager.AddDialogLine(dialogueText4, NavalStorylineData.Bjolgur.CharacterObject);
		}));
		num += 0.5f;
		_cinematicCameraTrack.AddKeyframe(new MatrixFrameKeyFrame(num, globalFrame4));
		num += 5.5f;
		_cinematicCameraTrack.AddKeyframe(new MatrixFrameKeyFrame(num, globalFrame5));
		_cinematicEventTrack.AddKeyframe(new EventKeyframe(num, delegate
		{
			CampaignInformationManager.ClearAllDialogNotifications(fadeOut: true);
			CampaignInformationManager.AddDialogLine(dialogueText5, NavalStorylineData.Bjolgur.CharacterObject);
		}));
		num += 0.5f;
		_cinematicCameraTrack.AddKeyframe(new MatrixFrameKeyFrame(num, globalFrame6));
		num += 1.5f;
		_cinematicCameraTrack.AddKeyframe(new MatrixFrameKeyFrame(num, globalFrame7));
		num += 0.5f;
		_cinematicCameraTrack.AddKeyframe(new MatrixFrameKeyFrame(num, globalFrame8));
		num += 1.5f;
		_cinematicCameraTrack.AddKeyframe(new MatrixFrameKeyFrame(num, globalFrame9));
		_cinematicEventTrack.AddKeyframe(new EventKeyframe(num, delegate
		{
			CampaignInformationManager.ClearAllDialogNotifications(fadeOut: true);
			CampaignInformationManager.AddDialogLine(dialogueText6, NavalStorylineData.Bjolgur.CharacterObject);
		}));
		num += 6f;
		_cinematicCameraTrack.AddKeyframe(new MatrixFrameKeyFrame(num, globalFrame10));
		num += 6f;
		_cinematicEventTrack.AddKeyframe(new EventKeyframe(num, delegate
		{
			CampaignInformationManager.ClearAllDialogNotifications(fadeOut: true);
			CampaignInformationManager.AddDialogLine(dialogueText7, NavalStorylineData.Bjolgur.CharacterObject);
		}));
		num += 8f;
		_cinematicEventTrack.AddKeyframe(new EventKeyframe(num, delegate
		{
			CampaignInformationManager.ClearAllDialogNotifications(fadeOut: true);
			CampaignInformationManager.AddDialogLine(dialogueText8, NavalStorylineData.Bjolgur.CharacterObject);
		}));
		num += 7f;
		_cinematicCameraTrack.AddKeyframe(new MatrixFrameKeyFrame(num, globalFrame11));
	}

	private void OnPhaseOneCompleted()
	{
		if (_controller.IsStartedFromCheckpoint)
		{
			ScreenFadeController.BeginFadeIn(0.75f);
		}
		else
		{
			_willFadeOutForPhaseOneCompletion = true;
		}
	}

	private static AnimatedBasicAreaIndicator AddMarker(WeakGameEntity gameEntity, TextObject name, string type, float radius = 5f)
	{
		gameEntity.CreateAndAddScriptComponent("AnimatedBasicAreaIndicator", callScriptCallbacks: true);
		AnimatedBasicAreaIndicator firstScriptOfType = gameEntity.GetFirstScriptOfType<AnimatedBasicAreaIndicator>();
		firstScriptOfType.AreaRadius = radius;
		firstScriptOfType.Type = type;
		firstScriptOfType.SetOverriddenName(name);
		return firstScriptOfType;
	}
}
