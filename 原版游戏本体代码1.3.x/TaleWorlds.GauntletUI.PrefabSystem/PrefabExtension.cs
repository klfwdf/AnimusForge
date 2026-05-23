using System;
using System.Collections.Generic;
using System.Xml;

namespace TaleWorlds.GauntletUI.PrefabSystem
{
	// Token: 0x02000009 RID: 9
	public abstract class PrefabExtension
	{
		// Token: 0x06000037 RID: 55 RVA: 0x00002845 File Offset: 0x00000A45
		protected internal virtual void RegisterAttributeTypes(WidgetAttributeContext widgetAttributeContext)
		{
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00002847 File Offset: 0x00000A47
		protected internal virtual void OnWidgetCreated(WidgetCreationData widgetCreationData, WidgetInstantiationResult widgetInstantiationResult, int childCount)
		{
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00002849 File Offset: 0x00000A49
		protected internal virtual void OnSave(PrefabExtensionContext prefabExtensionContext, XmlNode node, WidgetTemplate widgetTemplate)
		{
		}

		// Token: 0x0600003A RID: 58 RVA: 0x0000284B File Offset: 0x00000A4B
		protected internal virtual void OnAttributesSet(WidgetCreationData widgetCreationData, WidgetInstantiationResult widgetInstantiationResult, Dictionary<string, WidgetAttributeTemplate> parameters)
		{
		}

		// Token: 0x0600003B RID: 59 RVA: 0x0000284D File Offset: 0x00000A4D
		protected internal virtual void DoLoading(PrefabExtensionContext prefabExtensionContext, WidgetAttributeContext widgetAttributeContext, WidgetTemplate template, XmlNode node)
		{
		}

		// Token: 0x0600003C RID: 60 RVA: 0x0000284F File Offset: 0x00000A4F
		protected internal virtual void OnLoadingFinished(WidgetPrefab widgetPrefab)
		{
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00002851 File Offset: 0x00000A51
		protected internal virtual void AfterAttributesSet(WidgetCreationData widgetCreationData, WidgetInstantiationResult widgetInstantiationResult, Dictionary<string, WidgetAttributeTemplate> parameters)
		{
		}
	}
}
