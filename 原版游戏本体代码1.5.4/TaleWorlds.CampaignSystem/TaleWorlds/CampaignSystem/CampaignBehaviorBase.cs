namespace TaleWorlds.CampaignSystem;

public abstract class CampaignBehaviorBase : ICampaignBehavior
{
	public readonly string StringId;

	public CampaignBehaviorBase(string stringId)
	{
		StringId = stringId;
	}

	public CampaignBehaviorBase()
	{
		StringId = GetType().Name;
	}

	public abstract void RegisterEvents();

	public static T GetCampaignBehavior<T>()
	{
		return Campaign.Current.GetCampaignBehavior<T>();
	}

	public abstract void SyncData(IDataStore dataStore);
}
