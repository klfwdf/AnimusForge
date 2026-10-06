using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem;

public struct NavigationPermissionItem
{
	public bool IsAuthorized { get; private set; }

	public TextObject ReasonString { get; private set; }

	public NavigationPermissionItem(bool isAuthorized, TextObject reasonString)
	{
		IsAuthorized = isAuthorized;
		ReasonString = reasonString;
	}
}
