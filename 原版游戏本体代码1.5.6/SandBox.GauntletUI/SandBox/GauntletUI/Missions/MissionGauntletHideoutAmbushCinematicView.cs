using SandBox.View.Missions;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Missions;

[OverrideView(typeof(MissionHideoutAmbushCinematicView))]
public class MissionGauntletHideoutAmbushCinematicView : MissionHideoutAmbushCinematicView
{
	private class HideoutAmbushCutsceneGauntletLayer : GauntletLayer
	{
		public HideoutAmbushCutsceneGauntletLayer(int localOrder, bool shouldClear = false)
			: base("MissionHideoutAmbushCutscene", localOrder, shouldClear)
		{
		}

		public override bool HitTest()
		{
			return true;
		}
	}

	private HideoutAmbushCutsceneGauntletLayer _gauntletLayer;

	public MissionGauntletHideoutAmbushCinematicView()
	{
		_gauntletLayer = new HideoutAmbushCutsceneGauntletLayer(10);
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		base.MissionScreen.AddLayer(_gauntletLayer);
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		base.MissionScreen.RemoveLayer(_gauntletLayer);
	}

	protected override void SetPlayerMovementEnabled(bool isPlayerMovementEnabled)
	{
		base.SetPlayerMovementEnabled(isPlayerMovementEnabled);
		for (int i = 0; i < base.Mission.MissionBehaviors.Count; i++)
		{
			if (base.Mission.MissionBehaviors[i] is MissionBattleUIBaseView missionBattleUIBaseView)
			{
				if (!isPlayerMovementEnabled)
				{
					missionBattleUIBaseView.SuspendView();
				}
				else
				{
					missionBattleUIBaseView.ResumeView();
				}
			}
		}
		if (isPlayerMovementEnabled)
		{
			_gauntletLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_gauntletLayer);
			_gauntletLayer.InputRestrictions.ResetInputRestrictions();
		}
		else
		{
			_gauntletLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(_gauntletLayer);
			_gauntletLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: false);
		}
	}
}
