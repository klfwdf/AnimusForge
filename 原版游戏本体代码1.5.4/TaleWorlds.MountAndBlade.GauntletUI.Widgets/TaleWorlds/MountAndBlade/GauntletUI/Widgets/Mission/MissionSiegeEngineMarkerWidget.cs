using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;

public class MissionSiegeEngineMarkerWidget : Widget
{
	private Color _fullColor = new Color(0.2784314f, 84f / 85f, 0.44313726f);

	private Color _emptyColor = new Color(84f / 85f, 0.2784314f, 0.2784314f);

	private bool _isBrushChanged;

	private bool _isEnemy;

	private bool _isActive;

	private Widget _machineTypeIconWidget;

	private string _engineType;

	public SliderWidget Slider { get; set; }

	public BrushWidget MachineIconParent { get; set; }

	public Brush EnemyBrush { get; set; }

	public Brush AllyBrush { get; set; }

	public Vec2 ScreenPosition { get; set; }

	public bool IsEnemy
	{
		get
		{
			return _isEnemy;
		}
		set
		{
			if (_isEnemy != value)
			{
				_isEnemy = value;
			}
		}
	}

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
			}
		}
	}

	public string EngineType
	{
		get
		{
			return _engineType;
		}
		set
		{
			if (_engineType != value)
			{
				_engineType = value;
				SetMachineTypeIcon(value);
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

	public MissionSiegeEngineMarkerWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		base.ScaledPositionXOffset = ScreenPosition.x - base.Size.X / 2f;
		base.ScaledPositionYOffset = ScreenPosition.y;
		float valueTo = (IsActive ? 0.65f : 0f);
		float alphaFactor = MathF.Lerp(base.AlphaFactor, valueTo, dt * 10f);
		this.SetGlobalAlphaRecursively(alphaFactor);
		if (!_isBrushChanged)
		{
			MachineIconParent.Brush = (IsEnemy ? EnemyBrush : AllyBrush);
			_isBrushChanged = true;
		}
		UpdateColorOfSlider();
	}

	private void SetMachineTypeIcon(string machineType)
	{
		string name = "SPGeneral\\MapSiege\\" + machineType;
		MachineTypeIconWidget.Sprite = base.Context.SpriteData.GetSprite(name);
	}

	private void UpdateColorOfSlider()
	{
		(Slider.Filler as BrushWidget).Brush.Color = Color.Lerp(_emptyColor, _fullColor, Slider.ValueFloat / Slider.MaxValueFloat);
	}
}
