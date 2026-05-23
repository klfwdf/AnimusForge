using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;

[EncyclopediaViewModel(typeof(Hero))]
public class EncyclopediaHeroPageVM : EncyclopediaContentPageVM
{
	private readonly Hero _hero;

	private readonly TextObject _infoHiddenReasonText;

	private List<Hero> _allRelatedHeroes;

	private readonly HeroRelationComparer _relationAscendingComparer;

	private readonly HeroRelationComparer _relationDescendingComparer;

	private const int _alliesEnemiesCapacity = 13;

	private MBBindingList<HeroVM> _enemies;

	private MBBindingList<HeroVM> _allies;

	private MBBindingList<EncyclopediaFamilyMemberVM> _family;

	private MBBindingList<HeroVM> _companions;

	private MBBindingList<EncyclopediaSettlementVM> _settlements;

	private MBBindingList<EncyclopediaDwellingVM> _dwellings;

	private MBBindingList<EncyclopediaHistoryEventVM> _history;

	private MBBindingList<EncyclopediaSkillVM> _skills;

	private MBBindingList<StringPairItemVM> _stats;

	private MBBindingList<EncyclopediaTraitItemVM> _traits;

	private string _clanText;

	private string _settlementsText;

	private string _dwellingsText;

	private string _alliesText;

	private string _enemiesText;

	private string _companionsText;

	private string _lastSeenText;

	private string _nameText;

	private string _informationText;

	private string _deceasedText;

	private string _traitsText;

	private string _skillsText;

	private string _infoText;

	private string _kingdomRankText;

	private string _familyText;

	private HeroViewModel _heroCharacter;

	private bool _isCompanion;

	private bool _isPregnant;

	private bool _hasNeutralClan;

	private bool _isDead;

	private bool _isInformationHidden;

	private HeroVM _master;

	private EncyclopediaFactionVM _faction;

	private string _masterText;

	private HintViewModel _pregnantHint;

	private bool _hasAnySkills;

	private MBBindingList<HeroVM> _additionalAllies;

	private MBBindingList<HeroVM> _additionalEnemies;

	private bool _anyAdditionalAllies;

	private bool _anyAdditionalEnemies;

	private string _additionalAlliesString;

	private string _additionalEnemiesString;

	private BasicTooltipViewModel _additionalAlliesHint;

	private BasicTooltipViewModel _additionalEnemiesHint;

