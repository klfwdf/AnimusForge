using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Inventory
{
	// Token: 0x02000092 RID: 146
	public class ItemPreviewVM : ViewModel
	{
		// Token: 0x06000CB7 RID: 3255 RVA: 0x00035D75 File Offset: 0x00033F75
		public ItemPreviewVM(Action onClosed)
		{
			this._onClosed = onClosed;
			this.ItemTableau = new ItemCollectionElementViewModel();
			this.RefreshValues();
		}

		// Token: 0x06000CB8 RID: 3256 RVA: 0x00035D95 File Offset: 0x00033F95
		public override void OnFinalize()
		{
			this.ItemTableau.OnFinalize();
			this.ItemTableau = null;
			base.OnFinalize();
		}

		// Token: 0x06000CB9 RID: 3257 RVA: 0x00035DAF File Offset: 0x00033FAF
		public void Open(EquipmentElement item)
		{
			this.ItemTableau.FillFrom(item, Clan.PlayerClan.Banner);
			this.ItemName = item.Item.Name.ToString();
			this.IsSelected = true;
		}

		// Token: 0x06000CBA RID: 3258 RVA: 0x00035DE5 File Offset: 0x00033FE5
		public void ExecuteClose()
		{
			this.Close();
		}

		// Token: 0x06000CBB RID: 3259 RVA: 0x00035DED File Offset: 0x00033FED
		public void Close()
		{
			this._onClosed();
			this.IsSelected = false;
		}

		// Token: 0x1700040E RID: 1038
		// (get) Token: 0x06000CBC RID: 3260 RVA: 0x00035E01 File Offset: 0x00034001
		// (set) Token: 0x06000CBD RID: 3261 RVA: 0x00035E09 File Offset: 0x00034009
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (value != this._isSelected)
				{
					this._isSelected = value;
					base.OnPropertyChanged("IsSelected");
				}
			}
		}

		// Token: 0x1700040F RID: 1039
		// (get) Token: 0x06000CBE RID: 3262 RVA: 0x00035E26 File Offset: 0x00034026
		// (set) Token: 0x06000CBF RID: 3263 RVA: 0x00035E2E File Offset: 0x0003402E
		[DataSourceProperty]
		public string ItemName
		{
			get
			{
				return this._itemName;
			}
			set
			{
				if (value != this._itemName)
				{
					this._itemName = value;
					base.OnPropertyChanged("ItemName");
				}
			}
		}

		// Token: 0x17000410 RID: 1040
		// (get) Token: 0x06000CC0 RID: 3264 RVA: 0x00035E50 File Offset: 0x00034050
		// (set) Token: 0x06000CC1 RID: 3265 RVA: 0x00035E58 File Offset: 0x00034058
		[DataSourceProperty]
		public ItemCollectionElementViewModel ItemTableau
		{
			get
			{
				return this._itemTableau;
			}
			set
			{
				if (value != this._itemTableau)
				{
					this._itemTableau = value;
					base.OnPropertyChangedWithValue<ItemCollectionElementViewModel>(value, "ItemTableau");
				}
			}
		}

		// Token: 0x040005CA RID: 1482
		private Action _onClosed;

		// Token: 0x040005CB RID: 1483
		private bool _isSelected;

		// Token: 0x040005CC RID: 1484
		private string _itemName;

		// Token: 0x040005CD RID: 1485
		private ItemCollectionElementViewModel _itemTableau;
	}
}
