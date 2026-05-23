using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items
{
	// Token: 0x020000E8 RID: 232
	public class EncyclopediaSettlementVM : ViewModel
	{
		// Token: 0x0600157F RID: 5503 RVA: 0x00054530 File Offset: 0x00052730
		public EncyclopediaSettlementVM(Settlement settlement)
		{
			if (!settlement.IsHideout)
			{
				this._settlement = settlement;
			}
			SettlementComponent settlementComponent = settlement.SettlementComponent;
			this.FileName = ((settlementComponent == null) ? "placeholder" : (settlementComponent.BackgroundMeshName + "_t"));
			this.RefreshValues();
		}

		// Token: 0x06001580 RID: 5504 RVA: 0x0005457F File Offset: 0x0005277F
		public override void RefreshValues()
		{
			base.RefreshValues();
			Settlement settlement = this._settlement;
			this.NameText = ((settlement != null) ? settlement.Name.ToString() : null) ?? "";
		}

		// Token: 0x06001581 RID: 5505 RVA: 0x000545AD File Offset: 0x000527AD
		public void ExecuteLink()
		{
			if (this._settlement != null)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(this._settlement.EncyclopediaLink);
			}
		}

		// Token: 0x06001582 RID: 5506 RVA: 0x000545D1 File Offset: 0x000527D1
		public void ExecuteEndHint()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x06001583 RID: 5507 RVA: 0x000545D8 File Offset: 0x000527D8
		public void ExecuteBeginHint()
		{
			InformationManager.ShowTooltip(typeof(Settlement), new object[] { this._settlement });
		}

		// Token: 0x1700071C RID: 1820
		// (get) Token: 0x06001584 RID: 5508 RVA: 0x000545F8 File Offset: 0x000527F8
		// (set) Token: 0x06001585 RID: 5509 RVA: 0x00054600 File Offset: 0x00052800
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

		// Token: 0x1700071D RID: 1821
		// (get) Token: 0x06001586 RID: 5510 RVA: 0x00054623 File Offset: 0x00052823
		// (set) Token: 0x06001587 RID: 5511 RVA: 0x0005462B File Offset: 0x0005282B
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

		// Token: 0x040009CA RID: 2506
		private Settlement _settlement;

		// Token: 0x040009CB RID: 2507
		private string _fileName;

		// Token: 0x040009CC RID: 2508
		private string _nameText;
	}
}
