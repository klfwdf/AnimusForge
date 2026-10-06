using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.PrefabSystem;

namespace TaleWorlds.GauntletUI.Data;

public static class WidgetInstantiationResultDatabindingExtension
{
	public static GauntletView GetGauntletView(this WidgetInstantiationResult widgetInstantiationResult)
	{
		Widget widget = null;
		widget = ((widgetInstantiationResult.CustomWidgetInstantiationData == null) ? widgetInstantiationResult.Widget : widgetInstantiationResult.CustomWidgetInstantiationData.Widget);
		return widget.GetComponent<GauntletView>();
	}
}
