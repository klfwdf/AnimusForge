using System;
using System.Collections.Generic;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x02000053 RID: 83
	public class ButtonWidget : ImageWidget
	{
		// Token: 0x17000195 RID: 405
		// (get) Token: 0x06000592 RID: 1426 RVA: 0x00017453 File Offset: 0x00015653
		// (set) Token: 0x06000593 RID: 1427 RVA: 0x0001745B File Offset: 0x0001565B
		[Editor(false)]
		public ButtonType ButtonType
		{
			get
			{
				return this._buttonType;
			}
			set
			{
				if (this._buttonType != value)
				{
					this._buttonType = value;
					this.Refresh();
				}
			}
		}

		// Token: 0x06000594 RID: 1428 RVA: 0x00017473 File Offset: 0x00015673
		protected override bool OnPreviewMousePressed()
		{
			base.OnPreviewMousePressed();
			return true;
		}

		// Token: 0x06000595 RID: 1429 RVA: 0x00017480 File Offset: 0x00015680
		protected override void RefreshState()
		{
			base.RefreshState();
			if (!base.OverrideDefaultStateSwitchingEnabled)
			{
				if (base.IsDisabled)
				{
					this.SetState("Disabled");
				}
				else if (this.IsSelected && this.DominantSelectedState)
				{
					this.SetState("Selected");
				}
				else if (base.IsPressed)
				{
					this.SetState("Pressed");
				}
				else if (base.IsHovered)
				{
					this.SetState("Hovered");
				}
				else if (this.IsSelected && !this.DominantSelectedState)
				{
					this.SetState("Selected");
				}
				else
				{
					this.SetState("Default");
				}
			}
			if (base.UpdateChildrenStates)
			{
				for (int i = 0; i < base.ChildCount; i++)
				{
					Widget child = base.GetChild(i);
					if (!(child is ImageWidget) || !((ImageWidget)child).OverrideDefaultStateSwitchingEnabled)
					{
						child.SetState(base.CurrentState);
					}
				}
			}
		}

		// Token: 0x06000596 RID: 1430 RVA: 0x00017564 File Offset: 0x00015764
		private void Refresh()
		{
			if (this.IsToggle)
			{
				this.ShowHideToggle();
			}
		}

		// Token: 0x06000597 RID: 1431 RVA: 0x00017574 File Offset: 0x00015774
		private void ShowHideToggle()
		{
			if (this.ToggleIndicator != null)
			{
				if (this._isSelected)
				{
					this.ToggleIndicator.Show();
					return;
				}
				this.ToggleIndicator.Hide();
			}
		}

		// Token: 0x06000598 RID: 1432 RVA: 0x0001759D File Offset: 0x0001579D
		public ButtonWidget(UIContext context)
			: base(context)
		{
			base.FrictionEnabled = true;
		}

		// Token: 0x06000599 RID: 1433 RVA: 0x000175C0 File Offset: 0x000157C0
		protected internal override void OnMousePressed()
		{
			if (this._clickState == ButtonWidget.ButtonClickState.None)
			{
				this._clickState = ButtonWidget.ButtonClickState.HandlingClick;
				base.IsPressed = true;
				if (!base.DoNotPassEventsToChildren)
				{
					for (int i = 0; i < base.ChildCount; i++)
					{
						Widget child = base.GetChild(i);
						if (child != null)
						{
							child.IsPressed = true;
						}
					}
				}
			}
		}

		// Token: 0x0600059A RID: 1434 RVA: 0x00017610 File Offset: 0x00015810
		protected internal override void OnMouseReleased()
		{
			if (this._clickState == ButtonWidget.ButtonClickState.HandlingClick)
			{
				this._clickState = ButtonWidget.ButtonClickState.None;
				base.IsPressed = false;
				if (!base.DoNotPassEventsToChildren)
				{
					for (int i = 0; i < base.ChildCount; i++)
					{
						Widget child = base.GetChild(i);
						if (child != null)
						{
							child.IsPressed = false;
						}
					}
				}
				if (this.IsPointInsideMeasuredAreaAndCheckIfVisible())
				{
					this.HandleClick();
				}
			}
		}

		// Token: 0x0600059B RID: 1435 RVA: 0x0001766D File Offset: 0x0001586D
		private bool IsPointInsideMeasuredAreaAndCheckIfVisible()
		{
			return base.IsPointInsideMeasuredArea(base.EventManager.MousePosition) && base.IsRecursivelyVisible();
		}

		// Token: 0x0600059C RID: 1436 RVA: 0x00017690 File Offset: 0x00015890
		protected internal override void OnMouseAlternatePressed()
		{
			if (this._clickState == ButtonWidget.ButtonClickState.None)
			{
				this._clickState = ButtonWidget.ButtonClickState.HandlingAlternateClick;
				base.IsPressed = true;
				if (!base.DoNotPassEventsToChildren)
				{
					for (int i = 0; i < base.ChildCount; i++)
					{
						Widget child = base.GetChild(i);
						if (child != null)
						{
							child.IsPressed = true;
						}
					}
				}
			}
		}

		// Token: 0x0600059D RID: 1437 RVA: 0x000176E0 File Offset: 0x000158E0
		protected internal override void OnMouseAlternateReleased()
		{
			if (this._clickState == ButtonWidget.ButtonClickState.HandlingAlternateClick)
			{
				this._clickState = ButtonWidget.ButtonClickState.None;
				base.IsPressed = false;
				if (!base.DoNotPassEventsToChildren)
				{
					for (int i = 0; i < base.ChildCount; i++)
					{
						Widget child = base.GetChild(i);
						if (child != null)
						{
							child.IsPressed = false;
						}
					}
				}
				if (this.IsPointInsideMeasuredAreaAndCheckIfVisible())
				{
					this.HandleAlternateClick();
				}
			}
		}

		// Token: 0x0600059E RID: 1438 RVA: 0x00017740 File Offset: 0x00015940
		protected virtual void HandleClick()
		{
			foreach (Action<Widget> action in this.ClickEventHandlers)
			{
				action(this);
			}
			bool isSelected = this.IsSelected;
			if (this.IsToggle)
			{
				this.IsSelected = !this.IsSelected;
			}
			else if (this.IsRadio)
			{
				this.IsSelected = true;
				if (this.IsSelected && !isSelected && base.ParentWidget is Container)
				{
					(base.ParentWidget as Container).OnChildSelected(this);
				}
			}
			base.EventFired("Click", Array.Empty<object>());
			if (base.Context.EventManager.Time - this._lastClickTime < 0.5f)
			{
				base.EventFired("DoubleClick", Array.Empty<object>());
				return;
			}
			this._lastClickTime = base.Context.EventManager.Time;
		}

		// Token: 0x0600059F RID: 1439 RVA: 0x00017840 File Offset: 0x00015A40
		protected virtual void HandleAlternateClick()
		{
			base.EventFired("AlternateClick", Array.Empty<object>());
		}

		// Token: 0x17000196 RID: 406
		// (get) Token: 0x060005A0 RID: 1440 RVA: 0x00017852 File Offset: 0x00015A52
		public bool IsToggle
		{
			get
			{
				return this.ButtonType == ButtonType.Toggle;
			}
		}

		// Token: 0x17000197 RID: 407
		// (get) Token: 0x060005A1 RID: 1441 RVA: 0x0001785D File Offset: 0x00015A5D
		public bool IsRadio
		{
			get
			{
				return this.ButtonType == ButtonType.Radio;
			}
		}

		// Token: 0x17000198 RID: 408
		// (get) Token: 0x060005A2 RID: 1442 RVA: 0x00017868 File Offset: 0x00015A68
		// (set) Token: 0x060005A3 RID: 1443 RVA: 0x00017870 File Offset: 0x00015A70
		[Editor(false)]
		public Widget ToggleIndicator
		{
			get
			{
				return this._toggleIndicator;
			}
			set
			{
				if (this._toggleIndicator != value)
				{
					this._toggleIndicator = value;
					this.Refresh();
				}
			}
		}

		// Token: 0x17000199 RID: 409
		// (get) Token: 0x060005A4 RID: 1444 RVA: 0x00017888 File Offset: 0x00015A88
		// (set) Token: 0x060005A5 RID: 1445 RVA: 0x00017890 File Offset: 0x00015A90
		[Editor(false)]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (this._isSelected != value)
				{
					this._isSelected = value;
					this.Refresh();
					this.RefreshState();
					base.OnPropertyChanged(value, "IsSelected");
				}
			}
		}

		// Token: 0x1700019A RID: 410
		// (get) Token: 0x060005A6 RID: 1446 RVA: 0x000178BA File Offset: 0x00015ABA
		// (set) Token: 0x060005A7 RID: 1447 RVA: 0x000178C2 File Offset: 0x00015AC2
		[Editor(false)]
		public bool DominantSelectedState
		{
			get
			{
				return this._dominantSelectedState;
			}
			set
			{
				if (this._dominantSelectedState != value)
				{
					this._dominantSelectedState = value;
					base.OnPropertyChanged(value, "DominantSelectedState");
				}
			}
		}

		// Token: 0x040002A2 RID: 674
		protected const float _maxDoubleClickDeltaTimeInSeconds = 0.5f;

		// Token: 0x040002A3 RID: 675
		protected float _lastClickTime;

		// Token: 0x040002A4 RID: 676
		private ButtonWidget.ButtonClickState _clickState;

		// Token: 0x040002A5 RID: 677
		private ButtonType _buttonType;

		// Token: 0x040002A6 RID: 678
		public List<Action<Widget>> ClickEventHandlers = new List<Action<Widget>>();

		// Token: 0x040002A7 RID: 679
		private Widget _toggleIndicator;

		// Token: 0x040002A8 RID: 680
		private bool _isSelected;

		// Token: 0x040002A9 RID: 681
		private bool _dominantSelectedState = true;

		// Token: 0x0200008F RID: 143
		private enum ButtonClickState
		{
			// Token: 0x04000478 RID: 1144
			None,
			// Token: 0x04000479 RID: 1145
			HandlingClick,
			// Token: 0x0400047A RID: 1146
			HandlingAlternateClick
		}
	}
}
