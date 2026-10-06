using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.LogEntries;

public interface IEncyclopediaLog
{
	CampaignTime GameTime { get; }

	bool IsVisibleInEncyclopediaPageOf(MBObjectBase obj);

	TextObject GetEncyclopediaText();
}
