using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem;

public sealed class CultureObject : BasicCultureObject
{
	public enum BoardGameType
	{
		None = -1,
		Seega,
		Puluc,
		Konane,
		MuTorere,
		Tablut,
		BaghChal,
		Total
	}

	private MBList<TextObject> _maleNameList;

	private MBList<TextObject> _femaleNameList;

	private MBList<TextObject> _clanNameList;

	private MBList<FeatObject> _cultureFeats;

	private MBList<PolicyObject> _defaultPolicyList;

	public CultureTrait[] Traits { get; private set; }

	public CharacterObject BasicTroop { get; private set; }

	public CharacterObject EliteBasicTroop { get; private set; }

	public CharacterObject MeleeMilitiaTroop { get; private set; }

	public CharacterObject MeleeEliteMilitiaTroop { get; private set; }

	public CharacterObject RangedEliteMilitiaTroop { get; private set; }

	public CharacterObject RangedMilitiaTroop { get; private set; }

	public CharacterObject TournamentMaster { get; private set; }

	public CharacterObject Villager { get; private set; }

	public CharacterObject CaravanMaster { get; private set; }

	public CharacterObject CaravanGuard { get; private set; }

	public CharacterObject PrisonGuard { get; private set; }

	public CharacterObject Guard { get; private set; }

	public CharacterObject Blacksmith { get; private set; }

	public CharacterObject Weaponsmith { get; private set; }

	public CharacterObject Townswoman { get; private set; }

	public CharacterObject TownswomanInfant { get; private set; }

	public CharacterObject TownswomanChild { get; private set; }

	public CharacterObject TownswomanTeenager { get; private set; }

	public CharacterObject VillageWoman { get; private set; }

	public CharacterObject VillagerMaleChild { get; private set; }

	public CharacterObject VillagerMaleTeenager { get; private set; }

	public CharacterObject VillagerFemaleChild { get; private set; }

	public CharacterObject VillagerFemaleTeenager { get; private set; }

	public CharacterObject Townsman { get; private set; }

	public CharacterObject TownsmanInfant { get; private set; }

	public CharacterObject TownsmanChild { get; private set; }

	public CharacterObject TownsmanTeenager { get; private set; }

	public CharacterObject RansomBroker { get; private set; }

	public CharacterObject GangleaderBodyguard { get; private set; }

	public CharacterObject MerchantNotary { get; private set; }

	public CharacterObject ArtisanNotary { get; private set; }

	public CharacterObject PreacherNotary { get; private set; }

	public CharacterObject RuralNotableNotary { get; private set; }

	public CharacterObject ShopWorker { get; private set; }

	public CharacterObject Tavernkeeper { get; private set; }

	public CharacterObject TavernGamehost { get; private set; }

	public CharacterObject Musician { get; private set; }

	public CharacterObject TavernWench { get; private set; }

	public CharacterObject Armorer { get; private set; }

	public CharacterObject HorseMerchant { get; private set; }

	public CharacterObject Barber { get; private set; }

	public CharacterObject Merchant { get; private set; }

	public CharacterObject Beggar { get; private set; }

	public CharacterObject FemaleBeggar { get; private set; }

	public CharacterObject FemaleDancer { get; private set; }

	public CharacterObject Shipwright { get; private set; }

	public CharacterObject ShipyardWorker { get; private set; }

	public CharacterObject MilitiaVeteranArcher { get; private set; }

	public CharacterObject GearDummy { get; private set; }

	public MBEquipmentRoster DefaultBattleEquipmentRoster { get; private set; }

	public MBEquipmentRoster DefaultCivilianEquipmentRoster { get; private set; }

	public MBEquipmentRoster DefaultStealthEquipmentRoster { get; private set; }

	public MBEquipmentRoster DuelPresetEquipmentRoster { get; private set; }

	public MBEquipmentRoster MarriageBrideEquipmentRoster { get; private set; }

	public CharacterObject BanditChief { get; private set; }

	public CharacterObject BanditRaider { get; private set; }

	public CharacterObject BanditBandit { get; private set; }

	public CharacterObject BanditBoss { get; private set; }

	public MBBodyProperty DefaultCharacterCreationBodyProperty { get; private set; }

	public TextObject EncyclopediaText { get; private set; }

	public CampaignVec2 StartingPoint { get; private set; }

