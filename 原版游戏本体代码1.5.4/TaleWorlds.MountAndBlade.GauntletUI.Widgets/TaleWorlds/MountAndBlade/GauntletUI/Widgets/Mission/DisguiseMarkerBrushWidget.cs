using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;

public class DisguiseMarkerBrushWidget : BrushWidget
{
	private string _offenseTypeIdentifier;

	public Vec2 Position { get; set; }

	public string OffenseTypeIdentifier
	{
		get
		{
			return _offenseTypeIdentifier;
		}
		set
		{
			if (value != _offenseTypeIdentifier)
			{
				_offenseTypeIdentifier = value;
				OnPropertyChanged(value, "OffenseTypeIdentifier");
				UpdateState();
			}
		}
	}

	public DisguiseMarkerBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		base.ScaledPositionYOffset = Position.y - base.Size.Y / 2f;
		base.ScaledPositionXOffset = Position.x - base.Size.X / 2f;
	}

	private void UpdateState()
	{
		SetState(OffenseTypeIdentifier);
	}
}
