using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MarriageOfferPopup;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.HeirSelectionPopup
{
	// Token: 0x02000060 RID: 96
	public class HeirSelectionPopupHeroVM : ViewModel
	{
		// Token: 0x170001EF RID: 495
		// (get) Token: 0x06000718 RID: 1816 RVA: 0x0002263C File Offset: 0x0002083C
		public Hero Hero { get; }

		// Token: 0x06000719 RID: 1817 RVA: 0x00022644 File Offset: 0x00020844
		public HeirSelectionPopupHeroVM(Hero hero)
		{
			this.Hero = hero;
			this.FillHeroInformation();
			this.CreateImageIdentifier();
			this.CreateHeroModel();
			this.RefreshValues();
		}

		// Token: 0x0600071A RID: 1818 RVA: 0x0002266C File Offset: 0x0002086C
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = this.Hero.Name.ToString();
			this.Culture = this.Hero.Culture.Name.ToString();
			this.Occupation = CampaignUIHelper.GetHeroOccupationName(this.Hero);
			this.RelationToMainHero = CampaignUIHelper.GetHeroRelationToHeroText(this.Hero, Hero.MainHero, true).ToString();
		}

		// Token: 0x0600071B RID: 1819 RVA: 0x000226E0 File Offset: 0x000208E0
		public override void OnFinalize()
		{
			HeroViewModel model = this.Model;
			if (model != null)
			{
				model.OnFinalize();
			}
			CharacterImageIdentifierVM imageIdentifier = this.ImageIdentifier;
			if (imageIdentifier != null)
			{
				imageIdentifier.OnFinalize();
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

		// Token: 0x0600071C RID: 1820 RVA: 0x000227D8 File Offset: 0x000209D8
		private void CreateImageIdentifier()
		{
			this.ImageIdentifier = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(this.Hero.CharacterObject));
		}

		// Token: 0x0600071D RID: 1821 RVA: 0x000227F8 File Offset: 0x000209F8
		private void CreateHeroModel()
		{
			this.Model = new HeroViewModel(CharacterViewModel.StanceTypes.None);
			this.Model.FillFrom(this.Hero, -1, false, true);
			this.Model.SetEquipment(EquipmentIndex.ArmorItemEndSlot, default(EquipmentElement));
			this.Model.SetEquipment(EquipmentIndex.HorseHarness, default(EquipmentElement));
			this.Model.SetEquipment(EquipmentIndex.NumAllWeaponSlots, default(EquipmentElement));
		}

		// Token: 0x0600071E RID: 1822 RVA: 0x00022868 File Offset: 0x00020A68
		private void FillHeroInformation()
		{
			this.Age = (int)this.Hero.Age;
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

		// Token: 0x170001F0 RID: 496
		// (get) Token: 0x0600071F RID: 1823 RVA: 0x00022A18 File Offset: 0x00020C18
		// (set) Token: 0x06000720 RID: 1824 RVA: 0x00022A20 File Offset: 0x00020C20
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

		// Token: 0x170001F1 RID: 497
		// (get) Token: 0x06000721 RID: 1825 RVA: 0x00022A43 File Offset: 0x00020C43
		// (set) Token: 0x06000722 RID: 1826 RVA: 0x00022A4B File Offset: 0x00020C4B
		[DataSourceProperty]
		public int Age
		{
			get
			{
				return this._age;
			}
			set
			{
				if (value != this._age)
				{
					this._age = value;
					base.OnPropertyChangedWithValue(value, "Age");
				}
			}
		}

		// Token: 0x170001F2 RID: 498
		// (get) Token: 0x06000723 RID: 1827 RVA: 0x00022A69 File Offset: 0x00020C69
		// (set) Token: 0x06000724 RID: 1828 RVA: 0x00022A71 File Offset: 0x00020C71
		[DataSourceProperty]
		public string Culture
		{
			get
			{
				return this._culture;
			}
			set
			{
				if (value != this._culture)
				{
					this._culture = value;
					base.OnPropertyChangedWithValue<string>(value, "Culture");
				}
			}
		}

		// Token: 0x170001F3 RID: 499
		// (get) Token: 0x06000725 RID: 1829 RVA: 0x00022A94 File Offset: 0x00020C94
		// (set) Token: 0x06000726 RID: 1830 RVA: 0x00022A9C File Offset: 0x00020C9C
		[DataSourceProperty]
		public string Occupation
		{
			get
			{
				return this._occupation;
			}
			set
			{
				if (value != this._occupation)
				{
					this._occupation = value;
					base.OnPropertyChangedWithValue<string>(value, "Occupation");
				}
			}
		}

		// Token: 0x170001F4 RID: 500
		// (get) Token: 0x06000727 RID: 1831 RVA: 0x00022ABF File Offset: 0x00020CBF
		// (set) Token: 0x06000728 RID: 1832 RVA: 0x00022AC7 File Offset: 0x00020CC7
		[DataSourceProperty]
		public string RelationToMainHero
		{
			get
			{
				return this._relationToMainHero;
			}
			set
			{
				if (value != this._relationToMainHero)
				{
					this._relationToMainHero = value;
					base.OnPropertyChangedWithValue<string>(value, "RelationToMainHero");
				}
			}
		}

		// Token: 0x170001F5 RID: 501
		// (get) Token: 0x06000729 RID: 1833 RVA: 0x00022AEA File Offset: 0x00020CEA
		// (set) Token: 0x0600072A RID: 1834 RVA: 0x00022AF2 File Offset: 0x00020CF2
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

		// Token: 0x170001F6 RID: 502
		// (get) Token: 0x0600072B RID: 1835 RVA: 0x00022B10 File Offset: 0x00020D10
		// (set) Token: 0x0600072C RID: 1836 RVA: 0x00022B18 File Offset: 0x00020D18
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

		// Token: 0x170001F7 RID: 503
		// (get) Token: 0x0600072D RID: 1837 RVA: 0x00022B36 File Offset: 0x00020D36
		// (set) Token: 0x0600072E RID: 1838 RVA: 0x00022B3E File Offset: 0x00020D3E
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

		// Token: 0x170001F8 RID: 504
		// (get) Token: 0x0600072F RID: 1839 RVA: 0x00022B5C File Offset: 0x00020D5C
		// (set) Token: 0x06000730 RID: 1840 RVA: 0x00022B64 File Offset: 0x00020D64
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

		// Token: 0x170001F9 RID: 505
		// (get) Token: 0x06000731 RID: 1841 RVA: 0x00022B82 File Offset: 0x00020D82
		// (set) Token: 0x06000732 RID: 1842 RVA: 0x00022B8A File Offset: 0x00020D8A
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

		// Token: 0x170001FA RID: 506
		// (get) Token: 0x06000733 RID: 1843 RVA: 0x00022BA8 File Offset: 0x00020DA8
		// (set) Token: 0x06000734 RID: 1844 RVA: 0x00022BB0 File Offset: 0x00020DB0
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

		// Token: 0x170001FB RID: 507
		// (get) Token: 0x06000735 RID: 1845 RVA: 0x00022BCE File Offset: 0x00020DCE
		// (set) Token: 0x06000736 RID: 1846 RVA: 0x00022BD6 File Offset: 0x00020DD6
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

		// Token: 0x04000315 RID: 789
		private string _name;

		// Token: 0x04000316 RID: 790
		private int _age;

		// Token: 0x04000317 RID: 791
		private string _culture;

		// Token: 0x04000318 RID: 792
		private string _occupation;

		// Token: 0x04000319 RID: 793
		private string _relationToMainHero;

		// Token: 0x0400031A RID: 794
		private HeroViewModel _model;

		// Token: 0x0400031B RID: 795
		private CharacterImageIdentifierVM _imageIdentifier;

		// Token: 0x0400031C RID: 796
		private MBBindingList<EncyclopediaTraitItemVM> _traits;

		// Token: 0x0400031D RID: 797
		private MBBindingList<MarriageOfferPopupHeroAttributeVM> _attributes;

		// Token: 0x0400031E RID: 798
		private bool _isSelected;

		// Token: 0x0400031F RID: 799
		private MBBindingList<EncyclopediaSkillVM> _otherSkills;

		// Token: 0x04000320 RID: 800
		private bool _hasOtherSkills;
	}
}
