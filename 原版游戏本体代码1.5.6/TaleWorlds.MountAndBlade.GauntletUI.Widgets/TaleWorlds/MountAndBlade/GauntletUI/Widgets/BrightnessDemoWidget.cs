using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class BrightnessDemoWidget : TextureWidget
{
	public enum DemoTypes
	{
		None = -1,
		BrightnessWide,
		ExposureTexture1,
		ExposureTexture2,
		ExposureTexture3,
		ExposureTexture4,
		ExposureTexture5,
		ExposureTexture6
	}

	private DemoTypes _demoType = DemoTypes.None;

	[Editor(false)]
	public DemoTypes DemoType
	{
		get
		{
			return _demoType;
		}
		set
		{
			if (_demoType != value)
			{
				_demoType = value;
				OnPropertyChanged(Enum.GetName(typeof(DemoTypes), value), "DemoType");
				SetTextureProviderProperty("DemoType", (int)value);
			}
		}
	}

	public BrightnessDemoWidget(UIContext context)
		: base(context)
	{
		base.TextureProviderName = "BrightnessDemoTextureProvider";
	}
}
