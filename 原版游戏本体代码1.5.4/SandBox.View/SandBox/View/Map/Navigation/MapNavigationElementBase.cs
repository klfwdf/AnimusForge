using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace SandBox.View.Map.Navigation;

public abstract class MapNavigationElementBase : INavigationElement
{
	protected readonly MapNavigationHandler _handler;

	protected readonly IViewDataTracker _viewDataTracker;

	public NavigationPermissionItem Permission => GetPermission();

	public TextObject Tooltip => GetTooltip();

	public TextObject AlertTooltip => GetAlertTooltip();

	public abstract bool IsActive { get; }

	public abstract bool IsLockingNavigation { get; }

	public abstract bool HasAlert { get; }

	public abstract string StringId { get; }

	protected Game _game => Game.Current;

	public abstract void OpenView();

	public abstract void OpenView(params object[] parameters);

	public abstract void GoToLink();

	public MapNavigationElementBase(MapNavigationHandler handler)
	{
		_handler = handler;
		_viewDataTracker = Campaign.Current.GetCampaignBehavior<IViewDataTracker>();
	}

	protected abstract NavigationPermissionItem GetPermission();

	protected abstract TextObject GetTooltip();

	protected abstract TextObject GetAlertTooltip();
}
