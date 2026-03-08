using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu
{
	// Token: 0x0200009D RID: 157
	public class GameMenuPlunderItemVM : ViewModel
	{
		// Token: 0x06000F2C RID: 3884 RVA: 0x0003EA73 File Offset: 0x0003CC73
		public GameMenuPlunderItemVM(EquipmentElement item, int amount = 1)
		{
			this.Item = item;
			this.Amount = amount;
			this.Visual = new ItemImageIdentifierVM(item.Item, "");
		}

		// Token: 0x06000F2D RID: 3885 RVA: 0x0003EAA0 File Offset: 0x0003CCA0
		public void ExecuteBeginTooltip()
		{
			if (this.Item.Item != null)
			{
				InformationManager.ShowTooltip(typeof(ItemObject), new object[] { this.Item });
			}
		}

		// Token: 0x06000F2E RID: 3886 RVA: 0x0003EAE0 File Offset: 0x0003CCE0
		public void ExecuteEndTooltip()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x170004E7 RID: 1255
		// (get) Token: 0x06000F2F RID: 3887 RVA: 0x0003EAE7 File Offset: 0x0003CCE7
		// (set) Token: 0x06000F30 RID: 3888 RVA: 0x0003EAEF File Offset: 0x0003CCEF
		[DataSourceProperty]
		public ItemImageIdentifierVM Visual
		{
			get
			{
				return this._visual;
			}
			set
			{
				if (value != this._visual)
				{
					this._visual = value;
					base.OnPropertyChangedWithValue<ItemImageIdentifierVM>(value, "Visual");
				}
			}
		}

		// Token: 0x170004E8 RID: 1256
		// (get) Token: 0x06000F31 RID: 3889 RVA: 0x0003EB0D File Offset: 0x0003CD0D
		// (set) Token: 0x06000F32 RID: 3890 RVA: 0x0003EB15 File Offset: 0x0003CD15
		[DataSourceProperty]
		public int Amount
		{
			get
			{
				return this._amount;
			}
			set
			{
				if (value != this._amount)
				{
					this._amount = value;
					base.OnPropertyChangedWithValue(value, "Amount");
				}
			}
		}

		// Token: 0x040006DE RID: 1758
		public readonly EquipmentElement Item;

		// Token: 0x040006DF RID: 1759
		private ItemImageIdentifierVM _visual;

		// Token: 0x040006E0 RID: 1760
		private int _amount;
	}
}
