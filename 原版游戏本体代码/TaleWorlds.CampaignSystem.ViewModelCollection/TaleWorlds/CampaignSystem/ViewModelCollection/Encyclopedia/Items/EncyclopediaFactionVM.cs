using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items
{
	// Token: 0x020000E5 RID: 229
	public class EncyclopediaFactionVM : ViewModel
	{
		// Token: 0x17000715 RID: 1813
		// (get) Token: 0x06001567 RID: 5479 RVA: 0x00054256 File Offset: 0x00052456
		// (set) Token: 0x06001568 RID: 5480 RVA: 0x0005425E File Offset: 0x0005245E
		public IFaction Faction { get; private set; }

		// Token: 0x06001569 RID: 5481 RVA: 0x00054268 File Offset: 0x00052468
		public EncyclopediaFactionVM(IFaction faction)
		{
			this.Faction = faction;
			if (faction != null)
			{
				this.ImageIdentifier = new BannerImageIdentifierVM(faction.Banner, true);
				this.IsDestroyed = faction.IsEliminated;
			}
			else
			{
				this.ImageIdentifier = new BannerImageIdentifierVM(null, false);
				this.IsDestroyed = false;
			}
			this.RefreshValues();
		}

		// Token: 0x0600156A RID: 5482 RVA: 0x000542BF File Offset: 0x000524BF
		public override void RefreshValues()
		{
			base.RefreshValues();
			if (this.Faction != null)
			{
				this.NameText = this.Faction.Name.ToString();
				return;
			}
			this.NameText = new TextObject("{=2abtb4xu}Independent", null).ToString();
		}

		// Token: 0x0600156B RID: 5483 RVA: 0x000542FC File Offset: 0x000524FC
		public void ExecuteLink()
		{
			if (this.Faction != null)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(this.Faction.EncyclopediaLink);
			}
		}

		// Token: 0x0600156C RID: 5484 RVA: 0x00054320 File Offset: 0x00052520
		public void ExecuteBeginHint()
		{
			if (this.Faction is Clan)
			{
				InformationManager.ShowTooltip(typeof(Clan), new object[] { this.Faction });
				return;
			}
			if (this.Faction is Kingdom)
			{
				InformationManager.ShowTooltip(typeof(Kingdom), new object[] { this.Faction });
			}
		}

		// Token: 0x0600156D RID: 5485 RVA: 0x00054384 File Offset: 0x00052584
		public void ExecuteEndHint()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x17000716 RID: 1814
		// (get) Token: 0x0600156E RID: 5486 RVA: 0x0005438B File Offset: 0x0005258B
		// (set) Token: 0x0600156F RID: 5487 RVA: 0x00054393 File Offset: 0x00052593
		[DataSourceProperty]
		public BannerImageIdentifierVM ImageIdentifier
		{
			get
			{
				return this._imageIdentifier;
			}
			set
			{
				if (value != this._imageIdentifier)
				{
					this._imageIdentifier = value;
					base.OnPropertyChanged("Banner");
				}
			}
		}

		// Token: 0x17000717 RID: 1815
		// (get) Token: 0x06001570 RID: 5488 RVA: 0x000543B0 File Offset: 0x000525B0
		// (set) Token: 0x06001571 RID: 5489 RVA: 0x000543B8 File Offset: 0x000525B8
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

		// Token: 0x17000718 RID: 1816
		// (get) Token: 0x06001572 RID: 5490 RVA: 0x000543DB File Offset: 0x000525DB
		// (set) Token: 0x06001573 RID: 5491 RVA: 0x000543E3 File Offset: 0x000525E3
		[DataSourceProperty]
		public bool IsDestroyed
		{
			get
			{
				return this._isDestroyed;
			}
			set
			{
				if (value != this._isDestroyed)
				{
					this._isDestroyed = value;
					base.OnPropertyChangedWithValue(value, "IsDestroyed");
				}
			}
		}

		// Token: 0x040009C2 RID: 2498
		private BannerImageIdentifierVM _imageIdentifier;

		// Token: 0x040009C3 RID: 2499
		private string _nameText;

		// Token: 0x040009C4 RID: 2500
		private bool _isDestroyed;
	}
}
