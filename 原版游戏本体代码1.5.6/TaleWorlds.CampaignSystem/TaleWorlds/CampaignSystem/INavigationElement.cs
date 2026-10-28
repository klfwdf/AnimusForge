using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem;

public interface INavigationElement
{
	string StringId { get; }

	NavigationPermissionItem Permission { get; }

	bool IsLockingNavigation { get; }

	bool IsActive { get; }

	TextObject Tooltip { get; }

	bool HasAlert { get; }

	TextObject AlertTooltip { get; }

	void OpenView();

	void OpenView(params object[] parameters);

	void GoToLink();
}
