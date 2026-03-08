using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TaleWorlds.InputSystem;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x0200005D RID: 93
	public class IntegerInputTextWidget : EditableTextWidget
	{
		// Token: 0x170001C2 RID: 450
		// (get) Token: 0x06000641 RID: 1601 RVA: 0x0001AB18 File Offset: 0x00018D18
		// (set) Token: 0x06000642 RID: 1602 RVA: 0x0001AB20 File Offset: 0x00018D20
		public bool EnableClamp { get; set; }

		// Token: 0x06000643 RID: 1603 RVA: 0x0001AB29 File Offset: 0x00018D29
		public IntegerInputTextWidget(UIContext context)
			: base(context)
		{
		}

		// Token: 0x06000644 RID: 1604 RVA: 0x0001AB50 File Offset: 0x00018D50
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
						this.HandleInput(num);
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
				this._nextRepeatTime = tickCount + 500;
				if (Input.IsKeyDown(InputKey.LeftShift))
				{
					if (!this._editableText.IsAnySelected())
					{
						this._editableText.BeginSelection();
					}
					this._isSelection = true;
				}
			}
			if (this._cursorDirection != EditableTextWidget.CursorMovementDirection.None && (flag || flag2 || tickCount >= this._nextRepeatTime))
			{
				if (flag)
				{
					int direction = (int)this._cursorDirection;
					if (Input.IsKeyDown(InputKey.LeftControl))
					{
						direction = base.FindNextWordPosition(direction) - this._editableText.CursorPosition;
					}
					base.MoveCursor(direction, this._isSelection);
					if (tickCount >= this._nextRepeatTime)
					{
						this._nextRepeatTime = tickCount + 30;
					}
				}
				else if (flag2)
				{
					int direction2 = ((this._cursorDirection == EditableTextWidget.CursorMovementDirection.Left) ? (-this._editableText.CursorPosition) : (this._editableText.VisibleText.Length - this._editableText.CursorPosition));
					base.MoveCursor(direction2, this._isSelection);
					if (tickCount >= this._nextRepeatTime)
					{
						this._nextRepeatTime = tickCount + 30;
					}
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
						base.DeleteText(this._editableText.SelectedTextBegin, this._editableText.SelectedTextEnd);
					}
					else if (Input.IsKeyDown(InputKey.LeftControl))
					{
						if (this._keyboardAction == EditableTextWidget.KeyboardAction.BackSpace)
						{
							base.DeleteText(base.FindNextWordPosition(-1), this._editableText.CursorPosition);
						}
						else
						{
							base.DeleteText(this._editableText.CursorPosition, base.FindNextWordPosition(1));
						}
					}
					else
					{
						base.DeleteChar(this._keyboardAction == EditableTextWidget.KeyboardAction.Delete);
					}
					this.TrySetStringAsInteger(base.RealText);
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
					base.CopyText(this._editableText.SelectedTextBegin, this._editableText.SelectedTextEnd);
					return;
				}
				if (Input.IsKeyPressed(InputKey.X))
				{
					base.CopyText(this._editableText.SelectedTextBegin, this._editableText.SelectedTextEnd);
					base.DeleteText(this._editableText.SelectedTextBegin, this._editableText.SelectedTextEnd);
					this.TrySetStringAsInteger(base.RealText);
					return;
				}
				if (Input.IsKeyPressed(InputKey.V))
				{
					base.DeleteText(this._editableText.SelectedTextBegin, this._editableText.SelectedTextEnd);
					string text = Regex.Replace(Input.GetClipboardText(), "[<>]+", " ");
					text = new string((from c in text
						where char.IsDigit(c)
						select c).ToArray<char>());
					base.AppendText(text);
					this.TrySetStringAsInteger(base.RealText);
				}
			}
		}

		// Token: 0x06000645 RID: 1605 RVA: 0x0001AFF8 File Offset: 0x000191F8
		private void HandleInput(int lastPressedKey)
		{
			bool flag = false;
			string text = base.RealText;
			string str;
			if (this._editableText.SelectedTextBegin != this._editableText.SelectedTextEnd)
			{
				if (this._editableText.SelectedTextEnd > base.RealText.Length)
				{
					str = Convert.ToChar(lastPressedKey).ToString();
					flag = true;
				}
				else
				{
					text = base.RealText.Substring(0, this._editableText.SelectedTextBegin) + base.RealText.Substring(this._editableText.SelectedTextEnd, base.RealText.Length - this._editableText.SelectedTextEnd);
					if (this._editableText.SelectedTextEnd - this._editableText.SelectedTextBegin >= base.RealText.Length)
					{
						this._editableText.SetCursorPosition(0, true);
						this._editableText.ResetSelected();
						flag = true;
					}
					else
					{
						this._editableText.SetCursorPosition(this._editableText.SelectedTextBegin, true);
					}
					int cursorPosition = this._editableText.CursorPosition;
					char c = Convert.ToChar(lastPressedKey);
					str = text.Substring(0, cursorPosition) + c.ToString() + text.Substring(cursorPosition, text.Length - cursorPosition);
				}
				this._editableText.ResetSelected();
			}
			else if (base.MaxLength > -1 && base.Text.Length >= base.MaxLength)
			{
				str = base.RealText;
			}
			else
			{
				if (this._editableText.CursorPosition == base.RealText.Length)
				{
					flag = true;
				}
				int cursorPosition2 = this._editableText.CursorPosition;
				char c2 = Convert.ToChar(lastPressedKey);
				str = text.Substring(0, cursorPosition2) + c2.ToString() + text.Substring(cursorPosition2, text.Length - cursorPosition2);
				if (!flag)
				{
					this._editableText.SetCursor(cursorPosition2 + 1, true, false);
				}
			}
			this.TrySetStringAsInteger(str);
			if (flag)
			{
				this._editableText.SetCursorPosition(base.RealText.Length, true);
			}
		}

		// Token: 0x06000646 RID: 1606 RVA: 0x0001B1F8 File Offset: 0x000193F8
		private void SetInteger(int newInteger)
		{
			if (this.EnableClamp && (newInteger > this.MaxInt || newInteger < this.MinInt))
			{
				newInteger = ((newInteger > this.MaxInt) ? this.MaxInt : this.MinInt);
				base.ResetSelected();
			}
			this.IntText = newInteger;
			if (this.IntText.ToString() != base.RealText)
			{
				base.RealText = this.IntText.ToString();
				base.Text = this.IntText.ToString();
			}
		}

		// Token: 0x06000647 RID: 1607 RVA: 0x0001B288 File Offset: 0x00019488
		private bool TrySetStringAsInteger(string str)
		{
			int integer;
			if (!int.TryParse(str, out integer))
			{
				if (!string.IsNullOrWhiteSpace(str))
				{
					return false;
				}
				integer = 0;
			}
			this.SetInteger(integer);
			if (this._editableText.SelectedTextEnd - this._editableText.SelectedTextBegin >= base.RealText.Length)
			{
				this._editableText.SetCursorPosition(0, true);
				this._editableText.ResetSelected();
			}
			else if (this._editableText.SelectedTextBegin != 0 || this._editableText.SelectedTextEnd != 0)
			{
				this._editableText.SetCursorPosition(this._editableText.SelectedTextBegin, true);
			}
			if (this._editableText.CursorPosition > base.RealText.Length)
			{
				this._editableText.SetCursorPosition(base.RealText.Length, true);
			}
			return true;
		}

		// Token: 0x06000648 RID: 1608 RVA: 0x0001B354 File Offset: 0x00019554
		public override void SetAllText(string text)
		{
			base.DeleteText(0, base.RealText.Length);
			string text2 = Regex.Replace(text, "[<>]+", " ");
			text2 = new string((from c in text2
				where char.IsDigit(c)
				select c).ToArray<char>());
			this.TrySetStringAsInteger(text2);
		}

		// Token: 0x170001C3 RID: 451
		// (get) Token: 0x06000649 RID: 1609 RVA: 0x0001B3BC File Offset: 0x000195BC
		// (set) Token: 0x0600064A RID: 1610 RVA: 0x0001B3C4 File Offset: 0x000195C4
		[Editor(false)]
		public int IntText
		{
			get
			{
				return this._intText;
			}
			set
			{
				if (this._intText != value)
				{
					this._intText = value;
					base.OnPropertyChanged(value, "IntText");
					base.RealText = value.ToString();
					base.Text = value.ToString();
				}
			}
		}

		// Token: 0x170001C4 RID: 452
		// (get) Token: 0x0600064B RID: 1611 RVA: 0x0001B3FC File Offset: 0x000195FC
		// (set) Token: 0x0600064C RID: 1612 RVA: 0x0001B404 File Offset: 0x00019604
		[Editor(false)]
		public int MaxInt
		{
			get
			{
				return this._maxInt;
			}
			set
			{
				if (this._maxInt != value)
				{
					this._maxInt = value;
				}
			}
		}

		// Token: 0x170001C5 RID: 453
		// (get) Token: 0x0600064D RID: 1613 RVA: 0x0001B416 File Offset: 0x00019616
		// (set) Token: 0x0600064E RID: 1614 RVA: 0x0001B41E File Offset: 0x0001961E
		[Editor(false)]
		public int MinInt
		{
			get
			{
				return this._minInt;
			}
			set
			{
				if (this._minInt != value)
				{
					this._minInt = value;
				}
			}
		}

		// Token: 0x040002F3 RID: 755
		private int _intText = -1;

		// Token: 0x040002F4 RID: 756
		private int _maxInt = int.MaxValue;

		// Token: 0x040002F5 RID: 757
		private int _minInt = int.MinValue;
	}
}
