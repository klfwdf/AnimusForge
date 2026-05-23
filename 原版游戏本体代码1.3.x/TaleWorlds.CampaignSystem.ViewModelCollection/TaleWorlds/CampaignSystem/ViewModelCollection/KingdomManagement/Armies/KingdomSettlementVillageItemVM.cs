using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Armies
{
	// Token: 0x0200008D RID: 141
	public class KingdomSettlementVillageItemVM : ViewModel
	{
		// Token: 0x06000C27 RID: 3111 RVA: 0x00031FC8 File Offset: 0x000301C8
		public KingdomSettlementVillageItemVM(Village village)
		{
			this._village = village;
			this.VisualPath = village.BackgroundMeshName + "_t";
			this.RefreshValues();
		}

		// Token: 0x06000C28 RID: 3112 RVA: 0x00031FF3 File Offset: 0x000301F3
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = this._village.Name.ToString();
		}

		// Token: 0x06000C29 RID: 3113 RVA: 0x00032011 File Offset: 0x00030211
		private void ExecuteBeginHint()
		{
			InformationManager.ShowTooltip(typeof(Settlement), new object[]
			{
				this._village.Settlement,
				true
			});
		}

		// Token: 0x06000C2A RID: 3114 RVA: 0x0003203F File Offset: 0x0003023F
		private void ExecuteEndHint()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x06000C2B RID: 3115 RVA: 0x00032046 File Offset: 0x00030246
		public void ExecuteLink()
		{
			if (this._village != null && this._village.Settlement != null)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(this._village.Settlement.EncyclopediaLink);
			}
		}

		// Token: 0x170003E7 RID: 999
		// (get) Token: 0x06000C2C RID: 3116 RVA: 0x0003207C File Offset: 0x0003027C
		// (set) Token: 0x06000C2D RID: 3117 RVA: 0x00032084 File Offset: 0x00030284
		[DataSourceProperty]
		public ImageIdentifierVM Visual
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
					base.OnPropertyChangedWithValue<ImageIdentifierVM>(value, "Visual");
				}
			}
		}

		// Token: 0x170003E8 RID: 1000
		// (get) Token: 0x06000C2E RID: 3118 RVA: 0x000320A2 File Offset: 0x000302A2
		// (set) Token: 0x06000C2F RID: 3119 RVA: 0x000320AA File Offset: 0x000302AA
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

		// Token: 0x170003E9 RID: 1001
		// (get) Token: 0x06000C30 RID: 3120 RVA: 0x000320CD File Offset: 0x000302CD
		// (set) Token: 0x06000C31 RID: 3121 RVA: 0x000320D5 File Offset: 0x000302D5
		[DataSourceProperty]
		public string VisualPath
		{
			get
			{
				return this._visualPath;
			}
			set
			{
				if (value != this._visualPath)
				{
					this._visualPath = value;
					base.OnPropertyChangedWithValue<string>(value, "VisualPath");
				}
			}
		}

		// Token: 0x0400056B RID: 1387
		private Village _village;

		// Token: 0x0400056C RID: 1388
		private ImageIdentifierVM _visual;

		// Token: 0x0400056D RID: 1389
		private string _name;

		// Token: 0x0400056E RID: 1390
		private string _visualPath;
	}
}
