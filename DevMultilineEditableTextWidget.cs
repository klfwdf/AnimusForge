using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace AnimusForge;

public class DevMultilineEditableTextWidget : BrushWidget
{
	private sealed class VisualLine
	{
		public int StartIndex;

		public int EndIndex;

		public string Text = string.Empty;
	}

	private sealed class EditorSnapshot
	{
		public string Text = string.Empty;

		public int CursorIndex;

		public int SelectionAnchor = -1;
	}

	private enum RepeatAction
	{
		None,
		Backspace,
		Delete,
		Left,
		Right,
		Up,
		Down
	}

	private const int InitialRepeatDelayMs = 500;

	private const int RepeatDelayMs = 30;

	private readonly Widget _contentRoot;

	private readonly Widget _caretWidget;

	private readonly Text _measurementText;

	private readonly List<VisualLine> _visualLines = new List<VisualLine>();

	private readonly List<TextWidget> _lineWidgets = new List<TextWidget>();

	private readonly List<Widget> _selectionWidgets = new List<Widget>();

	private readonly Stack<EditorSnapshot> _undoStack = new Stack<EditorSnapshot>();

	private readonly Stack<EditorSnapshot> _redoStack = new Stack<EditorSnapshot>();

	private string _realText = string.Empty;

	private int _cursorIndex;

	private int _selectionAnchor = -1;

	private bool _layoutDirty = true;

	private bool _hasCaretFocus = true;

	private bool _mouseSelecting;

	private float _lineHeightScaled = 28f;

	private float _lineHeightUnscaled = 28f;

	private int _editorFontSize = 12;

	private RepeatAction _repeatAction;

	private int _nextRepeatTime;

	private bool _repeatActionHasUndoSnapshot;

	[Editor(false)]
	public int MaxLength { get; set; } = -1;

	[Editor(false)]
	public bool IsObfuscationEnabled { get; set; }

	[Editor(false)]
	public int EditorFontSize
	{
		get
		{
			return _editorFontSize;
		}
		set
		{
			int value2 = Math.Max(8, value);
			if (_editorFontSize != value2)
			{
				_editorFontSize = value2;
				ApplyBrushToChildren();
				MarkLayoutDirty();
			}
		}
	}

	[Editor(false)]
	public string RealText
	{
		get
		{
			return _realText;
		}
		set
		{
			string text = ApplyLengthLimit(NormalizeText(value));
			if (_realText != text)
			{
				_realText = text;
				_cursorIndex = Math.Min(_cursorIndex, _realText.Length);
				if (_selectionAnchor > _realText.Length)
				{
					_selectionAnchor = _realText.Length;
				}
				_undoStack.Clear();
				_redoStack.Clear();
				OnPropertyChanged(text, "RealText");
				MarkLayoutDirty();
			}
		}
	}

	public DevMultilineEditableTextWidget(UIContext context)
		: base(context)
	{
		IsFocusable = true;
		DoNotPassEventsToChildren = true;
		_contentRoot = new Widget(context)
		{
			Id = "EditorContentRoot",
			WidthSizePolicy = SizePolicy.StretchToParent,
			HeightSizePolicy = SizePolicy.Fixed,
			DoNotAcceptEvents = true,
			DoNotPassEventsToChildren = true
		};
		AddChild(_contentRoot);

		_caretWidget = new Widget(context)
		{
			Id = "EditorCaret",
			WidthSizePolicy = SizePolicy.Fixed,
			HeightSizePolicy = SizePolicy.Fixed,
			SuggestedWidth = 2f,
			SuggestedHeight = 24f,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Color = Color.FromUint(4294956543u),
			Sprite = context.SpriteData.GetSprite("BlankWhiteSquare_9"),
			DoNotAcceptEvents = true,
			DoNotPassEventsToChildren = true
		};
		AddChild(_caretWidget);

		Font defaultFont = context.FontFactory.DefaultFont;
		_measurementText = new Text(1, 1, defaultFont, new Func<int, Font>(context.FontFactory.GetUsableFontForCharacter));
		_measurementText.CurrentLanguage = context.FontFactory.CurrentLanguage;
		_measurementText.CanBreakWords = false;
		ApplyBrushToChildren();
	}

	public override void OnBrushChanged()
	{
		base.OnBrushChanged();
		ApplyBrushToChildren();
		MarkLayoutDirty();
	}

