using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.GameOver
{
	// Token: 0x02000058 RID: 88
	public class GameOverStatCategoryVM : ViewModel
	{
		// Token: 0x06000577 RID: 1399 RVA: 0x000148A8 File Offset: 0x00012AA8
		public GameOverStatCategoryVM(StatCategory category, Action<GameOverStatCategoryVM> onSelect)
		{
			this._category = category;
			this._onSelect = onSelect;
			this.Items = new MBBindingList<GameOverStatItemVM>();
			this.ID = category.ID;
			this.RefreshValues();
		}

		// Token: 0x06000578 RID: 1400 RVA: 0x000148DC File Offset: 0x00012ADC
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Items.Clear();
			this.Name = GameTexts.FindText("str_game_over_stat_category", this._category.ID).ToString();
			foreach (StatItem item in this._category.Items)
			{
				this.Items.Add(new GameOverStatItemVM(item));
			}
		}

		// Token: 0x06000579 RID: 1401 RVA: 0x0001496C File Offset: 0x00012B6C
		public void ExecuteSelectCategory()
		{
			Action<GameOverStatCategoryVM> onSelect = this._onSelect;
			if (onSelect == null)
			{
				return;
			}
			onSelect.DynamicInvokeWithLog(new object[] { this });
		}

		// Token: 0x1700019F RID: 415
		// (get) Token: 0x0600057A RID: 1402 RVA: 0x00014989 File Offset: 0x00012B89
		// (set) Token: 0x0600057B RID: 1403 RVA: 0x00014991 File Offset: 0x00012B91
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

		// Token: 0x170001A0 RID: 416
		// (get) Token: 0x0600057C RID: 1404 RVA: 0x000149B4 File Offset: 0x00012BB4
		// (set) Token: 0x0600057D RID: 1405 RVA: 0x000149BC File Offset: 0x00012BBC
		[DataSourceProperty]
		public string ID
		{
			get
			{
				return this._id;
			}
			set
			{
				if (value != this._id)
				{
					this._id = value;
					base.OnPropertyChangedWithValue<string>(value, "ID");
				}
			}
		}

		// Token: 0x170001A1 RID: 417
		// (get) Token: 0x0600057E RID: 1406 RVA: 0x000149DF File Offset: 0x00012BDF
		// (set) Token: 0x0600057F RID: 1407 RVA: 0x000149E7 File Offset: 0x00012BE7
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

		// Token: 0x170001A2 RID: 418
		// (get) Token: 0x06000580 RID: 1408 RVA: 0x00014A05 File Offset: 0x00012C05
		// (set) Token: 0x06000581 RID: 1409 RVA: 0x00014A0D File Offset: 0x00012C0D
		[DataSourceProperty]
		public MBBindingList<GameOverStatItemVM> Items
		{
			get
			{
				return this._items;
			}
			set
			{
				if (value != this._items)
				{
					this._items = value;
					base.OnPropertyChangedWithValue<MBBindingList<GameOverStatItemVM>>(value, "Items");
				}
			}
		}

		// Token: 0x040002B1 RID: 689
		private readonly StatCategory _category;

		// Token: 0x040002B2 RID: 690
		private readonly Action<GameOverStatCategoryVM> _onSelect;

		// Token: 0x040002B3 RID: 691
		private string _name;

		// Token: 0x040002B4 RID: 692
		private string _id;

		// Token: 0x040002B5 RID: 693
		private bool _isSelected;

		// Token: 0x040002B6 RID: 694
		private MBBindingList<GameOverStatItemVM> _items;
	}
}
