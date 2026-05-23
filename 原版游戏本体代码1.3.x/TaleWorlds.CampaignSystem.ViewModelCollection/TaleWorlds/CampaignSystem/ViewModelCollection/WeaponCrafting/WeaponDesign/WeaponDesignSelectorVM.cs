using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign
{
	// Token: 0x0200010D RID: 269
	public class WeaponDesignSelectorVM : ViewModel
	{
		// Token: 0x170007FF RID: 2047
		// (get) Token: 0x060017E3 RID: 6115 RVA: 0x0005AB13 File Offset: 0x00058D13
		public WeaponDesign Design { get; }

		// Token: 0x060017E4 RID: 6116 RVA: 0x0005AB1C File Offset: 0x00058D1C
		public WeaponDesignSelectorVM(WeaponDesign design, Action<WeaponDesignSelectorVM> onSelection)
		{
			this._onSelection = onSelection;
			this.Design = design;
			TextObject textObject = new TextObject("{=uZhHh7pm}Crafted {CURR_TEMPLATE_NAME}", null);
			textObject.SetTextVariable("CURR_TEMPLATE_NAME", design.Template.TemplateName);
			TextObject textObject2 = design.WeaponName ?? textObject;
			this.Name = textObject2.ToString();
			Crafting.GenerateItem(design, textObject2, Hero.MainHero.Culture, design.Template.ItemModifierGroup, ref this._generatedVisualItem, design.HashedCode);
			MBObjectManager.Instance.RegisterObject<ItemObject>(this._generatedVisualItem);
			this.Visual = new ItemImageIdentifierVM(this._generatedVisualItem, "");
			this.WeaponTypeCode = design.Template.StringId;
			this.Hint = new BasicTooltipViewModel(() => this.GetHint());
		}

		// Token: 0x060017E5 RID: 6117 RVA: 0x0005ABF0 File Offset: 0x00058DF0
		private List<TooltipProperty> GetHint()
		{
			List<TooltipProperty> list = new List<TooltipProperty>();
			list.Add(new TooltipProperty("", this._generatedVisualItem.Name.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.Title));
			foreach (CraftingStatData craftingStatData in Crafting.GetStatDatasFromTemplate(0, this._generatedVisualItem, this.Design.Template))
			{
				if (craftingStatData.IsValid && craftingStatData.CurValue > 0f && craftingStatData.MaxValue > 0f)
				{
					list.Add(new TooltipProperty(craftingStatData.DescriptionText.ToString(), craftingStatData.CurValue.ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
				}
			}
			return list;
		}

		// Token: 0x060017E6 RID: 6118 RVA: 0x0005ACC0 File Offset: 0x00058EC0
		public void ExecuteSelect()
		{
			Action<WeaponDesignSelectorVM> onSelection = this._onSelection;
			if (onSelection == null)
			{
				return;
			}
			onSelection(this);
		}

		// Token: 0x060017E7 RID: 6119 RVA: 0x0005ACD3 File Offset: 0x00058ED3
		public override void OnFinalize()
		{
			base.OnFinalize();
			MBObjectManager.Instance.UnregisterObject(this._generatedVisualItem);
		}

		// Token: 0x17000800 RID: 2048
		// (get) Token: 0x060017E8 RID: 6120 RVA: 0x0005ACEB File Offset: 0x00058EEB
		// (set) Token: 0x060017E9 RID: 6121 RVA: 0x0005ACF3 File Offset: 0x00058EF3
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
					base.OnPropertyChangedWithValue(value, "IsSelected");
				}
			}
		}

		// Token: 0x17000801 RID: 2049
		// (get) Token: 0x060017EA RID: 6122 RVA: 0x0005AD11 File Offset: 0x00058F11
		// (set) Token: 0x060017EB RID: 6123 RVA: 0x0005AD19 File Offset: 0x00058F19
		[DataSourceProperty]
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				if (value != this._name)
				{
					this._name = value;
					base.OnPropertyChangedWithValue<string>(value, "Name");
				}
			}
		}

		// Token: 0x17000802 RID: 2050
		// (get) Token: 0x060017EC RID: 6124 RVA: 0x0005AD3C File Offset: 0x00058F3C
		// (set) Token: 0x060017ED RID: 6125 RVA: 0x0005AD44 File Offset: 0x00058F44
		[DataSourceProperty]
		public string WeaponTypeCode
		{
			get
			{
				return this._weaponTypeCode;
			}
			set
			{
				if (value != this._weaponTypeCode)
				{
					this._weaponTypeCode = value;
					base.OnPropertyChangedWithValue<string>(value, "WeaponTypeCode");
				}
			}
		}

		// Token: 0x17000803 RID: 2051
		// (get) Token: 0x060017EE RID: 6126 RVA: 0x0005AD67 File Offset: 0x00058F67
		// (set) Token: 0x060017EF RID: 6127 RVA: 0x0005AD6F File Offset: 0x00058F6F
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

		// Token: 0x17000804 RID: 2052
		// (get) Token: 0x060017F0 RID: 6128 RVA: 0x0005AD8D File Offset: 0x00058F8D
		// (set) Token: 0x060017F1 RID: 6129 RVA: 0x0005AD95 File Offset: 0x00058F95
		[DataSourceProperty]
		public BasicTooltipViewModel Hint
		{
			get
			{
				return this._hint;
			}
			set
			{
				if (value != this._hint)
				{
					this._hint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "Hint");
				}
			}
		}

		// Token: 0x04000AFA RID: 2810
		private readonly Action<WeaponDesignSelectorVM> _onSelection;

		// Token: 0x04000AFB RID: 2811
		private readonly ItemObject _generatedVisualItem;

		// Token: 0x04000AFC RID: 2812
		private bool _isSelected;

		// Token: 0x04000AFD RID: 2813
		private string _name;

		// Token: 0x04000AFE RID: 2814
		private string _weaponTypeCode;

		// Token: 0x04000AFF RID: 2815
		private ItemImageIdentifierVM _visual;

		// Token: 0x04000B00 RID: 2816
		private BasicTooltipViewModel _hint;
	}
}