	public override void HandleInput(IReadOnlyList<int> lastKeysPressed)
	{
		if (IsDisabled)
		{
			return;
		}
		if (HandleClipboardAndUndoShortcuts())
		{
			return;
		}
		if (IsCtrlDown() || Input.IsKeyDown(InputKey.RightAlt))
		{
			ClearRepeatActionIfReleased();
			return;
		}
		if (HandleSinglePressKeys())
		{
			return;
		}
		if (HandleRepeatableKeys())
		{
			return;
		}
		HandleCharacterInput(lastKeysPressed);
	}

	protected override void OnMousePressed()
	{
		base.OnMousePressed();
		if (EventManager != null && EventManager.FocusedWidget != this)
		{
			EventManager.FocusedWidget = this;
		}
		_hasCaretFocus = true;
		_mouseSelecting = true;
		int cursorIndexFromMouse = GetCursorIndexFromMouse();
		if (IsShiftDown())
		{
			if (_selectionAnchor < 0)
			{
				_selectionAnchor = _cursorIndex;
			}
			_cursorIndex = cursorIndexFromMouse;
		}
		else
		{
			_selectionAnchor = cursorIndexFromMouse;
			_cursorIndex = cursorIndexFromMouse;
		}
	}

	protected override void OnMouseMove()
	{
		base.OnMouseMove();
		if (_mouseSelecting)
		{
			_cursorIndex = GetCursorIndexFromMouse();
		}
	}

	protected override void OnMouseReleased()
	{
		base.OnMouseReleased();
		if (_mouseSelecting)
		{
			_cursorIndex = GetCursorIndexFromMouse();
			if (_selectionAnchor == _cursorIndex)
			{
				ClearSelection();
			}
		}
		_mouseSelecting = false;
	}

	protected override void OnGainFocus()
	{
		base.OnGainFocus();
		_hasCaretFocus = true;
		_caretWidget.IsVisible = true;
	}

