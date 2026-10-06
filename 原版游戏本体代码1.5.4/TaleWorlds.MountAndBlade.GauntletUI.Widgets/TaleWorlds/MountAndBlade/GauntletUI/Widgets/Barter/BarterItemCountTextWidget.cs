using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Barter;

public class BarterItemCountTextWidget : TextWidget
{
	private int _count;

	[Editor(false)]
	public int Count
	{
		get
		{
			return _count;
		}
		set
		{
			if (_count != value)
			{
				_count = value;
				OnPropertyChanged(value, "Count");
				base.IntText = value;
				base.IsVisible = value > 1;
			}
		}
	}

	public BarterItemCountTextWidget(UIContext context)
		: base(context)
	{
	}
}
