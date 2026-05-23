using System;
using SandBox.View.Map;
using SandBox.ViewModelCollection;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map
{
	// Token: 0x0200002F RID: 47
	[OverrideView(typeof(BattleSimulationMapView))]
	public class GauntletMapBattleSimulationView : MapView
	{
		// Token: 0x06000243 RID: 579 RVA: 0x0000E2FB File Offset: 0x0000C4FB
		public GauntletMapBattleSimulationView(SPScoreboardVM dataSource)
		{
			this._dataSource = dataSource;
		}

		// Token: 0x06000244 RID: 580 RVA: 0x0000E30A File Offset: 0x0000C50A
		protected override void OnMapConversationStart()
		{
			base.OnMapConversationStart();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, true);
			}
		}

		// Token: 0x06000245 RID: 581 RVA: 0x0000E326 File Offset: 0x0000C526
		protected override void OnMapConversationOver()
		{
			base.OnMapConversationOver();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, false);
			}
		}

		// Token: 0x06000246 RID: 582 RVA: 0x0000E344 File Offset: 0x0000C544
		protected override void CreateLayout()
		{
			base.CreateLayout();
			this._dataSource.Initialize(null, null, new Action(base.MapState.EndBattleSimulation), null);
			this._dataSource.SetShortcuts(new ScoreboardHotkeys
			{
				ShowMouseHotkey = null,
				ShowScoreboardHotkey = null,
				DoneInputKey = HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"),
				FastForwardKey = HotKeyManager.GetCategory("ScoreboardHotKeyCategory").GetHotKey("ToggleFastForward"),
				PauseInputKey = HotKeyManager.GetCategory("ScoreboardHotKeyCategory").GetHotKey("TogglePause")
			});
			base.Layer = new GauntletLayer("MapBattleSimulation", 101, false);
			this._layerAsGauntletLayer = base.Layer as GauntletLayer;
			base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("ScoreboardHotKeyCategory"));
			this._layerAsGauntletLayer.LoadMovie("SPScoreboard", this._dataSource);
			this._dataSource.ExecutePlayAction();
			base.Layer.IsFocusLayer = true;
			base.Layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			base.MapScreen.AddLayer(base.Layer);
			ScreenManager.TrySetFocus(base.Layer);
		}

		// Token: 0x06000247 RID: 583 RVA: 0x0000E4A0 File Offset: 0x0000C6A0
		protected override void OnFinalize()
		{
			this._dataSource.OnFinalize();
			base.MapScreen.RemoveLayer(base.Layer);
			base.Layer.IsFocusLayer = false;
			base.Layer.InputRestrictions.ResetInputRestrictions();
			ScreenManager.TryLoseFocus(base.Layer);
			this._layerAsGauntletLayer = null;
			base.Layer = null;
		}

		// Token: 0x06000248 RID: 584 RVA: 0x0000E500 File Offset: 0x0000C700
		protected override void OnMapScreenUpdate(float dt)
		{
			base.OnMapScreenUpdate(dt);
			if (this._dataSource != null && base.Layer != null)
			{
				this._dataSource.Tick(dt);
				if (!this._dataSource.IsOver && base.Layer.Input.IsHotKeyReleased("ToggleFastForward"))
				{
					this._dataSource.IsFastForwarding = !this._dataSource.IsFastForwarding;
					this._dataSource.ExecuteFastForwardAction();
					return;
				}
				if (!this._dataSource.IsOver && this._dataSource.IsSimulation && this._dataSource.ShowScoreboard && base.Layer.Input.IsHotKeyReleased("TogglePause"))
				{
					this._dataSource.IsPaused = !this._dataSource.IsPaused;
					this._dataSource.ExecutePauseSimulationAction();
					return;
				}
				if (this._dataSource.IsOver && this._dataSource.ShowScoreboard && base.Layer.Input.IsHotKeyPressed("Confirm"))
				{
					this._dataSource.ExecuteQuitAction();
				}
			}
		}

		// Token: 0x040000CF RID: 207
		private GauntletLayer _layerAsGauntletLayer;

		// Token: 0x040000D0 RID: 208
		private readonly SPScoreboardVM _dataSource;
	}
}
