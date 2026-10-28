using System;
using NavalDLC.Storyline;
using NavalDLC.View.Missions;
using SandBox.ViewModelCollection.Missions.NameMarker;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Hints;
using TaleWorlds.MountAndBlade.Missions.MissionLogics;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace NavalDLC.View.MissionViews.Storyline;

public class NavalCaptivityBattleMissionView : MissionView
{
	private const int FreeHintHotKey = 13;

	private NavalStorylineCaptivityMissionController _captivityMissionController;

	private MissionHintLogic _missionHintLogic;

	private TextObject FreeHintText => new TextObject("{=EThbCDao}Free yourself");

	public bool AreMarkersDirty { get; private set; }

	public override void OnBehaviorInitialize()
	{
		MissionNameMarkerFactory.PushContext("NavalCaptivityBattleContext", addDefaultProviders: false).AddProvider<NavalStorylineCaptivityMissionNameMarkerProvider>();
		_captivityMissionController = base.Mission.GetMissionBehavior<NavalStorylineCaptivityMissionController>();
		_missionHintLogic = base.Mission.GetMissionBehavior<MissionHintLogic>();
		if (_captivityMissionController != null)
		{
			NavalStorylineCaptivityMissionController captivityMissionController = _captivityMissionController;
			captivityMissionController.OnMarkedObjectStatusChangedEvent = (Action)Delegate.Combine(captivityMissionController.OnMarkedObjectStatusChangedEvent, new Action(OnMarkersAreDirty));
			NavalStorylineCaptivityMissionController captivityMissionController2 = _captivityMissionController;
			captivityMissionController2.OnPlayerStartedEscapeEvent = (Action)Delegate.Combine(captivityMissionController2.OnPlayerStartedEscapeEvent, new Action(OnPlayerStartedEscape));
			NavalStorylineCaptivityMissionController captivityMissionController3 = _captivityMissionController;
			captivityMissionController3.OnConversationSetupEvent = (Action<Vec3>)Delegate.Combine(captivityMissionController3.OnConversationSetupEvent, new Action<Vec3>(OnConversationSetup));
			NavalStorylineCaptivityMissionController captivityMissionController4 = _captivityMissionController;
			captivityMissionController4.OnStartFadeOutEvent = (Action<float, float, float>)Delegate.Combine(captivityMissionController4.OnStartFadeOutEvent, new Action<float, float, float>(OnStartFadeOut));
			NavalStorylineCaptivityMissionController captivityMissionController5 = _captivityMissionController;
			captivityMissionController5.OnFirstHighlightClearedEvent = (Action)Delegate.Combine(captivityMissionController5.OnFirstHighlightClearedEvent, new Action(OnFirstHighlightCleared));
			NavalStorylineCaptivityMissionController captivityMissionController6 = _captivityMissionController;
			captivityMissionController6.OnOarsmenLevelChanged = (Action<int>)Delegate.Combine(captivityMissionController6.OnOarsmenLevelChanged, new Action<int>(OnOarsmenLevelChanged));
		}
	}

	public override void OnMissionScreenFinalize()
	{
		if (_captivityMissionController != null)
		{
			NavalStorylineCaptivityMissionController captivityMissionController = _captivityMissionController;
			captivityMissionController.OnMarkedObjectStatusChangedEvent = (Action)Delegate.Remove(captivityMissionController.OnMarkedObjectStatusChangedEvent, new Action(OnMarkersAreDirty));
		}
		MissionNameMarkerFactory.PopContext("NavalCaptivityBattleContext");
	}

	public override void AfterStart()
	{
		ShowFreePlayerHintText();
	}

	private void OnPlayerStartedEscape()
	{
		_missionHintLogic.Clear();
		OnPlayerStartedEscapeInternal();
	}

	private void OnOarsmenLevelChanged(int level)
	{
		OnOarsmenLevelChangedInternal(level);
	}

	private void OnStartFadeOut(float fadeInDuration, float blackDuration, float fadeOutDuration)
	{
		ScreenFadeController.BeginFadeOutAndIn(fadeOutDuration, blackDuration, fadeInDuration);
	}

	private void OnConversationSetup(Vec3 direction)
	{
		base.MissionScreen.CameraBearing = direction.RotationZ;
	}

	private void ShowFreePlayerHintText()
	{
		if (_missionHintLogic.ActiveHint != null)
		{
			_missionHintLogic.Clear();
		}
		MissionHint activeHint = MissionHint.CreateWithKeyAndAction(FreeHintText, HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13));
		_missionHintLogic.SetActiveHint(activeHint);
	}

	private void OnFirstHighlightCleared()
	{
		OnFirstHighlightClearedInternal();
	}

	protected virtual void OnFirstHighlightClearedInternal()
	{
	}

	protected virtual void OnPlayerStartedEscapeInternal()
	{
	}

	protected virtual void OnOarsmenLevelChangedInternal(int level)
	{
	}

	private void OnMarkersAreDirty()
	{
		AreMarkersDirty = true;
	}

	public void OnDirtyMarkersHandled()
	{
		AreMarkersDirty = false;
	}
}
