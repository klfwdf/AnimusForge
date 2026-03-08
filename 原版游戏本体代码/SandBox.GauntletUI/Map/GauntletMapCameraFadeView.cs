using System;
using SandBox.View.Map;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map
{
	// Token: 0x02000030 RID: 48
	[OverrideView(typeof(MapCameraFadeView))]
	public class GauntletMapCameraFadeView : MapCameraFadeView
	{
		// Token: 0x06000249 RID: 585 RVA: 0x0000E620 File Offset: 0x0000C820
		protected override void CreateLayout()
		{
			base.CreateLayout();
			this._dataSource = new BindingListFloatItem(0f);
			this._layer = new GauntletLayer("MapCameraFade", 100000, false);
			this._layer.LoadMovie("CameraFade", this._dataSource);
			base.MapScreen.AddLayer(this._layer);
		}

		// Token: 0x0600024A RID: 586 RVA: 0x0000E681 File Offset: 0x0000C881
		private void Tick(float dt)
		{
			if (this._dataSource != null)
			{
				this._dataSource.Item = base.FadeAlpha;
			}
		}

		// Token: 0x0600024B RID: 587 RVA: 0x0000E69C File Offset: 0x0000C89C
		protected override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			this.Tick(dt);
		}

		// Token: 0x0600024C RID: 588 RVA: 0x0000E6AC File Offset: 0x0000C8AC
		protected override void OnIdleTick(float dt)
		{
			base.OnIdleTick(dt);
			this.Tick(dt);
		}

		// Token: 0x0600024D RID: 589 RVA: 0x0000E6BC File Offset: 0x0000C8BC
		protected override void OnMenuModeTick(float dt)
		{
			base.OnMenuModeTick(dt);
			this.Tick(dt);
		}

		// Token: 0x0600024E RID: 590 RVA: 0x0000E6CC File Offset: 0x0000C8CC
		protected override void OnFinalize()
		{
			base.OnFinalize();
			base.MapScreen.RemoveLayer(this._layer);
			this._dataSource = null;
			this._layer = null;
		}

		// Token: 0x0600024F RID: 591 RVA: 0x0000E6F3 File Offset: 0x0000C8F3
		protected override void OnMapConversationStart()
		{
			base.OnMapConversationStart();
			if (this._layer != null)
			{
				ScreenManager.SetSuspendLayer(this._layer, true);
			}
		}

		// Token: 0x06000250 RID: 592 RVA: 0x0000E70F File Offset: 0x0000C90F
		protected override void OnMapConversationOver()
		{
			base.OnMapConversationOver();
			if (this._layer != null)
			{
				ScreenManager.SetSuspendLayer(this._layer, false);
			}
		}

		// Token: 0x040000D1 RID: 209
		private GauntletLayer _layer;

		// Token: 0x040000D2 RID: 210
		private BindingListFloatItem _dataSource;
	}
}
