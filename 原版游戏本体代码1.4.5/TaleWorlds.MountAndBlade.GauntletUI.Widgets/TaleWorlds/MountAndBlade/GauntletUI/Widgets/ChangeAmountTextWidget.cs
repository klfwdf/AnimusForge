using System.Linq;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class ChangeAmountTextWidget : TextWidget
{
	private bool _isVisualsDirty;

	private Brush _negativeBrush;

	private Brush _positiveBrush;

	private bool _useParentheses;

	private int _amount;

	private string _negativeBrushName;

	private string _positiveBrushName;

	private bool _shouldBeVisible = true;

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
				_isVisualsDirty = false;
			}
		}
	}

	[Editor(false)]
	public bool UseParentheses
	{
		get
		{
			return _useParentheses;
		}
		set
		{
			if (_useParentheses != value)
			{
				_useParentheses = value;
				OnPropertyChanged(value, "UseParentheses");
			}
		}
	}

	[Editor(false)]
	public bool ShouldBeVisible
	{
		get
		{
			return _shouldBeVisible;
		}
		set
		{
			if (_shouldBeVisible != value)
			{
				_shouldBeVisible = value;
				OnPropertyChanged(value, "ShouldBeVisible");
			}
		}
	}

	[Editor(false)]
	public string NegativeBrushName
	{
		get
		{
			return _negativeBrushName;
		}
		set
		{
			if (_negativeBrushName != value)
			{
				_negativeBrushName = value;
				OnPropertyChanged(value, "NegativeBrushName");
				_negativeBrush = base.EventManager.Context.Brushes.First((Brush b) => b.Name == value);
			}
		}
	}

	[Editor(false)]
	public string PositiveBrushName
	{
		get
		{
			return _positiveBrushName;
		}
		set
		{
			if (_positiveBrushName != value)
			{
				_positiveBrushName = value;
				OnPropertyChanged(value, "PositiveBrushName");
				_positiveBrush = base.EventManager.Context.Brushes.First((Brush b) => b.Name == value);
			}
		}
	}

	public ChangeAmountTextWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_isVisualsDirty)
		{
			return;
		}
		if (!ShouldBeVisible)
		{
			base.IsVisible = false;
		}
		else
		{
			base.IsVisible = Amount != 0;
			if (base.IsVisible)
			{
				base.Text = ((Amount > 0) ? ("+" + Amount) : Amount.ToString());
				if (UseParentheses)
				{
					base.Text = "(" + base.Text + ")";
				}
				if (Amount > 0)
				{
					base.Brush = _positiveBrush;
				}
				else if (Amount < 0)
				{
					base.Brush = _negativeBrush;
				}
			}
		}
		_isVisualsDirty = true;
	}
}
