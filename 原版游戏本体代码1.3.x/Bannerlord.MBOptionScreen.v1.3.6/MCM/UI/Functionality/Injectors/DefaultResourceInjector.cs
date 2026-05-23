using System;
using Bannerlord.UIExtenderEx.ResourceManager;

namespace MCM.UI.Functionality.Injectors
{
	// Token: 0x02000026 RID: 38
	internal class DefaultResourceInjector : ResourceInjector
	{
		// Token: 0x0600017E RID: 382 RVA: 0x00006E6C File Offset: 0x0000506C
		public override void Inject()
		{
			BrushFactoryManager.CreateAndRegister(ResourceInjector.Load("MCM.UI.GUI.Brushes.ButtonBrushes.xml"));
			BrushFactoryManager.CreateAndRegister(ResourceInjector.Load("MCM.UI.GUI.Brushes.CompatibilityBrushes.xml"));
			BrushFactoryManager.CreateAndRegister(ResourceInjector.Load("MCM.UI.GUI.Brushes.DividerBrushes.xml"));
			BrushFactoryManager.CreateAndRegister(ResourceInjector.Load("MCM.UI.GUI.Brushes.ExpandIndicator.xml"));
			BrushFactoryManager.CreateAndRegister(ResourceInjector.Load("MCM.UI.GUI.Brushes.SettingsBrush.xml"));
			BrushFactoryManager.CreateAndRegister(ResourceInjector.Load("MCM.UI.GUI.Brushes.TextBrushes.xml"));
			WidgetFactoryManager.CreateAndRegister("ModOptionsView_MCM", ResourceInjector.Load("MCM.UI.GUI.Prefabs.ModOptionsView.xml"));
			WidgetFactoryManager.CreateAndRegister("DropdownWithHorizontalControlCheckboxView", ResourceInjector.Load("MCM.UI.GUI.Prefabs.DropdownWithHorizontalControl.Checkbox.xml"));
			WidgetFactoryManager.CreateAndRegister("ModOptionsPageView", ResourceInjector.Load("MCM.UI.GUI.Prefabs.ModOptionsPageView.xml"));
			WidgetFactoryManager.CreateAndRegister("SettingsItemView", ResourceInjector.Load("MCM.UI.GUI.Prefabs.SettingsItemView.xml"));
			WidgetFactoryManager.CreateAndRegister("SettingsPropertyGroupView", ResourceInjector.Load("MCM.UI.GUI.Prefabs.SettingsPropertyGroupView.xml"));
			WidgetFactoryManager.CreateAndRegister("SettingsPropertyView", ResourceInjector.Load("MCM.UI.GUI.Prefabs.SettingsPropertyView.xml"));
			WidgetFactoryManager.CreateAndRegister("SettingsPropertyDisplayValueView", ResourceInjector.Load("MCM.UI.GUI.Prefabs.SettingsPropertyDisplayValueView.xml"));
			WidgetFactoryManager.CreateAndRegister("SettingsView", ResourceInjector.Load("MCM.UI.GUI.Prefabs.SettingsView.xml"));
			WidgetFactoryManager.CreateAndRegister("SettingsPropertyBoolView", ResourceInjector.Load("MCM.UI.GUI.Prefabs.Properties.SettingsPropertyBoolView.xml"));
			WidgetFactoryManager.CreateAndRegister("SettingsPropertyButtonView", ResourceInjector.Load("MCM.UI.GUI.Prefabs.Properties.SettingsPropertyButtonView.xml"));
			WidgetFactoryManager.CreateAndRegister("SettingsPropertyDropdownView", ResourceInjector.Load("MCM.UI.GUI.Prefabs.Properties.SettingsPropertyDropdownView.xml"));
			WidgetFactoryManager.CreateAndRegister("SettingsPropertyFloatView", ResourceInjector.Load("MCM.UI.GUI.Prefabs.Properties.SettingsPropertyFloatView.xml"));
			WidgetFactoryManager.CreateAndRegister("SettingsPropertyIntView", ResourceInjector.Load("MCM.UI.GUI.Prefabs.Properties.SettingsPropertyIntView.xml"));
			WidgetFactoryManager.CreateAndRegister("SettingsPropertyStringView", ResourceInjector.Load("MCM.UI.GUI.Prefabs.Properties.SettingsPropertyStringView.xml"));
		}
	}
}
