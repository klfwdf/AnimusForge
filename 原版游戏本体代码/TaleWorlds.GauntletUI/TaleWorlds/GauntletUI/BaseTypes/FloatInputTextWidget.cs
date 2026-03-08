using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TaleWorlds.InputSystem;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x02000059 RID: 89
	public class FloatInputTextWidget : EditableTextWidget
	{
		// Token: 0x170001B0 RID: 432
		// (get) Token: 0x0600060C RID: 1548 RVA: 0x00019D87 File Offset: 0x00017F87
		// (set) Token: 0x0600060D RID: 1549 RVA: 0x00019D8F File Offset: 0x00017F8F
		public bool EnableClamp { get; set; }

		// Token: 0x0600060E RID: 1550 RVA: 0x00019D98 File Offset: 0x00017F98
		public FloatInputTextWidget(UIContext context)
			: base(context)
		{
			base.PropertyChanged += this.IntegerInputTextWidget_PropertyChanged;
		}

		// Token: 0x0600060F RID: 1551 RVA: 0x00019DCC File Offset: 0x00017FCC
		private void IntegerInputTextWidget_PropertyChanged(PropertyOwnerObject arg1, string arg2, object arg3)
		{
			float floatText;
			if (arg2 == "RealText" && (string)arg3 != this.FloatText.ToString() && float.TryParse((string)arg3, out floatText))
			{
				this.FloatText = floatText;
			}
		}

		// Token: 0x06000610 RID: 1552 RVA: 0x00019E18 File Offset: 0x00018018
		public override void HandleInput(IReadOnlyList<int> lastKeysPressed)
		{
			int count = lastKeysPressed.Count;
			for (int i = 0; i < count; i++)
			{
				int num = lastKeysPressed[i];
				char c2 = Convert.ToChar(num);
				if (char.IsDigit(c2) || (c2 == '.' && this.GetNumberOfSeperatorsInText(base.RealText) == 0))
				{
					float num2;
					if (num != 60 && num != 62 && float.TryParse(this.GetAppendResult(num), out num2))
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
					this.TrySetStringAsFloat(base.RealText);
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
					this.TrySetStringAsFloat(base.RealText);
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
					this.TrySetStringAsFloat(base.RealText);
				}
			}
		}

		// Token: 0x06000611 RID: 1553 RVA: 0x0001A2F0 File Offset: 0x000184F0
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
				string text2 = base.RealText;
				if (this._editableText.SelectedTextBegin != this._editableText.SelectedTextEnd)
				{
					if (this._editableText.SelectedTextEnd > base.RealText.Length)
					{
						text = Convert.ToChar(lastPressedKey).ToString();
						flag = true;
					}
					else
					{
						text2 = base.RealText.Substring(0, this._editableText.SelectedTextBegin) + base.RealText.Substring(this._editableText.SelectedTextEnd, base.RealText.Length - this._editableText.SelectedTextEnd);
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
						text = text2.Substring(0, cursorPosition) + c.ToString() + text2.Substring(cursorPosition, text2.Length - cursorPosition);
					}
					this._editableText.ResetSelected();
				}
				else
				{
					if (this._editableText.CursorPosition == base.RealText.Length)
					{
						flag = true;
					}
					int cursorPosition2 = this._editableText.CursorPosition;
					char c2 = Convert.ToChar(lastPressedKey);
					text = text2.Substring(0, cursorPosition2) + c2.ToString() + text2.Substring(cursorPosition2, text2.Length - cursorPosition2);
					if (!flag)
					{
						this._editableText.SetCursor(cursorPosition2 + 1, true, false);
					}
				}
			}
			this.TrySetStringAsFloat(text);
			if (flag)
			{
				this._editableText.SetCursorPosition(base.RealText.Length, true);
			}
		}

		// Token: 0x06000612 RID: 1554 RVA: 0x0001A4F0 File Offset: 0x000186F0
		private bool TrySetStringAsFloat(string str)
		{
			float @float;
			if (float.TryParse(str, out @float))
			{
				this.SetFloat(@float);
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
			return false;
		}

		// Token: 0x06000613 RID: 1555 RVA: 0x0001A5B4 File Offset: 0x000187B4
		private void SetFloat(float newFloat)
		{
			if (this.EnableClamp && (newFloat > this.MaxFloat || newFloat < this.MinFloat))
			{
				newFloat = ((newFloat > this.MaxFloat) ? this.MaxFloat : this.MinFloat);
				base.ResetSelected();
			}
			this.FloatText = newFloat;
			if (this.FloatText.ToString() != base.RealText)
			{
				base.RealText = this.FloatText.ToString();
				base.Text = this.FloatText.ToString();
			}
		}

		// Token: 0x06000614 RID: 1556 RVA: 0x0001A644 File Offset: 0x00018844
		private int GetNumberOfSeperatorsInText(string realText)
		{
			return realText.Count((char c) => char.IsPunctuation(c));
		}

		// Token: 0x06000615 RID: 1557 RVA: 0x0001A66C File Offset: 0x0001886C
		private string GetAppendResult(int lastPressedKey)
		{
			if (base.MaxLength > -1 && base.Text.Length >= base.MaxLength)
			{
				return base.RealText;
			}
			string realText = base.RealText;
			if (this._editableText.SelectedTextBegin != this._editableText.SelectedTextEnd)
			{
				base.RealText.Substring(0, this._editableText.SelectedTextBegin) + base.RealText.Substring(this._editableText.SelectedTextEnd, base.RealText.Length - this._editableText.SelectedTextEnd);
			}
			int cursorPosition = this._editableText.CursorPosition;
			char c = Convert.ToChar(lastPressedKey);
			return base.RealText.Substring(0, cursorPosition) + c.ToString() + base.RealText.Substring(cursorPosition, base.RealText.Length - cursorPosition);
		}

		// Token: 0x06000616 RID: 1558 RVA: 0x0001A74C File Offset: 0x0001894C
		public override void SetAllText(string text)
		{
			base.DeleteText(0, base.RealText.Length);
			string text2 = Regex.Replace(text, "[<>]+", " ");
			text2 = new string((from c in text2
				where char.IsDigit(c)
				select c).ToArray<char>());
			base.AppendText(text2);
			this.TrySetStringAsFloat(text2);
		}

		// Token: 0x170001B1 RID: 433
		// (get) Token: 0x06000617 RID: 1559 RVA: 0x0001A7BB File Offset: 0x000189BB
		// (set) Token: 0x06000618 RID: 1560 RVA: 0x0001A7C3 File Offset: 0x000189C3
		[Editor(false)]
		public float FloatText
		{
			get
			{
				return this._floatText;
			}
			set
			{
				if (this._floatText != value)
				{
					this._floatText = value;
					base.OnPropertyChanged(value, "FloatText");
					base.RealText = value.ToString();
					base.Text = value.ToString();
				}
			}
		}

		// Token: 0x170001B2 RID: 434
		// (get) Token: 0x06000619 RID: 1561 RVA: 0x0001A7FB File Offset: 0x000189FB
		// (set) Token: 0x0600061A RID: 1562 RVA: 0x0001A803 File Offset: 0x00018A03
		[Editor(false)]
		public float MaxFloat
		{
			get
			{
				return this._maxFloat;
			}
			set
			{
				if (this._maxFloat != value)
				{
					this._maxFloat = value;
				}
			}
		}

		// Token: 0x170001B3 RID: 435
		// (get) Token: 0x0600061B RID: 1563 RVA: 0x0001A815 File Offset: 0x00018A15
		// (set) Token: 0x0600061C RID: 1564 RVA: 0x0001A81D File Offset: 0x00018A1D
		[Editor(false)]
		public float MinFloat
		{
			get
			{
				return this._minFloat;
			}
			set
			{
				if (this._minFloat != value)
				{
					this._minFloat = value;
				}
			}
		}

		// Token: 0x040002E3 RID: 739
		private float _floatText;

		// Token: 0x040002E4 RID: 740
		private float _maxFloat = float.MaxValue;

		// Token: 0x040002E5 RID: 741
		private float _minFloat = float.MinValue;
	}
}