	public PartyTemplateObject DefaultPartyTemplate { get; private set; }

	public PartyTemplateObject VillagerPartyTemplate { get; private set; }

	public PartyTemplateObject FishingPartyTemplate { get; private set; }

	public PartyTemplateObject MilitiaPartyTemplate { get; private set; }

	public PartyTemplateObject RebelsPartyTemplate { get; private set; }

	public MBList<PartyTemplateObject> CaravanPartyTemplates { get; private set; }

	public MBList<PartyTemplateObject> EliteCaravanPartyTemplates { get; private set; }

	public MBList<ShipHull> AvailableShipHulls { get; private set; }

	public PartyTemplateObject BanditBossPartyTemplate { get; private set; }

	public PartyTemplateObject VassalRewardTroopsPartyTemplate { get; private set; }

	public PartyTemplateObject SettlementPatrolPartyTemplateWeak { get; private set; }

	public PartyTemplateObject SettlementPatrolPartyTemplateModerate { get; private set; }

	public PartyTemplateObject SettlementPatrolPartyTemplateStrong { get; private set; }

	public PartyTemplateObject SettlementPatrolPartyTemplateNaval { get; private set; }

	public MBReadOnlyList<ItemObject> VassalRewardItems { get; private set; }

	public MBReadOnlyList<ItemObject> BannerBearerReplacementWeapons { get; private set; }

	public MBReadOnlyList<TextObject> MaleNameList => _maleNameList;

	public MBReadOnlyList<TextObject> FemaleNameList => _femaleNameList;

	public MBReadOnlyList<TextObject> ClanNameList => _clanNameList;

	public MBReadOnlyList<FeatObject> CultureFeats => _cultureFeats;

	public MBReadOnlyList<PolicyObject> DefaultPolicyList => _defaultPolicyList;

	public MBReadOnlyList<int> PossibleClanBannerIconsIDs { get; private set; }

	public MBReadOnlyList<CharacterObject> NotableTemplates { get; private set; }

	public MBReadOnlyList<CharacterObject> RebelliousHeroTemplates { get; private set; }

	public MBReadOnlyList<CharacterObject> LordTemplates { get; private set; }

	public MBReadOnlyList<CharacterObject> TournamentTeamTemplatesForOneParticipant { get; private set; }

	public MBReadOnlyList<CharacterObject> TournamentTeamTemplatesForTwoParticipant { get; private set; }

	public MBReadOnlyList<CharacterObject> TournamentTeamTemplatesForFourParticipant { get; private set; }

	public MBReadOnlyList<CharacterObject> BasicMercenaryTroops { get; private set; }

	public int MilitiaBonus { get; set; }

	public int ProsperityBonus { get; set; }

	public BoardGameType BoardGame { get; private set; }

	public float NavalFactor { get; private set; }

