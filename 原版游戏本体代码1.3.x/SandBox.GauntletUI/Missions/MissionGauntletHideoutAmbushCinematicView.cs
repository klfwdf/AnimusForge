using System;
using SandBox.View.Missions;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Missions
{
	// Token: 0x0200001F RID: 31
	[OverrideView(typeof(MissionHideoutAmbushCinematicView))]
	public class MissionGauntletHideoutAmbushCinematicView : MissionHideoutAmbushCinematicView
	{
		// Token: 0x060001B8 RID: 440 RVA: 0x0000B63E File Offset: 0x0000983E
		public MissionGauntletHideoutAmbushCinematicView()
		{
			this._gauntletLayer = new MissionGauntletHideoutAmbushCinematicView.HideoutAmbushCutsceneGauntletLayer(10, false);
		}

		// Token: 0x060001B9 RID: 441 RVA: 0x0000B654 File Offset: 0x00009854
		public override void OnMissionScreenInitialize()
		{
			base.OnMissionScreenInitialize();
			base.MissionScreen.AddLayer(this._gauntletLayer);
		}

		// Token: 0x060001BA RID: 442 RVA: 0x0000B66D File Offset: 0x0000986D
		public override void OnMissionScreenFinalize()
		{
			base.OnMissionScreenFinalize();
			base.MissionScreen.RemoveLayer(this._gauntletLayer);
		}

		// Token: 0x060001BB RID: 443 RVA: 0x0000B688 File Offset: 0x00009888
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

		// Token: 0x0400008C RID: 140
		private MissionGauntletHideoutAmbushCinematicView.HideoutAmbushCutsceneGauntletLayer _gauntletLayer;

		// Token: 0x0200007E RID: 126
		private class HideoutAmbushCutsceneGauntletLayer : GauntletLayer
		{
			// Token: 0x0600044F RID: 1103 RVA: 0x000181BC File Offset: 0x000163BC
			public HideoutAmbushCutsceneGauntletLayer(int localOrder, bool shouldClear = false)
				: base("MissionHideoutAmbushCutscene", localOrder, shouldClear)
			{
			}

			// Token: 0x06000450 RID: 1104 RVA: 0x000181CB File Offset: 0x000163CB
			public override bool HitTest()
			{
				return true;
			}
		}
	}
}
