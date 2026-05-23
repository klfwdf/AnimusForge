using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement
{
	// Token: 0x0200012B RID: 299
	public class ClanPartyMemberItemVM : ViewModel
	{
		// Token: 0x1700097F RID: 2431
		// (get) Token: 0x06001BE4 RID: 7140 RVA: 0x00066FD1 File Offset: 0x000651D1
		// (set) Token: 0x06001BE5 RID: 7141 RVA: 0x00066FD9 File Offset: 0x000651D9
		public Hero HeroObject { get; private set; }

		// Token: 0x06001BE6 RID: 7142 RVA: 0x00066FE4 File Offset: 0x000651E4
		public ClanPartyMemberItemVM(Hero hero, MobileParty party)
		{
			this.HeroObject = hero;
			this.IsLeader = hero == party.LeaderHero;
			CharacterCode characterCode = CampaignUIHelper.GetCharacterCode(hero.CharacterObject, false);
			this.Visual = new CharacterImageIdentifierVM(characterCode);
			this.HeroModel = new HeroViewModel(CharacterViewModel.StanceTypes.None);
			this.HeroModel.FillFrom(this.HeroObject, -1, false, false);
			this.RefreshValues();
		}

		// Token: 0x06001BE7 RID: 7143 RVA: 0x0006704C File Offset: 0x0006524C
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = this.HeroObject.Name.ToString();
			this.UpdateProperties();
		}

		// Token: 0x06001BE8 RID: 7144 RVA: 0x00067070 File Offset: 0x00065270
		private void ExecuteLocationLink(string link)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(link);
		}

		// Token: 0x06001BE9 RID: 7145 RVA: 0x00067082 File Offset: 0x00065282
		public void UpdateProperties()
		{
			this.HeroModel = new HeroViewModel(CharacterViewModel.StanceTypes.None);
			this.HeroModel.FillFrom(this.HeroObject, -1, false, false);
			this.Banner_9 = new BannerImageIdentifierVM(this.HeroObject.ClanBanner, true);
		}

		// Token: 0x06001BEA RID: 7146 RVA: 0x000670BB File Offset: 0x000652BB
		public void ExecuteLink()
		{
			Campaign.Current.EncyclopediaManager.GoToLink(this.HeroObject.EncyclopediaLink);
		}

		// Token: 0x06001BEB RID: 7147 RVA: 0x000670D7 File Offset: 0x000652D7
		public virtual void ExecuteBeginHint()
		{
			InformationManager.ShowTooltip(typeof(Hero), new object[] { this.HeroObject, true });
		}

		// Token: 0x06001BEC RID: 7148 RVA: 0x00067100 File Offset: 0x00065300
		public virtual void ExecuteEndHint()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x06001BED RID: 7149 RVA: 0x00067107 File Offset: 0x00065307
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.HeroModel.OnFinalize();
		}

		// Token: 0x17000980 RID: 2432
		// (get) Token: 0x06001BEE RID: 7150 RVA: 0x0006711A File Offset: 0x0006531A
		// (set) Token: 0x06001BEF RID: 7151 RVA: 0x00067122 File Offset: 0x00065322
		[DataSourceProperty]
		public HeroViewModel HeroModel
		{
			get
			{
				return this._heroModel;
			}
			set
			{
				if (value != this._heroModel)
				{
					this._heroModel = value;
					base.OnPropertyChangedWithValue<HeroViewModel>(value, "HeroModel");
				}
			}
		}

		// Token: 0x17000981 RID: 2433
		// (get) Token: 0x06001BF0 RID: 7152 RVA: 0x00067140 File Offset: 0x00065340
		// (set) Token: 0x06001BF1 RID: 7153 RVA: 0x00067148 File Offset: 0x00065348
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

		// Token: 0x17000982 RID: 2434
		// (get) Token: 0x06001BF2 RID: 7154 RVA: 0x00067166 File Offset: 0x00065366
		// (set) Token: 0x06001BF3 RID: 7155 RVA: 0x0006716E File Offset: 0x0006536E
		[DataSourceProperty]
		public BannerImageIdentifierVM Banner_9
		{
			get
			{
				return this._banner_9;
			}
			set
			{
				if (value != this._banner_9)
				{
					this._banner_9 = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "Banner_9");
				}
			}
		}

		// Token: 0x17000983 RID: 2435
		// (get) Token: 0x06001BF4 RID: 7156 RVA: 0x0006718C File Offset: 0x0006538C
		// (set) Token: 0x06001BF5 RID: 7157 RVA: 0x00067194 File Offset: 0x00065394
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

		// Token: 0x17000984 RID: 2436
		// (get) Token: 0x06001BF6 RID: 7158 RVA: 0x000671B7 File Offset: 0x000653B7
		// (set) Token: 0x06001BF7 RID: 7159 RVA: 0x000671BF File Offset: 0x000653BF
		[DataSourceProperty]
		public bool IsLeader
		{
			get
			{
				return this._isLeader;
			}
			set
			{
				if (value != this._isLeader)
				{
					this._isLeader = value;
					base.OnPropertyChangedWithValue(value, "IsLeader");
				}
			}
		}

		// Token: 0x04000D07 RID: 3335
		private CharacterImageIdentifierVM _visual;

		// Token: 0x04000D08 RID: 3336
		private BannerImageIdentifierVM _banner_9;

		// Token: 0x04000D09 RID: 3337
		private string _name;

		// Token: 0x04000D0A RID: 3338
		private bool _isLeader;

		// Token: 0x04000D0B RID: 3339
		private HeroViewModel _heroModel;
	}
}
