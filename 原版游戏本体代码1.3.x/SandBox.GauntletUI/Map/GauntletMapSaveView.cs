using System;
using SandBox.View.Map;
using SandBox.ViewModelCollection.SaveLoad;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map
{
	// Token: 0x02000040 RID: 64
	[OverrideView(typeof(MapSaveView))]
	public class GauntletMapSaveView : MapView
	{
		// Token: 0x060002F1 RID: 753 RVA: 0x00011420 File Offset: 0x0000F620
		protected override void CreateLayout()
		{
			base.CreateLayout();
			this._dataSource = new MapSaveVM(new Action<bool>(this.OnStateChange));
			this._layerAsGauntletLayer = new GauntletLayer("MapSave", 10000, false);
			this._layerAsGauntletLayer.LoadMovie("MapSave", this._dataSource);
			base.Layer = this._layerAsGauntletLayer;
			base.Layer.InputRestrictions.SetInputRestrictions(false, InputUsageMask.MouseButtons | InputUsageMask.Keyboardkeys);
			base.MapScreen.AddLayer(base.Layer);
		}

		// Token: 0x060002F2 RID: 754 RVA: 0x000114A8 File Offset: 0x0000F6A8
		private void OnStateChange(bool isActive)
		{
			if (isActive)
			{
				base.Layer.IsFocusLayer = true;
				ScreenManager.TrySetFocus(base.Layer);
				base.Layer.InputRestrictions.SetInputRestrictions(false, InputUsageMask.All);
				return;
			}
			base.Layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(base.Layer);
			base.Layer.InputRestrictions.ResetInputRestrictions();
		}

		// Token: 0x060002F3 RID: 755 RVA: 0x00011509 File Offset: 0x0000F709
		protected override void OnFinalize()
		{
			base.OnFinalize();
			this._dataSource.OnFinalize();
			base.MapScreen.RemoveLayer(base.Layer);
			base.Layer = null;
			this._dataSource = null;
		}

		// Token: 0x060002F4 RID: 756 RVA: 0x0001153B File Offset: 0x0000F73B
		protected override void OnMapConversationStart()
		{
			base.OnMapConversationStart();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, true);
			}
		}

		// Token: 0x060002F5 RID: 757 RVA: 0x00011557 File Offset: 0x0000F757
		protected override void OnMapConversationOver()
		{
			base.OnMapConversationOver();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, false);
			}
		}

		// Token: 0x04000123 RID: 291
		private GauntletLayer _layerAsGauntletLayer;

		// Token: 0x04000124 RID: 292
		private MapSaveVM _dataSource;
	}
}
