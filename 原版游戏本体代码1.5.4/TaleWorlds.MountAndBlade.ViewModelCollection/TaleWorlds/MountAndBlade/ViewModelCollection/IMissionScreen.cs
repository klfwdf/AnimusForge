namespace TaleWorlds.MountAndBlade.ViewModelCollection;

public interface IMissionScreen
{
	bool GetDisplayDialog();

	void SetOrderFlagVisibility(bool value);

	string GetFollowText();

	string GetFollowPartyText();
}
