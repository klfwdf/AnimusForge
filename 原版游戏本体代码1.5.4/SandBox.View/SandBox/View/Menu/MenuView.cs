using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;

namespace SandBox.View.Menu;

public abstract class MenuView : SandboxView
{
	protected const float ContextAlphaModifier = 8.5f;

	internal bool Removed { get; set; }

	public virtual bool ShouldUpdateMenuAfterRemoved => false;

	public MenuViewContext MenuViewContext { get; internal set; }

	public MenuContext MenuContext { get; internal set; }

	protected internal virtual void OnMenuContextUpdated(MenuContext newMenuContext)
	{
	}

	protected internal virtual void OnMenuContextRefreshed()
	{
	}

	protected internal virtual void OnOverlayTypeChange(GameMenu.MenuOverlayType newType)
	{
	}

	protected internal virtual void OnCharacterDeveloperOpened()
	{
	}

	protected internal virtual void OnCharacterDeveloperClosed()
	{
	}

	protected internal virtual void OnBackgroundMeshNameSet(string name)
	{
	}

	protected internal virtual void OnHourlyTick()
	{
	}

	protected internal virtual void OnResume()
	{
	}

	protected internal virtual void OnMapConversationActivated()
	{
	}

	protected internal virtual void OnMapConversationDeactivated()
	{
	}

	protected internal virtual TutorialContexts GetTutorialContext()
	{
		return TutorialContexts.MapWindow;
	}
}
