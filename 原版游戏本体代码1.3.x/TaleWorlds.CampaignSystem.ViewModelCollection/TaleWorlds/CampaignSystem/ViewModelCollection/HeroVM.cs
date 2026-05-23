using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection
{
	// Token: 0x0200001B RID: 27
	public class HeroVM : ViewModel
	{
		// Token: 0x1700003B RID: 59
		// (get) Token: 0x06000198 RID: 408 RVA: 0x0000C147 File Offset: 0x0000A347
		public Hero Hero { get; }

		// Token: 0x06000199 RID: 409 RVA: 0x0000C150 File Offset: 0x0000A350
		public HeroVM(Hero hero, bool useCivilian = false)
		{
			if (hero != null)
			{
				CharacterCode characterCode = CampaignUIHelper.GetCharacterCode(hero.CharacterObject, useCivilian);
				this.ImageIdentifier = new CharacterImageIdentifierVM(characterCode);
				this.ClanBanner = new BannerImageIdentifierVM(hero.ClanBanner, false);
				this.ClanBanner_9 = new BannerImageIdentifierVM(hero.ClanBanner, true);
				this.Relation = HeroVM.GetRelation(hero);
				this.IsDead = !hero.IsAlive;
				TextObject textObject;
				this.IsChild = !CampaignUIHelper.IsHeroInformationHidden(hero, out textObject) && FaceGen.GetMaturityTypeWithAge(hero.Age) <= BodyMeshMaturityType.Child;
				this.IsKingdomLeader = hero.IsKingdomLeader;
			}
			else
			{
				this.ImageIdentifier = new CharacterImageIdentifierVM(null);
				this.ClanBanner = new BannerImageIdentifierVM(null, false);
				this.ClanBanner_9 = new BannerImageIdentifierVM(null, false);
				this.Relation = 0;
			}
			this.Hero = hero;
			this.RefreshValues();
		}

		// Token: 0x0600019A RID: 410 RVA: 0x0000C23C File Offset: 0x0000A43C
		public override void RefreshValues()
		{
			base.RefreshValues();
			if (this.Hero != null)
			{
				this.NameText = this.Hero.Name.ToString();
				return;
			}
			this.NameText = "";
		}

		// Token: 0x0600019B RID: 411 RVA: 0x0000C26E File Offset: 0x0000A46E
		public void ExecuteLink()
		{
			if (this.Hero != null)
			{
				Campaign.Current.EncyclopediaManager.GoToLink(this.Hero.EncyclopediaLink);
			}
		}

		// Token: 0x0600019C RID: 412 RVA: 0x0000C292 File Offset: 0x0000A492
		public virtual void ExecuteBeginHint()
		{
			if (this.Hero != null)
			{
				InformationManager.ShowTooltip(typeof(Hero), new object[] { this.Hero, false });
			}
		}

		// Token: 0x0600019D RID: 413 RVA: 0x0000C2C3 File Offset: 0x0000A4C3
		public virtual void ExecuteEndHint()
		{
			if (this.Hero != null)
			{
				MBInformationManager.HideInformations();
			}
		}

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x0600019E RID: 414 RVA: 0x0000C2D2 File Offset: 0x0000A4D2
		// (set) Token: 0x0600019F RID: 415 RVA: 0x0000C2DA File Offset: 0x0000A4DA
		[DataSourceProperty]
		public bool IsDead
		{
			get
			{
				return this._isDead;
			}
			set
			{
				if (value != this._isDead)
				{
					this._isDead = value;
					base.OnPropertyChangedWithValue(value, "IsDead");
				}
			}
		}

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x060001A0 RID: 416 RVA: 0x0000C2F8 File Offset: 0x0000A4F8
		// (set) Token: 0x060001A1 RID: 417 RVA: 0x0000C300 File Offset: 0x0000A500
		[DataSourceProperty]
		public bool IsChild
		{
			get
			{
				return this._isChild;
			}
			set
			{
				if (value != this._isChild)
				{
					this._isChild = value;
					base.OnPropertyChangedWithValue(value, "IsChild");
				}
			}
		}

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x060001A2 RID: 418 RVA: 0x0000C31E File Offset: 0x0000A51E
		// (set) Token: 0x060001A3 RID: 419 RVA: 0x0000C326 File Offset: 0x0000A526
		[DataSourceProperty]
		public bool IsKingdomLeader
		{
			get
			{
				return this._isKingdomLeader;
			}
			set
			{
				if (value != this._isKingdomLeader)
				{
					this._isKingdomLeader = value;
					base.OnPropertyChangedWithValue(value, "IsKingdomLeader");
				}
			}
		}

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x060001A4 RID: 420 RVA: 0x0000C344 File Offset: 0x0000A544
		// (set) Token: 0x060001A5 RID: 421 RVA: 0x0000C34C File Offset: 0x0000A54C
		[DataSourceProperty]
		public int Relation
		{
			get
			{
				return this._relation;
			}
			set
			{
				if (value != this._relation)
				{
					this._relation = value;
					base.OnPropertyChangedWithValue(value, "Relation");
				}
			}
		}

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x060001A6 RID: 422 RVA: 0x0000C36A File Offset: 0x0000A56A
		// (set) Token: 0x060001A7 RID: 423 RVA: 0x0000C372 File Offset: 0x0000A572
		[DataSourceProperty]
		public CharacterImageIdentifierVM ImageIdentifier
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
					base.OnPropertyChangedWithValue<CharacterImageIdentifierVM>(value, "ImageIdentifier");
				}
			}
		}

		// Token: 0x17000041 RID: 65
		// (get) Token: 0x060001A8 RID: 424 RVA: 0x0000C390 File Offset: 0x0000A590
		// (set) Token: 0x060001A9 RID: 425 RVA: 0x0000C398 File Offset: 0x0000A598
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

		// Token: 0x17000042 RID: 66
		// (get) Token: 0x060001AA RID: 426 RVA: 0x0000C3BB File Offset: 0x0000A5BB
		// (set) Token: 0x060001AB RID: 427 RVA: 0x0000C3C3 File Offset: 0x0000A5C3
		[DataSourceProperty]
		public BannerImageIdentifierVM ClanBanner
		{
			get
			{
				return this._clanBanner;
			}
			set
			{
				if (value != this._clanBanner)
				{
					this._clanBanner = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "ClanBanner");
				}
			}
		}

		// Token: 0x17000043 RID: 67
		// (get) Token: 0x060001AC RID: 428 RVA: 0x0000C3E1 File Offset: 0x0000A5E1
		// (set) Token: 0x060001AD RID: 429 RVA: 0x0000C3E9 File Offset: 0x0000A5E9
		[DataSourceProperty]
		public BannerImageIdentifierVM ClanBanner_9
		{
			get
			{
				return this._clanBanner_9;
			}
			set
			{
				if (value != this._clanBanner_9)
				{
					this._clanBanner_9 = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "ClanBanner_9");
				}
			}
		}

		// Token: 0x060001AE RID: 430 RVA: 0x0000C407 File Offset: 0x0000A607
		public static int GetRelation(Hero hero)
		{
			if (hero == null)
			{
				return -101;
			}
			if (hero == Hero.MainHero)
			{
				return 101;
			}
			if (ViewModel.UIDebugMode)
			{
				return MBRandom.RandomInt(-100, 100);
			}
			return Hero.MainHero.GetRelation(hero);
		}

		// Token: 0x040000C2 RID: 194
		private CharacterImageIdentifierVM _imageIdentifier;

		// Token: 0x040000C3 RID: 195
		private BannerImageIdentifierVM _clanBanner;

		// Token: 0x040000C4 RID: 196
		private BannerImageIdentifierVM _clanBanner_9;

		// Token: 0x040000C5 RID: 197
		private string _nameText;

		// Token: 0x040000C6 RID: 198
		private int _relation = -102;

		// Token: 0x040000C7 RID: 199
		private bool _isDead = true;

		// Token: 0x040000C8 RID: 200
		private bool _isChild;

		// Token: 0x040000C9 RID: 201
		private bool _isKingdomLeader;
	}
}
