using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TaleWorlds.InputSystem;

namespace TaleWorlds.GauntletUI.BaseTypes;

public class IntegerInputTextWidget : EditableTextWidget
{
	private int _intText = -1;

	private int _maxInt = int.MaxValue;

	private int _minInt = int.MinValue;

	public bool EnableClamp { get; set; }

	public bool UpdateValueOnDone { get; set; }

	[Editor(false)]
	public int IntText
	{
		get
		{
			return _intText;
		}
		set
		{
			if (_intText != value)
			{
				_intText = value;
				OnPropertyChanged(value, "IntText");
				base.RealText = value.ToString();
				base.Text = value.ToString();
			}
		}
	}

	[Editor(false)]
	public int MaxInt
	{
		get
		{
			return _maxInt;
		}
		set
		{
			if (_maxInt != value)
			{
				_maxInt = value;
			}
		}
	}

	[Editor(false)]
	public int MinInt
	{
		get
		{
			return _minInt;
		}
		set
		{
			if (_minInt != value)
			{
				_minInt = value;
			}
		}
	}

	public IntegerInputTextWidget(UIContext context)
		: base(context)
	{
	}

	public override void HandleInput(IReadOnlyList<int> lastKeysPressed)
	{
		int count = lastKeysPressed.Count;
		for (int i = 0; i < count; i++)
		{
			int num = lastKeysPressed[i];
			if (char.IsDigit(Convert.ToChar(num)))
			{
				if (num != 60 && num != 62)
				{
					HandleInput(num);
				}
				_cursorDirection = CursorMovementDirection.None;
				_isSelection = false;
			}
		}
		int tickCount = Environment.TickCount;
		bool flag = false;
		bool flag2 = false;
		if (Input.IsKeyPressed(InputKey.Left))
		{
			_cursorDirection = CursorMovementDirection.Left;
			flag = true;
		}
		else if (Input.IsKeyPressed(InputKey.Right))
		{
			_cursorDirection = CursorMovementDirection.Right;
			flag = true;
		}
		else if ((_cursorDirection == CursorMovementDirection.Left && !Input.IsKeyDown(InputKey.Left)) || (_cursorDirection == CursorMovementDirection.Right && !Input.IsKeyDown(InputKey.Right)))
		{
			_cursorDirection = CursorMovementDirection.None;
			if (!Input.IsKeyDown(InputKey.LeftShift))
			{
				_isSelection = false;
			}
		}
		else if (Input.IsKeyReleased(InputKey.LeftShift))
		{
			_isSelection = false;
		}
		else if (Input.IsKeyDown(InputKey.Home))
		{
			_cursorDirection = CursorMovementDirection.Left;
			flag2 = true;
		}
		else if (Input.IsKeyDown(InputKey.End))
		{
			_cursorDirection = CursorMovementDirection.Right;
			flag2 = true;
		}
		if (flag || flag2)
		{
			_nextRepeatTime = tickCount + 500;
			if (Input.IsKeyDown(InputKey.LeftShift))
			{
				if (!_editableText.IsAnySelected())
				{
					_editableText.BeginSelection();
				}
				_isSelection = true;
			}
		}
		if (_cursorDirection != CursorMovementDirection.None && (flag || flag2 || tickCount >= _nextRepeatTime))
		{
			if (flag)
			{
				int direction = (int)_cursorDirection;
				if (Input.IsKeyDown(InputKey.LeftControl))
				{
					direction = FindNextWordPosition(direction) - _editableText.CursorPosition;
				}
				MoveCursor(direction, _isSelection);
				if (tickCount >= _nextRepeatTime)
				{
					_nextRepeatTime = tickCount + 30;
				}
			}
			else if (flag2)
			{
				int direction2 = ((_cursorDirection == CursorMovementDirection.Left) ? (-_editableText.CursorPosition) : (_editableText.VisibleText.Length - _editableText.CursorPosition));
				MoveCursor(direction2, _isSelection);
				if (tickCount >= _nextRepeatTime)
				{
					_nextRepeatTime = tickCount + 30;
				}
			}
		}
		bool flag3 = false;
		if (Input.IsKeyPressed(InputKey.BackSpace))
		{
			flag3 = true;
			_keyboardAction = KeyboardAction.BackSpace;
			_nextRepeatTime = tickCount + 500;
		}
		else if (Input.IsKeyPressed(InputKey.Delete))
		{
			flag3 = true;
			_keyboardAction = KeyboardAction.Delete;
			_nextRepeatTime = tickCount + 500;
		}
		if ((_keyboardAction == KeyboardAction.BackSpace && !Input.IsKeyDown(InputKey.BackSpace)) || (_keyboardAction == KeyboardAction.Delete && !Input.IsKeyDown(InputKey.Delete)))
		{
			_keyboardAction = KeyboardAction.None;
		}
		if (Input.IsKeyReleased(InputKey.Enter) || Input.IsKeyReleased(InputKey.NumpadEnter))
		{
			if (int.TryParse(base.RealText, out var result))
			{
				ForceSetInteger(result);
			}
			EventFired("TextEntered");
		}
		else if (_keyboardAction == KeyboardAction.BackSpace || _keyboardAction == KeyboardAction.Delete)
		{
			if (!flag3 && tickCount < _nextRepeatTime)
			{
				return;
			}
			if (_editableText.IsAnySelected())
			{
				DeleteText(_editableText.SelectedTextBegin, _editableText.SelectedTextEnd);
			}
			else if (Input.IsKeyDown(InputKey.LeftControl))
			{
				if (_keyboardAction == KeyboardAction.BackSpace)
				{
					DeleteText(FindNextWordPosition(-1), _editableText.CursorPosition);
				}
				else
				{
					DeleteText(_editableText.CursorPosition, FindNextWordPosition(1));
				}
			}
			else
			{
				DeleteChar(_keyboardAction == KeyboardAction.Delete);
			}
			TrySetStringAsInteger(base.RealText);
			if (tickCount >= _nextRepeatTime)
			{
				_nextRepeatTime = tickCount + 30;
			}
		}
		else
		{
			if (!Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightAlt))
			{
				return;
			}
			if (Input.IsKeyPressed(InputKey.A))
			{
				_editableText.SelectAll();
			}
			else if (Input.IsKeyPressed(InputKey.C))
			{
				CopyText(_editableText.SelectedTextBegin, _editableText.SelectedTextEnd);
			}
			else if (Input.IsKeyPressed(InputKey.X))
			{
				CopyText(_editableText.SelectedTextBegin, _editableText.SelectedTextEnd);
				DeleteText(_editableText.SelectedTextBegin, _editableText.SelectedTextEnd);
				TrySetStringAsInteger(base.RealText);
			}
			else if (Input.IsKeyPressed(InputKey.V))
			{
				DeleteText(_editableText.SelectedTextBegin, _editableText.SelectedTextEnd);
				string source = Regex.Replace(Input.GetClipboardText(), "[<>]+", " ");
				source = new string(source.Where((char c) => char.IsDigit(c)).ToArray());
				AppendText(source);
				TrySetStringAsInteger(base.RealText);
			}
		}
	}

	private void HandleInput(int lastPressedKey)
	{
		string text = null;
		bool flag = false;
		string realText = base.RealText;
		if (_editableText.SelectedTextBegin != _editableText.SelectedTextEnd)
		{
			if (_editableText.SelectedTextEnd > base.RealText.Length)
			{
				text = Convert.ToChar(lastPressedKey).ToString();
				flag = true;
			}
			else
			{
				realText = base.RealText.Substring(0, _editableText.SelectedTextBegin) + base.RealText.Substring(_editableText.SelectedTextEnd, base.RealText.Length - _editableText.SelectedTextEnd);
				if (_editableText.SelectedTextEnd - _editableText.SelectedTextBegin >= base.RealText.Length)
				{
					_editableText.SetCursorPosition(0, visible: true);
					_editableText.ResetSelected();
					flag = true;
				}
				else
				{
					_editableText.SetCursorPosition(_editableText.SelectedTextBegin, visible: true);
				}
				int cursorPosition = _editableText.CursorPosition;
				char c = Convert.ToChar(lastPressedKey);
				text = realText.Substring(0, cursorPosition) + c + realText.Substring(cursorPosition, realText.Length - cursorPosition);
			}
			_editableText.ResetSelected();
		}
		else if (base.MaxLength > -1 && base.Text.Length >= base.MaxLength)
		{
			text = base.RealText;
		}
		else
		{
			if (_editableText.CursorPosition == base.RealText.Length)
			{
				flag = true;
			}
			int cursorPosition2 = _editableText.CursorPosition;
			char c2 = Convert.ToChar(lastPressedKey);
			text = realText.Substring(0, cursorPosition2) + c2 + realText.Substring(cursorPosition2, realText.Length - cursorPosition2);
			if (!flag)
			{
				_editableText.SetCursor(cursorPosition2 + 1);
			}
		}
		TrySetStringAsInteger(text);
		if (flag)
		{
			_editableText.SetCursorPosition(base.RealText.Length, visible: true);
		}
	}

	private void SetInteger(int newInteger)
	{
		if (UpdateValueOnDone)
		{
			base.RealText = newInteger.ToString();
			base.Text = newInteger.ToString();
		}
		else
		{
			ForceSetInteger(newInteger);
		}
	}

	private void ForceSetInteger(int newInteger)
	{
		if (EnableClamp && (newInteger > MaxInt || newInteger < MinInt))
		{
			newInteger = ((newInteger > MaxInt) ? MaxInt : MinInt);
			ResetSelected();
		}
		IntText = newInteger;
		if (IntText.ToString() != base.RealText)
		{
			base.RealText = IntText.ToString();
			base.Text = IntText.ToString();
		}
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (!base.IsFocused && int.TryParse(base.RealText, out var result))
		{
			ForceSetInteger(result);
		}
	}

	private bool TrySetStringAsInteger(string str)
	{
		if (!int.TryParse(str, out var result))
		{
			if (!string.IsNullOrWhiteSpace(str))
			{
				return false;
			}
			result = 0;
		}
		SetInteger(result);
		if (_editableText.SelectedTextEnd - _editableText.SelectedTextBegin >= base.RealText.Length)
		{
			_editableText.SetCursorPosition(0, visible: true);
			_editableText.ResetSelected();
		}
		else if (_editableText.SelectedTextBegin != 0 || _editableText.SelectedTextEnd != 0)
		{
			_editableText.SetCursorPosition(_editableText.SelectedTextBegin, visible: true);
		}
		if (_editableText.CursorPosition > base.RealText.Length)
		{
			_editableText.SetCursorPosition(base.RealText.Length, visible: true);
		}
		return true;
	}

	public override void SetAllText(string text)
	{
		DeleteText(0, base.RealText.Length);
		string source = Regex.Replace(text, "[<>]+", " ");
		source = new string(source.Where((char c) => char.IsDigit(c)).ToArray());
		TrySetStringAsInteger(source);
	}
}
