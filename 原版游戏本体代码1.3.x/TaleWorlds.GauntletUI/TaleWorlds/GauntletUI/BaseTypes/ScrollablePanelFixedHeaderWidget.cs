using System;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x02000064 RID: 100
	public class ScrollablePanelFixedHeaderWidget : Widget
	{
		// Token: 0x170001E3 RID: 483
		// (get) Token: 0x060006C2 RID: 1730 RVA: 0x0001D932 File Offset: 0x0001BB32
		// (set) Token: 0x060006C3 RID: 1731 RVA: 0x0001D93A File Offset: 0x0001BB3A
		public Widget FixedHeader { get; set; }

		// Token: 0x170001E4 RID: 484
		// (get) Token: 0x060006C4 RID: 1732 RVA: 0x0001D943 File Offset: 0x0001BB43
		// (set) Token: 0x060006C5 RID: 1733 RVA: 0x0001D94B File Offset: 0x0001BB4B
		public float TopOffset { get; set; }

		// Token: 0x170001E5 RID: 485
		// (get) Token: 0x060006C6 RID: 1734 RVA: 0x0001D954 File Offset: 0x0001BB54
		// (set) Token: 0x060006C7 RID: 1735 RVA: 0x0001D95C File Offset: 0x0001BB5C
		public float BottomOffset { get; set; } = float.MinValue;

		// Token: 0x060006C8 RID: 1736 RVA: 0x0001D965 File Offset: 0x0001BB65
		public ScrollablePanelFixedHeaderWidget(UIContext context)
			: base(context)
		{
		}

		// Token: 0x060006C9 RID: 1737 RVA: 0x0001D980 File Offset: 0x0001BB80
		protected override void OnLateUpdate(float dt)
		{
			base.OnLateUpdate(dt);
			if (this._isDirty)
			{
				base.EventFired("FixedHeaderPropertyChanged", Array.Empty<object>());
				this._isDirty = false;
			}
		}

		// Token: 0x170001E6 RID: 486
		// (get) Token: 0x060006CA RID: 1738 RVA: 0x0001D9A8 File Offset: 0x0001BBA8
		// (set) Token: 0x060006CB RID: 1739 RVA: 0x0001D9B0 File Offset: 0x0001BBB0
		public float HeaderHeight
		{
			get
			{
				return this._headerHeight;
			}
			set
			{
				if (value != this._headerHeight)
				{
					this._headerHeight = value;
					base.SuggestedHeight = this._headerHeight;
					this._isDirty = true;
				}
			}
		}

		// Token: 0x170001E7 RID: 487
		// (get) Token: 0x060006CC RID: 1740 RVA: 0x0001D9D5 File Offset: 0x0001BBD5
		// (set) Token: 0x060006CD RID: 1741 RVA: 0x0001D9DD File Offset: 0x0001BBDD
		public float AdditionalTopOffset
		{
			get
			{
				return this._additionalTopOffset;
			}
			set
			{
				if (value != this._additionalTopOffset)
				{
					this._additionalTopOffset = value;
					this._isDirty = true;
				}
			}
		}

		// Token: 0x170001E8 RID: 488
		// (get) Token: 0x060006CE RID: 1742 RVA: 0x0001D9F6 File Offset: 0x0001BBF6
		// (set) Token: 0x060006CF RID: 1743 RVA: 0x0001D9FE File Offset: 0x0001BBFE
		public float AdditionalBottomOffset
		{
			get
			{
				return this._additionalBottomOffset;
			}
			set
			{
				if (value != this._additionalBottomOffset)
				{
					this._additionalBottomOffset = value;
					this._isDirty = true;
				}
			}
		}

		// Token: 0x170001E9 RID: 489
		// (get) Token: 0x060006D0 RID: 1744 RVA: 0x0001DA17 File Offset: 0x0001BC17
		// (set) Token: 0x060006D1 RID: 1745 RVA: 0x0001DA1F File Offset: 0x0001BC1F
		[Editor(false)]
		public bool IsRelevant
		{
			get
			{
				return this._isRelevant;
			}
			set
			{
				if (value != this._isRelevant)
				{
					this._isRelevant = value;
					base.IsVisible = value;
					this._isDirty = true;
					base.OnPropertyChanged(value, "IsRelevant");
				}
			}
		}

		// Token: 0x0400032B RID: 811
		private bool _isDirty;

		// Token: 0x0400032F RID: 815
		private float _headerHeight;

		// Token: 0x04000330 RID: 816
		private float _additionalTopOffset;

		// Token: 0x04000331 RID: 817
		private float _additionalBottomOffset;

		// Token: 0x04000332 RID: 818
		private bool _isRelevant = true;
	}
}
