using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
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
			int num = Math.Max(8, value);
			if (_editorFontSize != num)
			{
				_editorFontSize = num;
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
				((PropertyOwnerObject)this).OnPropertyChanged<string>(text, "RealText");
				MarkLayoutDirty();
			}
		}
	}

	public DevMultilineEditableTextWidget(UIContext context)
		: base(context)
	{
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Expected O, but got Unknown
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Expected O, but got Unknown
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Expected O, but got Unknown
		((Widget)this).IsFocusable = true;
		((Widget)this).DoNotPassEventsToChildren = true;
		_contentRoot = new Widget(context)
		{
			Id = "EditorContentRoot",
			WidthSizePolicy = (SizePolicy)1,
			HeightSizePolicy = (SizePolicy)0,
			DoNotAcceptEvents = true,
			DoNotPassEventsToChildren = true
		};
		((Widget)this).AddChild(_contentRoot);
		_caretWidget = new Widget(context)
		{
			Id = "EditorCaret",
			WidthSizePolicy = (SizePolicy)0,
			HeightSizePolicy = (SizePolicy)0,
			SuggestedWidth = 2f,
			SuggestedHeight = 24f,
			HorizontalAlignment = (HorizontalAlignment)0,
			VerticalAlignment = (VerticalAlignment)0,
			Color = Color.FromUint(4294956543u),
			Sprite = context.SpriteData.GetSprite("BlankWhiteSquare_9"),
			DoNotAcceptEvents = true,
			DoNotPassEventsToChildren = true
		};
		((Widget)this).AddChild(_caretWidget);
		Font defaultFont = context.FontFactory.DefaultFont;
		_measurementText = new Text(1, 1, defaultFont, (Func<int, Font>)context.FontFactory.GetUsableFontForCharacter);
		_measurementText.CurrentLanguage = (ILanguage)(object)context.FontFactory.CurrentLanguage;
		_measurementText.CanBreakWords = false;
		ApplyBrushToChildren();
	}

	public override void OnBrushChanged()
	{
		((BrushWidget)this).OnBrushChanged();
		ApplyBrushToChildren();
		MarkLayoutDirty();
	}

	public override void HandleInput(IReadOnlyList<int> lastKeysPressed)
	{
		if (!((Widget)this).IsDisabled && !HandleClipboardAndUndoShortcuts())
		{
			if (IsCtrlDown() || Input.IsKeyDown((InputKey)184))
			{
				ClearRepeatActionIfReleased();
			}
			else if (!HandleSinglePressKeys() && !HandleRepeatableKeys())
			{
				HandleCharacterInput(lastKeysPressed);
			}
		}
	}

	protected override void OnMousePressed()
	{
		((Widget)this).OnMousePressed();
		if (((Widget)this).EventManager != null && (object)((Widget)this).EventManager.FocusedWidget != this)
		{
			((Widget)this).EventManager.FocusedWidget = (Widget)(object)this;
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
		((Widget)this).OnMouseMove();
		if (_mouseSelecting)
		{
			_cursorIndex = GetCursorIndexFromMouse();
		}
	}

	protected override void OnMouseReleased()
	{
		((Widget)this).OnMouseReleased();
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
		((Widget)this).OnGainFocus();
		_hasCaretFocus = true;
		_caretWidget.IsVisible = true;
	}

	protected override void OnLoseFocus()
	{
		((Widget)this).OnLoseFocus();
		_hasCaretFocus = false;
		_mouseSelecting = false;
		_caretWidget.IsVisible = false;
	}

	protected override void OnLateUpdate(float dt)
	{
		((Widget)this).OnLateUpdate(dt);
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
			((BrushWidget)_lineWidgets[i]).Brush = CreateLineBrush();
		}
	}

	private Brush CreateLineBrush()
	{
		Brush val = ((BrushWidget)this).ReadOnlyBrush.Clone();
		val.FontSize = EditorFontSize;
		val.TextHorizontalAlignment = (TextHorizontalAlignment)0;
		val.TextVerticalAlignment = (TextVerticalAlignment)0;
		return val;
	}

	private void MarkLayoutDirty()
	{
		_layoutDirty = true;
		((Widget)this).SetMeasureAndLayoutDirty();
	}

	private void RebuildVisualLines()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		_layoutDirty = false;
		_visualLines.Clear();
		UpdateMeasurementFont();
		float availableWidth = Math.Max(32f, ((Widget)this).Size.X);
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
			int num = 0;
			while (num <= text.Length)
			{
				if (num == text.Length)
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
				if (text[num] == '\n')
				{
					_visualLines.Add(new VisualLine
					{
						StartIndex = num,
						EndIndex = num,
						Text = string.Empty
					});
					num++;
					continue;
				}
				int num2 = text.IndexOf('\n', num);
				if (num2 < 0)
				{
					num2 = text.Length;
				}
				while (num < num2)
				{
					int val = FindBestLineLength(text, num, num2, availableWidth);
					int end = Math.Min(num2, num + Math.Max(1, val));
					int val2 = FindPreferredBreak(text, num, end, num2);
					end = Math.Max(num + 1, val2);
					_visualLines.Add(new VisualLine
					{
						StartIndex = num,
						EndIndex = end,
						Text = text.Substring(num, end - num)
					});
					num = end;
				}
				if (num < text.Length && text[num] == '\n')
				{
					num++;
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
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Expected O, but got Unknown
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Expected O, but got Unknown
		while (_lineWidgets.Count < _visualLines.Count)
		{
			Widget val = new Widget(((Widget)this).Context)
			{
				Id = "Selection_" + _selectionWidgets.Count,
				WidthSizePolicy = (SizePolicy)0,
				HeightSizePolicy = (SizePolicy)0,
				HorizontalAlignment = (HorizontalAlignment)0,
				VerticalAlignment = (VerticalAlignment)0,
				Sprite = ((Widget)this).Context.SpriteData.GetSprite("BlankWhiteSquare_9"),
				Color = Color.FromUint(1714631475u),
				DoNotAcceptEvents = true,
				DoNotPassEventsToChildren = true,
				IsVisible = false
			};
			TextWidget val2 = new TextWidget(((Widget)this).Context)
			{
				Id = "Line_" + _lineWidgets.Count,
				WidthSizePolicy = (SizePolicy)1,
				HeightSizePolicy = (SizePolicy)0,
				HorizontalAlignment = (HorizontalAlignment)0,
				VerticalAlignment = (VerticalAlignment)0,
				CanBreakWords = false,
				DoNotAcceptEvents = true,
				DoNotPassEventsToChildren = true
			};
			((BrushWidget)val2).Brush = CreateLineBrush();
			_selectionWidgets.Add(val);
			_lineWidgets.Add(val2);
			_contentRoot.AddChild(val);
			_contentRoot.AddChild((Widget)(object)val2);
		}
		while (_lineWidgets.Count > _visualLines.Count)
		{
			int index = _lineWidgets.Count - 1;
			_contentRoot.RemoveChild((Widget)(object)_lineWidgets[index]);
			_contentRoot.RemoveChild(_selectionWidgets[index]);
			_lineWidgets.RemoveAt(index);
			_selectionWidgets.RemoveAt(index);
		}
		float suggestedHeight = Math.Max(_lineHeightUnscaled, (float)_visualLines.Count * _lineHeightUnscaled);
		_contentRoot.SuggestedHeight = suggestedHeight;
		for (int i = 0; i < _visualLines.Count; i++)
		{
			float positionYOffset = (float)i * _lineHeightUnscaled;
			TextWidget val3 = _lineWidgets[i];
			((BrushWidget)val3).Brush = CreateLineBrush();
			val3.Text = _visualLines[i].Text;
			((Widget)val3).PositionXOffset = 0f;
			((Widget)val3).PositionYOffset = positionYOffset;
			((Widget)val3).SuggestedHeight = _lineHeightUnscaled;
			Widget val4 = _selectionWidgets[i];
			val4.PositionYOffset = positionYOffset;
			val4.SuggestedHeight = _lineHeightUnscaled;
		}
	}

	private int FindBestLineLength(string text, int start, int paragraphEnd, float availableWidth)
	{
		int num = 1;
		int num2 = Math.Max(1, paragraphEnd - start);
		int result = 1;
		while (num <= num2)
		{
			int num3 = (num + num2) / 2;
			if (MeasureWidth(text.Substring(start, num3)) <= availableWidth)
			{
				result = num3;
				num = num3 + 1;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		return result;
	}

	private int FindPreferredBreak(string text, int start, int end, int paragraphEnd)
	{
		if (end >= paragraphEnd)
		{
			return end;
		}
		for (int num = end; num > start; num--)
		{
			char c = text[num - 1];
			if (char.IsWhiteSpace(c) || c == '，' || c == '。' || c == '；' || c == '：' || c == '、' || c == ',' || c == '.' || c == ';' || c == ':')
			{
				return num;
			}
		}
		return end;
	}

	private void UpdateMeasurementFont()
	{
		_measurementText.CurrentLanguage = (ILanguage)(object)((Widget)this).Context.FontFactory.CurrentLanguage;
		_measurementText.Font = GetCurrentFont();
		_measurementText.FontSize = (float)EditorFontSize * ((Widget)this)._scaleToUse;
		_measurementText.HorizontalAlignment = (TextHorizontalAlignment)0;
		_measurementText.VerticalAlignment = (TextVerticalAlignment)0;
		_measurementText.CanBreakWords = false;
		_lineHeightScaled = Math.Max(16f, (float)GetCurrentFont().LineHeight * ((float)EditorFontSize / (float)Math.Max(1, GetCurrentFont().Size)) * ((Widget)this)._scaleToUse);
		_lineHeightUnscaled = _lineHeightScaled * Math.Max(0.001f, ((Widget)this)._inverseScaleToUse);
		_caretWidget.SuggestedHeight = _lineHeightUnscaled;
		_caretWidget.SuggestedWidth = Math.Max(1f, 2f * Math.Max(0.001f, ((Widget)this)._inverseScaleToUse));
	}

	private Font GetCurrentFont()
	{
		Font val = ((((BrushWidget)this).ReadOnlyBrush.Font != null) ? ((BrushWidget)this).ReadOnlyBrush.Font : ((Widget)this).Context.FontFactory.DefaultFont);
		return ((Widget)this).Context.FontFactory.GetMappedFontForLocalization(val.Name);
	}

	private float MeasureWidth(string text)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		_measurementText.Value = (string.IsNullOrEmpty(text) ? " " : text);
		return _measurementText.GetPreferredSize(false, 0f, false, 0f, ((Widget)this).Context.SpriteData, ((Widget)this)._scaleToUse).X;
	}

	private bool HandleClipboardAndUndoShortcuts()
	{
		if (!IsCtrlDown())
		{
			return false;
		}
		if (IsShiftDown() && Input.IsKeyPressed((InputKey)44))
		{
			Redo();
			return true;
		}
		if (Input.IsKeyPressed((InputKey)44))
		{
			Undo();
			return true;
		}
		if (Input.IsKeyPressed((InputKey)21))
		{
			Redo();
			return true;
		}
		if (Input.IsKeyPressed((InputKey)30))
		{
			_selectionAnchor = 0;
			_cursorIndex = _realText.Length;
			return true;
		}
		if (Input.IsKeyPressed((InputKey)46))
		{
			string selectedText = GetSelectedText();
			if (!string.IsNullOrEmpty(selectedText))
			{
				WindowsClipboardHelper.SetText(selectedText);
			}
			return true;
		}
		if (Input.IsKeyPressed((InputKey)45))
		{
			string selectedText2 = GetSelectedText();
			if (!string.IsNullOrEmpty(selectedText2))
			{
				WindowsClipboardHelper.SetText(selectedText2);
				ReplaceSelection(string.Empty);
			}
			return true;
		}
		if (Input.IsKeyPressed((InputKey)47))
		{
			ReplaceSelection(SanitizeClipboardText(WindowsClipboardHelper.GetText()));
			return true;
		}
		return false;
	}

	private bool HandleSinglePressKeys()
	{
		bool keepSelection = IsShiftDown();
		if (Input.IsKeyPressed((InputKey)199))
		{
			MoveCursorToBoundary(toLineStart: true, keepSelection);
			ClearRepeatAction();
			return true;
		}
		if (Input.IsKeyPressed((InputKey)207))
		{
			MoveCursorToBoundary(toLineStart: false, keepSelection);
			ClearRepeatAction();
			return true;
		}
		if (Input.IsKeyPressed((InputKey)28) || Input.IsKeyPressed((InputKey)156))
		{
			ReplaceSelection("\n");
			ClearRepeatAction();
			return true;
		}
		if (Input.IsKeyPressed((InputKey)15))
		{
			ReplaceSelection("    ");
			ClearRepeatAction();
			return true;
		}
		return false;
	}

	private bool HandleRepeatableKeys()
	{
		bool keepSelection = IsShiftDown();
		bool flag = IsCtrlDown();
		if (Input.IsKeyPressed((InputKey)14))
		{
			StartRepeatAction(RepeatAction.Backspace);
			if (PerformBackspace(flag, recordUndo: true))
			{
				_repeatActionHasUndoSnapshot = true;
			}
			return true;
		}
		if (Input.IsKeyPressed((InputKey)211))
		{
			StartRepeatAction(RepeatAction.Delete);
			if (PerformDelete(flag, recordUndo: true))
			{
				_repeatActionHasUndoSnapshot = true;
			}
			return true;
		}
		if (Input.IsKeyPressed((InputKey)203))
		{
			StartRepeatAction(RepeatAction.Left);
			MoveCursorHorizontal(flag ? (FindWordBoundary(-1) - _cursorIndex) : (-1), keepSelection);
			return true;
		}
		if (Input.IsKeyPressed((InputKey)205))
		{
			StartRepeatAction(RepeatAction.Right);
			MoveCursorHorizontal((!flag) ? 1 : (FindWordBoundary(1) - _cursorIndex), keepSelection);
			return true;
		}
		if (Input.IsKeyPressed((InputKey)200))
		{
			StartRepeatAction(RepeatAction.Up);
			MoveCursorVertical(-1, keepSelection);
			return true;
		}
		if (Input.IsKeyPressed((InputKey)208))
		{
			StartRepeatAction(RepeatAction.Down);
			MoveCursorVertical(1, keepSelection);
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
			PerformBackspace(flag, !_repeatActionHasUndoSnapshot);
			_repeatActionHasUndoSnapshot = true;
			break;
		case RepeatAction.Delete:
			PerformDelete(flag, !_repeatActionHasUndoSnapshot);
			_repeatActionHasUndoSnapshot = true;
			break;
		case RepeatAction.Left:
			MoveCursorHorizontal(flag ? (FindWordBoundary(-1) - _cursorIndex) : (-1), keepSelection);
			break;
		case RepeatAction.Right:
			MoveCursorHorizontal((!flag) ? 1 : (FindWordBoundary(1) - _cursorIndex), keepSelection);
			break;
		case RepeatAction.Up:
			MoveCursorVertical(-1, keepSelection);
			break;
		case RepeatAction.Down:
			MoveCursorVertical(1, keepSelection);
			break;
		}
		_nextRepeatTime = Environment.TickCount + 30;
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
		string newText = _realText.Remove(selectionStart, selectionEnd - selectionStart).Insert(selectionStart, replacement);
		ApplyTextChange(newText, selectionStart + replacement.Length, -1, recordUndo: true);
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
		int num = (ctrl ? FindWordBoundary(-1) : (_cursorIndex - 1));
		string newText = _realText.Remove(num, _cursorIndex - num);
		ApplyTextChange(newText, num, -1, recordUndo);
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
		int num = (ctrl ? FindWordBoundary(1) : (_cursorIndex + 1));
		string newText = _realText.Remove(_cursorIndex, Math.Max(1, num - _cursorIndex));
		ApplyTextChange(newText, _cursorIndex, -1, recordUndo);
		return true;
	}

	private void MoveCursorHorizontal(int delta, bool keepSelection)
	{
		if (delta != 0)
		{
			SetCursorIndex(Math.Max(0, Math.Min(_realText.Length, _cursorIndex + delta)), keepSelection);
		}
	}

	private void MoveCursorVertical(int lineDelta, bool keepSelection)
	{
		if (_layoutDirty)
		{
			RebuildVisualLines();
		}
		GetCursorLineAndColumn(out var lineIndex, out var column);
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
		GetCursorLineAndColumn(out var lineIndex, out var _);
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
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		if (_layoutDirty)
		{
			RebuildVisualLines();
		}
		ref Rectangle2D areaRect = ref ((Widget)this).AreaRect;
		Vector2 mousePosition = ((Widget)this).EventManager.MousePosition;
		Vector2 val = ((Rectangle2D)(ref areaRect)).TransformScreenPositionToLocal(ref mousePosition);
		float num = Math.Max(0f, val.X);
		float num2 = Math.Max(0f, val.Y);
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
		float num = Math.Max(0.001f, ((Widget)this)._inverseScaleToUse);
		for (int i = 0; i < _selectionWidgets.Count; i++)
		{
			Widget val = _selectionWidgets[i];
			if (!flag || i >= _visualLines.Count)
			{
				val.IsVisible = false;
				continue;
			}
			VisualLine visualLine = _visualLines[i];
			int num2 = Math.Max(selectionStart, visualLine.StartIndex);
			int num3 = Math.Min(selectionEnd, visualLine.EndIndex);
			if (num3 <= num2)
			{
				val.IsVisible = false;
				continue;
			}
			float num4 = MeasureWidth(_realText.Substring(visualLine.StartIndex, Math.Max(0, num2 - visualLine.StartIndex)));
			float num5 = MeasureWidth(_realText.Substring(visualLine.StartIndex, Math.Max(0, num3 - visualLine.StartIndex)));
			val.PositionXOffset = num4 * num;
			val.PositionYOffset = (float)i * _lineHeightUnscaled;
			val.SuggestedWidth = Math.Max(1f, (num5 - num4) * num);
			val.SuggestedHeight = _lineHeightUnscaled;
			val.IsVisible = true;
		}
	}

	private void UpdateCaretVisual()
	{
		if (_layoutDirty || !_hasCaretFocus)
		{
			_caretWidget.IsVisible = false;
			return;
		}
		GetCursorLineAndColumn(out var lineIndex, out var _);
		VisualLine visualLine = _visualLines[Math.Max(0, Math.Min(_visualLines.Count - 1, lineIndex))];
		string text = _realText.Substring(visualLine.StartIndex, Math.Max(0, _cursorIndex - visualLine.StartIndex));
		float num = MeasureWidth(text);
		float num2 = Math.Max(0.001f, ((Widget)this)._inverseScaleToUse);
		_caretWidget.PositionXOffset = num * num2;
		_caretWidget.PositionYOffset = (float)lineIndex * _lineHeightUnscaled;
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
		column = ((_visualLines.Count != 0) ? Math.Max(0, _cursorIndex - _visualLines[lineIndex].StartIndex) : 0);
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
		int i;
		for (i = _cursorIndex; i < _realText.Length && !char.IsWhiteSpace(_realText[i]); i++)
		{
		}
		for (; i < _realText.Length && char.IsWhiteSpace(_realText[i]); i++)
		{
		}
		return i;
	}

	private void ApplyTextChange(string newText, int newCursorIndex, int newSelectionAnchor, bool recordUndo)
	{
		newText = ApplyLengthLimit(newText);
		if (!(_realText == newText) || _cursorIndex != newCursorIndex || _selectionAnchor != newSelectionAnchor)
		{
			if (recordUndo)
			{
				PushUndoSnapshot();
				_redoStack.Clear();
			}
			_realText = newText;
			_cursorIndex = Math.Max(0, Math.Min(newText.Length, newCursorIndex));
			_selectionAnchor = ((newSelectionAnchor >= 0) ? Math.Max(0, Math.Min(newText.Length, newSelectionAnchor)) : (-1));
			((PropertyOwnerObject)this).OnPropertyChanged<string>(_realText, "RealText");
			MarkLayoutDirty();
		}
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
		if (_undoStack.Count != 0)
		{
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
			((PropertyOwnerObject)this).OnPropertyChanged<string>(_realText, "RealText");
			MarkLayoutDirty();
		}
	}

	private void Redo()
	{
		if (_redoStack.Count != 0)
		{
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
			((PropertyOwnerObject)this).OnPropertyChanged<string>(_realText, "RealText");
			MarkLayoutDirty();
		}
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
		return Input.IsKeyDown((InputKey)29) || Input.IsKeyDown((InputKey)157);
	}

	private bool IsShiftDown()
	{
		return Input.IsKeyDown((InputKey)42) || Input.IsKeyDown((InputKey)54);
	}

	private void StartRepeatAction(RepeatAction action)
	{
		_repeatAction = action;
		_repeatActionHasUndoSnapshot = false;
		_nextRepeatTime = Environment.TickCount + 500;
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
		return action switch
		{
			RepeatAction.Backspace => Input.IsKeyDown((InputKey)14), 
			RepeatAction.Delete => Input.IsKeyDown((InputKey)211), 
			RepeatAction.Left => Input.IsKeyDown((InputKey)203), 
			RepeatAction.Right => Input.IsKeyDown((InputKey)205), 
			RepeatAction.Up => Input.IsKeyDown((InputKey)200), 
			RepeatAction.Down => Input.IsKeyDown((InputKey)208), 
			_ => false, 
		};
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
		string text2 = text;
		foreach (char c in text2)
		{
			switch (c)
			{
			case '\u00a0':
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
			case '\u200a':
			case '\u202f':
			case '\u205f':
			case '\u3000':
				stringBuilder.Append(' ');
				continue;
			case '\u200b':
			case '\u200c':
			case '\u200d':
			case '\u2060':
			case '\ufeff':
			case '\ufffd':
				continue;
			}
			UnicodeCategory unicodeCategory = char.GetUnicodeCategory(c);
			if (unicodeCategory != UnicodeCategory.Surrogate && unicodeCategory != UnicodeCategory.PrivateUse && unicodeCategory != UnicodeCategory.OtherNotAssigned && unicodeCategory != UnicodeCategory.Format)
			{
				stringBuilder.Append(c);
			}
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
