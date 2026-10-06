using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby;

public class MultiplayerLobbyMaskedIntTextWidget : TextWidget
{
	private int _intValue;

	private int _maskedIntValue;

	private string _maskText;

	[Editor(false)]
	public int IntValue
	{
		get
		{
			return _intValue;
		}
		set
		{
			if (_intValue != value)
			{
				_intValue = value;
				OnPropertyChanged(value, "IntValue");
				IntValueUpdated();
			}
		}
	}

	[Editor(false)]
	public int MaskedIntValue
	{
		get
		{
			return _maskedIntValue;
		}
		set
		{
			if (_maskedIntValue != value)
			{
				_maskedIntValue = value;
				OnPropertyChanged(value, "MaskedIntValue");
			}
		}
	}

	[Editor(false)]
	public string MaskText
	{
		get
		{
			return _maskText;
		}
		set
		{
			if (_maskText != value)
			{
				_maskText = value;
				OnPropertyChanged(value, "MaskText");
			}
		}
	}

	public MultiplayerLobbyMaskedIntTextWidget(UIContext context)
		: base(context)
	{
	}

	private void IntValueUpdated()
	{
		if (IntValue == MaskedIntValue)
		{
			base.Text = MaskText;
		}
		else
		{
			base.IntText = IntValue;
		}
	}
}
