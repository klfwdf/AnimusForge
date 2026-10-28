using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer;

public class MaterialValueOffsetTextWidget : TextWidget
{
	private bool _visualDirty;

	private float _valueOffset;

	private float _saturationOffset;

	private float _hueOffset;

	public float ValueOffset
	{
		get
		{
			return _valueOffset;
		}
		set
		{
			if (_valueOffset != value)
			{
				_valueOffset = value;
				_visualDirty = true;
			}
		}
	}

	public float SaturationOffset
	{
		get
		{
			return _saturationOffset;
		}
		set
		{
			if (_saturationOffset != value)
			{
				_saturationOffset = value;
				_visualDirty = true;
			}
		}
	}

	public float HueOffset
	{
		get
		{
			return _hueOffset;
		}
		set
		{
			if (_hueOffset != value)
			{
				_hueOffset = value;
				_visualDirty = true;
			}
		}
	}

	public MaterialValueOffsetTextWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_visualDirty)
		{
			return;
		}
		base.Brush.TextValueFactor += ValueOffset;
		base.Brush.TextSaturationFactor += SaturationOffset;
		base.Brush.TextHueFactor += HueOffset;
		foreach (Style style in base.Brush.Styles)
		{
			style.TextValueFactor += ValueOffset;
			style.TextSaturationFactor += SaturationOffset;
			style.TextHueFactor += HueOffset;
		}
		_visualDirty = false;
	}
}
