namespace TaleWorlds.GauntletUI.BaseTypes;

public class IntegerInputPercentageTextWidget : IntegerInputTextWidget
{
	private string _percentageText;

	[Editor(false)]
	public string PercentageText
	{
		get
		{
			return _percentageText;
		}
		set
		{
			if (_percentageText != value)
			{
				_percentageText = value;
				OnPropertyChanged(value, "PercentageText");
			}
		}
	}

	public IntegerInputPercentageTextWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (!base.IsFocused)
		{
			SetPercentageText();
		}
	}

	protected internal override void OnGainFocus()
	{
		base.OnGainFocus();
		SetIntText();
	}

	private void SetPercentageText()
	{
		base.Text = PercentageText;
	}

	private void SetIntText()
	{
		base.Text = base.IntText.ToString();
	}
}
