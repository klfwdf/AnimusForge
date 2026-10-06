using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MarriageOfferPopup;

public class MarriageOfferPopupHeroVM : ViewModel
{
	private bool _modelCreated;

	private string _encyclopediaLinkWithName;

	private string _ageString;

	private string _occupationString;

	private int _relation;

	private string _clanName;

	private BannerImageIdentifierVM _clanBanner;

	private HeroViewModel _model;

	private MBBindingList<EncyclopediaTraitItemVM> _traits;

	private MBBindingList<MarriageOfferPopupHeroAttributeVM> _attributes;

	private MBBindingList<EncyclopediaSkillVM> _otherSkills;

	private bool _hasOtherSkills;

	public Hero Hero { get; }

	[DataSourceProperty]
	public string EncyclopediaLinkWithName
	{
		get
		{
			return _encyclopediaLinkWithName;
		}
		set
		{
			if (value != _encyclopediaLinkWithName)
			{
				_encyclopediaLinkWithName = value;
				OnPropertyChangedWithValue(value, "EncyclopediaLinkWithName");
			}
		}
	}

	[DataSourceProperty]
	public string AgeString
	{
		get
		{
			return _ageString;
		}
		set
		{
			if (value != _ageString)
			{
				_ageString = value;
				OnPropertyChangedWithValue(value, "AgeString");
			}
		}
	}

	[DataSourceProperty]
	public string OccupationString
	{
		get
		{
			return _occupationString;
		}
		set
		{
			if (value != _occupationString)
			{
				_occupationString = value;
				OnPropertyChangedWithValue(value, "OccupationString");
			}
		}
	}

	[DataSourceProperty]
	public int Relation
	{
		get
		{
			return _relation;
		}
		set
		{
			if (value != _relation)
			{
				_relation = value;
				OnPropertyChangedWithValue(value, "Relation");
			}
		}
	}

	[DataSourceProperty]
	public string ClanName
	{
		get
		{
			return _clanName;
		}
		set
		{
			if (value != _clanName)
			{
				_clanName = value;
				OnPropertyChangedWithValue(value, "ClanName");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM ClanBanner
	{
		get
		{
			return _clanBanner;
		}
		set
		{
			if (value != _clanBanner)
			{
				_clanBanner = value;
				OnPropertyChangedWithValue(value, "ClanBanner");
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

	public MarriageOfferPopupHeroVM(Hero hero)
	{
		Hero = hero;
		Model = new HeroViewModel();
		FillHeroInformation();
		CreateClanBanner();
		RefreshValues();
	}

	public void Update()
	{
		if (!_modelCreated && !CampaignUIHelper.IsHeroInformationHidden(Hero, out var _))
		{
			_modelCreated = true;
			CreateHeroModel();
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		EncyclopediaLinkWithName = Hero.EncyclopediaLinkWithName.ToString();
		AgeString = ((int)Hero.Age).ToString();
		OccupationString = CampaignUIHelper.GetHeroOccupationName(Hero);
		Relation = (int)Hero.GetRelationWithPlayer();
	}

	public override void OnFinalize()
	{
		ClanBanner?.OnFinalize();
		Model?.OnFinalize();
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

	public void ExecuteHeroLink()
	{
		Campaign.Current.EncyclopediaManager.GoToLink(Hero.EncyclopediaLink);
	}

	public void ExecuteClanLink()
	{
		Campaign.Current.EncyclopediaManager.GoToLink(Hero.Clan.EncyclopediaLink);
	}

	private void CreateClanBanner()
	{
		ClanName = Hero.Clan.Name.ToString();
		ClanBanner = new BannerImageIdentifierVM(Hero.ClanBanner, nineGrid: true);
	}

	private void CreateHeroModel()
	{
		Model.FillFrom(Hero, -1, useCivilian: true, useCharacteristicIdleAction: true);
		Model.SetEquipment(EquipmentIndex.ArmorItemEndSlot, default(EquipmentElement));
		Model.SetEquipment(EquipmentIndex.HorseHarness, default(EquipmentElement));
		Model.SetEquipment(EquipmentIndex.NumAllWeaponSlots, default(EquipmentElement));
	}

	private void FillHeroInformation()
	{
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
