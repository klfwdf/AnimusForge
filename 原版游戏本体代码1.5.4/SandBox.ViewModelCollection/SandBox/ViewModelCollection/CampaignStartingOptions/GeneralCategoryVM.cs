using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.CampaignStartingOptions;

public class GeneralCategoryVM : StartingOptionCategoryVM
{
	public GeneralCategoryVM(string categoryId, TextObject name)
		: base(categoryId, name)
	{
	}

	public override string GetDescription()
	{
		return string.Empty;
	}
}
