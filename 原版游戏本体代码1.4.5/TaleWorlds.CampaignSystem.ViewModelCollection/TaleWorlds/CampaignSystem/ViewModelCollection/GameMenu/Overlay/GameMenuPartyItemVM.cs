using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;

public class GameMenuPartyItemVM : ViewModel
{
	private class QuestMarkerComparer : IComparer<QuestMarkerVM>
	{
		public int Compare(QuestMarkerVM x, QuestMarkerVM y)
		{
			return x.QuestMarkerType.CompareTo(y.QuestMarkerType);
		}
	}

	public CharacterObject Character;

	public PartyBase Party;

	public Settlement Settlement;

	private readonly bool _canShowQuest = true;

	private readonly bool _useCivilianEquipment;

	private readonly Action<GameMenuPartyItemVM> _onSetAsContextMenuActiveItem;

	private MBBindingList<QuestMarkerVM> _quests;

	private int _partySize;

	private int _partyWoundedSize;

	private int _shipCount;

	private int _relation = -101;

	private CharacterImageIdentifierVM _visual;

	private BannerImageIdentifierVM _banner_9;

	private string _settlementPath;

	private string _partySizeLbl;

	private string _nameText;

	private string _locationText;

	private string _descriptionText;

	private string _professionText;

	private string _powerText;

	private string _encyclopediaCursorEffect;

	private bool _isIdle;

	private bool _isPlayer;

	private bool _isEnemy;

	private bool _isAlly;

	private bool _isNeutral;

	private bool _isHighlightEnabled;

	private bool _isLeader;

	private bool _isMergedWithArmy;

	private bool _isCharacterInPrison;

