using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Clan;

public class ClanLordStatusWidget : Widget
{
	private int _statusType = -1;

	[Editor(false)]
	public int StatusType
	{
		get
		{
			return _statusType;
		}
		set
		{
			if (_statusType != value)
			{
				_statusType = value;
				OnPropertyChanged(value, "StatusType");
				SetVisualState(value);
			}
		}
	}

	public ClanLordStatusWidget(UIContext context)
		: base(context)
	{
	}

	private void SetVisualState(int type)
	{
		switch (type)
		{
		case 0:
			SetState("Dead");
			break;
		case 1:
			SetState("Married");
			break;
		case 2:
			SetState("Pregnant");
			break;
		case 3:
			SetState("InBattle");
			break;
		case 4:
			SetState("InSiege");
			break;
		case 5:
			SetState("Child");
			break;
		case 6:
			SetState("Prisoner");
			break;
		case 7:
			SetState("Sick");
			break;
		}
	}
}
