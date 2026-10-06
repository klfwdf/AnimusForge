using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission;

public class MissionLeaveBarSliderWidget : SliderWidget
{
	private bool _initialized;

	private float CurrentAlpha => base.ReadOnlyBrush.GlobalAlphaFactor;

	public float FadeInMultiplier { get; set; } = 1f;

	public float FadeOutMultiplier { get; set; } = 1f;

	public MissionLeaveBarSliderWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_initialized)
		{
			this.SetGlobalAlphaRecursively(0f);
			_initialized = true;
		}
		float num = ((base.ValueFloat > 0f) ? FadeInMultiplier : FadeOutMultiplier);
		float end = ((base.ValueFloat > 0f) ? 1 : 0);
		float alphaFactor = Mathf.Clamp(Mathf.Lerp(CurrentAlpha, end, num * 0.2f), 0f, 1f);
		this.SetGlobalAlphaRecursively(alphaFactor);
	}
}
