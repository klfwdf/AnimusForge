using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Clans
{
	// Token: 0x02000085 RID: 133
	public class KingdomClanFiefItemVM : ViewModel
	{
		// Token: 0x06000B2D RID: 2861 RVA: 0x0002F5C8 File Offset: 0x0002D7C8
		public KingdomClanFiefItemVM(Settlement settlement)
		{
			this.Settlement = settlement;
			SettlementComponent settlementComponent = settlement.SettlementComponent;
			this.VisualPath = ((settlementComponent == null) ? "placeholder" : (settlementComponent.BackgroundMeshName + "_t"));
			this.RefreshValues();
		}

		// Token: 0x06000B2E RID: 2862 RVA: 0x0002F60F File Offset: 0x0002D80F
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.FiefName = this.Settlement.Name.ToString();
		}

		// Token: 0x06000B2F RID: 2863 RVA: 0x0002F62D File Offset: 0x0002D82D
		private void ExecuteBeginHint()
		{
			InformationManager.ShowTooltip(typeof(Settlement), new object[] { this.Settlement, true });
		}

		// Token: 0x06000B30 RID: 2864 RVA: 0x0002F656 File Offset: 0x0002D856
		private void ExecuteEndHint()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x06000B31 RID: 2865 RVA: 0x0002F65D File Offset: 0x0002D85D
		public void ExecuteLink()
		{
			if (this.Settlement != null)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(this.Settlement.EncyclopediaLink);
			}
		}

		// Token: 0x1700038A RID: 906
		// (get) Token: 0x06000B32 RID: 2866 RVA: 0x0002F681 File Offset: 0x0002D881
		// (set) Token: 0x06000B33 RID: 2867 RVA: 0x0002F689 File Offset: 0x0002D889
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
					base.OnPropertyChanged("FileName");
				}
			}
		}

		// Token: 0x1700038B RID: 907
		// (get) Token: 0x06000B34 RID: 2868 RVA: 0x0002F6AB File Offset: 0x0002D8AB
		// (set) Token: 0x06000B35 RID: 2869 RVA: 0x0002F6B3 File Offset: 0x0002D8B3
		[DataSourceProperty]
		public string FiefName
		{
			get
			{
				return this._fiefName;
			}
			set
			{
				if (value != this._fiefName)
				{
					this._fiefName = value;
					base.OnPropertyChangedWithValue<string>(value, "FiefName");
				}
			}
		}

		// Token: 0x040004F4 RID: 1268
		private readonly Settlement Settlement;

		// Token: 0x040004F5 RID: 1269
		private string _visualPath;

		// Token: 0x040004F6 RID: 1270
		private string _fiefName;
	}
}
