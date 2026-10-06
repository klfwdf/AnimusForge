namespace TaleWorlds.CampaignSystem;

public interface IDataStore
{
	bool IsSaving { get; }

	bool IsLoading { get; }

	bool SyncData<T>(string key, ref T data);
}
