using System;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.PrefabSystem;

namespace TaleWorlds.GauntletUI.Data
{
	// Token: 0x02000014 RID: 20
	public static class WidgetInstantiationResultDatabindingExtension
	{
		// Token: 0x060000AD RID: 173 RVA: 0x000045B8 File Offset: 0x000027B8
		public static GauntletView GetGauntletView(this WidgetInstantiationResult widgetInstantiationResult)
		{
			Widget widget;
			if (widgetInstantiationResult.CustomWidgetInstantiationData != null)
			{
				widget = widgetInstantiationResult.CustomWidgetInstantiationData.Widget;
			}
			else
			{
				widget = widgetInstantiationResult.Widget;
			}
			return widget.GetComponent<GauntletView>();
		}
	}
}