	protected override void OnLoseFocus()
	{
		base.OnLoseFocus();
		_hasCaretFocus = false;
		_mouseSelecting = false;
		_caretWidget.IsVisible = false;
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_layoutDirty)
		{
			RebuildVisualLines();
		}
		UpdateSelectionVisuals();
		UpdateCaretVisual();
	}

	private void ApplyBrushToChildren()
	{
		for (int i = 0; i < _lineWidgets.Count; i++)
		{
			_lineWidgets[i].Brush = CreateLineBrush();
		}
	}

	private Brush CreateLineBrush()
	{
		Brush brush = ReadOnlyBrush.Clone();
		brush.FontSize = EditorFontSize;
		brush.TextHorizontalAlignment = TextHorizontalAlignment.Left;
		brush.TextVerticalAlignment = TextVerticalAlignment.Top;
		return brush;
	}

	private void MarkLayoutDirty()
	{
		_layoutDirty = true;
		SetMeasureAndLayoutDirty();
	}

	private void RebuildVisualLines()
	{
		_layoutDirty = false;
		_visualLines.Clear();
		UpdateMeasurementFont();
		float availableWidth = Math.Max(32f, Size.X);
		string text = _realText ?? string.Empty;
		if (text.Length == 0)
		{
			_visualLines.Add(new VisualLine
			{
				StartIndex = 0,
				EndIndex = 0,
				Text = string.Empty
			});
		}
		else
		{
			int index = 0;
			while (index <= text.Length)
			{
				if (index == text.Length)
				{
					if (text.Length > 0 && text[text.Length - 1] == '\n')
					{
						_visualLines.Add(new VisualLine
						{
							StartIndex = text.Length,
							EndIndex = text.Length,
							Text = string.Empty
						});
					}
					break;
				}
				if (text[index] == '\n')
				{
					_visualLines.Add(new VisualLine
					{
						StartIndex = index,
						EndIndex = index,
						Text = string.Empty
					});
					index++;
					continue;
				}
				int paragraphEnd = text.IndexOf('\n', index);
				if (paragraphEnd < 0)
				{
					paragraphEnd = text.Length;
				}
				while (index < paragraphEnd)
				{
					int length = FindBestLineLength(text, index, paragraphEnd, availableWidth);
					int end = Math.Min(paragraphEnd, index + Math.Max(1, length));
					int preferredBreak = FindPreferredBreak(text, index, end, paragraphEnd);
					end = Math.Max(index + 1, preferredBreak);
					_visualLines.Add(new VisualLine
					{
						StartIndex = index,
						EndIndex = end,
						Text = text.Substring(index, end - index)
					});
					index = end;
				}
				if (index < text.Length && text[index] == '\n')
				{
					index++;
				}
			}
		}
		if (_visualLines.Count == 0)
		{
			_visualLines.Add(new VisualLine
			{
				StartIndex = 0,
				EndIndex = 0,
				Text = string.Empty
			});
		}
		SyncLineWidgets();
	}

	private void SyncLineWidgets()
	{
		while (_lineWidgets.Count < _visualLines.Count)
		{
			Widget widget = new Widget(Context)
			{
				Id = "Selection_" + _selectionWidgets.Count,
				WidthSizePolicy = SizePolicy.Fixed,
				HeightSizePolicy = SizePolicy.Fixed,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Sprite = Context.SpriteData.GetSprite("BlankWhiteSquare_9"),
				Color = Color.FromUint(1714631475u),
				DoNotAcceptEvents = true,
				DoNotPassEventsToChildren = true,
				IsVisible = false
			};
			TextWidget textWidget = new TextWidget(Context)
			{
				Id = "Line_" + _lineWidgets.Count,
				WidthSizePolicy = SizePolicy.StretchToParent,
				HeightSizePolicy = SizePolicy.Fixed,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				CanBreakWords = false,
				DoNotAcceptEvents = true,
				DoNotPassEventsToChildren = true
			};
			textWidget.Brush = CreateLineBrush();
			_selectionWidgets.Add(widget);
			_lineWidgets.Add(textWidget);
			_contentRoot.AddChild(widget);
			_contentRoot.AddChild(textWidget);
		}
		while (_lineWidgets.Count > _visualLines.Count)
		{
			int index = _lineWidgets.Count - 1;
			_contentRoot.RemoveChild(_lineWidgets[index]);
			_contentRoot.RemoveChild(_selectionWidgets[index]);
			_lineWidgets.RemoveAt(index);
			_selectionWidgets.RemoveAt(index);
		}
		float num = Math.Max(_lineHeightUnscaled, _visualLines.Count * _lineHeightUnscaled);
		_contentRoot.SuggestedHeight = num;
		for (int i = 0; i < _visualLines.Count; i++)
		{
			float num2 = i * _lineHeightUnscaled;
			TextWidget textWidget2 = _lineWidgets[i];
			textWidget2.Brush = CreateLineBrush();
			textWidget2.Text = _visualLines[i].Text;
			textWidget2.PositionXOffset = 0f;
			textWidget2.PositionYOffset = num2;
			textWidget2.SuggestedHeight = _lineHeightUnscaled;
			Widget widget2 = _selectionWidgets[i];
			widget2.PositionYOffset = num2;
			widget2.SuggestedHeight = _lineHeightUnscaled;
		}
	}

	private int FindBestLineLength(string text, int start, int paragraphEnd, float availableWidth)
	{
		int low = 1;
		int high = Math.Max(1, paragraphEnd - start);
		int best = 1;
		while (low <= high)
		{
			int mid = (low + high) / 2;
			if (MeasureWidth(text.Substring(start, mid)) <= availableWidth)
			{
				best = mid;
				low = mid + 1;
			}
			else
			{
				high = mid - 1;
			}
		}
		return best;
	}

	private int FindPreferredBreak(string text, int start, int end, int paragraphEnd)
	{
		if (end >= paragraphEnd)
		{
			return end;
		}
		for (int i = end; i > start; i--)
		{
			char c = text[i - 1];
			if (char.IsWhiteSpace(c) || c == '，' || c == '。' || c == '；' || c == '：' || c == '、' || c == ',' || c == '.' || c == ';' || c == ':')
			{
				return i;
			}
		}
		return end;
	}

	private void UpdateMeasurementFont()
	{
		_measurementText.CurrentLanguage = Context.FontFactory.CurrentLanguage;
		_measurementText.Font = GetCurrentFont();
		_measurementText.FontSize = EditorFontSize * _scaleToUse;
		_measurementText.HorizontalAlignment = TextHorizontalAlignment.Left;
		_measurementText.VerticalAlignment = TextVerticalAlignment.Top;
		_measurementText.CanBreakWords = false;
		_lineHeightScaled = Math.Max(16f, GetCurrentFont().LineHeight * (EditorFontSize / (float)Math.Max(1, GetCurrentFont().Size)) * _scaleToUse);
		_lineHeightUnscaled = _lineHeightScaled * Math.Max(0.001f, _inverseScaleToUse);
		_caretWidget.SuggestedHeight = _lineHeightUnscaled;
		_caretWidget.SuggestedWidth = Math.Max(1f, 2f * Math.Max(0.001f, _inverseScaleToUse));
	}

	private Font GetCurrentFont()
	{
		Font font = (ReadOnlyBrush.Font != null) ? ReadOnlyBrush.Font : Context.FontFactory.DefaultFont;
		return Context.FontFactory.GetMappedFontForLocalization(font.Name);
	}

	private float MeasureWidth(string text)
	{
		_measurementText.Value = string.IsNullOrEmpty(text) ? " " : text;
		return _measurementText.GetPreferredSize(fixedWidth: false, 0f, fixedHeight: false, 0f, Context.SpriteData, _scaleToUse).X;
	}

	private bool HandleClipboardAndUndoShortcuts()
	{
		if (!IsCtrlDown())
		{
			return false;
		}
		if (IsShiftDown() && Input.IsKeyPressed(InputKey.Z))
		{
			Redo();
			return true;
		}
		if (Input.IsKeyPressed(InputKey.Z))
		{
			Undo();
			return true;
		}
		if (Input.IsKeyPressed(InputKey.Y))
		{
			Redo();
			return true;
		}
		if (Input.IsKeyPressed(InputKey.A))
		{
			_selectionAnchor = 0;
			_cursorIndex = _realText.Length;
			return true;
		}
		if (Input.IsKeyPressed(InputKey.C))
		{
			string selectedText = GetSelectedText();
			if (!string.IsNullOrEmpty(selectedText))
			{
				WindowsClipboardHelper.SetText(selectedText);
			}
			return true;
		}
		if (Input.IsKeyPressed(InputKey.X))
		{
			string selectedText2 = GetSelectedText();
			if (!string.IsNullOrEmpty(selectedText2))
			{
				WindowsClipboardHelper.SetText(selectedText2);
				ReplaceSelection(string.Empty);
			}
			return true;
		}
		if (Input.IsKeyPressed(InputKey.V))
		{
			ReplaceSelection(SanitizeClipboardText(WindowsClipboardHelper.GetText()));
			return true;
		}
		return false;
	}

	private bool HandleSinglePressKeys()
	{
		bool shift = IsShiftDown();
		if (Input.IsKeyPressed(InputKey.Home))
		{
			MoveCursorToBoundary(toLineStart: true, shift);
			ClearRepeatAction();
			return true;
		}
		if (Input.IsKeyPressed(InputKey.End))
		{
			MoveCursorToBoundary(toLineStart: false, shift);
			ClearRepeatAction();
			return true;
		}
		if (Input.IsKeyPressed(InputKey.Enter) || Input.IsKeyPressed(InputKey.NumpadEnter))
		{
			ReplaceSelection("\n");
			ClearRepeatAction();
			return true;
		}
		if (Input.IsKeyPressed(InputKey.Tab))
		{
			ReplaceSelection("    ");
			ClearRepeatAction();
			return true;
		}
		return false;
	}

	private bool HandleRepeatableKeys()
	{
		bool shift = IsShiftDown();
		bool ctrl = IsCtrlDown();
		if (Input.IsKeyPressed(InputKey.BackSpace))
		{
			StartRepeatAction(RepeatAction.Backspace);
			if (PerformBackspace(ctrl, recordUndo: true))
			{
				_repeatActionHasUndoSnapshot = true;
			}
			return true;
		}
		if (Input.IsKeyPressed(InputKey.Delete))
		{
			StartRepeatAction(RepeatAction.Delete);
			if (PerformDelete(ctrl, recordUndo: true))
			{
				_repeatActionHasUndoSnapshot = true;
			}
			return true;
		}
		if (Input.IsKeyPressed(InputKey.Left))
		{
			StartRepeatAction(RepeatAction.Left);
			MoveCursorHorizontal(ctrl ? (FindWordBoundary(-1) - _cursorIndex) : -1, shift);
			return true;
		}
		if (Input.IsKeyPressed(InputKey.Right))
		{
			StartRepeatAction(RepeatAction.Right);
			MoveCursorHorizontal(ctrl ? (FindWordBoundary(1) - _cursorIndex) : 1, shift);
			return true;
		}
		if (Input.IsKeyPressed(InputKey.Up))
		{
			StartRepeatAction(RepeatAction.Up);
			MoveCursorVertical(-1, shift);
			return true;
		}
		if (Input.IsKeyPressed(InputKey.Down))
		{
			StartRepeatAction(RepeatAction.Down);
			MoveCursorVertical(1, shift);
			return true;
		}
		if (_repeatAction == RepeatAction.None)
		{
			return false;
		}
		if (!IsRepeatKeyStillDown(_repeatAction))
		{
			ClearRepeatAction();
			return false;
		}
		if (Environment.TickCount < _nextRepeatTime)
		{
			return false;
		}
		switch (_repeatAction)
		{
		case RepeatAction.Backspace:
			PerformBackspace(ctrl, recordUndo: !_repeatActionHasUndoSnapshot);
			_repeatActionHasUndoSnapshot = true;
			break;
		case RepeatAction.Delete:
			PerformDelete(ctrl, recordUndo: !_repeatActionHasUndoSnapshot);
			_repeatActionHasUndoSnapshot = true;
			break;
		case RepeatAction.Left:
			MoveCursorHorizontal(ctrl ? (FindWordBoundary(-1) - _cursorIndex) : -1, shift);
			break;
		case RepeatAction.Right:
			MoveCursorHorizontal(ctrl ? (FindWordBoundary(1) - _cursorIndex) : 1, shift);
			break;
		case RepeatAction.Up:
			MoveCursorVertical(-1, shift);
			break;
		case RepeatAction.Down:
			MoveCursorVertical(1, shift);
			break;
		}
		_nextRepeatTime = Environment.TickCount + RepeatDelayMs;
		return true;
	}

	private void HandleCharacterInput(IReadOnlyList<int> lastKeysPressed)
	{
		ClearRepeatActionIfReleased();
		if (lastKeysPressed == null || lastKeysPressed.Count == 0)
		{
			return;
		}
		for (int i = 0; i < lastKeysPressed.Count; i++)
		{
			int num = lastKeysPressed[i];
			if (num >= 32 && (num < 127 || num >= 160) && num != 60 && num != 62)
			{
				ReplaceSelection(char.ConvertFromUtf32(num));
			}
		}
	}

	private void ReplaceSelection(string replacement)
	{
		replacement = NormalizeText(replacement);
		int selectionStart = GetSelectionStart();
		int selectionEnd = GetSelectionEnd();
		int allowedExtraLength = GetAllowedExtraLength(selectionEnd - selectionStart);
		if (allowedExtraLength == 0 && replacement.Length > 0)
		{
			replacement = string.Empty;
		}
		else if (allowedExtraLength > 0 && replacement.Length > allowedExtraLength)
		{
			replacement = replacement.Substring(0, allowedExtraLength);
		}
		string text = _realText.Remove(selectionStart, selectionEnd - selectionStart).Insert(selectionStart, replacement);
		ApplyTextChange(text, selectionStart + replacement.Length, -1, recordUndo: true);
	}

	private bool PerformBackspace(bool ctrl, bool recordUndo)
	{
		if (HasSelection())
		{
			ReplaceSelection(string.Empty);
			return true;
		}
		if (_cursorIndex <= 0)
		{
			return false;
		}
		int num = ctrl ? FindWordBoundary(-1) : (_cursorIndex - 1);
		string text = _realText.Remove(num, _cursorIndex - num);
		ApplyTextChange(text, num, -1, recordUndo);
		return true;
	}

	private bool PerformDelete(bool ctrl, bool recordUndo)
	{
		if (HasSelection())
		{
			ReplaceSelection(string.Empty);
			return true;
		}
		if (_cursorIndex >= _realText.Length)
		{
			return false;
		}
		int num = ctrl ? FindWordBoundary(1) : (_cursorIndex + 1);
		string text = _realText.Remove(_cursorIndex, Math.Max(1, num - _cursorIndex));
		ApplyTextChange(text, _cursorIndex, -1, recordUndo);
		return true;
	}

	private void MoveCursorHorizontal(int delta, bool keepSelection)
	{
		if (delta == 0)
		{
			return;
		}
		SetCursorIndex(Math.Max(0, Math.Min(_realText.Length, _cursorIndex + delta)), keepSelection);
	}

	private void MoveCursorVertical(int lineDelta, bool keepSelection)
	{
		if (_layoutDirty)
		{
			RebuildVisualLines();
		}
		GetCursorLineAndColumn(out int lineIndex, out int column);
		int index = Math.Max(0, Math.Min(_visualLines.Count - 1, lineIndex + lineDelta));
		VisualLine visualLine = _visualLines[index];
		SetCursorIndex(Math.Min(visualLine.EndIndex, visualLine.StartIndex + column), keepSelection);
	}

	private void MoveCursorToBoundary(bool toLineStart, bool keepSelection)
	{
		if (_layoutDirty)
		{
			RebuildVisualLines();
		}
		GetCursorLineAndColumn(out int lineIndex, out _);
		VisualLine visualLine = _visualLines[Math.Max(0, Math.Min(_visualLines.Count - 1, lineIndex))];
		SetCursorIndex(toLineStart ? visualLine.StartIndex : visualLine.EndIndex, keepSelection);
	}

	private void SetCursorIndex(int newIndex, bool keepSelection)
	{
		newIndex = Math.Max(0, Math.Min(_realText.Length, newIndex));
		if (keepSelection)
		{
			if (_selectionAnchor < 0)
			{
				_selectionAnchor = _cursorIndex;
			}
		}
		else
		{
			ClearSelection();
		}
		_cursorIndex = newIndex;
	}

	private int GetCursorIndexFromMouse()
	{
		if (_layoutDirty)
		{
			RebuildVisualLines();
		}
		var vector = AreaRect.TransformScreenPositionToLocal(EventManager.MousePosition);
		float num = Math.Max(0f, vector.X);
		float num2 = Math.Max(0f, vector.Y);
		int index = Math.Max(0, Math.Min(_visualLines.Count - 1, (int)(num2 / Math.Max(1f, _lineHeightScaled))));
		VisualLine visualLine = _visualLines[index];
		int num3 = visualLine.StartIndex;
		float num4 = 0f;
		while (num3 < visualLine.EndIndex)
		{
			float num5 = MeasureWidth(_realText.Substring(visualLine.StartIndex, num3 - visualLine.StartIndex + 1));
			if (num5 >= num)
			{
				if (Math.Abs(num5 - num) < Math.Abs(num - num4))
				{
					num3++;
				}
				break;
			}
			num3++;
			num4 = num5;
		}
		return Math.Max(visualLine.StartIndex, Math.Min(visualLine.EndIndex, num3));
	}

	private void UpdateSelectionVisuals()
	{
		int selectionStart = GetSelectionStart();
		int selectionEnd = GetSelectionEnd();
		bool flag = HasSelection();
		float num = Math.Max(0.001f, _inverseScaleToUse);
		for (int i = 0; i < _selectionWidgets.Count; i++)
		{
			Widget widget = _selectionWidgets[i];
			if (!flag || i >= _visualLines.Count)
			{
				widget.IsVisible = false;
				continue;
			}
			VisualLine visualLine = _visualLines[i];
			int num2 = Math.Max(selectionStart, visualLine.StartIndex);
			int num3 = Math.Min(selectionEnd, visualLine.EndIndex);
			if (num3 <= num2)
			{
				widget.IsVisible = false;
				continue;
			}
			float num4 = MeasureWidth(_realText.Substring(visualLine.StartIndex, Math.Max(0, num2 - visualLine.StartIndex)));
			float num5 = MeasureWidth(_realText.Substring(visualLine.StartIndex, Math.Max(0, num3 - visualLine.StartIndex)));
			widget.PositionXOffset = num4 * num;
			widget.PositionYOffset = i * _lineHeightUnscaled;
			widget.SuggestedWidth = Math.Max(1f, (num5 - num4) * num);
			widget.SuggestedHeight = _lineHeightUnscaled;
			widget.IsVisible = true;
		}
	}

	private void UpdateCaretVisual()
	{
		if (_layoutDirty || !_hasCaretFocus)
		{
			_caretWidget.IsVisible = false;
			return;
		}
		GetCursorLineAndColumn(out int lineIndex, out _);
		VisualLine visualLine = _visualLines[Math.Max(0, Math.Min(_visualLines.Count - 1, lineIndex))];
		string text = _realText.Substring(visualLine.StartIndex, Math.Max(0, _cursorIndex - visualLine.StartIndex));
		float num = MeasureWidth(text);
		float num2 = Math.Max(0.001f, _inverseScaleToUse);
		_caretWidget.PositionXOffset = num * num2;
		_caretWidget.PositionYOffset = lineIndex * _lineHeightUnscaled;
		_caretWidget.IsVisible = true;
	}

	private void GetCursorLineAndColumn(out int lineIndex, out int column)
	{
		if (_layoutDirty)
		{
			RebuildVisualLines();
		}
		for (int i = 0; i < _visualLines.Count; i++)
		{
			VisualLine visualLine = _visualLines[i];
			if (_cursorIndex >= visualLine.StartIndex && _cursorIndex <= visualLine.EndIndex)
			{
				lineIndex = i;
				column = _cursorIndex - visualLine.StartIndex;
				return;
			}
		}
		lineIndex = Math.Max(0, _visualLines.Count - 1);
		column = (_visualLines.Count == 0) ? 0 : Math.Max(0, _cursorIndex - _visualLines[lineIndex].StartIndex);
	}

	private int FindWordBoundary(int direction)
	{
		if (direction < 0)
		{
			int num = Math.Max(0, _cursorIndex - 1);
			while (num > 0 && char.IsWhiteSpace(_realText[num]))
			{
				num--;
			}
			while (num > 0 && !char.IsWhiteSpace(_realText[num - 1]))
			{
				num--;
			}
			return num;
		}
		int num2 = _cursorIndex;
		while (num2 < _realText.Length && !char.IsWhiteSpace(_realText[num2]))
		{
			num2++;
		}
		while (num2 < _realText.Length && char.IsWhiteSpace(_realText[num2]))
		{
			num2++;
		}
		return num2;
	}

	private void ApplyTextChange(string newText, int newCursorIndex, int newSelectionAnchor, bool recordUndo)
	{
		newText = ApplyLengthLimit(newText);
		if (_realText == newText && _cursorIndex == newCursorIndex && _selectionAnchor == newSelectionAnchor)
		{
			return;
		}
		if (recordUndo)
		{
			PushUndoSnapshot();
			_redoStack.Clear();
		}
		_realText = newText;
		_cursorIndex = Math.Max(0, Math.Min(newText.Length, newCursorIndex));
		_selectionAnchor = ((newSelectionAnchor >= 0) ? Math.Max(0, Math.Min(newText.Length, newSelectionAnchor)) : (-1));
		OnPropertyChanged(_realText, "RealText");
		MarkLayoutDirty();
	}

	private void PushUndoSnapshot()
	{
		if (_undoStack.Count > 0)
		{
			EditorSnapshot editorSnapshot = _undoStack.Peek();
			if (editorSnapshot.Text == _realText && editorSnapshot.CursorIndex == _cursorIndex && editorSnapshot.SelectionAnchor == _selectionAnchor)
			{
				return;
			}
		}
		_undoStack.Push(new EditorSnapshot
		{
			Text = _realText,
			CursorIndex = _cursorIndex,
			SelectionAnchor = _selectionAnchor
		});
	}

	private void Undo()
	{
		if (_undoStack.Count == 0)
		{
			return;
		}
		_redoStack.Push(new EditorSnapshot
		{
			Text = _realText,
			CursorIndex = _cursorIndex,
			SelectionAnchor = _selectionAnchor
		});
		EditorSnapshot editorSnapshot = _undoStack.Pop();
		_realText = editorSnapshot.Text ?? string.Empty;
		_cursorIndex = Math.Max(0, Math.Min(_realText.Length, editorSnapshot.CursorIndex));
		_selectionAnchor = ((editorSnapshot.SelectionAnchor >= 0) ? Math.Max(0, Math.Min(_realText.Length, editorSnapshot.SelectionAnchor)) : (-1));
		OnPropertyChanged(_realText, "RealText");
		MarkLayoutDirty();
	}

	private void Redo()
	{
		if (_redoStack.Count == 0)
		{
			return;
		}
		_undoStack.Push(new EditorSnapshot
		{
			Text = _realText,
			CursorIndex = _cursorIndex,
			SelectionAnchor = _selectionAnchor
		});
		EditorSnapshot editorSnapshot = _redoStack.Pop();
		_realText = editorSnapshot.Text ?? string.Empty;
		_cursorIndex = Math.Max(0, Math.Min(_realText.Length, editorSnapshot.CursorIndex));
		_selectionAnchor = ((editorSnapshot.SelectionAnchor >= 0) ? Math.Max(0, Math.Min(_realText.Length, editorSnapshot.SelectionAnchor)) : (-1));
		OnPropertyChanged(_realText, "RealText");
		MarkLayoutDirty();
	}

	private bool HasSelection()
	{
		return _selectionAnchor >= 0 && _selectionAnchor != _cursorIndex;
	}

	private int GetSelectionStart()
	{
		return HasSelection() ? Math.Min(_selectionAnchor, _cursorIndex) : _cursorIndex;
	}

	private int GetSelectionEnd()
	{
		return HasSelection() ? Math.Max(_selectionAnchor, _cursorIndex) : _cursorIndex;
	}

	private string GetSelectedText()
	{
		if (!HasSelection())
		{
			return string.Empty;
		}
		int selectionStart = GetSelectionStart();
		return _realText.Substring(selectionStart, GetSelectionEnd() - selectionStart);
	}

	private void ClearSelection()
	{
		_selectionAnchor = -1;
	}

	private bool IsCtrlDown()
	{
		return Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl);
	}

	private bool IsShiftDown()
	{
		return Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift);
	}

	private void StartRepeatAction(RepeatAction action)
	{
		_repeatAction = action;
		_repeatActionHasUndoSnapshot = false;
		_nextRepeatTime = Environment.TickCount + InitialRepeatDelayMs;
	}

	private void ClearRepeatAction()
	{
		_repeatAction = RepeatAction.None;
		_repeatActionHasUndoSnapshot = false;
	}

	private void ClearRepeatActionIfReleased()
	{
		if (!IsRepeatKeyStillDown(_repeatAction))
		{
			ClearRepeatAction();
		}
	}

	private bool IsRepeatKeyStillDown(RepeatAction action)
	{
		switch (action)
		{
		case RepeatAction.Backspace:
			return Input.IsKeyDown(InputKey.BackSpace);
		case RepeatAction.Delete:
			return Input.IsKeyDown(InputKey.Delete);
		case RepeatAction.Left:
			return Input.IsKeyDown(InputKey.Left);
		case RepeatAction.Right:
			return Input.IsKeyDown(InputKey.Right);
		case RepeatAction.Up:
			return Input.IsKeyDown(InputKey.Up);
		case RepeatAction.Down:
			return Input.IsKeyDown(InputKey.Down);
		default:
			return false;
		}
	}

	private static string NormalizeText(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		text = text.Replace("\r\n", "\n").Replace('\r', '\n');
		text = text.Replace('<', ' ').Replace('>', ' ');
		return new string(text.Where((char c) => !char.IsControl(c) || c == '\n' || c == '\t').ToArray());
	}

	private static string SanitizeClipboardText(string text)
	{
		text = NormalizeText(text).Normalize(NormalizationForm.FormC);
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		foreach (char c in text)
		{
			switch (c)
			{
			case '\u00A0':
			case '\u1680':
			case '\u2000':
			case '\u2001':
			case '\u2002':
			case '\u2003':
			case '\u2004':
			case '\u2005':
			case '\u2006':
			case '\u2007':
			case '\u2008':
			case '\u2009':
			case '\u200A':
			case '\u202F':
			case '\u205F':
			case '\u3000':
				stringBuilder.Append(' ');
				continue;
			case '\u200B':
			case '\u200C':
			case '\u200D':
			case '\u2060':
			case '\uFEFF':
			case '\uFFFD':
				continue;
			}
			UnicodeCategory unicodeCategory = char.GetUnicodeCategory(c);
			if (unicodeCategory == UnicodeCategory.Surrogate || unicodeCategory == UnicodeCategory.PrivateUse || unicodeCategory == UnicodeCategory.OtherNotAssigned || unicodeCategory == UnicodeCategory.Format)
			{
				continue;
			}
			stringBuilder.Append(c);
		}
		return stringBuilder.ToString();
	}

	private string ApplyLengthLimit(string text)
	{
		if (MaxLength >= 0 && text.Length > MaxLength)
		{
			return text.Substring(0, MaxLength);
		}
		return text;
	}

	private int GetAllowedExtraLength(int selectedLength)
	{
		if (MaxLength < 0)
		{
			return int.MaxValue;
		}
		return Math.Max(0, MaxLength - (_realText.Length - selectedLength));
	}
}
