using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Menu.Overlay;

public class SettlementOverlayWallIconBrushWidget : BrushWidget
{
	private int _wallsLevel;

	[Editor(false)]
	public int WallsLevel
	{
		get
		{
			return _wallsLevel;
		}
		set
		{
			if (_wallsLevel != value)
			{
				_wallsLevel = value;
				OnPropertyChanged(value, "WallsLevel");
			}
		}
	}

	public SettlementOverlayWallIconBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		SetState(WallsLevel.ToString());
	}
}
