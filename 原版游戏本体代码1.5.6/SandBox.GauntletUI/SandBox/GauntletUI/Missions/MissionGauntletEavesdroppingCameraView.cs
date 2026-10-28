using SandBox.View.Missions;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Missions;

[OverrideView(typeof(EavesdroppingMissionCameraView))]
public class MissionGauntletEavesdroppingCameraView : EavesdroppingMissionCameraView
{
	private class EavesdroppingGauntletLayer : GauntletLayer
	{
		public EavesdroppingGauntletLayer(int localOrder, bool shouldClear = false)
			: base("MissionEavesdropping", localOrder, shouldClear)
		{
		}

		public override bool HitTest()
		{
			return true;
		}
	}

	private EavesdroppingGauntletLayer _gauntletLayer;

	public MissionGauntletEavesdroppingCameraView()
	{
		_gauntletLayer = new EavesdroppingGauntletLayer(10);
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
