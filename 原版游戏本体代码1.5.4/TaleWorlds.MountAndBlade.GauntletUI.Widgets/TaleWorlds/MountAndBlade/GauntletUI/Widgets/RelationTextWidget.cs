using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class RelationTextWidget : TextWidget
{
	private bool _isVisualsDirty = true;

	private int _amount;

	private Color _zeroColor;

	private Color _positiveColor;

	private Color _negativeColor;

	[Editor(false)]
	public int Amount
	{
		get
		{
			return _amount;
		}
		set
		{
			if (_amount != value)
			{
				_amount = value;
				OnPropertyChanged(value, "Amount");
				_isVisualsDirty = true;
			}
		}
	}

	[Editor(false)]
	public Color ZeroColor
	{
		get
		{
			return _zeroColor;
		}
		set
		{
			if (value != _zeroColor)
			{
				_zeroColor = value;
				OnPropertyChanged(value, "ZeroColor");
			}
		}
	}

	[Editor(false)]
	public Color PositiveColor
	{
		get
		{
			return _positiveColor;
		}
		set
		{
			if (value != _positiveColor)
			{
				_positiveColor = value;
				OnPropertyChanged(value, "PositiveColor");
			}
		}
	}

	[Editor(false)]
	public Color NegativeColor
	{
		get
		{
			return _negativeColor;
		}
		set
		{
			if (value != _negativeColor)
			{
				_negativeColor = value;
				OnPropertyChanged(value, "NegativeColor");
			}
		}
	}

	public RelationTextWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_isVisualsDirty)
		{
			base.Text = ((Amount > 0) ? ("+" + Amount) : Amount.ToString());
			if (Amount > 0)
			{
				base.Brush.FontColor = PositiveColor;
			}
			else if (Amount < 0)
			{
				base.Brush.FontColor = NegativeColor;
			}
			else
			{
				base.Brush.FontColor = ZeroColor;
			}
			_isVisualsDirty = false;
		}
	}
}
