using System;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.ExtraWidgets
{
	// Token: 0x02000005 RID: 5
	public class DialogButtonsParentWidget : Widget
	{
		// Token: 0x1700000D RID: 13
		// (get) Token: 0x06000028 RID: 40 RVA: 0x00002520 File Offset: 0x00000720
		// (set) Token: 0x06000029 RID: 41 RVA: 0x00002528 File Offset: 0x00000728
		public string CancelClickSound { get; set; }

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x0600002A RID: 42 RVA: 0x00002531 File Offset: 0x00000731
		// (set) Token: 0x0600002B RID: 43 RVA: 0x00002539 File Offset: 0x00000739
		public string ConfirmClickSound { get; set; }

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x0600002C RID: 44 RVA: 0x00002542 File Offset: 0x00000742
		// (set) Token: 0x0600002D RID: 45 RVA: 0x0000254A File Offset: 0x0000074A
		public string ResetClickSound { get; set; }

		// Token: 0x0600002E RID: 46 RVA: 0x00002553 File Offset: 0x00000753
		public DialogButtonsParentWidget(UIContext context)
			: base(context)
		{
		}

		// Token: 0x0600002F RID: 47 RVA: 0x0000255C File Offset: 0x0000075C
		private void OnClickCancel(Widget widget)
		{
			if (!string.IsNullOrEmpty(this.CancelClickSound))
			{
				base.Context.TwoDimensionContext.PlaySound(this.CancelClickSound);
			}
		}

		// Token: 0x06000030 RID: 48 RVA: 0x00002581 File Offset: 0x00000781
		private void OnClickConfirm(Widget widget)
		{
			if (!string.IsNullOrEmpty(this.ConfirmClickSound))
			{
				base.Context.TwoDimensionContext.PlaySound(this.ConfirmClickSound);
			}
		}

		// Token: 0x06000031 RID: 49 RVA: 0x000025A6 File Offset: 0x000007A6
		private void OnClickReset(Widget widget)
		{
			if (!string.IsNullOrEmpty(this.ResetClickSound))
			{
				base.Context.TwoDimensionContext.PlaySound(this.ResetClickSound);
			}
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x06000032 RID: 50 RVA: 0x000025CB File Offset: 0x000007CB
		// (set) Token: 0x06000033 RID: 51 RVA: 0x000025D4 File Offset: 0x000007D4
		[Editor(false)]
		public ButtonWidget CancelButton
		{
			get
			{
				return this._cancelButton;
			}
			set
			{
				if (value != this._cancelButton)
				{
					ButtonWidget cancelButton = this._cancelButton;
					if (cancelButton != null)
					{
						cancelButton.ClickEventHandlers.Remove(new Action<Widget>(this.OnClickCancel));
					}
					this._cancelButton = value;
					ButtonWidget cancelButton2 = this._cancelButton;
					if (cancelButton2 == null)
					{
						return;
					}
					cancelButton2.ClickEventHandlers.Add(new Action<Widget>(this.OnClickCancel));
				}
			}
		}

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000034 RID: 52 RVA: 0x00002635 File Offset: 0x00000835
		// (set) Token: 0x06000035 RID: 53 RVA: 0x00002640 File Offset: 0x00000840
		[Editor(false)]
		public ButtonWidget ConfirmButton
		{
			get
			{
				return this._confirmButton;
			}
			set
			{
				if (value != this._confirmButton)
				{
					ButtonWidget confirmButton = this._confirmButton;
					if (confirmButton != null)
					{
						confirmButton.ClickEventHandlers.Remove(new Action<Widget>(this.OnClickConfirm));
					}
					this._confirmButton = value;
					ButtonWidget confirmButton2 = this._confirmButton;
					if (confirmButton2 == null)
					{
						return;
					}
					confirmButton2.ClickEventHandlers.Add(new Action<Widget>(this.OnClickConfirm));
				}
			}
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000036 RID: 54 RVA: 0x000026A1 File Offset: 0x000008A1
		// (set) Token: 0x06000037 RID: 55 RVA: 0x000026AC File Offset: 0x000008AC
		[Editor(false)]
		public ButtonWidget ResetButton
		{
			get
			{
				return this._resetButton;
			}
			set
			{
				if (value != this._resetButton)
				{
					ButtonWidget resetButton = this._resetButton;
					if (resetButton != null)
					{
						resetButton.ClickEventHandlers.Remove(new Action<Widget>(this.OnClickReset));
					}
					this._resetButton = value;
					ButtonWidget resetButton2 = this._resetButton;
					if (resetButton2 == null)
					{
						return;
					}
					resetButton2.ClickEventHandlers.Add(new Action<Widget>(this.OnClickReset));
				}
			}
		}

		// Token: 0x04000018 RID: 24
		private ButtonWidget _cancelButton;

		// Token: 0x04000019 RID: 25
		private ButtonWidget _confirmButton;

		// Token: 0x0400001A RID: 26
		private ButtonWidget _resetButton;
	}
}
