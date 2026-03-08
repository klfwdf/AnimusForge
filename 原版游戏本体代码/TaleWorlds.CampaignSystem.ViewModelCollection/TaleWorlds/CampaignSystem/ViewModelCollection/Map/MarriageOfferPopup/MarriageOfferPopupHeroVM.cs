using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MarriageOfferPopup
{
	// Token: 0x02000039 RID: 57
	public class MarriageOfferPopupHeroVM : ViewModel
	{
		// Token: 0x1700018C RID: 396
		// (get) Token: 0x06000581 RID: 1409 RVA: 0x0001DB2D File Offset: 0x0001BD2D
		public Hero Hero { get; }

		// Token: 0x06000582 RID: 1410 RVA: 0x0001DB35 File Offset: 0x0001BD35
		public MarriageOfferPopupHeroVM(Hero hero)
		{
			this.Hero = hero;
			this.Model = new HeroViewModel(CharacterViewModel.StanceTypes.None);
			this.FillHeroInformation();
			this.CreateClanBanner();
			this.RefreshValues();
		}

		// Token: 0x06000583 RID: 1411 RVA: 0x0001DB64 File Offset: 0x0001BD64
		public void Update()
		{
			TextObject textObject;
			if (!this._modelCreated && !CampaignUIHelper.IsHeroInformationHidden(this.Hero, out textObject))
			{
				this._modelCreated = true;
				this.CreateHeroModel();
			}
		}

		// Token: 0x06000584 RID: 1412 RVA: 0x0001DB98 File Offset: 0x0001BD98
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.EncyclopediaLinkWithName = this.Hero.EncyclopediaLinkWithName.ToString();
			this.AgeString = ((int)this.Hero.Age).ToString();
			this.OccupationString = CampaignUIHelper.GetHeroOccupationName(this.Hero);
			this.Relation = (int)this.Hero.GetRelationWithPlayer();
		}

		// Token: 0x06000585 RID: 1413 RVA: 0x0001DC00 File Offset: 0x0001BE00
		public override void OnFinalize()
		{
			BannerImageIdentifierVM clanBanner = this.ClanBanner;
			if (clanBanner != null)
			{
				clanBanner.OnFinalize();
			}
			HeroViewModel model = this.Model;
			if (model != null)
			{
				model.OnFinalize();
			}
			MBBindingList<EncyclopediaTraitItemVM> traits = this.Traits;
			if (traits != null)
			{
				traits.ApplyActionOnAllItems(delegate(EncyclopediaTraitItemVM x)
				{
					x.OnFinalize();
				});
			}
			MBBindingList<EncyclopediaTraitItemVM> traits2 = this.Traits;
			if (traits2 != null)
			{
				traits2.Clear();
			}
			MBBindingList<MarriageOfferPopupHeroAttributeVM> attributes = this.Attributes;
			if (attributes != null)
			{
				attributes.ApplyActionOnAllItems(delegate(MarriageOfferPopupHeroAttributeVM x)
				{
					x.OnFinalize();
				});
			}
			MBBindingList<MarriageOfferPopupHeroAttributeVM> attributes2 = this.Attributes;
			if (attributes2 != null)
			{
				attributes2.Clear();
			}
			MBBindingList<EncyclopediaSkillVM> otherSkills = this.OtherSkills;
			if (otherSkills != null)
			{
				otherSkills.ApplyActionOnAllItems(delegate(EncyclopediaSkillVM x)
				{
					x.OnFinalize();
				});
			}
			MBBindingList<EncyclopediaSkillVM> otherSkills2 = this.OtherSkills;
			if (otherSkills2 != null)
			{
				otherSkills2.Clear();
			}
			base.OnFinalize();
		}

		// Token: 0x06000586 RID: 1414 RVA: 0x0001DCF8 File Offset: 0x0001BEF8
		public void ExecuteHeroLink()
		{
			Campaign.Current.EncyclopediaManager.GoToLink(this.Hero.EncyclopediaLink);
		}

		// Token: 0x06000587 RID: 1415 RVA: 0x0001DD14 File Offset: 0x0001BF14
		public void ExecuteClanLink()
		{
			Campaign.Current.EncyclopediaManager.GoToLink(this.Hero.Clan.EncyclopediaLink);
		}

		// Token: 0x06000588 RID: 1416 RVA: 0x0001DD35 File Offset: 0x0001BF35
		private void CreateClanBanner()
		{
			this.ClanName = this.Hero.Clan.Name.ToString();
			this.ClanBanner = new BannerImageIdentifierVM(this.Hero.ClanBanner, true);
		}

		// Token: 0x06000589 RID: 1417 RVA: 0x0001DD6C File Offset: 0x0001BF6C
		private void CreateHeroModel()
		{
			this.Model.FillFrom(this.Hero, -1, true, true);
			this.Model.SetEquipment(EquipmentIndex.ArmorItemEndSlot, default(EquipmentElement));
			this.Model.SetEquipment(EquipmentIndex.HorseHarness, default(EquipmentElement));
			this.Model.SetEquipment(EquipmentIndex.NumAllWeaponSlots, default(EquipmentElement));
		}

		// Token: 0x0600058A RID: 1418 RVA: 0x0001DDD0 File Offset: 0x0001BFD0
		private void FillHeroInformation()
		{
			this.Traits = new MBBindingList<EncyclopediaTraitItemVM>();
			this.Attributes = new MBBindingList<MarriageOfferPopupHeroAttributeVM>();
			this.OtherSkills = new MBBindingList<EncyclopediaSkillVM>();
			List<CharacterAttribute> list = TaleWorlds.CampaignSystem.Extensions.Attributes.All.ToList<CharacterAttribute>();
			list.Sort(CampaignUIHelper.CharacterAttributeComparerInstance);
			foreach (CharacterAttribute attribute2 in list)
			{
				this.Attributes.Add(new MarriageOfferPopupHeroAttributeVM(this.Hero, attribute2));
			}
			List<SkillObject> list2 = Skills.All.ToList<SkillObject>();
			list2.Sort(CampaignUIHelper.SkillObjectComparerInstance);
			using (List<SkillObject>.Enumerator enumerator2 = list2.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					SkillObject skill = enumerator2.Current;
					Func<EncyclopediaSkillVM, bool> <>9__1;
					if (!this.Attributes.Any(delegate(MarriageOfferPopupHeroAttributeVM attribute)
					{
						IEnumerable<EncyclopediaSkillVM> attributeSkills = attribute.AttributeSkills;
						Func<EncyclopediaSkillVM, bool> predicate;
						if ((predicate = <>9__1) == null)
						{
							predicate = (<>9__1 = (EncyclopediaSkillVM attributeSkill) => attributeSkill.SkillId == skill.StringId);
						}
						return attributeSkills.Any(predicate);
					}))
					{
						this.OtherSkills.Add(new EncyclopediaSkillVM(skill, this.Hero.GetSkillValue(skill)));
					}
				}
			}
			this.HasOtherSkills = this.OtherSkills.Count > 0;
			foreach (TraitObject traitObject in CampaignUIHelper.GetHeroTraits())
			{
				if (this.Hero.GetTraitLevel(traitObject) != 0)
				{
					this.Traits.Add(new EncyclopediaTraitItemVM(traitObject, this.Hero));
				}
			}
		}

		// Token: 0x1700018D RID: 397
		// (get) Token: 0x0600058B RID: 1419 RVA: 0x0001DF70 File Offset: 0x0001C170
		// (set) Token: 0x0600058C RID: 1420 RVA: 0x0001DF78 File Offset: 0x0001C178
		[DataSourceProperty]
		public string EncyclopediaLinkWithName
		{
			get
			{
				return this._encyclopediaLinkWithName;
			}
			set
			{
				if (value != this._encyclopediaLinkWithName)
				{
					this._encyclopediaLinkWithName = value;
					base.OnPropertyChangedWithValue<string>(value, "EncyclopediaLinkWithName");
				}
			}
		}

		// Token: 0x1700018E RID: 398
		// (get) Token: 0x0600058D RID: 1421 RVA: 0x0001DF9B File Offset: 0x0001C19B
		// (set) Token: 0x0600058E RID: 1422 RVA: 0x0001DFA3 File Offset: 0x0001C1A3
		[DataSourceProperty]
		public string AgeString
		{
			get
			{
				return this._ageString;
			}
			set
			{
				if (value != this._ageString)
				{
					this._ageString = value;
					base.OnPropertyChangedWithValue<string>(value, "AgeString");
				}
			}
		}

		// Token: 0x1700018F RID: 399
		// (get) Token: 0x0600058F RID: 1423 RVA: 0x0001DFC6 File Offset: 0x0001C1C6
		// (set) Token: 0x06000590 RID: 1424 RVA: 0x0001DFCE File Offset: 0x0001C1CE
		[DataSourceProperty]
		public string OccupationString
		{
			get
			{
				return this._occupationString;
			}
			set
			{
				if (value != this._occupationString)
				{
					this._occupationString = value;
					base.OnPropertyChangedWithValue<string>(value, "OccupationString");
				}
			}
		}

		// Token: 0x17000190 RID: 400
		// (get) Token: 0x06000591 RID: 1425 RVA: 0x0001DFF1 File Offset: 0x0001C1F1
		// (set) Token: 0x06000592 RID: 1426 RVA: 0x0001DFF9 File Offset: 0x0001C1F9
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

		// Token: 0x17000191 RID: 401
		// (get) Token: 0x06000593 RID: 1427 RVA: 0x0001E017 File Offset: 0x0001C217
		// (set) Token: 0x06000594 RID: 1428 RVA: 0x0001E01F File Offset: 0x0001C21F
		[DataSourceProperty]
		public string ClanName
		{
			get
			{
				return this._clanName;
			}
			set
			{
				if (value != this._clanName)
				{
					this._clanName = value;
					base.OnPropertyChangedWithValue<string>(value, "ClanName");
				}
			}
		}

		// Token: 0x17000192 RID: 402
		// (get) Token: 0x06000595 RID: 1429 RVA: 0x0001E042 File Offset: 0x0001C242
		// (set) Token: 0x06000596 RID: 1430 RVA: 0x0001E04A File Offset: 0x0001C24A
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

		// Token: 0x17000193 RID: 403
		// (get) Token: 0x06000597 RID: 1431 RVA: 0x0001E068 File Offset: 0x0001C268
		// (set) Token: 0x06000598 RID: 1432 RVA: 0x0001E070 File Offset: 0x0001C270
		[DataSourceProperty]
		public HeroViewModel Model
		{
			get
			{
				return this._model;
			}
			set
			{
				if (value != this._model)
				{
					this._model = value;
					base.OnPropertyChangedWithValue<HeroViewModel>(value, "Model");
				}
			}
		}

		// Token: 0x17000194 RID: 404
		// (get) Token: 0x06000599 RID: 1433 RVA: 0x0001E08E File Offset: 0x0001C28E
		// (set) Token: 0x0600059A RID: 1434 RVA: 0x0001E096 File Offset: 0x0001C296
		[DataSourceProperty]
		public MBBindingList<EncyclopediaTraitItemVM> Traits
		{
			get
			{
				return this._traits;
			}
			set
			{
				if (value != this._traits)
				{
					this._traits = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaTraitItemVM>>(value, "Traits");
				}
			}
		}

		// Token: 0x17000195 RID: 405
		// (get) Token: 0x0600059B RID: 1435 RVA: 0x0001E0B4 File Offset: 0x0001C2B4
		// (set) Token: 0x0600059C RID: 1436 RVA: 0x0001E0BC File Offset: 0x0001C2BC
		[DataSourceProperty]
		public MBBindingList<MarriageOfferPopupHeroAttributeVM> Attributes
		{
			get
			{
				return this._attributes;
			}
			set
			{
				if (value != this._attributes)
				{
					this._attributes = value;
					base.OnPropertyChangedWithValue<MBBindingList<MarriageOfferPopupHeroAttributeVM>>(value, "Attributes");
				}
			}
		}

		// Token: 0x17000196 RID: 406
		// (get) Token: 0x0600059D RID: 1437 RVA: 0x0001E0DA File Offset: 0x0001C2DA
		// (set) Token: 0x0600059E RID: 1438 RVA: 0x0001E0E2 File Offset: 0x0001C2E2
		[DataSourceProperty]
		public MBBindingList<EncyclopediaSkillVM> OtherSkills
		{
			get
			{
				return this._otherSkills;
			}
			set
			{
				if (value != this._otherSkills)
				{
					this._otherSkills = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaSkillVM>>(value, "OtherSkills");
				}
			}
		}

		// Token: 0x17000197 RID: 407
		// (get) Token: 0x0600059F RID: 1439 RVA: 0x0001E100 File Offset: 0x0001C300
		// (set) Token: 0x060005A0 RID: 1440 RVA: 0x0001E108 File Offset: 0x0001C308
		[DataSourceProperty]
		public bool HasOtherSkills
		{
			get
			{
				return this._hasOtherSkills;
			}
			set
			{
				if (value != this._hasOtherSkills)
				{
					this._hasOtherSkills = value;
					base.OnPropertyChangedWithValue(value, "HasOtherSkills");
				}
			}
		}

		// Token: 0x04000259 RID: 601
		private bool _modelCreated;

		// Token: 0x0400025B RID: 603
		private string _encyclopediaLinkWithName;

		// Token: 0x0400025C RID: 604
		private string _ageString;

		// Token: 0x0400025D RID: 605
		private string _occupationString;

		// Token: 0x0400025E RID: 606
		private int _relation;

		// Token: 0x0400025F RID: 607
		private string _clanName;

		// Token: 0x04000260 RID: 608
		private BannerImageIdentifierVM _clanBanner;

		// Token: 0x04000261 RID: 609
		private HeroViewModel _model;

		// Token: 0x04000262 RID: 610
		private MBBindingList<EncyclopediaTraitItemVM> _traits;

		// Token: 0x04000263 RID: 611
		private MBBindingList<MarriageOfferPopupHeroAttributeVM> _attributes;

		// Token: 0x04000264 RID: 612
		private MBBindingList<EncyclopediaSkillVM> _otherSkills;

		// Token: 0x04000265 RID: 613
		private bool _hasOtherSkills;
	}
}
