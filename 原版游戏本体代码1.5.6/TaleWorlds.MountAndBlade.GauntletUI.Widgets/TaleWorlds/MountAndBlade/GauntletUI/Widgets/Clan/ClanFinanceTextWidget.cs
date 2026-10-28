using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Clan;

public class ClanFinanceTextWidget : TextWidget
{
	private TextWidget _negativeMarkWidget;

	[Editor(false)]
	public TextWidget NegativeMarkWidget
	{
		get
		{
			return _negativeMarkWidget;
		}
		set
		{
			if (_negativeMarkWidget != value)
			{
				_negativeMarkWidget = value;
				OnPropertyChanged(value, "NegativeMarkWidget");
			}
		}
	}

	public ClanFinanceTextWidget(UIContext context)
		: base(context)
	{
		base.intPropertyChanged += IntText_PropertyChanged;
	}

	private void IntText_PropertyChanged(PropertyOwnerObject widget, string propertyName, int propertyValue)
	{
		if (NegativeMarkWidget != null && propertyName == "IntText")
		{
			NegativeMarkWidget.IsVisible = propertyValue < 0;
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (base.Text != null && base.Text != string.Empty)
		{
			base.Text = MathF.Abs(base.IntText).ToString();
		}
	}
}
