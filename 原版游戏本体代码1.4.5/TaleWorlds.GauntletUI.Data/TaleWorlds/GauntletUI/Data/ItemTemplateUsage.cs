using TaleWorlds.GauntletUI.PrefabSystem;

namespace TaleWorlds.GauntletUI.Data;

public class ItemTemplateUsage
{
	public WidgetTemplate DefaultItemTemplate { get; private set; }

	public WidgetTemplate FirstItemTemplate { get; set; }

	public WidgetTemplate LastItemTemplate { get; set; }

	public ItemTemplateUsage(WidgetTemplate defaultItemTemplate)
	{
		DefaultItemTemplate = defaultItemTemplate;
	}
}
