using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.NameMarker;

public class AlwaysVisibleNameMarkerListPanel : ListPanel
{
	private Widget _parentScreenWidget;

	private float _totalDt;

	private Vec2 _position;

	private float _normalOpacity => 0.5f;

	private float _screenCenterOpacity => 0.15f;

	private float _stayOnScreenTimeInSeconds => 5f;

	[DataSourceProperty]
	public Vec2 Position
	{
		get
		{
			return _position;
		}
		set
		{
			if (_position != value)
			{
				_position = value;
				OnPropertyChanged(value, "Position");
			}
		}
	}

	public AlwaysVisibleNameMarkerListPanel(UIContext context)
		: base(context)
	{
		_parentScreenWidget = base.EventManager.Root.GetChild(0).GetChild(0);
	}

	protected override void OnLateUpdate(float dt)
	{
		ApplyActionToAllChildrenRecursive(delegate(Widget child)
		{
			child.IsVisible = true;
		});
		base.ScaledPositionYOffset = Position.y - base.Size.Y / 2f;
		base.ScaledPositionXOffset = Position.x - base.Size.X / 2f;
		UpdateOpacity();
		if (_totalDt > _stayOnScreenTimeInSeconds)
		{
			EventFired("Remove");
		}
		_totalDt += dt;
	}

	private void UpdateOpacity()
	{
		Vec2 v = new Vec2(base.Context.TwoDimensionContext.Platform.Width / 2f, base.Context.TwoDimensionContext.Platform.Height / 2f);
		float alphaFactor = ((new Vec2(base.ScaledPositionXOffset, base.ScaledPositionYOffset).Distance(v) <= 150f) ? _screenCenterOpacity : _normalOpacity);
		this.SetGlobalAlphaRecursively(alphaFactor);
	}
}
