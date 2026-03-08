using System;
using System.Collections.Generic;
using SandBox.ViewModelCollection.Map.Cheat;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Missions
{
	// Token: 0x0200001C RID: 28
	[OverrideView(typeof(MissionCheatView))]
	public class MissionGauntletCheatView : MissionCheatView
	{
		// Token: 0x0600019B RID: 411 RVA: 0x0000AE3E File Offset: 0x0000903E
		public override void OnMissionScreenFinalize()
		{
			base.OnMissionScreenFinalize();
			this.FinalizeScreen();
		}

		// Token: 0x0600019C RID: 412 RVA: 0x0000AE4C File Offset: 0x0000904C
		public override bool GetIsCheatsAvailable()
		{
			return true;
		}

		// Token: 0x0600019D RID: 413 RVA: 0x0000AE50 File Offset: 0x00009050
		public override void InitializeScreen()
		{
			if (this._isActive)
			{
				return;
			}
			this._isActive = true;
			IEnumerable<GameplayCheatBase> missionCheatList = GameplayCheatsManager.GetMissionCheatList();
			this._dataSource = new GameplayCheatsVM(new Action(this.FinalizeScreen), missionCheatList);
			this.InitializeKeyVisuals();
			this._gauntletLayer = new GauntletLayer("MissionCheat", 4500, false);
			this._gauntletLayer.LoadMovie("MapCheats", this._dataSource);
			this._gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			this._gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			this._gauntletLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(this._gauntletLayer);
			base.MissionScreen.AddLayer(this._gauntletLayer);
		}

		// Token: 0x0600019E RID: 414 RVA: 0x0000AF14 File Offset: 0x00009114
		public override void FinalizeScreen()
		{
			if (!this._isActive)
			{
				return;
			}
			this._isActive = false;
			base.MissionScreen.RemoveLayer(this._gauntletLayer);
			GameplayCheatsVM dataSource = this._dataSource;
			if (dataSource != null)
			{
				dataSource.OnFinalize();
			}
			this._gauntletLayer = null;
			this._dataSource = null;
		}

		// Token: 0x0600019F RID: 415 RVA: 0x0000AF61 File Offset: 0x00009161
		public override void OnMissionScreenTick(float dt)
		{
			base.OnMissionScreenTick(dt);
			if (this._isActive)
			{
				this.HandleInput();
			}
		}

		// Token: 0x060001A0 RID: 416 RVA: 0x0000AF78 File Offset: 0x00009178
		private void HandleInput()
		{
			if (this._gauntletLayer.Input.IsHotKeyReleased("Exit"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				GameplayCheatsVM dataSource = this._dataSource;
				if (dataSource == null)
				{
					return;
				}
				dataSource.ExecuteClose();
			}
		}

		// Token: 0x060001A1 RID: 417 RVA: 0x0000AFAB File Offset: 0x000091AB
		private void InitializeKeyVisuals()
		{
			this._dataSource.SetCloseInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		}

		// Token: 0x04000082 RID: 130
		private GauntletLayer _gauntletLayer;

		// Token: 0x04000083 RID: 131
		private GameplayCheatsVM _dataSource;

		// Token: 0x04000084 RID: 132
		private bool _isActive;
	}
}
