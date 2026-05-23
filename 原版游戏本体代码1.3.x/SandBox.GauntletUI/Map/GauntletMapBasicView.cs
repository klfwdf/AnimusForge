using System;
using SandBox.View.Map;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map
{
	// Token: 0x0200002E RID: 46
	[OverrideView(typeof(MapBasicView))]
	public class GauntletMapBasicView : MapView
	{
		// Token: 0x1700003A RID: 58
		// (get) Token: 0x0600023A RID: 570 RVA: 0x0000E1EF File Offset: 0x0000C3EF
		// (set) Token: 0x0600023B RID: 571 RVA: 0x0000E1F7 File Offset: 0x0000C3F7
		public GauntletLayer GauntletLayer { get; private set; }

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x0600023C RID: 572 RVA: 0x0000E200 File Offset: 0x0000C400
		// (set) Token: 0x0600023D RID: 573 RVA: 0x0000E208 File Offset: 0x0000C408
		public GauntletLayer GauntletNameplateLayer { get; private set; }

		// Token: 0x0600023E RID: 574 RVA: 0x0000E214 File Offset: 0x0000C414
		protected override void CreateLayout()
		{
			base.CreateLayout();
			this.GauntletLayer = new GauntletLayer("MapMenuView", 100, false);
			this.GauntletLayer.InputRestrictions.SetInputRestrictions(false, InputUsageMask.All);
			base.MapScreen.AddLayer(this.GauntletLayer);
			this.GauntletNameplateLayer = new GauntletLayer("MapNameplateLayer", 90, false);
			this.GauntletNameplateLayer.InputRestrictions.SetInputRestrictions(false, InputUsageMask.MouseButtons | InputUsageMask.Keyboardkeys);
			base.MapScreen.AddLayer(this.GauntletNameplateLayer);
		}

		// Token: 0x0600023F RID: 575 RVA: 0x0000E293 File Offset: 0x0000C493
		protected override void OnMapConversationStart()
		{
			base.OnMapConversationStart();
			ScreenManager.SetSuspendLayer(this.GauntletLayer, true);
			ScreenManager.SetSuspendLayer(this.GauntletNameplateLayer, true);
		}

		// Token: 0x06000240 RID: 576 RVA: 0x0000E2B3 File Offset: 0x0000C4B3
		protected override void OnMapConversationOver()
		{
			base.OnMapConversationOver();
			ScreenManager.SetSuspendLayer(this.GauntletLayer, false);
			ScreenManager.SetSuspendLayer(this.GauntletNameplateLayer, false);
		}

		// Token: 0x06000241 RID: 577 RVA: 0x0000E2D3 File Offset: 0x0000C4D3
		protected override void OnFinalize()
		{
			base.MapScreen.RemoveLayer(this.GauntletLayer);
			this.GauntletLayer = null;
			base.OnFinalize();
		}
	}
}
