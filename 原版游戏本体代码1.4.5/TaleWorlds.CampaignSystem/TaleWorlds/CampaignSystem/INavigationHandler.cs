namespace TaleWorlds.CampaignSystem;

public interface INavigationHandler
{
	bool IsNavigationLocked { get; set; }

	INavigationElement[] GetElements();

	INavigationElement GetElement(string id);

	bool IsAnyElementActive();
}
