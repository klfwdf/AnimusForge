using System.Text;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.CampaignStartingOptions;

public class GlobalModifiersCategoryVM : StartingOptionCategoryVM
{
	public GlobalModifiersCategoryVM(string categoryId, TextObject name)
		: base(categoryId, name)
	{
	}

	public override string GetDescription()
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < base.Options.Count; i++)
		{
			StartingOptionVM startingOptionVM = base.Options[i];
			if (!StartingOptionCategoryVM.IsOptionRelevant(startingOptionVM))
			{
				continue;
			}
			switch (startingOptionVM.OptionType)
			{
			case 0:
				if (startingOptionVM.ValueAsBoolean)
				{
					StartingOptionCategoryVM.AppendEntry(stringBuilder, startingOptionVM.Name, "\n");
				}
				break;
			case 1:
				StartingOptionCategoryVM.AppendEntry(stringBuilder, startingOptionVM.Name + ": " + startingOptionVM.ValueAsString, "\n");
				break;
			}
		}
		return stringBuilder.ToString();
	}
}
