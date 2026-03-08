using System;
using SandBox.View.Map;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map
{
	// Token: 0x0200003F RID: 63
	[OverrideView(typeof(MapReadyView))]
	public class GauntletMapReadyView : MapReadyView
	{
		// Token: 0x060002EB RID: 747 RVA: 0x00011328 File Offset: 0x0000F528
		protected override void CreateLayout()
		{
			base.CreateLayout();
			this._dataSource = new BoolItemWithActionVM(null, true, null);
			this._layerAsGauntletLayer = new GauntletLayer("MapReadyBlocker", 9999, false);
			this._layerAsGauntletLayer.LoadMovie("MapReadyBlocker", this._dataSource);
			base.Layer = this._layerAsGauntletLayer;
			base.MapScreen.AddLayer(base.Layer);
		}

		// Token: 0x060002EC RID: 748 RVA: 0x00011393 File Offset: 0x0000F593
		protected override void OnFinalize()
		{
			base.OnFinalize();
			this._dataSource.OnFinalize();
			base.MapScreen.RemoveLayer(base.Layer);
			base.Layer = null;
			this._dataSource = null;
		}

		// Token: 0x060002ED RID: 749 RVA: 0x000113C5 File Offset: 0x0000F5C5
		public override void SetIsMapSceneReady(bool isReady)
		{
			base.SetIsMapSceneReady(isReady);
			this._dataSource.IsActive = !isReady;
		}

		// Token: 0x060002EE RID: 750 RVA: 0x000113DD File Offset: 0x0000F5DD
		protected override void OnMapConversationStart()
		{
			base.OnMapConversationStart();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, true);
			}
		}

		// Token: 0x060002EF RID: 751 RVA: 0x000113F9 File Offset: 0x0000F5F9
		protected override void OnMapConversationOver()
		{
			base.OnMapConversationOver();
			if (this._layerAsGauntletLayer != null)
			{
				ScreenManager.SetSuspendLayer(this._layerAsGauntletLayer, false);
			}
		}

		// Token: 0x04000121 RID: 289
		private GauntletLayer _layerAsGauntletLayer;

		// Token: 0x04000122 RID: 290
		private BoolItemWithActionVM _dataSource;
	}
}
