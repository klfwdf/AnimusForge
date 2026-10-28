using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.CustomBattle;

public class SelectionGroup : ViewModel
{
	protected List<string> _textList = new List<string>();

	private int _index;

	private string _name;

	private string _text;

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

	public List<string> TextList
	{
		get
		{
			return _textList;
		}
		set
		{
			if (value != _textList)
			{
				_textList = value;
				Text = ((_textList.Count > 0) ? _textList[_index] : "");
			}
		}
	}

	public int Index
	{
		get
		{
			return _index;
		}
		private set
		{
			value = _index;
		}
	}

	public SelectionGroup(string name, List<string> textList = null)
	{
		_name = name;
		if (textList != null)
		{
			_textList = textList;
		}
		Text = ((_textList.Count > 0) ? _textList[0] : "");
	}

	protected virtual void ClickSelectionLeft()
	{
		_index--;
		if (_index < 0)
		{
			_index = _textList.Count - 1;
		}
		Text = ((_textList.Count > 0) ? _textList[_index] : "");
	}

	protected virtual void ClickSelectionRight()
	{
		_index++;
		_index %= _textList.Count;
		Text = ((_textList.Count > 0) ? _textList[_index] : "");
	}
}
