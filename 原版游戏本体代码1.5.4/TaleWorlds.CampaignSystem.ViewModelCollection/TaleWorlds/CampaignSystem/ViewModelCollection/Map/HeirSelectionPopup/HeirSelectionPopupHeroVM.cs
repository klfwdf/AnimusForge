using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MarriageOfferPopup;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.HeirSelectionPopup;

public class HeirSelectionPopupHeroVM : ViewModel
{
	private string _name;

	private int _age;

	private string _culture;

	private string _occupation;

	private string _relationToMainHero;

	private HeroViewModel _model;

	private CharacterImageIdentifierVM _imageIdentifier;

	private MBBindingList<EncyclopediaTraitItemVM> _traits;

	private MBBindingList<MarriageOfferPopupHeroAttributeVM> _attributes;

	private bool _isSelected;

	private MBBindingList<EncyclopediaSkillVM> _otherSkills;

	private bool _hasOtherSkills;

	public Hero Hero { get; }

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public int Age
	{
		get
		{
			return _age;
		}
		set
		{
			if (value != _age)
			{
				_age = value;
				OnPropertyChangedWithValue(value, "Age");
			}
		}
	}

	[DataSourceProperty]
	public string Culture
	{
		get
		{
			return _culture;
		}
		set
		{
			if (value != _culture)
			{
				_culture = value;
				OnPropertyChangedWithValue(value, "Culture");
			}
		}
	}

	[DataSourceProperty]
	public string Occupation
	{
		get
		{
			return _occupation;
		}
		set
		{
			if (value != _occupation)
			{
				_occupation = value;
				OnPropertyChangedWithValue(value, "Occupation");
			}
		}
	}

	[DataSourceProperty]
	public string RelationToMainHero
	{
		get
		{
			return _relationToMainHero;
		}
		set
		{
			if (value != _relationToMainHero)
			{
				_relationToMainHero = value;
				OnPropertyChangedWithValue(value, "RelationToMainHero");
			}
		}
	}

	[DataSourceProperty]
	public HeroViewModel Model
	{
		get
		{
			return _model;
		}
		set
		{
			if (value != _model)
			{
				_model = value;
				OnPropertyChangedWithValue(value, "Model");
			}
		}
	}

	[DataSourceProperty]
	public CharacterImageIdentifierVM ImageIdentifier
	{
		get
		{
			return _imageIdentifier;
		}
		set
		{
			if (value != _imageIdentifier)
			{
				_imageIdentifier = value;
				OnPropertyChangedWithValue(value, "ImageIdentifier");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EncyclopediaTraitItemVM> Traits
	{
		get
		{
			return _traits;
		}
		set
		{
			if (value != _traits)
			{
				_traits = value;
				OnPropertyChangedWithValue(value, "Traits");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MarriageOfferPopupHeroAttributeVM> Attributes
	{
		get
		{
			return _attributes;
		}
		set
		{
			if (value != _attributes)
			{
				_attributes = value;
				OnPropertyChangedWithValue(value, "Attributes");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EncyclopediaSkillVM> OtherSkills
	{
		get
		{
			return _otherSkills;
		}
		set
		{
			if (value != _otherSkills)
			{
				_otherSkills = value;
				OnPropertyChangedWithValue(value, "OtherSkills");
			}
		}
	}

	[DataSourceProperty]
	public bool HasOtherSkills
	{
		get
		{
			return _hasOtherSkills;
		}
		set
		{
			if (value != _hasOtherSkills)
			{
				_hasOtherSkills = value;
				OnPropertyChangedWithValue(value, "HasOtherSkills");
			}
		}
	}

	public HeirSelectionPopupHeroVM(Hero hero)
	{
		Hero = hero;
		FillHeroInformation();
		CreateImageIdentifier();
		CreateHeroModel();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = Hero.Name.ToString();
		Culture = Hero.Culture.Name.ToString();
		Occupation = CampaignUIHelper.GetHeroOccupationName(Hero);
		RelationToMainHero = CampaignUIHelper.GetHeroRelationToHeroText(Hero, Hero.MainHero, uppercaseFirst: true).ToString();
	}

	public override void OnFinalize()
	{
		Model?.OnFinalize();
		ImageIdentifier?.OnFinalize();
		Traits?.ApplyActionOnAllItems(delegate(EncyclopediaTraitItemVM x)
		{
			x.OnFinalize();
		});
		Traits?.Clear();
		Attributes?.ApplyActionOnAllItems(delegate(MarriageOfferPopupHeroAttributeVM x)
		{
			x.OnFinalize();
		});
		Attributes?.Clear();
		OtherSkills?.ApplyActionOnAllItems(delegate(EncyclopediaSkillVM x)
		{
			x.OnFinalize();
		});
		OtherSkills?.Clear();
		base.OnFinalize();
	}

	private void CreateImageIdentifier()
	{
		ImageIdentifier = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(Hero.CharacterObject));
	}

	private void CreateHeroModel()
	{
		Model = new HeroViewModel();
		Model.FillFrom(Hero, -1, useCivilian: false, useCharacteristicIdleAction: true);
		Model.SetEquipment(EquipmentIndex.ArmorItemEndSlot, default(EquipmentElement));
		Model.SetEquipment(EquipmentIndex.HorseHarness, default(EquipmentElement));
		Model.SetEquipment(EquipmentIndex.NumAllWeaponSlots, default(EquipmentElement));
	}

	private void FillHeroInformation()
	{
		Age = (int)Hero.Age;
		Traits = new MBBindingList<EncyclopediaTraitItemVM>();
		Attributes = new MBBindingList<MarriageOfferPopupHeroAttributeVM>();
		OtherSkills = new MBBindingList<EncyclopediaSkillVM>();
		List<CharacterAttribute> list = TaleWorlds.CampaignSystem.Extensions.Attributes.All.ToList();
		list.Sort(CampaignUIHelper.CharacterAttributeComparerInstance);
		foreach (CharacterAttribute item in list)
		{
			Attributes.Add(new MarriageOfferPopupHeroAttributeVM(Hero, item));
		}
		List<SkillObject> list2 = Skills.All.ToList();
		list2.Sort(CampaignUIHelper.SkillObjectComparerInstance);
		foreach (SkillObject skill in list2)
		{
			if (!Attributes.Any((MarriageOfferPopupHeroAttributeVM attribute) => attribute.AttributeSkills.Any((EncyclopediaSkillVM attributeSkill) => attributeSkill.SkillId == skill.StringId)))
			{
				OtherSkills.Add(new EncyclopediaSkillVM(skill, Hero.GetSkillValue(skill)));
			}
		}
		HasOtherSkills = OtherSkills.Count > 0;
		foreach (TraitObject heroTrait in CampaignUIHelper.GetHeroTraits())
		{
			if (Hero.GetTraitLevel(heroTrait) != 0)
			{
				Traits.Add(new EncyclopediaTraitItemVM(heroTrait, Hero));
			}
		}
	}
}
