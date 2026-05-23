using System;
using SandBox.View.Missions;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Missions
{
	// Token: 0x0200001E RID: 30
	[OverrideView(typeof(EavesdroppingMissionCameraView))]
	public class MissionGauntletEavesdroppingCameraView : EavesdroppingMissionCameraView
	{
		// Token: 0x060001B4 RID: 436 RVA: 0x0000B545 File Offset: 0x00009745
		public MissionGauntletEavesdroppingCameraView()
		{
			this._gauntletLayer = new MissionGauntletEavesdroppingCameraView.EavesdroppingGauntletLayer(10, false);
		}

		// Token: 0x060001B5 RID: 437 RVA: 0x0000B55B File Offset: 0x0000975B
		public override void OnMissionScreenInitialize()
		{
			base.OnMissionScreenInitialize();
			base.MissionScreen.AddLayer(this._gauntletLayer);
		}

		// Token: 0x060001B6 RID: 438 RVA: 0x0000B574 File Offset: 0x00009774
		public override void OnMissionScreenFinalize()
		{
			base.OnMissionScreenFinalize();
			base.MissionScreen.RemoveLayer(this._gauntletLayer);
		}

		// Token: 0x060001B7 RID: 439 RVA: 0x0000B590 File Offset: 0x00009790
		protected override void SetPlayerMovementEnabled(bool isPlayerMovementEnabled)
		{
			base.SetPlayerMovementEnabled(isPlayerMovementEnabled);
			for (int i = 0; i < base.Mission.MissionBehaviors.Count; i++)
			{
				MissionBattleUIBaseView missionBattleUIBaseView;
				if ((missionBattleUIBaseView = base.Mission.MissionBehaviors[i] as MissionBattleUIBaseView) != null)
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
				this._gauntletLayer.IsFocusLayer = false;
				ScreenManager.TryLoseFocus(this._gauntletLayer);
				this._gauntletLayer.InputRestrictions.ResetInputRestrictions();
				return;
			}
			this._gauntletLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(this._gauntletLayer);
			this._gauntletLayer.InputRestrictions.SetInputRestrictions(false, InputUsageMask.All);
		}

		// Token: 0x0400008B RID: 139
		private MissionGauntletEavesdroppingCameraView.EavesdroppingGauntletLayer _gauntletLayer;

		// Token: 0x0200007D RID: 125
		private class EavesdroppingGauntletLayer : GauntletLayer
		{
			// Token: 0x0600044D RID: 1101 RVA: 0x000181AA File Offset: 0x000163AA
			public EavesdroppingGauntletLayer(int localOrder, bool shouldClear = false)
				: base("MissionEavesdropping", localOrder, shouldClear)
			{
			}

			// Token: 0x0600044E RID: 1102 RVA: 0x000181B9 File Offset: 0x000163B9
			public override bool HitTest()
			{
				return true;
			}
		}
	}
}
