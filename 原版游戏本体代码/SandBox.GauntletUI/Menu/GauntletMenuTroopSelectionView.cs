using System;
using SandBox.View.Map;
using SandBox.View.Menu;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TroopSelection;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Menu
{
	// Token: 0x0200002A RID: 42
	[OverrideView(typeof(MenuTroopSelectionView))]
	public class GauntletMenuTroopSelectionView : MenuView
	{
		// Token: 0x06000210 RID: 528 RVA: 0x0000D0E3 File Offset: 0x0000B2E3
		public GauntletMenuTroopSelectionView(TroopRoster fullRoster, TroopRoster initialSelections, Func<CharacterObject, bool> changeChangeStatusOfTroop, Action<TroopRoster> onDone, int maxSelectableTroopCount, int minSelectableTroopCount)
		{
			this._onDone = onDone;
			this._fullRoster = fullRoster;
			this._initialSelections = initialSelections;
			this._changeChangeStatusOfTroop = changeChangeStatusOfTroop;
			this._maxSelectableTroopCount = maxSelectableTroopCount;
			this._minSelectableTroopCount = minSelectableTroopCount;
		}

		// Token: 0x06000211 RID: 529 RVA: 0x0000D118 File Offset: 0x0000B318
		protected override void OnInitialize()
		{
			base.OnInitialize();
			this._dataSource = new GameMenuTroopSelectionVM(this._fullRoster, this._initialSelections, this._changeChangeStatusOfTroop, new Action<TroopRoster>(this.OnDone), this._maxSelectableTroopCount, this._minSelectableTroopCount)
			{
				IsEnabled = true
			};
			this._dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
			this._dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
			this._dataSource.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
			base.Layer = new GauntletLayer("MapTroopSelection", 206, false);
			this._layerAsGauntletLayer = base.Layer as GauntletLayer;
			base.Layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
			this._movie = this._layerAsGauntletLayer.LoadMovie("GameMenuTroopSelection", this._dataSource);
			base.Layer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(this._layerAsGauntletLayer);
			base.MenuViewContext.AddLayer(base.Layer);
			MapScreen mapScreen;
			if ((mapScreen = ScreenManager.TopScreen as MapScreen) != null)
			{
				mapScreen.SetIsInHideoutTroopManage(true);
			}
		}

		// Token: 0x06000212 RID: 530 RVA: 0x0000D28A File Offset: 0x0000B48A
		private void OnDone(TroopRoster obj)
		{
			MapScreen.Instance.SetIsInHideoutTroopManage(false);
			base.MenuViewContext.CloseTroopSelection();
			Action<TroopRoster> onDone = this._onDone;
			if (onDone == null)
			{
				return;
			}
			onDone.DynamicInvokeWithLog(new object[] { obj });
		}

		// Token: 0x06000213 RID: 531 RVA: 0x0000D2C0 File Offset: 0x0000B4C0
		protected override void OnFinalize()
		{
			base.Layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(base.Layer);
			this._dataSource.OnFinalize();
			this._dataSource = null;
			this._layerAsGauntletLayer.ReleaseMovie(this._movie);
			base.MenuViewContext.RemoveLayer(base.Layer);
			this._movie = null;
			base.Layer = null;
			this._layerAsGauntletLayer = null;
			MapScreen.Instance.SetIsInHideoutTroopManage(false);
			base.OnFinalize();
		}

		// Token: 0x06000214 RID: 532 RVA: 0x0000D340 File Offset: 0x0000B540
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			if (this._dataSource != null)
			{
				this._dataSource.IsFiveStackModifierActive = base.Layer.Input.IsHotKeyDown("FiveStackModifier");
				this._dataSource.IsEntireStackModifierActive = base.Layer.Input.IsHotKeyDown("EntireStackModifier");
			}
			ScreenLayer layer = base.Layer;
			if (layer != null && layer.Input.IsHotKeyPressed("Exit"))
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				this._dataSource.ExecuteCancel();
			}
			else
			{
				ScreenLayer layer2 = base.Layer;
				if (layer2 != null && layer2.Input.IsHotKeyPressed("Confirm") && this._dataSource.IsDoneEnabled)
				{
					UISoundsHelper.PlayUISound("event:/ui/default");
					this._dataSource.ExecuteDone();
				}
				else
				{
					ScreenLayer layer3 = base.Layer;
					if (layer3 != null && layer3.Input.IsHotKeyPressed("Reset"))
					{
						UISoundsHelper.PlayUISound("event:/ui/default");
						this._dataSource.ExecuteReset();
					}
				}
			}
			GameMenuTroopSelectionVM dataSource = this._dataSource;
			if (dataSource != null && !dataSource.IsEnabled)
			{
				base.MenuViewContext.CloseTroopSelection();
			}
		}

		// Token: 0x06000215 RID: 533 RVA: 0x0000D468 File Offset: 0x0000B668
		protected override void OnMapConversationActivated()
		{
			base.OnMapConversationActivated();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, true);
			}
		}

		// Token: 0x06000216 RID: 534 RVA: 0x0000D484 File Offset: 0x0000B684
		protected override void OnMapConversationDeactivated()
		{
			base.OnMapConversationDeactivated();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, false);
			}
		}

		// Token: 0x040000B0 RID: 176
		private readonly Action<TroopRoster> _onDone;

		// Token: 0x040000B1 RID: 177
		private readonly TroopRoster _fullRoster;

		// Token: 0x040000B2 RID: 178
		private readonly TroopRoster _initialSelections;

		// Token: 0x040000B3 RID: 179
		private readonly Func<CharacterObject, bool> _changeChangeStatusOfTroop;

		// Token: 0x040000B4 RID: 180
		private readonly int _maxSelectableTroopCount;

		// Token: 0x040000B5 RID: 181
		private readonly int _minSelectableTroopCount;

		// Token: 0x040000B6 RID: 182
		private GauntletLayer _layerAsGauntletLayer;

		// Token: 0x040000B7 RID: 183
		private GameMenuTroopSelectionVM _dataSource;

		// Token: 0x040000B8 RID: 184
		private GauntletMovieIdentifier _movie;
	}
}
