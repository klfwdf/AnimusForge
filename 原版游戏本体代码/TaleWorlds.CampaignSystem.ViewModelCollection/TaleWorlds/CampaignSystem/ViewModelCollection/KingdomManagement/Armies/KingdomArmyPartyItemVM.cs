using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Armies
{
	// Token: 0x0200008A RID: 138
	public class KingdomArmyPartyItemVM : ViewModel
	{
		// Token: 0x06000BC2 RID: 3010 RVA: 0x00030F43 File Offset: 0x0002F143
		public KingdomArmyPartyItemVM(MobileParty party)
		{
			this._party = party;
			Hero leaderHero = party.LeaderHero;
			this.Visual = new CharacterImageIdentifierVM(CampaignUIHelper.GetCharacterCode((leaderHero != null) ? leaderHero.CharacterObject : null, false));
			this.RefreshValues();
		}

		// Token: 0x06000BC3 RID: 3011 RVA: 0x00030F7B File Offset: 0x0002F17B
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = this._party.Name.ToString();
		}

		// Token: 0x06000BC4 RID: 3012 RVA: 0x00030F99 File Offset: 0x0002F199
		private void ExecuteBeginHint()
		{
			InformationManager.ShowTooltip(typeof(MobileParty), new object[] { this._party, true, false });
		}

		// Token: 0x06000BC5 RID: 3013 RVA: 0x00030FCB File Offset: 0x0002F1CB
		private void ExecuteEndHint()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x06000BC6 RID: 3014 RVA: 0x00030FD2 File Offset: 0x0002F1D2
		public void ExecuteLink()
		{
			if (this._party != null && this._party.LeaderHero != null)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(this._party.LeaderHero.EncyclopediaLink);
			}
		}

		// Token: 0x170003C1 RID: 961
		// (get) Token: 0x06000BC7 RID: 3015 RVA: 0x00031008 File Offset: 0x0002F208
		// (set) Token: 0x06000BC8 RID: 3016 RVA: 0x00031010 File Offset: 0x0002F210
		[DataSourceProperty]
		public CharacterImageIdentifierVM Visual
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
					base.OnPropertyChangedWithValue<CharacterImageIdentifierVM>(value, "Visual");
				}
			}
		}

		// Token: 0x170003C2 RID: 962
		// (get) Token: 0x06000BC9 RID: 3017 RVA: 0x0003102E File Offset: 0x0002F22E
		// (set) Token: 0x06000BCA RID: 3018 RVA: 0x00031036 File Offset: 0x0002F236
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

		// Token: 0x04000539 RID: 1337
		private MobileParty _party;

		// Token: 0x0400053A RID: 1338
		private CharacterImageIdentifierVM _visual;

		// Token: 0x0400053B RID: 1339
		private string _name;
	}
}
