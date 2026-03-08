using System;
using SandBox.View.Map;
using SandBox.View.Map.Navigation;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map
{
	// Token: 0x0200002C RID: 44
	[OverrideView(typeof(MapBarView))]
	public class GauntletMapBarView : MapView
	{
		// Token: 0x06000223 RID: 547 RVA: 0x0000D728 File Offset: 0x0000B928
		protected override void OnMapConversationStart()
		{
			base.OnMapConversationStart();
			this._mapBarGlobalLayer.OnMapConversationStarted();
		}

		// Token: 0x06000224 RID: 548 RVA: 0x0000D73B File Offset: 0x0000B93B
		protected override void OnMapConversationOver()
		{
			base.OnMapConversationOver();
			this._mapBarGlobalLayer.OnMapConversationOver();
		}

		// Token: 0x06000225 RID: 549 RVA: 0x0000D74E File Offset: 0x0000B94E
		protected override void CreateLayout()
		{
			base.CreateLayout();
			this._mapBarGlobalLayer = new GauntletMapBarGlobalLayer(base.MapScreen, new MapNavigationHandler(), 8.5f);
			this._mapBarGlobalLayer.Initialize(new MapBarVM());
			ScreenManager.AddGlobalLayer(this._mapBarGlobalLayer, true);
		}

		// Token: 0x06000226 RID: 550 RVA: 0x0000D78D File Offset: 0x0000B98D
		protected override void OnFinalize()
		{
			this._mapBarGlobalLayer.OnFinalize();
			ScreenManager.RemoveGlobalLayer(this._mapBarGlobalLayer);
			base.OnFinalize();
		}

		// Token: 0x06000227 RID: 551 RVA: 0x0000D7AB File Offset: 0x0000B9AB
		protected override void OnResume()
		{
			base.OnResume();
			this._mapBarGlobalLayer.Refresh();
		}

		// Token: 0x06000228 RID: 552 RVA: 0x0000D7BE File Offset: 0x0000B9BE
		protected override bool IsEscaped()
		{
			return this._mapBarGlobalLayer.IsEscaped();
		}

		// Token: 0x06000229 RID: 553 RVA: 0x0000D7CB File Offset: 0x0000B9CB
		protected override TutorialContexts GetTutorialContext()
		{
			if (this._mapBarGlobalLayer.IsInArmyManagement)
			{
				return TutorialContexts.ArmyManagement;
			}
			return base.GetTutorialContext();
		}

		// Token: 0x040000BE RID: 190
		protected GauntletMapBarGlobalLayer _mapBarGlobalLayer;
	}
}
