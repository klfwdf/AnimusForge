using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.ClassLoadout;

public class MultiplayerClassLoadoutTroopInfoBrushWidget : BrushWidget
{
	private float _defaultAlpha = 0.7f;

	[Editor(false)]
	public float DefaultAlpha
	{
		get
		{
			return _defaultAlpha;
		}
		set
		{
			if (value != _defaultAlpha)
			{
				_defaultAlpha = value;
				OnPropertyChanged(value, "DefaultAlpha");
			}
		}
	}

	public MultiplayerClassLoadoutTroopInfoBrushWidget(UIContext context)
		: base(context)
	{
		this.SetAlpha(DefaultAlpha);
	}

	protected override void OnHoverBegin()
	{
		base.OnHoverBegin();
		this.SetAlpha(1f);
	}

	protected override void OnHoverEnd()
	{
		base.OnHoverEnd();
		this.SetAlpha(DefaultAlpha);
	}

	public override void OnBrushChanged()
	{
		base.OnBrushChanged();
		this.SetAlpha(DefaultAlpha);
	}
}