	internal static void AutoGeneratedStaticCollectObjectsCultureObject(object o, List<object> collectedObjects)
	{
		((CultureObject)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	public bool HasTrait(CultureTrait trait)
	{
		return Traits.Contains(trait);
	}

	public bool HasFeat(FeatObject feat)
	{
		return _cultureFeats.Contains(feat);
	}

	public IEnumerable<FeatObject> GetCulturalFeats(Func<FeatObject, bool> predicate = null)
	{
		foreach (FeatObject cultureFeat in _cultureFeats)
		{
			if (predicate == null || predicate(cultureFeat))
			{
				yield return cultureFeat;
			}
		}
	}

	public override string ToString()
	{
		return base.Name.ToString();
	}

	public override void Deserialize(MBObjectManager objectManager, XmlNode node)
	{
		base.Deserialize(objectManager, node);
		MilitiaBonus = ((node.Attributes["militia_bonus"] != null) ? Convert.ToInt32(node.Attributes["militia_bonus"].Value) : 0);
		ProsperityBonus = ((node.Attributes["prosperity_bonus"] != null) ? Convert.ToInt32(node.Attributes["prosperity_bonus"].Value) : 0);
		NavalFactor = ((node.Attributes["naval_factor"] == null) ? 0f : Convert.ToSingle(node.Attributes["naval_factor"].Value));
		DefaultPartyTemplate = objectManager.ReadObjectReferenceFromXml<PartyTemplateObject>("default_party_template", node);
		VillagerPartyTemplate = objectManager.ReadObjectReferenceFromXml<PartyTemplateObject>("villager_party_template", node);
		FishingPartyTemplate = objectManager.ReadObjectReferenceFromXml<PartyTemplateObject>("fishing_party_template", node);
		MilitiaPartyTemplate = objectManager.ReadObjectReferenceFromXml<PartyTemplateObject>("militia_party_template", node);
		RebelsPartyTemplate = objectManager.ReadObjectReferenceFromXml<PartyTemplateObject>("rebels_party_template", node);
		BanditBossPartyTemplate = objectManager.ReadObjectReferenceFromXml<PartyTemplateObject>("bandit_boss_party_template", node);
		VassalRewardTroopsPartyTemplate = objectManager.ReadObjectReferenceFromXml<PartyTemplateObject>("vassal_reward_party_template", node);
		SettlementPatrolPartyTemplateWeak = objectManager.ReadObjectReferenceFromXml<PartyTemplateObject>("settlement_patrol_template_level_1", node);
		SettlementPatrolPartyTemplateModerate = objectManager.ReadObjectReferenceFromXml<PartyTemplateObject>("settlement_patrol_template_level_2", node);
		SettlementPatrolPartyTemplateStrong = objectManager.ReadObjectReferenceFromXml<PartyTemplateObject>("settlement_patrol_template_level_3", node);
		SettlementPatrolPartyTemplateNaval = objectManager.ReadObjectReferenceFromXml<PartyTemplateObject>("settlement_patrol_template_coastal", node);
		EliteBasicTroop = objectManager.ReadObjectReferenceFromXml<CharacterObject>("elite_basic_troop", node);
		MeleeEliteMilitiaTroop = objectManager.ReadObjectReferenceFromXml<CharacterObject>("melee_elite_militia_troop", node);
		RangedEliteMilitiaTroop = objectManager.ReadObjectReferenceFromXml<CharacterObject>("ranged_elite_militia_troop", node);
		MeleeMilitiaTroop = objectManager.ReadObjectReferenceFromXml<CharacterObject>("melee_militia_troop", node);
		RangedMilitiaTroop = objectManager.ReadObjectReferenceFromXml<CharacterObject>("ranged_militia_troop", node);
		BasicTroop = objectManager.ReadObjectReferenceFromXml<CharacterObject>("basic_troop", node);
		DefaultBattleEquipmentRoster = objectManager.ReadObjectReferenceFromXml<MBEquipmentRoster>("default_battle_equipment_roster", node);
		DefaultCivilianEquipmentRoster = objectManager.ReadObjectReferenceFromXml<MBEquipmentRoster>("default_civilian_equipment_roster", node);
		DefaultStealthEquipmentRoster = objectManager.ReadObjectReferenceFromXml<MBEquipmentRoster>("default_stealth_equipment_roster", node);
		DuelPresetEquipmentRoster = objectManager.ReadObjectReferenceFromXml<MBEquipmentRoster>("duel_preset_equipment_roster", node);
		MarriageBrideEquipmentRoster = objectManager.ReadObjectReferenceFromXml<MBEquipmentRoster>("marriage_bride_equipment_roster", node);
		TournamentMaster = objectManager.ReadObjectReferenceFromXml<CharacterObject>("tournament_master", node);
		Villager = objectManager.ReadObjectReferenceFromXml<CharacterObject>("villager", node);
		CaravanMaster = objectManager.ReadObjectReferenceFromXml<CharacterObject>("caravan_master", node);
		CaravanGuard = objectManager.ReadObjectReferenceFromXml<CharacterObject>("caravan_guard", node);
		PrisonGuard = objectManager.ReadObjectReferenceFromXml<CharacterObject>("prison_guard", node);
		Guard = objectManager.ReadObjectReferenceFromXml<CharacterObject>("guard", node);
		Blacksmith = objectManager.ReadObjectReferenceFromXml<CharacterObject>("blacksmith", node);
		Weaponsmith = objectManager.ReadObjectReferenceFromXml<CharacterObject>("weaponsmith", node);
		Townswoman = objectManager.ReadObjectReferenceFromXml<CharacterObject>("townswoman", node);
		TownswomanInfant = objectManager.ReadObjectReferenceFromXml<CharacterObject>("townswoman_infant", node);
		TownswomanChild = objectManager.ReadObjectReferenceFromXml<CharacterObject>("townswoman_child", node);
		TownswomanTeenager = objectManager.ReadObjectReferenceFromXml<CharacterObject>("townswoman_teenager", node);
		Townsman = objectManager.ReadObjectReferenceFromXml<CharacterObject>("townsman", node);
		TownsmanInfant = objectManager.ReadObjectReferenceFromXml<CharacterObject>("townsman_infant", node);
		TownsmanChild = objectManager.ReadObjectReferenceFromXml<CharacterObject>("townsman_child", node);
		TownsmanTeenager = objectManager.ReadObjectReferenceFromXml<CharacterObject>("townsman_teenager", node);
		VillageWoman = objectManager.ReadObjectReferenceFromXml<CharacterObject>("village_woman", node);
		VillagerMaleChild = objectManager.ReadObjectReferenceFromXml<CharacterObject>("villager_male_child", node);
		VillagerMaleTeenager = objectManager.ReadObjectReferenceFromXml<CharacterObject>("villager_male_teenager", node);
		VillagerFemaleChild = objectManager.ReadObjectReferenceFromXml<CharacterObject>("villager_female_child", node);
		VillagerFemaleTeenager = objectManager.ReadObjectReferenceFromXml<CharacterObject>("villager_female_teenager", node);
		RansomBroker = objectManager.ReadObjectReferenceFromXml<CharacterObject>("ransom_broker", node);
		GangleaderBodyguard = objectManager.ReadObjectReferenceFromXml<CharacterObject>("gangleader_bodyguard", node);
		MerchantNotary = objectManager.ReadObjectReferenceFromXml<CharacterObject>("merchant_notary", node);
		ArtisanNotary = objectManager.ReadObjectReferenceFromXml<CharacterObject>("artisan_notary", node);
		PreacherNotary = objectManager.ReadObjectReferenceFromXml<CharacterObject>("preacher_notary", node);
		RuralNotableNotary = objectManager.ReadObjectReferenceFromXml<CharacterObject>("rural_notable_notary", node);
		ShopWorker = objectManager.ReadObjectReferenceFromXml<CharacterObject>("shop_worker", node);
		Tavernkeeper = objectManager.ReadObjectReferenceFromXml<CharacterObject>("tavernkeeper", node);
		TavernGamehost = objectManager.ReadObjectReferenceFromXml<CharacterObject>("taverngamehost", node);
		Musician = objectManager.ReadObjectReferenceFromXml<CharacterObject>("musician", node);
		TavernWench = objectManager.ReadObjectReferenceFromXml<CharacterObject>("tavern_wench", node);
		Armorer = objectManager.ReadObjectReferenceFromXml<CharacterObject>("armorer", node);
		HorseMerchant = objectManager.ReadObjectReferenceFromXml<CharacterObject>("horseMerchant", node);
		Barber = objectManager.ReadObjectReferenceFromXml<CharacterObject>("barber", node);
		Merchant = objectManager.ReadObjectReferenceFromXml<CharacterObject>("merchant", node);
		Beggar = objectManager.ReadObjectReferenceFromXml<CharacterObject>("beggar", node);
		FemaleBeggar = objectManager.ReadObjectReferenceFromXml<CharacterObject>("female_beggar", node);
		FemaleDancer = objectManager.ReadObjectReferenceFromXml<CharacterObject>("female_dancer", node);
		Shipwright = objectManager.ReadObjectReferenceFromXml<CharacterObject>("shipwright", node);
		ShipyardWorker = objectManager.ReadObjectReferenceFromXml<CharacterObject>("shipyard_worker", node);
		MilitiaVeteranArcher = objectManager.ReadObjectReferenceFromXml<CharacterObject>("militia_veteran_archer", node);
		GearDummy = objectManager.ReadObjectReferenceFromXml<CharacterObject>("gear_dummy", node);
		BanditBandit = objectManager.ReadObjectReferenceFromXml<CharacterObject>("bandit_bandit", node);
		BanditRaider = objectManager.ReadObjectReferenceFromXml<CharacterObject>("bandit_raider", node);
		BanditChief = objectManager.ReadObjectReferenceFromXml<CharacterObject>("bandit_chief", node);
		BanditBoss = objectManager.ReadObjectReferenceFromXml<CharacterObject>("bandit_boss", node);
		DefaultCharacterCreationBodyProperty = ((node.Attributes["default_character_creation_body_property"] != null) ? objectManager.ReadObjectReferenceFromXml<MBBodyProperty>("default_character_creation_body_property", node) : null);
		EncyclopediaText = ((node.Attributes["text"] != null) ? new TextObject(node.Attributes["text"].Value) : TextObject.GetEmpty());
		float a = ((node.Attributes["start_point_position_x"] == null) ? 0f : Convert.ToSingle(node.Attributes["start_point_position_x"].Value));
		float b = ((node.Attributes["start_point_position_y"] == null) ? 0f : Convert.ToSingle(node.Attributes["start_point_position_y"].Value));
		StartingPoint = new CampaignVec2(new Vec2(a, b), isOnLand: true);
		if (node.Attributes["board_game_type"] != null && Enum.TryParse<BoardGameType>(node.Attributes["board_game_type"].Value, out var result))
		{
			BoardGame = result;
		}
		XmlNodeList childNodes = node.ChildNodes;
		_defaultPolicyList = new MBList<PolicyObject>();
		_maleNameList = new MBList<TextObject>();
		_femaleNameList = new MBList<TextObject>();
		_clanNameList = new MBList<TextObject>();
		_cultureFeats = new MBList<FeatObject>();
		MBList<int> mBList = new MBList<int>();
		MBList<CharacterObject> mBList2 = new MBList<CharacterObject>();
		MBList<CharacterObject> mBList3 = new MBList<CharacterObject>();
		MBList<CharacterObject> mBList4 = new MBList<CharacterObject>();
		MBList<CharacterObject> mBList5 = new MBList<CharacterObject>();
		MBList<CharacterObject> mBList6 = new MBList<CharacterObject>();
		MBList<CharacterObject> mBList7 = new MBList<CharacterObject>();
		MBList<ItemObject> mBList8 = new MBList<ItemObject>();
		MBList<ItemObject> mBList9 = new MBList<ItemObject>();
		MBList<PartyTemplateObject> mBList10 = new MBList<PartyTemplateObject>();
		MBList<PartyTemplateObject> mBList11 = new MBList<PartyTemplateObject>();
		MBList<ShipHull> mBList12 = new MBList<ShipHull>();
		MBList<CharacterObject> mBList13 = new MBList<CharacterObject>();
		foreach (XmlNode item5 in childNodes)
		{
			if (item5.Name == "default_policies")
			{
				foreach (XmlNode childNode in item5.ChildNodes)
				{
					PolicyObject item = objectManager.GetObject<PolicyObject>(childNode.Attributes["id"].Value);
					_defaultPolicyList.Add(item);
				}
			}
			else if (item5.Name == "male_names")
			{
				foreach (XmlNode childNode2 in item5.ChildNodes)
				{
					_maleNameList.Add(new TextObject(childNode2.Attributes["name"].Value));
				}
			}
			else if (item5.Name == "female_names")
			{
				foreach (XmlNode childNode3 in item5.ChildNodes)
				{
					_femaleNameList.Add(new TextObject(childNode3.Attributes["name"].Value));
				}
			}
			else if (item5.Name == "clan_names")
			{
				foreach (XmlNode childNode4 in item5.ChildNodes)
				{
					_clanNameList.Add(new TextObject(childNode4.Attributes["name"].Value));
				}
			}
			else if (item5.Name == "cultural_feats")
			{
				foreach (XmlNode childNode5 in item5.ChildNodes)
				{
					string value = childNode5.Attributes["id"].Value;
					FeatObject featObject = (FeatObject)MBObjectManager.Instance.CreateObjectFromXmlNode(childNode5);
					Debug.Print("Reading feat with id " + value + ",  in Culture  " + base.StringId);
					if (featObject != null)
					{
						_cultureFeats.Add(featObject);
					}
				}
			}
			else if (item5.Name == "possible_clan_banner_icon_ids")
			{
				foreach (XmlNode childNode6 in item5.ChildNodes)
				{
					int.TryParse(childNode6.Attributes["id"].Value, out var result2);
					mBList.Add(result2);
				}
			}
			else if (item5.Name == "notable_templates")
			{
				foreach (XmlNode childNode7 in item5.ChildNodes)
				{
					CharacterObject item2 = objectManager.ReadObjectReferenceFromXml<CharacterObject>("name", childNode7);
					mBList2.Add(item2);
				}
			}
			else if (item5.Name == "lord_templates")
			{
				foreach (XmlNode childNode8 in item5.ChildNodes)
				{
					CharacterObject item3 = objectManager.ReadObjectReferenceFromXml<CharacterObject>("name", childNode8);
					mBList4.Add(item3);
				}
			}
			else if (item5.Name == "rebellion_hero_templates")
			{
				foreach (XmlNode childNode9 in item5.ChildNodes)
				{
					CharacterObject item4 = objectManager.ReadObjectReferenceFromXml<CharacterObject>("name", childNode9);
					mBList3.Add(item4);
				}
			}
			else if (item5.Name == "tournament_team_templates_one_participant")
			{
				foreach (XmlNode childNode10 in item5.ChildNodes)
				{
					mBList5.Add(objectManager.ReadObjectReferenceFromXml<CharacterObject>("name", childNode10));
				}
			}
			else if (item5.Name == "tournament_team_templates_two_participant")
			{
				foreach (XmlNode childNode11 in item5.ChildNodes)
				{
					mBList6.Add(objectManager.ReadObjectReferenceFromXml<CharacterObject>("name", childNode11));
				}
			}
			else if (item5.Name == "tournament_team_templates_four_participant")
			{
				foreach (XmlNode childNode12 in item5.ChildNodes)
				{
					mBList7.Add(objectManager.ReadObjectReferenceFromXml<CharacterObject>("name", childNode12));
				}
			}
			else if (item5.Name == "vassal_reward_items")
			{
				foreach (XmlNode childNode13 in item5.ChildNodes)
				{
					mBList8.Add(objectManager.ReadObjectReferenceFromXml<ItemObject>("id", childNode13));
				}
			}
			else if (item5.Name == "basic_mercenary_troops")
			{
				foreach (XmlNode childNode14 in item5.ChildNodes)
				{
					mBList13.Add(objectManager.ReadObjectReferenceFromXml<CharacterObject>("name", childNode14));
				}
			}
			else if (item5.Name == "banner_bearer_replacement_weapons")
			{
				foreach (XmlNode childNode15 in item5.ChildNodes)
				{
					mBList9.Add(objectManager.ReadObjectReferenceFromXml<ItemObject>("id", childNode15));
				}
			}
			else if (item5.Name == "caravan_party_templates")
			{
				foreach (XmlNode childNode16 in item5.ChildNodes)
				{
					mBList10.Add(objectManager.ReadObjectReferenceFromXml<PartyTemplateObject>("id", childNode16));
				}
			}
			else if (item5.Name == "elite_caravan_party_templates")
			{
				foreach (XmlNode childNode17 in item5.ChildNodes)
				{
					mBList11.Add(objectManager.ReadObjectReferenceFromXml<PartyTemplateObject>("id", childNode17));
				}
			}
			else
			{
				if (!(item5.Name == "available_ship_hulls"))
				{
					continue;
				}
				foreach (XmlNode childNode18 in item5.ChildNodes)
				{
					mBList12.Add(objectManager.ReadObjectReferenceFromXml<ShipHull>("id", childNode18));
				}
			}
		}
		PossibleClanBannerIconsIDs = mBList;
		if (PossibleClanBannerIconsIDs == null)
		{
			PossibleClanBannerIconsIDs = new MBList<int>();
		}
		NotableTemplates = mBList2;
		RebelliousHeroTemplates = mBList3;
		LordTemplates = mBList4;
		TournamentTeamTemplatesForOneParticipant = mBList5;
		TournamentTeamTemplatesForTwoParticipant = mBList6;
		TournamentTeamTemplatesForFourParticipant = mBList7;
		mBList8.RemoveAll((ItemObject x) => !x.IsReady);
		VassalRewardItems = mBList8;
		BasicMercenaryTroops = mBList13;
		mBList9.RemoveAll((ItemObject x) => !x.IsReady);
		BannerBearerReplacementWeapons = mBList9;
		CaravanPartyTemplates = mBList10;
		EliteCaravanPartyTemplates = mBList11;
		AvailableShipHulls = mBList12;
	}

	public override TextObject GetName()
	{
		return base.Name;
	}
}
