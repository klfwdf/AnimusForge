using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement
{
	// Token: 0x020000A3 RID: 163
	public class SettlementDailyProjectVM : SettlementProjectVM
	{
		// Token: 0x06000FC4 RID: 4036 RVA: 0x00040E1F File Offset: 0x0003F01F
		public SettlementDailyProjectVM(Action<SettlementProjectVM, bool> onSelection, Action<SettlementProjectVM> onSetAsCurrent, Action onResetCurrent, Building building, Settlement settlement)
			: base(onSelection, onSetAsCurrent, onResetCurrent, building, settlement)
		{
			base.IsDaily = true;
			this.RefreshValues();
		}

		// Token: 0x06000FC5 RID: 4037 RVA: 0x00040E3B File Offset: 0x0003F03B
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.DefaultText = GameTexts.FindText("str_default", null).ToString();
		}

		// Token: 0x06000FC6 RID: 4038 RVA: 0x00040E59 File Offset: 0x0003F059
		public override void RefreshProductionText()
		{
			base.RefreshProductionText();
			base.ProductionText = new TextObject("{=bd7oAQq6}Daily", null).ToString();
		}

		// Token: 0x06000FC7 RID: 4039 RVA: 0x00040E77 File Offset: 0x0003F077
		public override void ExecuteAddToQueue()
		{
		}

		// Token: 0x06000FC8 RID: 4040 RVA: 0x00040E79 File Offset: 0x0003F079
		public override void ExecuteSetAsActiveDevelopment()
		{
			this._onSelection(this, false);
		}

		// Token: 0x06000FC9 RID: 4041 RVA: 0x00040E88 File Offset: 0x0003F088
		public override void ExecuteSetAsCurrent()
		{
			Action<SettlementProjectVM> onSetAsCurrent = this._onSetAsCurrent;
			if (onSetAsCurrent == null)
			{
				return;
			}
			onSetAsCurrent(this);
		}

		// Token: 0x06000FCA RID: 4042 RVA: 0x00040E9B File Offset: 0x0003F09B
		public override void ExecuteResetCurrent()
		{
			Action onResetCurrent = this._onResetCurrent;
			if (onResetCurrent == null)
			{
				return;
			}
			onResetCurrent();
		}

		// Token: 0x06000FCB RID: 4043 RVA: 0x00040EAD File Offset: 0x0003F0AD
		public override void ExecuteToggleSelected()
		{
		}

		// Token: 0x17000519 RID: 1305
		// (get) Token: 0x06000FCC RID: 4044 RVA: 0x00040EAF File Offset: 0x0003F0AF
		// (set) Token: 0x06000FCD RID: 4045 RVA: 0x00040EB7 File Offset: 0x0003F0B7
		[DataSourceProperty]
		public bool IsDefault
		{
			get
			{
				return this._isDefault;
			}
			set
			{
				if (value != this._isDefault)
				{
					this._isDefault = value;
					base.OnPropertyChangedWithValue(value, "IsDefault");
				}
			}
		}

		// Token: 0x1700051A RID: 1306
		// (get) Token: 0x06000FCE RID: 4046 RVA: 0x00040ED5 File Offset: 0x0003F0D5
		// (set) Token: 0x06000FCF RID: 4047 RVA: 0x00040EDD File Offset: 0x0003F0DD
		[DataSourceProperty]
		public string DefaultText
		{
			get
			{
				return this._defaultText;
			}
			set
			{
				if (value != this._defaultText)
				{
					this._defaultText = value;
					base.OnPropertyChangedWithValue<string>(value, "DefaultText");
				}
			}
		}

		// Token: 0x04000733 RID: 1843
		private bool _isDefault;

		// Token: 0x04000734 RID: 1844
		private string _defaultText;
	}
}
