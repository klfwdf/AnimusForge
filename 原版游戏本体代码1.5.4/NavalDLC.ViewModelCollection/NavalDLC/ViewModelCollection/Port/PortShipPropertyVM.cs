using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.ViewModelCollection.Port;

public class PortShipPropertyVM : ViewModel
{
	private readonly TextObject _titleText;

	private readonly TextObject _valueText;

	private string _text;

	[DataSourceProperty]
	public string Text
	{
		get
		{
			return _text;
		}
		set
		{
			if (value != _text)
			{
				_text = value;
				OnPropertyChangedWithValue(value, "Text");
			}
		}
	}

	public PortShipPropertyVM(TextObject title, TextObject value)
	{
		_titleText = title;
		_valueText = value;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Text = CreateLabeledValueText(_titleText, _valueText).ToString();
	}

	private TextObject CreateLabeledValueText(TextObject label, TextObject value)
	{
		TextObject textObject = new TextObject("{=!}<span style=\"Label\">{LABEL}</span>: {VALUE}");
		textObject.SetTextVariable("LABEL", label);
		textObject.SetTextVariable("VALUE", value);
		return textObject;
	}
}
