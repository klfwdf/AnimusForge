using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TaleWorlds.InputSystem;

namespace TaleWorlds.GauntletUI.BaseTypes;

public class FloatInputTextWidget : EditableTextWidget
{
	private float _floatText;

	private float _maxFloat = float.MaxValue;

	private float _minFloat = float.MinValue;

	public bool EnableClamp { get; set; }

	public bool UpdateValueOnDone { get; set; }

	[Editor(false)]
	public float FloatText
	{
		get
		{
			return _floatText;
		}
		set
		{
			if (_floatText != value)
			{
				_floatText = value;
				OnPropertyChanged(value, "FloatText");
				base.RealText = value.ToString();
				base.Text = value.ToString();
			}
		}
	}

	[Editor(false)]
	public float MaxFloat
	{
		get
		{
			return _maxFloat;
		}
		set
		{
			if (_maxFloat != value)
			{
				_maxFloat = value;
			}
		}
	}

	[Editor(false)]
	public float MinFloat
	{
		get
		{
			return _minFloat;
		}
		set
		{
			if (_minFloat != value)
			{
				_minFloat = value;
			}
		}
	}

	public FloatInputTextWidget(UIContext context)
		: base(context)
	{
		base.PropertyChanged += IntegerInputTextWidget_PropertyChanged;
	}

	private void IntegerInputTextWidget_PropertyChanged(PropertyOwnerObject arg1, string arg2, object arg3)
	{
		if (arg2 == "RealText" && (string)arg3 != FloatText.ToString() && float.TryParse((string)arg3, out var result))
		{
			FloatText = result;
		}
	}

	public override void HandleInput(IReadOnlyList<int> lastKeysPressed)
	{
		int count = lastKeysPressed.Count;
		for (int i = 0; i < count; i++)
		{
			int num = lastKeysPressed[i];
			char c = Convert.ToChar(num);
			if (char.IsDigit(c) || (c == '.' && GetNumberOfSeperatorsInText(base.RealText) == 0))
			{
				if (num != 60 && num != 62 && float.TryParse(GetAppendResult(num), out var _))
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
			if (float.TryParse(base.RealText, out var result2))
			{
				ForceSetFloat(result2);
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
			TrySetStringAsFloat(base.RealText);
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
				TrySetStringAsFloat(base.RealText);
			}
			else if (Input.IsKeyPressed(InputKey.V))
			{
				DeleteText(_editableText.SelectedTextBegin, _editableText.SelectedTextEnd);
				string source = Regex.Replace(Input.GetClipboardText(), "[<>]+", " ");
				source = new string(source.Where((char c2) => char.IsDigit(c2)).ToArray());
				AppendText(source);
				TrySetStringAsFloat(base.RealText);
			}
		}
	}

	private void HandleInput(int lastPressedKey)
	{
		string text = null;
		bool flag = false;
		if (base.MaxLength > -1 && base.Text.Length >= base.MaxLength)
		{
			text = base.RealText;
		}
		if (text == null)
		{
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
		}
		TrySetStringAsFloat(text);
		if (flag)
		{
			_editableText.SetCursorPosition(base.RealText.Length, visible: true);
		}
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (!base.IsFocused && float.TryParse(base.RealText, out var result))
		{
			ForceSetFloat(result);
		}
	}

	private bool TrySetStringAsFloat(string str)
	{
		if (float.TryParse(str, out var result))
		{
			SetFloat(result);
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
		return false;
	}

	private void SetFloat(float newFloat)
	{
		if (UpdateValueOnDone)
		{
			base.RealText = newFloat.ToString();
			base.Text = newFloat.ToString();
		}
		else
		{
			ForceSetFloat(newFloat);
		}
	}

	private void ForceSetFloat(float newFloat)
	{
		if (EnableClamp && (newFloat > MaxFloat || newFloat < MinFloat))
		{
			newFloat = ((newFloat > MaxFloat) ? MaxFloat : MinFloat);
			ResetSelected();
		}
		FloatText = newFloat;
		if (FloatText.ToString() != base.RealText)
		{
			base.RealText = FloatText.ToString();
			base.Text = FloatText.ToString();
		}
	}

	private int GetNumberOfSeperatorsInText(string realText)
	{
		return realText.Count((char c) => char.IsPunctuation(c));
	}

	private string GetAppendResult(int lastPressedKey)
	{
		if (base.MaxLength > -1 && base.Text.Length >= base.MaxLength)
		{
			return base.RealText;
		}
		_ = base.RealText;
		if (_editableText.SelectedTextBegin != _editableText.SelectedTextEnd)
		{
			_ = base.RealText.Substring(0, _editableText.SelectedTextBegin) + base.RealText.Substring(_editableText.SelectedTextEnd, base.RealText.Length - _editableText.SelectedTextEnd);
		}
		int cursorPosition = _editableText.CursorPosition;
		char c = Convert.ToChar(lastPressedKey);
		return base.RealText.Substring(0, cursorPosition) + c + base.RealText.Substring(cursorPosition, base.RealText.Length - cursorPosition);
	}

	public override void SetAllText(string text)
	{
		DeleteText(0, base.RealText.Length);
		string source = Regex.Replace(text, "[<>]+", " ");
		source = new string(source.Where((char c) => char.IsDigit(c)).ToArray());
		AppendText(source);
		TrySetStringAsFloat(source);
	}
}
