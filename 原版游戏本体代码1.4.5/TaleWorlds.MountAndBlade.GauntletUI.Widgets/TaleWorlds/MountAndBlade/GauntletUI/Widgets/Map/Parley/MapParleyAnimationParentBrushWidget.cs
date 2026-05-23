using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.Parley;

public class MapParleyAnimationParentBrushWidget : BrushWidget
{
	private bool _firstFrame = true;

	private const float _fadeInOutDuration = 0.1f;

	private float _animationDelta;

	private float _targetYOffset;

	private float _minYOffset;

	private const float _fadeInOutYMovement = 50f;

	private float _animationDuration;

	[Editor(false)]
	public float AnimationDuration
	{
		get
		{
			return _animationDuration;
		}
		set
		{
			if (_animationDuration != value)
			{
				_animationDuration = value;
				OnPropertyChanged(value, "AnimationDuration");
			}
		}
	}

	public MapParleyAnimationParentBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (_firstFrame)
		{
			_firstFrame = false;
			_targetYOffset = base.PositionYOffset;
			_minYOffset = _targetYOffset - 50f;
		}
		_animationDelta += dt;
		if (_animationDelta < 0.1f)
		{
			float amount = _animationDelta / 0.1f;
			base.PositionYOffset = MathF.Lerp(_minYOffset, _targetYOffset, amount);
			this.SetGlobalAlphaRecursively(MathF.Lerp(0f, 1f, amount));
		}
		else if (_animationDelta < AnimationDuration - 0.1f)
		{
			base.PositionYOffset = _targetYOffset;
			this.SetGlobalAlphaRecursively(1f);
		}
		else if (_animationDelta < AnimationDuration)
		{
			float amount2 = (_animationDelta - (AnimationDuration - 0.1f)) / 0.1f;
			base.PositionYOffset = MathF.Lerp(_targetYOffset, _minYOffset, amount2);
			this.SetGlobalAlphaRecursively(MathF.Lerp(1f, 0f, amount2));
		}
	}
}
