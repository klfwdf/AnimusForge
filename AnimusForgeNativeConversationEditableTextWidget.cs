using System;
using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;

namespace AnimusForge;

public sealed class AnimusForgeNativeConversationEditableTextWidget : EditableTextWidget
{
	private bool _autoFocusApplied;

	private bool _focusRequested;

	private int _focusRequestId;

	public bool AutoFocus { get; set; }

	public int FocusRequestId
	{
		get => _focusRequestId;
		set
		{
			if (value != _focusRequestId)
			{
				_focusRequestId = value;
				_focusRequested = true;
			}
		}
	}

	public AnimusForgeNativeConversationEditableTextWidget(UIContext context)
		: base(context)
	{
	}

	public override void HandleInput(IReadOnlyList<int> lastKeysPressed)
	{
		if (IsDisabled)
		{
			return;
		}
		if (IsPasteShortcut())
		{
			ReplaceSelectionWithSanitizedClipboardText();
			return;
		}

		base.HandleInput(FilterUnsafeTextKeys(lastKeysPressed));
		SanitizeCurrentText();
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (EventManager == null || !IsEnabled || !IsRecursivelyVisible())
		{
			return;
		}
		if (AutoFocus && !_autoFocusApplied)
		{
			FocusSelf();
			_autoFocusApplied = true;
			_focusRequested = false;
		}
		else if (_focusRequested)
		{
			FocusSelf();
			_focusRequested = false;
			_autoFocusApplied = true;
		}
	}

	private void FocusSelf()
	{
		if (EventManager != null && EventManager.FocusedWidget != this)
		{
			EventManager.FocusedWidget = this;
		}
	}

	private void ReplaceSelectionWithSanitizedClipboardText()
	{
		DeleteText(_editableText.SelectedTextBegin, _editableText.SelectedTextEnd);
		string text = AnimusForgeTextInputSanitizer.SanitizeSingleLine(Input.GetClipboardText() ?? string.Empty, AnimusForgeTextInputSanitizer.MaxNativeConversationChars);
		if (string.IsNullOrEmpty(text))
		{
			SanitizeCurrentText();
			return;
		}

		int remaining = Math.Max(0, AnimusForgeTextInputSanitizer.MaxNativeConversationChars - (RealText ?? string.Empty).Length);
		if (remaining == 0)
		{
			SanitizeCurrentText();
			return;
		}
		if (text.Length > remaining)
		{
			text = text.Substring(0, remaining);
		}
		AppendText(text);
		SanitizeCurrentText();
	}

	private void SanitizeCurrentText()
	{
		string current = RealText ?? string.Empty;
		string sanitized = AnimusForgeTextInputSanitizer.SanitizeSingleLine(current, AnimusForgeTextInputSanitizer.MaxNativeConversationChars);
		if (string.Equals(current, sanitized, StringComparison.Ordinal))
		{
			return;
		}

		int cursor = Math.Min(_editableText.CursorPosition, sanitized.Length);
		RealText = sanitized;
		_editableText.SetCursor(cursor);
		ResetSelected();
	}

	private static IReadOnlyList<int> FilterUnsafeTextKeys(IReadOnlyList<int> lastKeysPressed)
	{
		if (lastKeysPressed == null || lastKeysPressed.Count == 0)
		{
			return Array.Empty<int>();
		}

		List<int> filtered = null;
		for (int i = 0; i < lastKeysPressed.Count; i++)
		{
			int key = lastKeysPressed[i];
			bool safe = !IsTextInputKey(key) || AnimusForgeTextInputSanitizer.IsSafeEditableCodePoint(key, allowNewLines: false);
			if (safe)
			{
				filtered?.Add(key);
			}
			else
			{
				if (filtered == null)
				{
					filtered = new List<int>(lastKeysPressed.Count);
					for (int j = 0; j < i; j++)
					{
						filtered.Add(lastKeysPressed[j]);
					}
				}
			}
		}

		return filtered ?? lastKeysPressed;
	}

	private static bool IsTextInputKey(int key)
	{
		return key >= 32 && (key < 127 || key >= 160);
	}

	private static bool IsPasteShortcut()
	{
		return IsCtrlDown() && !Input.IsKeyDown(InputKey.RightAlt) && Input.IsKeyPressed(InputKey.V);
	}

	private static bool IsCtrlDown()
	{
		return Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl);
	}
}
