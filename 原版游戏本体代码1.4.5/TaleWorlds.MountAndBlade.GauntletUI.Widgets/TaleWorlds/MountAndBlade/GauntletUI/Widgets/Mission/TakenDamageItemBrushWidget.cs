using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;

public class TakenDamageItemBrushWidget : BrushWidget
{
	private bool _initialized;

	private int _damageAmount;

	private Vec2 _screenPosOfAffectorAgent;

	private bool _isBehind;

	private bool _isRanged;

	public float VerticalWidth { get; set; }

	public float VerticalHeight { get; set; }

	public float HorizontalWidth { get; set; }

	public float HorizontalHeight { get; set; }

	public float RangedOnScreenStayTime { get; set; } = 0.3f;

	public float MeleeOnScreenStayTime { get; set; } = 1f;

	[DataSourceProperty]
	public int DamageAmount
	{
		get
		{
			return _damageAmount;
		}
		set
		{
			if (_damageAmount != value)
			{
				_damageAmount = value;
				OnPropertyChanged(value, "DamageAmount");
			}
		}
	}

	[DataSourceProperty]
	public bool IsBehind
	{
		get
		{
			return _isBehind;
		}
		set
		{
			if (_isBehind != value)
			{
				_isBehind = value;
				OnPropertyChanged(value, "IsBehind");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRanged
	{
		get
		{
			return _isRanged;
		}
		set
		{
			if (_isRanged != value)
			{
				_isRanged = value;
				OnPropertyChanged(value, "IsRanged");
			}
		}
	}

	[DataSourceProperty]
	public Vec2 ScreenPosOfAffectorAgent
	{
		get
		{
			return _screenPosOfAffectorAgent;
		}
		set
		{
			if (_screenPosOfAffectorAgent != value)
			{
				_screenPosOfAffectorAgent = value;
				OnPropertyChanged(value, "ScreenPosOfAffectorAgent");
			}
		}
	}

	public TakenDamageItemBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_initialized)
		{
			this.RegisterBrushStatesOfWidget();
			_initialized = true;
			if (!IsRanged)
			{
				float value = (float)DamageAmount / 70f;
				value = MathF.Clamp(value, 0f, 1f);
				base.AlphaFactor = MathF.Lerp(0.3f, 1f, value);
			}
		}
		UpdateAlpha(dt);
	}

	private void UpdateAlpha(float dt)
	{
		if (base.AlphaFactor < 0.01f)
		{
			EventFired("OnRemove");
		}
		float num = (IsRanged ? RangedOnScreenStayTime : MeleeOnScreenStayTime);
		this.SetGlobalAlphaRecursively(MathF.Lerp(base.AlphaFactor, 0f, dt / num));
	}

	protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
	{
		if (base.AlphaFactor > 0f)
		{
			base.OnRender(twoDimensionContext, drawContext);
		}
	}
}
