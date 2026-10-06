using SandBox.View.Map;
using SandBox.View.Map.Navigation;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Map;

[OverrideView(typeof(MapBarView))]
public class GauntletMapBarView : MapView
{
	protected GauntletMapBarGlobalLayer _mapBarGlobalLayer;

	protected override void OnMapConversationStart()
	{
		base.OnMapConversationStart();
		_mapBarGlobalLayer.OnMapConversationStarted();
	}

	protected override void OnMapConversationOver()
	{
		base.OnMapConversationOver();
		_mapBarGlobalLayer.OnMapConversationOver();
	}

	protected override void CreateLayout()
	{
		base.CreateLayout();
		_mapBarGlobalLayer = new GauntletMapBarGlobalLayer(base.MapScreen, new MapNavigationHandler(), 8.5f);
		_mapBarGlobalLayer.Initialize(new MapBarVM());
		ScreenManager.AddGlobalLayer(_mapBarGlobalLayer, isFocusable: true);
	}

	protected override void OnFinalize()
	{
		_mapBarGlobalLayer.OnFinalize();
		ScreenManager.RemoveGlobalLayer(_mapBarGlobalLayer);
		base.OnFinalize();
	}

	protected override void OnResume()
	{
		base.OnResume();
		_mapBarGlobalLayer.Refresh();
	}

	protected override bool IsEscaped()
	{
		return _mapBarGlobalLayer.IsEscaped();
	}

	protected override TutorialContexts GetTutorialContext()
	{
		if (_mapBarGlobalLayer.IsInArmyManagement)
		{
			return TutorialContexts.ArmyManagement;
		}
		return base.GetTutorialContext();
	}
}
