using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using TaleWorlds.GauntletUI.Layout;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x02000058 RID: 88
	public class EditableTextWidget : BrushWidget
	{
		// Token: 0x170001A9 RID: 425
		// (get) Token: 0x060005E3 RID: 1507 RVA: 0x0001882C File Offset: 0x00016A2C
		// (set) Token: 0x060005E4 RID: 1508 RVA: 0x00018834 File Offset: 0x00016A34
		public int MaxLength { get; set; } = 512;

		// Token: 0x170001AA RID: 426
		// (get) Token: 0x060005E5 RID: 1509 RVA: 0x0001883D File Offset: 0x00016A3D
		// (set) Token: 0x060005E6 RID: 1510 RVA: 0x00018845 File Offset: 0x00016A45
		public bool IsObfuscationEnabled
		{
			get
			{
				return this._isObfuscationEnabled;
			}
			set
			{
				if (value != this._isObfuscationEnabled)
				{
					this._isObfuscationEnabled = value;
					this.OnObfuscationToggled(value);
				}
			}
		}

		// Token: 0x170001AB RID: 427
		// (get) Token: 0x060005E7 RID: 1511 RVA: 0x00018860 File Offset: 0x00016A60
		private Vector2 LocalMousePosition
		{
			get
			{
				Vector2 mousePosition = base.EventManager.MousePosition;
				return this.AreaRect.TransformScreenPositionToLocal(mousePosition);
			}
		}

		// Token: 0x170001AC RID: 428
		// (get) Token: 0x060005E8 RID: 1512 RVA: 0x00018886 File Offset: 0x00016A86
		// (set) Token: 0x060005E9 RID: 1513 RVA: 0x0001888E File Offset: 0x00016A8E
		public string DefaultSearchText { get; set; }

		// Token: 0x170001AD RID: 429
		// (get) Token: 0x060005EA RID: 1514 RVA: 0x00018897 File Offset: 0x00016A97
		// (set) Token: 0x060005EB RID: 1515 RVA: 0x0001889F File Offset: 0x00016A9F
		[Editor(false)]
		public string RealText
		{
			get
			{
				return this._realText;
			}
			set
			{
				if (this._realText != value)
				{
					if (string.IsNullOrEmpty(value))
					{
						value = "";
					}
					this._realText = value;
					base.OnPropertyChanged<string>(value, "RealText");
					this.UpdateRealAndVisibleText(value);
				}
			}
		}

		// Token: 0x170001AE RID: 430
		// (get) Token: 0x060005EC RID: 1516 RVA: 0x000188D8 File Offset: 0x00016AD8
		// (set) Token: 0x060005ED RID: 1517 RVA: 0x000188E0 File Offset: 0x00016AE0
		[Editor(false)]
		public string KeyboardInfoText
		{
			get
			{
				return this._keyboardInfoText;
			}
			set
			{
				if (this._keyboardInfoText != value)
				{
					this._keyboardInfoText = value;
					base.OnPropertyChanged<string>(value, "KeyboardInfoText");
				}
			}
		}

		// Token: 0x170001AF RID: 431
		// (get) Token: 0x060005EE RID: 1518 RVA: 0x00018903 File Offset: 0x00016B03
		// (set) Token: 0x060005EF RID: 1519 RVA: 0x00018910 File Offset: 0x00016B10
		[Editor(false)]
		public string Text
		{
			get
			{
				return this._editableText.VisibleText;
			}
			set
			{
				if (this._editableText.VisibleText != value)
				{
					if (string.IsNullOrEmpty(value))
					{
						value = "";
					}
					this._editableText.VisibleText = value;
					base.OnPropertyChanged<string>(value, "Text");
					this._editableText.SetCursor(this._editableText.VisibleText.Length, base.IsFocused, false);
					this.UpdateRealAndVisibleText(value);
					base.SetMeasureAndLayoutDirty();
				}
			}
		}

		// Token: 0x060005F0 RID: 1520 RVA: 0x00018988 File Offset: 0x00016B88
		public EditableTextWidget(UIContext context)
			: base(context)
		{
			FontFactory fontFactory = context.FontFactory;
			this._editableText = new EditableText((int)base.Size.X, (int)base.Size.Y, fontFactory.DefaultFont, new Func<int, Font>(fontFactory.GetUsableFontForCharacter));
			base.LayoutImp = new TextLayout(this._editableText);
			this._realText = "";
			this._textHeight = -1;
			this._cursorVisible = false;
			this._lastFontBrush = null;
			this._cursorRectangle = Rectangle2D.Create();
			this._highlightRectangle = Rectangle2D.Create();
			this._cursorDirection = EditableTextWidget.CursorMovementDirection.None;
			this._keyboardAction = EditableTextWidget.KeyboardAction.None;
			this._nextRepeatTime = int.MinValue;
			this._isSelection = false;
			base.IsFocusable = true;
		}

		// Token: 0x060005F1 RID: 1521 RVA: 0x00018A7C File Offset: 0x00016C7C
		protected override void OnUpdate(float dt)
		{
			base.OnUpdate(dt);
			this.UpdateText();
			if (base.IsFocused && base.IsEnabled)
			{
				this._editableText.BlinkTimer += dt;
				if (this._editableText.BlinkTimer > 0.5f)
				{
					this._editableText.BlinkCursor();
					this._editableText.BlinkTimer = 0f;
				}
				if (base.ContainsState("Selected"))
				{
					this.SetState("Selected");
				}
			}
			else if (this._editableText.IsCursorVisible())
			{
				this._editableText.BlinkCursor();
			}
			this.SetEditTextParameters();
		}

		// Token: 0x060005F2 RID: 1522 RVA: 0x00018B20 File Offset: 0x00016D20
		private void UpdateRealAndVisibleText(string newText)
		{
			if (!this._updatingTexts)
			{
				this._updatingTexts = true;
				this._editableText.CurrentLanguage = base.Context.FontFactory.CurrentLanguage;
				this.RealText = newText;
				this.Text = (this.IsObfuscationEnabled ? this.ObfuscateText(this.RealText) : this.RealText);
				this._updatingTexts = false;
			}
		}

		// Token: 0x060005F3 RID: 1523 RVA: 0x00018B88 File Offset: 0x00016D88
		private void SetEditTextParameters()
		{
			bool flag = false;
			this._editableText.CurrentLanguage = base.Context.FontFactory.CurrentLanguage;
			this.UpdateFontData();
			if (this._editableText.HorizontalAlignment != base.ReadOnlyBrush.TextHorizontalAlignment)
			{
				this._editableText.HorizontalAlignment = base.ReadOnlyBrush.TextHorizontalAlignment;
				flag = true;
			}
			if (this._editableText.VerticalAlignment != base.ReadOnlyBrush.TextVerticalAlignment)
			{
				this._editableText.VerticalAlignment = base.ReadOnlyBrush.TextVerticalAlignment;
				flag = true;
			}
			if (this._editableText.TextHeight != this._textHeight)
			{
				this._textHeight = this._editableText.TextHeight;
				flag = true;
			}
			if (flag)
			{
				base.SetMeasureAndLayoutDirty();
			}
		}

		// Token: 0x060005F4 RID: 1524 RVA: 0x00018C47 File Offset: 0x00016E47
		protected void BlinkCursor()
		{
			this._cursorVisible = !this._cursorVisible;
		}

		// Token: 0x060005F5 RID: 1525 RVA: 0x00018C58 File Offset: 0x00016E58
		protected void ResetSelected()
		{
			this._editableText.ResetSelected();
		}

		// Token: 0x060005F6 RID: 1526 RVA: 0x00018C68 File Offset: 0x00016E68
		protected void DeleteChar(bool nextChar = false)
		{
			int num = this._editableText.CursorPosition;
			if (nextChar)
			{
				num++;
			}
			if (num == 0 || num > this.Text.Length)
			{
				return;
			}
			this.RealText = this.RealText.Substring(0, num - 1) + this.RealText.Substring(num, this.RealText.Length - num);
			this._editableText.SetCursor(num - 1, true, false);
			this.ResetSelected();
		}

		// Token: 0x060005F7 RID: 1527 RVA: 0x00018CE3 File Offset: 0x00016EE3
		protected int FindNextWordPosition(int direction)
		{
			return this._editableText.FindNextWordPosition(direction);
		}

		// Token: 0x060005F8 RID: 1528 RVA: 0x00018CF1 File Offset: 0x00016EF1
		protected void MoveCursor(int direction, bool withSelection = false)
		{
			this._editableText.SetCursor(this._editableText.CursorPosition + direction, true, withSelection);
			if (!withSelection)
			{
				this.ResetSelected();
			}
		}

		// Token: 0x060005F9 RID: 1529 RVA: 0x00018D18 File Offset: 0x00016F18
		protected string GetAppendCharacterResult(int charCode)
		{
			if (this.MaxLength > -1 && this.Text.Length >= this.MaxLength)
			{
				return this.RealText;
			}
			int cursorPosition = this._editableText.CursorPosition;
			char c = Convert.ToChar(charCode);
			return this.RealText.Substring(0, cursorPosition) + c.ToString() + this.RealText.Substring(cursorPosition, this.RealText.Length - cursorPosition);
		}

		// Token: 0x060005FA RID: 1530 RVA: 0x00018D90 File Offset: 0x00016F90
		protected void AppendCharacter(int charCode)
		{
			if (this.MaxLength > -1 && this.Text.Length >= this.MaxLength)
			{
				return;
			}
			int cursorPosition = this._editableText.CursorPosition;
			this.RealText = this.GetAppendCharacterResult(charCode);
			this._editableText.SetCursor(cursorPosition + 1, true, false);
			this.ResetSelected();
		}

		// Token: 0x060005FB RID: 1531 RVA: 0x00018DEC File Offset: 0x00016FEC
		protected void AppendText(string text)
		{
			if (this.MaxLength > -1 && this.Text.Length >= this.MaxLength)
			{
				return;
			}
			if (this.MaxLength > -1 && this.Text.Length + text.Length >= this.MaxLength)
			{
				text = text.Substring(0, this.MaxLength - this.Text.Length);
			}
			int cursorPosition = this._editableText.CursorPosition;
			this.RealText = this.RealText.Substring(0, cursorPosition) + text + this.RealText.Substring(cursorPosition, this.RealText.Length - cursorPosition);
			this._editableText.SetCursor(cursorPosition + text.Length, true, false);
			this.ResetSelected();
		}

		// Token: 0x060005FC RID: 1532 RVA: 0x00018EB0 File Offset: 0x000170B0
		protected void DeleteText(int beginIndex, int endIndex)
		{
			if (beginIndex == endIndex)
			{
				return;
			}
			if (beginIndex > endIndex || beginIndex < 0 || endIndex < 0 || endIndex > this.RealText.Length)
			{
				Debug.FailedAssert("Calling DeleteText when beginIndex or endIndex is invalid!", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\BaseTypes\\EditableTextWidget.cs", "DeleteText", 355);
				return;
			}
			this.RealText = this.RealText.Substring(0, beginIndex) + this.RealText.Substring(endIndex, this.RealText.Length - endIndex);
			this._editableText.SetCursor(beginIndex, true, false);
			this.ResetSelected();
		}

		// Token: 0x060005FD RID: 1533 RVA: 0x00018F3C File Offset: 0x0001713C
		protected void CopyText(int beginIndex, int endIndex)
		{
			if (beginIndex == endIndex)
			{
				return;
			}
			int num = Math.Min(beginIndex, endIndex);
			int num2 = Math.Max(beginIndex, endIndex);
			if (num < 0)
			{
				num = 0;
			}
			if (num2 > this.RealText.Length)
			{
				num2 = this.RealText.Length;
			}
			Input.SetClipboardText(this.RealText.Substring(num, num2 - num));
		}

		// Token: 0x060005FE RID: 1534 RVA: 0x00018F94 File Offset: 0x00017194
		protected void PasteText()
		{
			string text = Regex.Replace(Input.GetClipboardText(), "[<>]+", " ");
			text = new string((from c in text
				where !char.IsControl(c)
				select c).ToArray<char>());
			this.AppendText(text);
		}

		// Token: 0x060005FF RID: 1535 RVA: 0x00018FF0 File Offset: 0x000171F0
		public override void HandleInput(IReadOnlyList<int> lastKeysPressed)
		{
			if (base.IsDisabled)
			{
				return;
			}
			int count = lastKeysPressed.Count;
			for (int i = 0; i < count; i++)
			{
				int num = lastKeysPressed[i];
				if (num >= 32 && (num < 127 || num >= 160))
				{
					if (num != 60 && num != 62)
					{
						this.DeleteText(this._editableText.SelectedTextBegin, this._editableText.SelectedTextEnd);
						this.AppendCharacter(num);
					}
					this._cursorDirection = EditableTextWidget.CursorMovementDirection.None;
					this._isSelection = false;
				}
			}
			int tickCount = Environment.TickCount;
			bool flag = false;
			bool flag2 = false;
			if (Input.IsKeyPressed(InputKey.Left))
			{
				this._cursorDirection = EditableTextWidget.CursorMovementDirection.Left;
				flag = true;
			}
			else if (Input.IsKeyPressed(InputKey.Right))
			{
				this._cursorDirection = EditableTextWidget.CursorMovementDirection.Right;
				flag = true;
			}
			else if ((this._cursorDirection == EditableTextWidget.CursorMovementDirection.Left && !Input.IsKeyDown(InputKey.Left)) || (this._cursorDirection == EditableTextWidget.CursorMovementDirection.Right && !Input.IsKeyDown(InputKey.Right)))
			{
				this._cursorDirection = EditableTextWidget.CursorMovementDirection.None;
				if (!Input.IsKeyDown(InputKey.LeftShift))
				{
					this._isSelection = false;
				}
			}
			else if (Input.IsKeyReleased(InputKey.LeftShift))
			{
				this._isSelection = false;
			}
			else if (Input.IsKeyDown(InputKey.Home))
			{
				this._cursorDirection = EditableTextWidget.CursorMovementDirection.Left;
				flag2 = true;
			}
			else if (Input.IsKeyDown(InputKey.End))
			{
				this._cursorDirection = EditableTextWidget.CursorMovementDirection.Right;
				flag2 = true;
			}
			if (flag || flag2)
			{
				if (flag)
				{
					this._nextRepeatTime = tickCount + 500;
				}
				if (Input.IsKeyDown(InputKey.LeftShift))
				{
					if (!this._editableText.IsAnySelected())
					{
						this._editableText.BeginSelection();
					}
					this._isSelection = true;
				}
			}
			if (this._cursorDirection != EditableTextWidget.CursorMovementDirection.None)
			{
				if (flag || tickCount >= this._nextRepeatTime)
				{
					int direction = (int)this._cursorDirection;
					if (Input.IsKeyDown(InputKey.LeftControl))
					{
						direction = this.FindNextWordPosition(direction) - this._editableText.CursorPosition;
					}
					this.MoveCursor(direction, this._isSelection);
					if (tickCount >= this._nextRepeatTime)
					{
						this._nextRepeatTime = tickCount + 30;
					}
				}
				else if (flag2)
				{
					int direction2 = ((this._cursorDirection == EditableTextWidget.CursorMovementDirection.Left) ? (-this._editableText.CursorPosition) : (this._editableText.VisibleText.Length - this._editableText.CursorPosition));
					this.MoveCursor(direction2, this._isSelection);
				}
			}
			bool flag3 = false;
			if (Input.IsKeyPressed(InputKey.BackSpace))
			{
				flag3 = true;
				this._keyboardAction = EditableTextWidget.KeyboardAction.BackSpace;
				this._nextRepeatTime = tickCount + 500;
			}
			else if (Input.IsKeyPressed(InputKey.Delete))
			{
				flag3 = true;
				this._keyboardAction = EditableTextWidget.KeyboardAction.Delete;
				this._nextRepeatTime = tickCount + 500;
			}
			if ((this._keyboardAction == EditableTextWidget.KeyboardAction.BackSpace && !Input.IsKeyDown(InputKey.BackSpace)) || (this._keyboardAction == EditableTextWidget.KeyboardAction.Delete && !Input.IsKeyDown(InputKey.Delete)))
			{
				this._keyboardAction = EditableTextWidget.KeyboardAction.None;
			}
			if (Input.IsKeyReleased(InputKey.Enter) || Input.IsKeyReleased(InputKey.NumpadEnter))
			{
				base.EventFired("TextEntered", Array.Empty<object>());
				return;
			}
			if (this._keyboardAction == EditableTextWidget.KeyboardAction.BackSpace || this._keyboardAction == EditableTextWidget.KeyboardAction.Delete)
			{
				if (flag3 || tickCount >= this._nextRepeatTime)
				{
					if (this._editableText.IsAnySelected())
					{
						this.DeleteText(this._editableText.SelectedTextBegin, this._editableText.SelectedTextEnd);
					}
					else if (Input.IsKeyDown(InputKey.LeftControl))
					{
						if (this._keyboardAction == EditableTextWidget.KeyboardAction.BackSpace)
						{
							this.DeleteText(this.FindNextWordPosition(-1), this._editableText.CursorPosition);
						}
						else
						{
							this.DeleteText(this._editableText.CursorPosition, this.FindNextWordPosition(1));
						}
					}
					else
					{
						this.DeleteChar(this._keyboardAction == EditableTextWidget.KeyboardAction.Delete);
					}
					if (tickCount >= this._nextRepeatTime)
					{
						this._nextRepeatTime = tickCount + 30;
						return;
					}
				}
			}
			else if (Input.IsKeyDown(InputKey.LeftControl) && !Input.IsKeyDown(InputKey.RightAlt))
			{
				if (Input.IsKeyPressed(InputKey.A))
				{
					this._editableText.SelectAll();
					return;
				}
				if (Input.IsKeyPressed(InputKey.C))
				{
					this.CopyText(this._editableText.SelectedTextBegin, this._editableText.SelectedTextEnd);
					return;
				}
				if (Input.IsKeyPressed(InputKey.X))
				{
					this.CopyText(this._editableText.SelectedTextBegin, this._editableText.SelectedTextEnd);
					this.DeleteText(this._editableText.SelectedTextBegin, this._editableText.SelectedTextEnd);
					return;
				}
				if (Input.IsKeyPressed(InputKey.V))
				{
					this.DeleteText(this._editableText.SelectedTextBegin, this._editableText.SelectedTextEnd);
					this.PasteText();
				}
			}
		}

		// Token: 0x06000600 RID: 1536 RVA: 0x0001943F File Offset: 0x0001763F
		protected internal override void OnGainFocus()
		{
			base.OnGainFocus();
			if (string.IsNullOrEmpty(this.RealText) && !string.IsNullOrEmpty(this.DefaultSearchText))
			{
				this._editableText.VisibleText = "";
			}
		}

		// Token: 0x06000601 RID: 1537 RVA: 0x00019474 File Offset: 0x00017674
		protected internal override void OnLoseFocus()
		{
			base.OnLoseFocus();
			this._editableText.ResetSelected();
			this._isSelection = false;
			this._editableText.SetCursor(0, false, false);
			if (string.IsNullOrEmpty(this.RealText) && !string.IsNullOrEmpty(this.DefaultSearchText))
			{
				this._editableText.VisibleText = this.DefaultSearchText;
			}
		}

		// Token: 0x06000602 RID: 1538 RVA: 0x000194D4 File Offset: 0x000176D4
		private void UpdateText()
		{
			if (base.IsDisabled)
			{
				this.SetState("Disabled");
			}
			else if (base.IsPressed)
			{
				this.SetState("Pressed");
			}
			else if (base.IsHovered)
			{
				this.SetState("Hovered");
			}
			else
			{
				this.SetState("Default");
			}
			if (string.IsNullOrEmpty(this.Text) && !string.IsNullOrEmpty(this.DefaultSearchText) && this._mouseState == EditableTextWidget.MouseState.None && base.EventManager.FocusedWidget != this)
			{
				this._editableText.VisibleText = this.DefaultSearchText;
			}
		}

		// Token: 0x06000603 RID: 1539 RVA: 0x0001956C File Offset: 0x0001776C
		private void UpdateFontData()
		{
			if (this._lastFontBrush == base.ReadOnlyBrush && this._lastScale == base._scaleToUse && this._lastLanguageCode == base.Context.FontFactory.CurrentLanguage.LanguageID)
			{
				return;
			}
			this._editableText.StyleFontContainer.ClearFonts();
			foreach (Style style in base.ReadOnlyBrush.Styles)
			{
				Font font;
				if (style.Font != null)
				{
					font = style.Font;
				}
				else if (base.ReadOnlyBrush.Font != null)
				{
					font = base.ReadOnlyBrush.Font;
				}
				else
				{
					font = base.Context.FontFactory.DefaultFont;
				}
				Font mappedFontForLocalization = base.Context.FontFactory.GetMappedFontForLocalization(font.Name);
				this._editableText.StyleFontContainer.Add(style.Name, mappedFontForLocalization, (float)base.ReadOnlyBrush.FontSize * base._scaleToUse);
			}
			this._lastFontBrush = base.ReadOnlyBrush;
			this._lastScale = base._scaleToUse;
			this._lastLanguageCode = base.Context.FontFactory.CurrentLanguage.LanguageID;
			this._editableText.CurrentLanguage = base.Context.FontFactory.CurrentLanguage;
		}

		// Token: 0x06000604 RID: 1540 RVA: 0x000196E0 File Offset: 0x000178E0
		private Font GetFont(Style style = null)
		{
			if (((style != null) ? style.Font : null) != null)
			{
				return base.Context.FontFactory.GetMappedFontForLocalization(style.Font.Name);
			}
			if (base.ReadOnlyBrush.Font != null)
			{
				return base.Context.FontFactory.GetMappedFontForLocalization(base.ReadOnlyBrush.Font.Name);
			}
			return base.Context.FontFactory.DefaultFont;
		}

		// Token: 0x06000605 RID: 1541 RVA: 0x00019758 File Offset: 0x00017958
		protected override void OnLateUpdate(float dt)
		{
			base.OnLateUpdate(dt);
			if (base.Size.X > 0f && base.Size.Y > 0f)
			{
				Vector2 localMousePosition = this.LocalMousePosition;
				bool focus = this._mouseState == EditableTextWidget.MouseState.Down;
				this._editableText.UpdateSize((int)base.Size.X, (int)base.Size.Y);
				this.SetEditTextParameters();
				this.UpdateFontData();
				bool isFixedWidth = base.WidthSizePolicy != SizePolicy.CoverChildren || base.MaxWidth != 0f;
				bool isFixedHeight = base.HeightSizePolicy != SizePolicy.CoverChildren || base.MaxHeight != 0f;
				this._editableText.Update(dt, base.Context.SpriteData, localMousePosition, focus, isFixedWidth, isFixedHeight, base._scaleToUse);
			}
		}

		// Token: 0x06000606 RID: 1542 RVA: 0x00019834 File Offset: 0x00017A34
		protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
		{
			base.OnRender(twoDimensionContext, drawContext);
			if (!string.IsNullOrEmpty(this._editableText.Value))
			{
				Vector2 globalPosition = base.GlobalPosition;
				Style styleOrDefault = base.ReadOnlyBrush.GetStyleOrDefault("Default");
				Font font = this.GetFont(styleOrDefault);
				int lineHeight = font.LineHeight;
				float num = (float)styleOrDefault.FontSize / (float)font.Size;
				float scaleToUse = base._scaleToUse;
				foreach (RichTextPart richTextPart in this._editableText.GetParts())
				{
					if (richTextPart.TextDrawObject.IsValid)
					{
						TextDrawObject textDrawObject = richTextPart.TextDrawObject;
						Rectangle2D rectangle = textDrawObject.Rectangle;
						rectangle.FillLocalValuesFrom(this.AreaRect);
						rectangle.LocalScale = new Vector2(textDrawObject.Text_MeshWidth, textDrawObject.Text_MeshHeight);
						rectangle.CalculateMatrixFrame(base.ParentWidget.AreaRect);
						Style styleOrDefault2 = base.ReadOnlyBrush.GetStyleOrDefault(richTextPart.Style);
						Font defaultFont = richTextPart.DefaultFont;
						int fontSize = styleOrDefault2.FontSize;
						float scaleFactor = (float)fontSize * base._scaleToUse;
						float num2 = (float)fontSize / (float)defaultFont.Size;
						float num3 = (float)defaultFont.LineHeight * num2 * base._scaleToUse;
						TextMaterial textMaterial = styleOrDefault2.CreateTextMaterial(drawContext);
						textMaterial.ColorFactor *= base.ReadOnlyBrush.GlobalColorFactor;
						textMaterial.AlphaFactor *= base.ReadOnlyBrush.GlobalAlphaFactor;
						textMaterial.Color *= base.ReadOnlyBrush.GlobalColor;
						textMaterial.Texture = defaultFont.FontSprite.Texture;
						textMaterial.ScaleFactor = scaleFactor;
						textMaterial.Smooth = defaultFont.Smooth;
						textMaterial.SmoothingConstant = defaultFont.SmoothingConstant;
						if (textMaterial.GlowRadius > 0f || textMaterial.Blur > 0f || textMaterial.OutlineAmount > 0f)
						{
							TextMaterial textMaterial2 = styleOrDefault2.CreateTextMaterial(drawContext);
							textMaterial2.CopyFrom(textMaterial);
							drawContext.Draw(textMaterial2, textDrawObject);
						}
						textMaterial.GlowRadius = 0f;
						textMaterial.Blur = 0f;
						textMaterial.OutlineAmount = 0f;
						if (richTextPart.Style == "Highlight")
						{
							SpriteData spriteData = base.Context.SpriteData;
							string name = "warm_overlay";
							Sprite sprite = spriteData.GetSprite(name);
							SimpleMaterial simpleMaterial = drawContext.CreateSimpleMaterial();
							simpleMaterial.Reset((sprite != null) ? sprite.Texture : null);
							this._highlightRectangle.FillLocalValuesFrom(this.AreaRect);
							this._highlightRectangle.LocalPosition = new Vector2(base.LocalPosition.X + richTextPart.PartPosition.X, base.LocalPosition.Y + richTextPart.PartPosition.Y);
							this._highlightRectangle.LocalScale = new Vector2(richTextPart.WordWidth, num3);
							this._highlightRectangle.CalculateMatrixFrame(base.ParentWidget.AreaRect);
							drawContext.DrawSprite(sprite, simpleMaterial, this._highlightRectangle, base._scaleToUse);
						}
						textDrawObject.Rectangle = rectangle;
						drawContext.Draw(textMaterial, textDrawObject);
					}
				}
				if (this._editableText.IsCursorVisible())
				{
					Style styleOrDefault3 = base.ReadOnlyBrush.GetStyleOrDefault("Default");
					Font font2 = this.GetFont(styleOrDefault3);
					float num4 = (float)styleOrDefault3.FontSize / (float)font2.Size;
					float num5 = (float)font2.LineHeight * num4 * base._scaleToUse;
					Vector2 cursorPosition = this._editableText.GetCursorPosition();
					this._cursorRectangle.FillLocalValuesFrom(this.AreaRect);
					this._cursorRectangle.LocalPosition = new Vector2(base.LocalPosition.X + cursorPosition.X, base.LocalPosition.Y + cursorPosition.Y);
					this._cursorRectangle.LocalScale = new Vector2(1f, num5);
					this._cursorRectangle.CalculateMatrixFrame(base.ParentWidget.AreaRect);
					SpriteData spriteData2 = base.Context.SpriteData;
					string name2 = "BlankWhiteSquare_9";
					Sprite sprite2 = spriteData2.GetSprite(name2);
					SimpleMaterial simpleMaterial2 = drawContext.CreateSimpleMaterial();
					simpleMaterial2.Reset((sprite2 != null) ? sprite2.Texture : null);
					drawContext.DrawSprite(sprite2, simpleMaterial2, this._cursorRectangle, base._scaleToUse);
				}
			}
		}

		// Token: 0x06000607 RID: 1543 RVA: 0x00019CB0 File Offset: 0x00017EB0
		protected internal override void OnMousePressed()
		{
			base.OnMousePressed();
			this._mouseDownPosition = this.LocalMousePosition;
			this._mouseState = EditableTextWidget.MouseState.Down;
			this._editableText.HighlightStart = true;
			this._editableText.HighlightEnd = false;
		}

		// Token: 0x06000608 RID: 1544 RVA: 0x00019CE3 File Offset: 0x00017EE3
		protected internal override void OnMouseReleased()
		{
			base.OnMouseReleased();
			this._mouseState = EditableTextWidget.MouseState.Up;
			this._editableText.HighlightEnd = true;
		}

		// Token: 0x06000609 RID: 1545 RVA: 0x00019CFE File Offset: 0x00017EFE
		private void OnObfuscationToggled(bool isEnabled)
		{
			this.UpdateRealAndVisibleText(this.RealText);
		}

		// Token: 0x0600060A RID: 1546 RVA: 0x00019D0C File Offset: 0x00017F0C
		private string ObfuscateText(string stringToObfuscate)
		{
			return new string(this._obfuscationChar, stringToObfuscate.Length);
		}

		// Token: 0x0600060B RID: 1547 RVA: 0x00019D20 File Offset: 0x00017F20
		public virtual void SetAllText(string text)
		{
			this.DeleteText(0, this.RealText.Length);
			string text2 = Regex.Replace(text, "[<>]+", " ");
			text2 = new string((from c in text2
				where !char.IsControl(c)
				select c).ToArray<char>());
			this.AppendText(text2);
		}

		// Token: 0x040002CD RID: 717
		private Rectangle2D _cursorRectangle;

		// Token: 0x040002CE RID: 718
		private Rectangle2D _highlightRectangle;

		// Token: 0x040002CF RID: 719
		protected EditableText _editableText;

		// Token: 0x040002D0 RID: 720
		protected readonly char _obfuscationChar = '*';

		// Token: 0x040002D1 RID: 721
		protected float _lastScale = -1f;

		// Token: 0x040002D2 RID: 722
		protected bool _isObfuscationEnabled;

		// Token: 0x040002D3 RID: 723
		protected string _lastLanguageCode;

		// Token: 0x040002D4 RID: 724
		protected Brush _lastFontBrush;

		// Token: 0x040002D5 RID: 725
		protected EditableTextWidget.MouseState _mouseState;

		// Token: 0x040002D6 RID: 726
		protected Vector2 _mouseDownPosition;

		// Token: 0x040002D7 RID: 727
		protected bool _cursorVisible;

		// Token: 0x040002D8 RID: 728
		protected int _textHeight;

		// Token: 0x040002D9 RID: 729
		protected EditableTextWidget.CursorMovementDirection _cursorDirection;

		// Token: 0x040002DA RID: 730
		protected EditableTextWidget.KeyboardAction _keyboardAction;

		// Token: 0x040002DB RID: 731
		protected int _nextRepeatTime;

		// Token: 0x040002DC RID: 732
		protected bool _isSelection;

		// Token: 0x040002DD RID: 733
		private bool _updatingTexts;

		// Token: 0x040002E0 RID: 736
		private string _realText = "";

		// Token: 0x040002E1 RID: 737
		private string _keyboardInfoText = "";

		// Token: 0x02000090 RID: 144
		protected enum MouseState
		{
			// Token: 0x0400047C RID: 1148
			None,
			// Token: 0x0400047D RID: 1149
			Down,
			// Token: 0x0400047E RID: 1150
			Up
		}

		// Token: 0x02000091 RID: 145
		protected enum CursorMovementDirection
		{
			// Token: 0x04000480 RID: 1152
			None,
			// Token: 0x04000481 RID: 1153
			Left = -1,
			// Token: 0x04000482 RID: 1154
			Right = 1
		}

		// Token: 0x02000092 RID: 146
		protected enum KeyboardAction
		{
			// Token: 0x04000484 RID: 1156
			None,
			// Token: 0x04000485 RID: 1157
			BackSpace,
			// Token: 0x04000486 RID: 1158
			Delete
		}
	}
}
