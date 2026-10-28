using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map;

public class MapEventVisualBrushWidget(UIContext context) : BrushWidget(context)
{
	private bool _initialUpdate = true;

	private int _mapEventType = -1;

	[Editor(false)]
	public int MapEventType
	{
		get
		{
			return _mapEventType;
		}
		set
		{
			if (_mapEventType != value)
			{
				_mapEventType = value;
				UpdateVisual(value);
			}
		}
	}

	private void UpdateVisual(int type)
	{
		if (_initialUpdate)
		{
			this.RegisterBrushStatesOfWidget();
			_initialUpdate = false;
		}
		switch (type)
		{
		case 1:
			SetState("Raid");
			break;
		case 2:
			SetState("Siege");
			break;
		case 3:
			SetState("Battle");
			break;
		case 4:
			SetState("Rebellion");
			break;
		case 5:
			SetState("SallyOut");
			break;
		default:
			SetState("None");
			break;
		}
	}
}
