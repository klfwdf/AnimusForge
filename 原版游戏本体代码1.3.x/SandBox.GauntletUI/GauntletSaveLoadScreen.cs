using System;
using SandBox.View;
using SandBox.ViewModelCollection.SaveLoad;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI
{
	// Token: 0x0200000F RID: 15
	[OverrideView(typeof(SaveLoadScreen))]
	public class GauntletSaveLoadScreen : ScreenBase
	{
		// Token: 0x060000B9 RID: 185 RVA: 0x000072EF File Offset: 0x000054EF
		public GauntletSaveLoadScreen(bool isSaving)
		{
			this._isSaving = isSaving;
		}

		// Token: 0x060000BA RID: 186 RVA: 0x00007300 File Offset: 0x00005500
		protected override void OnInitialize()
		{
			base.OnInitialize();
			bool isCampaignMapOnStack = GameStateManager.Current.GameStates.FirstOrDefaultQ((GameState s) => s is MapState) != null;
			this._dataSource = new SaveLoadVM(this._isSaving, isCampaignMapOnStack);
			this._dataSource.SetDeleteInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Delete"));
			this._dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
			this._dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
			if (Game.Current != null)
			{
				Game.Current.GameStateManager.RegisterActiveStateDisableRequest(this);
			}
			this._spriteCategory = UIResourceManager.LoadSpriteCategory("ui_saveload");
			this._gauntletLayer = new GauntletLayer("SaveLoadScreen", 1, true);
			this._gauntletLayer.LoadMovie("SaveLoadScreen", this._dataSource);
			this._gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			base.AddLayer(this._gauntletLayer);
			if (BannerlordConfig.ForceVSyncInMenus)
			{
				Utilities.SetForceVsync(true);
			}
			InformationManager.HideAllMessages();
			this._dataSource.Initialize();
		}

		// Token: 0x060000BB RID: 187 RVA: 0x00007447 File Offset: 0x00005647
		protected override void OnPostFrameTick(float dt)
		{
			base.OnPostFrameTick(dt);
			this.UpdateInputRestrictions();
		}

		// Token: 0x060000BC RID: 188 RVA: 0x00007458 File Offset: 0x00005658
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			this.UpdateInputRestrictions();
			if (this._gauntletLayer.Input.IsHotKeyReleased("Exit"))
			{
				this._dataSource.ExecuteDone();
				UISoundsHelper.PlayUISound("event:/ui/panels/next");
				return;
			}
			if (this._gauntletLayer.Input.IsHotKeyPressed("Confirm") && !this._gauntletLayer.IsFocusedOnInput())
			{
				this._dataSource.ExecuteLoadSave();
				UISoundsHelper.PlayUISound("event:/ui/panels/next");
				return;
			}
			if (this._gauntletLayer.Input.IsHotKeyPressed("Delete") && !this._gauntletLayer.IsFocusedOnInput())
			{
				this._dataSource.DeleteSelectedSave();
				UISoundsHelper.PlayUISound("event:/ui/panels/next");
			}
		}

		// Token: 0x060000BD RID: 189 RVA: 0x00007514 File Offset: 0x00005714
		protected override void OnFinalize()
		{
			base.OnFinalize();
			if (Game.Current != null)
			{
				Game.Current.GameStateManager.UnregisterActiveStateDisableRequest(this);
			}
			base.RemoveLayer(this._gauntletLayer);
			this._gauntletLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(this._gauntletLayer);
			this._gauntletLayer = null;
			this._dataSource.OnFinalize();
			this._dataSource = null;
			this._spriteCategory.Unload();
			Utilities.SetForceVsync(false);
		}

		// Token: 0x060000BE RID: 190 RVA: 0x0000758C File Offset: 0x0000578C
		private void UpdateInputRestrictions()
		{
			if (this._dataSource.IsBusyWithAnAction)
			{
				this._gauntletLayer.InputRestrictions.ResetInputRestrictions();
				this._gauntletLayer.InputRestrictions.SetMouseVisibility(true);
				this._gauntletLayer.IsFocusLayer = false;
				ScreenManager.TryLoseFocus(this._gauntletLayer);
				return;
			}
			this._gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			this._gauntletLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(this._gauntletLayer);
		}

		// Token: 0x04000050 RID: 80
		private GauntletLayer _gauntletLayer;

		// Token: 0x04000051 RID: 81
		private SaveLoadVM _dataSource;

		// Token: 0x04000052 RID: 82
		private SpriteCategory _spriteCategory;

		// Token: 0x04000053 RID: 83
		private readonly bool _isSaving;
	}
}
