using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items
{
	// Token: 0x020000E4 RID: 228
	public class EncyclopediaDwellingVM : ViewModel
	{
		// Token: 0x06001560 RID: 5472 RVA: 0x0005416C File Offset: 0x0005236C
		public EncyclopediaDwellingVM(WorkshopType workshop)
		{
			this._workshop = workshop;
			this.FileName = workshop.StringId;
			this.RefreshValues();
		}

		// Token: 0x06001561 RID: 5473 RVA: 0x0005418D File Offset: 0x0005238D
		public EncyclopediaDwellingVM(VillageType villageType)
		{
			this._villageType = villageType;
			this.FileName = villageType.StringId;
			this.RefreshValues();
		}

		// Token: 0x06001562 RID: 5474 RVA: 0x000541B0 File Offset: 0x000523B0
		public override void RefreshValues()
		{
			base.RefreshValues();
			if (this._workshop != null)
			{
				this.NameText = this._workshop.Name.ToString();
				return;
			}
			if (this._villageType != null)
			{
				this.NameText = this._villageType.ShortName.ToString();
			}
		}

		// Token: 0x17000713 RID: 1811
		// (get) Token: 0x06001563 RID: 5475 RVA: 0x00054200 File Offset: 0x00052400
		// (set) Token: 0x06001564 RID: 5476 RVA: 0x00054208 File Offset: 0x00052408
		[DataSourceProperty]
		public string FileName
		{
			get
			{
				return this._fileName;
			}
			set
			{
				if (value != this._fileName)
				{
					this._fileName = value;
					base.OnPropertyChangedWithValue<string>(value, "FileName");
				}
			}
		}

		// Token: 0x17000714 RID: 1812
		// (get) Token: 0x06001565 RID: 5477 RVA: 0x0005422B File Offset: 0x0005242B
		// (set) Token: 0x06001566 RID: 5478 RVA: 0x00054233 File Offset: 0x00052433
		[DataSourceProperty]
		public string NameText
		{
			get
			{
				return this._nameText;
			}
			set
			{
				if (value != this._nameText)
				{
					this._nameText = value;
					base.OnPropertyChangedWithValue<string>(value, "NameText");
				}
			}
		}

		// Token: 0x040009BD RID: 2493
		private readonly WorkshopType _workshop;

		// Token: 0x040009BE RID: 2494
		private readonly VillageType _villageType;

		// Token: 0x040009BF RID: 2495
		private string _fileName;

		// Token: 0x040009C0 RID: 2496
		private string _nameText;
	}
}
