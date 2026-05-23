using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar
{
	// Token: 0x0200005D RID: 93
	public class MapNavigationItemVM : ViewModel
	{
		// Token: 0x060006AA RID: 1706 RVA: 0x00021854 File Offset: 0x0001FA54
		public MapNavigationItemVM(INavigationElement navigationElement)
		{
			this.NavigationElement = navigationElement;
			this.Tooltip = new BasicTooltipViewModel(() => this.GetTooltip());
			this.AlertTooltip = new BasicTooltipViewModel(() => this.GetAlertTooltip());
			this.ItemId = this.NavigationElement.StringId;
			this.RefreshStates(true);
			this.RefreshValues();
		}

		// Token: 0x060006AB RID: 1707 RVA: 0x000218BC File Offset: 0x0001FABC
		private string GetTooltip()
		{
			NavigationPermissionItem permission = this.NavigationElement.Permission;
			if (permission.IsAuthorized || this.NavigationElement.IsActive)
			{
				TextObject tooltip = this.NavigationElement.Tooltip;
				if (tooltip == null)
				{
					return null;
				}
				return tooltip.ToString();
			}
			else
			{
				TextObject reasonString = permission.ReasonString;
				if (reasonString == null)
				{
					return null;
				}
				return reasonString.ToString();
			}
		}

		// Token: 0x060006AC RID: 1708 RVA: 0x00021914 File Offset: 0x0001FB14
		private string GetAlertTooltip()
		{
			TextObject alertTooltip = this.NavigationElement.AlertTooltip;
			if (alertTooltip == null)
			{
				return null;
			}
			return alertTooltip.ToString();
		}

		// Token: 0x060006AD RID: 1709 RVA: 0x0002192C File Offset: 0x0001FB2C
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.AlertText = GameTexts.FindText("str_map_bar_alert", null).ToString();
		}

		// Token: 0x060006AE RID: 1710 RVA: 0x0002194C File Offset: 0x0001FB4C
		public void RefreshStates(bool forceRefresh = false)
		{
			this.IsActive = this.NavigationElement.IsActive;
			this.HasAlert = this.NavigationElement.HasAlert;
			this.IsEnabled = this.NavigationElement.Permission.IsAuthorized;
		}

		// Token: 0x060006AF RID: 1711 RVA: 0x00021994 File Offset: 0x0001FB94
		public void ExecuteOpen()
		{
			this.NavigationElement.OpenView();
		}

		// Token: 0x060006B0 RID: 1712 RVA: 0x000219A1 File Offset: 0x0001FBA1
		public void ExecuteGoToLink()
		{
			this.NavigationElement.GoToLink();
		}

		// Token: 0x170001CB RID: 459
		// (get) Token: 0x060006B1 RID: 1713 RVA: 0x000219AE File Offset: 0x0001FBAE
		// (set) Token: 0x060006B2 RID: 1714 RVA: 0x000219B6 File Offset: 0x0001FBB6
		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				return this._isEnabled;
			}
			set
			{
				if (value != this._isEnabled)
				{
					this._isEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsEnabled");
				}
			}
		}

		// Token: 0x170001CC RID: 460
		// (get) Token: 0x060006B3 RID: 1715 RVA: 0x000219D4 File Offset: 0x0001FBD4
		// (set) Token: 0x060006B4 RID: 1716 RVA: 0x000219DC File Offset: 0x0001FBDC
		[DataSourceProperty]
		public bool IsActive
		{
			get
			{
				return this._isActive;
			}
			set
			{
				if (value != this._isActive)
				{
					this._isActive = value;
					base.OnPropertyChangedWithValue(value, "IsActive");
				}
			}
		}

		// Token: 0x170001CD RID: 461
		// (get) Token: 0x060006B5 RID: 1717 RVA: 0x000219FA File Offset: 0x0001FBFA
		// (set) Token: 0x060006B6 RID: 1718 RVA: 0x00021A02 File Offset: 0x0001FC02
		[DataSourceProperty]
		public bool HasAlert
		{
			get
			{
				return this._hasAlert;
			}
			set
			{
				if (value != this._hasAlert)
				{
					this._hasAlert = value;
					base.OnPropertyChangedWithValue(value, "HasAlert");
				}
			}
		}

		// Token: 0x170001CE RID: 462
		// (get) Token: 0x060006B7 RID: 1719 RVA: 0x00021A20 File Offset: 0x0001FC20
		// (set) Token: 0x060006B8 RID: 1720 RVA: 0x00021A28 File Offset: 0x0001FC28
		[DataSourceProperty]
		public string ItemId
		{
			get
			{
				return this._itemId;
			}
			set
			{
				if (value != this._itemId)
				{
					this._itemId = value;
					base.OnPropertyChangedWithValue<string>(value, "ItemId");
				}
			}
		}

		// Token: 0x170001CF RID: 463
		// (get) Token: 0x060006B9 RID: 1721 RVA: 0x00021A4B File Offset: 0x0001FC4B
		// (set) Token: 0x060006BA RID: 1722 RVA: 0x00021A53 File Offset: 0x0001FC53
		[DataSourceProperty]
		public string AlertText
		{
			get
			{
				return this._alertText;
			}
			set
			{
				if (value != this._alertText)
				{
					this._alertText = value;
					base.OnPropertyChangedWithValue<string>(value, "AlertText");
				}
			}
		}

		// Token: 0x170001D0 RID: 464
		// (get) Token: 0x060006BB RID: 1723 RVA: 0x00021A76 File Offset: 0x0001FC76
		// (set) Token: 0x060006BC RID: 1724 RVA: 0x00021A7E File Offset: 0x0001FC7E
		[DataSourceProperty]
		public BasicTooltipViewModel Tooltip
		{
			get
			{
				return this._tooltip;
			}
			set
			{
				if (value != this._tooltip)
				{
					this._tooltip = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "Tooltip");
				}
			}
		}

		// Token: 0x170001D1 RID: 465
		// (get) Token: 0x060006BD RID: 1725 RVA: 0x00021A9C File Offset: 0x0001FC9C
		// (set) Token: 0x060006BE RID: 1726 RVA: 0x00021AA4 File Offset: 0x0001FCA4
		[DataSourceProperty]
		public BasicTooltipViewModel AlertTooltip
		{
			get
			{
				return this._alertTooltip;
			}
			set
			{
				if (value != this._alertTooltip)
				{
					this._alertTooltip = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "AlertTooltip");
				}
			}
		}

		// Token: 0x040002E4 RID: 740
		public readonly INavigationElement NavigationElement;

		// Token: 0x040002E5 RID: 741
		private bool _isEnabled;

		// Token: 0x040002E6 RID: 742
		private bool _isActive;

		// Token: 0x040002E7 RID: 743
		private bool _hasAlert;

		// Token: 0x040002E8 RID: 744
		private string _itemId;

		// Token: 0x040002E9 RID: 745
		private string _alertText;

		// Token: 0x040002EA RID: 746
		private BasicTooltipViewModel _tooltip;

		// Token: 0x040002EB RID: 747
		private BasicTooltipViewModel _alertTooltip;
	}
}
