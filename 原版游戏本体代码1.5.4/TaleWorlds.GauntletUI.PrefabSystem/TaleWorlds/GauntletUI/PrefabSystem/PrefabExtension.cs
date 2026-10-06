using System.Collections.Generic;
using System.Xml;

namespace TaleWorlds.GauntletUI.PrefabSystem;

public abstract class PrefabExtension
{
	protected internal virtual void RegisterAttributeTypes(WidgetAttributeContext widgetAttributeContext)
	{
	}

	protected internal virtual void OnWidgetCreated(WidgetCreationData widgetCreationData, WidgetInstantiationResult widgetInstantiationResult, int childCount)
	{
	}

	protected internal virtual void OnSave(PrefabExtensionContext prefabExtensionContext, XmlNode node, WidgetTemplate widgetTemplate)
	{
	}

	protected internal virtual void OnAttributesSet(WidgetCreationData widgetCreationData, WidgetInstantiationResult widgetInstantiationResult, Dictionary<string, WidgetAttributeTemplate> parameters)
	{
	}

	protected internal virtual void DoLoading(PrefabExtensionContext prefabExtensionContext, WidgetAttributeContext widgetAttributeContext, WidgetTemplate template, XmlNode node)
	{
	}

	protected internal virtual void OnLoadingFinished(WidgetPrefab widgetPrefab)
	{
	}

	protected internal virtual void AfterAttributesSet(WidgetCreationData widgetCreationData, WidgetInstantiationResult widgetInstantiationResult, Dictionary<string, WidgetAttributeTemplate> parameters)
	{
	}
}
