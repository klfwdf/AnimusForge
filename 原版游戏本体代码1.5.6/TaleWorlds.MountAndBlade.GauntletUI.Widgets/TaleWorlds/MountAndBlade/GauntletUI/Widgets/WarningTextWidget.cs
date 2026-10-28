using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class WarningTextWidget : TextWidget
{
	private bool _isWarned;

	[Editor(false)]
	public bool IsWarned
	{
		get
		{
			return _isWarned;
		}
		set
		{
			if (_isWarned != value)
			{
				_isWarned = value;
				OnPropertyChanged(value, "IsWarned");
				SetState(_isWarned ? "Warned" : "Default");
			}
		}
	}

	public WarningTextWidget(UIContext context)
		: base(context)
	{
		base.UseGlobalTimeForAnimation = true;
	}
}
