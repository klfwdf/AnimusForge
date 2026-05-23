namespace TaleWorlds.CampaignSystem;

public interface ISaveManager
{
	int GetAutoSaveInterval();

	bool IsAutoSaveDisabled();

	void OnSaveOver(bool isSuccessful, string newSaveGameName);
}
