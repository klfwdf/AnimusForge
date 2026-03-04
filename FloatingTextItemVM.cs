using TaleWorlds.Library;

namespace AnimusForge;

public class FloatingTextItemVM : ViewModel
{
	private float _x;

	private float _y;

	private float _bubbleWidth;

	private string _text;

	private float _alpha;

	private int _fontSize = 14;

	[DataSourceProperty]
	public float X
	{
		get
		{
			return _x;
		}
		set
		{
			if (value != _x)
			{
				_x = value;
				OnPropertyChangedWithValue(value, "X");
			}
		}
	}

	[DataSourceProperty]
	public float Y
	{
		get
		{
			return _y;
		}
		set
		{
			if (value != _y)
			{
				_y = value;
				OnPropertyChangedWithValue(value, "Y");
			}
		}
	}

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

	[DataSourceProperty]
	public float BubbleWidth
	{
		get
		{
			return _bubbleWidth;
		}
		set
		{
			if (value != _bubbleWidth)
			{
				_bubbleWidth = value;
				OnPropertyChangedWithValue(value, "BubbleWidth");
			}
		}
	}

	[DataSourceProperty]
	public float Alpha
	{
		get
		{
			return _alpha;
		}
		set
		{
			if (value != _alpha)
			{
				_alpha = value;
				OnPropertyChangedWithValue(value, "Alpha");
			}
		}
	}

	[DataSourceProperty]
	public int FontSize
	{
		get
		{
			return _fontSize;
		}
		set
		{
			if (value != _fontSize)
			{
				_fontSize = value;
				OnPropertyChangedWithValue(value, "FontSize");
			}
		}
	}
}
