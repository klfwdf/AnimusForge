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
				((ViewModel)this).OnPropertyChangedWithValue(value, "X");
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
				((ViewModel)this).OnPropertyChangedWithValue(value, "Y");
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
				((ViewModel)this).OnPropertyChangedWithValue<string>(value, "Text");
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
				((ViewModel)this).OnPropertyChangedWithValue(value, "BubbleWidth");
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
				((ViewModel)this).OnPropertyChangedWithValue(value, "Alpha");
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
				((ViewModel)this).OnPropertyChangedWithValue(value, "FontSize");
			}
		}
	}
}
