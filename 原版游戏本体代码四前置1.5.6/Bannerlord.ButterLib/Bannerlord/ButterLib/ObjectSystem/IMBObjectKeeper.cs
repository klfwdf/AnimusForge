using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.ButterLib.ObjectSystem;

public interface IMBObjectKeeper
{
	void Keep(MBObjectBase mbObject);

	void Sync(IDataStore dataStore);
}
