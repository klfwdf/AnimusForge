using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;

public class EncyclopediaPageChangedEvent : EventBase
{
	public EncyclopediaPages NewPage { get; private set; }

	public bool NewPageHasHiddenInformation { get; private set; }

	public EncyclopediaPageChangedEvent(EncyclopediaPages newPage, bool hasHiddenInformation = false)
	{
		NewPage = newPage;
		NewPageHasHiddenInformation = hasHiddenInformation;
	}
}
