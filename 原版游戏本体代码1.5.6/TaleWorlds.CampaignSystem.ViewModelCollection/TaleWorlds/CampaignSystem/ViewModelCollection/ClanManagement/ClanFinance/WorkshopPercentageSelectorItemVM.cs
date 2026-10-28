using TaleWorlds.Core.ViewModelCollection.Selector;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanFinance;

public class WorkshopPercentageSelectorItemVM : SelectorItemVM
{
	public readonly float Percentage;

	public WorkshopPercentageSelectorItemVM(string s, float percentage)
		: base(s)
	{
		Percentage = percentage;
	}
}