	[DataSourceProperty]
	public EncyclopediaFactionVM Faction
	{
		get
		{
			return _faction;
		}
		set
		{
			if (value != _faction)
			{
				_faction = value;
				OnPropertyChangedWithValue(value, "Faction");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCompanion
	{
		get
		{
			return _isCompanion;
		}
		set
		{
			if (value != _isCompanion)
			{
				_isCompanion = value;
				OnPropertyChangedWithValue(value, "IsCompanion");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPregnant
	{
		get
		{
			return _isPregnant;
		}
		set
		{
			if (value != _isPregnant)
			{
				_isPregnant = value;
				OnPropertyChangedWithValue(value, "IsPregnant");
			}
		}
	}

	[DataSourceProperty]
	public HeroVM Master
	{
		get
		{
			return _master;
		}
		set
		{
			if (value != _master)
			{
				_master = value;
				OnPropertyChangedWithValue(value, "Master");
			}
		}
	}

	[DataSourceProperty]
	public string ClanText
	{
		get
		{
			return _clanText;
		}
		set
		{
			if (value != _clanText)
			{
				_clanText = value;
				OnPropertyChangedWithValue(value, "ClanText");
			}
		}
	}

	[DataSourceProperty]
	public string InfoText
	{
		get
		{
			return _infoText;
		}
		set
		{
			if (value != _infoText)
			{
				_infoText = value;
				OnPropertyChangedWithValue(value, "InfoText");
			}
		}
	}

	[DataSourceProperty]
	public string TraitsText
	{
		get
		{
			return _traitsText;
		}
		set
		{
			if (value != _traitsText)
			{
				_traitsText = value;
				OnPropertyChangedWithValue(value, "TraitsText");
			}
		}
	}

	[DataSourceProperty]
	public string MasterText
	{
		get
		{
			return _masterText;
		}
		set
		{
			if (value != _masterText)
			{
				_masterText = value;
				OnPropertyChangedWithValue(value, "MasterText");
			}
		}
	}

	[DataSourceProperty]
	public string KingdomRankText
	{
		get
		{
			return _kingdomRankText;
		}
		set
		{
			if (value != _kingdomRankText)
			{
				_kingdomRankText = value;
				OnPropertyChangedWithValue(value, "KingdomRankText");
			}
		}
	}

	[DataSourceProperty]
	public string InfoHiddenReasonText => _infoHiddenReasonText.ToString();

	[DataSourceProperty]
	public string SkillsText
	{
		get
		{
			return _skillsText;
		}
		set
		{
			if (value != _skillsText)
			{
				_skillsText = value;
				OnPropertyChangedWithValue(value, "SkillsText");
			}
		}
	}

	[DataSourceProperty]
	public HeroViewModel HeroCharacter
	{
		get
		{
			return _heroCharacter;
		}
		set
		{
			if (value != _heroCharacter)
			{
				_heroCharacter = value;
				OnPropertyChangedWithValue(value, "HeroCharacter");
			}
		}
	}

	[DataSourceProperty]
	public string LastSeenText
	{
		get
		{
			return _lastSeenText;
		}
		set
		{
			if (value != _lastSeenText)
			{
				_lastSeenText = value;
				OnPropertyChangedWithValue(value, "LastSeenText");
			}
		}
	}

	[DataSourceProperty]
	public string DeceasedText
	{
		get
		{
			return _deceasedText;
		}
		set
		{
			if (value != _deceasedText)
			{
				_deceasedText = value;
				OnPropertyChangedWithValue(value, "DeceasedText");
			}
		}
	}

	[DataSourceProperty]
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (value != _nameText)
			{
				_nameText = value;
				OnPropertyChangedWithValue(value, "NameText");
			}
		}
	}

	[DataSourceProperty]
	public string SettlementsText
	{
		get
		{
			return _settlementsText;
		}
		set
		{
			if (value != _settlementsText)
			{
				_settlementsText = value;
				OnPropertyChangedWithValue(value, "SettlementsText");
			}
		}
	}

	[DataSourceProperty]
	public string DwellingsText
	{
		get
		{
			return _dwellingsText;
		}
		set
		{
			if (value != _dwellingsText)
			{
				_dwellingsText = value;
				OnPropertyChangedWithValue(value, "DwellingsText");
			}
		}
	}

	[DataSourceProperty]
	public string CompanionsText
	{
		get
		{
			return _companionsText;
		}
		set
		{
			if (value != _companionsText)
			{
				_companionsText = value;
				OnPropertyChangedWithValue(value, "CompanionsText");
			}
		}
	}

	[DataSourceProperty]
	public string AlliesText
	{
		get
		{
			return _alliesText;
		}
		set
		{
			if (value != _alliesText)
			{
				_alliesText = value;
				OnPropertyChangedWithValue(value, "AlliesText");
			}
		}
	}

	[DataSourceProperty]
	public string EnemiesText
	{
		get
		{
			return _enemiesText;
		}
		set
		{
			if (value != _enemiesText)
			{
				_enemiesText = value;
				OnPropertyChangedWithValue(value, "EnemiesText");
			}
		}
	}

	[DataSourceProperty]
	public string FamilyText
	{
		get
		{
			return _familyText;
		}
		set
		{
			if (value != _familyText)
			{
				_familyText = value;
				OnPropertyChangedWithValue(value, "FamilyText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<StringPairItemVM> Stats
	{
		get
		{
			return _stats;
		}
		set
		{
			if (value != _stats)
			{
				_stats = value;
				OnPropertyChangedWithValue(value, "Stats");
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
	public MBBindingList<EncyclopediaSkillVM> Skills
	{
		get
		{
			return _skills;
		}
		set
		{
			if (value != _skills)
			{
				_skills = value;
				OnPropertyChangedWithValue(value, "Skills");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EncyclopediaDwellingVM> Dwellings
	{
		get
		{
			return _dwellings;
		}
		set
		{
			if (value != _dwellings)
			{
				_dwellings = value;
				OnPropertyChangedWithValue(value, "Dwellings");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EncyclopediaSettlementVM> Settlements
	{
		get
		{
			return _settlements;
		}
		set
		{
			if (value != _settlements)
			{
				_settlements = value;
				OnPropertyChangedWithValue(value, "Settlements");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EncyclopediaFamilyMemberVM> Family
	{
		get
		{
			return _family;
		}
		set
		{
			if (value != _family)
			{
				_family = value;
				OnPropertyChangedWithValue(value, "Family");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<HeroVM> Companions
	{
		get
		{
			return _companions;
		}
		set
		{
			if (value != _companions)
			{
				_companions = value;
				OnPropertyChangedWithValue(value, "Companions");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<HeroVM> Enemies
	{
		get
		{
			return _enemies;
		}
		set
		{
			if (value != _enemies)
			{
				_enemies = value;
				OnPropertyChangedWithValue(value, "Enemies");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<HeroVM> Allies
	{
		get
		{
			return _allies;
		}
		set
		{
			if (value != _allies)
			{
				_allies = value;
				OnPropertyChangedWithValue(value, "Allies");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EncyclopediaHistoryEventVM> History
	{
		get
		{
			return _history;
		}
		set
		{
			if (value != _history)
			{
				_history = value;
				OnPropertyChangedWithValue(value, "History");
			}
		}
	}

	[DataSourceProperty]
	public bool HasNeutralClan
	{
		get
		{
			return _hasNeutralClan;
		}
		set
		{
			if (value != _hasNeutralClan)
			{
				_hasNeutralClan = value;
				OnPropertyChangedWithValue(value, "HasNeutralClan");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDead
	{
		get
		{
			return _isDead;
		}
		set
		{
			if (value != _isDead)
			{
				_isDead = value;
				OnPropertyChanged("IsAlive");
				OnPropertyChangedWithValue(value, "IsDead");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInformationHidden
	{
		get
		{
			return _isInformationHidden;
		}
		set
		{
			if (value != _isInformationHidden)
			{
				_isInformationHidden = value;
				OnPropertyChangedWithValue(value, "IsInformationHidden");
			}
		}
	}

	[DataSourceProperty]
	public string InformationText
	{
		get
		{
			return _informationText;
		}
		set
		{
			if (value != _informationText)
			{
				_informationText = value;
				OnPropertyChangedWithValue(value, "InformationText");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel PregnantHint
	{
		get
		{
			return _pregnantHint;
		}
		set
		{
			if (value != _pregnantHint)
			{
				_pregnantHint = value;
				OnPropertyChangedWithValue(value, "PregnantHint");
			}
		}
	}

	[DataSourceProperty]
	public bool HasAnySkills
	{
		get
		{
			return _hasAnySkills;
		}
		set
		{
			if (value != _hasAnySkills)
			{
				_hasAnySkills = value;
				OnPropertyChangedWithValue(value, "HasAnySkills");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<HeroVM> AdditionalEnemies
	{
		get
		{
			return _additionalEnemies;
		}
		set
		{
			if (value != _additionalEnemies)
			{
				_additionalEnemies = value;
				OnPropertyChangedWithValue(value, "AdditionalEnemies");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<HeroVM> AdditionalAllies
	{
		get
		{
			return _additionalAllies;
		}
		set
		{
			if (value != _additionalAllies)
			{
				_additionalAllies = value;
				OnPropertyChangedWithValue(value, "AdditionalAllies");
			}
		}
	}

	[DataSourceProperty]
	public bool AnyAdditionalAllies
	{
		get
		{
			return _anyAdditionalAllies;
		}
		set
		{
			if (value != _anyAdditionalAllies)
			{
				_anyAdditionalAllies = value;
				OnPropertyChangedWithValue(value, "AnyAdditionalAllies");
			}
		}
	}

	[DataSourceProperty]
	public bool AnyAdditionalEnemies
	{
		get
		{
			return _anyAdditionalEnemies;
		}
		set
		{
			if (value != _anyAdditionalEnemies)
			{
				_anyAdditionalEnemies = value;
				OnPropertyChangedWithValue(value, "AnyAdditionalEnemies");
			}
		}
	}

	[DataSourceProperty]
	public string AdditionalAlliesString
	{
		get
		{
			return _additionalAlliesString;
		}
		set
		{
			if (value != _additionalAlliesString)
			{
				_additionalAlliesString = value;
				OnPropertyChangedWithValue(value, "AdditionalAlliesString");
			}
		}
	}

	[DataSourceProperty]
	public string AdditionalEnemiesString
	{
		get
		{
			return _additionalEnemiesString;
		}
		set
		{
			if (value != _additionalEnemiesString)
			{
				_additionalEnemiesString = value;
				OnPropertyChangedWithValue(value, "AdditionalEnemiesString");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel AdditionalAlliesHint
	{
		get
		{
			return _additionalAlliesHint;
		}
		set
		{
			if (value != _additionalAlliesHint)
			{
				_additionalAlliesHint = value;
				OnPropertyChangedWithValue(value, "AdditionalAlliesHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel AdditionalEnemiesHint
	{
		get
		{
			return _additionalEnemiesHint;
		}
		set
		{
			if (value != _additionalEnemiesHint)
			{
				_additionalEnemiesHint = value;
				OnPropertyChangedWithValue(value, "AdditionalEnemiesHint");
			}
		}
	}

	public EncyclopediaHeroPageVM(EncyclopediaPageArgs args)
		: base(args)
	{
		_hero = base.Obj as Hero;
		_relationAscendingComparer = new HeroRelationComparer(_hero, isAscending: true, showLeadersFirst: true);
		_relationDescendingComparer = new HeroRelationComparer(_hero, isAscending: false, showLeadersFirst: true);
		IsInformationHidden = CampaignUIHelper.IsHeroInformationHidden(_hero, out var disableReason);
		_infoHiddenReasonText = disableReason;
		_allRelatedHeroes = new List<Hero> { _hero.Father, _hero.Mother, _hero.Spouse };
		_allRelatedHeroes.AddRange(_hero.Siblings);
		_allRelatedHeroes.AddRange(_hero.ExSpouses);
		_allRelatedHeroes.AddRange(CampaignUIHelper.GetChildrenAndGrandchildrenOfHero(_hero));
		StringHelpers.SetCharacterProperties("NPC", _hero.CharacterObject);
		Settlements = new MBBindingList<EncyclopediaSettlementVM>();
		Dwellings = new MBBindingList<EncyclopediaDwellingVM>();
		Allies = new MBBindingList<HeroVM>();
		AdditionalAllies = new MBBindingList<HeroVM>();
		Enemies = new MBBindingList<HeroVM>();
		AdditionalEnemies = new MBBindingList<HeroVM>();
		Family = new MBBindingList<EncyclopediaFamilyMemberVM>();
		Companions = new MBBindingList<HeroVM>();
		History = new MBBindingList<EncyclopediaHistoryEventVM>();
		Skills = new MBBindingList<EncyclopediaSkillVM>();
		Stats = new MBBindingList<StringPairItemVM>();
		Traits = new MBBindingList<EncyclopediaTraitItemVM>();
		HeroCharacter = new HeroViewModel(CharacterViewModel.StanceTypes.EmphasizeFace);
		base.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(_hero);
		Faction = new EncyclopediaFactionVM(_hero.Clan);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ClanText = GameTexts.FindText("str_clan").ToString();
		AlliesText = GameTexts.FindText("str_friends").ToString();
		EnemiesText = GameTexts.FindText("str_enemies").ToString();
		FamilyText = GameTexts.FindText("str_family_group").ToString();
		CompanionsText = GameTexts.FindText("str_companions").ToString();
		DwellingsText = GameTexts.FindText("str_dwellings").ToString();
		SettlementsText = GameTexts.FindText("str_settlements").ToString();
		DeceasedText = GameTexts.FindText("str_encyclopedia_deceased").ToString();
		TraitsText = GameTexts.FindText("str_traits_group").ToString();
		SkillsText = GameTexts.FindText("str_skills").ToString();
		InfoText = GameTexts.FindText("str_info").ToString();
		PregnantHint = new HintViewModel(GameTexts.FindText("str_pregnant"));
		UpdateBookmarkHintText();
		UpdateInformationText();
		Refresh();
	}

	public override void Refresh()
	{
		base.IsLoadingOver = false;
		Settlements.Clear();
		Dwellings.Clear();
		Allies.Clear();
		Enemies.Clear();
		AdditionalAllies.Clear();
		AdditionalEnemies.Clear();
		Companions.Clear();
		Family.Clear();
		History.Clear();
		Skills.Clear();
		Stats.Clear();
		Traits.Clear();
		NameText = _hero.Name.ToString();
		string text = GameTexts.FindText("str_missing_info_indicator").ToString();
		EncyclopediaPage pageOf = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Hero));
		HasNeutralClan = _hero.Clan == null;
		if (!IsInformationHidden)
		{
			List<SkillObject> list = TaleWorlds.CampaignSystem.Extensions.Skills.All.ToList();
			list.Sort(CampaignUIHelper.SkillObjectComparerInstance);
			foreach (SkillObject item3 in list)
			{
				if (_hero.GetSkillValue(item3) >= 50)
				{
					Skills.Add(new EncyclopediaSkillVM(item3, _hero.GetSkillValue(item3)));
				}
			}
			foreach (TraitObject heroTrait in CampaignUIHelper.GetHeroTraits())
			{
				if (_hero.GetTraitLevel(heroTrait) != 0)
				{
					Traits.Add(new EncyclopediaTraitItemVM(heroTrait, _hero));
				}
			}
			if (_hero.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge)
			{
				for (int i = 0; i < Hero.AllAliveHeroes.Count; i++)
				{
					AddHeroToRelatedVMList(Hero.AllAliveHeroes[i]);
				}
				for (int j = 0; j < Hero.DeadOrDisabledHeroes.Count; j++)
				{
					AddHeroToRelatedVMList(Hero.DeadOrDisabledHeroes[j]);
				}
				Allies.Sort(_relationDescendingComparer);
				Enemies.Sort(_relationAscendingComparer);
				while (Allies.Count > 13)
				{
					HeroVM item = Allies[13];
					Allies.Remove(item);
					AdditionalAllies.Add(item);
				}
				while (Enemies.Count > 13)
				{
					HeroVM item2 = Enemies[13];
					Enemies.Remove(item2);
					AdditionalEnemies.Add(item2);
				}
				OnAdditionalListsUpdated();
			}
			if (_hero.Clan != null && _hero == _hero.Clan.Leader)
			{
				for (int k = 0; k < _hero.Clan.Companions.Count; k++)
				{
					Hero hero = _hero.Clan.Companions[k];
					Companions.Add(new HeroVM(hero));
				}
			}
			for (int l = 0; l < _allRelatedHeroes.Count; l++)
			{
				Hero hero2 = _allRelatedHeroes[l];
				if (hero2 != null && pageOf.IsValidEncyclopediaItem(hero2))
				{
					Family.Add(new EncyclopediaFamilyMemberVM(hero2, _hero));
				}
			}
			for (int m = 0; m < _hero.OwnedWorkshops.Count; m++)
			{
				Dwellings.Add(new EncyclopediaDwellingVM(_hero.OwnedWorkshops[m].WorkshopType));
			}
			EncyclopediaPage pageOf2 = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Settlement));
			for (int n = 0; n < Settlement.All.Count; n++)
			{
				Settlement settlement = Settlement.All[n];
				if (settlement.OwnerClan != null && settlement.OwnerClan.Leader == _hero && pageOf2.IsValidEncyclopediaItem(settlement))
				{
					Settlements.Add(new EncyclopediaSettlementVM(settlement));
				}
			}
		}
		HasAnySkills = Skills.Count > 0;
		if (_hero.Culture != null)
		{
			string definition = GameTexts.FindText("str_enc_sf_culture").ToString();
			Stats.Add(new StringPairItemVM(definition, _hero.Culture.Name.ToString()));
		}
		string definition2 = GameTexts.FindText("str_enc_sf_age").ToString();
		Stats.Add(new StringPairItemVM(definition2, IsInformationHidden ? text : ((int)_hero.Age).ToString()));
		for (int num = Campaign.Current.LogEntryHistory.GameActionLogs.Count - 1; num >= 0; num--)
		{
			if (Campaign.Current.LogEntryHistory.GameActionLogs[num] is IEncyclopediaLog encyclopediaLog && encyclopediaLog.IsVisibleInEncyclopediaPageOf(_hero))
			{
				History.Add(new EncyclopediaHistoryEventVM(encyclopediaLog));
			}
		}
		if (!_hero.IsNotable && !_hero.IsWanderer && _hero.Clan?.Kingdom != null)
		{
			KingdomRankText = CampaignUIHelper.GetHeroKingdomRank(_hero);
		}
		string heroOccupationName = CampaignUIHelper.GetHeroOccupationName(_hero);
		if (!string.IsNullOrEmpty(heroOccupationName))
		{
			string definition3 = GameTexts.FindText("str_enc_sf_occupation").ToString();
			Stats.Add(new StringPairItemVM(definition3, IsInformationHidden ? text : heroOccupationName));
		}
		if (_hero != Hero.MainHero)
		{
			string definition4 = GameTexts.FindText("str_enc_sf_relation").ToString();
			Stats.Add(new StringPairItemVM(definition4, IsInformationHidden ? text : _hero.GetRelationWithPlayer().ToString()));
		}
		LastSeenText = ((_hero == Hero.MainHero) ? "" : HeroHelper.GetLastSeenText(_hero).ToString());
		HeroCharacter.FillFrom(_hero, -1, _hero.IsNotable, useCharacteristicIdleAction: true);
		HeroCharacter.SetEquipment(EquipmentIndex.ArmorItemEndSlot, default(EquipmentElement));
		HeroCharacter.SetEquipment(EquipmentIndex.HorseHarness, default(EquipmentElement));
		HeroCharacter.SetEquipment(EquipmentIndex.NumAllWeaponSlots, default(EquipmentElement));
		IsCompanion = _hero.CompanionOf != null;
		if (IsCompanion)
		{
			MasterText = GameTexts.FindText("str_companion_of").ToString();
			Master = new HeroVM(_hero.CompanionOf?.Leader);
		}
		IsPregnant = _hero.IsPregnant;
		IsDead = !_hero.IsAlive;
		base.IsLoadingOver = true;
	}

	private void AddHeroToRelatedVMList(Hero hero)
	{
		if (Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Hero)).IsValidEncyclopediaItem(hero) && !hero.IsNotable && hero != _hero && hero.IsAlive && hero.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && !_allRelatedHeroes.Contains(hero))
		{
			if (_hero.IsFriend(hero))
			{
				Allies.Add(new HeroVM(hero));
			}
			else if (_hero.IsEnemy(hero))
			{
				Enemies.Add(new HeroVM(hero));
			}
		}
	}

	public override string GetName()
	{
		return _hero.Name.ToString();
	}

	public override string GetNavigationBarURL()
	{
		return string.Concat(string.Concat(string.Concat(HyperlinkTexts.GetGenericHyperlinkText("Home", GameTexts.FindText("str_encyclopedia_home").ToString()) + " \\ ", HyperlinkTexts.GetGenericHyperlinkText("ListPage-Heroes", GameTexts.FindText("str_encyclopedia_heroes").ToString())), " \\ "), GetName());
	}

	public void ExecuteLink(string link)
	{
		Campaign.Current.EncyclopediaManager.GoToLink(link);
	}

	public override void ExecuteSwitchBookmarkedState()
	{
		base.ExecuteSwitchBookmarkedState();
		if (base.IsBookmarked)
		{
			Campaign.Current.EncyclopediaManager.ViewDataTracker.AddEncyclopediaBookmarkToItem(_hero);
		}
		else
		{
			Campaign.Current.EncyclopediaManager.ViewDataTracker.RemoveEncyclopediaBookmarkFromItem(_hero);
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		HeroCharacter.OnFinalize();
	}

	private void UpdateInformationText()
	{
		InformationText = "";
		if (!TextObject.IsNullOrEmpty(_hero.EncyclopediaText))
		{
			InformationText = _hero.EncyclopediaText.ToString();
		}
		else if (_hero.CharacterObject.Occupation == Occupation.Lord)
		{
			InformationText = Hero.SetHeroEncyclopediaTextAndLinks(_hero).ToString();
		}
	}

	private void OnAdditionalListsUpdated()
	{
		AnyAdditionalAllies = AdditionalAllies.Count > 0;
		AnyAdditionalEnemies = AdditionalEnemies.Count > 0;
		AdditionalAlliesString = (AnyAdditionalAllies ? new TextObject("{=!}+{REMAINING}").SetTextVariable("REMAINING", AdditionalAllies.Count).ToString() : string.Empty);
		AdditionalEnemiesString = (AnyAdditionalEnemies ? new TextObject("{=!}+{REMAINING}").SetTextVariable("REMAINING", AdditionalEnemies.Count).ToString() : string.Empty);
		AdditionalAlliesHint = new BasicTooltipViewModel(() => GetOverflowTooltip(AdditionalAllies));
		AdditionalEnemiesHint = new BasicTooltipViewModel(() => GetOverflowTooltip(AdditionalEnemies));
	}

	private List<TooltipProperty> GetOverflowTooltip(MBBindingList<HeroVM> overflowList)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		foreach (HeroVM overflow in overflowList)
		{
			list.Add(new TooltipProperty(string.Empty, overflow.NameText, 0));
		}
		return list;
	}
}
