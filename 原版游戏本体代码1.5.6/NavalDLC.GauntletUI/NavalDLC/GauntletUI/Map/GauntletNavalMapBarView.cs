using NavalDLC.View.Map.Navigation;
using NavalDLC.ViewModelCollection.Map.MapBar;
using SandBox.GauntletUI.Map;
using SandBox.View.Map;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace NavalDLC.GauntletUI.Map;

[OverrideView(typeof(MapBarView))]
public class GauntletNavalMapBarView : GauntletMapBarView
{
	protected override void CreateLayout()
	{
		_mapBarGlobalLayer = new GauntletNavalMapBarGlobalLayer(base.MapScreen, new NavalMapNavigationHandler(), 8.5f);
		_mapBarGlobalLayer.Initialize(new NavalMapBarVM());
		ScreenManager.AddGlobalLayer(_mapBarGlobalLayer, isFocusable: true);
	}
}
