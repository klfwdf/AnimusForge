using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection;

public class SelectableFiefItemPropertyVM : SelectableItemPropertyVM
{
	private int _changeAmount;

	[DataSourceProperty]
	public int ChangeAmount
	{
		get
		{
			return _changeAmount;
		}
		set
		{
			if (value != _changeAmount)
			{
				_changeAmount = value;
				OnPropertyChangedWithValue(value, "ChangeAmount");
			}
		}
	}

	public SelectableFiefItemPropertyVM(string name, string value, int changeAmount, PropertyType type, BasicTooltipViewModel hint = null, bool isWarning = false)
		: base(name, value, isWarning, hint)
	{
		ChangeAmount = changeAmount;
		base.Type = (int)type;
	}
}
