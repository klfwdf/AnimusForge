using System;
using System.Collections.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Library.Information;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar
{
	// Token: 0x0200005B RID: 91
	public class MapInfoItemVM : ViewModel
	{
		// Token: 0x0600068B RID: 1675 RVA: 0x00020E31 File Offset: 0x0001F031
		public MapInfoItemVM(string itemId, Func<List<TooltipProperty>> getTooltip)
		{
			this.ItemId = itemId;
			this.VisualId = itemId;
			this._tooltip = new BasicTooltipViewModel(getTooltip);
			this.FloatValue = -1f;
		}

		// Token: 0x0600068C RID: 1676 RVA: 0x00020E5E File Offset: 0x0001F05E
		public MapInfoItemVM(string itemId, TooltipTriggerVM tooltipTrigger)
		{
			this.ItemId = itemId;
			this.VisualId = itemId;
			this._tooltipTrigger = tooltipTrigger;
			this.FloatValue = -1f;
		}

		// Token: 0x0600068D RID: 1677 RVA: 0x00020E86 File Offset: 0x0001F086
		public void ExecuteBeginHint()
		{
			if (this._tooltip != null)
			{
				this._tooltip.ExecuteBeginHint();
				return;
			}
			if (this._tooltipTrigger != null)
			{
				this._tooltipTrigger.ExecuteBeginHint();
			}
		}

		// Token: 0x0600068E RID: 1678 RVA: 0x00020EAF File Offset: 0x0001F0AF
		public void ExecuteEndHint()
		{
			if (this._tooltip != null)
			{
				this._tooltip.ExecuteEndHint();
				return;
			}
			if (this._tooltipTrigger != null)
			{
				this._tooltipTrigger.ExecuteEndHint();
			}
		}

		// Token: 0x0600068F RID: 1679 RVA: 0x00020ED8 File Offset: 0x0001F0D8
		public void SetOverriddenVisualId(string visualId)
		{
			this.VisualId = (string.IsNullOrEmpty(visualId) ? this.ItemId : visualId);
		}

		// Token: 0x170001C1 RID: 449
		// (get) Token: 0x06000690 RID: 1680 RVA: 0x00020EF1 File Offset: 0x0001F0F1
		// (set) Token: 0x06000691 RID: 1681 RVA: 0x00020EF9 File Offset: 0x0001F0F9
		[DataSourceProperty]
		public bool HasWarning
		{
			get
			{
				return this._hasWarning;
			}
			set
			{
				if (value != this._hasWarning)
				{
					this._hasWarning = value;
					base.OnPropertyChangedWithValue(value, "HasWarning");
				}
			}
		}

		// Token: 0x170001C2 RID: 450
		// (get) Token: 0x06000692 RID: 1682 RVA: 0x00020F17 File Offset: 0x0001F117
		// (set) Token: 0x06000693 RID: 1683 RVA: 0x00020F1F File Offset: 0x0001F11F
		[DataSourceProperty]
		public int IntValue
		{
			get
			{
				return this._intValue;
			}
			set
			{
				if (value != this._intValue)
				{
					this._intValue = value;
					base.OnPropertyChangedWithValue(value, "IntValue");
					this.FloatValue = (float)value;
				}
			}
		}

		// Token: 0x170001C3 RID: 451
		// (get) Token: 0x06000694 RID: 1684 RVA: 0x00020F45 File Offset: 0x0001F145
		// (set) Token: 0x06000695 RID: 1685 RVA: 0x00020F4D File Offset: 0x0001F14D
		[DataSourceProperty]
		public float FloatValue
		{
			get
			{
				return this._floatValue;
			}
			set
			{
				if (value != this._floatValue)
				{
					this._floatValue = value;
					base.OnPropertyChangedWithValue(value, "FloatValue");
					this.IntValue = (int)value;
				}
			}
		}

		// Token: 0x170001C4 RID: 452
		// (get) Token: 0x06000696 RID: 1686 RVA: 0x00020F73 File Offset: 0x0001F173
		// (set) Token: 0x06000697 RID: 1687 RVA: 0x00020F7B File Offset: 0x0001F17B
		[DataSourceProperty]
		public string VisualId
		{
			get
			{
				return this._visualId;
			}
			set
			{
				if (value != this._visualId)
				{
					this._visualId = value;
					base.OnPropertyChangedWithValue<string>(value, "VisualId");
				}
			}
		}

		// Token: 0x170001C5 RID: 453
		// (get) Token: 0x06000698 RID: 1688 RVA: 0x00020F9E File Offset: 0x0001F19E
		// (set) Token: 0x06000699 RID: 1689 RVA: 0x00020FA6 File Offset: 0x0001F1A6
		[DataSourceProperty]
		public string Value
		{
			get
			{
				return this._value;
			}
			set
			{
				if (value != this._value)
				{
					this._value = value;
					base.OnPropertyChangedWithValue<string>(value, "Value");
				}
			}
		}

		// Token: 0x040002CB RID: 715
		public readonly string ItemId;

		// Token: 0x040002CC RID: 716
		private readonly BasicTooltipViewModel _tooltip;

		// Token: 0x040002CD RID: 717
		private readonly TooltipTriggerVM _tooltipTrigger;

		// Token: 0x040002CE RID: 718
		private bool _hasWarning;

		// Token: 0x040002CF RID: 719
		private int _intValue;

		// Token: 0x040002D0 RID: 720
		private float _floatValue;

		// Token: 0x040002D1 RID: 721
		private string _visualId;

		// Token: 0x040002D2 RID: 722
		private string _value;
	}
}
