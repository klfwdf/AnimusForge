using System.Collections.Generic;
using TaleWorlds.GauntletUI.PrefabSystem;

namespace TaleWorlds.GauntletUI.Data;

public class ItemTemplateUsageWithData
{
	public Dictionary<string, WidgetAttributeTemplate> GivenParameters { get; private set; }

	public ItemTemplateUsage ItemTemplateUsage { get; private set; }

	public WidgetTemplate DefaultItemTemplate => ItemTemplateUsage.DefaultItemTemplate;

	public WidgetTemplate FirstItemTemplate => ItemTemplateUsage.FirstItemTemplate;

	public WidgetTemplate LastItemTemplate => ItemTemplateUsage.LastItemTemplate;

	public ItemTemplateUsageWithData(ItemTemplateUsage itemTemplateUsage)
	{
		GivenParameters = new Dictionary<string, WidgetAttributeTemplate>();
		ItemTemplateUsage = itemTemplateUsage;
	}
}
