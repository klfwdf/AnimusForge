using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem;

public interface ITrackableCampaignObject : ITrackableBase
{
	bool IsReady { get; }

	Banner GetBanner();
}
