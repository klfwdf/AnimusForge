using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Options;

public class OptionsBrightnessImageSliderWidget : SliderWidget
{
	private bool _isInitialized;

	public bool IsMax { get; set; }

	public Widget ImageWidget { get; set; }

	public OptionsBrightnessImageSliderWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_isInitialized)
		{
			float num = 0f;
			num = ((!IsMax) ? ((float)(base.ValueInt + 1) * 0.003f) : ((float)(base.ValueInt - 1) * 0.003f + 1f));
			SetColorOfImage(MBMath.ClampFloat(num, 0f, 1f));
			_isInitialized = true;
		}
	}

	protected override void OnValueFloatChanged(float value)
	{
		base.OnValueFloatChanged(value);
		float num = 0f;
		num = ((!IsMax) ? ((value + 1f) * 0.003f) : ((value - 1f) * 0.003f + 1f));
		SetColorOfImage(MBMath.ClampFloat(num, 0f, 1f));
	}

	private void SetColorOfImage(float value)
	{
		ImageWidget.Color = new Color(value, value, value);
	}
}
