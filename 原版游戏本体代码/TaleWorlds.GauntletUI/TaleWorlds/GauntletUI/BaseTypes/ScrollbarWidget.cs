using System;
using System.Numerics;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x02000065 RID: 101
	public class ScrollbarWidget : ImageWidget
	{
		// Token: 0x170001EA RID: 490
		// (get) Token: 0x060006D2 RID: 1746 RVA: 0x0001DA4B File Offset: 0x0001BC4B
		// (set) Token: 0x060006D3 RID: 1747 RVA: 0x0001DA53 File Offset: 0x0001BC53
		[Editor(false)]
		public bool IsDiscrete
		{
			get
			{
				return this._isDiscrete;
			}
			set
			{
				if (this._isDiscrete != value)
				{
					this._isDiscrete = value;
					base.OnPropertyChanged(this._isDiscrete, "IsDiscrete");
				}
			}
		}

		// Token: 0x170001EB RID: 491
		// (get) Token: 0x060006D4 RID: 1748 RVA: 0x0001DA76 File Offset: 0x0001BC76
		// (set) Token: 0x060006D5 RID: 1749 RVA: 0x0001DA7E File Offset: 0x0001BC7E
		[Editor(false)]
		public bool Locked
		{
			get
			{
				return this._locked;
			}
			set
			{
				if (this._locked != value)
				{
					this._locked = value;
					base.OnPropertyChanged(this._locked, "Locked");
				}
			}
		}

		// Token: 0x170001EC RID: 492
		// (get) Token: 0x060006D6 RID: 1750 RVA: 0x0001DAA1 File Offset: 0x0001BCA1
		// (set) Token: 0x060006D7 RID: 1751 RVA: 0x0001DAA9 File Offset: 0x0001BCA9
		[Editor(false)]
		public AlignmentAxis AlignmentAxis { get; set; }

		// Token: 0x170001ED RID: 493
		// (get) Token: 0x060006D8 RID: 1752 RVA: 0x0001DAB2 File Offset: 0x0001BCB2
		// (set) Token: 0x060006D9 RID: 1753 RVA: 0x0001DABA File Offset: 0x0001BCBA
		[Editor(false)]
		public bool ReverseDirection { get; set; }

		// Token: 0x170001EE RID: 494
		// (get) Token: 0x060006DA RID: 1754 RVA: 0x0001DAC3 File Offset: 0x0001BCC3
		// (set) Token: 0x060006DB RID: 1755 RVA: 0x0001DACC File Offset: 0x0001BCCC
		[Editor(false)]
		public float ValueFloat
		{
			get
			{
				return this._valueFloat;
			}
			set
			{
				if (!this.Locked && MathF.Abs(this._valueFloat - value) > 1E-05f)
				{
					float valueFloat = this._valueFloat;
					if (this.MinValue <= this.MaxValue)
					{
						if (this._valueFloat < this.MinValue)
						{
							this._valueFloat = this.MinValue;
						}
						if (this._valueFloat > this.MaxValue)
						{
							this._valueFloat = this.MaxValue;
						}
						if (this.IsDiscrete)
						{
							this._valueFloat = (float)MathF.Round(value);
						}
						else
						{
							this._valueFloat = value;
						}
						this.UpdateHandleByValue();
						if (MathF.Abs(this._valueFloat - valueFloat) > 1E-05f)
						{
							base.OnPropertyChanged(this._valueFloat, "ValueFloat");
							base.OnPropertyChanged(this.ValueInt, "ValueInt");
						}
					}
				}
			}
		}

		// Token: 0x170001EF RID: 495
		// (get) Token: 0x060006DC RID: 1756 RVA: 0x0001DBA1 File Offset: 0x0001BDA1
		// (set) Token: 0x060006DD RID: 1757 RVA: 0x0001DBAE File Offset: 0x0001BDAE
		[Editor(false)]
		public int ValueInt
		{
			get
			{
				return MathF.Round(this.ValueFloat);
			}
			set
			{
				this.ValueFloat = (float)value;
			}
		}

		// Token: 0x170001F0 RID: 496
		// (get) Token: 0x060006DE RID: 1758 RVA: 0x0001DBB8 File Offset: 0x0001BDB8
		// (set) Token: 0x060006DF RID: 1759 RVA: 0x0001DBC0 File Offset: 0x0001BDC0
		[Editor(false)]
		public float MinValue { get; set; }

		// Token: 0x170001F1 RID: 497
		// (get) Token: 0x060006E0 RID: 1760 RVA: 0x0001DBC9 File Offset: 0x0001BDC9
		// (set) Token: 0x060006E1 RID: 1761 RVA: 0x0001DBD1 File Offset: 0x0001BDD1
		[Editor(false)]
		public float MaxValue { get; set; }

		// Token: 0x170001F2 RID: 498
		// (get) Token: 0x060006E2 RID: 1762 RVA: 0x0001DBDA File Offset: 0x0001BDDA
		// (set) Token: 0x060006E3 RID: 1763 RVA: 0x0001DBE2 File Offset: 0x0001BDE2
		[Editor(false)]
		public bool DoNotUpdateHandleSize { get; set; }

		// Token: 0x170001F3 RID: 499
		// (get) Token: 0x060006E4 RID: 1764 RVA: 0x0001DBEB File Offset: 0x0001BDEB
		// (set) Token: 0x060006E5 RID: 1765 RVA: 0x0001DBF3 File Offset: 0x0001BDF3
		[Editor(false)]
		public Widget Handle
		{
			get
			{
				return this._handle;
			}
			set
			{
				if (this._handle != value)
				{
					this._handle = value;
					this.UpdateHandleByValue();
				}
			}
		}

		// Token: 0x170001F4 RID: 500
		// (get) Token: 0x060006E6 RID: 1766 RVA: 0x0001DC0B File Offset: 0x0001BE0B
		// (set) Token: 0x060006E7 RID: 1767 RVA: 0x0001DC13 File Offset: 0x0001BE13
		[Editor(false)]
		public Widget ScrollbarArea { get; set; }

		// Token: 0x060006E8 RID: 1768 RVA: 0x0001DC1C File Offset: 0x0001BE1C
		public ScrollbarWidget(UIContext context)
			: base(context)
		{
			this.ScrollbarArea = this;
			this._firstFrame = true;
		}

		// Token: 0x060006E9 RID: 1769 RVA: 0x0001DC34 File Offset: 0x0001BE34
		protected override void OnLateUpdate(float dt)
		{
			base.OnUpdate(dt);
			if (this.Handle.IsPressed)
			{
				if (!this._handleClicked)
				{
					this._handleClicked = true;
					Widget handle = this.Handle;
					Vector2 mousePosition = base.EventManager.MousePosition;
					this._localClickPos = handle.AreaRect.TransformScreenPositionToLocal(mousePosition);
				}
				this.HandleMouseMove();
			}
			else
			{
				this._handleClicked = false;
			}
			this.UpdateScrollBar();
			this.UpdateHandleLength();
			this._firstFrame = false;
		}

		// Token: 0x060006EA RID: 1770 RVA: 0x0001DCAC File Offset: 0x0001BEAC
		protected internal override void OnMousePressed()
		{
			if (this.Handle != null)
			{
				base.IsPressed = true;
				Vector2 mousePosition = base.EventManager.MousePosition;
				this._localClickPos = this.Handle.AreaRect.TransformScreenPositionToLocal(mousePosition);
				if (this._localClickPos.X < -5f)
				{
					this._localClickPos.X = -5f;
				}
				else if (this._localClickPos.X > this.Handle.Size.X + 5f)
				{
					this._localClickPos.X = this.Handle.Size.X + 5f;
				}
				if (this._localClickPos.Y < -5f)
				{
					this._localClickPos.Y = -5f;
				}
				else if (this._localClickPos.Y > this.Handle.Size.Y + 5f)
				{
					this._localClickPos.Y = this.Handle.Size.Y + 5f;
				}
				this.HandleMouseMove();
			}
		}

		// Token: 0x060006EB RID: 1771 RVA: 0x0001DDC5 File Offset: 0x0001BFC5
		protected internal override void OnMouseReleased()
		{
			if (this.Handle != null)
			{
				base.IsPressed = false;
			}
		}

		// Token: 0x060006EC RID: 1772 RVA: 0x0001DDD6 File Offset: 0x0001BFD6
		public void SetValueForced(float value)
		{
			if (value > this.MaxValue)
			{
				this.MaxValue = value;
			}
			else if (value < this.MinValue)
			{
				this.MinValue = value;
			}
			this.ValueFloat = value;
		}

		// Token: 0x060006ED RID: 1773 RVA: 0x0001DE01 File Offset: 0x0001C001
		private void UpdateScrollBar()
		{
			if (!this._firstFrame)
			{
				this.UpdateHandleByValue();
			}
		}

		// Token: 0x060006EE RID: 1774 RVA: 0x0001DE11 File Offset: 0x0001C011
		private float GetValue(Vector2 value, AlignmentAxis alignmentAxis)
		{
			if (alignmentAxis == AlignmentAxis.Horizontal)
			{
				return value.X;
			}
			return value.Y;
		}

		// Token: 0x060006EF RID: 1775 RVA: 0x0001DE24 File Offset: 0x0001C024
		private void HandleMouseMove()
		{
			if (this.Handle != null)
			{
				Vector2 value = base.EventManager.MousePosition - this._localClickPos;
				float num = this.GetValue(value, this.AlignmentAxis);
				float num2;
				float num3;
				if (this.AlignmentAxis == AlignmentAxis.Horizontal)
				{
					float x = base.ParentWidget.GlobalPosition.X;
					num2 = x + base.Left;
					num3 = x + base.Right;
					num3 -= this.Handle.Size.X;
				}
				else
				{
					float y = base.ParentWidget.GlobalPosition.Y;
					num2 = y + base.Top;
					num3 = y + base.Bottom;
					num3 -= this.Handle.Size.Y;
				}
				if (Mathf.Abs(num3 - num2) < 1E-05f)
				{
					this.ValueFloat = 0f;
				}
				else
				{
					if (num < num2)
					{
						num = num2;
					}
					if (num > num3)
					{
						num = num3;
					}
					float num4 = (num - num2) / (num3 - num2);
					this.ValueFloat = this.MinValue + (this.MaxValue - this.MinValue) * num4;
				}
				this.UpdateHandleByValue();
			}
		}

		// Token: 0x060006F0 RID: 1776 RVA: 0x0001DF28 File Offset: 0x0001C128
		private void UpdateHandleLength()
		{
			if (!this.DoNotUpdateHandleSize && this.IsDiscrete && this.Handle.WidthSizePolicy == SizePolicy.Fixed)
			{
				if (this.AlignmentAxis == AlignmentAxis.Horizontal)
				{
					this.Handle.SuggestedWidth = Mathf.Clamp(base.SuggestedWidth / (this.MaxValue + 1f), 50f, base.SuggestedWidth / 2f);
					return;
				}
				if (this.AlignmentAxis == AlignmentAxis.Vertical)
				{
					this.Handle.SuggestedHeight = Mathf.Clamp(base.SuggestedHeight / (this.MaxValue + 1f), 50f, base.SuggestedHeight / 2f);
				}
			}
		}

		// Token: 0x060006F1 RID: 1777 RVA: 0x0001DFD4 File Offset: 0x0001C1D4
		private void UpdateHandleByValue()
		{
			if (this._valueFloat < this.MinValue)
			{
				this.ValueFloat = this.MinValue;
			}
			if (this._valueFloat > this.MaxValue)
			{
				this.ValueFloat = this.MaxValue;
			}
			float num = 0f;
			if (MathF.Abs(this.MaxValue - this.MinValue) > 1E-45f)
			{
				num = (this._valueFloat - this.MinValue) / (this.MaxValue - this.MinValue);
				if (this.ReverseDirection)
				{
					num = 1f - num;
				}
			}
			if (this.Handle != null)
			{
				if (this.AlignmentAxis == AlignmentAxis.Horizontal)
				{
					this.Handle.HorizontalAlignment = HorizontalAlignment.Left;
					this.Handle.VerticalAlignment = VerticalAlignment.Center;
					float num2 = this.ScrollbarArea.Size.X;
					num2 -= this.Handle.Size.X;
					this.Handle.ScaledPositionXOffset = num2 * num;
					this.Handle.ScaledPositionYOffset = 0f;
					return;
				}
				this.Handle.HorizontalAlignment = HorizontalAlignment.Center;
				this.Handle.VerticalAlignment = VerticalAlignment.Bottom;
				float num3 = this.ScrollbarArea.Size.Y;
				num3 -= this.Handle.Size.Y;
				this.Handle.ScaledPositionYOffset = -1f * num3 * (1f - num);
				this.Handle.ScaledPositionXOffset = 0f;
			}
		}

		// Token: 0x04000333 RID: 819
		private bool _locked;

		// Token: 0x04000334 RID: 820
		private bool _isDiscrete;

		// Token: 0x04000337 RID: 823
		private float _valueFloat;

		// Token: 0x0400033B RID: 827
		public float HandleRatio;

		// Token: 0x0400033C RID: 828
		private Widget _handle;

		// Token: 0x0400033E RID: 830
		private bool _firstFrame;

		// Token: 0x0400033F RID: 831
		private Vector2 _localClickPos;

		// Token: 0x04000340 RID: 832
		private bool _handleClicked;
	}
}