	private bool _hasShips;

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
	public MBBindingList<QuestMarkerVM> Quests
	{
		get
		{
			return _quests;
		}
		set
		{
			if (value != _quests)
			{
				_quests = value;
				OnPropertyChangedWithValue(value, "Quests");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHighlightEnabled
	{
		get
		{
			return _isHighlightEnabled;
		}
		set
		{
			if (value != _isHighlightEnabled)
			{
				_isHighlightEnabled = value;
				OnPropertyChangedWithValue(value, "IsHighlightEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCharacterInPrison
	{
		get
		{
			return _isCharacterInPrison;
		}
		set
		{
			if (value != _isCharacterInPrison)
			{
				_isCharacterInPrison = value;
				OnPropertyChangedWithValue(value, "IsCharacterInPrison");
			}
		}
	}

	[DataSourceProperty]
	public bool HasShips
	{
		get
		{
			return _hasShips;
		}
		set
		{
			if (value != _hasShips)
			{
				_hasShips = value;
				OnPropertyChangedWithValue(value, "HasShips");
			}
		}
	}

	[DataSourceProperty]
	public bool IsIdle
	{
		get
		{
			return _isIdle;
		}
		set
		{
			if (value != _isIdle)
			{
				_isIdle = value;
				OnPropertyChangedWithValue(value, "IsIdle");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlayer
	{
		get
		{
			return _isPlayer;
		}
		set
		{
			if (value != _isPlayer)
			{
				_isPlayer = value;
				OnPropertyChanged("IsPlayerParty");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnemy
	{
		get
		{
			return _isEnemy;
		}
		set
		{
			if (value != _isEnemy)
			{
				_isEnemy = value;
				OnPropertyChangedWithValue(value, "IsEnemy");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAlly
	{
		get
		{
			return _isAlly;
		}
		set
		{
			if (value != _isAlly)
			{
				_isAlly = value;
				OnPropertyChangedWithValue(value, "IsAlly");
			}
		}
	}

	[DataSourceProperty]
	public bool IsNeutral
	{
		get
		{
			return _isNeutral;
		}
		set
		{
			if (value != _isNeutral)
			{
				_isNeutral = value;
				OnPropertyChangedWithValue(value, "IsNeutral");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMergedWithArmy
	{
		get
		{
			return _isMergedWithArmy;
		}
		set
		{
			if (value != _isMergedWithArmy)
			{
				_isMergedWithArmy = value;
				OnPropertyChangedWithValue(value, "IsMergedWithArmy");
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
	public string SettlementPath
	{
		get
		{
			return _settlementPath;
		}
		set
		{
			if (value != _settlementPath)
			{
				_settlementPath = value;
				OnPropertyChangedWithValue(value, "SettlementPath");
			}
		}
	}

	[DataSourceProperty]
	public string LocationText
	{
		get
		{
			return _locationText;
		}
		set
		{
			if (value != _locationText)
			{
				_locationText = value;
				OnPropertyChangedWithValue(value, "LocationText");
			}
		}
	}

	[DataSourceProperty]
	public string PowerText
	{
		get
		{
			return _powerText;
		}
		set
		{
			if (value != _powerText)
			{
				_powerText = value;
				OnPropertyChangedWithValue(value, "PowerText");
			}
		}
	}

	[DataSourceProperty]
	public string DescriptionText
	{
		get
		{
			return _descriptionText;
		}
		set
		{
			if (value != _descriptionText)
			{
				_descriptionText = value;
				OnPropertyChangedWithValue(value, "DescriptionText");
			}
		}
	}

	[DataSourceProperty]
	public string ProfessionText
	{
		get
		{
			return _professionText;
		}
		set
		{
			if (value != _professionText)
			{
				_professionText = value;
				OnPropertyChangedWithValue(value, "ProfessionText");
			}
		}
	}

	[DataSourceProperty]
	public string EncyclopediaCursorEffect
	{
		get
		{
			return _encyclopediaCursorEffect;
		}
		set
		{
			if (value != _encyclopediaCursorEffect)
			{
				_encyclopediaCursorEffect = value;
				OnPropertyChangedWithValue(value, "EncyclopediaCursorEffect");
			}
		}
	}

	[DataSourceProperty]
	public CharacterImageIdentifierVM Visual
	{
		get
		{
			return _visual;
		}
		set
		{
			if (value != _visual)
			{
				_visual = value;
				OnPropertyChangedWithValue(value, "Visual");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM Banner_9
	{
		get
		{
			return _banner_9;
		}
		set
		{
			if (value != _banner_9)
			{
				_banner_9 = value;
				OnPropertyChangedWithValue(value, "Banner_9");
			}
		}
	}

	[DataSourceProperty]
	public int PartySize
	{
		get
		{
			return _partySize;
		}
		set
		{
			if (value != _partySize)
			{
				_partySize = value;
				OnPropertyChangedWithValue(value, "PartySize");
			}
		}
	}

	[DataSourceProperty]
	public int PartyWoundedSize
	{
		get
		{
			return _partyWoundedSize;
		}
		set
		{
			if (value != _partySize)
			{
				_partyWoundedSize = value;
				OnPropertyChangedWithValue(value, "PartyWoundedSize");
			}
		}
	}

	[DataSourceProperty]
	public int ShipCount
	{
		get
		{
			return _shipCount;
		}
		set
		{
			if (value != _shipCount)
			{
				_shipCount = value;
				OnPropertyChangedWithValue(value, "ShipCount");
			}
		}
	}

	[DataSourceProperty]
	public string PartySizeLbl
	{
		get
		{
			return _partySizeLbl;
		}
		set
		{
			if (value != _partySizeLbl)
			{
				_partySizeLbl = value;
				OnPropertyChangedWithValue(value, "PartySizeLbl");
			}
		}
	}

	[DataSourceProperty]
	public bool IsLeader
	{
		get
		{
			return _isLeader;
		}
		set
		{
			if (value != _isLeader)
			{
				_isLeader = value;
				OnPropertyChangedWithValue(value, "IsLeader");
			}
		}
	}

	public GameMenuPartyItemVM()
	{
		Visual = new CharacterImageIdentifierVM(null);
		RegisterEvents();
	}

	public GameMenuPartyItemVM(Action<GameMenuPartyItemVM> onSetAsContextMenuActiveItem, Settlement settlement)
	{
		_onSetAsContextMenuActiveItem = onSetAsContextMenuActiveItem;
		Settlement = settlement;
		SettlementComponent settlementComponent = settlement.SettlementComponent;
		SettlementPath = ((settlementComponent == null) ? "placeholder" : (settlementComponent.BackgroundMeshName + "_t"));
		Visual = new CharacterImageIdentifierVM(null);
		NameText = settlement.Name.ToString();
		PartySize = -1;
		PartyWoundedSize = -1;
		PartySizeLbl = "";
		IsPlayer = false;
		IsAlly = false;
		IsEnemy = false;
		Quests = new MBBindingList<QuestMarkerVM>();
		RefreshProperties();
		RegisterEvents();
	}

	public GameMenuPartyItemVM(Action<GameMenuPartyItemVM> onSetAsContextMenuActiveItem, PartyBase item, bool canShowQuest)
	{
		_onSetAsContextMenuActiveItem = onSetAsContextMenuActiveItem;
		Party = item;
		CharacterObject visualPartyLeader = PartyBaseHelper.GetVisualPartyLeader(Party);
		if (visualPartyLeader != null)
		{
			CharacterCode characterCode = CampaignUIHelper.GetCharacterCode(visualPartyLeader);
			Visual = new CharacterImageIdentifierVM(characterCode);
		}
		else
		{
			Visual = new CharacterImageIdentifierVM(null);
		}
		Quests = new MBBindingList<QuestMarkerVM>();
		_canShowQuest = canShowQuest;
		RefreshProperties();
		RegisterEvents();
	}

	public GameMenuPartyItemVM(Action<GameMenuPartyItemVM> onSetAsContextMenuActiveItem, CharacterObject character, bool useCivilianEquipment)
	{
		_onSetAsContextMenuActiveItem = onSetAsContextMenuActiveItem;
		Character = character;
		_useCivilianEquipment = useCivilianEquipment;
		CharacterCode characterCode = CampaignUIHelper.GetCharacterCode(character, useCivilianEquipment);
		Visual = new CharacterImageIdentifierVM(characterCode);
		Hero heroObject = Character.HeroObject;
		Banner_9 = (((heroObject != null && heroObject.IsLord) || (Character.IsHero && Character.HeroObject.Clan == Clan.PlayerClan && character.HeroObject.IsLord)) ? new BannerImageIdentifierVM(Character.HeroObject.ClanBanner, nineGrid: true) : new BannerImageIdentifierVM(null));
		NameText = Character.Name.ToString();
		PartySize = -1;
		PartyWoundedSize = -1;
		PartySizeLbl = "";
		IsPlayer = character.IsPlayerCharacter;
		Quests = new MBBindingList<QuestMarkerVM>();
		RefreshProperties();
		RegisterEvents();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		RefreshProperties();
	}

	public void ExecuteSetAsContextMenuItem()
	{
		_onSetAsContextMenuActiveItem?.Invoke(this);
	}

	public void ExecuteOpenEncyclopedia()
	{
		string encyclopediaPageLink = GetEncyclopediaPageLink();
		if (!string.IsNullOrEmpty(encyclopediaPageLink))
		{
			Campaign.Current.EncyclopediaManager.GoToLink(encyclopediaPageLink);
		}
	}

	public void ExecuteCloseTooltip()
	{
		MBInformationManager.HideInformations();
	}

	public void ExecuteOpenTooltip()
	{
		if (Party?.MobileParty != null)
		{
			InformationManager.ShowTooltip(typeof(MobileParty), Party.MobileParty, true, false);
		}
		else if (Settlement != null)
		{
			InformationManager.ShowTooltip(typeof(Settlement), Settlement);
		}
		else
		{
			InformationManager.ShowTooltip(typeof(Hero), Character.HeroObject, true);
		}
	}

	public void RefreshProperties()
	{
		EncyclopediaCursorEffect = ((!string.IsNullOrEmpty(GetEncyclopediaPageLink())) ? "RightClickLink" : null);
		if (Party != null)
		{
			RefreshCounts();
			Relation = HeroVM.GetRelation(Party.LeaderHero);
			LocationText = " ";
			TextObject name = Party.Name;
			if (Party.IsMobile)
			{
				name = Party.MobileParty.Name;
				float getEncounterJoiningRadius = Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius;
				if (Party.MobileParty.Position.DistanceSquared(MobileParty.MainParty.Position) > getEncounterJoiningRadius * getEncounterJoiningRadius)
				{
					if (Party.MobileParty.MapEvent == null)
					{
						GameTexts.SetVariable("LEFT", GameTexts.FindText("str_distance_to_army_leader"));
						float num = DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(Party.MobileParty, MobileParty.MainParty, Party.MobileParty.NavigationCapability);
						GameTexts.SetVariable("RIGHT", CampaignUIHelper.GetPartyDistanceByTimeText((int)num, Party.MobileParty.Speed));
						LocationText = GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon").ToString();
					}
					else
					{
						TextObject variable = GameTexts.FindText("str_at_map_event");
						TextObject textObject = new TextObject("{=zawBaxl5}Distance : {DISTANCE}");
						textObject.SetTextVariable("DISTANCE", variable);
						LocationText = textObject.ToString();
					}
				}
				DescriptionText = GetPartyDescriptionTextFromValues();
				IsMergedWithArmy = true;
				if (Party.MobileParty.Army != null)
				{
					IsMergedWithArmy = Party.MobileParty.Army.DoesLeaderPartyAndAttachedPartiesContain(Party.MobileParty);
				}
			}
			NameText = name.ToString();
			ProfessionText = " ";
			HasShips = Party.Ships.Count > 0;
		}
		else if (Character != null)
		{
			Relation = HeroVM.GetRelation(Character.HeroObject);
			IsCharacterInPrison = Character.HeroObject?.IsPrisoner ?? false;
			GameTexts.SetVariable("PROFESSION", HeroHelper.GetCharacterTypeName(Character.HeroObject));
			GameTexts.SetVariable("LOCATION", (Character.HeroObject?.CurrentSettlement != null) ? Character.HeroObject.CurrentSettlement.Name.ToString() : "");
			Hero heroObject = Character.HeroObject;
			DescriptionText = ((heroObject != null && !heroObject.IsSpecial) ? GameTexts.FindText("str_character_in_town").ToString() : string.Empty);
			GameTexts.SetVariable("LOCATION", LocationComplex.Current?.GetLocationOfCharacter(Character.HeroObject)?.Name ?? TextObject.GetEmpty());
			LocationText = GameTexts.FindText("str_location_colon").ToString();
			GameTexts.SetVariable("PROFESSION", HeroHelper.GetCharacterTypeName(Character.HeroObject));
			ProfessionText = GameTexts.FindText("str_profession_colon").ToString();
			if (Character.IsHero && Character.HeroObject.IsNotable)
			{
				GameTexts.SetVariable("POWER", Campaign.Current.Models.NotablePowerModel.GetPowerRankName(Character.HeroObject).ToString());
				PowerText = GameTexts.FindText("str_power_colon").ToString();
			}
			NameText = Character.Name.ToString();
			HasShips = false;
		}
		RefreshQuestStatus();
		RefreshRelationStatus();
	}

	public void RefreshQuestStatus()
	{
		Quests.Clear();
		Hero hero = Party?.LeaderHero ?? Character?.HeroObject;
		if (hero != null)
		{
			List<(CampaignUIHelper.IssueQuestFlags, TextObject, TextObject)> questTypes = CampaignUIHelper.GetQuestStateOfHero(hero);
			int i;
			for (i = 0; i < questTypes.Count; i++)
			{
				if (!Quests.Any((QuestMarkerVM q) => q.QuestMarkerType == (int)questTypes[i].Item1))
				{
					Quests.Add(new QuestMarkerVM(questTypes[i].Item1, questTypes[i].Item2, questTypes[i].Item3));
				}
			}
		}
		else if (Party?.MobileParty != null)
		{
			List<QuestBase> questsRelatedToParty = CampaignUIHelper.GetQuestsRelatedToParty(Party.MobileParty);
			for (int num = 0; num < questsRelatedToParty.Count; num++)
			{
				TextObject questHintText = ((questsRelatedToParty[num].JournalEntries.Count > 0) ? questsRelatedToParty[num].JournalEntries[0].LogText : TextObject.GetEmpty());
				CampaignUIHelper.IssueQuestFlags issueQuestFlag = ((hero == null || questsRelatedToParty[num].QuestGiver != hero) ? (questsRelatedToParty[num].IsSpecialQuest ? CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest : CampaignUIHelper.IssueQuestFlags.TrackedIssue) : (questsRelatedToParty[num].IsSpecialQuest ? CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest : CampaignUIHelper.IssueQuestFlags.ActiveIssue));
				Quests.Add(new QuestMarkerVM(issueQuestFlag, questsRelatedToParty[num].Title, questHintText));
			}
		}
		Quests.Sort(new QuestMarkerComparer());
	}

	private void RefreshRelationStatus()
	{
		IsEnemy = false;
		IsAlly = false;
		IsNeutral = false;
		IFaction faction = null;
		bool flag = false;
		if (Character != null)
		{
			IsPlayer = Character.IsPlayerCharacter;
			flag = Character.IsHero && Character.HeroObject.IsNotable;
			faction = (IsPlayer ? null : Character?.HeroObject.MapFaction);
		}
		else if (Party != null)
		{
			IsPlayer = Party.IsMobile && (Party.MobileParty?.IsMainParty ?? false);
			flag = false;
			faction = (IsPlayer ? null : Party?.MobileParty?.MapFaction);
		}
		if (!IsPlayer && faction != null && !flag)
		{
			if (FactionManager.IsAtWarAgainstFaction(faction, Hero.MainHero.MapFaction))
			{
				IsEnemy = true;
			}
			else if (DiplomacyHelper.IsSameFactionAndNotEliminated(faction, Hero.MainHero.MapFaction))
			{
				IsAlly = true;
			}
			else
			{
				IsNeutral = true;
			}
		}
		else if (!IsPlayer)
		{
			IsNeutral = true;
		}
	}

	public void RefreshVisual()
	{
		if (!Visual.IsEmpty)
		{
			return;
		}
		if (Character != null)
		{
			CharacterCode characterCode = CampaignUIHelper.GetCharacterCode(Character, _useCivilianEquipment);
			Visual = new CharacterImageIdentifierVM(characterCode);
		}
		else if (Party != null)
		{
			CharacterObject visualPartyLeader = PartyBaseHelper.GetVisualPartyLeader(Party);
			if (visualPartyLeader != null)
			{
				CharacterCode characterCode2 = CampaignUIHelper.GetCharacterCode(visualPartyLeader);
				Visual = new CharacterImageIdentifierVM(characterCode2);
			}
			else
			{
				Visual = new CharacterImageIdentifierVM(null);
			}
		}
	}

	public void RefreshCounts()
	{
		if (PartySize != Party.NumberOfHealthyMembers || PartyWoundedSize != Party.NumberOfAllMembers - Party.NumberOfHealthyMembers)
		{
			PartyWoundedSize = Party.NumberOfAllMembers - Party.NumberOfHealthyMembers;
			PartySize = Party.NumberOfHealthyMembers;
			MobileParty mobileParty = Party.MobileParty;
			PartySizeLbl = ((mobileParty != null && mobileParty.IsInfoHidden) ? "?" : Party.NumberOfHealthyMembers.ToString());
		}
		ShipCount = Party.Ships?.Count ?? 0;
	}

	public string GetPartyDescriptionTextFromValues()
	{
		GameTexts.SetVariable("newline", "\n");
		string content = ((Party.MobileParty.CurrentSettlement != null && Party.MobileParty.MapEvent == null) ? "" : CampaignUIHelper.GetMobilePartyBehaviorText(Party.MobileParty));
		GameTexts.SetVariable("LEFT", GameTexts.FindText("str_food").ToString());
		GameTexts.SetVariable("RIGHT", Party.MobileParty.Food);
		string content2 = GameTexts.FindText("str_LEFT_colon_RIGHT").ToString();
		GameTexts.SetVariable("LEFT", GameTexts.FindText("str_map_tooltip_speed").ToString());
		GameTexts.SetVariable("RIGHT", Party.MobileParty.Speed.ToString("F"));
		string content3 = GameTexts.FindText("str_LEFT_colon_RIGHT").ToString();
		GameTexts.SetVariable("LEFT", GameTexts.FindText("str_view_distance").ToString());
		GameTexts.SetVariable("RIGHT", Party.MobileParty.SeeingRange);
		string content4 = GameTexts.FindText("str_LEFT_colon_RIGHT").ToString();
		GameTexts.SetVariable("STR1", content);
		GameTexts.SetVariable("STR2", content2);
		string content5 = GameTexts.FindText("str_string_newline_string").ToString();
		GameTexts.SetVariable("STR1", content5);
		GameTexts.SetVariable("STR2", content3);
		content5 = GameTexts.FindText("str_string_newline_string").ToString();
		GameTexts.SetVariable("STR1", content5);
		GameTexts.SetVariable("STR2", content4);
		return GameTexts.FindText("str_string_newline_string").ToString();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		UnregisterEvents();
	}

	private void RegisterEvents()
	{
		CampaignEvents.OnPlayerBodyPropertiesChangedEvent.AddNonSerializedListener(this, OnPlayerCharacterChangedEvent);
	}

	private void OnPlayerCharacterChangedEvent()
	{
		CharacterObject characterObject = ((Party != null) ? PartyBaseHelper.GetVisualPartyLeader(Party) : Character);
		if (characterObject == CharacterObject.PlayerCharacter)
		{
			CharacterCode characterCode = CampaignUIHelper.GetCharacterCode(characterObject);
			Visual = new CharacterImageIdentifierVM(characterCode);
		}
	}

	private void UnregisterEvents()
	{
		CampaignEventDispatcher.Instance.RemoveListeners(this);
	}

	private string GetEncyclopediaPageLink()
	{
		PartyBase party = Party;
		if (party == null || !party.MobileParty.IsCaravan)
		{
			PartyBase party2 = Party;
			if (party2 == null || !party2.MobileParty.IsGarrison)
			{
				PartyBase party3 = Party;
				if (party3 == null || !party3.MobileParty.IsMilitia)
				{
					PartyBase party4 = Party;
					if (party4 == null || !party4.MobileParty.IsVillager)
					{
						if (Character != null)
						{
							return Character.EncyclopediaLink;
						}
						if (Party != null)
						{
							if (Party.LeaderHero != null)
							{
								return Party.LeaderHero.EncyclopediaLink;
							}
							if (Party.Owner != null)
							{
								return Party.Owner.EncyclopediaLink;
							}
							CharacterObject visualPartyLeader = CampaignUIHelper.GetVisualPartyLeader(Party);
							if (visualPartyLeader != null)
							{
								return visualPartyLeader.EncyclopediaLink;
							}
						}
						return null;
					}
				}
			}
		}
		return null;
	}
}
