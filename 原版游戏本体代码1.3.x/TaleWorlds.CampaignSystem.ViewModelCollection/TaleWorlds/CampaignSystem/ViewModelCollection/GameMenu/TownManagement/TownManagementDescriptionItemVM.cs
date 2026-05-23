using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement
{
	// Token: 0x020000A8 RID: 168
	public class TownManagementDescriptionItemVM : ViewModel
	{
		// Token: 0x06001022 RID: 4130 RVA: 0x00041E48 File Offset: 0x00040048
		public TownManagementDescriptionItemVM(TextObject title, int value, int valueChange, TownManagementDescriptionItemVM.DescriptionType type, BasicTooltipViewModel hint = null)
		{
			this._titleObj = title;
			this.Value = value;
			this.ValueChange = valueChange;
			this.Type = (int)type;
			this.Hint = hint ?? new BasicTooltipViewModel();
			this.RefreshValues();
		}

		// Token: 0x06001023 RID: 4131 RVA: 0x00041E96 File Offset: 0x00040096
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Title = this._titleObj.ToString();
			this.RefreshIsWarning();
		}

		// Token: 0x06001024 RID: 4132 RVA: 0x00041EB8 File Offset: 0x000400B8
		private void RefreshIsWarning()
		{
			int type = this.Type;
			if (type == 1)
			{
				this.IsWarning = this.Value < 1;
				return;
			}
			if (type == 5)
			{
				this.IsWarning = this.Value < Campaign.Current.Models.SettlementLoyaltyModel.RebelliousStateStartLoyaltyThreshold;
				return;
			}
			if (type != 7)
			{
				this.IsWarning = false;
				return;
			}
			this.IsWarning = this.Value < 1;
		}

		// Token: 0x17000536 RID: 1334
		// (get) Token: 0x06001025 RID: 4133 RVA: 0x00041F24 File Offset: 0x00040124
		// (set) Token: 0x06001026 RID: 4134 RVA: 0x00041F2C File Offset: 0x0004012C
		[DataSourceProperty]
		public int Type
		{
			get
			{
				return this._type;
			}
			set
			{
				if (value != this._type)
				{
					this._type = value;
					base.OnPropertyChangedWithValue(value, "Type");
				}
			}
		}

		// Token: 0x17000537 RID: 1335
		// (get) Token: 0x06001027 RID: 4135 RVA: 0x00041F4A File Offset: 0x0004014A
		// (set) Token: 0x06001028 RID: 4136 RVA: 0x00041F52 File Offset: 0x00040152
		[DataSourceProperty]
		public string Title
		{
			get
			{
				return this._title;
			}
			set
			{
				if (value != this._title)
				{
					this._title = value;
					base.OnPropertyChangedWithValue<string>(value, "Title");
				}
			}
		}

		// Token: 0x17000538 RID: 1336
		// (get) Token: 0x06001029 RID: 4137 RVA: 0x00041F75 File Offset: 0x00040175
		// (set) Token: 0x0600102A RID: 4138 RVA: 0x00041F7D File Offset: 0x0004017D
		[DataSourceProperty]
		public int Value
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
					base.OnPropertyChangedWithValue(value, "Value");
				}
			}
		}

		// Token: 0x17000539 RID: 1337
		// (get) Token: 0x0600102B RID: 4139 RVA: 0x00041F9B File Offset: 0x0004019B
		// (set) Token: 0x0600102C RID: 4140 RVA: 0x00041FA3 File Offset: 0x000401A3
		[DataSourceProperty]
		public int ValueChange
		{
			get
			{
				return this._valueChange;
			}
			set
			{
				if (value != this._valueChange)
				{
					this._valueChange = value;
					base.OnPropertyChangedWithValue(value, "ValueChange");
				}
			}
		}

		// Token: 0x1700053A RID: 1338
		// (get) Token: 0x0600102D RID: 4141 RVA: 0x00041FC1 File Offset: 0x000401C1
		// (set) Token: 0x0600102E RID: 4142 RVA: 0x00041FC9 File Offset: 0x000401C9
		[DataSourceProperty]
		public BasicTooltipViewModel Hint
		{
			get
			{
				return this._hint;
			}
			set
			{
				if (value != this._hint && value != null)
				{
					this._hint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "Hint");
				}
			}
		}

		// Token: 0x1700053B RID: 1339
		// (get) Token: 0x0600102F RID: 4143 RVA: 0x00041FEA File Offset: 0x000401EA
		// (set) Token: 0x06001030 RID: 4144 RVA: 0x00041FF2 File Offset: 0x000401F2
		[DataSourceProperty]
		public bool IsWarning
		{
			get
			{
				return this._isWarning;
			}
			set
			{
				if (value != this._isWarning)
				{
					this._isWarning = value;
					base.OnPropertyChangedWithValue(value, "IsWarning");
				}
			}
		}

		// Token: 0x0400075E RID: 1886
		private readonly TextObject _titleObj;

		// Token: 0x0400075F RID: 1887
		private int _type = -1;

		// Token: 0x04000760 RID: 1888
		private string _title;

		// Token: 0x04000761 RID: 1889
		private int _value;

		// Token: 0x04000762 RID: 1890
		private int _valueChange;

		// Token: 0x04000763 RID: 1891
		private BasicTooltipViewModel _hint;

		// Token: 0x04000764 RID: 1892
		private bool _isWarning;

		// Token: 0x02000219 RID: 537
		public enum DescriptionType
		{
			// Token: 0x040011B5 RID: 4533
			Gold,
			// Token: 0x040011B6 RID: 4534
			Production,
			// Token: 0x040011B7 RID: 4535
			Militia,
			// Token: 0x040011B8 RID: 4536
			Prosperity,
			// Token: 0x040011B9 RID: 4537
			Food,
			// Token: 0x040011BA RID: 4538
			Loyalty,
			// Token: 0x040011BB RID: 4539
			Security,
			// Token: 0x040011BC RID: 4540
			Garrison
		}
	}
}
