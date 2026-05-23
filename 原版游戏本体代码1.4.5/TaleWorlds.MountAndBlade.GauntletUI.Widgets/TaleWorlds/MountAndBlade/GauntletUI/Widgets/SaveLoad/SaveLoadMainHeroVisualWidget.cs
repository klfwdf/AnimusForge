using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.SaveLoad;

public class SaveLoadMainHeroVisualWidget : Widget
{
	public Widget DefaultVisualWidget { get; set; }

	public SaveLoadHeroTableauWidget SaveLoadHeroTableau { get; set; }

	public bool IsVisualDisabledForMemoryPurposes { get; set; }

	public SaveLoadMainHeroVisualWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (DefaultVisualWidget != null)
		{
			if (IsVisualDisabledForMemoryPurposes)
			{
				DefaultVisualWidget.IsVisible = true;
				SaveLoadHeroTableau.IsVisible = false;
			}
			else
			{
				DefaultVisualWidget.IsVisible = string.IsNullOrEmpty(SaveLoadHeroTableau.HeroVisualCode) || !SaveLoadHeroTableau.IsVersionCompatible;
				SaveLoadHeroTableau.IsVisible = !DefaultVisualWidget.IsVisible;
			}
		}
	}
}
