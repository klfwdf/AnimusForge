using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.Siege;

public class MapSiegePOIBrushWidget : BrushWidget
{
	public enum AnimState
	{
		Idle,
		Start,
		Starting,
		Playing
	}

	private AnimState _animState;

	private bool _isBrushChanged;

	private int _tickCount;

	private bool _isConstructing;

	private bool _isPlayerSidePOI;

	private bool _isInVisibleRange;

	private bool _isPOISelected;

	private BrushWidget _hammerAnimWidget;

	private Widget _machineTypeIconWidget;

	private int _machineType = -1;

	private int _queueIndex = -1;

	private MapSiegeConstructionControllerWidget _constructionControllerWidget;

	private Color _fullColor => new Color(0.2784314f, 84f / 85f, 0.44313726f);

	private Color _emptyColor => new Color(84f / 85f, 0.2784314f, 0.2784314f);

	public SliderWidget Slider { get; set; }

	public Brush ConstructionBrush { get; set; }

	public Brush NormalBrush { get; set; }

	public Vec2 ScreenPosition { get; set; }

	public MapSiegeConstructionControllerWidget ConstructionControllerWidget
	{
		get
		{
			return _constructionControllerWidget;
		}
		set
		{
			if (_constructionControllerWidget != value)
			{
				_constructionControllerWidget = value;
			}
		}
	}

	public bool IsPlayerSidePOI
	{
		get
		{
			return _isPlayerSidePOI;
		}
		set
		{
			if (_isPlayerSidePOI != value)
			{
				_isPlayerSidePOI = value;
			}
		}
	}

	public bool IsInVisibleRange
	{
		get
		{
			return _isInVisibleRange;
		}
		set
		{
			if (_isInVisibleRange != value)
			{
				_isInVisibleRange = value;
			}
		}
	}

	public bool IsPOISelected
	{
		get
		{
			return _isPOISelected;
		}
		set
		{
			if (_isPOISelected != value)
			{
				_isPOISelected = value;
				ConstructionControllerWidget.SetCurrentPOIWidget(value ? this : null);
			}
		}
	}

	public bool IsConstructing
	{
		get
		{
			return _isConstructing;
		}
		set
		{
			if (_isConstructing != value)
			{
				_isConstructing = value;
				_isBrushChanged = false;
				_animState = AnimState.Idle;
			}
		}
	}

	public int MachineType
	{
		get
		{
			return _machineType;
		}
		set
		{
			if (_machineType != value)
			{
				_machineType = value;
				SetMachineTypeIcon(value);
			}
		}
	}

	public int QueueIndex
	{
		get
		{
			return _queueIndex;
		}
		set
		{
			if (_queueIndex != value)
			{
				_queueIndex = value;
				_animState = AnimState.Start;
				_tickCount = 0;
			}
		}
	}

	public Widget MachineTypeIconWidget
	{
		get
		{
			return _machineTypeIconWidget;
		}
		set
		{
			if (_machineTypeIconWidget != value)
			{
				_machineTypeIconWidget = value;
			}
		}
	}

	public BrushWidget HammerAnimWidget
	{
		get
		{
			return _hammerAnimWidget;
		}
		set
		{
			if (_hammerAnimWidget != value)
			{
				_hammerAnimWidget = value;
			}
		}
	}

	public MapSiegePOIBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		base.ScaledPositionXOffset = ScreenPosition.x - base.Size.X / 2f;
		base.ScaledPositionYOffset = ScreenPosition.y;
		float valueTo = (IsInVisibleRange ? 1 : 0);
		float alphaFactor = MathF.Lerp(base.ReadOnlyBrush.GlobalAlphaFactor, valueTo, dt * 10f);
		this.SetGlobalAlphaRecursively(alphaFactor);
		base.IsEnabled = false;
		if (_animState == AnimState.Start)
		{
			_tickCount++;
			if (_tickCount > 5)
			{
				_animState = AnimState.Starting;
			}
		}
		else if (_animState == AnimState.Starting)
		{
			(Slider.Filler as BrushWidget).BrushRenderer.RestartAnimation();
			if (QueueIndex == 0)
			{
				HammerAnimWidget.BrushRenderer.RestartAnimation();
			}
			_animState = AnimState.Playing;
		}
		if (!_isBrushChanged)
		{
			(Slider.Filler as BrushWidget).Brush = (IsConstructing ? ConstructionBrush : NormalBrush);
			_animState = AnimState.Start;
			_isBrushChanged = true;
		}
		if (!IsConstructing)
		{
			UpdateColorOfSlider();
		}
	}

	protected override void OnMousePressed()
	{
		base.OnMousePressed();
		IsPOISelected = true;
		EventFired("OnSelection");
	}

	protected override void OnHoverBegin()
	{
		base.OnHoverBegin();
	}

	protected override void OnHoverEnd()
	{
		base.OnHoverEnd();
	}

	private void SetMachineTypeIcon(int machineType)
	{
		string text = "SPGeneral\\MapSiege\\";
		text = machineType switch
		{
			0 => text + "wall", 
			1 => text + "broken_wall", 
			2 => text + "ballista", 
			3 => text + "trebuchet", 
			4 => text + "ladder", 
			5 => text + "ram", 
			6 => text + "tower", 
			7 => text + "mangonel", 
			_ => text + "fallback", 
		};
		MachineTypeIconWidget.Sprite = base.Context.SpriteData.GetSprite(text);
	}

	private void UpdateColorOfSlider()
	{
		(Slider.Filler as BrushWidget).Brush.Color = Color.Lerp(_emptyColor, _fullColor, Slider.ValueFloat / Slider.MaxValueFloat);
	}
}
