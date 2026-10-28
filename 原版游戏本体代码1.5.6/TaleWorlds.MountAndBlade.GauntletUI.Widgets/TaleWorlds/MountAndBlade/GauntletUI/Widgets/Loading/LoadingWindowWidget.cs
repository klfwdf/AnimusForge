using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Loading;

public class LoadingWindowWidget : Widget
{
	private const string _defaultBackgroundSpriteData = "background_1";

	private const float _animWidgetMaxOffset = 200f;

	private float _totalDt;

	private Widget _animWidget;

	private bool _isActive;

	private string _imageName;

	[Editor(false)]
	public Widget AnimWidget
	{
		get
		{
			return _animWidget;
		}
		set
		{
			if (_animWidget != value)
			{
				_animWidget = value;
				OnPropertyChanged(value, "AnimWidget");
			}
		}
	}

	[Editor(false)]
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (_isActive != value)
			{
				_isActive = value;
				OnPropertyChanged(value, "IsActive");
				UpdateStates();
			}
		}
	}

	[Editor(false)]
	public string ImageName
	{
		get
		{
			return _imageName;
		}
		set
		{
			if (_imageName != value)
			{
				_imageName = value;
				OnPropertyChanged(value, "ImageName");
				UpdateImage(value);
			}
		}
	}

	public LoadingWindowWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (AnimWidget != null && base.IsVisible && AnimWidget.IsVisible)
		{
			AnimWidget.PositionXOffset = MathF.PingPong(-200f, 200f, _totalDt);
			_totalDt += dt * 500f;
		}
	}

	private void UpdateStates()
	{
		base.IsVisible = IsActive;
		base.IsEnabled = IsActive;
		base.ParentWidget.IsVisible = IsActive;
		base.ParentWidget.IsEnabled = IsActive;
	}

	private void UpdateImage(string imageName)
	{
		Sprite sprite = base.Context.SpriteData.GetSprite(imageName);
		if (sprite == null)
		{
			base.Sprite = base.Context.SpriteData.GetSprite("background_1");
		}
		else
		{
			base.Sprite = sprite;
		}
	}
}
