using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting
{
	// Token: 0x020000FC RID: 252
	public class CraftingResourceItemVM : ViewModel
	{
		// Token: 0x1700077F RID: 1919
		// (get) Token: 0x0600168D RID: 5773 RVA: 0x0005729B File Offset: 0x0005549B
		// (set) Token: 0x0600168E RID: 5774 RVA: 0x000572A3 File Offset: 0x000554A3
		public ItemObject ResourceItem { get; private set; }

		// Token: 0x17000780 RID: 1920
		// (get) Token: 0x0600168F RID: 5775 RVA: 0x000572AC File Offset: 0x000554AC
		// (set) Token: 0x06001690 RID: 5776 RVA: 0x000572B4 File Offset: 0x000554B4
		public CraftingMaterials ResourceMaterial { get; private set; }

		// Token: 0x06001691 RID: 5777 RVA: 0x000572C0 File Offset: 0x000554C0
		public CraftingResourceItemVM(CraftingMaterials material, int amount, int changeAmount = 0)
		{
			this.ResourceMaterial = material;
			Campaign campaign = Campaign.Current;
			ItemObject resourceItem;
			if (campaign == null)
			{
				resourceItem = null;
			}
			else
			{
				GameModels models = campaign.Models;
				if (models == null)
				{
					resourceItem = null;
				}
				else
				{
					SmithingModel smithingModel = models.SmithingModel;
					resourceItem = ((smithingModel != null) ? smithingModel.GetCraftingMaterialItem(material) : null);
				}
			}
			this.ResourceItem = resourceItem;
			ItemObject resourceItem2 = this.ResourceItem;
			string text;
			if (resourceItem2 == null)
			{
				text = null;
			}
			else
			{
				TextObject name = resourceItem2.Name;
				text = ((name != null) ? name.ToString() : null);
			}
			this.ResourceName = text ?? "none";
			this.ResourceHint = new HintViewModel(new TextObject("{=!}" + this.ResourceName, null), null);
			this.ResourceAmount = amount;
			ItemObject resourceItem3 = this.ResourceItem;
			this.ResourceItemStringId = ((resourceItem3 != null) ? resourceItem3.StringId : null) ?? "none";
			this.ResourceMaterialTypeAsStr = this.ResourceMaterial.ToString();
			this.ResourceChangeAmount = changeAmount;
		}

		// Token: 0x17000781 RID: 1921
		// (get) Token: 0x06001692 RID: 5778 RVA: 0x000573A9 File Offset: 0x000555A9
		// (set) Token: 0x06001693 RID: 5779 RVA: 0x000573B1 File Offset: 0x000555B1
		[DataSourceProperty]
		public string ResourceName
		{
			get
			{
				return this._resourceName;
			}
			set
			{
				if (value != this._resourceName)
				{
					this._resourceName = value;
					base.OnPropertyChangedWithValue<string>(value, "ResourceName");
				}
			}
		}

		// Token: 0x17000782 RID: 1922
		// (get) Token: 0x06001694 RID: 5780 RVA: 0x000573D4 File Offset: 0x000555D4
		// (set) Token: 0x06001695 RID: 5781 RVA: 0x000573DC File Offset: 0x000555DC
		[DataSourceProperty]
		public HintViewModel ResourceHint
		{
			get
			{
				return this._resourceHint;
			}
			set
			{
				if (value != this._resourceHint)
				{
					this._resourceHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ResourceHint");
				}
			}
		}

		// Token: 0x17000783 RID: 1923
		// (get) Token: 0x06001696 RID: 5782 RVA: 0x000573FA File Offset: 0x000555FA
		// (set) Token: 0x06001697 RID: 5783 RVA: 0x00057402 File Offset: 0x00055602
		[DataSourceProperty]
		public string ResourceMaterialTypeAsStr
		{
			get
			{
				return this._resourceMaterialTypeAsStr;
			}
			set
			{
				if (value != this._resourceMaterialTypeAsStr)
				{
					this._resourceMaterialTypeAsStr = value;
					base.OnPropertyChangedWithValue<string>(value, "ResourceMaterialTypeAsStr");
				}
			}
		}

		// Token: 0x17000784 RID: 1924
		// (get) Token: 0x06001698 RID: 5784 RVA: 0x00057425 File Offset: 0x00055625
		// (set) Token: 0x06001699 RID: 5785 RVA: 0x0005742D File Offset: 0x0005562D
		[DataSourceProperty]
		public int ResourceAmount
		{
			get
			{
				return this._resourceUsageAmount;
			}
			set
			{
				if (value != this._resourceUsageAmount)
				{
					this._resourceUsageAmount = value;
					base.OnPropertyChangedWithValue(value, "ResourceAmount");
				}
			}
		}

		// Token: 0x17000785 RID: 1925
		// (get) Token: 0x0600169A RID: 5786 RVA: 0x0005744B File Offset: 0x0005564B
		// (set) Token: 0x0600169B RID: 5787 RVA: 0x00057453 File Offset: 0x00055653
		[DataSourceProperty]
		public int ResourceChangeAmount
		{
			get
			{
				return this._resourceChangeAmount;
			}
			set
			{
				if (value != this._resourceChangeAmount)
				{
					this._resourceChangeAmount = value;
					base.OnPropertyChangedWithValue(value, "ResourceChangeAmount");
				}
			}
		}

		// Token: 0x17000786 RID: 1926
		// (get) Token: 0x0600169C RID: 5788 RVA: 0x00057471 File Offset: 0x00055671
		// (set) Token: 0x0600169D RID: 5789 RVA: 0x00057479 File Offset: 0x00055679
		[DataSourceProperty]
		public string ResourceItemStringId
		{
			get
			{
				return this._resourceItemStringId;
			}
			set
			{
				if (value != this._resourceItemStringId)
				{
					this._resourceItemStringId = value;
					base.OnPropertyChangedWithValue<string>(value, "ResourceItemStringId");
				}
			}
		}

		// Token: 0x17000787 RID: 1927
		// (get) Token: 0x0600169E RID: 5790 RVA: 0x0005749C File Offset: 0x0005569C
		// (set) Token: 0x0600169F RID: 5791 RVA: 0x000574A4 File Offset: 0x000556A4
		[DataSourceProperty]
		public bool IsResourceAvailable
		{
			get
			{
				return this._isResourceAvailable;
			}
			set
			{
				if (value != this._isResourceAvailable)
				{
					this._isResourceAvailable = value;
					base.OnPropertyChangedWithValue(value, "IsResourceAvailable");
				}
			}
		}

		// Token: 0x04000A55 RID: 2645
		private string _resourceName;

		// Token: 0x04000A56 RID: 2646
		private string _resourceItemStringId;

		// Token: 0x04000A57 RID: 2647
		private int _resourceUsageAmount;

		// Token: 0x04000A58 RID: 2648
		private int _resourceChangeAmount;

		// Token: 0x04000A59 RID: 2649
		private string _resourceMaterialTypeAsStr;

		// Token: 0x04000A5A RID: 2650
		private HintViewModel _resourceHint;

		// Token: 0x04000A5B RID: 2651
		private bool _isResourceAvailable = true;
	}
}
