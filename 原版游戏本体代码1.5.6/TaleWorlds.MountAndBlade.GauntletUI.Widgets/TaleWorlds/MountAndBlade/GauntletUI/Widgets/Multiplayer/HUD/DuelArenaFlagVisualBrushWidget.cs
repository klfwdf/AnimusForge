using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.HUD;

public class DuelArenaFlagVisualBrushWidget(UIContext context) : BrushWidget(context)
{
	private int _arenaType = -1;

	[Editor(false)]
	public int ArenaType
	{
		get
		{
			return _arenaType;
		}
		set
		{
			if (_arenaType != value)
			{
				_arenaType = value;
				OnPropertyChanged(value, "ArenaType");
				UpdateVisual();
			}
		}
	}

	private void UpdateVisual()
	{
		switch (ArenaType)
		{
		case 0:
			SetState("Infantry");
			break;
		case 1:
			SetState("Archery");
			break;
		case 2:
			SetState("Cavalry");
			break;
		default:
			SetState("Infantry");
			break;
		}
	}
}
