using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Helpers;
using HarmonyLib;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace AnimusForge;

public partial class RewardSystemBehavior : CampaignBehaviorBase
{
	private const int RewardQuickInfoDurationMs = 5000;
	private const float JoinPartyConversationCloseDelaySeconds = 5f;

	private const string NotableMarketPromptPrefix = "market@";
	private const int NotableMarketInventoryPromptMaxItems = 40;
	private const int NotableMarketPostprocessMaxItems = 80;
	private const float RewardItemNameMatchThreshold = 0.8f;
	private const float GeneratedRewardTemplateMiscScoreBonus = 0.18f;
	private const float GeneratedRewardTemplateMiscScoreMultiplier = 1.25f;
	private const float GeneratedRewardTemplateWeaponArmorScorePenalty = 0.2f;
	private const float GeneratedRewardTemplateWeaponArmorScoreMultiplier = 0.55f;
	private const float GeneratedRewardTemplateSemanticHintBonus = 0.42f;
	private const float GeneratedRewardTemplateWeakSemanticHintBonus = 0.2f;
	private const float GeneratedRewardTemplateDiversityTieBreaker = 0.08f;
	private const uint GeneratedRewardReservedSubIdMask = 0x02000000u;
	private const uint GeneratedRewardReservedSubIdBits = 0x02000000u;
	private const string GeneratedRewardItemManifestFileName = "GeneratedRewardItems.json";
	private const string GeneratedRewardPlayerRosterStorageKey = "_rewardGeneratedPlayerRosterItems_v1";
	private static readonly Encoding GeneratedRewardManifestEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
	private static readonly object GeneratedRewardItemRegistrationLock = new object();
	private static readonly Dictionary<uint, ItemObject> GeneratedRewardPendingItemsByObjectId = new Dictionary<uint, ItemObject>();
	private static readonly Dictionary<uint, ItemObject> GeneratedRewardDetachedItemsByObjectId = new Dictionary<uint, ItemObject>();
	private static readonly Dictionary<string, ItemObject> GeneratedRewardDetachedItemsByStringId = new Dictionary<string, ItemObject>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<uint, GeneratedRewardItemRecord> GeneratedRewardManifestByObjectId = new Dictionary<uint, GeneratedRewardItemRecord>();
	private static readonly Dictionary<string, GeneratedRewardItemRecord> GeneratedRewardManifestByStringId = new Dictionary<string, GeneratedRewardItemRecord>(StringComparer.OrdinalIgnoreCase);
	private static bool GeneratedRewardManifestLoaded;
	private static readonly object GeneratedRewardEconomicPoolCacheLock = new object();
	private static MBReadOnlyList<ItemObject> GeneratedRewardEconomicPoolSource;
	private static MBReadOnlyList<ItemObject> GeneratedRewardEconomicPoolFiltered;
	private static int GeneratedRewardEconomicPoolSourceCount = -1;
	private static readonly FieldInfo WorkshopsItemsInCategoryField = AccessTools.Field(typeof(WorkshopsCampaignBehavior), "_itemsInCategory");
	private static readonly FieldInfo HideoutPotentialLootItemsField = AccessTools.Field(typeof(HideoutCampaignBehavior), "_potentialLootItems");
	[ThreadStatic]
	private static bool SuppressGeneratedRewardObjectLookup;
	[ThreadStatic]
	private static bool SuppressGeneratedRewardPendingLookup;
	private static DateTime GeneratedRewardLastInventoryVmLogUtc = DateTime.MinValue;
	private static string GeneratedRewardLastInventoryVmLogSignature = "";
	private static readonly PropertyInfo RewardItemObjectNameProperty = typeof(ItemObject).GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static readonly PropertyInfo RewardItemObjectCategoryProperty = typeof(ItemObject).GetProperty("ItemCategory", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static readonly PropertyInfo RewardItemObjectComponentProperty = typeof(ItemObject).GetProperty("ItemComponent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static readonly PropertyInfo RewardItemObjectValueProperty = typeof(ItemObject).GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static readonly PropertyInfo RewardItemObjectWeightProperty = typeof(ItemObject).GetProperty("Weight", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static readonly PropertyInfo RewardItemObjectIsFoodProperty = typeof(ItemObject).GetProperty("IsFood", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static readonly PropertyInfo RewardItemObjectNotMerchandiseProperty = typeof(ItemObject).GetProperty("NotMerchandise", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static readonly PropertyInfo RewardItemObjectItemFlagsProperty = typeof(ItemObject).GetProperty("ItemFlags", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static readonly string[] GeneratedRewardItemTemplateStatePropertyNames = new string[31]
	{
		"ItemCategory",
		"ItemComponent",
		"WeaponDesign",
		"MultiMeshName",
		"HolsterMeshName",
		"HolsterWithWeaponMeshName",
		"ItemHolsters",
		"HolsterPositionShift",
		"FlyingMeshName",
		"BodyName",
		"SkeletonName",
		"StaticAnimationName",
		"HolsterBodyName",
		"CollisionBodyName",
		"RecalculateBody",
		"PrefabName",
		"ItemFlags",
		"Value",
		"Effectiveness",
		"Weight",
		"Difficulty",
		"Appearance",
		"IsUsingTableau",
		"ArmBandMeshName",
		"IsFood",
		"ScaleFactor",
		"Culture",
		"MultiplayerItem",
		"NotMerchandise",
		"LodAtlasIndex",
		"ItemType"
	};
	private static readonly PropertyInfo[] GeneratedRewardItemTemplateStateProperties =
		GeneratedRewardItemTemplateStatePropertyNames
			.Select((string propertyName) => typeof(ItemObject).GetProperty(
				propertyName,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			.ToArray();
	private static readonly MethodInfo RewardObjectManagerTryRegisterWithoutInitializationMethod = typeof(MBObjectManager).GetMethod("TryRegisterObjectWithoutInitialization", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static readonly FieldInfo HeroClanBackingField = typeof(Hero).GetField("_clan", BindingFlags.Instance | BindingFlags.NonPublic);
	// Some models copy the C&L template verbatim. Its only safe fallback is the companion branch (C).
	private static readonly Regex HeroJoinPlayerPartyTagRegex = new Regex("\\[A:H_J_P_P_(C(?:&L)?|L)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static readonly Regex DebtCreationTagRegex = new Regex("\\[AD:(\\d+):(\\d+):(N|P):([^\\]\\r\\n]*)\\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
	private static readonly Regex DebtResolutionTagRegex = new Regex("\\[ADP:([a-zA-Z0-9_\\-]+)\\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	public enum SettlementMerchantKind
	{
		None,
		Weapon,
		Blacksmith,
		Armor,
		Horse,
		Goods
	}

	private enum GeneratedRpEquipmentKind
	{
		None,
		AnyEquipment,
		AnyWeapon,
		Sword,
		Axe,
		Mace,
		Dagger,
		Whip,
		Polearm,
		Bow,
		Crossbow,
		Shield,
		Thrown,
		ThrowingAxe,
		ThrowingKnife,
		Javelin,
		Arrows,
		Bolts,
		Sling,
		Firearm,
		Bullets,
		AnyArmor,
		HeadArmor,
		BodyArmor,
		LegArmor,
		HandArmor,
		Cape,
		Horse,
		HorseHarness,
		Banner
	}

	private enum GeneratedRpFoodKind
	{
		None,
		AnyFood,
		Meat,
		Fish,
		Grain,
		Fruit,
		Vegetable,
		Dairy,
		Egg,
		Sweet,
		PreparedMeal,
		Water,
		Medicine,
		Beer,
		Wine,
		Drink
	}

	private sealed class GeneratedRpEquipmentSuffixRule
	{
		public GeneratedRpEquipmentKind Kind { get; }

		public bool RequiresEnglishWordBoundary { get; }

		public string[] Suffixes { get; }

		public GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind kind, bool requiresEnglishWordBoundary, params string[] suffixes)
		{
			Kind = kind;
			RequiresEnglishWordBoundary = requiresEnglishWordBoundary;
			Suffixes = suffixes ?? Array.Empty<string>();
		}
	}

	private sealed class GeneratedRpEquipmentTemplateCandidate
	{
		public ItemObject Item { get; set; }

		public string[] Aliases { get; set; }
	}

	private sealed class GeneratedRpFoodSuffixRule
	{
		public GeneratedRpFoodKind Kind { get; }

		public bool RequiresEnglishWordBoundary { get; }

		public string[] Suffixes { get; }

		public GeneratedRpFoodSuffixRule(GeneratedRpFoodKind kind, bool requiresEnglishWordBoundary, params string[] suffixes)
		{
			Kind = kind;
			RequiresEnglishWordBoundary = requiresEnglishWordBoundary;
			Suffixes = suffixes ?? Array.Empty<string>();
		}
	}

	private sealed class GeneratedRpFoodTemplateCandidate
	{
		public ItemObject Item { get; set; }

		public string[] Aliases { get; set; }
	}

	private static readonly object GeneratedRpEquipmentTemplateCacheLock = new object();
	private static object GeneratedRpEquipmentTemplateCacheOwner;
	private static Dictionary<GeneratedRpEquipmentKind, List<GeneratedRpEquipmentTemplateCandidate>> GeneratedRpEquipmentTemplatesByKind = new Dictionary<GeneratedRpEquipmentKind, List<GeneratedRpEquipmentTemplateCandidate>>();
	private static bool GeneratedRpEquipmentTemplateCacheReady;
	private static DateTime GeneratedRpEquipmentTemplateCacheRetryAfterUtc = DateTime.MinValue;
	private static readonly object GeneratedRpFoodTemplateCacheLock = new object();
	private static object GeneratedRpFoodTemplateCacheOwner;
	private static Dictionary<GeneratedRpFoodKind, List<GeneratedRpFoodTemplateCandidate>> GeneratedRpFoodTemplatesByKind = new Dictionary<GeneratedRpFoodKind, List<GeneratedRpFoodTemplateCandidate>>();
	private static readonly object PlayerRpMiscTemplateCacheLock = new object();
	private static object PlayerRpMiscTemplateCacheOwner;
	private static List<GeneratedRpFoodTemplateCandidate> PlayerRpMiscTemplateCandidates = new List<GeneratedRpFoodTemplateCandidate>();
	private static readonly object PlayerRpPriceCacheLock = new object();
	private static object PlayerRpPriceCacheOwner;
	private static Dictionary<int, int> PlayerRpMedianPriceByItemType = new Dictionary<int, int>();
	private static int PlayerRpCraftCommitGate;
	private static readonly char[] GeneratedRpTrailingPunctuation = new char[24] { '.', ',', ';', '!', '?', ':', '\'', '"', '\u3002', '\uff0c', '\uff1b', '\uff01', '\uff1f', '\uff1a', '\u2019', '\u201d', '\u300b', '\u3011', ')', ']', '}', '\uff09', '\u3015', '\u3009' };
	private static readonly string[] GeneratedRpFoodNonFoodEndingExceptions = new string[]
	{
		"如果", "结果", "后果", "因果", "效果", "成果", "战果", "恶果", "苦果",
		"笨蛋", "混蛋", "坏蛋", "傻蛋", "滚蛋", "完蛋", "脸蛋", "捣蛋", "穷光蛋", "王八蛋",
		"奶奶", "姑奶奶",
		"血肉", "骨肉", "皮肉",
		"铁饼", "画饼", "帮派",
		"香水", "花露水", "墨水", "薪水", "泪水", "汗水", "口水", "血水", "海水", "洪水", "污水", "废水", "泥水",
		"火药", "炸药", "弹药", "农药", "鼠药", "麻药", "迷药", "外用药", "膏药", "兽药", "杀虫药",
		"waste water", "sea water", "dirty water", "sewage water"
	};
	private static readonly string[] GeneratedRpEquipmentNonEquipmentEndingExceptions = new string[]
	{
		"bottle cap", "jar cap", "pen cap", "hub cap", "wheel cap", "knee cap",
		"gold standard", "living standard", "quality standard", "industry standard",
		"safety standard", "technical standard", "accounting standard"
	};
	private static readonly string[] GeneratedRpFoodMeatTemplateTokens = new string[20] { "meat", "beef", "pork", "mutton", "lamb", "chicken", "poultry", "turkey", "duck", "venison", "bacon", "ham", "sausage", "steak", "jerky", "肉", "牛排", "羊排", "猪排", "香肠" };
	private static readonly string[] GeneratedRpFoodFishTemplateTokens = new string[15] { "fish", "seafood", "salmon", "trout", "herring", "tuna", "sardine", "shrimp", "prawn", "crab", "oyster", "shellfish", "鱼", "虾", "蟹" };
	private static readonly string[] GeneratedRpFoodGrainTemplateTokens = new string[18] { "grain", "wheat", "flour", "bread", "rice", "noodle", "pasta", "porridge", "cereal", "oatmeal", "dumpling", "ration", "粮", "麦", "面包", "米饭", "面条", "粥" };
	private static readonly string[] GeneratedRpFoodFruitTemplateTokens = new string[22] { "fruit", "apple", "pear", "peach", "plum", "apricot", "grape", "berry", "orange", "lemon", "melon", "banana", "mango", "coconut", "olive", "date_fruit", "苹果", "梨", "葡萄", "水果", "浆果", "枣" };
	private static readonly string[] GeneratedRpFoodVegetableTemplateTokens = new string[18] { "vegetable", "veggie", "cabbage", "carrot", "potato", "onion", "garlic", "lentil", "chickpea", "bean", "pea", "turnip", "mushroom", "蔬菜", "白菜", "萝卜", "土豆", "蘑菇" };
	private static readonly string[] GeneratedRpFoodDairyTemplateTokens = new string[12] { "dairy", "cheese", "butter", "cream", "milk", "yogurt", "yoghurt", "奶酪", "芝士", "黄油", "牛奶", "酸奶" };
	private static readonly string[] GeneratedRpFoodEggTemplateTokens = new string[6] { "egg", "鸡蛋", "鸭蛋", "鹅蛋", "鸟蛋", "鹌鹑蛋" };
	private static readonly string[] GeneratedRpFoodSweetTemplateTokens = new string[18] { "cake", "pastry", "biscuit", "cookie", "candy", "chocolate", "pudding", "jam", "honey", "sweet", "糖果", "巧克力", "蛋糕", "糕", "布丁", "果酱", "蜂蜜", "甜点" };
	private static readonly string[] GeneratedRpFoodBeerTemplateTokens = new string[8] { "beer", "ale", "lager", "stout", "porter", "啤酒", "麦酒", "麦芽酒" };
	private static readonly string[] GeneratedRpFoodWineTemplateTokens = new string[13] { "wine", "mead", "cider", "liquor", "spirit", "葡萄酒", "果酒", "蜂蜜酒", "红酒", "白酒", "米酒", "黄酒", "烈酒" };
	private static readonly string[] GeneratedRpFoodDrinkTemplateTokens = new string[16] { "drink", "beverage", "juice", "tea", "coffee", "cocoa", "milkshake", "beer", "wine", "果汁", "饮料", "饮品", "茶", "咖啡", "啤酒", "酒" };
	private static readonly string[] GeneratedRpFoodWaterTemplateTokens = new string[15] { "water", "freshwater", "spring water", "mineral water", "drinking water", "泉水", "井水", "清水", "净水", "纯净水", "饮用水", "矿泉水", "淡水", "开水", "圣水" };
	private static readonly string[] GeneratedRpFoodMedicineTemplateTokens = new string[20] { "medicine", "medication", "remedy", "potion", "elixir", "tonic", "antidote", "drug", "pill", "capsule", "herb", "syrup", "药", "药剂", "药丸", "药水", "丹药", "灵药", "草药", "解药" };
	private static readonly GeneratedRpEquipmentSuffixRule[] GeneratedRpEquipmentSuffixRules = new GeneratedRpEquipmentSuffixRule[]
	{
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.ThrowingAxe, false, "投斧", "飞斧"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.ThrowingKnife, false, "投刀", "飞刀"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Javelin, false, "标枪"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Firearm, false, "霰弹枪", "散弹枪", "狙击枪", "冲锋枪", "左轮枪", "燧发枪", "火绳枪", "激光枪", "猎枪", "机枪", "火枪", "手枪", "步枪", "鸟铳", "火铳", "铳"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Bolts, false, "弩箭", "弩矢"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Crossbow, false, "强弩", "重弩", "轻弩", "弩"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Bow, false, "长弓", "短弓", "战弓", "反曲弓", "弓"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Shield, false, "盾牌", "大盾", "小盾", "圆盾", "塔盾", "盾"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Arrows, false, "箭矢", "箭袋", "箭"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Sling, false, "投石索"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Bullets, false, "弹药", "子弹", "枪弹"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Dagger, false, "匕首", "短刃", "小刀"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Whip, false, "马鞭", "长鞭", "短鞭", "战鞭", "皮鞭", "鞭子", "鞭"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Sword, false, "双手剑", "单手剑", "长剑", "短剑", "大剑", "巨剑", "弯刀", "佩剑", "战刀", "军刀", "长刀", "短刀", "剑", "刀"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Axe, false, "双手斧", "单手斧", "战斧", "巨斧", "手斧", "斧"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Mace, false, "钉头锤", "战锤", "大锤", "铁锤", "锤矛", "权杖", "锤", "棒", "棍"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Polearm, false, "长柄武器", "长柄", "长枪", "骑枪", "短枪", "战矛", "长矛", "短矛", "槊", "戟", "矛", "枪"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Thrown, false, "投掷武器"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Horse, false, "战马", "骏马", "军马", "良马", "坐骑", "马匹"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.HorseHarness, false, "马铠", "马甲", "马具", "鞍具", "马鞍", "鞍"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.HeadArmor, false, "头盔", "战盔", "铁盔", "兜帽", "风帽", "帽子", "头巾", "面纱", "面甲", "面罩", "面具", "头冠", "王冠", "冠冕", "盔", "帽"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.HandArmor, false, "手甲", "臂甲", "腕甲", "臂铠", "护臂", "护腕", "护手", "手套", "拳套", "手衣"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.LegArmor, false, "腿甲", "胫甲", "足甲", "脚甲", "护腿", "护胫", "护膝", "战靴", "长靴", "靴子", "鞋子", "战鞋", "皮鞋", "布鞋", "凉鞋", "袜子", "长袜", "短袜", "靴", "鞋", "袜"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Cape, false, "肩甲", "护肩", "披风", "斗篷", "披肩", "围巾", "披巾", "披帛"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.BodyArmor, false, "长袍", "短袍", "战袍", "法袍", "礼袍", "罩袍", "袍子", "袍"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.BodyArmor, false, "上衣", "外衣", "衬衣", "内衣", "衣服", "服装", "礼服", "制服", "战服", "外套", "大衣", "夹克", "长衫", "短衫", "衫", "衣"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.BodyArmor, false, "裤子", "长裤", "短裤", "马裤", "皮裤", "布裤", "裙子", "长裙", "短裙", "战裙", "裤", "裙"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.BodyArmor, false, "胸甲", "身甲", "板甲", "链甲", "鳞甲", "皮甲", "布甲", "重甲", "轻甲"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.BodyArmor, false, "甲胄", "铠甲", "盔甲", "护甲", "铠", "甲"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Banner, false, "旗帜", "军旗", "战旗", "旗"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.AnyWeapon, false, "武器", "兵器", "兵刃"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.AnyEquipment, false, "装备", "武装"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.ThrowingAxe, true, "throwing axes", "throwing axe"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.ThrowingKnife, true, "throwing knives", "throwing knife"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Javelin, true, "javelins", "javelin"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Crossbow, true, "crossbows", "crossbow"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Bow, true, "longbows", "longbow", "shortbows", "shortbow", "warbows", "warbow", "recurve bows", "recurve bow", "bows", "bow"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Shield, true, "shields", "shield", "bucklers", "buckler"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Arrows, true, "arrows", "arrow", "quivers", "quiver"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Bolts, true, "crossbow bolts", "bolts", "bolt"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Sling, true, "slings", "sling"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Firearm, true, "pistols", "pistol", "muskets", "musket", "rifles", "rifle", "firearms", "firearm", "guns", "gun"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Bullets, true, "ammunition", "ammo", "bullets", "bullet"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Sword, true, "longswords", "longsword", "shortswords", "shortsword", "broadswords", "broadsword", "greatswords", "greatsword", "swords", "sword", "sabers", "saber", "sabres", "sabre", "scimitars", "scimitar", "katanas", "katana", "blades", "blade"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Axe, true, "battleaxes", "battleaxe", "greataxes", "greataxe", "handaxes", "handaxe", "axes", "axe"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Mace, true, "warhammers", "warhammer", "hammers", "hammer", "maces", "mace", "clubs", "club"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Dagger, true, "daggers", "dagger", "knives", "knife"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Whip, true, "horsewhips", "horsewhip", "bullwhips", "bullwhip", "whips", "whip"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Polearm, true, "polearms", "polearm", "spears", "spear", "lances", "lance", "pikes", "pike", "halberds", "halberd", "glaives", "glaive"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Thrown, true, "throwing weapons", "thrown weapons"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Horse, true, "warhorses", "warhorse", "horses", "horse", "steeds", "steed", "mounts", "mount"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.HorseHarness, true, "horse armors", "horse armor", "horse armours", "horse armour", "bardings", "barding", "saddles", "saddle", "harnesses", "harness"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.HeadArmor, true, "helmets", "helmet", "helms", "helm", "headscarves", "headscarf", "circlets", "circlet", "turbans", "turban", "crowns", "crown", "hoods", "hood", "hats", "hat", "caps", "cap", "masks", "mask", "veils", "veil", "coifs", "coif"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.HandArmor, true, "gauntlets", "gauntlet", "gloves", "glove", "bracers", "bracer", "vambraces", "vambrace", "mittens", "mitten", "handwraps", "handwrap", "wristguards", "wristguard", "armguards", "armguard"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.LegArmor, true, "greaves", "greave", "boots", "boot", "shoes", "shoe", "sandals", "sandal", "slippers", "slipper", "leggings", "legging", "stockings", "stocking", "socks", "sock", "footwear"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Cape, true, "pauldrons", "pauldron", "capes", "cape", "cloaks", "cloak", "mantles", "mantle", "scarves", "scarf", "shawls", "shawl"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.BodyArmor, true, "robes", "robe", "tunics", "tunic", "shirts", "shirt", "trousers", "trouser", "pants", "breeches", "skirts", "skirt", "kilts", "kilt"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.BodyArmor, true, "dresses", "dress", "gowns", "gown", "coats", "coat", "jackets", "jacket", "waistcoats", "waistcoat", "vests", "vest", "jerkins", "jerkin", "doublets", "doublet"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.BodyArmor, true, "uniforms", "uniform", "outfits", "outfit", "garments", "garment", "clothing", "clothes", "apparel"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.BodyArmor, true, "body armors", "body armor", "body armours", "body armour", "chest armors", "chest armor", "chest armours", "chest armour", "cuirasses", "cuirass", "breastplates", "breastplate", "chainmail"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.BodyArmor, true, "armors", "armor", "armours", "armour"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.Banner, true, "banners", "banner", "standards", "standard"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.AnyWeapon, true, "weapons", "weapon", "armaments", "armament"),
		new GeneratedRpEquipmentSuffixRule(GeneratedRpEquipmentKind.AnyEquipment, true, "equipment", "combat gear", "gear")
	};
	private static readonly GeneratedRpFoodSuffixRule[] GeneratedRpFoodSuffixRules = new GeneratedRpFoodSuffixRule[]
	{
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Medicine, false, "疗伤药水", "解毒药水", "治疗药水", "恢复药水", "法力药水", "生命药水", "药水", "药剂", "药丸", "药片", "药粉", "药散", "药材", "药草", "药酒", "药茶", "药汤", "药粥", "药膳", "丹药", "灵药", "草药", "汤药", "中药", "西药", "补药", "秘药", "圣药", "神药", "解药", "伤药", "疗伤药", "退烧药", "止痛药", "治病药", "仙丹", "金丹", "灵丹", "妙药", "药"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Beer, false, "麦芽啤酒", "黑啤酒", "白啤酒", "淡啤酒", "啤酒", "麦芽酒", "麦酒"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Wine, false, "蜂蜜酒", "葡萄酒", "苹果酒", "梨酒", "果酒", "米酒", "黄酒", "白酒", "红酒", "烈酒", "烧酒", "清酒", "甜酒", "奶酒", "酒"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Drink, false, "蔬菜汁", "葡萄汁", "苹果汁", "柠檬汁", "橙汁", "梨汁", "果汁", "酸梅汤", "奶茶", "红茶", "绿茶", "花茶", "茶", "咖啡", "可可", "饮料", "饮品", "汽水"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Water, false, "饮用水", "矿泉水", "纯净水", "过滤水", "净化水", "蜂蜜水", "柠檬水", "椰子水", "泉水", "井水", "清水", "净水", "淡水", "凉水", "温水", "热水", "开水", "圣水", "河水", "雨水", "糖水", "盐水", "水"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Sweet, false, "巧克力蛋糕", "蜂蜜蛋糕", "奶油蛋糕", "水果蛋糕", "蛋糕", "糕点", "米糕", "年糕", "发糕", "糕", "饼干", "月饼", "烧饼", "煎饼", "烙饼", "馅饼", "糖果", "软糖", "硬糖", "蜂蜜", "巧克力", "布丁", "卜丁", "果酱", "甜点"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Fish, false, "烤鱼青", "鱼青", "烤虾圭", "虾圭", "虾面盒", "鱼肉", "烤鱼", "咸鱼", "熏鱼", "鱼干", "鱼排", "鱼丸", "鱼饼", "河鱼", "海鱼", "鲜鱼", "海鲜", "虾", "蟹", "鱼"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Meat, false, "鸡丁敏士", "烤马骏", "纸包鸡", "椰子鸡", "奶油鸡", "焖鸡", "鸡肉元", "肉元", "鸡肉丝", "肉丝", "鸡肉饼", "鸡卷", "鸡排", "鸡徘", "牛扒", "羊腿", "羊肝", "牛尾", "牛排", "羊排", "猪排", "肉排", "牛肉", "羊肉", "猪肉", "鸡肉", "鸭肉", "鹅肉", "鹿肉", "兔肉", "马肉", "禽肉", "兽肉", "熏肉", "腊肉", "烤肉", "肉干", "肉饼", "肉松", "肉酱", "肉串", "香肠", "火腿", "培根", "肉丸", "肉"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Grain, false, "谷物", "穀物", "谷类", "穀類", "五谷", "五穀", "杂粮", "雜糧", "粗粮", "粗糧", "细粮", "細糧", "稻谷", "稻穀", "谷子", "穀子", "面包", "馒头", "包子", "饺子", "馄饨", "面条", "拉面", "汤面", "炒面", "凉面", "挂面", "意面", "米饭", "炒饭", "饭团", "稀饭", "米粥", "麦粥", "麦片", "大米", "小麦", "燕麦", "玉米", "口粮", "粮食", "饭", "粥", "粮"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Fruit, false, "苹果", "雪梨", "鸭梨", "香梨", "梨", "樱桃", "桃子", "蜜桃", "李子", "杏子", "红枣", "蜜枣", "枣", "葡萄", "橙子", "橘子", "柑橘", "柠檬", "石榴", "香蕉", "芒果", "菠萝", "柚子", "柿子", "椰子", "西瓜", "甜瓜", "草莓", "蓝莓", "树莓", "黑莓", "浆果", "莓果", "莓", "水果", "鲜果", "干果", "坚果", "果"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Vegetable, false, "大白菜", "小白菜", "胡萝卜", "马铃薯", "大蒜", "生姜", "白菜", "青菜", "蔬菜", "萝卜", "土豆", "红薯", "地瓜", "南瓜", "黄瓜", "冬瓜", "丝瓜", "茄子", "洋葱", "豆角", "豌豆", "扁豆", "蚕豆", "豆腐", "蘑菇", "香菇", "菌菇", "菜"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Dairy, false, "奶酪", "乳酪", "芝士", "黄油", "奶油", "酸奶", "牛奶", "羊奶", "马奶", "驼奶", "椰奶", "乳品", "奶"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Egg, false, "鹌鹑蛋", "鸡蛋", "鸭蛋", "鹅蛋", "鸟蛋", "咸蛋", "皮蛋", "蛋"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.PreparedMeal, false, "三明治", "汉堡", "炖菜", "沙拉", "菜肴", "佳肴", "寿司", "浓汤", "肉汤", "鱼汤", "菜汤", "汤", "羹", "餐", "食物", "食品"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Medicine, true, "healing potions", "healing potion", "health potions", "health potion", "medicinal syrups", "medicinal syrup", "herbal medicines", "herbal medicine", "medicines", "medicine", "medications", "medication", "remedies", "remedy", "potions", "potion", "elixirs", "elixir", "tonics", "tonic", "antidotes", "antidote", "capsules", "capsule", "pills", "pill", "herbs", "herb", "syrups", "syrup", "drugs", "drug"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Water, true, "purified waters", "purified water", "drinking waters", "drinking water", "mineral waters", "mineral water", "spring waters", "spring water", "holy waters", "holy water", "clean waters", "clean water", "fresh waters", "fresh water", "rainwaters", "rainwater", "freshwaters", "freshwater", "waters", "water"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Wine, true, "sparkling wines", "sparkling wine", "dessert wines", "dessert wine", "red wines", "red wine", "white wines", "white wine", "fruit wines", "fruit wine", "meads", "mead", "ciders", "cider", "liquors", "liquor", "spirits", "spirit", "wines", "wine"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Beer, true, "wheat beers", "wheat beer", "dark beers", "dark beer", "lagers", "lager", "stouts", "stout", "porters", "porter", "beers", "beer", "ales", "ale"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Drink, true, "vegetable juices", "vegetable juice", "fruit juices", "fruit juice", "apple juices", "apple juice", "orange juices", "orange juice", "grape juices", "grape juice", "lemonades", "lemonade", "milkshakes", "milkshake", "beverages", "beverage", "drinks", "drink", "juices", "juice", "coffees", "coffee", "teas", "tea", "cocoa"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Sweet, true, "cheesecakes", "cheesecake", "fruitcakes", "fruitcake", "shortcakes", "shortcake", "moon cakes", "moon cake", "mooncakes", "mooncake", "cupcakes", "cupcake", "pancakes", "pancake", "cakes", "cake", "pastries", "pastry", "biscuits", "biscuit", "cookies", "cookie", "candies", "candy", "chocolates", "chocolate", "puddings", "pudding", "jams", "jam", "honeys", "honey", "sweets", "sweet"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Fish, true, "smoked salmon", "grilled salmon", "salmons", "salmon", "trouts", "trout", "herrings", "herring", "tunas", "tuna", "sardines", "sardine", "shrimps", "shrimp", "prawns", "prawn", "crabs", "crab", "oysters", "oyster", "shellfish", "seafood", "fishes", "fish"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Meat, true, "beefsteaks", "beefsteak", "lamb chops", "lamb chop", "pork chops", "pork chop", "meatballs", "meatball", "sausages", "sausage", "venison", "mutton", "chicken", "poultry", "turkey", "ducks", "duck", "bacons", "bacon", "jerkies", "jerky", "steaks", "steak", "beefs", "beef", "porks", "pork", "lambs", "lamb", "hams", "ham", "meats", "meat"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Grain, true, "sourdough breads", "sourdough bread", "flatbreads", "flatbread", "cornbreads", "cornbread", "bread rolls", "bread roll", "dumplings", "dumpling", "porridges", "porridge", "noodles", "noodle", "pastas", "pasta", "cereals", "cereal", "oatmeals", "oatmeal", "breads", "bread", "rices", "rice", "grains", "grain", "wheats", "wheat", "rations", "ration"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Fruit, true, "date fruits", "date fruit", "dragon fruits", "dragon fruit", "passion fruits", "passion fruit", "strawberries", "strawberry", "blueberries", "blueberry", "raspberries", "raspberry", "blackberries", "blackberry", "coconuts", "coconut", "pomegranates", "pomegranate", "apricots", "apricot", "bananas", "banana", "oranges", "orange", "lemons", "lemon", "melons", "melon", "mangoes", "mango", "grapes", "grape", "peaches", "peach", "plums", "plum", "pears", "pear", "apples", "apple", "berries", "berry", "olives", "olive", "fruits", "fruit"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Vegetable, true, "sweet potatoes", "sweet potato", "green beans", "green bean", "kidney beans", "kidney bean", "mushrooms", "mushroom", "cabbages", "cabbage", "carrots", "carrot", "potatoes", "potato", "onions", "onion", "garlics", "garlic", "lentils", "lentil", "chickpeas", "chickpea", "beans", "bean", "peas", "pea", "turnips", "turnip", "vegetables", "vegetable", "veggies", "veggie"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Dairy, true, "goat cheeses", "goat cheese", "cream cheeses", "cream cheese", "yoghurts", "yoghurt", "yogurts", "yogurt", "cheeses", "cheese", "butters", "butter", "creams", "cream", "milks", "milk", "dairy products", "dairy"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.Egg, true, "quail eggs", "quail egg", "duck eggs", "duck egg", "chicken eggs", "chicken egg", "boiled eggs", "boiled egg", "fried eggs", "fried egg", "eggs", "egg"),
		new GeneratedRpFoodSuffixRule(GeneratedRpFoodKind.PreparedMeal, true, "meat pies", "meat pie", "fruit pies", "fruit pie", "cheeseburgers", "cheeseburger", "hot dogs", "hot dog", "sandwiches", "sandwich", "hamburgers", "hamburger", "burgers", "burger", "pizzas", "pizza", "tacos", "taco", "curries", "curry", "kebabs", "kebab", "sushis", "sushi", "salads", "salad", "stews", "stew", "soups", "soup", "pies", "pie", "meals", "meal", "foods", "food")
	};

	public class RewardItemInfo
	{
		public ItemObject Item { get; set; }

		public string StringId { get; set; }

		public string PromptStringId { get; set; }

		public string ModifierStringId { get; set; }

		public string Name { get; set; }

		public int Count { get; set; }

		public int GuidePrice { get; set; }

		public EquipmentElement EquipmentElement { get; set; }

		public bool IsPrivateEquipment { get; set; }
	}

	public sealed class GeneratedInventoryItemSnapshot
	{
		public string GeneratedStringId { get; set; }

		public string DisplayName { get; set; }

		public string TemplateStringId { get; set; }

		public uint ObjectId { get; set; }
	}

	public sealed class GeneratedRewardRpItemComponent : ItemComponent
	{
		public TextObject Description { get; set; }

		public string CreatedBy { get; set; }

		public int CreatedDay { get; set; }

		public GeneratedRewardRpItemComponent()
		{
			Description = new TextObject("{=!}");
			CreatedBy = "";
			CreatedDay = 0;
		}

		private GeneratedRewardRpItemComponent(GeneratedRewardRpItemComponent other)
		{
			Description = other?.Description ?? new TextObject("{=!}");
			CreatedBy = other?.CreatedBy ?? "";
			CreatedDay = other?.CreatedDay ?? 0;
		}

		public override ItemComponent GetCopy()
		{
			return new GeneratedRewardRpItemComponent(this);
		}

		public override void Deserialize(MBObjectManager objectManager, XmlNode node)
		{
			Initialize();
		}
	}

	public class DuelStakeOption
	{
		public string ItemId { get; set; }

		public string Name { get; set; }

		public int Count { get; set; }

		public int GuidePrice { get; set; }

		public ItemObject Item { get; set; }

		public bool IsPrivateEquipment { get; set; }
	}

	private sealed class RewardItemResolutionCandidate
	{
		public RewardItemInfo Info;

		public bool IsContext;

		public int Order;
	}

	private sealed class RewardItemResolution
	{
		public RewardItemInfo Info;

		public ItemObject Item;

		public EquipmentElement EquipmentElement;

		public string ActionKey;

		public string MatchedName;

		public string MatchedStringId;

		public float BestScore;

		public float SecondScore;

		public bool IsContext;

		public bool IsGeneratedFromLowScore;

		public ItemObject TemplateItem;

		public string RequestedName;
	}

	private sealed class HeroJoinOriginalClanRecord
	{
		public string OriginalClanId;

		public string OriginalSettlementId;

		public string OriginalSupporterClanId;

		public int OriginalOccupation;

		public bool WasLord;

		public bool WasNotable;
	}

	private sealed class GeneratedRewardItemRecord
	{
		public string GeneratedStringId;

		public string DisplayName;

		public string TemplateStringId;

		public uint ObjectId;

		public List<uint> LegacyObjectIds;

		public int LastTouchedDay;

		// Shared by every stack that resolves to this deterministic generated id.
		// The runtime "pending" state intentionally lives outside save data.
		public string RpItemIntroductionText;

		public string RpItemIntroductionSource;

		public int RpItemIntroductionLastTouchedDay;

		public PlayerRpCraftData PlayerCraft;
	}

	private sealed class GeneratedRewardRosterItemRecord
	{
		public string GeneratedStringId;

		public string DisplayName;

		public string TemplateStringId;

		public uint ObjectId;

		public int Amount;

		public int LastTouchedDay;
	}

	private sealed class PendingNpcBattleEquipmentRestoreSlot
	{
		public int SlotIndex;

		public string ItemId;

		public string ModifierId;

		public string CosmeticItemId;

		public bool IsQuestItem;

		public float RestoreOnOrAfterDay;
	}

	private sealed class PendingNpcBattleEquipmentRestoreRecord
	{
		public List<PendingNpcBattleEquipmentRestoreSlot> Slots = new List<PendingNpcBattleEquipmentRestoreSlot>();
	}

	public class DebtExportEntry
	{
		public int OwedGold;

		public Dictionary<string, int> OwedItems = new Dictionary<string, int>();

		public float CreatedDay;

		public float DueDay;

		public List<DebtLineExportEntry> DebtLines = new List<DebtLineExportEntry>();
	}

	public class DebtLineExportEntry
	{
		public string DebtId;

		public bool IsGold;

		public string ItemId;

		public bool IsDueUnlimited;

		public bool IsItemUnavailableDeclared;

		public int InitialAmount;

		public int RemainingAmount;

		public float CreatedDay;

		public float DueDay;

		public float BestPreDueCoverage;

		public int OnTimePenaltyTierApplied;

		public int OverduePenaltyDaysApplied;

		public int LastOverduePenaltyDay;

		public int OverdueTrustPenaltyPerDay;

		public int OverdueRelationPenaltyPerDay;

		public int CompensationUnitPrice;

		public int CompensationGoldCredit;

		public long UnlimitedTrustPenaltyNumeratorCarry;

		public string DebtNote;
	}

	private class DebtRecord
	{
		public class DebtLine
		{
			public string DebtId;

			public bool IsGold;

			public string ItemId;

			public bool IsDueUnlimited;

			public bool IsItemUnavailableDeclared;

			public int InitialAmount;

			public int RemainingAmount;

			public float CreatedDay;

			public float DueDay;

			public float BestPreDueCoverage;

			public int OnTimePenaltyTierApplied;

			public int OverduePenaltyDaysApplied;

			public int LastOverduePenaltyDay;

			public int OverdueTrustPenaltyPerDay;

			public int OverdueRelationPenaltyPerDay;

			public int CompensationUnitPrice;

			public int CompensationGoldCredit;

			public long UnlimitedTrustPenaltyNumeratorCarry;

			public string DebtNote;
		}

		public int OwedGold;

		public Dictionary<string, int> OwedItems = new Dictionary<string, int>();

		public float CreatedDay;

		public float DueDay;

		public List<DebtLine> DebtLines = new List<DebtLine>();
	}

	private class PendingPlayerTransfer
	{
		public int Gold;

		public int LastTouchedDay;

		public Dictionary<string, int> Items = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	}

	private class MerchantFactRecord
	{
		public int LastTouchedDay;

		public List<string> Facts = new List<string>();
	}

	private class ItemGuidePriceInfo
	{
		public int UnitPrice;

		public int SampleCount;

		public bool ExpandedSearch;

		public bool UsedNoStockFallback;

		public bool UsedBaseValueFallback;
	}

	private const float DefaultDebtDueDays = 1f;

	private const float NpcBattleEquipmentRestoreDelayDays = 3f;

	private const string PendingNpcBattleEquipmentRestoreStorageKey = "_rewardPendingNpcBattleEquipmentRestore_v1";

	private const int TrustMin = -100;

	private const int TrustMax = 100;

	private const int PublicTrustDeltaUnit = 2;

	private const int AutoTrustValuePerPoint = 1000;

	private const int TrustGainUnitsPerPoint = 1600;

	private const int SettlementTrustUnitsPerTier = 48;

	private const int PublicTrustPoolPointsPerTrust = 3;

	private const int UnlimitedDebtReminderIntervalDays = 7;

	private const int UnlimitedDebtPenaltyReferenceValue = 100000;

	private const int UnlimitedDebtPenaltyTrustUnitsPerReferencePerDay = TrustGainUnitsPerPoint / 100;

	private const double TrustCurveExponent = 3.0;

	private const double TrustCurveMaxScaleOffset = 15.0;

	private const int SettlementTrustContributionSharePercent = 30;

	private const float SettlementTrustBattleEffectRadius = 25f;

	private const int TrustGainOnCleanTrade = 1;

	private const int TrustGainOnOnTimeRepay = 2;

	private const int TrustPenaltyOnOverdueRepay = -2;

	private const float DebtCoverageThreshold = 0.95f;

	private const int OverduePenaltyIntervalDays = 7;

	private const int OverduePenaltyDurationDays = 84;

	private const int OverduePenaltyMaxWeeks = OverduePenaltyDurationDays / OverduePenaltyIntervalDays;

	private const int OverdueTrustPenaltyPerWeekValueStep = 10000;

	private const int OverdueRelationPenaltyPerWeekTrustStep = 5;

	private const int LlmManualPenaltyMax = 10;

	private const int KingdomServiceMercenaryTrustMin = 5;

	private const int KingdomServiceVassalTrustMin = 20;

	private const int TrustGainOnQuestSuccess = 5;

	private static readonly string[] TrustLevelTexts = new string[10] { "彻底不信", "极度怀疑", "强烈戒备", "不太信任", "保留态度", "中性观望", "基本信任", "较高信任", "高度信任", "完全信赖" };

	private static readonly string[] TrustAiBehaviorTexts = new string[10] { "彻底不信任玩家，优先回避一切经济接触。", "仍高度怀疑玩家，只愿进行极端保守的交易试探。", "明显戒备，允许有限交易但条件苛刻。", "偏不信任，接受交易但会要求更高风险补偿。", "保留态度，交易可谈但会附加明显溢价。", "中性观望，可做常规交易但仍以安全为先。", "达到基本信任，可在可控风险下提供信用交易。", "较高信任，可提供中大额信用交易。", "高度信任，可提供高额信用交易。", "完全信赖，可提供接近无限额的信用交易。" };

	private static readonly string[] TrustAiActionGuideTexts = new string[10] { "L1：拒绝任何经济往来。禁止买卖、禁止赊账、禁止借款；不输出任何新增交易/欠款标签。", "L2：可进行即时交易，但应明显高于 guidePrice 定价以覆盖风险；禁止赊账/借款。", "L3：可进行即时交易，定价仍偏高；禁止赊账/借款。", "L4：可进行即时交易，定价可在“偏高到合理”之间浮动；禁止赊账/借款。", "L5：可进行即时交易，定价趋于合理但可保留风险溢价；禁止赊账/借款。", "L6：可进行即时交易，定价基本合理；禁止赊账/借款。", "L7：可赊账/借款（小到中额），额度需结合NPC财力与局势评估；默认计息。", "L8：可赊账/借款（中到较大额），额度需结合NPC财力与局势评估；默认计息。", "L9：可赊账/借款（大额），额度需结合NPC财力与局势评估；默认计息。", "L10：可赊账/借款（极高额度），仍需结合NPC财力、性格与风险控制；默认计息。" };

	private Dictionary<string, DebtRecord> _debts = new Dictionary<string, DebtRecord>();

	// New promises are queued until their originating conversation has naturally closed before calling QuestBase.StartQuest.
	private HashSet<string> _pendingDebtPromiseQuestKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, string> _debtStorage = new Dictionary<string, string>();

	private Dictionary<string, int> _npcTrust = new Dictionary<string, int>();

	private Dictionary<string, int> _publicTrust = new Dictionary<string, int>();

	private Dictionary<string, int> _npcTrustStorage = new Dictionary<string, int>();

	private Dictionary<string, int> _publicTrustStorage = new Dictionary<string, int>();

	private Dictionary<string, int> _tradeTrustValueCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _tradeTrustValueCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _directTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _directTrustProgressCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _settlementTrustCentiCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _settlementTrustCentiCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _settlementTrustSharedPublicCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _settlementTrustSharedPublicCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _publicTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _publicTrustProgressCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private int _currentBattlePlayerActualSettlementTrustUnits;

	private Dictionary<string, PendingPlayerTransfer> _pendingPlayerTransfers = new Dictionary<string, PendingPlayerTransfer>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, MerchantFactRecord> _merchantFacts = new Dictionary<string, MerchantFactRecord>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, string> _merchantFactStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, HeroJoinOriginalClanRecord> _heroJoinOriginalClanRecords = new Dictionary<string, HeroJoinOriginalClanRecord>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, string> _heroJoinOriginalClanStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, GeneratedRewardItemRecord> _generatedRewardItemRecords = new Dictionary<string, GeneratedRewardItemRecord>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, string> _generatedRewardItemStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, GeneratedRewardRosterItemRecord> _generatedRewardPlayerRosterRecords = new Dictionary<string, GeneratedRewardRosterItemRecord>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, string> _generatedRewardPlayerRosterStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, PendingNpcBattleEquipmentRestoreRecord> _pendingNpcBattleEquipmentRestoreRecords = new Dictionary<string, PendingNpcBattleEquipmentRestoreRecord>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, string> _pendingNpcBattleEquipmentRestoreStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private List<string> _lastGeneratedNpcFactLines = new List<string>();

	private static readonly Dictionary<int, Hero> _promotedNonHeroCompanionsByAgentIndex = new Dictionary<int, Hero>();

	private static Mission _promotedNonHeroCompanionMission;

	private sealed class WildernessNonHeroJoinConversationCloseContext
	{
		public PartyBase SourcePartyBase;

		public MobileParty SourceParty;

		public CharacterObject TargetCharacter;

		public int TargetAgentIndex;

		public string SourcePartyId;

		public string TargetCharacterId;

	}

	private sealed class PendingHeroJoinConversationClose
	{
		public Hero JoinedHero;

		public CharacterObject TargetCharacter;

		public PartyBase OriginalParty;

		public MobileParty OriginalMobileParty;

		public string JoinedHeroId;

		public string TargetCharacterId;

		public string OriginalPartyId;

		public bool DestroyOriginalPartyIfEmpty;

		public long CreatedUtcTicks;
	}

	private enum TavernMercenaryPoolJoinResolution
	{
		NotPoolTarget,
		Ready,
		Stale
	}

	private static readonly object HeroJoinConversationCloseLock = new object();

	private static PendingHeroJoinConversationClose _pendingHeroJoinConversationClose;

	private static int _hasPendingHeroJoinConversationClose;

	public static RewardSystemBehavior Instance { get; private set; }

	private static void ShowRewardQuickInfo(string message, Hero npcHero)
	{
		AnimusForgeQuickInfo.ShowForDuration(message, RewardQuickInfoDurationMs, npcHero?.CharacterObject);
	}

	private static void ShowRewardQuickInfo(string message, BasicCharacterObject npcCharacter)
	{
		AnimusForgeQuickInfo.ShowForDuration(message, RewardQuickInfoDurationMs, npcCharacter);
	}

	private static void ShowRewardMessage(string message, Color color, Hero npcHero)
	{
		InformationManager.DisplayMessage(new InformationMessage(message, color));
		ShowRewardQuickInfo(message, npcHero);
	}

	private static void ShowRewardMessage(string message, Color color, BasicCharacterObject npcCharacter)
	{
		InformationManager.DisplayMessage(new InformationMessage(message, color));
		ShowRewardQuickInfo(message, npcCharacter);
	}

	private static void ShowRewardMessage(string message, Hero npcHero)
	{
		InformationManager.DisplayMessage(new InformationMessage(message));
		ShowRewardQuickInfo(message, npcHero);
	}

	private static void ShowRewardMessage(string message, BasicCharacterObject npcCharacter)
	{
		InformationManager.DisplayMessage(new InformationMessage(message));
		ShowRewardQuickInfo(message, npcCharacter);
	}

	public RewardSystemBehavior()
	{
		Instance = this;
	}

	public override void RegisterEvents()
	{
		ClearGeneratedRewardRuntimeState("register_events");
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailyTickSettlement);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.OnPlayerPartyKnockedOrKilledTroopEvent.AddNonSerializedListener(this, OnPlayerPartyKnockedOrKilledTroop);
		CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
		CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, OnCompanionRemoved);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
	}

	public static void RegisterHarmonyPatches(Harmony harmony)
	{
		Harmony patcher = harmony ?? new Harmony("com.AnimusForge.reward.generateditems");
		try
		{
			MethodInfo getObjectByGuid = AccessTools.Method(typeof(MBObjectManager), nameof(MBObjectManager.GetObject), new Type[1] { typeof(MBGUID) });
			MethodInfo getObjectByGuidPostfix = AccessTools.Method(typeof(RewardSystemBehavior), nameof(MBObjectManagerGetObjectPostfix));
			if (getObjectByGuid != null && getObjectByGuidPostfix != null)
			{
				patcher.Patch(getObjectByGuid, postfix: new HarmonyMethod(getObjectByGuidPostfix));
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("RewardSystem", ">>> Generated item object lookup patch failed: " + ex.Message);
		}
		try
		{
			MethodInfo getObjectByString = AccessTools.Method(typeof(MBObjectManager), nameof(MBObjectManager.GetObject), new Type[1] { typeof(string) });
			MethodInfo getObjectByStringPostfix = AccessTools.Method(typeof(RewardSystemBehavior), nameof(MBObjectManagerGetObjectByStringPostfix));
			if (getObjectByString != null && getObjectByStringPostfix != null)
			{
				patcher.Patch(getObjectByString, postfix: new HarmonyMethod(getObjectByStringPostfix));
			}
		}
		catch (Exception ex2)
		{
			Logger.LogTrace("RewardSystem", ">>> Generated item string lookup patch failed: " + ex2.Message);
		}
		try
		{
			MethodInfo replaceInvalidItemsWithTrash = AccessTools.Method(typeof(ItemRoster), "ReplaceInvalidItemsWithTrash", new Type[1] { typeof(ItemRoster) });
			MethodInfo replaceInvalidItemsWithTrashPrefix = AccessTools.Method(typeof(RewardSystemBehavior), nameof(ItemRosterReplaceInvalidItemsWithTrashPrefix));
			if (replaceInvalidItemsWithTrash != null && replaceInvalidItemsWithTrashPrefix != null)
			{
				patcher.Patch(replaceInvalidItemsWithTrash, prefix: new HarmonyMethod(replaceInvalidItemsWithTrashPrefix));
			}
		}
		catch (Exception ex3)
		{
			Logger.LogTrace("RewardSystem", ">>> Generated item pre-trash roster repair patch failed: " + ex3.Message);
		}
		try
		{
			Type inventoryVmType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.ViewModelCollection.Inventory.SPInventoryVM");
			MethodInfo refreshInformationValues = inventoryVmType != null ? AccessTools.Method(inventoryVmType, "RefreshInformationValues") : null;
			MethodInfo refreshInformationValuesPostfix = AccessTools.Method(typeof(RewardSystemBehavior), nameof(SPInventoryVMRefreshInformationValuesPostfix));
			if (refreshInformationValues != null && refreshInformationValuesPostfix != null)
			{
				patcher.Patch(refreshInformationValues, postfix: new HarmonyMethod(refreshInformationValuesPostfix));
			}
		}
		catch (Exception ex4)
		{
			Logger.LogTrace("RewardSystem", ">>> Generated item inventory VM diagnostics patch failed: " + ex4.Message);
		}
		try
		{
			Type itemMenuVmType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.ViewModelCollection.Inventory.ItemMenuVM");
			MethodInfo refreshItemTooltips = itemMenuVmType != null ? AccessTools.Method(itemMenuVmType, "RefreshItemTooltips") : null;
			MethodInfo refreshItemTooltipsPostfix = AccessTools.Method(typeof(RewardSystemBehavior), nameof(ItemMenuVMRefreshItemTooltipsPostfix));
			if (refreshItemTooltips != null && refreshItemTooltipsPostfix != null)
			{
				patcher.Patch(refreshItemTooltips, postfix: new HarmonyMethod(refreshItemTooltipsPostfix));
			}
		}
		catch (Exception ex5)
		{
			Logger.LogTrace("RewardSystem", ">>> Generated item tooltip patch failed: " + ex5.Message);
		}
		try
		{
			Type inventoryVmType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.ViewModelCollection.Inventory.SPInventoryVM");
			MethodInfo processPreviewItem = inventoryVmType != null ? AccessTools.Method(inventoryVmType, "ProcessPreviewItem", new Type[1] { typeof(ItemVM) }) : null;
			MethodInfo processPreviewItemPrefix = AccessTools.Method(typeof(RewardSystemBehavior), nameof(SPInventoryVMProcessPreviewItemPrefix));
			if (processPreviewItem != null && processPreviewItemPrefix != null)
			{
				patcher.Patch(processPreviewItem, prefix: new HarmonyMethod(processPreviewItemPrefix));
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("RewardSystem", ">>> Courier letter inventory preview patch failed: " + ex.Message);
		}
		try
		{
			MethodInfo openScreenAsTrade = AccessTools.Method(typeof(InventoryScreenHelper), nameof(InventoryScreenHelper.OpenScreenAsTrade), new Type[4]
			{
				typeof(ItemRoster),
				typeof(SettlementComponent),
				typeof(InventoryScreenHelper.InventoryCategoryType),
				typeof(Action)
			});
			MethodInfo openScreenAsTradePrefix = AccessTools.Method(typeof(RewardSystemBehavior), nameof(InventoryScreenHelperOpenScreenAsTradePrefix));
			if (openScreenAsTrade != null && openScreenAsTradePrefix != null)
			{
				patcher.Patch(openScreenAsTrade, prefix: new HarmonyMethod(openScreenAsTradePrefix));
			}
		}
		catch (Exception ex6)
		{
			Logger.LogTrace("RewardSystem", ">>> Generated item market trade cleanup patch failed: " + ex6.Message);
		}
		try
		{
			MethodInfo addTransferCommand = AccessTools.Method(typeof(InventoryLogic), nameof(InventoryLogic.AddTransferCommand), new Type[1] { typeof(TransferCommand) });
			MethodInfo addTransferCommandPrefix = AccessTools.Method(typeof(RewardSystemBehavior), nameof(InventoryLogicAddTransferCommandPrefix));
			if (addTransferCommand != null && addTransferCommandPrefix != null)
			{
				patcher.Patch(addTransferCommand, prefix: new HarmonyMethod(addTransferCommandPrefix));
			}
		}
		catch (Exception ex7)
		{
			Logger.LogTrace("RewardSystem", ">>> Generated item market transfer guard patch failed: " + ex7.Message);
		}
		try
		{
			MethodInfo addTransferCommands = AccessTools.Method(typeof(InventoryLogic), nameof(InventoryLogic.AddTransferCommands), new Type[1] { typeof(IEnumerable<TransferCommand>) });
			MethodInfo addTransferCommandsPrefix = AccessTools.Method(typeof(RewardSystemBehavior), nameof(InventoryLogicAddTransferCommandsPrefix));
			if (addTransferCommands != null && addTransferCommandsPrefix != null)
			{
				patcher.Patch(addTransferCommands, prefix: new HarmonyMethod(addTransferCommandsPrefix));
			}
		}
		catch (Exception ex8)
		{
			Logger.LogTrace("RewardSystem", ">>> Generated item market batch transfer guard patch failed: " + ex8.Message);
		}
		try
		{
			MethodInfo sellItems = AccessTools.Method(typeof(SellItemsAction), nameof(SellItemsAction.Apply), new Type[5]
			{
				typeof(PartyBase),
				typeof(PartyBase),
				typeof(ItemRosterElement),
				typeof(int),
				typeof(Settlement)
			});
			MethodInfo sellItemsPrefix = AccessTools.Method(typeof(RewardSystemBehavior), nameof(SellItemsActionApplyPrefix));
			if (sellItems != null && sellItemsPrefix != null)
			{
				patcher.Patch(sellItems, prefix: new HarmonyMethod(sellItemsPrefix));
			}
		}
		catch (Exception ex9)
		{
			Logger.LogTrace("RewardSystem", ">>> Generated item settlement sale guard patch failed: " + ex9.Message);
		}
		try
		{
			MethodInfo villagerTrade = AccessTools.Method(typeof(SellGoodsForTradeAction), nameof(SellGoodsForTradeAction.ApplyByVillagerTrade));
			PatchGeneratedRewardMarketPartySale(patcher, villagerTrade, nameof(VillagerSellGoodsPrefix), nameof(VillagerSellGoodsFinalizer));
		}
		catch (Exception ex10)
		{
			Logger.LogTrace("RewardSystem", ">>> Generated item villager market sale guard patch failed: " + ex10.Message);
		}
		try
		{
			MethodInfo caravanSellGoods = AccessTools.Method(typeof(CaravansCampaignBehavior), "SellGoodsInternal");
			PatchGeneratedRewardMarketPartySale(patcher, caravanSellGoods, nameof(CaravanSellGoodsPrefix), nameof(CaravanSellGoodsFinalizer));
		}
		catch (Exception ex11)
		{
			Logger.LogTrace("RewardSystem", ">>> Generated item caravan market sale guard patch failed: " + ex11.Message);
		}
		try
		{
			MethodInfo allItemsGetter = AccessTools.PropertyGetter(typeof(Campaign), "AllItems");
			MethodInfo allItemsGetterPostfix = AccessTools.Method(typeof(RewardSystemBehavior), nameof(CampaignAllItemsGetterPostfix));
			if (allItemsGetter != null && allItemsGetterPostfix != null)
			{
				patcher.Patch(allItemsGetter, postfix: new HarmonyMethod(allItemsGetterPostfix));
			}
		}
		catch (Exception ex12)
		{
			Logger.LogTrace("RewardSystem", ">>> Generated item global economy pool guard patch failed: " + ex12.Message);
		}
		try
		{
			MethodInfo fillItemsInAllCategories = AccessTools.Method(typeof(WorkshopsCampaignBehavior), "FillItemsInAllCategories", Type.EmptyTypes);
			MethodInfo fillItemsInAllCategoriesPostfix = AccessTools.Method(typeof(RewardSystemBehavior), nameof(WorkshopsCampaignBehaviorFillItemsInAllCategoriesPostfix));
			if (fillItemsInAllCategories != null && fillItemsInAllCategoriesPostfix != null)
			{
				patcher.Patch(fillItemsInAllCategories, postfix: new HarmonyMethod(fillItemsInAllCategoriesPostfix));
			}
		}
		catch (Exception ex13)
		{
			Logger.LogTrace("RewardSystem", ">>> Generated item workshop category cache guard patch failed: " + ex13.Message);
		}
		try
		{
			MethodInfo getRandomWorkshopItem = AccessTools.Method(typeof(WorkshopsCampaignBehavior), "GetRandomItem", new Type[2] { typeof(ItemCategory), typeof(Town) });
			MethodInfo getRandomWorkshopItemPostfix = AccessTools.Method(typeof(RewardSystemBehavior), nameof(WorkshopsCampaignBehaviorGetRandomItemPostfix));
			if (getRandomWorkshopItem != null && getRandomWorkshopItemPostfix != null)
			{
				patcher.Patch(getRandomWorkshopItem, postfix: new HarmonyMethod(getRandomWorkshopItemPostfix));
			}
		}
		catch (Exception ex14)
		{
			Logger.LogTrace("RewardSystem", ">>> Generated item workshop output guard patch failed: " + ex14.Message);
		}
		try
		{
			MethodInfo hideoutSessionLaunched = AccessTools.Method(typeof(HideoutCampaignBehavior), "OnSessionLaunched", new Type[1] { typeof(CampaignGameStarter) });
			MethodInfo hideoutSessionLaunchedPostfix = AccessTools.Method(typeof(RewardSystemBehavior), nameof(HideoutCampaignBehaviorOnSessionLaunchedPostfix));
			if (hideoutSessionLaunched != null && hideoutSessionLaunchedPostfix != null)
			{
				patcher.Patch(hideoutSessionLaunched, postfix: new HarmonyMethod(hideoutSessionLaunchedPostfix));
			}
		}
		catch (Exception ex15)
		{
			Logger.LogTrace("RewardSystem", ">>> Generated item hideout loot pool guard patch failed: " + ex15.Message);
		}
	}

	private static void PatchGeneratedRewardMarketPartySale(Harmony patcher, MethodInfo original, string prefixName, string finalizerName)
	{
		MethodInfo prefix = AccessTools.Method(typeof(RewardSystemBehavior), prefixName);
		MethodInfo finalizer = AccessTools.Method(typeof(RewardSystemBehavior), finalizerName);
		if (original != null && prefix != null && finalizer != null)
		{
			patcher.Patch(original, prefix: new HarmonyMethod(prefix), finalizer: new HarmonyMethod(finalizer));
		}
	}

	private static MethodInfo GetSerializableObjectDeserializeMethod(Type type)
	{
		if (type == null)
		{
			return null;
		}
		try
		{
			InterfaceMapping interfaceMap = type.GetInterfaceMap(typeof(ISerializableObject));
			for (int i = 0; i < interfaceMap.InterfaceMethods.Length; i++)
			{
				if (string.Equals(interfaceMap.InterfaceMethods[i].Name, nameof(ISerializableObject.DeserializeFrom), StringComparison.Ordinal))
				{
					return interfaceMap.TargetMethods[i];
				}
			}
		}
		catch
		{
		}
		return AccessTools.Method(type, "TaleWorlds.Library.ISerializableObject.DeserializeFrom", new Type[1] { typeof(IReader) });
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (_debts == null)
		{
			_debts = new Dictionary<string, DebtRecord>();
		}
		if (_debtStorage == null)
		{
			_debtStorage = new Dictionary<string, string>();
		}
		if (_npcTrust == null)
		{
			_npcTrust = new Dictionary<string, int>();
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		if (_npcTrustStorage == null)
		{
			_npcTrustStorage = new Dictionary<string, int>();
		}
		if (_publicTrustStorage == null)
		{
			_publicTrustStorage = new Dictionary<string, int>();
		}
		if (_tradeTrustValueCarry == null)
		{
			_tradeTrustValueCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_tradeTrustValueCarryStorage == null)
		{
			_tradeTrustValueCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_directTrustProgressCarry == null)
		{
			_directTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_directTrustProgressCarryStorage == null)
		{
			_directTrustProgressCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_settlementTrustCentiCarry == null)
		{
			_settlementTrustCentiCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_settlementTrustCentiCarryStorage == null)
		{
			_settlementTrustCentiCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_settlementTrustSharedPublicCarry == null)
		{
			_settlementTrustSharedPublicCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_settlementTrustSharedPublicCarryStorage == null)
		{
			_settlementTrustSharedPublicCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_publicTrustProgressCarry == null)
		{
			_publicTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_publicTrustProgressCarryStorage == null)
		{
			_publicTrustProgressCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_merchantFacts == null)
		{
			_merchantFacts = new Dictionary<string, MerchantFactRecord>(StringComparer.OrdinalIgnoreCase);
		}
		if (_merchantFactStorage == null)
		{
			_merchantFactStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		if (_heroJoinOriginalClanRecords == null)
		{
			_heroJoinOriginalClanRecords = new Dictionary<string, HeroJoinOriginalClanRecord>(StringComparer.OrdinalIgnoreCase);
		}
		if (_heroJoinOriginalClanStorage == null)
		{
			_heroJoinOriginalClanStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		if (_generatedRewardItemRecords == null)
		{
			_generatedRewardItemRecords = new Dictionary<string, GeneratedRewardItemRecord>(StringComparer.OrdinalIgnoreCase);
		}
		if (_generatedRewardItemStorage == null)
		{
			_generatedRewardItemStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		if (_generatedRewardPlayerRosterRecords == null)
		{
			_generatedRewardPlayerRosterRecords = new Dictionary<string, GeneratedRewardRosterItemRecord>(StringComparer.OrdinalIgnoreCase);
		}
		if (_generatedRewardPlayerRosterStorage == null)
		{
			_generatedRewardPlayerRosterStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		if (_pendingNpcBattleEquipmentRestoreRecords == null)
		{
			_pendingNpcBattleEquipmentRestoreRecords = new Dictionary<string, PendingNpcBattleEquipmentRestoreRecord>(StringComparer.OrdinalIgnoreCase);
		}
		if (_pendingNpcBattleEquipmentRestoreStorage == null)
		{
			_pendingNpcBattleEquipmentRestoreStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		try
		{
			_debtStorage.Clear();
			foreach (KeyValuePair<string, DebtRecord> debt in _debts)
			{
				if (string.IsNullOrEmpty(debt.Key) || debt.Value == null)
				{
					continue;
				}
				try
				{
					NormalizeDebtRecord(debt.Value);
					if (HasDebtContent(debt.Value))
					{
						string value = JsonConvert.SerializeObject(debt.Value);
						_debtStorage[debt.Key] = value;
					}
				}
				catch (Exception ex)
				{
					Logger.Log("RewardSystem", "[ERROR] Serialize debt for " + debt.Key + ": " + ex.Message);
				}
			}
			Dictionary<string, string> dictionary = CampaignSaveChunkHelper.FlattenStringDictionary(_debtStorage, "_rewardDebts_v2", "RewardDebt");
			dataStore.SyncData("_rewardDebts_v2", ref dictionary);
			_debtStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary, "RewardSystem");
			_debts.Clear();
			if (_debtStorage != null)
			{
				foreach (KeyValuePair<string, string> item in _debtStorage)
				{
					if (string.IsNullOrEmpty(item.Key) || string.IsNullOrEmpty(item.Value))
					{
						continue;
					}
					try
					{
						DebtRecord debtRecord = JsonConvert.DeserializeObject<DebtRecord>(item.Value);
						if (debtRecord != null)
						{
							NormalizeDebtRecord(debtRecord);
							if (HasDebtContent(debtRecord))
							{
								_debts[item.Key] = debtRecord;
							}
						}
					}
					catch (Exception ex2)
					{
						Logger.Log("RewardSystem", "[ERROR] Deserialize debt for " + item.Key + ": " + ex2.Message);
					}
				}
			}
			SyncMerchantFactData(dataStore);
			SyncTrustData(dataStore);
			SyncTradeTrustCarryData(dataStore);
			SyncDirectTrustProgressCarryData(dataStore);
			SyncSettlementTrustCarryData(dataStore);
			SyncPublicTrustProgressCarryData(dataStore);
			SyncHeroJoinOriginalClanData(dataStore);
			SyncGeneratedRewardItemData(dataStore);
			SyncPendingNpcBattleEquipmentRestoreData(dataStore);
		}
		catch (Exception ex3)
		{
			Logger.Log("RewardSystem", "[ERROR] SyncData v2 failed: " + ex3.ToString());
			_debts = new Dictionary<string, DebtRecord>();
			_debtStorage = new Dictionary<string, string>();
			_npcTrust = new Dictionary<string, int>();
			_publicTrust = new Dictionary<string, int>();
			_npcTrustStorage = new Dictionary<string, int>();
			_publicTrustStorage = new Dictionary<string, int>();
			_tradeTrustValueCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_tradeTrustValueCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_directTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_directTrustProgressCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_settlementTrustCentiCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_settlementTrustCentiCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_settlementTrustSharedPublicCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_settlementTrustSharedPublicCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_publicTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_publicTrustProgressCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			_merchantFacts = new Dictionary<string, MerchantFactRecord>(StringComparer.OrdinalIgnoreCase);
			_merchantFactStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			_heroJoinOriginalClanRecords = new Dictionary<string, HeroJoinOriginalClanRecord>(StringComparer.OrdinalIgnoreCase);
			_heroJoinOriginalClanStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			_generatedRewardItemRecords = new Dictionary<string, GeneratedRewardItemRecord>(StringComparer.OrdinalIgnoreCase);
			_generatedRewardItemStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			_generatedRewardPlayerRosterRecords = new Dictionary<string, GeneratedRewardRosterItemRecord>(StringComparer.OrdinalIgnoreCase);
			_generatedRewardPlayerRosterStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			_pendingNpcBattleEquipmentRestoreRecords = new Dictionary<string, PendingNpcBattleEquipmentRestoreRecord>(StringComparer.OrdinalIgnoreCase);
			_pendingNpcBattleEquipmentRestoreStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
	}

	private void SyncHeroJoinOriginalClanData(IDataStore dataStore)
	{
		if (_heroJoinOriginalClanRecords == null)
		{
			_heroJoinOriginalClanRecords = new Dictionary<string, HeroJoinOriginalClanRecord>(StringComparer.OrdinalIgnoreCase);
		}
		if (_heroJoinOriginalClanStorage == null)
		{
			_heroJoinOriginalClanStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		try
		{
			_heroJoinOriginalClanStorage.Clear();
			foreach (KeyValuePair<string, HeroJoinOriginalClanRecord> item in _heroJoinOriginalClanRecords)
			{
				if (string.IsNullOrWhiteSpace(item.Key) || item.Value == null || (string.IsNullOrWhiteSpace(item.Value.OriginalClanId) && string.IsNullOrWhiteSpace(item.Value.OriginalSettlementId)))
				{
					continue;
				}
				_heroJoinOriginalClanStorage[item.Key] = JsonConvert.SerializeObject(item.Value);
			}
			Dictionary<string, string> dictionary = CampaignSaveChunkHelper.FlattenStringDictionary(_heroJoinOriginalClanStorage, "_rewardHeroJoinOriginalClans_v1", "RewardHeroJoinOriginalClan");
			dataStore.SyncData("_rewardHeroJoinOriginalClans_v1", ref dictionary);
			_heroJoinOriginalClanStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary, "RewardSystem");
			_heroJoinOriginalClanRecords.Clear();
			foreach (KeyValuePair<string, string> item2 in _heroJoinOriginalClanStorage)
			{
				if (string.IsNullOrWhiteSpace(item2.Key) || string.IsNullOrWhiteSpace(item2.Value))
				{
					continue;
				}
				try
				{
					HeroJoinOriginalClanRecord record = JsonConvert.DeserializeObject<HeroJoinOriginalClanRecord>(item2.Value);
					if (record != null && (!string.IsNullOrWhiteSpace(record.OriginalClanId) || !string.IsNullOrWhiteSpace(record.OriginalSettlementId)))
					{
						_heroJoinOriginalClanRecords[item2.Key] = record;
					}
				}
				catch (Exception ex)
				{
					Logger.Log("RewardSystem", "[HeroJoinOriginalClan] deserialize failed hero=" + item2.Key + " error=" + ex.Message);
				}
			}
		}
		catch (Exception ex2)
		{
			Logger.Log("RewardSystem", "[HeroJoinOriginalClan] SyncData failed: " + ex2.Message);
			_heroJoinOriginalClanRecords = new Dictionary<string, HeroJoinOriginalClanRecord>(StringComparer.OrdinalIgnoreCase);
			_heroJoinOriginalClanStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
	}

	private static bool IsValidNpcBattleEquipmentRestoreSlot(int slotIndex)
	{
		return slotIndex >= 0 && slotIndex < (int)EquipmentIndex.NumEquipmentSetSlots;
	}

	private static bool CanTransferNpcBattleEquipment(EquipmentIndex slot, EquipmentElement equipmentElement)
	{
		ItemObject item = equipmentElement.Item;
		return IsValidNpcBattleEquipmentRestoreSlot((int)slot)
			&& item != null
			&& item != DefaultItems.Trash
			&& !equipmentElement.IsQuestItem
			&& !item.IsBannerItem;
	}

	private static PendingNpcBattleEquipmentRestoreRecord NormalizePendingNpcBattleEquipmentRestoreRecord(string heroId, PendingNpcBattleEquipmentRestoreRecord record)
	{
		if (string.IsNullOrWhiteSpace(heroId) || record == null || record.Slots == null)
		{
			return null;
		}
		Dictionary<int, PendingNpcBattleEquipmentRestoreSlot> slotsByIndex = new Dictionary<int, PendingNpcBattleEquipmentRestoreSlot>();
		foreach (PendingNpcBattleEquipmentRestoreSlot slot in record.Slots)
		{
			string itemId = (slot?.ItemId ?? "").Trim();
			if (slot == null || !IsValidNpcBattleEquipmentRestoreSlot(slot.SlotIndex) || string.IsNullOrWhiteSpace(itemId))
			{
				continue;
			}
			slotsByIndex[slot.SlotIndex] = new PendingNpcBattleEquipmentRestoreSlot
			{
				SlotIndex = slot.SlotIndex,
				ItemId = itemId,
				ModifierId = (slot.ModifierId ?? "").Trim(),
				CosmeticItemId = (slot.CosmeticItemId ?? "").Trim(),
				IsQuestItem = slot.IsQuestItem,
				RestoreOnOrAfterDay = Math.Max(0f, slot.RestoreOnOrAfterDay)
			};
		}
		if (slotsByIndex.Count == 0)
		{
			return null;
		}
		record.Slots = slotsByIndex.Values.OrderBy((PendingNpcBattleEquipmentRestoreSlot x) => x.SlotIndex).ToList();
		return record;
	}

	private void SyncPendingNpcBattleEquipmentRestoreData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		if (_pendingNpcBattleEquipmentRestoreRecords == null)
		{
			_pendingNpcBattleEquipmentRestoreRecords = new Dictionary<string, PendingNpcBattleEquipmentRestoreRecord>(StringComparer.OrdinalIgnoreCase);
		}
		if (_pendingNpcBattleEquipmentRestoreStorage == null)
		{
			_pendingNpcBattleEquipmentRestoreStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		try
		{
			if (dataStore.IsSaving)
			{
				_pendingNpcBattleEquipmentRestoreStorage.Clear();
				foreach (KeyValuePair<string, PendingNpcBattleEquipmentRestoreRecord> item in _pendingNpcBattleEquipmentRestoreRecords)
				{
					PendingNpcBattleEquipmentRestoreRecord record = NormalizePendingNpcBattleEquipmentRestoreRecord(item.Key, item.Value);
					if (record != null)
					{
						_pendingNpcBattleEquipmentRestoreStorage[item.Key] = JsonConvert.SerializeObject(record);
					}
				}
			}
			Dictionary<string, string> storage = dataStore.IsSaving
				? CampaignSaveChunkHelper.FlattenStringDictionary(_pendingNpcBattleEquipmentRestoreStorage, PendingNpcBattleEquipmentRestoreStorageKey, "RewardNpcEquipmentRestore")
				: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			dataStore.SyncData(PendingNpcBattleEquipmentRestoreStorageKey, ref storage);
			_pendingNpcBattleEquipmentRestoreStorage = CampaignSaveChunkHelper.RestoreStringDictionary(storage, "RewardNpcEquipmentRestore") ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (!dataStore.IsLoading)
			{
				return;
			}
			_pendingNpcBattleEquipmentRestoreRecords.Clear();
			foreach (KeyValuePair<string, string> item2 in _pendingNpcBattleEquipmentRestoreStorage)
			{
				if (string.IsNullOrWhiteSpace(item2.Key) || string.IsNullOrWhiteSpace(item2.Value))
				{
					continue;
				}
				try
				{
					PendingNpcBattleEquipmentRestoreRecord record2 = NormalizePendingNpcBattleEquipmentRestoreRecord(item2.Key, JsonConvert.DeserializeObject<PendingNpcBattleEquipmentRestoreRecord>(item2.Value));
					if (record2 != null)
					{
						_pendingNpcBattleEquipmentRestoreRecords[item2.Key] = record2;
					}
				}
				catch (Exception ex)
				{
					Logger.Log("RewardSystem", "[NpcEquipmentRestore] deserialize failed hero=" + item2.Key + " error=" + ex.Message);
				}
			}
		}
		catch (Exception ex2)
		{
			Logger.Log("RewardSystem", "[NpcEquipmentRestore] SyncData failed: " + ex2.Message);
			_pendingNpcBattleEquipmentRestoreRecords = new Dictionary<string, PendingNpcBattleEquipmentRestoreRecord>(StringComparer.OrdinalIgnoreCase);
			_pendingNpcBattleEquipmentRestoreStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
	}

	private void SyncGeneratedRewardItemData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		EnsureGeneratedRewardItemData();
		try
		{
			if (dataStore.IsLoading)
			{
				ClearGeneratedRewardRuntimeState(
					"sync_data_load_begin",
					preservePendingItems: true);
				_generatedRewardItemRecords.Clear();
				_generatedRewardItemStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				_generatedRewardPlayerRosterRecords.Clear();
				_generatedRewardPlayerRosterStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				try
				{
					Logger.Log("Logic", "[RewardItemResolve] generated_save_scope_cleared reason=sync_data_load_begin");
				}
				catch
				{
				}
			}
			if (dataStore.IsSaving)
			{
				CaptureGeneratedRewardPlayerRosterItems("sync_data_save");
				_generatedRewardItemStorage.Clear();
				foreach (KeyValuePair<string, GeneratedRewardItemRecord> item in _generatedRewardItemRecords)
				{
					GeneratedRewardItemRecord record = NormalizeGeneratedRewardItemRecord(item.Key, item.Value);
					if (record == null)
					{
						continue;
					}
					try
					{
						_generatedRewardItemStorage[record.GeneratedStringId] = JsonConvert.SerializeObject(record);
					}
					catch (Exception ex)
					{
						Logger.Log("RewardSystem", "[GeneratedRewardItem] serialize failed item=" + item.Key + " error=" + ex.Message);
					}
				}
			}
			Dictionary<string, string> dictionary = dataStore.IsSaving ? CampaignSaveChunkHelper.FlattenStringDictionary(_generatedRewardItemStorage, "_rewardGeneratedItems_v1", "GeneratedRewardItem") : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			dataStore.SyncData("_rewardGeneratedItems_v1", ref dictionary);
			_generatedRewardItemStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary, "RewardSystem") ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (dataStore.IsSaving)
			{
				SyncGeneratedRewardPlayerRosterData(dataStore);
				SyncGeneratedRewardRecordsToManifest("sync_data_save");
				return;
			}
			ClearGeneratedRewardRuntimeState(
				"sync_data_load",
				preservePendingItems: true);
			_generatedRewardItemRecords.Clear();
			foreach (KeyValuePair<string, string> item2 in _generatedRewardItemStorage)
			{
				if (string.IsNullOrWhiteSpace(item2.Key) || string.IsNullOrWhiteSpace(item2.Value))
				{
					continue;
				}
				try
				{
					GeneratedRewardItemRecord record2 = NormalizeGeneratedRewardItemRecord(item2.Key, JsonConvert.DeserializeObject<GeneratedRewardItemRecord>(item2.Value));
					if (record2 != null)
					{
						_generatedRewardItemRecords[record2.GeneratedStringId] = record2;
					}
				}
				catch (Exception ex2)
				{
					Logger.Log("RewardSystem", "[GeneratedRewardItem] deserialize failed item=" + item2.Key + " error=" + ex2.Message);
				}
			}
			SyncGeneratedRewardPlayerRosterData(dataStore);
			SyncGeneratedRewardRecordsToManifest("sync_data_load");
			RestoreGeneratedRewardItemDefinitions("sync_data_load");
			RestoreGeneratedRewardPlayerRosterItems("sync_data_load");
		}
		catch (Exception ex3)
		{
			Logger.Log("RewardSystem", "[GeneratedRewardItem] SyncData failed: " + ex3.Message);
			_generatedRewardItemRecords = new Dictionary<string, GeneratedRewardItemRecord>(StringComparer.OrdinalIgnoreCase);
			_generatedRewardItemStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			_generatedRewardPlayerRosterRecords = new Dictionary<string, GeneratedRewardRosterItemRecord>(StringComparer.OrdinalIgnoreCase);
			_generatedRewardPlayerRosterStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
	}

	private void SyncGeneratedRewardPlayerRosterData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		EnsureGeneratedRewardItemData();
		try
		{
			if (dataStore.IsSaving)
			{
				_generatedRewardPlayerRosterStorage.Clear();
				foreach (KeyValuePair<string, GeneratedRewardRosterItemRecord> item in _generatedRewardPlayerRosterRecords.ToList())
				{
					GeneratedRewardRosterItemRecord record = NormalizeGeneratedRewardRosterItemRecord(item.Key, item.Value);
					if (record == null || record.Amount <= 0)
					{
						continue;
					}
					try
					{
						_generatedRewardPlayerRosterStorage[record.GeneratedStringId] = JsonConvert.SerializeObject(record);
					}
					catch (Exception ex)
					{
						Logger.Log("RewardSystem", "[GeneratedRewardRoster] serialize failed item=" + item.Key + " error=" + ex.Message);
					}
				}
				_generatedRewardPlayerRosterStorage = CampaignSaveChunkHelper.FlattenStringDictionary(_generatedRewardPlayerRosterStorage, GeneratedRewardPlayerRosterStorageKey, "GeneratedRewardPlayerRoster");
				try
				{
					Logger.Log("Logic", "[RewardItemResolve] generated_player_roster_sync_save records=" + _generatedRewardPlayerRosterRecords.Count + " stored=" + _generatedRewardPlayerRosterStorage.Count + " key=" + GeneratedRewardPlayerRosterStorageKey);
				}
				catch
				{
				}
			}
			Dictionary<string, string> dictionary = dataStore.IsSaving ? _generatedRewardPlayerRosterStorage : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			dataStore.SyncData(GeneratedRewardPlayerRosterStorageKey, ref dictionary);
			if (!dataStore.IsLoading)
			{
				return;
			}
			_generatedRewardPlayerRosterStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary, "GeneratedRewardPlayerRoster") ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			_generatedRewardPlayerRosterRecords.Clear();
			foreach (KeyValuePair<string, string> item in _generatedRewardPlayerRosterStorage)
			{
				if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
				{
					continue;
				}
				try
				{
					GeneratedRewardRosterItemRecord record = NormalizeGeneratedRewardRosterItemRecord(item.Key, JsonConvert.DeserializeObject<GeneratedRewardRosterItemRecord>(item.Value));
					if (record != null && record.Amount > 0)
					{
						_generatedRewardPlayerRosterRecords[record.GeneratedStringId] = record;
					}
				}
				catch (Exception ex2)
				{
					Logger.Log("RewardSystem", "[GeneratedRewardRoster] deserialize failed item=" + item.Key + " error=" + ex2.Message);
				}
			}
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_player_roster_sync_load stored=" + _generatedRewardPlayerRosterStorage.Count + " records=" + _generatedRewardPlayerRosterRecords.Count + " key=" + GeneratedRewardPlayerRosterStorageKey);
			}
			catch
			{
			}
		}
		catch (Exception ex3)
		{
			Logger.Log("RewardSystem", "[GeneratedRewardRoster] SyncData failed: " + ex3.Message);
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_player_roster_sync_failed error=" + ex3.GetType().Name + ":" + ex3.Message);
			}
			catch
			{
			}
			_generatedRewardPlayerRosterRecords = new Dictionary<string, GeneratedRewardRosterItemRecord>(StringComparer.OrdinalIgnoreCase);
			_generatedRewardPlayerRosterStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
	}

	private void OnGameLoadFinished()
	{
		ClearGeneratedRpEquipmentTemplateCache();
		ClearPlayerRpExactTemplateLookupCache();
		ClearPromotedNonHeroCompanionCache();
		RestoreGeneratedRewardItemDefinitions("game_load_finished");
		RestoreGeneratedRewardPlayerRosterItems("game_load_finished");
		RepairGeneratedRewardItemCategories("game_load_finished");
		RemoveGeneratedRewardItemsFromMarketRosters("game_load_finished");
		CourierDeliveryBehavior.Instance?.RestoreCourierLetterInventoryItemsAfterGeneratedRewardRestore("reward_game_load_finished");
		CleanupPlayerCompanionLordCacheDuplicates("game_load_finished");
		RepairPlayerHeroMemberPrisonerDuplicates("game_load_finished");
		RepairInactivePromotedPlayerCompanions("game_load_finished");
		BackfillHeroJoinOriginalClanRecordsForExistingPlayerCompanions();
		CleanupStalePlayerJoinedHeroMapPartiesAfterLoad();
		// A one-time queued reconciliation migrates existing save debts without putting a scan in the daily hot path.
		QueueDebtPromiseQuestsForActiveDebts();
	}

	public void OnEngineTick()
	{
		TryClosePendingHeroJoinConversation();
	}

	private void OnCampaignTick(float dt)
	{
		TryClosePendingHeroJoinConversation();
		// Starting a QuestBase ends the current conversation, so drain only after the conversation manager is idle.
		DrainPendingDebtPromiseQuestCreations();
		DrainRpItemIntroductionCompletionsOnCampaignTick();
		StartQueuedRpItemIntroductionRequestsOnCampaignTick();
	}

	private static void ClearPromotedNonHeroCompanionCache()
	{
		_promotedNonHeroCompanionsByAgentIndex.Clear();
		_promotedNonHeroCompanionMission = Mission.Current;
	}

	private static void EnsurePromotedNonHeroCompanionCacheForCurrentMission()
	{
		Mission current = Mission.Current;
		if (!ReferenceEquals(_promotedNonHeroCompanionMission, current))
		{
			_promotedNonHeroCompanionsByAgentIndex.Clear();
			_promotedNonHeroCompanionMission = current;
		}
	}

	private static void RememberPromotedNonHeroCompanion(int targetAgentIndex, Hero promotedHero)
	{
		EnsurePromotedNonHeroCompanionCacheForCurrentMission();
		if (targetAgentIndex >= 0 && promotedHero != null)
		{
			_promotedNonHeroCompanionsByAgentIndex[targetAgentIndex] = promotedHero;
		}
	}

	private static bool TryGetPromotedNonHeroCompanion(int targetAgentIndex, out Hero promotedHero)
	{
		promotedHero = null;
		EnsurePromotedNonHeroCompanionCacheForCurrentMission();
		if (targetAgentIndex < 0 || !_promotedNonHeroCompanionsByAgentIndex.TryGetValue(targetAgentIndex, out promotedHero) || promotedHero == null)
		{
			return false;
		}
		if (promotedHero.IsPlayerCompanion || promotedHero.PartyBelongedTo == MobileParty.MainParty || IsHeroInParty(promotedHero, MobileParty.MainParty))
		{
			return true;
		}
		_promotedNonHeroCompanionsByAgentIndex.Remove(targetAgentIndex);
		promotedHero = null;
		return false;
	}

	internal static bool TryResolvePromotedNonHeroCompanionForSceneAgentExternal(int targetAgentIndex, out Hero promotedHero)
	{
		try
		{
			return TryGetPromotedNonHeroCompanion(targetAgentIndex, out promotedHero);
		}
		catch
		{
			promotedHero = null;
			return false;
		}
	}

	private static bool IsHeroInParty(Hero hero, MobileParty party)
	{
		try
		{
			return hero?.CharacterObject != null && party?.MemberRoster != null && party.MemberRoster.FindIndexOfTroop(hero.CharacterObject) >= 0;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsHeroInPrisonRoster(Hero hero, PartyBase party)
	{
		try
		{
			return hero?.CharacterObject != null && party?.PrisonRoster != null && party.PrisonRoster.FindIndexOfTroop(hero.CharacterObject) >= 0;
		}
		catch
		{
			return false;
		}
	}

	private static int RemoveAllHeroCopiesFromPrisonRoster(PartyBase party, Hero hero)
	{
		if (party?.PrisonRoster == null || hero?.CharacterObject == null)
		{
			return 0;
		}
		int before = Math.Max(0, party.PrisonRoster.GetTroopCount(hero.CharacterObject));
		if (before <= 0)
		{
			return 0;
		}
		party.PrisonRoster.AddToCounts(hero.CharacterObject, -before, insertAtFront: false, woundedCount: 0, xpChange: 0, removeDepleted: true, index: -1);
		int after = Math.Max(0, party.PrisonRoster.GetTroopCount(hero.CharacterObject));
		return Math.Max(0, before - after);
	}

	private static void RepairPlayerHeroMemberPrisonerDuplicates(string reason)
	{
		try
		{
			Clan playerClan = Clan.PlayerClan;
			MobileParty mainParty = MobileParty.MainParty;
			TroopRoster memberRoster = mainParty?.MemberRoster;
			TroopRoster prisonRoster = mainParty?.Party?.PrisonRoster;
			if (playerClan == null || mainParty == null || memberRoster == null || prisonRoster == null || prisonRoster.Count == 0)
			{
				return;
			}
			HashSet<Hero> duplicates = new HashSet<Hero>();
			for (int i = 0; i < prisonRoster.Count; i++)
			{
				Hero hero = prisonRoster.GetElementCopyAtIndex(i).Character?.HeroObject;
				if (hero == null || hero == Hero.MainHero || hero.PartyBelongedTo != mainParty || memberRoster.GetTroopCount(hero.CharacterObject) <= 0)
				{
					continue;
				}
				if (hero.CompanionOf == playerClan || hero.Clan == playerClan || hero.IsPlayerCompanion)
				{
					duplicates.Add(hero);
				}
			}
			foreach (Hero hero in duplicates)
			{
				Hero.CharacterStates oldState = hero.HeroState;
				int removed = RemoveAllHeroCopiesFromPrisonRoster(mainParty.Party, hero);
				if (removed <= 0 || IsHeroInPrisonRoster(hero, mainParty.Party))
				{
					Logger.Log("RewardSystemBehavior", "[HeroJoin] member_prisoner_duplicate_repair_failed reason=" + (reason ?? "") + " hero=" + (hero.StringId ?? "") + " removed=" + removed);
					continue;
				}
				if (!hero.IsActive && !hero.IsDead)
				{
					hero.ChangeState(Hero.CharacterStates.Active);
				}
				Logger.Log("RewardSystemBehavior", "[HeroJoin] member_prisoner_duplicate_repaired reason=" + (reason ?? "") + " hero=" + (hero.StringId ?? "") + " removed=" + removed + " oldState=" + oldState + " newState=" + hero.HeroState);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("RewardSystemBehavior", "[HeroJoin] member_prisoner_duplicate_repair_failed reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static void CleanupPlayerCompanionLordCacheDuplicates(string reason)
	{
		try
		{
			Clan playerClan = Clan.PlayerClan;
			if (playerClan == null || playerClan.Companions == null)
			{
				return;
			}
			List<Hero> duplicates = playerClan.Companions
				.Where((Hero hero) => hero != null && hero.IsPlayerCompanion && hero.IsWanderer && (playerClan.AliveLords?.Contains(hero) == true || playerClan.DeadLords?.Contains(hero) == true))
				.Distinct()
				.ToList();
			foreach (Hero hero in duplicates)
			{
				hero.Clan = null;
			}
			if (duplicates.Count > 0)
			{
				Logger.Log("RewardSystemBehavior", "[NonHeroJoin] companion_lord_cache_cleanup reason=" + (reason ?? "") + " count=" + duplicates.Count + " heroes=" + string.Join(",", duplicates.Select((Hero h) => h?.StringId ?? "").Where((string x) => !string.IsNullOrWhiteSpace(x))));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("RewardSystemBehavior", "[NonHeroJoin] companion_lord_cache_cleanup_failed reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static void RepairInactivePromotedPlayerCompanions(string reason)
	{
		try
		{
			Clan playerClan = Clan.PlayerClan;
			MobileParty mainParty = MobileParty.MainParty;
			if (playerClan == null || mainParty == null)
			{
				return;
			}
			List<Hero> candidates = new List<Hero>();
			if (playerClan.Companions != null)
			{
				candidates.AddRange(playerClan.Companions.Where((Hero hero) => hero != null));
			}
			if (playerClan.Heroes != null)
			{
				candidates.AddRange(playerClan.Heroes.Where((Hero hero) => hero != null));
			}
			int repaired = 0;
			foreach (Hero hero in candidates.Distinct())
			{
				if (!IsInactivePromotedPlayerCompanionRepairCandidate(hero, mainParty))
				{
					continue;
				}
				if (TryActivatePromotedCompanionHero(hero, reason))
				{
					repaired++;
				}
				LogPromotedCompanionGovernorEligibility(hero, reason + "_repair");
			}
			if (repaired > 0)
			{
				Logger.Log("RewardSystemBehavior", "[NonHeroJoin] inactive_promoted_companion_repair reason=" + (reason ?? "") + " count=" + repaired);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("RewardSystemBehavior", "[NonHeroJoin] inactive_promoted_companion_repair_failed reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static bool IsInactivePromotedPlayerCompanionRepairCandidate(Hero hero, MobileParty mainParty)
	{
		if (hero == null || mainParty == null || hero.IsDead || hero.IsActive || hero.IsHumanPlayerCharacter || hero.IsChild)
		{
			return false;
		}
		if (!hero.IsPlayerCompanion || hero.CompanionOf != Clan.PlayerClan || hero.Occupation != Occupation.Wanderer)
		{
			return false;
		}
		if (hero.IsPrisoner || hero.IsFugitive || hero.IsReleased || hero.IsTraveling || hero.IsSpecial || hero.IsTemplate)
		{
			return false;
		}
		return hero.PartyBelongedTo == mainParty || IsHeroInParty(hero, mainParty);
	}

	private static bool TryActivatePromotedCompanionHero(Hero hero, string reason)
	{
		if (hero == null || hero.IsDead || hero.IsActive)
		{
			return false;
		}
		try
		{
			Hero.CharacterStates oldState = hero.HeroState;
			hero.ChangeState(Hero.CharacterStates.Active);
			Logger.Log("RewardSystemBehavior", "[NonHeroJoin] promoted_companion_activated reason=" + (reason ?? "") + " hero=" + (hero.StringId ?? "") + " oldState=" + oldState + " newState=" + hero.HeroState);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("RewardSystemBehavior", "[NonHeroJoin] promoted_companion_activate_failed reason=" + (reason ?? "") + " hero=" + (hero?.StringId ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	private static void LogPromotedCompanionGovernorEligibility(Hero hero, string reason)
	{
		try
		{
			if (hero == null)
			{
				return;
			}
			bool canHavePartyRole = false;
			try
			{
				canHavePartyRole = hero.CanBeGovernorOrHavePartyRole();
			}
			catch
			{
			}
			bool canBeGovernor = false;
			try
			{
				canBeGovernor = Campaign.Current?.Models?.ClanPoliticsModel?.CanHeroBeGovernor(hero) == true;
			}
			catch
			{
			}
			Logger.Log("RewardSystemBehavior", "[NonHeroJoin] governor_eligibility reason=" + (reason ?? "") +
				" hero=" + (hero.StringId ?? "") +
				" state=" + hero.HeroState +
				" active=" + hero.IsActive +
				" child=" + hero.IsChild +
				" player=" + hero.IsHumanPlayerCharacter +
				" partyLeader=" + hero.IsPartyLeader +
				" fugitive=" + hero.IsFugitive +
				" released=" + hero.IsReleased +
				" traveling=" + hero.IsTraveling +
				" prisoner=" + hero.IsPrisoner +
				" canRole=" + canHavePartyRole +
				" special=" + hero.IsSpecial +
				" template=" + hero.IsTemplate +
				" occupation=" + hero.Occupation +
				" party=" + (hero.PartyBelongedTo?.StringId ?? "") +
				" companionOf=" + (hero.CompanionOf?.StringId ?? "") +
				" clan=" + (hero.Clan?.StringId ?? "") +
				" canGovernor=" + canBeGovernor);
		}
		catch (Exception ex)
		{
			Logger.Log("RewardSystemBehavior", "[NonHeroJoin] governor_eligibility_log_failed reason=" + (reason ?? "") + " hero=" + (hero?.StringId ?? "") + " error=" + ex.Message);
		}
	}

	private static string GetHeroRecordKey(Hero hero)
	{
		try
		{
			string text = (hero?.StringId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
			return (hero?.CharacterObject?.StringId ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static Clan GetHeroBackingClan(Hero hero)
	{
		try
		{
			return HeroClanBackingField?.GetValue(hero) as Clan;
		}
		catch
		{
			return null;
		}
	}

	private static Clan ResolveClanByStringId(string clanId)
	{
		string text = (clanId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return Clan.All?.FirstOrDefault((Clan clan) => clan != null && string.Equals((clan.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static Settlement ResolveSettlementByStringId(string settlementId)
	{
		string text = (settlementId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return Settlement.All?.FirstOrDefault((Settlement settlement) => settlement != null && string.Equals((settlement.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static bool IsClanStillInCampaign(Clan clan)
	{
		if (clan == null)
		{
			return false;
		}
		try
		{
			return Clan.All?.Any((Clan candidate) => ReferenceEquals(candidate, clan)) == true;
		}
		catch
		{
			return true;
		}
	}

	private static bool IsOriginalClanAvailableForDismissedLord(Clan clan)
	{
		try
		{
			return clan != null && clan != Clan.PlayerClan && !clan.IsEliminated && IsClanStillInCampaign(clan);
		}
		catch
		{
			return false;
		}
	}

	private static bool TryResolveOccupation(int value, out Occupation occupation)
	{
		occupation = Occupation.Lord;
		try
		{
			if (Enum.IsDefined(typeof(Occupation), value))
			{
				occupation = (Occupation)value;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsReturnableNotableOccupation(Occupation occupation)
	{
		return occupation == Occupation.Merchant
			|| occupation == Occupation.Artisan
			|| occupation == Occupation.GangLeader
			|| occupation == Occupation.RuralNotable
			|| occupation == Occupation.Headman
			|| occupation == Occupation.Preacher;
	}

	private static bool IsHeroJoinReturnTrackedHero(Hero hero)
	{
		if (hero == null)
		{
			return false;
		}
		try
		{
			return hero.IsLord || hero.IsNotable || IsReturnableNotableOccupation(hero.Occupation);
		}
		catch
		{
			return false;
		}
	}

	private static Settlement ResolveOriginalSettlementForHeroJoin(Hero hero, Settlement currentSettlement)
	{
		if (hero == null)
		{
			return currentSettlement;
		}
		try
		{
			return FirstUsableSettlement(
				currentSettlement,
				ResolveNotableMarketSettlement(hero, currentSettlement),
				hero.CurrentSettlement,
				hero.HomeSettlement,
				hero.BornSettlement,
				Settlement.CurrentSettlement,
				MobileParty.MainParty?.CurrentSettlement);
		}
		catch
		{
			return currentSettlement;
		}
	}

	private void RememberHeroJoinOriginalClan(Hero hero, Clan originalClan, Settlement originalSettlement, string reason)
	{
		try
		{
			string heroKey = GetHeroRecordKey(hero);
			string clanId = (originalClan?.StringId ?? "").Trim();
			string settlementId = (originalSettlement?.StringId ?? "").Trim();
			bool wasLord = hero?.IsLord == true;
			bool wasNotable = false;
			try
			{
				wasNotable = hero?.IsNotable == true || (hero != null && IsReturnableNotableOccupation(hero.Occupation));
			}
			catch
			{
				wasNotable = false;
			}
			if (string.IsNullOrWhiteSpace(heroKey) || (!wasLord && !wasNotable) || (string.IsNullOrWhiteSpace(clanId) && string.IsNullOrWhiteSpace(settlementId)))
			{
				return;
			}
			if (_heroJoinOriginalClanRecords == null)
			{
				_heroJoinOriginalClanRecords = new Dictionary<string, HeroJoinOriginalClanRecord>(StringComparer.OrdinalIgnoreCase);
			}
			_heroJoinOriginalClanRecords[heroKey] = new HeroJoinOriginalClanRecord
			{
				OriginalClanId = originalClan == Clan.PlayerClan ? "" : clanId,
				OriginalSettlementId = settlementId,
				OriginalSupporterClanId = (hero?.SupporterOf?.StringId ?? "").Trim(),
				OriginalOccupation = (int)hero.Occupation,
				WasLord = wasLord,
				WasNotable = wasNotable
			};
			Logger.Log("RewardSystemBehavior", "[HeroJoinOriginalClan] remember hero=" + heroKey + " clan=" + (clanId ?? "") + " settlement=" + (settlementId ?? "") + " reason=" + (reason ?? ""));
		}
		catch (Exception ex)
		{
			Logger.Log("RewardSystemBehavior", "[HeroJoinOriginalClan] remember failed: " + ex.Message);
		}
	}

	private void BackfillHeroJoinOriginalClanRecordsForExistingPlayerCompanions()
	{
		try
		{
			Clan playerClan = Clan.PlayerClan;
			if (playerClan?.Companions == null)
			{
				return;
			}
			foreach (Hero hero in playerClan.Companions)
			{
				if (hero == null || !hero.IsPlayerCompanion || !IsHeroJoinReturnTrackedHero(hero))
				{
					continue;
				}
				string heroKey = GetHeroRecordKey(hero);
				if (!string.IsNullOrWhiteSpace(heroKey) && _heroJoinOriginalClanRecords != null && _heroJoinOriginalClanRecords.ContainsKey(heroKey))
				{
					continue;
				}
				Clan originalClan = GetHeroBackingClan(hero);
				if (originalClan == playerClan)
				{
					originalClan = null;
				}
				Settlement originalSettlement = ResolveOriginalSettlementForHeroJoin(hero, hero.CurrentSettlement);
				if (originalClan != null || originalSettlement != null)
				{
					RememberHeroJoinOriginalClan(hero, originalClan, originalSettlement, "game_load_backfill");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("RewardSystemBehavior", "[HeroJoinOriginalClan] backfill failed: " + ex.Message);
		}
	}

	private void OnCompanionRemoved(Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
	{
		string heroKey = GetHeroRecordKey(companion);
		HeroJoinOriginalClanRecord record = null;
		bool hasRecord = !string.IsNullOrWhiteSpace(heroKey)
			&& _heroJoinOriginalClanRecords != null
			&& _heroJoinOriginalClanRecords.TryGetValue(heroKey, out record);
		if (detail != RemoveCompanionAction.RemoveCompanionDetail.Fire)
		{
			if (hasRecord)
			{
				_heroJoinOriginalClanRecords.Remove(heroKey);
			}
			return;
		}
		try
		{
			Clan originalClan = hasRecord ? ResolveClanByStringId(record.OriginalClanId) : null;
			Settlement originalSettlement = hasRecord ? ResolveSettlementByStringId(record.OriginalSettlementId) : null;
			Clan originalSupporterClan = hasRecord ? ResolveClanByStringId(record.OriginalSupporterClanId) : null;
			if (originalClan == null)
			{
				Clan backingClan = GetHeroBackingClan(companion);
				if (backingClan != null && backingClan != Clan.PlayerClan)
				{
					originalClan = backingClan;
				}
			}
			if (originalSettlement == null)
			{
				originalSettlement = ResolveOriginalSettlementForHeroJoin(companion, companion?.CurrentSettlement);
			}
			bool originalClanWasRecorded = hasRecord && !string.IsNullOrWhiteSpace(record?.OriginalClanId);
			bool wasLord = record?.WasLord == true || companion?.IsLord == true;
			bool wasNotable = record?.WasNotable == true || companion?.IsNotable == true || (companion != null && IsReturnableNotableOccupation(companion.Occupation));
			bool shouldHandle = hasRecord || (wasLord && originalClan != null && originalClan != Clan.PlayerClan) || (wasNotable && originalSettlement != null);
			if (!shouldHandle)
			{
				return;
			}
			if (originalClanWasRecorded && !IsOriginalClanAvailableForDismissedLord(originalClan))
			{
				MakeDismissedHeroWanderer(companion, originalClan);
			}
			else if (wasLord)
			{
				if (IsOriginalClanAvailableForDismissedLord(originalClan))
				{
					RestoreDismissedHeroToOriginalClan(companion, originalClan, hasRecord ? record : null);
				}
				else
				{
					MakeDismissedHeroWanderer(companion, originalClan);
				}
			}
			else if (wasNotable && originalSettlement != null)
			{
				RestoreDismissedHeroToOriginalSettlement(companion, originalSettlement, originalSupporterClan, hasRecord ? record : null);
			}
			else
			{
				MakeDismissedHeroWanderer(companion, originalClan);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("RewardSystemBehavior", "[HeroJoinOriginalClan] companion removed restore failed hero=" + heroKey + " error=" + ex.Message);
		}
		finally
		{
			if (hasRecord && !string.IsNullOrWhiteSpace(heroKey))
			{
				_heroJoinOriginalClanRecords.Remove(heroKey);
			}
		}
	}

	private static void RestoreDismissedHeroToOriginalClan(Hero hero, Clan originalClan, HeroJoinOriginalClanRecord record)
	{
		if (hero == null || originalClan == null)
		{
			return;
		}
		if (hero.Clan != originalClan)
		{
			hero.Clan = originalClan;
		}
		Occupation targetOccupation = Occupation.Lord;
		if (record != null && TryResolveOccupation(record.OriginalOccupation, out Occupation recordedOccupation))
		{
			targetOccupation = recordedOccupation;
		}
		if (record?.WasLord == true)
		{
			targetOccupation = Occupation.Lord;
		}
		if (hero.Occupation != targetOccupation)
		{
			hero.SetNewOccupation(targetOccupation);
		}
		if (originalClan.Leader == null && hero.IsLord)
		{
			originalClan.SetLeader(hero);
		}
		PlaceDismissedFormerLord(hero, originalClan);
		string heroName = hero.Name?.ToString() ?? "Hero";
		string clanName = GetClanDisplayNameForNotification(originalClan);
		InformationManager.DisplayMessage(new InformationMessage("【同伴离队】" + heroName + " 已回到 " + clanName + "。", Color.FromUint(4278242559u)));
		Logger.Log("RewardSystemBehavior", "[HeroJoinOriginalClan] restored hero=" + (hero.StringId ?? "") + " clan=" + (originalClan.StringId ?? ""));
	}

	private static void RestoreDismissedHeroToOriginalSettlement(Hero hero, Settlement originalSettlement, Clan originalSupporterClan, HeroJoinOriginalClanRecord record)
	{
		if (hero == null || originalSettlement == null)
		{
			return;
		}
		if (hero.Clan != null)
		{
			hero.Clan = null;
		}
		Occupation targetOccupation = hero.Occupation;
		if (record != null && TryResolveOccupation(record.OriginalOccupation, out Occupation recordedOccupation))
		{
			targetOccupation = recordedOccupation;
		}
		if (!IsReturnableNotableOccupation(targetOccupation))
		{
			targetOccupation = originalSettlement.IsVillage ? Occupation.RuralNotable : Occupation.Merchant;
		}
		if (hero.Occupation != targetOccupation)
		{
			hero.SetNewOccupation(targetOccupation);
		}
		if (originalSupporterClan != null && !originalSupporterClan.IsEliminated && IsClanStillInCampaign(originalSupporterClan))
		{
			hero.SupporterOf = originalSupporterClan;
		}
		else if (!string.IsNullOrWhiteSpace(record?.OriginalSupporterClanId))
		{
			hero.SupporterOf = null;
		}
		TeleportHeroAction.ApplyImmediateTeleportToSettlement(hero, originalSettlement);
		try
		{
			hero.UpdateHomeSettlement();
		}
		catch
		{
		}
		string heroName = hero.Name?.ToString() ?? "Hero";
		string settlementName = originalSettlement.Name?.ToString() ?? originalSettlement.StringId ?? "原定居点";
		InformationManager.DisplayMessage(new InformationMessage("【同伴离队】" + heroName + " 已回到 " + settlementName + "。", Color.FromUint(4278242559u)));
		Logger.Log("RewardSystemBehavior", "[HeroJoinOriginalClan] restored_notable hero=" + (hero.StringId ?? "") + " settlement=" + (originalSettlement.StringId ?? "") + " occupation=" + targetOccupation);
	}

	private static void MakeDismissedHeroWanderer(Hero hero, Clan originalClan)
	{
		if (hero == null)
		{
			return;
		}
		if (hero.Clan != null)
		{
			hero.Clan = null;
		}
		if (hero.Occupation != Occupation.Wanderer)
		{
			hero.SetNewOccupation(Occupation.Wanderer);
		}
		PlaceDismissedFormerLord(hero, null);
		string heroName = hero.Name?.ToString() ?? "Hero";
		string clanName = GetClanDisplayNameForNotification(originalClan);
		string reasonText = originalClan != null ? (clanName + " 已灭亡，") : "原归属不可用，";
		InformationManager.DisplayMessage(new InformationMessage("【同伴离队】" + reasonText + heroName + " 已成为流浪者。", Color.FromUint(4294936661u)));
		Logger.Log("RewardSystemBehavior", "[HeroJoinOriginalClan] wanderer hero=" + (hero.StringId ?? "") + " oldClan=" + (originalClan?.StringId ?? ""));
	}

	private static void PlaceDismissedFormerLord(Hero hero, Clan clan)
	{
		if (hero == null || hero.IsDead)
		{
			return;
		}
		try
		{
			Settlement settlement = FindDismissedFormerLordSettlement(hero, clan);
			if (settlement != null)
			{
				TeleportHeroAction.ApplyImmediateTeleportToSettlement(hero, settlement);
			}
			else if (!hero.IsActive)
			{
				hero.ChangeState(Hero.CharacterStates.Active);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("RewardSystemBehavior", "[HeroJoinOriginalClan] placement failed hero=" + (hero.StringId ?? "") + " error=" + ex.Message);
		}
	}

	private static Settlement FindDismissedFormerLordSettlement(Hero hero, Clan clan)
	{
		try
		{
			Settlement settlement = FirstUsableSettlement(
				hero?.HomeSettlement,
				clan?.HomeSettlement,
				clan?.InitialHomeSettlement,
				Settlement.CurrentSettlement,
				Hero.MainHero?.CurrentSettlement,
				MobileParty.MainParty?.CurrentSettlement);
			if (settlement != null)
			{
				return settlement;
			}
			settlement = clan?.Settlements?.FirstOrDefault((Settlement x) => IsUsableDismissedHeroSettlement(x));
			if (settlement != null)
			{
				return settlement;
			}
			return Settlement.All?.FirstOrDefault((Settlement x) => IsUsableDismissedHeroSettlement(x) && x.IsTown)
				?? Settlement.All?.FirstOrDefault((Settlement x) => IsUsableDismissedHeroSettlement(x));
		}
		catch
		{
			return null;
		}
	}

	private static Settlement FirstUsableSettlement(params Settlement[] settlements)
	{
		if (settlements == null)
		{
			return null;
		}
		foreach (Settlement settlement in settlements)
		{
			if (IsUsableDismissedHeroSettlement(settlement))
			{
				return settlement;
			}
		}
		return null;
	}

	private static bool IsUsableDismissedHeroSettlement(Settlement settlement)
	{
		try
		{
			return settlement != null && !settlement.IsHideout && (settlement.IsTown || settlement.IsCastle || settlement.IsVillage);
		}
		catch
		{
			return false;
		}
	}

	private static float GetNowCampaignDay()
	{
		try
		{
			return (float)CampaignTime.Now.ToDays;
		}
		catch
		{
			return 0f;
		}
	}

	private static int ToDisplayDay(float day)
	{
		if (day <= 0f)
		{
			return 0;
		}
		return Math.Max(1, (int)Math.Ceiling(day));
	}

	private static string NormalizeHeroId(Hero hero)
	{
		return (hero?.StringId ?? "").Trim();
	}

	private static string BuildSettlementMerchantPendingTransferKey(Settlement settlement, SettlementMerchantKind kind)
	{
		string text = BuildSettlementMerchantFactKey(settlement, kind);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "merchant:" + text;
	}

	private static string BuildSettlementMerchantDebtKey(Settlement settlement, SettlementMerchantKind kind)
	{
		string text = BuildSettlementMerchantFactKey(settlement, kind);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "merchant_debt:" + text;
	}

	private static string BuildSettlementMerchantTrustKey(Settlement settlement, SettlementMerchantKind kind)
	{
		string text = BuildSettlementMerchantFactKey(settlement, kind);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "merchant_trust:" + text;
	}

	private static bool TryParseSettlementMerchantDebtKey(string debtKey, out string settlementId, out SettlementMerchantKind kind)
	{
		settlementId = "";
		kind = SettlementMerchantKind.None;
		string text = (debtKey ?? "").Trim();
		if (!text.StartsWith("merchant_debt:", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string[] array = text.Substring("merchant_debt:".Length).Split(new char[1] { ':' }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length != 2)
		{
			return false;
		}
		settlementId = array[0].Trim();
		return Enum.TryParse<SettlementMerchantKind>(array[1].Trim(), ignoreCase: true, out kind) && !string.IsNullOrWhiteSpace(settlementId) && kind != SettlementMerchantKind.None;
	}

	private static Settlement ResolveSettlementById(string settlementId)
	{
		try
		{
			string text = (settlementId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			foreach (Settlement item in Settlement.All)
			{
				if (item != null && string.Equals((item.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase))
				{
					return item;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static string BuildSettlementMerchantDebtLabel(Settlement settlement, SettlementMerchantKind kind)
	{
		string text = settlement?.Name?.ToString() ?? "这座城镇";
		return text + "的" + GetSettlementMerchantMarketLabel(kind);
	}

	private PendingPlayerTransfer GetOrCreatePendingPlayerTransferByKey(string transferKey)
	{
		string text = (transferKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		if (_pendingPlayerTransfers == null)
		{
			_pendingPlayerTransfers = new Dictionary<string, PendingPlayerTransfer>(StringComparer.OrdinalIgnoreCase);
		}
		if (!_pendingPlayerTransfers.TryGetValue(text, out var value) || value == null)
		{
			value = new PendingPlayerTransfer();
			_pendingPlayerTransfers[text] = value;
		}
		value.LastTouchedDay = GetCampaignDayIndex();
		if (value.Items == null)
		{
			value.Items = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		return value;
	}

	private PendingPlayerTransfer GetPendingPlayerTransferByKey(string transferKey)
	{
		string text = (transferKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || _pendingPlayerTransfers == null)
		{
			return null;
		}
		if (_pendingPlayerTransfers.TryGetValue(text, out var value) && value != null)
		{
			if (value.Items == null)
			{
				value.Items = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			}
			return value;
		}
		return null;
	}

	private PendingPlayerTransfer GetOrCreatePendingPlayerTransfer(Hero npc)
	{
		return GetOrCreatePendingPlayerTransferByKey(NormalizeHeroId(npc));
	}

	private PendingPlayerTransfer GetPendingPlayerTransfer(Hero npc)
	{
		return GetPendingPlayerTransferByKey(NormalizeHeroId(npc));
	}

	private void CleanupPendingPlayerTransfers(int currentDay)
	{
		if (_pendingPlayerTransfers == null || _pendingPlayerTransfers.Count <= 0)
		{
			return;
		}
		List<string> list = _pendingPlayerTransfers.Keys.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			string text = list[i];
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (!_pendingPlayerTransfers.TryGetValue(text, out var value) || value == null)
			{
				_pendingPlayerTransfers.Remove(text);
				continue;
			}
			int num = 0;
			try
			{
				if (value.Items != null)
				{
					foreach (KeyValuePair<string, int> item in value.Items)
					{
						if (item.Value > 0)
						{
							num += item.Value;
						}
					}
				}
			}
			catch
			{
				num = 0;
			}
			bool flag = value.Gold > 0 || num > 0;
			bool flag2 = currentDay - value.LastTouchedDay > 2;
			if (!flag || flag2)
			{
				_pendingPlayerTransfers.Remove(text);
			}
		}
	}

	private static string BuildSettlementMerchantFactKey(Settlement settlement, SettlementMerchantKind kind)
	{
		string text = (settlement?.StringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || kind == SettlementMerchantKind.None)
		{
			return "";
		}
		return text.ToLowerInvariant() + ":" + kind.ToString().ToLowerInvariant();
	}

	private static void CleanupMerchantFactRecord(MerchantFactRecord record)
	{
		if (record == null)
		{
			return;
		}
		if (record.Facts == null)
		{
			record.Facts = new List<string>();
			return;
		}
		List<string> list = record.Facts.Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList();
		if (list.Count > 8)
		{
			list = list.Skip(list.Count - 8).ToList();
		}
		record.Facts = list;
	}

	private MerchantFactRecord GetOrCreateMerchantFactRecord(Settlement settlement, SettlementMerchantKind kind)
	{
		string text = BuildSettlementMerchantFactKey(settlement, kind);
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		if (_merchantFacts == null)
		{
			_merchantFacts = new Dictionary<string, MerchantFactRecord>(StringComparer.OrdinalIgnoreCase);
		}
		if (!_merchantFacts.TryGetValue(text, out var value) || value == null)
		{
			value = new MerchantFactRecord();
			_merchantFacts[text] = value;
		}
		if (value.Facts == null)
		{
			value.Facts = new List<string>();
		}
		CleanupMerchantFactRecord(value);
		value.LastTouchedDay = GetCampaignDayIndex();
		return value;
	}

	private MerchantFactRecord GetMerchantFactRecord(Settlement settlement, SettlementMerchantKind kind)
	{
		string text = BuildSettlementMerchantFactKey(settlement, kind);
		if (string.IsNullOrWhiteSpace(text) || _merchantFacts == null)
		{
			return null;
		}
		if (!_merchantFacts.TryGetValue(text, out var value) || value == null)
		{
			return null;
		}
		if (value.Facts == null)
		{
			value.Facts = new List<string>();
		}
		CleanupMerchantFactRecord(value);
		return value;
	}

	public void AppendSettlementMerchantNpcFact(Settlement settlement, SettlementMerchantKind kind, string factText, string speakerName = null)
	{
		try
		{
			string text = (factText ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				MerchantFactRecord orCreateMerchantFactRecord = GetOrCreateMerchantFactRecord(settlement, kind);
				if (orCreateMerchantFactRecord != null)
				{
					string text2 = (speakerName ?? GetSettlementMerchantRoleLabel(kind) ?? "商贩").Trim();
					orCreateMerchantFactRecord.Facts.Add("[AFEF NPC行为补充] " + text2 + ": " + text);
					CleanupMerchantFactRecord(orCreateMerchantFactRecord);
				}
			}
		}
		catch
		{
		}
	}

	public string BuildSettlementMerchantNpcFactSummaryForAI(CharacterObject character, Settlement settlement = null, int maxLines = 4)
	{
		if (!TryGetSettlementMerchantKind(character, out var kind))
		{
			return "";
		}
		settlement = settlement ?? Settlement.CurrentSettlement;
		MerchantFactRecord merchantFactRecord = GetMerchantFactRecord(settlement, kind);
		if (merchantFactRecord?.Facts == null || merchantFactRecord.Facts.Count <= 0)
		{
			return "";
		}
		List<string> list = merchantFactRecord.Facts;
		int num = Math.Max(1, maxLines);
		if (list.Count > num)
		{
			list = list.Skip(list.Count - num).ToList();
		}
		return string.Join("\n", list);
	}

	public string BuildNpcBehaviorSupplementForAI(Hero hero, CharacterObject character = null, int maxLines = 4)
	{
		if (hero != null)
		{
			return MyBehavior.BuildRecentNpcFactContextForExternal(hero, maxLines);
		}
		if (character != null)
		{
			return BuildSettlementMerchantNpcFactSummaryForAI(character, null, maxLines);
		}
		return "";
	}

	private void SetLastGeneratedNpcFactLines(IEnumerable<string> lines)
	{
		_lastGeneratedNpcFactLines = ((lines != null) ? lines.Where((string x) => !string.IsNullOrWhiteSpace(x)).ToList() : new List<string>());
	}

	public List<string> ConsumeLastGeneratedNpcFactLines()
	{
		List<string> result = ((_lastGeneratedNpcFactLines != null) ? new List<string>(_lastGeneratedNpcFactLines) : new List<string>());
		_lastGeneratedNpcFactLines = new List<string>();
		return result;
	}

	public void RecordPlayerPrepaidTransfer(Hero npc, int goldAmount, string itemId, int itemAmount)
	{
		try
		{
			PendingPlayerTransfer orCreatePendingPlayerTransfer = GetOrCreatePendingPlayerTransfer(npc);
			if (orCreatePendingPlayerTransfer == null)
			{
				return;
			}
			if (goldAmount > 0)
			{
				orCreatePendingPlayerTransfer.Gold = Math.Max(0, orCreatePendingPlayerTransfer.Gold) + goldAmount;
			}
			if (!string.IsNullOrWhiteSpace(itemId) && itemAmount > 0)
			{
				string key = itemId.Trim();
				if (orCreatePendingPlayerTransfer.Items.TryGetValue(key, out var value))
				{
					orCreatePendingPlayerTransfer.Items[key] = value + itemAmount;
				}
				else
				{
					orCreatePendingPlayerTransfer.Items[key] = itemAmount;
				}
			}
		}
		catch
		{
		}
	}

	public void RecordPlayerPrepaidTransferForMerchant(Settlement settlement, SettlementMerchantKind kind, int goldAmount, string itemId, int itemAmount)
	{
		try
		{
			PendingPlayerTransfer orCreatePendingPlayerTransferByKey = GetOrCreatePendingPlayerTransferByKey(BuildSettlementMerchantPendingTransferKey(settlement, kind));
			if (orCreatePendingPlayerTransferByKey == null)
			{
				return;
			}
			if (goldAmount > 0)
			{
				orCreatePendingPlayerTransferByKey.Gold = Math.Max(0, orCreatePendingPlayerTransferByKey.Gold) + goldAmount;
			}
			if (!string.IsNullOrWhiteSpace(itemId) && itemAmount > 0)
			{
				string key = itemId.Trim();
				if (orCreatePendingPlayerTransferByKey.Items.TryGetValue(key, out var value))
				{
					orCreatePendingPlayerTransferByKey.Items[key] = value + itemAmount;
				}
				else
				{
					orCreatePendingPlayerTransferByKey.Items[key] = itemAmount;
				}
			}
		}
		catch
		{
		}
	}

	public int GetPlayerPrepaidGoldForExternal(Hero npc)
	{
		try
		{
			return Math.Max(0, GetPendingPlayerTransfer(npc)?.Gold ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	public int ConsumePlayerPrepaidGoldForExternal(Hero npc, int request)
	{
		try
		{
			return ConsumePlayerPrepaidGold(npc, request);
		}
		catch
		{
			return 0;
		}
	}

	private int ConsumePlayerPrepaidGold(Hero npc, int request)
	{
		if (request <= 0)
		{
			return 0;
		}
		PendingPlayerTransfer pendingPlayerTransfer = GetPendingPlayerTransfer(npc);
		if (pendingPlayerTransfer == null)
		{
			return 0;
		}
		int num = Math.Min(Math.Max(0, pendingPlayerTransfer.Gold), request);
		if (num <= 0)
		{
			return 0;
		}
		pendingPlayerTransfer.Gold = Math.Max(0, pendingPlayerTransfer.Gold - num);
		pendingPlayerTransfer.LastTouchedDay = GetCampaignDayIndex();
		return num;
	}

	private static bool HasDebtContent(DebtRecord rec)
	{
		if (rec == null)
		{
			return false;
		}
		if (rec.OwedGold > 0)
		{
			return true;
		}
		if (rec.OwedItems == null)
		{
			return false;
		}
		foreach (KeyValuePair<string, int> owedItem in rec.OwedItems)
		{
			if (owedItem.Value > 0)
			{
				return true;
			}
		}
		return false;
	}

	private static int NormalizeDueDays(int days)
	{
		if (days < 1)
		{
			return 1;
		}
		if (days > 120)
		{
			return 120;
		}
		return days;
	}

	private static float Clamp01(float v)
	{
		if (v < 0f)
		{
			return 0f;
		}
		if (v > 1f)
		{
			return 1f;
		}
		return v;
	}

	private static int ComputeWeeklyOverdueTrustPenaltyByDebtValue(int debtValue)
	{
		int num = Math.Max(0, debtValue);
		if (num <= 0)
		{
			return 0;
		}
		return Math.Max(1, num / OverdueTrustPenaltyPerWeekValueStep);
	}

	private static int ConsumeUnlimitedDebtTrustPenaltyUnits(DebtRecord.DebtLine line, int debtValue, int campaignDayIndex)
	{
		if (line == null || !line.IsDueUnlimited || line.RemainingAmount <= 0)
		{
			return 0;
		}
		if (line.LastOverduePenaltyDay <= 0)
		{
			line.LastOverduePenaltyDay = campaignDayIndex;
			return 0;
		}
		int elapsedDays = campaignDayIndex - line.LastOverduePenaltyDay;
		if (elapsedDays <= 0)
		{
			return 0;
		}
		line.LastOverduePenaltyDay = campaignDayIndex;
		int value = Math.Max(0, debtValue);
		if (value <= 0)
		{
			return 0;
		}
		decimal numerator = Math.Max(0L, line.UnlimitedTrustPenaltyNumeratorCarry)
			+ (decimal)value * UnlimitedDebtPenaltyTrustUnitsPerReferencePerDay * elapsedDays;
		decimal penaltyUnits = decimal.Floor(numerator / UnlimitedDebtPenaltyReferenceValue);
		line.UnlimitedTrustPenaltyNumeratorCarry = (long)(numerator % UnlimitedDebtPenaltyReferenceValue);
		if (penaltyUnits <= 0m)
		{
			return 0;
		}
		return penaltyUnits >= int.MaxValue ? int.MaxValue : (int)penaltyUnits;
	}

	private static bool ShouldIncludeDebtLineInScheduledReminder(DebtRecord.DebtLine line, int campaignDayIndex)
	{
		if (line == null || line.RemainingAmount <= 0)
		{
			return false;
		}
		if (!line.IsDueUnlimited)
		{
			return true;
		}
		int createdDay = Math.Max(0, (int)Math.Floor(line.CreatedDay));
		int elapsedDays = campaignDayIndex - createdDay;
		return elapsedDays >= UnlimitedDebtReminderIntervalDays && elapsedDays % UnlimitedDebtReminderIntervalDays == 0;
	}

	private static int ComputeWeeklyOverdueRelationPenaltyTotal(int weeksApplied, int trustPenaltyPerWeek)
	{
		if (weeksApplied <= 0 || trustPenaltyPerWeek <= 0)
		{
			return 0;
		}
		long num = (long)weeksApplied * (long)trustPenaltyPerWeek;
		long num2 = num / OverdueRelationPenaltyPerWeekTrustStep;
		if (num2 > int.MaxValue)
		{
			return int.MaxValue;
		}
		return Math.Max(0, (int)num2);
	}

	private static int ComputeWeeklyOverdueRelationPenaltyDelta(int previousWeeksApplied, int currentWeeksApplied, int trustPenaltyPerWeek)
	{
		int num = ComputeWeeklyOverdueRelationPenaltyTotal(Math.Max(0, previousWeeksApplied), trustPenaltyPerWeek);
		int num2 = ComputeWeeklyOverdueRelationPenaltyTotal(Math.Max(0, currentWeeksApplied), trustPenaltyPerWeek);
		return Math.Max(0, num2 - num);
	}

	private static int ComputeOverdueElapsedWeeks(float nowCampaignDay, float dueDay)
	{
		if (dueDay <= 0f || nowCampaignDay <= dueDay + 0.01f)
		{
			return 0;
		}
		int num = Math.Max(0, (int)Math.Floor(nowCampaignDay - dueDay));
		int num2 = num / OverduePenaltyIntervalDays;
		if (num2 < 0)
		{
			num2 = 0;
		}
		if (num2 > OverduePenaltyMaxWeeks)
		{
			num2 = OverduePenaltyMaxWeeks;
		}
		return num2;
	}

	private static int NormalizeLlmPenaltyValue(int value)
	{
		if (value < 0)
		{
			return 0;
		}
		if (value > 10)
		{
			return 10;
		}
		return value;
	}

	private static int NormalizeLlmTrustDeltaValue(int value)
	{
		if (value < -10)
		{
			return -10;
		}
		if (value > 10)
		{
			return 10;
		}
		return value;
	}

	private static int ClampPositiveLongToInt(long value)
	{
		if (value <= 0L)
		{
			return 0;
		}
		if (value > int.MaxValue)
		{
			return int.MaxValue;
		}
		return (int)value;
	}

	private static long ClampLong(long value, long min, long max)
	{
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	private static double GetTrustCurveNormalizedPosition(double currentTrust)
	{
		double num = Math.Abs(currentTrust) / (double)TrustMax;
		if (num < 0.0)
		{
			return 0.0;
		}
		if (num > 1.0)
		{
			return 1.0;
		}
		return num;
	}

	private static double GetTrustDeltaScaleByCurrentTrust(double currentTrust)
	{
		double trustCurveNormalizedPosition = GetTrustCurveNormalizedPosition(currentTrust);
		double num = 1.0 + TrustCurveMaxScaleOffset * Math.Pow(trustCurveNormalizedPosition, TrustCurveExponent);
		if (currentTrust < 0.0)
		{
			return num;
		}
		return 1.0 / num;
	}

	private double ConvertRawTrustDeltaToUnits(int rawDelta, double currentTrust)
	{
		if (rawDelta == 0)
		{
			return 0.0;
		}
		return (double)rawDelta * (double)TrustGainUnitsPerPoint * GetTrustDeltaScaleByCurrentTrust(currentTrust);
	}

	private int ApplyProgressiveTrustDeltaUnits(Dictionary<string, int> carryStore, string trustKey, int currentTrust, int rawDelta, out int appliedUnits)
	{
		appliedUnits = 0;
		string text = (trustKey ?? "").Trim();
		if (carryStore == null || string.IsNullOrWhiteSpace(text) || rawDelta == 0)
		{
			return 0;
		}
		carryStore.TryGetValue(text, out var value);
		double num = (double)((long)currentTrust * (long)TrustGainUnitsPerPoint + (long)value);
		long num2 = (long)num;
		long min = (long)TrustMin * (long)TrustGainUnitsPerPoint;
		long max = (long)TrustMax * (long)TrustGainUnitsPerPoint;
		int num3 = Math.Sign(rawDelta);
		int num4 = Math.Abs(rawDelta);
		for (int i = 0; i < num4; i++)
		{
			if (num <= (double)min || num >= (double)max)
			{
				break;
			}
			double currentTrust2 = num / (double)TrustGainUnitsPerPoint;
			double num5 = ConvertRawTrustDeltaToUnits(num3, currentTrust2);
			if (Math.Abs(num5) < 0.0001)
			{
				continue;
			}
			double num6 = Math.Max((double)min, Math.Min((double)max, num + num5));
			if (Math.Abs(num6 - num) < 0.0001)
			{
				break;
			}
			num = num6;
		}
		long num7 = (long)num;
		appliedUnits = (int)(num7 - num2);
		int num8 = (int)(num7 / TrustGainUnitsPerPoint);
		int num9 = (int)(num7 % TrustGainUnitsPerPoint);
		if (num9 != 0)
		{
			carryStore[text] = num9;
		}
		else
		{
			carryStore.Remove(text);
		}
		return num8 - currentTrust;
	}

	private static long GetTrustTotalUnitsWithCarry(int currentTrust, int carryUnits)
	{
		return (long)currentTrust * (long)TrustGainUnitsPerPoint + (long)carryUnits;
	}

	private int ApplyPositiveTrustSourceUnitsProgressively(long currentUnits, int sourceUnits, out long finalUnits)
	{
		finalUnits = currentUnits;
		if (sourceUnits <= 0)
		{
			return 0;
		}
		double num = (double)currentUnits;
		long max = (long)TrustMax * (long)TrustGainUnitsPerPoint;
		for (int i = 0; i < sourceUnits && num < (double)max; i++)
		{
			double currentTrust = num / (double)TrustGainUnitsPerPoint;
			num = Math.Min((double)max, num + GetTrustDeltaScaleByCurrentTrust(currentTrust));
		}
		finalUnits = (long)num;
		return (int)(finalUnits - currentUnits);
	}

	private int ApplyDirectTrustDeltaUnits(string trustKey, int currentTrust, int rawDelta, out int appliedUnits)
	{
		appliedUnits = 0;
		string text = (trustKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || rawDelta == 0)
		{
			return 0;
		}
		if (_directTrustProgressCarry == null)
		{
			_directTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		return ApplyProgressiveTrustDeltaUnits(_directTrustProgressCarry, text, currentTrust, rawDelta, out appliedUnits);
	}

	private int ApplyExactDirectTrustDeltaUnits(string trustKey, int currentTrust, int requestedUnits, out int appliedUnits)
	{
		appliedUnits = 0;
		string text = (trustKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || requestedUnits == 0)
		{
			return 0;
		}
		if (_directTrustProgressCarry == null)
		{
			_directTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		_directTrustProgressCarry.TryGetValue(text, out var carryUnits);
		long currentUnits = GetTrustTotalUnitsWithCarry(currentTrust, carryUnits);
		long minUnits = (long)TrustMin * TrustGainUnitsPerPoint;
		long maxUnits = (long)TrustMax * TrustGainUnitsPerPoint;
		long finalUnits = Math.Max(minUnits, Math.Min(maxUnits, currentUnits + requestedUnits));
		appliedUnits = (int)(finalUnits - currentUnits);
		int finalTrust = (int)(finalUnits / TrustGainUnitsPerPoint);
		int finalCarry = (int)(finalUnits % TrustGainUnitsPerPoint);
		if (finalCarry == 0)
		{
			_directTrustProgressCarry.Remove(text);
		}
		else
		{
			_directTrustProgressCarry[text] = finalCarry;
		}
		return finalTrust - currentTrust;
	}

	private int ApplySettlementTrustUnits(Settlement settlement, int rawDelta, out int appliedUnits)
	{
		appliedUnits = 0;
		if (settlement == null || rawDelta == 0)
		{
			return 0;
		}
		string text = BuildSettlementTrustCarryKey(settlement);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		if (_settlementTrustCentiCarry == null)
		{
			_settlementTrustCentiCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		int settlementLocalPublicTrust = GetSettlementLocalPublicTrust(settlement);
		return ApplyProgressiveTrustDeltaUnits(_settlementTrustCentiCarry, text, settlementLocalPublicTrust, rawDelta, out appliedUnits);
	}

	private void ApplySettlementLocalTrustWholeDeltaDirect(Settlement settlement, int localTrustDelta, string reason)
	{
		if (settlement == null || localTrustDelta == 0)
		{
			return;
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		string settlementPublicTrustKey = BuildSettlementLocalPublicTrustKey(settlement);
		if (string.IsNullOrWhiteSpace(settlementPublicTrustKey))
		{
			return;
		}
		int settlementLocalPublicTrust = GetSettlementLocalPublicTrust(settlement);
		int num = ClampTrust(settlementLocalPublicTrust + localTrustDelta);
		if (num == 0)
		{
			_publicTrust.Remove(settlementPublicTrustKey);
		}
		else
		{
			_publicTrust[settlementPublicTrustKey] = num;
		}
		Logger.Log("Trust", $"settlement={settlement.StringId} reason={reason} settlementTrust={settlementLocalPublicTrust}->{num} delta={localTrustDelta}");
	}

	private static string FormatTrustUnits(int units)
	{
		decimal d = (decimal)units / (decimal)TrustGainUnitsPerPoint;
		string text = d.ToString("0.######");
		if (text == "-0")
		{
			return "0";
		}
		return text;
	}

	private int AccumulateTradeTrustValueByKey(string carryKey, int addedValue)
	{
		string text = (carryKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || addedValue <= 0)
		{
			return 0;
		}
		if (_tradeTrustValueCarry == null)
		{
			_tradeTrustValueCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		_tradeTrustValueCarry.TryGetValue(text, out var value);
		long num = Math.Max(0, value);
		long num2 = num + (long)addedValue;
		int num3 = ClampPositiveLongToInt(num2 / AutoTrustValuePerPoint);
		int num4 = (int)(num2 % AutoTrustValuePerPoint);
		if (num4 > 0)
		{
			_tradeTrustValueCarry[text] = num4;
		}
		else
		{
			_tradeTrustValueCarry.Remove(text);
		}
		return num3;
	}

	private int GetItemTrustValueForHeroGift(Hero hero, ItemObject item, int amount)
	{
		return ClampPositiveLongToInt(GetItemGuideValueForHeroGift(hero, item, amount));
	}

	private long GetItemGuideValueForHeroGift(Hero hero, ItemObject item, int amount)
	{
		if (item == null || amount <= 0)
		{
			return 0L;
		}
		ItemGuidePriceInfo guidePriceForItemNearHero = GetGuidePriceForItemNearHero(hero ?? Hero.MainHero, item);
		return TransferQuantitySpec.AddProduct(0L, amount, Math.Max(1, guidePriceForItemNearHero.UnitPrice));
	}

	private int GetItemTrustValueForMerchantGift(Settlement settlement, ItemObject item, int amount)
	{
		return ClampPositiveLongToInt(GetItemGuideValueForMerchantGift(settlement, item, amount));
	}

	private long GetItemGuideValueForMerchantGift(Settlement settlement, ItemObject item, int amount)
	{
		if (item == null || amount <= 0)
		{
			return 0L;
		}
		if (settlement != null && TryGetSettlementBuyPrice(settlement, item, out var price) && price > 0)
		{
			return TransferQuantitySpec.AddProduct(0L, amount, price);
		}
		return GetItemGuideValueForHeroGift(Hero.MainHero, item, amount);
	}

	private static bool IsPrisonerTrustGainBlocked(Hero npc)
	{
		return npc != null && npc.IsPrisoner;
	}

	private void ApplyAutoTrustGainFromHeroGiftValue(Hero giver, int addedValue, List<string> giverFacts, List<string> receiverFacts, string giverName)
	{
		if (giver == null || addedValue <= 0)
		{
			return;
		}
		if (IsPrisonerTrustGainBlocked(giver))
		{
			Logger.Log("Trust", $"npc={giver.StringId} reason=auto_gift_value_accumulated blocked=prisoner addedValue={addedValue}");
			return;
		}
		int num = AccumulateTradeTrustValueByKey(NormalizeHeroId(giver), addedValue);
		if (num <= 0)
		{
			return;
		}
		int num2 = AdjustTrust(giver, num, 0, "auto_gift_value_accumulated", out var appliedUnits);
		string text = (num2 > 0) ? $"，公共信任提升 {num2}" : "";
		string text2 = FormatTrustUnits(appliedUnits);
		giverFacts?.Add($"你因累计向玩家实际交付的价值达到阈值，对玩家的个人信任提升了 {text2}{text}。");
		receiverFacts?.Add($"{giverName} 因累计向你实际交付的价值达到阈值，对你的个人信任提升了 {text2}{text}。");
		string message = $"【信任变化】{giverName} 因累计向你实际交付的价值，对你的个人信任 +{text2}" + ((num2 > 0) ? $"，公共信任 +{num2}" : "");
		InformationManager.DisplayMessage(new InformationMessage(message, Color.FromUint(4278242559u)));
		ShowRewardQuickInfo(message, giver);
	}

	private void ApplyAutoTrustGainFromMerchantGiftValue(Settlement settlement, SettlementMerchantKind kind, int addedValue, List<string> merchantFacts, List<string> playerFacts, string giverName, BasicCharacterObject giverCharacter = null)
	{
		if (settlement == null || kind == SettlementMerchantKind.None || addedValue <= 0)
		{
			return;
		}
		int num = AccumulateTradeTrustValueByKey(BuildSettlementMerchantTrustKey(settlement, kind), addedValue);
		if (num <= 0)
		{
			return;
		}
		int num2 = AdjustSettlementMerchantTrust(settlement, kind, num, "merchant_auto_gift_value_accumulated", out var appliedUnits);
		string settlementMerchantDebtLabel = BuildSettlementMerchantDebtLabel(settlement, kind);
		string text = (num2 > 0) ? $"，公共信任提升 {num2}" : "";
		string text2 = FormatTrustUnits(appliedUnits);
		merchantFacts?.Add($"你因累计向玩家实际交付的价值达到阈值，对玩家的市场信任提升了 {text2}{text}。");
		playerFacts?.Add($"{giverName} 代表的{settlementMerchantDebtLabel}因累计向你实际交付的价值达到阈值，对你的市场信任提升了 {text2}{text}。");
		string message = $"【市场信任变化】{settlementMerchantDebtLabel} 对你的市场信任 +{text2}" + ((num2 > 0) ? $"，公共信任 +{num2}" : "");
		ShowRewardMessage(message, Color.FromUint(4278242559u), giverCharacter);
	}

	private static int TruncateDivisionTowardsZero(int dividend, int divisor, out int remainder)
	{
		remainder = 0;
		if (divisor == 0)
		{
			return 0;
		}
		int num = dividend / divisor;
		remainder = dividend % divisor;
		return num;
	}

	private int AccumulatePublicTrustProgressByKey(string publicTrustKey, int sourceUnits)
	{
		string text = (publicTrustKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || sourceUnits == 0)
		{
			return 0;
		}
		if (_publicTrustProgressCarry == null)
		{
			_publicTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		_publicTrustProgressCarry.TryGetValue(text, out var value);
		int num = value + sourceUnits;
		int num2 = TruncateDivisionTowardsZero(num, PublicTrustPoolPointsPerTrust * TrustGainUnitsPerPoint, out var remainder);
		if (remainder != 0)
		{
			_publicTrustProgressCarry[text] = remainder;
		}
		else
		{
			_publicTrustProgressCarry.Remove(text);
		}
		return num2;
	}

	private int AdjustPublicTrustByKey(string publicTrustKey, int publicDelta, string reason)
	{
		string text = (publicTrustKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || publicDelta == 0)
		{
			return 0;
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		int num = 0;
		_publicTrust.TryGetValue(text, out num);
		num = ClampTrust(num);
		int num2 = ClampTrust(num + publicDelta);
		if (num2 == 0)
		{
			_publicTrust.Remove(text);
		}
		else
		{
			_publicTrust[text] = num2;
		}
		Logger.Log("Trust", $"publicKey={text} reason={reason} publicTrust={num}->{num2} delta={publicDelta}");
		return num2 - num;
	}

	private int ApplyPublicTrustPoolDeltaByKey(string publicTrustKey, int sourceUnits, string reason)
	{
		int num = AccumulatePublicTrustProgressByKey(publicTrustKey, sourceUnits);
		if (num == 0)
		{
			return 0;
		}
		return AdjustPublicTrustByKey(publicTrustKey, num, reason);
	}

	private static string BuildSettlementTrustCarryKey(Settlement settlement)
	{
		return BuildSettlementLocalPublicTrustKey(settlement);
	}

	private int AccumulateSettlementTrustCenti(Settlement settlement, int centiDelta)
	{
		string text = BuildSettlementTrustCarryKey(settlement);
		if (string.IsNullOrWhiteSpace(text) || centiDelta == 0)
		{
			return 0;
		}
		if (_settlementTrustCentiCarry == null)
		{
			_settlementTrustCentiCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		_settlementTrustCentiCarry.TryGetValue(text, out var value);
		int num = value + centiDelta;
		int num2 = TruncateDivisionTowardsZero(num, TrustGainUnitsPerPoint, out var remainder);
		if (remainder != 0)
		{
			_settlementTrustCentiCarry[text] = remainder;
		}
		else
		{
			_settlementTrustCentiCarry.Remove(text);
		}
		return num2;
	}

	private static int ComputeSettlementTrustCentiForTroop(CharacterObject troop, int count)
	{
		if (troop == null || count <= 0)
		{
			return 0;
		}
		int num = Math.Max(1, troop.Tier);
		return SettlementTrustUnitsPerTier * num * count;
	}

	private void OnPlayerPartyKnockedOrKilledTroop(CharacterObject strikedTroop)
	{
		try
		{
			MapEvent playerMapEvent = MapEvent.PlayerMapEvent;
			if (playerMapEvent == null || !playerMapEvent.IsPlayerMapEvent)
			{
				return;
			}
			_currentBattlePlayerActualSettlementTrustUnits += ComputeSettlementTrustCentiForTroop(strikedTroop, 1);
		}
		catch
		{
		}
	}

	private void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails details)
	{
		try
		{
			// Debt promises already apply their own repayment/penalty rules and must not receive the generic quest trust reward.
			if (quest is DebtPromiseQuest)
			{
				return;
			}
			if (quest == null || details != QuestBase.QuestCompleteDetails.Success)
			{
				return;
			}
			Hero hero = null;
			try
			{
				hero = quest.QuestGiver;
			}
			catch
			{
				hero = null;
			}
			if (hero == null)
			{
				return;
			}
			if (IsPrisonerTrustGainBlocked(hero))
			{
				Logger.Log("Trust", $"quest={quest.StringId} giver={hero.StringId} completed=success trustGainBlocked=prisoner");
				return;
			}
			int num = AdjustTrust(hero, TrustGainOnQuestSuccess, 0, "quest_completed_success", out var appliedUnits);
			string text = FormatTrustUnits(appliedUnits);
			string text2 = hero.Name?.ToString() ?? "任务发布人";
			Logger.Log("Trust", $"quest={quest.StringId} giver={hero.StringId} completed=success personalGain={text} publicGain={num}");
			string message = $"【信任变化】完成{text2}交付的任务，个人信任 +{text}" + ((num > 0) ? $"，公共信任 +{num}" : "");
			InformationManager.DisplayMessage(new InformationMessage(message, Color.FromUint(4278242559u)));
			ShowRewardQuickInfo(message, hero);
		}
		catch (Exception ex)
		{
			Logger.Log("Trust", "[ERROR] quest completion trust reward failed: " + ex);
		}
	}

	private static bool IsPartyHostileForSettlementTrust(PartyBase party, Settlement settlement)
	{
		if (party == null || settlement == null)
		{
			return false;
		}
		try
		{
			if (party.MobileParty != null && party.MobileParty.IsBandit)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			IFaction mapFaction = party.MapFaction;
			IFaction mapFaction2 = settlement.MapFaction;
			return mapFaction != null && mapFaction2 != null && mapFaction.IsAtWarWith(mapFaction2);
		}
		catch
		{
			return false;
		}
	}

	private static IEnumerable<Settlement> GetNearbySettlementsForTrust(MapEvent mapEvent)
	{
		if (mapEvent == null)
		{
			return Enumerable.Empty<Settlement>();
		}
		float num = SettlementTrustBattleEffectRadius;
		float num2 = num * num;
		return Settlement.All.Where((Settlement x) => x != null && !x.IsHideout && x.Position.DistanceSquared(mapEvent.Position) < num2).ToList();
	}

	private static int ComputeSettlementTrustCentiFromRoster(TroopRoster roster)
	{
		if (roster == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < roster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = roster.GetElementCopyAtIndex(i);
			num += ComputeSettlementTrustCentiForTroop(elementCopyAtIndex.Character, elementCopyAtIndex.Number);
		}
		return num;
	}

	private static int ComputeSettlementTrustCentiFromBattleRosters(MapEventParty party, bool includeSurrenderedActiveTroops)
	{
		if (party == null)
		{
			return 0;
		}
		int num = ComputeSettlementTrustCentiFromRoster(party.DiedInBattle) + ComputeSettlementTrustCentiFromRoster(party.WoundedInBattle);
		if (!includeSurrenderedActiveTroops || party.Troops == null)
		{
			return num;
		}
		foreach (FlattenedTroopRosterElement troop in party.Troops)
		{
			if (troop.Troop != null && troop.State == RosterTroopState.Active)
			{
				num += ComputeSettlementTrustCentiForTroop(troop.Troop, 1);
			}
		}
		return num;
	}

	private int ComputeSettlementTrustCentiFromDefeatedHostileTroops(MapEvent mapEvent, Settlement settlement)
	{
		if (mapEvent == null || settlement == null || !mapEvent.HasWinner)
		{
			return 0;
		}
		MapEventSide mapEventSide = mapEvent.GetMapEventSide(mapEvent.DefeatedSide);
		if (mapEventSide?.Parties == null)
		{
			return 0;
		}
		bool flag = IsMapEventSideSurrendered(mapEventSide);
		int num = 0;
		foreach (MapEventParty party in mapEventSide.Parties)
		{
			if (party?.Party == null || !IsPartyHostileForSettlementTrust(party.Party, settlement))
			{
				continue;
			}
			num += ComputeSettlementTrustCentiFromBattleRosters(party, flag);
		}
		return num;
	}

	private static bool IsMapEventSideSurrendered(MapEventSide side)
	{
		if (side == null)
		{
			return false;
		}
		try
		{
			var field = typeof(MapEventSide).GetField("IsSurrendered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			if (field != null && field.FieldType == typeof(bool))
			{
				return (bool)field.GetValue(side);
			}
		}
		catch
		{
		}
		return false;
	}

	private int ComputePlayerContributionSharePercentForWinningSide(MapEvent mapEvent)
	{
		if (mapEvent == null || !mapEvent.HasWinner)
		{
			return 0;
		}
		MapEventSide mapEventSide = mapEvent.GetMapEventSide(mapEvent.WinningSide);
		if (mapEventSide?.Parties == null)
		{
			return 0;
		}
		int num = 0;
		int num2 = 0;
		foreach (MapEventParty party in mapEventSide.Parties)
		{
			if (party == null)
			{
				continue;
			}
			int num3 = Math.Max(0, party.ContributionToBattle);
			num += num3;
			if (party.Party == PartyBase.MainParty)
			{
				num2 = num3;
			}
		}
		if (num <= 0 || num2 <= 0)
		{
			return 0;
		}
		return Math.Max(0, Math.Min(100, (int)Math.Round((double)(num2 * 100) / (double)num, MidpointRounding.AwayFromZero)));
	}

	private int AdjustSettlementLocalTrustInternal(Settlement settlement, int localTrustDelta, string reason)
	{
		if (settlement == null || localTrustDelta == 0)
		{
			return 0;
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		string settlementPublicTrustKey = BuildSettlementLocalPublicTrustKey(settlement);
		if (string.IsNullOrWhiteSpace(settlementPublicTrustKey))
		{
			return 0;
		}
		int settlementPublicTrust = GetSettlementLocalPublicTrust(settlement);
		int appliedUnits;
		int num2 = ApplySettlementTrustUnits(settlement, localTrustDelta, out appliedUnits);
		int num = ClampTrust(settlementPublicTrust + num2);
		ApplySettlementLocalTrustWholeDeltaDirect(settlement, num2, reason);
		int num3 = ApplyPublicTrustPoolDeltaByKey(BuildSettlementSharedPublicTrustKey(settlement), appliedUnits, (reason ?? "external") + "_local_public_pool");
		Logger.Log("Trust", $"settlement={settlement.StringId} reason={reason} settlementTrust={settlementPublicTrust}->{num} rawDelta={localTrustDelta} appliedDelta={FormatTrustUnits(appliedUnits)} publicDelta={num3}");
		return num3;
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		try
		{
			int currentBattlePlayerActualSettlementTrustCenti = _currentBattlePlayerActualSettlementTrustUnits;
			_currentBattlePlayerActualSettlementTrustUnits = 0;
			if (mapEvent == null || !mapEvent.IsPlayerMapEvent || !mapEvent.HasWinner)
			{
				return;
			}
			if (mapEvent.WinningSide != mapEvent.PlayerSide)
			{
				InformationManager.DisplayMessage(new InformationMessage("【定居点信任结算】本次战斗未获胜，未获得定居点信任。", Color.FromUint(4294945365u)));
				return;
			}
			List<Settlement> nearbySettlements = GetNearbySettlementsForTrust(mapEvent).ToList();
			if (nearbySettlements.Count <= 0)
			{
				InformationManager.DisplayMessage(new InformationMessage("【定居点信任结算】本次战斗附近没有可受影响的定居点。", Color.FromUint(4291611750u)));
				return;
			}
			List<string> list = new List<string>();
			int num = ComputePlayerContributionSharePercentForWinningSide(mapEvent);
			foreach (Settlement item in nearbySettlements)
			{
				int num2 = ComputeSettlementTrustCentiFromDefeatedHostileTroops(mapEvent, item);
				int num3 = Math.Max(0, currentBattlePlayerActualSettlementTrustCenti);
				int num4 = 0;
				if (num3 <= 0 && num > 0 && num2 > 0)
				{
					// Fallback for battles where the engine does not emit per-kill events reliably.
					num4 = (int)Math.Round((double)(num2 * num) / 100.0, MidpointRounding.AwayFromZero);
				}
				int num5 = Math.Max(num3, num4);
				int num6 = Math.Max(0, num2 - num5);
				int num7 = 0;
				if (num > 0 && num6 > 0)
				{
					num7 = (int)Math.Round((double)(num6 * num * SettlementTrustContributionSharePercent) / 10000.0, MidpointRounding.AwayFromZero);
				}
				string settlementTrustCarryKey = BuildSettlementTrustCarryKey(item);
				int num8 = 0;
				if (!string.IsNullOrWhiteSpace(settlementTrustCarryKey) && _settlementTrustCentiCarry != null)
				{
					_settlementTrustCentiCarry.TryGetValue(settlementTrustCarryKey, out num8);
				}
				long trustTotalUnitsWithCarry = GetTrustTotalUnitsWithCarry(GetSettlementLocalPublicTrust(item), num8);
				num5 = ApplyPositiveTrustSourceUnitsProgressively(trustTotalUnitsWithCarry, num5, out trustTotalUnitsWithCarry);
				num7 = ApplyPositiveTrustSourceUnitsProgressively(trustTotalUnitsWithCarry, num7, out trustTotalUnitsWithCarry);
				int num9 = num5 + num7;
				if (num9 <= 0)
				{
					continue;
				}
				int num10 = AccumulateSettlementTrustCenti(item, num9);
				int num11 = ApplyPublicTrustPoolDeltaByKey(BuildSettlementSharedPublicTrustKey(item), num9, "battle_hostile_party_defeated_local_public_pool");
				string text = (num4 > 0) ? "估算实击" : "实击";
				if (num10 != 0)
				{
					ApplySettlementLocalTrustWholeDeltaDirect(item, num10, "battle_hostile_party_defeated");
					list.Add($"{item.Name}: 定居点信任 +{num10}" + ((num11 > 0) ? $"，公共信任 +{num11}" : "") + $"\n{text} {FormatTrustUnits(num5)}，分成 {FormatTrustUnits(num7)}，本次累计 {FormatTrustUnits(num9)}");
				}
				else
				{
					list.Add($"{item.Name}: 定居点信任累计 +{FormatTrustUnits(num9)}" + ((num11 > 0) ? $"，公共信任 +{num11}" : "") + $"\n{text} {FormatTrustUnits(num5)}，分成 {FormatTrustUnits(num7)}，未满1点");
				}
			}
			if (list.Count > 0)
			{
				InformationManager.DisplayMessage(new InformationMessage("【定居点信任结算】\n" + string.Join("\n\n", list), Color.FromUint(4278242559u)));
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage("【定居点信任结算】本次战斗未击败附近定居点的敌对部队，因此没有获得定居点信任。", Color.FromUint(4291611750u)));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Trust", "[ERROR] OnMapEventEnded settlement trust failed: " + ex);
		}
	}

	private static int GetCampaignDayIndex()
	{
		try
		{
			return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
		}
		catch
		{
			return 0;
		}
	}

	private static int GetDaysInSeasonSafe()
	{
		try
		{
			int daysInSeason = CampaignTime.DaysInSeason;
			if (daysInSeason > 0)
			{
				return daysInSeason;
			}
		}
		catch
		{
		}
		return 21;
	}

	private static int GetDaysInYearSafe()
	{
		try
		{
			int daysInYear = CampaignTime.DaysInYear;
			if (daysInYear > 0)
			{
				return daysInYear;
			}
		}
		catch
		{
		}
		return GetDaysInSeasonSafe() * 4;
	}

	private static int NormalizeSeasonIndex(int seasonIndex)
	{
		int num = seasonIndex % 4;
		if (num < 0)
		{
			num += 4;
		}
		return num;
	}

	private static string GetSeasonTextZh(int seasonIndexZeroBased)
	{
		int num = NormalizeSeasonIndex(seasonIndexZeroBased);
		if (1 == 0)
		{
		}
		string result = num switch
		{
			0 => "春",
			1 => "夏",
			2 => "秋",
			_ => "冬",
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static string FormatAbsDayAsGameDate(int absDay)
	{
		int daysInSeasonSafe = GetDaysInSeasonSafe();
		int daysInYearSafe = GetDaysInYearSafe();
		int num = Math.Max(0, absDay);
		int num2 = num / daysInYearSafe;
		int num3 = num % daysInYearSafe;
		int seasonIndexZeroBased = num3 / daysInSeasonSafe;
		int num4 = num3 % daysInSeasonSafe + 1;
		return $"{num2}年{GetSeasonTextZh(seasonIndexZeroBased)}第{num4}天";
	}

	public string BuildDueDateReferenceForAI()
	{
		try
		{
			int daysInSeasonSafe = GetDaysInSeasonSafe();
			int daysInYearSafe = GetDaysInYearSafe();
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine($"换算：1季={daysInSeasonSafe}天，1年={daysInYearSafe}天。");
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return string.Empty;
		}
	}

	private static string BuildDebtId()
	{
		try
		{
			return "D" + Guid.NewGuid().ToString("N").Substring(0, 8)
				.ToUpperInvariant();
		}
		catch
		{
			return "D" + DateTime.UtcNow.Ticks;
		}
	}

	private void QueueDebtPromiseQuest(string ownerKey, string debtId)
	{
		string text = (ownerKey ?? "").Trim();
		string text2 = (debtId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return;
		}
		if (_pendingDebtPromiseQuestKeys == null)
		{
			_pendingDebtPromiseQuestKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}
		// The separator cannot occur in generated IDs and avoids allocating a request object for each promise.
		_pendingDebtPromiseQuestKeys.Add(text + "\u001f" + text2);
	}

	private void QueueDebtPromiseQuestsForActiveDebts()
	{
		if (_debts == null || _debts.Count == 0)
		{
			return;
		}
		// This migration/reconciliation is called only after load or import, never from the daily debt-maintenance loop.
		foreach (KeyValuePair<string, DebtRecord> debt in _debts)
		{
			if (string.IsNullOrWhiteSpace(debt.Key) || debt.Value == null)
			{
				continue;
			}
			NormalizeDebtRecord(debt.Value);
			if (debt.Value.DebtLines == null)
			{
				continue;
			}
			for (int i = 0; i < debt.Value.DebtLines.Count; i++)
			{
				DebtRecord.DebtLine debtLine = debt.Value.DebtLines[i];
				if (debtLine != null && debtLine.RemainingAmount > 0)
				{
					QueueDebtPromiseQuest(debt.Key, debtLine.DebtId);
				}
			}
		}
	}

	private void DrainPendingDebtPromiseQuestCreations()
	{
		if (_pendingDebtPromiseQuestKeys == null || _pendingDebtPromiseQuestKeys.Count == 0 || !CanStartDebtPromiseQuest())
		{
			return;
		}
		// Copy then clear so a task created by this pass can safely enqueue a later promise without being lost.
		List<string> list = _pendingDebtPromiseQuestKeys.ToList();
		_pendingDebtPromiseQuestKeys.Clear();
		for (int i = 0; i < list.Count; i++)
		{
			if (!TryParseDebtPromiseQuestKey(list[i], out var ownerKey, out var debtId)
				|| !TryGetActiveDebtPromiseQuestData(ownerKey, debtId, out var debtorName, out var debtSummary, out var deadlineText, out var debtNote, out var dueDay, out var isDueUnlimited))
			{
				// A same-conversation ADP can clear the debt before this deferred task creation runs.
				continue;
			}
			EnsureDebtPromiseQuest(ownerKey, debtId, debtorName, debtSummary, deadlineText, debtNote, dueDay, isDueUnlimited);
		}
	}

	private static bool CanStartDebtPromiseQuest()
	{
		try
		{
			return Campaign.Current != null && Campaign.Current.QuestManager != null && (Campaign.Current.ConversationManager == null || !Campaign.Current.ConversationManager.IsConversationInProgress);
		}
		catch
		{
			return false;
		}
	}

	private static bool TryParseDebtPromiseQuestKey(string value, out string ownerKey, out string debtId)
	{
		ownerKey = "";
		debtId = "";
		string text = value ?? "";
		int num = text.IndexOf('\u001f');
		if (num <= 0 || num >= text.Length - 1)
		{
			return false;
		}
		ownerKey = text.Substring(0, num).Trim();
		debtId = text.Substring(num + 1).Trim();
		return !string.IsNullOrWhiteSpace(ownerKey) && !string.IsNullOrWhiteSpace(debtId);
	}

	private bool TryGetActiveDebtPromiseQuestData(string ownerKey, string debtId, out string debtorName, out string debtSummary, out string deadlineText, out string debtNote, out float dueDay, out bool isDueUnlimited)
	{
		debtorName = "";
		debtSummary = "";
		deadlineText = "";
		debtNote = "";
		dueDay = 0f;
		isDueUnlimited = false;
		DebtRecord debtRecord = GetDebtRecordByKey(ownerKey);
		if (debtRecord == null)
		{
			return false;
		}
		NormalizeDebtRecord(debtRecord);
		DebtRecord.DebtLine debtLine = debtRecord.DebtLines?.FirstOrDefault((DebtRecord.DebtLine x) => x != null && x.RemainingAmount > 0 && string.Equals(x.DebtId ?? "", debtId, StringComparison.OrdinalIgnoreCase));
		if (debtLine == null)
		{
			return false;
		}
		Hero hero = null;
		try
		{
			hero = Hero.Find(ownerKey);
		}
		catch
		{
			hero = null;
		}
		if (hero != null)
		{
			debtorName = hero.Name?.ToString() ?? ownerKey;
		}
		else if (TryParseSettlementMerchantDebtKey(ownerKey, out var settlementId, out var kind))
		{
			Settlement settlement = ResolveSettlementById(settlementId);
			debtorName = BuildSettlementMerchantDebtLabel(settlement, kind);
		}
		else
		{
			debtorName = ownerKey;
		}
		debtSummary = BuildDebtPromiseSummary(debtLine);
		deadlineText = BuildDebtPromiseDeadlineText(debtLine.DueDay, debtLine.IsDueUnlimited);
		debtNote = string.IsNullOrWhiteSpace(debtLine.DebtNote) ? "无" : debtLine.DebtNote;
		// Pass raw deadline state so the task can use the native countdown instead of parsing display text.
		dueDay = debtLine.DueDay;
		isDueUnlimited = debtLine.IsDueUnlimited;
		return true;
	}

	private static string BuildDebtPromiseSummary(DebtRecord.DebtLine debtLine)
	{
		if (debtLine == null)
		{
			return "未说明";
		}
		int num = Math.Max(0, debtLine.RemainingAmount);
		if (debtLine.IsGold)
		{
			return num + " 第纳尔";
		}
		ItemObject itemObject = ResolveItemById(debtLine.ItemId);
		string text = itemObject?.Name?.ToString();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = string.IsNullOrWhiteSpace(debtLine.ItemId) ? "物品" : debtLine.ItemId;
		}
		return text + " ×" + num;
	}

	private void EnsureDebtPromiseQuest(string ownerKey, string debtId, string debtorName, string debtSummary, string deadlineText, string debtNote, float dueDay, bool isDueUnlimited)
	{
		try
		{
			foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
			{
				DebtPromiseQuest debtPromiseQuest = quest as DebtPromiseQuest;
				if (debtPromiseQuest != null && debtPromiseQuest.IsOngoing && debtPromiseQuest.Matches(ownerKey, debtId))
				{
					// Existing saves receive the exact ledger deadline during one-time load reconciliation.
					debtPromiseQuest.SynchronizeDeadline(dueDay, isDueUnlimited);
					return;
				}
			}
			// The task deliberately has no QuestGiver so it cannot reserve a hero's vanilla issue slot or force a map marker.
			DebtPromiseQuest debtPromiseQuest2 = new DebtPromiseQuest(debtId, ownerKey, debtorName, debtSummary, deadlineText, debtNote, dueDay, isDueUnlimited);
			debtPromiseQuest2.StartQuest();
			Logger.Log("Trust", "[DebtPromiseQuest] created debtId=" + debtId + " owner=" + ownerKey);
		}
		catch (Exception ex)
		{
			Logger.Log("Trust", "[WARN] Debt promise quest creation failed debtId=" + debtId + " owner=" + ownerKey + " error=" + ex.Message);
		}
	}

	private void CompleteDebtPromiseQuest(string ownerKey, string debtId)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(ownerKey) || string.IsNullOrWhiteSpace(debtId) || Campaign.Current?.QuestManager == null)
			{
				return;
			}
			List<DebtPromiseQuest> list = new List<DebtPromiseQuest>();
			foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
			{
				DebtPromiseQuest debtPromiseQuest = quest as DebtPromiseQuest;
				if (debtPromiseQuest != null && debtPromiseQuest.IsOngoing && debtPromiseQuest.Matches(ownerKey, debtId))
				{
					list.Add(debtPromiseQuest);
				}
			}
			// Complete every duplicate defensively; only one is normally created per debt ID.
			for (int i = 0; i < list.Count; i++)
			{
				list[i].CompleteByAgreement();
			}
		}
		catch (Exception ex)
		{
			// Quest UI/save failures must never roll back a debt release that was already applied to the ledger.
			Logger.Log("Trust", "[WARN] Debt promise quest completion failed debtId=" + debtId + " owner=" + ownerKey + " error=" + ex.Message);
		}
	}

	private void SyncMerchantFactData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		if (_merchantFacts == null)
		{
			_merchantFacts = new Dictionary<string, MerchantFactRecord>(StringComparer.OrdinalIgnoreCase);
		}
		if (_merchantFactStorage == null)
		{
			_merchantFactStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		if (dataStore.IsSaving)
		{
			_merchantFactStorage.Clear();
			foreach (KeyValuePair<string, MerchantFactRecord> merchantFact in _merchantFacts)
			{
				if (string.IsNullOrWhiteSpace(merchantFact.Key) || merchantFact.Value == null)
				{
					continue;
				}
				CleanupMerchantFactRecord(merchantFact.Value);
				if (merchantFact.Value.Facts != null && merchantFact.Value.Facts.Count > 0)
				{
					try
					{
						_merchantFactStorage[merchantFact.Key] = JsonConvert.SerializeObject(merchantFact.Value);
					}
					catch (Exception ex)
					{
						Logger.Log("RewardSystem", "[ERROR] Serialize merchant facts for " + merchantFact.Key + ": " + ex.Message);
					}
				}
			}
		}
		Dictionary<string, string> dictionary = dataStore.IsSaving ? CampaignSaveChunkHelper.FlattenStringDictionary(_merchantFactStorage, "_rewardMerchantFacts_v1", "MerchantFact") : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		dataStore.SyncData("_rewardMerchantFacts_v1", ref dictionary);
		_merchantFactStorage = CampaignSaveChunkHelper.RestoreStringDictionary(dictionary, "RewardSystem");
		if (dataStore.IsSaving)
		{
			return;
		}
		_merchantFacts.Clear();
		if (_merchantFactStorage == null)
		{
			return;
		}
		foreach (KeyValuePair<string, string> item in _merchantFactStorage)
		{
			if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
			{
				continue;
			}
			try
			{
				MerchantFactRecord merchantFactRecord = JsonConvert.DeserializeObject<MerchantFactRecord>(item.Value);
				if (merchantFactRecord != null)
				{
					CleanupMerchantFactRecord(merchantFactRecord);
					if (merchantFactRecord.Facts != null && merchantFactRecord.Facts.Count > 0)
					{
						_merchantFacts[item.Key] = merchantFactRecord;
					}
				}
			}
			catch (Exception ex2)
			{
				Logger.Log("RewardSystem", "[ERROR] Deserialize merchant facts for " + item.Key + ": " + ex2.Message);
			}
		}
	}

	private void SyncTrustData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		if (_npcTrust == null)
		{
			_npcTrust = new Dictionary<string, int>();
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		if (_npcTrustStorage == null)
		{
			_npcTrustStorage = new Dictionary<string, int>();
		}
		if (_publicTrustStorage == null)
		{
			_publicTrustStorage = new Dictionary<string, int>();
		}
		if (dataStore.IsSaving)
		{
			_npcTrustStorage.Clear();
			foreach (KeyValuePair<string, int> item in _npcTrust)
			{
				if (!string.IsNullOrWhiteSpace(item.Key))
				{
					int num = ClampTrust(item.Value);
					if (num != 0)
					{
						_npcTrustStorage[item.Key] = num;
					}
				}
			}
			_publicTrustStorage.Clear();
			foreach (KeyValuePair<string, int> item2 in _publicTrust)
			{
				if (!string.IsNullOrWhiteSpace(item2.Key))
				{
					int num2 = ClampTrust(item2.Value);
					if (num2 != 0)
					{
						_publicTrustStorage[item2.Key] = num2;
					}
				}
			}
		}
		dataStore.SyncData("_rewardNpcTrust_v1", ref _npcTrustStorage);
		dataStore.SyncData("_rewardPublicTrust_v1", ref _publicTrustStorage);
		if (dataStore.IsSaving)
		{
			return;
		}
		_npcTrust.Clear();
		if (_npcTrustStorage != null)
		{
			foreach (KeyValuePair<string, int> item3 in _npcTrustStorage)
			{
				if (!string.IsNullOrWhiteSpace(item3.Key))
				{
					int num3 = ClampTrust(item3.Value);
					if (num3 != 0)
					{
						_npcTrust[item3.Key] = num3;
					}
				}
			}
		}
		_publicTrust.Clear();
		if (_publicTrustStorage == null)
		{
			return;
		}
		foreach (KeyValuePair<string, int> item4 in _publicTrustStorage)
		{
			if (!string.IsNullOrWhiteSpace(item4.Key))
			{
				int num4 = ClampTrust(item4.Value);
				if (num4 != 0)
				{
					_publicTrust[item4.Key] = num4;
				}
			}
		}
	}

	private void SyncTradeTrustCarryData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		if (_tradeTrustValueCarry == null)
		{
			_tradeTrustValueCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_tradeTrustValueCarryStorage == null)
		{
			_tradeTrustValueCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (dataStore.IsSaving)
		{
			_tradeTrustValueCarryStorage.Clear();
			foreach (KeyValuePair<string, int> item in _tradeTrustValueCarry)
			{
				if (!string.IsNullOrWhiteSpace(item.Key) && item.Value > 0)
				{
					_tradeTrustValueCarryStorage[item.Key] = Math.Min(AutoTrustValuePerPoint - 1, Math.Max(0, item.Value));
				}
			}
		}
		dataStore.SyncData("_rewardTradeTrustCarry_v1", ref _tradeTrustValueCarryStorage);
		if (dataStore.IsSaving)
		{
			return;
		}
		_tradeTrustValueCarry.Clear();
		if (_tradeTrustValueCarryStorage == null)
		{
			return;
		}
		foreach (KeyValuePair<string, int> item2 in _tradeTrustValueCarryStorage)
		{
			if (!string.IsNullOrWhiteSpace(item2.Key))
			{
				int num = Math.Min(AutoTrustValuePerPoint - 1, Math.Max(0, item2.Value));
				if (num > 0)
				{
					_tradeTrustValueCarry[item2.Key] = num;
				}
			}
		}
	}

	private void SyncDirectTrustProgressCarryData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		if (_directTrustProgressCarry == null)
		{
			_directTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_directTrustProgressCarryStorage == null)
		{
			_directTrustProgressCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (dataStore.IsSaving)
		{
			_directTrustProgressCarryStorage.Clear();
			foreach (KeyValuePair<string, int> item in _directTrustProgressCarry)
			{
				if (!string.IsNullOrWhiteSpace(item.Key) && item.Value != 0)
				{
					_directTrustProgressCarryStorage[item.Key] = item.Value;
				}
			}
		}
		dataStore.SyncData("_rewardDirectTrustProgressCarry_v1", ref _directTrustProgressCarryStorage);
		if (dataStore.IsSaving)
		{
			return;
		}
		_directTrustProgressCarry.Clear();
		if (_directTrustProgressCarryStorage == null)
		{
			return;
		}
		foreach (KeyValuePair<string, int> item2 in _directTrustProgressCarryStorage)
		{
			if (!string.IsNullOrWhiteSpace(item2.Key) && item2.Value != 0)
			{
				_directTrustProgressCarry[item2.Key] = item2.Value;
			}
		}
	}

	private void SyncSettlementTrustCarryData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		if (_settlementTrustCentiCarry == null)
		{
			_settlementTrustCentiCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_settlementTrustCentiCarryStorage == null)
		{
			_settlementTrustCentiCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_settlementTrustSharedPublicCarry == null)
		{
			_settlementTrustSharedPublicCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_settlementTrustSharedPublicCarryStorage == null)
		{
			_settlementTrustSharedPublicCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (dataStore.IsSaving)
		{
			_settlementTrustCentiCarryStorage.Clear();
			foreach (KeyValuePair<string, int> item in _settlementTrustCentiCarry)
			{
				if (!string.IsNullOrWhiteSpace(item.Key) && item.Value != 0)
				{
					_settlementTrustCentiCarryStorage[item.Key] = item.Value;
				}
			}
			_settlementTrustSharedPublicCarryStorage.Clear();
			foreach (KeyValuePair<string, int> item2 in _settlementTrustSharedPublicCarry)
			{
				if (!string.IsNullOrWhiteSpace(item2.Key) && item2.Value != 0)
				{
					_settlementTrustSharedPublicCarryStorage[item2.Key] = item2.Value;
				}
			}
		}
		dataStore.SyncData("_rewardSettlementTrustCentiCarry_v2", ref _settlementTrustCentiCarryStorage);
		dataStore.SyncData("_rewardSettlementTrustSharedPublicCarry_v1", ref _settlementTrustSharedPublicCarryStorage);
		if (dataStore.IsSaving)
		{
			return;
		}
		_settlementTrustCentiCarry.Clear();
		if (_settlementTrustCentiCarryStorage != null)
		{
			foreach (KeyValuePair<string, int> item3 in _settlementTrustCentiCarryStorage)
			{
				if (!string.IsNullOrWhiteSpace(item3.Key) && item3.Value != 0)
				{
					_settlementTrustCentiCarry[item3.Key] = item3.Value;
				}
			}
		}
		_settlementTrustSharedPublicCarry.Clear();
		if (_settlementTrustSharedPublicCarryStorage == null)
		{
			return;
		}
		foreach (KeyValuePair<string, int> item4 in _settlementTrustSharedPublicCarryStorage)
		{
			if (!string.IsNullOrWhiteSpace(item4.Key) && item4.Value != 0)
			{
				_settlementTrustSharedPublicCarry[item4.Key] = item4.Value;
			}
		}
	}

	private void SyncPublicTrustProgressCarryData(IDataStore dataStore)
	{
		if (dataStore == null)
		{
			return;
		}
		if (_publicTrustProgressCarry == null)
		{
			_publicTrustProgressCarry = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_publicTrustProgressCarryStorage == null)
		{
			_publicTrustProgressCarryStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (dataStore.IsSaving)
		{
			_publicTrustProgressCarryStorage.Clear();
			foreach (KeyValuePair<string, int> item in _publicTrustProgressCarry)
			{
				if (!string.IsNullOrWhiteSpace(item.Key) && item.Value != 0)
				{
					_publicTrustProgressCarryStorage[item.Key] = item.Value;
				}
			}
		}
		dataStore.SyncData("_rewardPublicTrustProgressCarry_v2", ref _publicTrustProgressCarryStorage);
		if (dataStore.IsSaving)
		{
			return;
		}
		_publicTrustProgressCarry.Clear();
		if (_publicTrustProgressCarryStorage != null)
		{
			foreach (KeyValuePair<string, int> item2 in _publicTrustProgressCarryStorage)
			{
				if (!string.IsNullOrWhiteSpace(item2.Key) && item2.Value != 0)
				{
					_publicTrustProgressCarry[item2.Key] = item2.Value;
				}
			}
		}
		MigrateLegacySettlementSharedPublicCarryToUnifiedPool();
	}

	private static bool TryResolveSettlementByLocalPublicTrustKey(string key, out Settlement settlement)
	{
		settlement = null;
		string text = (key ?? "").Trim();
		const string text2 = "public:settlement:";
		if (!text.StartsWith(text2, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string text3 = text.Substring(text2.Length).Trim();
		if (string.IsNullOrWhiteSpace(text3))
		{
			return false;
		}
		try
		{
			settlement = Settlement.All.FirstOrDefault((Settlement x) => x != null && string.Equals((x.StringId ?? "").Trim(), text3, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			settlement = null;
		}
		return settlement != null;
	}

	private void MigrateLegacySettlementSharedPublicCarryToUnifiedPool()
	{
		if (_settlementTrustSharedPublicCarry == null || _settlementTrustSharedPublicCarry.Count <= 0)
		{
			return;
		}
		foreach (KeyValuePair<string, int> item in _settlementTrustSharedPublicCarry.ToList())
		{
			if (item.Value == 0 || !TryResolveSettlementByLocalPublicTrustKey(item.Key, out var settlement))
			{
				continue;
			}
			string settlementSharedPublicTrustKey = BuildSettlementSharedPublicTrustKey(settlement);
			if (!string.IsNullOrWhiteSpace(settlementSharedPublicTrustKey))
			{
				ApplyPublicTrustPoolDeltaByKey(settlementSharedPublicTrustKey, item.Value * TrustGainUnitsPerPoint, "legacy_settlement_public_pool_migration");
			}
		}
		_settlementTrustSharedPublicCarry.Clear();
	}

	private static int ClampTrust(int value)
	{
		if (value < -100)
		{
			return -100;
		}
		if (value > 100)
		{
			return 100;
		}
		return value;
	}

	private static int ToTenLevelIndexByTrust(int trust)
	{
		double num = ((double)ClampTrust(trust) + 100.0) / 200.0;
		int num2 = (int)Math.Floor(num * 10.0) + 1;
		if (num2 < 1)
		{
			num2 = 1;
		}
		if (num2 > 10)
		{
			num2 = 10;
		}
		return num2;
	}

	public static int GetTrustLevelIndex(int trust)
	{
		return ToTenLevelIndexByTrust(trust);
	}

	public static string GetTrustLevelText(int trust)
	{
		int num = ToTenLevelIndexByTrust(trust);
		return TrustLevelTexts[num - 1];
	}

	public static string GetTrustBehaviorText(int trust)
	{
		int num = ToTenLevelIndexByTrust(trust);
		return TrustAiBehaviorTexts[num - 1];
	}

	public static string GetTrustActionGuideText(int trust)
	{
		int num = ToTenLevelIndexByTrust(trust);
		return TrustAiActionGuideTexts[num - 1];
	}

	private static string BuildNpcTrustKey(Hero npc)
	{
		string text = (npc?.StringId ?? "").Trim().ToLower();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "hero:" + text;
	}

	private static string BuildPublicTrustKey(Hero npc)
	{
		string text = "";
		try
		{
			text = (npc?.MapFaction?.StringId ?? "").Trim().ToLower();
		}
		catch
		{
			text = "";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = (npc?.Clan?.Kingdom?.StringId ?? "").Trim().ToLower();
			}
			catch
			{
				text = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = (npc?.Clan?.StringId ?? "").Trim().ToLower();
			}
			catch
			{
				text = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = (npc?.Culture?.StringId ?? "").Trim().ToLower();
			}
			catch
			{
				text = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = (npc?.StringId ?? "").Trim().ToLower();
			}
			catch
			{
				text = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "public:" + text;
	}

	private static string BuildSettlementLocalPublicTrustKey(Settlement settlement)
	{
		string text = (settlement?.StringId ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "public:settlement:" + text;
	}

	private static string BuildSettlementPublicTrustKey(Settlement settlement)
	{
		return BuildSettlementLocalPublicTrustKey(settlement);
	}

	private static string BuildSettlementSharedPublicTrustKey(Settlement settlement)
	{
		string text = "";
		try
		{
			text = (settlement?.MapFaction?.StringId ?? "").Trim().ToLowerInvariant();
		}
		catch
		{
			text = "";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = (settlement?.OwnerClan?.Kingdom?.StringId ?? "").Trim().ToLowerInvariant();
			}
			catch
			{
				text = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = (settlement?.OwnerClan?.StringId ?? "").Trim().ToLowerInvariant();
			}
			catch
			{
				text = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = (settlement?.Culture?.StringId ?? "").Trim().ToLowerInvariant();
			}
			catch
			{
				text = "";
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "public:" + text;
	}

	private static string BuildSettlementFactionPublicTrustKey(Settlement settlement)
	{
		return BuildSettlementSharedPublicTrustKey(settlement);
	}

	private static string BuildPublicTrustLabel(Hero npc)
	{
		try
		{
			string text = npc?.Clan?.Kingdom?.Name?.ToString();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Trim();
			}
		}
		catch
		{
		}
		try
		{
			string text2 = npc?.MapFaction?.Name?.ToString();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				return text2.Trim();
			}
		}
		catch
		{
		}
		try
		{
			string text3 = npc?.Clan?.Name?.ToString();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				return text3.Trim();
			}
		}
		catch
		{
		}
		return "其所属势力";
	}

	public int GetNpcTrust(Hero npc)
	{
		if (npc == null)
		{
			return 0;
		}
		if (_npcTrust == null)
		{
			_npcTrust = new Dictionary<string, int>();
		}
		string text = BuildNpcTrustKey(npc);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		if (_npcTrust.TryGetValue(text, out var value))
		{
			return ClampTrust(value);
		}
		return 0;
	}

	public int GetPublicTrust(Hero npc)
	{
		if (npc == null)
		{
			return 0;
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		string text = BuildPublicTrustKey(npc);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		if (_publicTrust.TryGetValue(text, out var value))
		{
			return ClampTrust(value);
		}
		return 0;
	}

	public int GetEffectiveTrust(Hero npc)
	{
		int npcTrust = GetNpcTrust(npc);
		int publicTrust = GetPublicTrust(npc);
		return ClampTrust(npcTrust + publicTrust);
	}

	public int GetSettlementMerchantTrust(Settlement settlement, SettlementMerchantKind kind)
	{
		if (settlement == null || kind == SettlementMerchantKind.None)
		{
			return 0;
		}
		if (_npcTrust == null)
		{
			_npcTrust = new Dictionary<string, int>();
		}
		string text = BuildSettlementMerchantTrustKey(settlement, kind);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		if (_npcTrust.TryGetValue(text, out var value))
		{
			return ClampTrust(value);
		}
		return 0;
	}

	private int AdjustSettlementMerchantTrust(Settlement settlement, SettlementMerchantKind kind, int personalDelta, string reason, out int appliedUnits)
	{
		appliedUnits = 0;
		if (settlement == null || kind == SettlementMerchantKind.None || personalDelta == 0)
		{
			return 0;
		}
		if (_npcTrust == null)
		{
			_npcTrust = new Dictionary<string, int>();
		}
		string text = BuildSettlementMerchantTrustKey(settlement, kind);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		int settlementMerchantTrust = GetSettlementMerchantTrust(settlement, kind);
		int num2 = ApplyDirectTrustDeltaUnits(text, settlementMerchantTrust, personalDelta, out appliedUnits);
		int num = ClampTrust(settlementMerchantTrust + num2);
		if (num == 0)
		{
			_npcTrust.Remove(text);
		}
		else
		{
			_npcTrust[text] = num;
		}
		int num3 = ApplyPublicTrustPoolDeltaByKey(BuildSettlementSharedPublicTrustKey(settlement), appliedUnits, (reason ?? "merchant") + "_public_pool");
		Logger.Log("Trust", $"settlement={settlement.StringId} market={kind} reason={reason} trust={settlementMerchantTrust}->{num} rawDelta={personalDelta} appliedDelta={FormatTrustUnits(appliedUnits)} publicDelta={num3}");
		return num3;
	}

	private int AdjustSettlementMerchantTrustByExactUnits(Settlement settlement, SettlementMerchantKind kind, int personalUnits, string reason, out int appliedUnits)
	{
		appliedUnits = 0;
		if (settlement == null || kind == SettlementMerchantKind.None || personalUnits == 0)
		{
			return 0;
		}
		if (_npcTrust == null)
		{
			_npcTrust = new Dictionary<string, int>();
		}
		string trustKey = BuildSettlementMerchantTrustKey(settlement, kind);
		if (string.IsNullOrWhiteSpace(trustKey))
		{
			return 0;
		}
		int trustBefore = GetSettlementMerchantTrust(settlement, kind);
		int wholeDelta = ApplyExactDirectTrustDeltaUnits(trustKey, trustBefore, personalUnits, out appliedUnits);
		int trustAfter = ClampTrust(trustBefore + wholeDelta);
		if (trustAfter == 0)
		{
			_npcTrust.Remove(trustKey);
		}
		else
		{
			_npcTrust[trustKey] = trustAfter;
		}
		int publicDelta = ApplyPublicTrustPoolDeltaByKey(BuildSettlementSharedPublicTrustKey(settlement), appliedUnits, (reason ?? "merchant_exact_units") + "_public_pool");
		Logger.Log("Trust", $"settlement={settlement.StringId} market={kind} reason={reason} trust={trustBefore}->{trustAfter} requestedUnits={personalUnits} appliedDelta={FormatTrustUnits(appliedUnits)} publicDelta={publicDelta}");
		return publicDelta;
	}

	public string BuildTrustStatusInlineForAI(Hero npc)
	{
		if (npc == null)
		{
			return "综合信任 0（中性观望，6/10）";
		}
		int effectiveTrust = GetEffectiveTrust(npc);
		int trustLevelIndex = GetTrustLevelIndex(effectiveTrust);
		return $"综合信任 {effectiveTrust}（{GetTrustLevelText(effectiveTrust)}，{trustLevelIndex}/10）";
	}

	public string BuildTrustPromptForAI(Hero npc)
	{
		int effectiveTrust = GetEffectiveTrust(npc);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("本级语义：" + GetTrustBehaviorText(effectiveTrust));
		stringBuilder.AppendLine("本级信用规则：" + GetTrustActionGuideText(effectiveTrust));
		stringBuilder.AppendLine("价值口径：总价值=第纳尔金额+物品估值（guidePrice * 数量）。");
		return stringBuilder.ToString().TrimEnd();
	}

	private static int CompareSettlementTransferEntries(MyBehavior.SettlementTransferPromptEntry x, MyBehavior.SettlementTransferPromptEntry y)
	{
		int num = Math.Max(-999999999, x?.DailyIncomeDenars ?? 0).CompareTo(Math.Max(-999999999, y?.DailyIncomeDenars ?? 0));
		if (num != 0)
		{
			return num;
		}
		num = Math.Max(0, x?.GuidePriceDenars ?? 0).CompareTo(Math.Max(0, y?.GuidePriceDenars ?? 0));
		if (num != 0)
		{
			return num;
		}
		num = string.Compare(MyBehavior.GetSettlementTransferAssetIdForExternal(x), MyBehavior.GetSettlementTransferAssetIdForExternal(y), StringComparison.OrdinalIgnoreCase);
		if (num != 0)
		{
			return num;
		}
		return string.Compare((x?.DisplayName ?? "").Trim(), (y?.DisplayName ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
	}

	private static List<MyBehavior.SettlementTransferPromptEntry> SortSettlementTransferEntries(IEnumerable<MyBehavior.SettlementTransferPromptEntry> entries)
	{
		return (entries ?? Enumerable.Empty<MyBehavior.SettlementTransferPromptEntry>()).Where(MyBehavior.IsSettlementTransferEntryValidForExternal).OrderBy((MyBehavior.SettlementTransferPromptEntry x) => x, Comparer<MyBehavior.SettlementTransferPromptEntry>.Create(CompareSettlementTransferEntries)).ToList();
	}

	public int GetSettlementTransferTalkTrust(Hero npc)
	{
		return GetEffectiveTrust(npc);
	}

	public List<MyBehavior.SettlementTransferPromptEntry> GetAllowedNpcSettlementTransferEntriesForPlayer(Hero targetHero, CharacterObject targetCharacter = null)
	{
		return SortSettlementTransferEntries(MyBehavior.BuildSettlementTransferPromptEntriesForExternal(targetHero, targetCharacter).Where((MyBehavior.SettlementTransferPromptEntry x) => x != null && x.Section == MyBehavior.SettlementTransferEntrySection.NpcFiefs));
	}

	public string BuildSettlementTransferPromptGuidanceForAI(Hero targetHero, CharacterObject targetCharacter = null)
	{
		Hero hero = targetHero ?? targetCharacter?.HeroObject;
		int settlementTransferTalkTrust = GetSettlementTransferTalkTrust(hero);
		int num = RomanceSystemBehavior.TryGetPrivateLoveAsPlayerRelation(hero, out var relation) ? relation : (int)MathF.Round(hero?.GetRelationWithPlayer() ?? 0f);
		if (settlementTransferTalkTrust < 60)
		{
			return $"【固定资产转移谈判提示】综合信任{settlementTransferTalkTrust}<60。本轮可谈清单内固定资产转移，但若你愿意把资产出售或过户给玩家，正文必须按远高于该资产指导价/资产价格的报价出售，或要求等值的明显超额利益；玩家未先付清或交付对价前，不要立即过户。若想不经交易直接白拿，通常只在你与玩家关系达到100时才考虑。当前关系={num}。";
		}
		return $"【固定资产转移谈判提示】综合信任{settlementTransferTalkTrust}。本轮可谈清单内固定资产转移，但玩家通常仍得先给出明显利益；若想不经交易直接白拿，通常只在你与玩家关系达到100时才考虑。当前关系={num}。";
	}

	public bool TryApplyPlayerSettlementTransferForExternal(Hero receiverHero, Settlement settlement, out string statusText)
	{
		MyBehavior.SettlementTransferPromptEntry entry = new MyBehavior.SettlementTransferPromptEntry
		{
			Section = MyBehavior.SettlementTransferEntrySection.PlayerFiefs,
			AssetKind = MyBehavior.SettlementTransferAssetKind.Settlement,
			Settlement = settlement,
			SettlementId = (settlement?.StringId ?? "").Trim(),
			AssetId = (settlement?.StringId ?? "").Trim(),
			DisplayName = settlement?.Name?.ToString() ?? "未知定居点",
			TypeLabel = settlement?.IsTown == true ? "城市" : "城堡",
			OwnerClan = Clan.PlayerClan
		};
		return TryApplyPlayerSettlementTransferForExternal(receiverHero, entry, out statusText);
	}

	public bool TryApplyPlayerSettlementTransferForExternal(Hero receiverHero, string assetToken, out string statusText)
	{
		MyBehavior.SettlementTransferPromptEntry entry = MyBehavior.ResolveSettlementTransferEntryForExternal(receiverHero, receiverHero?.CharacterObject, "TO_NPC", assetToken);
		return TryApplyPlayerSettlementTransferForExternal(receiverHero, entry, out statusText);
	}

	public bool TryApplyPlayerSettlementTransferForExternal(Hero receiverHero, MyBehavior.SettlementTransferPromptEntry entry, out string statusText)
	{
		return TryApplySettlementTransferEntryAction(receiverHero, Hero.MainHero, "TO_NPC", entry, out statusText);
	}

	private bool TryApplySettlementTransferEntryAction(Hero giver, Hero receiver, string directionToken, MyBehavior.SettlementTransferPromptEntry entry, out string statusText)
	{
		return TryApplySettlementTransferEntryAction(giver, receiver, directionToken, entry, allowDirectFixedAssetIdOverride: false, out statusText);
	}

	private bool TryApplySettlementTransferEntryAction(Hero giver, Hero receiver, string directionToken, MyBehavior.SettlementTransferPromptEntry entry, bool allowDirectFixedAssetIdOverride, out string statusText)
	{
		return TryApplySettlementTransferEntryAction(
			giver,
			receiver,
			directionToken,
			entry,
			allowDirectFixedAssetIdOverride,
			out statusText,
			mutationObservation: null);
	}

	private bool TryApplySettlementTransferEntryAction(Hero giver, Hero receiver, string directionToken, MyBehavior.SettlementTransferPromptEntry entry, bool allowDirectFixedAssetIdOverride, out string statusText, EconomyMutationObservation mutationObservation)
	{
		statusText = "";
		try
		{
			if (giver == null || receiver == null)
			{
				statusText = "执行失败：缺少转移双方。";
				return false;
			}
			if (AIConfigHandler.IsPlayerCompanionOrFamilyTradeTarget(giver))
			{
				statusText = "执行失败：家族成员或同伴不允许通过固定资产转移。";
				return false;
			}
			if (!MyBehavior.IsSettlementTransferEntryValidForExternal(entry))
			{
				statusText = "执行失败：缺少可转移固定资产。";
				return false;
			}
			string direction = (directionToken ?? "").Trim().ToUpperInvariant();
			if (direction != "TO_PLAYER" && direction != "TO_NPC")
			{
				statusText = "执行失败：未知固定资产转移方向 " + directionToken + "。";
				return false;
			}
			Hero targetOwner = direction == "TO_PLAYER" ? receiver : giver;
			string assetName = MyBehavior.GetSettlementTransferAssetDisplayNameForExternal(entry);
			switch (entry.AssetKind)
			{
			case MyBehavior.SettlementTransferAssetKind.Settlement:
				return TryApplySettlementFixedAssetTransfer(giver, receiver, targetOwner, direction, entry.Settlement, assetName, allowDirectFixedAssetIdOverride, out statusText);
			case MyBehavior.SettlementTransferAssetKind.Workshop:
				return TryApplyWorkshopFixedAssetTransfer(giver, receiver, targetOwner, direction, entry.Workshop, assetName, allowDirectFixedAssetIdOverride, out statusText);
			case MyBehavior.SettlementTransferAssetKind.Caravan:
				return TryApplyCaravanFixedAssetTransfer(giver, receiver, targetOwner, direction, entry.CaravanParty, assetName, allowDirectFixedAssetIdOverride, out statusText);
			default:
				statusText = "执行失败：未知固定资产类型。";
				return false;
			}
		}
		catch (Exception ex)
		{
			mutationObservation?.MarkUnknown("economy.settlement_transfer_entry_exception");
			statusText = "执行失败（异常）：" + ex.Message;
			return false;
		}
	}

	private bool TryApplySettlementFixedAssetTransfer(Hero giver, Hero receiver, Hero targetOwner, string direction, Settlement settlement, string assetName, bool allowDirectFixedAssetIdOverride, out string statusText)
	{
		statusText = "";
		if (settlement == null || !settlement.IsFortification || settlement.Town == null)
		{
			statusText = $"执行失败：{assetName} 不是可转移的城市或城堡。";
			return false;
		}
		if (settlement.OwnerClan == null)
		{
			statusText = $"执行失败：{assetName} 当前没有可识别的合法归属。";
			return false;
		}
		if (settlement.IsUnderSiege || settlement.Party?.MapEvent != null)
		{
			statusText = $"执行失败：{assetName} 当前处于战事或围攻状态，不能转移。";
			return false;
		}
		if (direction == "TO_PLAYER")
		{
			if (!allowDirectFixedAssetIdOverride && settlement.OwnerClan != giver.Clan)
			{
				statusText = $"执行失败：{assetName} 不属于 {giver.Name} 的家族，不能由其转给玩家。";
				return false;
			}
		}
		else if (Clan.PlayerClan == null || settlement.OwnerClan != Clan.PlayerClan)
		{
			statusText = $"执行失败：{assetName} 当前不属于玩家家族。";
			return false;
		}
		if (targetOwner?.Clan == null)
		{
			statusText = "执行失败：目标接收方没有合法家族。";
			return false;
		}
		if (settlement.OwnerClan == targetOwner.Clan)
		{
			statusText = $"执行跳过：{assetName} 已经属于目标家族。";
			return false;
		}
		ChangeOwnerOfSettlementAction.ApplyByBarter(targetOwner, settlement);
		statusText = direction == "TO_PLAYER" ? $"执行成功：{assetName} 已转交给玩家家族（未自动扣款）。" : $"执行成功：玩家已将 {assetName} 转交给 {giver.Name} 的家族（未自动扣款）。";
		return true;
	}

	private static bool IsWorkshopFixedAssetStateUsable(Workshop workshop)
	{
		try
		{
			return workshop != null
				&& workshop.Settlement?.Town != null
				&& workshop.Settlement.OwnerClan != null
				&& workshop.Settlement.Town.Owner?.ItemRoster != null
				&& workshop.WorkshopType?.Productions != null;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsValidWorkshopFixedAssetOwner(Hero hero)
	{
		try
		{
			return hero != null && hero.IsAlive && hero.CharacterObject != null;
		}
		catch
		{
			return false;
		}
	}

	private bool TryApplyWorkshopFixedAssetTransfer(Hero giver, Hero receiver, Hero targetOwner, string direction, Workshop workshop, string assetName, bool allowDirectFixedAssetIdOverride, out string statusText)
	{
		statusText = "";
		if (!IsWorkshopFixedAssetStateUsable(workshop))
		{
			statusText = $"执行失败：{assetName} 不是可安全转移的工坊。";
			return false;
		}
		Hero oldOwner = workshop.Owner;
		if (oldOwner == null)
		{
			statusText = $"执行失败：{assetName} 当前没有可识别的工坊主人。";
			return false;
		}
		if (!IsValidWorkshopFixedAssetOwner(oldOwner))
		{
			statusText = $"执行失败：{assetName} 当前工坊主人无效或已死亡。";
			return false;
		}
		if (direction == "TO_PLAYER")
		{
			if (!allowDirectFixedAssetIdOverride && oldOwner != giver)
			{
				statusText = $"执行失败：{assetName} 不属于 {giver.Name}，不能由其转给玩家。";
				return false;
			}
		}
		else if (oldOwner != Hero.MainHero)
		{
			statusText = $"执行失败：{assetName} 当前不属于玩家本人。";
			return false;
		}
		if (targetOwner == null)
		{
			statusText = "执行失败：缺少工坊接收者。";
			return false;
		}
		if (!IsValidWorkshopFixedAssetOwner(targetOwner))
		{
			statusText = "执行失败：工坊接收者不是有效存活英雄。";
			return false;
		}
		if (oldOwner == targetOwner)
		{
			statusText = $"执行跳过：{assetName} 已经属于目标人物。";
			return false;
		}
		workshop.ChangeOwnerOfWorkshop(targetOwner, workshop.WorkshopType, workshop.Capital);
		CampaignEventDispatcher.Instance.OnWorkshopOwnerChanged(workshop, oldOwner);
		statusText = direction == "TO_PLAYER" ? $"执行成功：{assetName} 已转交给玩家（未自动扣款）。" : $"执行成功：玩家已将 {assetName} 转交给 {giver.Name}（未自动扣款）。";
		return true;
	}

	private bool TryApplyCaravanFixedAssetTransfer(Hero giver, Hero receiver, Hero targetOwner, string direction, MobileParty caravanParty, string assetName, bool allowDirectFixedAssetIdOverride, out string statusText)
	{
		statusText = "";
		CaravanPartyComponent component = caravanParty?.CaravanPartyComponent;
		if (caravanParty == null || !caravanParty.IsActive || component == null)
		{
			statusText = $"执行失败：{assetName} 不是可转移商队。";
			return false;
		}
		if (caravanParty.MapEvent != null)
		{
			statusText = $"执行失败：{assetName} 当前处于事件或战斗中，不能转移。";
			return false;
		}
		Hero oldOwner = component.Owner;
		if (oldOwner == null || !IsValidWorkshopFixedAssetOwner(oldOwner))
		{
			statusText = $"执行失败：{assetName} 当前商队主人无效或已死亡。";
			return false;
		}
		if (direction == "TO_PLAYER")
		{
			if (!allowDirectFixedAssetIdOverride && oldOwner != giver)
			{
				statusText = $"执行失败：{assetName} 不属于 {giver.Name}，不能由其转给玩家。";
				return false;
			}
		}
		else if (oldOwner != Hero.MainHero)
		{
			statusText = $"执行失败：{assetName} 当前不属于玩家本人。";
			return false;
		}
		if (targetOwner == null)
		{
			statusText = "执行失败：缺少商队接收者。";
			return false;
		}
		if (oldOwner == targetOwner)
		{
			statusText = $"执行跳过：{assetName} 已经属于目标人物。";
			return false;
		}
		Settlement homeSettlement = component.Settlement ?? targetOwner.HomeSettlement ?? Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
		if (homeSettlement == null)
		{
			statusText = $"执行失败：{assetName} 缺少可用的商队归属定居点。";
			return false;
		}
		CaravanPartyComponent.TransferCaravanOwnership(caravanParty, targetOwner, homeSettlement);
		statusText = direction == "TO_PLAYER" ? $"执行成功：{assetName} 已转交给玩家（未自动扣款）。" : $"执行成功：玩家已将 {assetName} 转交给 {giver.Name}（未自动扣款）。";
		return true;
	}

	private static List<Clan> FindRulingClanCandidatesForRecruitmentElection(Kingdom kingdom, Clan excludedClan, Hero excludedLeader)
	{
		List<Clan> fallback = new List<Clan>();
		List<Clan> eligible = new List<Clan>();
		try
		{
			if (kingdom == null || kingdom.IsEliminated || kingdom.Clans == null)
			{
				return fallback;
			}
			foreach (Clan clan in kingdom.Clans)
			{
				if (clan == null || clan == excludedClan || clan.IsEliminated || clan.IsUnderMercenaryService || clan.Leader == null || clan.Leader == excludedLeader)
				{
					continue;
				}
				fallback.Add(clan);
				try
				{
					if (Campaign.Current?.Models?.DiplomacyModel?.IsClanEligibleToBecomeRuler(clan) == true)
					{
						eligible.Add(clan);
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
			return fallback;
		}
		List<Clan> source = eligible.Count > 0 ? eligible : fallback;
		return source.OrderByDescending(GetRulingClanCandidateScoreForRecruitmentElection).ToList();
	}

	private static float GetRulingClanCandidateScoreForRecruitmentElection(Clan clan)
	{
		float result = 0f;
		try
		{
			result = Campaign.Current?.Models?.DiplomacyModel?.GetClanStrength(clan) ?? 0f;
		}
		catch
		{
			result = 0f;
		}
		try
		{
			result += clan?.Influence ?? 0f;
		}
		catch
		{
		}
		return result;
	}

	private static string BuildRecruitmentTransitionSummary(List<string> transitionNotes)
	{
		try
		{
			if (transitionNotes == null || transitionNotes.Count == 0)
			{
				return "";
			}
			List<string> list = transitionNotes.Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()).ToList();
			if (list.Count == 0)
			{
				return "";
			}
			return "（" + string.Join("；", list) + "）";
		}
		catch
		{
			return "";
		}
	}

	private static string BuildWildernessHeroPartyTransferCountText(int memberCount, int prisonerCount)
	{
		if (memberCount > 0 && prisonerCount > 0)
		{
			return memberCount + " 名非英雄成员及" + prisonerCount + " 名俘虏";
		}
		if (memberCount > 0)
		{
			return memberCount + " 名非英雄成员";
		}
		return prisonerCount > 0 ? prisonerCount + " 名俘虏" : "";
	}

	private static string BuildWildernessHeroPartyTransitionNote(string partyName, int memberCount, int prisonerCount)
	{
		string label = string.IsNullOrWhiteSpace(partyName) ? "目标英雄的原野外队伍" : partyName.Trim();
		string countText = BuildWildernessHeroPartyTransferCountText(memberCount, prisonerCount);
		return string.IsNullOrEmpty(countText)
			? label + "已随目标英雄归并至玩家主队，没有额外成员或俘虏"
			: label + "中的 " + countText + "已一并转入玩家主队";
	}

	private static string BuildClanRecruitmentFiefSummary(IEnumerable<Settlement> carriedSettlements)
	{
		try
		{
			List<string> list = (carriedSettlements ?? Enumerable.Empty<Settlement>())
				.Where((Settlement x) => x != null && (x.IsTown || x.IsCastle))
				.Select((Settlement x) => x.Name?.ToString() ?? x.StringId ?? "")
				.Where((string x) => !string.IsNullOrWhiteSpace(x))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Take(8)
				.ToList();
			if (list.Count == 0)
			{
				return "";
			}
			return "，其封地 " + string.Join("、", list) + " 已随家族归入玩家王国";
		}
		catch
		{
			return "";
		}
	}

	private static void PrepareRulingClanTransitionForDepartingClan(Kingdom kingdom, Clan departingClan, List<string> transitionNotes, out bool destroyKingdomAfterDeparture)
	{
		destroyKingdomAfterDeparture = false;
		try
		{
			if (kingdom == null || kingdom.IsEliminated || departingClan == null || kingdom.RulingClan != departingClan)
			{
				return;
			}
			string kingdomName = kingdom.Name?.ToString() ?? "旧王国";
			List<Clan> candidates = FindRulingClanCandidatesForRecruitmentElection(kingdom, departingClan, null);
			if (candidates.Count == 0)
			{
				destroyKingdomAfterDeparture = true;
				transitionNotes?.Add($"{kingdomName} 已无可接任执政家族，将在目标家族离开后清理旧王国");
				return;
			}
			Clan temporaryRulingClan = candidates[0];
			ChangeRulingClanAction.Apply(kingdom, temporaryRulingClan);
			transitionNotes?.Add($"{kingdomName} 已由 {GetClanDisplayNameForNotification(temporaryRulingClan)} 接任执政家族");
		}
		catch (Exception ex)
		{
			Logger.Log("Logic", "[Reward] prepare departing ruling clan transition failed: " + ex.Message);
			transitionNotes?.Add("旧王国执政家族修复失败：" + ex.Message);
		}
	}

	private static void FinalizeRulingClanTransitionForDepartedClan(Kingdom kingdom, bool destroyKingdomAfterDeparture, List<string> transitionNotes)
	{
		if (!destroyKingdomAfterDeparture)
		{
			return;
		}
		try
		{
			if (kingdom == null || kingdom.IsEliminated)
			{
				return;
			}
			string kingdomName = kingdom.Name?.ToString() ?? "旧王国";
			DestroyKingdomAction.Apply(kingdom);
			transitionNotes?.Add($"{kingdomName} 已按原版逻辑解散");
		}
		catch (Exception ex)
		{
			Logger.Log("Logic", "[Reward] finalize departed ruling clan transition failed: " + ex.Message);
			transitionNotes?.Add("旧王国解散失败：" + ex.Message);
		}
	}

	private static void RepairRulingClanAfterLeaderRecruitmentWithElection(Kingdom kingdom, Clan rulingClan, Hero removedLeader, List<string> transitionNotes)
	{
		try
		{
			if (kingdom == null || kingdom.IsEliminated || rulingClan == null || kingdom.RulingClan != rulingClan || rulingClan.Kingdom != kingdom)
			{
				return;
			}
			string kingdomName = kingdom.Name?.ToString() ?? "旧王国";
			List<Clan> candidates = FindRulingClanCandidatesForRecruitmentElection(kingdom, null, removedLeader);
			if (candidates.Count == 0)
			{
				DestroyKingdomAction.Apply(kingdom);
				transitionNotes?.Add($"{kingdomName} 已无可接任执政家族，已按原版逻辑解散");
				return;
			}
			if (candidates.Count > 1)
			{
				KingSelectionKingdomDecision decision = new KingSelectionKingdomDecision(rulingClan)
				{
					IsEnforced = true
				};
				kingdom.AddDecision(decision, ignoreInfluenceCost: true);
				transitionNotes?.Add($"{kingdomName} 已因原国王离开触发新统治者选举");
				return;
			}
			Clan onlyCandidate = candidates[0];
			if (kingdom.RulingClan != onlyCandidate)
			{
				ChangeRulingClanAction.Apply(kingdom, onlyCandidate);
			}
			transitionNotes?.Add($"{kingdomName} 已由 {GetClanDisplayNameForNotification(onlyCandidate)} 直接接任执政");
		}
		catch (Exception ex)
		{
			Logger.Log("Logic", "[Reward] repair ruling clan after leader recruitment failed: " + ex.Message);
			transitionNotes?.Add("旧王国执政家族修复失败：" + ex.Message);
		}
	}

	private static bool IsHeroPlayerClanLordInMainParty(Hero hero)
	{
		try
		{
			Clan playerClan = Clan.PlayerClan;
			MobileParty mainParty = MobileParty.MainParty;
			return hero != null
				&& playerClan != null
				&& mainParty != null
				&& hero.Clan == playerClan
				&& hero.Occupation == Occupation.Lord
				&& hero.CompanionOf == null
				&& hero.IsActive
				&& !hero.IsPrisoner
				&& hero.PartyBelongedToAsPrisoner == null
				&& !IsHeroInPrisonRoster(hero, PartyBase.MainParty)
				&& (hero.PartyBelongedTo == mainParty || IsHeroInParty(hero, mainParty));
		}
		catch
		{
			return false;
		}
	}

	private static bool IsHeroPlayerCompanionInMainParty(Hero hero)
	{
		try
		{
			Clan playerClan = Clan.PlayerClan;
			MobileParty mainParty = MobileParty.MainParty;
			return hero != null
				&& playerClan != null
				&& mainParty != null
				&& hero.CompanionOf == playerClan
				&& hero.Occupation == Occupation.Wanderer
				&& hero.IsActive
				&& !hero.IsPrisoner
				&& hero.PartyBelongedToAsPrisoner == null
				&& !IsHeroInPrisonRoster(hero, PartyBase.MainParty)
				&& (hero.PartyBelongedTo == mainParty || IsHeroInParty(hero, mainParty));
		}
		catch
		{
			return false;
		}
	}

	private static bool ShouldPreservePlayerFamilyIdentityForCompanionJoin(Hero hero)
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			Clan playerClan = Clan.PlayerClan ?? mainHero?.Clan;
			if (hero == null || mainHero == null || playerClan == null || hero == mainHero)
			{
				return false;
			}
			if (hero.Spouse == mainHero || mainHero.Spouse == hero)
			{
				return true;
			}
			return hero.CompanionOf == null && hero.Clan == playerClan;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryEndHeroCaptivityForPlayerJoin(Hero hero, PartyBase originalCaptivityParty, string reason, List<string> transitionNotes, out string statusText)
	{
		statusText = "";
		try
		{
			if (hero?.CharacterObject == null)
			{
				statusText = "执行失败：缺少要解除俘虏身份的目标英雄。";
				return false;
			}
			PartyBase captivityParty = hero.PartyBelongedToAsPrisoner ?? originalCaptivityParty;
			PartyBase mainParty = PartyBase.MainParty;
			bool hasCaptivityState = hero.IsPrisoner || hero.PartyBelongedToAsPrisoner != null;
			bool hasSourceRosterEntry = IsHeroInPrisonRoster(hero, captivityParty);
			bool hasMainPartyRosterEntry = IsHeroInPrisonRoster(hero, mainParty);
			if (!hasCaptivityState && !hasSourceRosterEntry && !hasMainPartyRosterEntry)
			{
				return true;
			}
			Hero.CharacterStates oldState = hero.HeroState;
			if (hasCaptivityState)
			{
				EndCaptivityAction.ApplyByReleasedByChoice(hero, Hero.MainHero);
			}
			int residualRemoved = RemoveAllHeroCopiesFromPrisonRoster(captivityParty, hero);
			if (mainParty != null && mainParty != captivityParty)
			{
				residualRemoved += RemoveAllHeroCopiesFromPrisonRoster(mainParty, hero);
			}
			bool stillCaptive = hero.IsPrisoner
				|| hero.PartyBelongedToAsPrisoner != null
				|| IsHeroInPrisonRoster(hero, captivityParty)
				|| (mainParty != captivityParty && IsHeroInPrisonRoster(hero, mainParty));
			if (stillCaptive)
			{
				statusText = "执行失败：目标英雄的俘虏状态或俘虏名册残留未能清除。";
				Logger.Log("RewardSystemBehavior", "[HeroJoin] captivity_cleanup_incomplete reason=" + (reason ?? "") + " hero=" + (hero.StringId ?? "") + " state=" + hero.HeroState + " prisonerParty=" + (hero.PartyBelongedToAsPrisoner?.Id ?? "") + " residualRemoved=" + residualRemoved);
				return false;
			}
			transitionNotes?.Add("已解除其俘虏身份");
			Logger.Log("RewardSystemBehavior", "[HeroJoin] captivity_ended reason=" + (reason ?? "") + " hero=" + (hero.StringId ?? "") + " oldState=" + oldState + " newState=" + hero.HeroState + " source=" + (captivityParty?.Id ?? "") + " residualRemoved=" + residualRemoved);
			return true;
		}
		catch (Exception ex)
		{
			statusText = "执行失败（解除俘虏身份异常）：" + ex.Message;
			Logger.Log("RewardSystemBehavior", "[HeroJoin] captivity_cleanup_failed reason=" + (reason ?? "") + " hero=" + (hero?.StringId ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	private static bool TryMoveHeroToPlayerClanAsLordAndMainParty(Hero hero, string reason, out string statusText)
	{
		statusText = "";
		try
		{
			Clan playerClan = Clan.PlayerClan;
			MobileParty mainParty = MobileParty.MainParty;
			if (hero == null)
			{
				statusText = "执行失败：缺少要加入玩家家族的目标英雄。";
				return false;
			}
			if (playerClan == null || mainParty == null)
			{
				statusText = "执行失败：玩家家族或玩家队伍不可用。";
				return false;
			}
			if (hero.CompanionOf != null)
			{
				hero.CompanionOf = null;
			}
			if (hero.Occupation != Occupation.Lord)
			{
				hero.SetNewOccupation(Occupation.Lord);
			}
			bool alreadyAliveLord = false;
			try
			{
				alreadyAliveLord = playerClan.AliveLords?.Contains(hero) == true;
			}
			catch
			{
				alreadyAliveLord = false;
			}
			if (hero.Clan != playerClan || !alreadyAliveLord)
			{
				if (hero.Clan == playerClan && !alreadyAliveLord)
				{
					hero.Clan = null;
				}
				hero.Clan = playerClan;
			}
			try
			{
				hero.UpdateHomeSettlement();
			}
			catch
			{
			}
			if (!hero.IsActive && !hero.IsDead && !TryActivatePromotedCompanionHero(hero, reason))
			{
				statusText = "执行失败：目标英雄未能恢复为可加入队伍的活动状态。";
				return false;
			}
			if (hero.PartyBelongedTo != mainParty && !IsHeroInParty(hero, mainParty))
			{
				AddHeroToPartyAction.Apply(hero, mainParty, showNotification: true);
			}
			if (!IsHeroPlayerClanLordInMainParty(hero))
			{
				statusText = "执行失败：目标英雄未能完成玩家家族成员身份或玩家主队归属更新。";
				return false;
			}
			Logger.Log("RewardSystemBehavior", "[HeroJoin] moved_to_player_clan reason=" + (reason ?? "") + " hero=" + (hero.StringId ?? "") + " clan=" + (hero.Clan?.StringId ?? "") + " occupation=" + hero.Occupation + " party=" + (hero.PartyBelongedTo?.StringId ?? ""));
			return true;
		}
		catch (Exception ex)
		{
			statusText = "执行失败（加入玩家家族异常）：" + ex.Message;
			Logger.Log("RewardSystemBehavior", "[HeroJoin] move_to_player_clan_failed reason=" + (reason ?? "") + " hero=" + (hero?.StringId ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	private static bool TryMoveHeroToPlayerClanAsCompanionAndMainParty(Hero hero, string reason, out string statusText)
	{
		statusText = "";
		try
		{
			Clan playerClan = Clan.PlayerClan;
			MobileParty mainParty = MobileParty.MainParty;
			if (hero == null)
			{
				statusText = "执行失败：缺少要成为玩家同伴的目标英雄。";
				return false;
			}
			if (playerClan == null || mainParty == null)
			{
				statusText = "执行失败：玩家家族或玩家队伍不可用。";
				return false;
			}
			if (hero.Clan != null)
			{
				hero.Clan = null;
			}
			if (hero.Occupation != Occupation.Wanderer)
			{
				hero.SetNewOccupation(Occupation.Wanderer);
			}
			if (hero.CompanionOf != playerClan)
			{
				AddCompanionAction.Apply(playerClan, hero);
			}
			if (!hero.IsActive && !hero.IsDead && !TryActivatePromotedCompanionHero(hero, reason))
			{
				statusText = "执行失败：目标英雄未能恢复为可加入队伍的活动状态。";
				return false;
			}
			if (hero.PartyBelongedTo != mainParty && !IsHeroInParty(hero, mainParty))
			{
				AddHeroToPartyAction.Apply(hero, mainParty, showNotification: true);
			}
			if (!IsHeroPlayerCompanionInMainParty(hero))
			{
				statusText = "执行失败：目标英雄未能完成同伴身份或玩家主队归属更新。";
				return false;
			}
			Logger.Log("RewardSystemBehavior", "[HeroJoin] moved_to_player_companion reason=" + (reason ?? "") + " hero=" + (hero.StringId ?? "") + " companionOf=" + (hero.CompanionOf?.StringId ?? "") + " occupation=" + hero.Occupation + " party=" + (hero.PartyBelongedTo?.StringId ?? ""));
			return true;
		}
		catch (Exception ex)
		{
			statusText = "执行失败（成为玩家同伴异常）：" + ex.Message;
			Logger.Log("RewardSystemBehavior", "[HeroJoin] move_to_player_companion_failed reason=" + (reason ?? "") + " hero=" + (hero?.StringId ?? "") + " error=" + ex.Message);
			return false;
		}
	}

	private static void RecordHeroJoinedPlayerClanForExternal(Hero hero, string reason, bool asCompanion = false, bool preservedPlayerFamilyIdentity = false, bool joinedWildernessParty = false, int joinedWildernessMembers = 0, int joinedWildernessPrisoners = 0)
	{
		try
		{
			if (hero == null || hero == Hero.MainHero)
			{
				return;
			}
			string heroKey = GetHeroRecordKey(hero);
			if (string.IsNullOrWhiteSpace(heroKey))
			{
				return;
			}
			string heroName = hero.Name?.ToString() ?? "该英雄";
			Settlement settlement = Settlement.CurrentSettlement ?? hero.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
			string locationText = settlement?.Name?.ToString() ?? "";
			bool preservedPlayerSpouseIdentity = preservedPlayerFamilyIdentity && (hero.Spouse == Hero.MainHero || Hero.MainHero?.Spouse == hero);
			string actionType = preservedPlayerFamilyIdentity ? "player_family_party_join" : (asCompanion ? "player_companion_join" : "player_clan_join");
			string stableKey = actionType + ":" + heroKey + ":" + GetCampaignDayIndex();
			string npcFact = preservedPlayerFamilyIdentity
				? (preservedPlayerSpouseIdentity ? "你仍是玩家的配偶，并已加入玩家队伍行动。" : "你仍是玩家家族成员，并已加入玩家队伍行动。")
				: (asCompanion ? "你成为了玩家的同伴，并随玩家队伍行动。" : "你加入了玩家家族，成为玩家家族成员，并随玩家队伍行动。");
			string playerFact = preservedPlayerFamilyIdentity
				? (preservedPlayerSpouseIdentity ? heroName + "仍是你的配偶，并已加入你的队伍。" : heroName + "仍是你的家族成员，并已加入你的队伍。")
				: (asCompanion ? "你招募了" + heroName + "成为同伴，并随你的队伍行动。" : "你招募了" + heroName + "加入玩家家族，并随你的队伍行动。");
			if (joinedWildernessParty)
			{
				string countText = BuildWildernessHeroPartyTransferCountText(joinedWildernessMembers, joinedWildernessPrisoners);
				if (string.IsNullOrEmpty(countText))
				{
					npcFact += " 你的原野外队伍已随你归并至玩家主队，没有额外成员或俘虏。";
					playerFact += " " + heroName + "的原野外队伍已随本人归并至你的主队，没有额外成员或俘虏。";
				}
				else
				{
					npcFact += " 你原野外队伍中的 " + countText + "已一并转入玩家主队。";
					playerFact += " " + heroName + "原野外队伍中的 " + countText + "已一并转入你的主队。";
				}
			}
			MyBehavior.RecordNpcActionForExternal(hero, npcFact, stableKey + ":npc", actionType, isMajor: true, isRecent: true, targetHero: Hero.MainHero, settlement: settlement, locationText: locationText, allowNonLordHero: true, won: true);
			MyBehavior.RecordPlayerActionForExternal(playerFact, stableKey + ":player", actionType, isMajor: true, targetHero: hero, settlement: settlement, locationText: locationText, won: true);
			Logger.Log("RewardSystemBehavior", "[HeroJoin] action_history_recorded reason=" + (reason ?? "") + " hero=" + heroKey);
		}
		catch (Exception ex)
		{
			Logger.Log("RewardSystemBehavior", "[HeroJoin] action_history_record_failed reason=" + (reason ?? "") + " hero=" + (hero?.StringId ?? "") + " error=" + ex.Message);
		}
	}

	public bool TryApplyHeroJoinPlayerPartyForExternal(Hero joiningHero, out string statusText)
	{
		return TryApplyHeroJoinPlayerPartyForExternal(joiningHero, false, out statusText);
	}

	public bool TryApplyHeroJoinPlayerPartyForExternal(Hero joiningHero, bool asCompanion, out string statusText)
	{
		bool joinedWildernessParty;
		int joinedWildernessMembers;
		int joinedWildernessPrisoners;
		return TryApplyHeroJoinPlayerPartyCore(joiningHero, asCompanion, out statusText, out joinedWildernessParty, out joinedWildernessMembers, out joinedWildernessPrisoners);
	}

	private bool TryApplyHeroJoinPlayerPartyCore(Hero joiningHero, bool asCompanion, out string statusText, out bool joinedWildernessParty, out int joinedWildernessMembers, out int joinedWildernessPrisoners)
	{
		statusText = "";
		joinedWildernessParty = false;
		joinedWildernessMembers = 0;
		joinedWildernessPrisoners = 0;
		try
		{
			if (joiningHero == null)
			{
				statusText = "执行失败：缺少要加入玩家家族的目标英雄。";
				return false;
			}
			if (joiningHero == Hero.MainHero)
			{
				statusText = "执行跳过：目标本来就是玩家本人。";
				return false;
			}
			if (Clan.PlayerClan == null || MobileParty.MainParty == null)
			{
				statusText = "执行失败：玩家家族或玩家队伍不可用。";
				return false;
			}
			bool preservePlayerFamilyIdentity = asCompanion && ShouldPreservePlayerFamilyIdentityForCompanionJoin(joiningHero);
			bool preservePlayerSpouseIdentity = preservePlayerFamilyIdentity && (joiningHero.Spouse == Hero.MainHero || Hero.MainHero?.Spouse == joiningHero);
			if (preservePlayerFamilyIdentity)
			{
				asCompanion = false;
				Logger.Log("RewardSystemBehavior", "[HeroJoin] companion_request_redirected_to_family hero=" + (joiningHero.StringId ?? "") + " spouse=" + preservePlayerSpouseIdentity);
			}
			if (asCompanion ? IsHeroPlayerCompanionInMainParty(joiningHero) : IsHeroPlayerClanLordInMainParty(joiningHero))
			{
				statusText = asCompanion
					? $"执行跳过：{joiningHero.Name} 已经是玩家同伴并在玩家队伍中。"
					: $"执行跳过：{joiningHero.Name} 已经是玩家家族成员并在玩家队伍中。";
				return false;
			}
			MobileParty originalMobileParty = joiningHero.PartyBelongedTo;
			PartyBase originalCaptivityParty = joiningHero.PartyBelongedToAsPrisoner;
			PartyBase originalParty = originalMobileParty?.Party ?? originalCaptivityParty;
			bool shouldCleanupOriginalMapParty = ShouldScheduleOriginalMapPartyCleanupAfterHeroJoin(originalMobileParty);
			TryResolveWildernessHeroJoinParty(joiningHero, out MobileParty wildernessSourceParty);
			string wildernessSourcePartyName = wildernessSourceParty?.Name?.ToString() ?? "";
			bool wildernessRosterTransferred = false;
			int movedWildernessMembers = 0;
			int movedWildernessPrisoners = 0;
			List<string> transitionNotes = new List<string>();
			Clan originalClan = GetHeroBackingClan(joiningHero) ?? joiningHero.Clan;
			Kingdom originalKingdom = originalClan?.Kingdom;
			bool originalClanWasRulingClan = originalKingdom != null && originalKingdom.RulingClan == originalClan;
			Settlement currentSettlement = joiningHero.CurrentSettlement;
			Settlement originalSettlement = ResolveOriginalSettlementForHeroJoin(joiningHero, currentSettlement);
			RememberHeroJoinOriginalClan(joiningHero, originalClan, originalSettlement, "hero_join_party");
			Town governorTown = joiningHero.GovernorOf;
			if (governorTown != null)
			{
				string governorTownName = governorTown.Name?.ToString() ?? "原定居点";
				ChangeGovernorAction.RemoveGovernorOf(joiningHero);
				transitionNotes.Add("已解除其在 " + governorTownName + " 的总督职位");
			}
			if (originalClan != null && originalClan != Clan.PlayerClan && originalClan.Leader == joiningHero && !originalClan.IsEliminated)
			{
				string originalClanName = GetClanDisplayNameForNotification(originalClan);
				Dictionary<Hero, int> heirApparents = originalClan.GetHeirApparents();
				if (heirApparents != null && heirApparents.Count > 0)
				{
					ChangeClanLeaderAction.ApplyWithoutSelectedNewLeader(originalClan);
					Hero newLeader = originalClan.Leader;
					if (originalClanWasRulingClan && newLeader != null && newLeader != joiningHero)
					{
						RepairRulingClanAfterLeaderRecruitmentWithElection(originalKingdom, originalClan, joiningHero, transitionNotes);
					}
					if (newLeader == null || newLeader == joiningHero)
					{
						statusText = "执行失败：" + originalClanName + " 的族长继承未完成，已阻止将现任族长直接拉入队伍。";
						return false;
					}
					transitionNotes.Add(originalClanName + " 已由 " + newLeader.Name + " 接任族长");
				}
				else
				{
					TransferWildernessHeroPartyRosterToMainParty(wildernessSourceParty, ref wildernessRosterTransferred, ref movedWildernessMembers, ref movedWildernessPrisoners);
					bool destroyOriginalKingdomAfterClanDestroyed = false;
					if (originalClanWasRulingClan)
					{
						PrepareRulingClanTransitionForDepartingClan(originalKingdom, originalClan, transitionNotes, out destroyOriginalKingdomAfterClanDestroyed);
					}
					DestroyClanAction.ApplyByClanLeaderDeath(originalClan);
					transitionNotes.Add(originalClanName + " 无可用继承人，已按原版族长死亡逻辑销毁原家族");
					FinalizeRulingClanTransitionForDepartedClan(originalKingdom, destroyOriginalKingdomAfterClanDestroyed, transitionNotes);
				}
			}
			if (!TryEndHeroCaptivityForPlayerJoin(joiningHero, originalCaptivityParty, asCompanion ? "hero_join_party_companion" : "hero_join_party_lord", transitionNotes, out string captivityStatus))
			{
				statusText = captivityStatus;
				return false;
			}
			if (currentSettlement != null && joiningHero.CurrentSettlement != null)
			{
				LeaveSettlementAction.ApplyForCharacterOnly(joiningHero);
			}
			TransferWildernessHeroPartyRosterToMainParty(wildernessSourceParty, ref wildernessRosterTransferred, ref movedWildernessMembers, ref movedWildernessPrisoners);
			bool moved = asCompanion
				? TryMoveHeroToPlayerClanAsCompanionAndMainParty(joiningHero, "hero_join_party_companion", out statusText)
				: TryMoveHeroToPlayerClanAsLordAndMainParty(joiningHero, "hero_join_party_lord", out statusText);
			if (!moved)
			{
				return false;
			}
			if (LocationComplex.Current != null)
			{
				LocationComplex.Current.RemoveCharacterIfExists(joiningHero);
			}
			PlayerEncounter.LocationEncounter?.RemoveAccompanyingCharacter(joiningHero);
			if (wildernessSourceParty != null)
			{
				joinedWildernessParty = true;
				joinedWildernessMembers = movedWildernessMembers;
				joinedWildernessPrisoners = movedWildernessPrisoners;
				transitionNotes.Add(BuildWildernessHeroPartyTransitionNote(wildernessSourcePartyName, movedWildernessMembers, movedWildernessPrisoners));
				Logger.Log("RewardSystemBehavior", "[HeroJoin] wilderness_party_join source=" + (wildernessSourceParty.StringId ?? "") + " members=" + movedWildernessMembers + " prisoners=" + movedWildernessPrisoners + " hero=" + (joiningHero.StringId ?? ""));
			}
			string transitionSummary = BuildRecruitmentTransitionSummary(transitionNotes);
			RecordHeroJoinedPlayerClanForExternal(joiningHero, preservePlayerFamilyIdentity ? "hero_join_party_family_preserved" : (asCompanion ? "hero_join_party_companion" : "hero_join_party_lord"), asCompanion, preservePlayerFamilyIdentity, joinedWildernessParty, joinedWildernessMembers, joinedWildernessPrisoners);
			statusText = preservePlayerFamilyIdentity
				? $"执行成功：{joiningHero.Name} 已保留{(preservePlayerSpouseIdentity ? "配偶" : "玩家家族成员")}身份，并加入玩家队伍{transitionSummary}。"
				: (asCompanion
					? $"执行成功：{joiningHero.Name} 已成为玩家同伴，并加入玩家队伍{transitionSummary}。"
					: $"执行成功：{joiningHero.Name} 已成为玩家家族成员，并加入玩家队伍{transitionSummary}。");
			if (shouldCleanupOriginalMapParty && IsEmptyMapPartyAfterHeroJoin(originalMobileParty))
			{
				CloseHeroJoinMapPartyConversationImmediately(joiningHero, originalParty, originalMobileParty);
			}
			else
			{
				ScheduleHeroJoinConversationClose(joiningHero, originalParty, originalMobileParty, shouldCleanupOriginalMapParty);
			}
			return true;
		}
		catch (Exception ex)
		{
			statusText = "执行失败（异常）：" + ex.Message;
			return false;
		}
	}

	public bool TryApplyNonHeroJoinPlayerPartyTagForExternal(CharacterObject joiningCharacter, int targetAgentIndex, ref string responseText, out List<string> generatedFacts, out List<string> notifications)
	{
		return TryApplyNonHeroJoinPlayerPartyTagCore(joiningCharacter, targetAgentIndex, "", "", false, null, int.MinValue, ref responseText, out generatedFacts, out notifications);
	}

	public bool TryApplyNonHeroJoinPlayerPartyTagForExternal(CharacterObject joiningCharacter, int targetAgentIndex, string promptGivenName, string promptDisplayName, ref string responseText, out List<string> generatedFacts, out List<string> notifications)
	{
		return TryApplyNonHeroJoinPlayerPartyTagCore(joiningCharacter, targetAgentIndex, promptGivenName, promptDisplayName, false, null, int.MinValue, ref responseText, out generatedFacts, out notifications);
	}

	public bool TryApplyNonHeroJoinPlayerPartyTagForNativeConversationExternal(CharacterObject joiningCharacter, int targetAgentIndex, string promptGivenName, string promptDisplayName, ConversationManager expectedConversationManager, int expectedConversationToken, ref string responseText, out List<string> generatedFacts, out List<string> notifications)
	{
		return TryApplyNonHeroJoinPlayerPartyTagCore(joiningCharacter, targetAgentIndex, promptGivenName, promptDisplayName, true, expectedConversationManager, expectedConversationToken, ref responseText, out generatedFacts, out notifications);
	}

	private bool TryApplyNonHeroJoinPlayerPartyTagCore(CharacterObject joiningCharacter, int targetAgentIndex, string promptGivenName, string promptDisplayName, bool requireCurrentConversationRequest, ConversationManager expectedConversationManager, int expectedConversationToken, ref string responseText, out List<string> generatedFacts, out List<string> notifications)
	{
		generatedFacts = new List<string>();
		notifications = new List<string>();
		if (joiningCharacter == null || string.IsNullOrEmpty(responseText))
		{
			return false;
		}
		Match joinTagMatch = HeroJoinPlayerPartyTagRegex.Match(responseText);
		if (!joinTagMatch.Success)
		{
			return false;
		}
		bool asCompanion = joinTagMatch.Groups[1].Value.StartsWith("C", StringComparison.OrdinalIgnoreCase);
		string latestReplyWithoutTag = HeroJoinPlayerPartyTagRegex.Replace(responseText, string.Empty).Trim();
		FreezeWatchdog.Mark("NonHeroJoin.tag_received", "target=" + (joiningCharacter.StringId ?? "") + " agent=" + targetAgentIndex + " native=" + requireCurrentConversationRequest, immediate: true);
		if (requireCurrentConversationRequest && !DoesNativeConversationRequestStillMatch(joiningCharacter, targetAgentIndex, expectedConversationManager, expectedConversationToken, out string staleReason))
		{
			responseText = latestReplyWithoutTag;
			notifications.Add("【加入队伍】已拦截过期动作：原版对话已先处理。");
			Logger.Log("RewardSystemBehavior", "[NonHeroJoin] stale native join tag intercepted reason=" + staleReason + " target=" + (joiningCharacter.StringId ?? "") + " agentIndex=" + targetAgentIndex + " expectedToken=" + expectedConversationToken);
			FreezeWatchdog.Mark("NonHeroJoin.stale_intercepted", "target=" + (joiningCharacter.StringId ?? "") + " agent=" + targetAgentIndex + " reason=" + staleReason, immediate: true);
			return true;
		}
		string statusText;
		Hero promotedHero;
		bool flag = TryApplyNonHeroJoinPlayerPartyForExternal(joiningCharacter, targetAgentIndex, promptGivenName, promptDisplayName, latestReplyWithoutTag, asCompanion, out statusText, out promotedHero);
		responseText = latestReplyWithoutTag;
		FreezeWatchdog.Mark("NonHeroJoin.result", "target=" + (joiningCharacter.StringId ?? "") + " agent=" + targetAgentIndex + " success=" + flag + " status=" + (statusText ?? ""), immediate: true);
		if (!string.IsNullOrWhiteSpace(statusText))
		{
			bool intercepted = !flag && statusText.StartsWith("执行拦截", StringComparison.Ordinal);
			bool promotedPlayerCompanion = flag && promotedHero != null && promotedHero.CompanionOf == Clan.PlayerClan && promotedHero.Occupation == Occupation.Wanderer;
			bool promotedPlayerClanLord = flag && promotedHero != null && promotedHero.Clan == Clan.PlayerClan && promotedHero.Occupation == Occupation.Lord;
			string successPrefix = promotedPlayerCompanion ? "【成为同伴】" : (promotedPlayerClanLord ? "【加入家族】" : "【加入队伍】");
			string text = (intercepted ? "【加入队伍】" : (flag ? successPrefix : "【加入队伍失败】")) + statusText;
			string factName = promotedHero?.Name?.ToString() ?? ResolveNonHeroFullDisplayName(joiningCharacter, promptDisplayName, promptGivenName, targetAgentIndex);
			if (!intercepted)
			{
				generatedFacts.Add("[AFEF NPC行为补充] " + (string.IsNullOrWhiteSpace(factName) ? "NPC" : factName) + ": " + statusText);
			}
			notifications.Add(text);
		}
		return true;
	}

	public bool TryApplyNonHeroJoinPlayerPartyForExternal(CharacterObject joiningCharacter, int targetAgentIndex, out string statusText)
	{
		Hero promotedHero;
		return TryApplyNonHeroJoinPlayerPartyForExternal(joiningCharacter, targetAgentIndex, "", "", "", out statusText, out promotedHero);
	}

	public bool TryApplyNonHeroJoinPlayerPartyForExternal(CharacterObject joiningCharacter, int targetAgentIndex, string promptGivenName, string promptDisplayName, string latestReply, out string statusText, out Hero promotedHero)
	{
		return TryApplyNonHeroJoinPlayerPartyForExternal(joiningCharacter, targetAgentIndex, promptGivenName, promptDisplayName, latestReply, false, out statusText, out promotedHero);
	}

	private bool TryApplyNonHeroJoinPlayerPartyForExternal(CharacterObject joiningCharacter, int targetAgentIndex, string promptGivenName, string promptDisplayName, string latestReply, bool asCompanion, out string statusText, out Hero promotedHero)
	{
		statusText = "";
		promotedHero = null;
		try
		{
			if (joiningCharacter == null)
			{
				statusText = "执行失败：缺少要加入队伍的非英雄NPC。";
				return false;
			}
			if (joiningCharacter.IsHero)
			{
				statusText = "执行失败：目标是英雄，应走英雄入队链路。";
				return false;
			}
			if (MobileParty.MainParty == null || Hero.MainHero == null || Clan.PlayerClan == null)
			{
				statusText = "执行失败：玩家队伍或玩家家族不可用。";
				return false;
			}
			bool wildernessJoinHandled;
			bool wildernessJoinApplied = TryApplyWildernessNonHeroPartyJoinPlayerPartyForExternal(joiningCharacter, targetAgentIndex, promptGivenName, promptDisplayName, out wildernessJoinHandled, out statusText);
			if (wildernessJoinHandled)
			{
				return wildernessJoinApplied;
			}
			if (TryGetPromotedNonHeroCompanion(targetAgentIndex, out var existingPromotedHero))
			{
				promotedHero = existingPromotedHero;
				string heroName = existingPromotedHero?.Name?.ToString() ?? ResolveNonHeroFullDisplayName(joiningCharacter, promptDisplayName, promptGivenName, targetAgentIndex);
				statusText = $"执行跳过：{heroName} 已经由当前场景 NPC 升格为玩家家族 Hero，不能重复招募生成新的 Hero。";
				return false;
			}
			TavernMercenaryPoolJoinResolution tavernPoolResolution = ResolveTavernMercenaryPoolJoin(joiningCharacter, targetAgentIndex, out var mercenaryData, out int count);
			if (tavernPoolResolution == TavernMercenaryPoolJoinResolution.Stale)
			{
				statusText = "执行拦截：酒馆雇佣兵池已被原版选项或其他流程处理，未重复执行入队。";
				Logger.Log("RewardSystemBehavior", "[NonHeroJoin] stale tavern pool join intercepted target=" + (joiningCharacter.StringId ?? "") + " agentIndex=" + targetAgentIndex);
				return false;
			}
			if (tavernPoolResolution == TavernMercenaryPoolJoinResolution.Ready)
			{
				count = Math.Max(1, count);
				CloseTavernMercenaryJoinConversationImmediately(joiningCharacter);
				mercenaryData.ChangeMercenaryCount(-count);
				MobileParty.MainParty.MemberRoster.AddToCounts(joiningCharacter, count, false, 0, 0, true, -1);
				CampaignEventDispatcher.Instance.OnUnitRecruited(joiningCharacter, count);
				RemoveJoinedNonHeroLocationCharacters(joiningCharacter, targetAgentIndex, removeAllMatchingTavernMercenaries: true);
				RemoveJoinedLiveAgent(targetAgentIndex);
				string name = joiningCharacter.Name?.ToString() ?? "该NPC";
				statusText = $"执行成功：酒馆中 {count} 名{name} 已全部作为普通士兵加入玩家队伍。";
				return true;
			}
			return TryPromoteNonHeroToCompanion(joiningCharacter, targetAgentIndex, promptGivenName, promptDisplayName, latestReply, asCompanion, out statusText, out promotedHero);
		}
		catch (Exception ex)
		{
			statusText = "执行失败（异常）：" + ex.Message;
			return false;
		}
	}

	private static bool TryResolveWildernessHeroJoinParty(Hero joiningHero, out MobileParty party)
	{
		party = null;
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			MobileParty sourceParty = joiningHero?.PartyBelongedTo;
			if (joiningHero == null || joiningHero == Hero.MainHero || joiningHero.CharacterObject == null
				|| joiningHero.IsPrisoner || joiningHero.PartyBelongedToAsPrisoner != null
				|| mainParty == null || sourceParty == null || sourceParty == mainParty
				|| sourceParty.Party == PartyBase.MainParty || !sourceParty.IsActive)
			{
				return false;
			}
			if (Settlement.CurrentSettlement != null || mainParty.CurrentSettlement != null
				|| sourceParty.CurrentSettlement != null || joiningHero.CurrentSettlement != null)
			{
				return false;
			}
			if (sourceParty.MemberRoster == null || !sourceParty.MemberRoster.Contains(joiningHero.CharacterObject))
			{
				return false;
			}
			party = sourceParty;
			return true;
		}
		catch
		{
			party = null;
			return false;
		}
	}

	// A hero can still be moved out of its party even when the optional wilderness-roster
	// transfer path is not applicable (for example, when the conversation context is not
	// classified as wilderness). Keep the original map party eligible for post-conversation
	// empty-party cleanup so an otherwise valid hero transfer cannot leave a 0-man party.
	private static bool ShouldScheduleOriginalMapPartyCleanupAfterHeroJoin(MobileParty party)
	{
		try
		{
			return party != null
				&& party != MobileParty.MainParty
				&& party.Party != PartyBase.MainParty
				&& party.IsActive
				&& party.CurrentSettlement == null
				&& party.MapEvent == null;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsEmptyMapPartyAfterHeroJoin(MobileParty party)
	{
		try
		{
			return party != null
				&& party.IsActive
				&& (party.MemberRoster?.TotalManCount ?? 0) <= 0
				&& (party.PrisonRoster?.TotalManCount ?? 0) <= 0;
		}
		catch
		{
			return false;
		}
	}

	// The delayed close context is intentionally runtime-only. If a player saves or exits
	// before it runs, repair only the precise orphan pattern created by a completed hero
	// join: an empty map party whose recorded leader is already in the player's main party.
	// This is a one-shot load repair, never a campaign-tick scan.
	private static void CleanupStalePlayerJoinedHeroMapPartiesAfterLoad()
	{
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			if (mainParty == null)
			{
				return;
			}
			List<MobileParty> parties = Campaign.Current?.MobileParties?.ToList();
			if (parties == null || parties.Count == 0)
			{
				return;
			}
			int staleCount = 0;
			int destroyedCount = 0;
			foreach (MobileParty party in parties)
			{
				if (!ShouldScheduleOriginalMapPartyCleanupAfterHeroJoin(party)
					|| (party.MemberRoster?.TotalManCount ?? 0) > 0
					|| (party.PrisonRoster?.TotalManCount ?? 0) > 0)
				{
					continue;
				}
				Hero formerLeader = party.LeaderHero;
				if (formerLeader == null || !IsHeroInParty(formerLeader, mainParty))
				{
					continue;
				}
				staleCount++;
				TryDestroyEmptyWildernessNonHeroJoinParty(party);
				if (!party.IsActive)
				{
					destroyedCount++;
				}
			}
			if (staleCount > 0)
			{
				Logger.Log("RewardSystemBehavior", "[HeroJoin] load_orphan_party_cleanup candidates=" + staleCount + " destroyed=" + destroyedCount);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("RewardSystemBehavior", "[HeroJoin] load_orphan_party_cleanup_failed error=" + ex.Message);
		}
	}

	private static void TransferWildernessHeroPartyRosterToMainParty(MobileParty sourceParty, ref bool transferred, ref int movedMembers, ref int movedPrisoners)
	{
		if (sourceParty == null || transferred)
		{
			return;
		}
		transferred = true;
		movedMembers = MoveWildernessNonHeroPartyMembersToMainParty(sourceParty);
		movedPrisoners = MoveWildernessNonHeroPartyPrisonersToMainParty(sourceParty);
		Logger.Log("RewardSystemBehavior", "[HeroJoin] wilderness_party_roster_transferred source=" + (sourceParty.StringId ?? "") + " members=" + movedMembers + " prisoners=" + movedPrisoners);
	}

	private static bool TryApplyWildernessNonHeroPartyJoinPlayerPartyForExternal(CharacterObject joiningCharacter, int targetAgentIndex, string promptGivenName, string promptDisplayName, out bool handled, out string statusText)
	{
		handled = false;
		statusText = "";
		if (!TryResolveWildernessNonHeroJoinParty(joiningCharacter, targetAgentIndex, out var sourcePartyBase))
		{
			return false;
		}
		handled = true;
		MobileParty sourceParty = sourcePartyBase.MobileParty;
		MobileParty mainParty = MobileParty.MainParty;
		if (sourceParty == null || mainParty == null || sourceParty == mainParty || sourceParty.Party == PartyBase.MainParty)
		{
			statusText = "执行失败：野外非英雄队伍目标不可用。";
			return false;
		}
		int beforeMembers = Math.Max(0, sourceParty.MemberRoster?.TotalManCount ?? 0);
		int beforePrisoners = Math.Max(0, sourceParty.Party?.PrisonRoster?.TotalManCount ?? 0);
		int movedMembers = MoveWildernessNonHeroPartyMembersToMainParty(sourceParty);
		int movedPrisoners = MoveWildernessNonHeroPartyPrisonersToMainParty(sourceParty);
		if (movedMembers <= 0 && movedPrisoners <= 0)
		{
			statusText = "执行失败：未能从野外非英雄队伍转入任何成员或俘虏。";
			return false;
		}
		string targetName = ResolveNonHeroFullDisplayName(joiningCharacter, promptDisplayName, promptGivenName, targetAgentIndex);
		string partyName = sourceParty.Name?.ToString();
		if (string.IsNullOrWhiteSpace(partyName))
		{
			partyName = string.IsNullOrWhiteSpace(targetName) ? "对方队伍" : (targetName + "所在队伍");
		}
		string prisonerText = movedPrisoners > 0 ? $"，并移交 {movedPrisoners} 名俘虏" : "";
		string beforeText = beforeMembers > movedMembers || beforePrisoners > movedPrisoners ? $"（原成员 {beforeMembers}，原俘虏 {beforePrisoners}）" : "";
		statusText = $"执行成功：{partyName} 已同意加入玩家队伍，已转入 {movedMembers} 名成员{prisonerText}{beforeText}。";
		Logger.Log("RewardSystemBehavior", "[NonHeroJoin] wilderness_party_join source=" + (sourceParty.StringId ?? "") + " members=" + movedMembers + " prisoners=" + movedPrisoners + " target=" + (joiningCharacter?.StringId ?? "") + " agentIndex=" + targetAgentIndex);
		CloseWildernessNonHeroJoinConversationImmediately(sourcePartyBase, sourceParty, joiningCharacter, targetAgentIndex);
		return true;
	}

	private static bool TryResolveWildernessNonHeroJoinParty(CharacterObject joiningCharacter, int targetAgentIndex, out PartyBase party)
	{
		party = null;
		try
		{
			if (joiningCharacter == null || joiningCharacter.HeroObject != null)
			{
				return false;
			}
			if (Settlement.CurrentSettlement != null || MobileParty.MainParty?.CurrentSettlement != null)
			{
				return false;
			}
			PartyBase resolved = MyBehavior.ResolvePartyTransferCounterpartyForExternal(null, joiningCharacter, targetAgentIndex);
			if (resolved == null || resolved == PartyBase.MainParty || resolved.MobileParty == null || resolved.MobileParty == MobileParty.MainParty)
			{
				return false;
			}
			bool matchesTarget = DoesWildernessNonHeroJoinPartyRepresentTarget(resolved, joiningCharacter);
			if (!matchesTarget && targetAgentIndex >= 0)
			{
				try
				{
					Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex);
					PartyBase agentParty = agent?.Origin?.BattleCombatant as PartyBase;
					matchesTarget = agentParty != null && agentParty == resolved;
				}
				catch
				{
					matchesTarget = false;
				}
			}
			if (!matchesTarget)
			{
				return false;
			}
			party = resolved;
			return true;
		}
		catch
		{
			party = null;
			return false;
		}
	}

	private static bool DoesWildernessNonHeroJoinPartyRepresentTarget(PartyBase party, CharacterObject joiningCharacter)
	{
		if (party == null || joiningCharacter == null)
		{
			return false;
		}
		try
		{
			if (party.MemberRoster != null && party.MemberRoster.Contains(joiningCharacter))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			CharacterObject conversationLeader = TaleWorlds.CampaignSystem.Conversation.ConversationHelper.GetConversationCharacterPartyLeader(party);
			if (conversationLeader == joiningCharacter)
			{
				return true;
			}
			string targetId = joiningCharacter.StringId;
			return !string.IsNullOrEmpty(targetId)
				&& string.Equals(conversationLeader?.StringId, targetId, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	private static int MoveWildernessNonHeroPartyMembersToMainParty(MobileParty sourceParty)
	{
		int moved = 0;
		MobileParty targetParty = MobileParty.MainParty;
		if (sourceParty?.MemberRoster == null || targetParty?.MemberRoster == null)
		{
			return 0;
		}
		for (int i = sourceParty.MemberRoster.Count - 1; i >= 0; i--)
		{
			TroopRosterElement element = sourceParty.MemberRoster.GetElementCopyAtIndex(i);
			CharacterObject character = element.Character;
			int count = Math.Max(0, element.Number);
			if (character == null || count <= 0)
			{
				continue;
			}
			if (character.IsHero)
			{
				continue;
			}
			int wounded = Math.Max(0, element.WoundedNumber);
			int xp = Math.Max(0, element.Xp);
			sourceParty.MemberRoster.AddToCounts(character, -count, insertAtFront: false, woundedCount: -wounded, xpChange: 0, removeDepleted: true, index: -1);
			if (xp > 0)
			{
				sourceParty.MemberRoster.AddXpToTroop(character, -xp);
			}
			targetParty.MemberRoster.AddToCounts(character, count, insertAtFront: false, woundedCount: wounded, xpChange: 0, removeDepleted: true, index: -1);
			if (xp > 0)
			{
				targetParty.MemberRoster.AddXpToTroop(character, xp);
			}
			try
			{
				CampaignEventDispatcher.Instance.OnUnitRecruited(character, count);
			}
			catch
			{
			}
			moved += count;
		}
		return moved;
	}

	private static int MoveWildernessNonHeroPartyPrisonersToMainParty(MobileParty sourceParty)
	{
		int moved = 0;
		MobileParty targetParty = MobileParty.MainParty;
		if (sourceParty?.Party?.PrisonRoster == null || targetParty?.Party == null)
		{
			return 0;
		}
		for (int i = sourceParty.Party.PrisonRoster.Count - 1; i >= 0; i--)
		{
			TroopRosterElement element = sourceParty.Party.PrisonRoster.GetElementCopyAtIndex(i);
			CharacterObject character = element.Character;
			int count = Math.Max(0, element.Number);
			if (character == null || count <= 0)
			{
				continue;
			}
			if (character.IsHero)
			{
				try
				{
					TransferPrisonerAction.Apply(character, sourceParty.Party, targetParty.Party);
					moved += 1;
				}
				catch (Exception ex)
				{
					Logger.Log("RewardSystemBehavior", "[NonHeroJoin] move prisoner hero failed hero=" + (character.HeroObject?.StringId ?? "") + " error=" + ex.Message);
				}
				continue;
			}
			int xp = Math.Max(0, element.Xp);
			sourceParty.Party.PrisonRoster.AddToCounts(character, -count, insertAtFront: false, woundedCount: 0, xpChange: 0, removeDepleted: true, index: -1);
			if (xp > 0)
			{
				sourceParty.Party.PrisonRoster.AddXpToTroop(character, -xp);
			}
			targetParty.Party.AddPrisoner(character, count);
			if (xp > 0)
			{
				targetParty.Party.PrisonRoster?.AddXpToTroop(character, xp);
			}
			moved += count;
		}
		return moved;
	}

	private static void TryDestroyEmptyWildernessNonHeroJoinParty(MobileParty party)
	{
		try
		{
			if (party == null || party == MobileParty.MainParty || !party.IsActive)
			{
				return;
			}
			int members = Math.Max(0, party.MemberRoster?.TotalManCount ?? 0);
			int prisoners = Math.Max(0, party.Party?.PrisonRoster?.TotalManCount ?? 0);
			if (members <= 0 && prisoners <= 0)
			{
				DestroyPartyAction.Apply(null, party);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("RewardSystemBehavior", "[NonHeroJoin] destroy empty wilderness party failed: " + ex.Message);
		}
	}

	private static void CloseWildernessNonHeroJoinConversationImmediately(PartyBase sourcePartyBase, MobileParty sourceParty, CharacterObject joiningCharacter, int targetAgentIndex)
	{
		if (sourcePartyBase == null && sourceParty == null)
		{
			return;
		}
		WildernessNonHeroJoinConversationCloseContext context = new WildernessNonHeroJoinConversationCloseContext
		{
			SourcePartyBase = sourcePartyBase,
			SourceParty = sourceParty,
			TargetCharacter = joiningCharacter,
			TargetAgentIndex = targetAgentIndex,
			SourcePartyId = sourceParty?.StringId ?? "",
			TargetCharacterId = joiningCharacter?.StringId ?? ""
		};
		ConversationExceptionGuard.MarkCurrentConversationStale("wilderness_nonhero_join_party_immediate_close");
		ExecuteWildernessNonHeroJoinConversationClose(context);
	}

	private static void CloseTavernMercenaryJoinConversationImmediately(CharacterObject joiningCharacter)
	{
		if (!DoesCurrentConversationTargetMatch(joiningCharacter))
		{
			return;
		}
		ConversationExceptionGuard.MarkCurrentConversationStale("tavern_mercenary_join_party_immediate_close");
		TryEndCurrentConversationForJoinAction("tavern_mercenary_join_party_immediate_close");
		Logger.Log("RewardSystemBehavior", "[NonHeroJoin] tavern mercenary conversation closed before pool mutation target=" + (joiningCharacter?.StringId ?? ""));
	}

	private static void ScheduleHeroJoinConversationClose(Hero joinedHero, PartyBase originalParty, MobileParty originalMobileParty, bool destroyOriginalPartyIfEmpty)
	{
		if (joinedHero == null)
		{
			return;
		}
		PendingHeroJoinConversationClose pending = new PendingHeroJoinConversationClose
		{
			JoinedHero = joinedHero,
			TargetCharacter = joinedHero.CharacterObject,
			OriginalParty = originalParty,
			OriginalMobileParty = originalMobileParty,
			JoinedHeroId = joinedHero.StringId ?? "",
			TargetCharacterId = joinedHero.CharacterObject?.StringId ?? "",
			OriginalPartyId = originalMobileParty?.StringId ?? "",
			DestroyOriginalPartyIfEmpty = destroyOriginalPartyIfEmpty,
			CreatedUtcTicks = DateTime.UtcNow.Ticks
		};
		lock (HeroJoinConversationCloseLock)
		{
			_pendingHeroJoinConversationClose = pending;
			Volatile.Write(ref _hasPendingHeroJoinConversationClose, 1);
		}
		ConversationExceptionGuard.MarkCurrentConversationStale("hero_join_party_scheduled_close");
		Logger.Log("RewardSystemBehavior", "[HeroJoin] scheduled delayed conversation close hero=" + pending.JoinedHeroId + " originalParty=" + pending.OriginalPartyId);
	}

	private static void CloseHeroJoinMapPartyConversationImmediately(Hero joinedHero, PartyBase originalParty, MobileParty originalMobileParty)
	{
		if (joinedHero == null)
		{
			return;
		}
		PendingHeroJoinConversationClose pending = new PendingHeroJoinConversationClose
		{
			JoinedHero = joinedHero,
			TargetCharacter = joinedHero.CharacterObject,
			OriginalParty = originalParty,
			OriginalMobileParty = originalMobileParty,
			JoinedHeroId = joinedHero.StringId ?? "",
			TargetCharacterId = joinedHero.CharacterObject?.StringId ?? "",
			OriginalPartyId = originalMobileParty?.StringId ?? "",
			DestroyOriginalPartyIfEmpty = true,
			CreatedUtcTicks = DateTime.UtcNow.Ticks
		};
		ConversationExceptionGuard.MarkCurrentConversationStale("hero_join_party_immediate_map_party_cleanup");
		ExecutePendingHeroJoinConversationClose(pending);
		Logger.Log("RewardSystemBehavior", "[HeroJoin] immediate map-party cleanup requested hero=" + pending.JoinedHeroId + " originalParty=" + pending.OriginalPartyId);
	}

	private static void TryClosePendingHeroJoinConversation()
	{
		if (Volatile.Read(ref _hasPendingHeroJoinConversationClose) == 0)
		{
			return;
		}
		PendingHeroJoinConversationClose pending = null;
		try
		{
			lock (HeroJoinConversationCloseLock)
			{
				if (_pendingHeroJoinConversationClose == null)
				{
					return;
				}
				long elapsedTicks = DateTime.UtcNow.Ticks - _pendingHeroJoinConversationClose.CreatedUtcTicks;
				if (elapsedTicks < (long)(JoinPartyConversationCloseDelaySeconds * TimeSpan.TicksPerSecond))
				{
					return;
				}
				pending = _pendingHeroJoinConversationClose;
				_pendingHeroJoinConversationClose = null;
				Volatile.Write(ref _hasPendingHeroJoinConversationClose, 0);
			}
			ExecutePendingHeroJoinConversationClose(pending);
		}
		catch (Exception ex)
		{
			Logger.Log("RewardSystemBehavior", "[HeroJoin] delayed close failed: " + ex.Message);
		}
	}

	private static void ExecuteWildernessNonHeroJoinConversationClose(WildernessNonHeroJoinConversationCloseContext context)
	{
		if (context == null)
		{
			return;
		}
		bool encounterMatches = DoesWildernessNonHeroJoinEncounterMatch(context);
		bool conversationMatches = DoesWildernessNonHeroJoinConversationMatch(context);
		if (encounterMatches)
		{
			PlayerEncounter.LeaveEncounter = true;
		}
		if (conversationMatches)
		{
			TryEndCurrentConversationForJoinAction("wilderness_nonhero_join_party_immediate_close");
		}
		TryDestroyEmptyWildernessNonHeroJoinParty(context.SourceParty);
		Logger.Log("RewardSystemBehavior", "[NonHeroJoin] immediate close applied conversation=" + conversationMatches + " encounter=" + encounterMatches + " source=" + (context.SourcePartyId ?? "") + " target=" + (context.TargetCharacterId ?? ""));
	}

	private static void ExecutePendingHeroJoinConversationClose(PendingHeroJoinConversationClose pending)
	{
		if (pending == null)
		{
			return;
		}
		bool conversationMatches = DoesPendingHeroJoinConversationMatch(pending);
		bool encounterMatches = DoesPendingHeroJoinEncounterMatch(pending);
		if (encounterMatches)
		{
			PlayerEncounter.LeaveEncounter = true;
		}
		if (conversationMatches)
		{
			TryEndCurrentConversationForJoinAction("hero_join_party_delayed_close");
		}
		if (pending.DestroyOriginalPartyIfEmpty)
		{
			TryDestroyEmptyWildernessNonHeroJoinParty(pending.OriginalMobileParty);
		}
		Logger.Log("RewardSystemBehavior", "[HeroJoin] close applied conversation=" + conversationMatches + " encounter=" + encounterMatches + " hero=" + (pending.JoinedHeroId ?? "") + " originalParty=" + (pending.OriginalPartyId ?? ""));
	}

	private static bool DoesWildernessNonHeroJoinConversationMatch(WildernessNonHeroJoinConversationCloseContext context)
	{
		if (context == null)
		{
			return false;
		}
		CharacterObject currentCharacter = Campaign.Current?.ConversationManager?.OneToOneConversationCharacter ?? CharacterObject.OneToOneConversationCharacter;
		if (currentCharacter == null)
		{
			return false;
		}
		if (context.TargetCharacter != null && currentCharacter == context.TargetCharacter)
		{
			return true;
		}
		return !string.IsNullOrEmpty(context.TargetCharacterId) && string.Equals(currentCharacter.StringId, context.TargetCharacterId, StringComparison.OrdinalIgnoreCase);
	}

	private static bool DoesCurrentConversationTargetMatch(CharacterObject targetCharacter)
	{
		if (targetCharacter == null)
		{
			return false;
		}
		CharacterObject currentCharacter = Campaign.Current?.ConversationManager?.OneToOneConversationCharacter ?? CharacterObject.OneToOneConversationCharacter;
		return currentCharacter == targetCharacter
			|| (!string.IsNullOrEmpty(targetCharacter.StringId) && string.Equals(currentCharacter?.StringId, targetCharacter.StringId, StringComparison.OrdinalIgnoreCase));
	}

	private static bool DoesNativeConversationRequestStillMatch(CharacterObject targetCharacter, int targetAgentIndex, ConversationManager expectedConversationManager, int expectedConversationToken, out string reason)
	{
		reason = "";
		try
		{
			ConversationManager currentManager = Campaign.Current?.ConversationManager;
			if (expectedConversationManager == null || currentManager == null || !ReferenceEquals(currentManager, expectedConversationManager))
			{
				reason = "conversation_manager_changed";
				return false;
			}
			if (!currentManager.IsConversationInProgress)
			{
				reason = "conversation_ended";
				return false;
			}
			if (expectedConversationToken != int.MinValue && currentManager.ActiveToken != expectedConversationToken)
			{
				reason = "conversation_token_changed:" + currentManager.ActiveToken;
				return false;
			}
			if (!DoesCurrentConversationTargetMatch(targetCharacter))
			{
				reason = "conversation_target_changed";
				return false;
			}
			Agent currentConversationAgent = currentManager.OneToOneConversationAgent as Agent;
			if (targetAgentIndex >= 0 && currentConversationAgent != null && currentConversationAgent.Index != targetAgentIndex)
			{
				reason = "conversation_agent_changed:" + currentConversationAgent.Index;
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = "validation_exception:" + ex.GetType().Name;
			return false;
		}
	}

	private static bool DoesWildernessNonHeroJoinEncounterMatch(WildernessNonHeroJoinConversationCloseContext context)
	{
		if (context == null || PlayerEncounter.Current == null)
		{
			return false;
		}
		PartyBase encountered = PlayerEncounterCompat.GetEncounteredPartySafe() ?? PlayerEncounter.EncounteredParty;
		if (encountered == null)
		{
			return false;
		}
		if (context.SourcePartyBase != null && encountered == context.SourcePartyBase)
		{
			return true;
		}
		if (context.SourceParty != null && encountered.MobileParty == context.SourceParty)
		{
			return true;
		}
		return !string.IsNullOrEmpty(context.SourcePartyId) && string.Equals(encountered.MobileParty?.StringId, context.SourcePartyId, StringComparison.OrdinalIgnoreCase);
	}

	private static bool DoesPendingHeroJoinConversationMatch(PendingHeroJoinConversationClose pending)
	{
		if (pending == null)
		{
			return false;
		}
		CharacterObject currentCharacter = Campaign.Current?.ConversationManager?.OneToOneConversationCharacter ?? CharacterObject.OneToOneConversationCharacter;
		if (currentCharacter == null)
		{
			return false;
		}
		Hero currentHero = currentCharacter.HeroObject;
		if (pending.JoinedHero != null && currentHero == pending.JoinedHero)
		{
			return true;
		}
		if (!string.IsNullOrEmpty(pending.JoinedHeroId) && string.Equals(currentHero?.StringId, pending.JoinedHeroId, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (pending.TargetCharacter != null && currentCharacter == pending.TargetCharacter)
		{
			return true;
		}
		return !string.IsNullOrEmpty(pending.TargetCharacterId) && string.Equals(currentCharacter.StringId, pending.TargetCharacterId, StringComparison.OrdinalIgnoreCase);
	}

	private static bool DoesPendingHeroJoinEncounterMatch(PendingHeroJoinConversationClose pending)
	{
		if (pending == null || PlayerEncounter.Current == null)
		{
			return false;
		}
		PartyBase encountered = PlayerEncounterCompat.GetEncounteredPartySafe() ?? PlayerEncounter.EncounteredParty;
		if (encountered == null)
		{
			return false;
		}
		if (pending.OriginalParty != null && encountered == pending.OriginalParty)
		{
			return true;
		}
		if (pending.OriginalMobileParty != null && encountered.MobileParty == pending.OriginalMobileParty)
		{
			return true;
		}
		if (!string.IsNullOrEmpty(pending.OriginalPartyId) && string.Equals(encountered.MobileParty?.StringId, pending.OriginalPartyId, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		Hero leaderHero = encountered.LeaderHero;
		if (pending.JoinedHero != null && leaderHero == pending.JoinedHero)
		{
			return true;
		}
		return !string.IsNullOrEmpty(pending.JoinedHeroId) && string.Equals(leaderHero?.StringId, pending.JoinedHeroId, StringComparison.OrdinalIgnoreCase);
	}

	private static void TryEndCurrentConversationForJoinAction(string staleReason)
	{
		try
		{
			if ((Campaign.Current?.ConversationManager?.OneToOneConversationCharacter ?? CharacterObject.OneToOneConversationCharacter) == null)
			{
				return;
			}
			Campaign.Current?.ConversationManager?.EndConversation();
		}
		catch (Exception ex)
		{
			Logger.Log("RewardSystemBehavior", "[JoinParty] EndConversation failed: " + ex.Message);
			try
			{
				ConversationExceptionGuard.MarkCurrentConversationStale(string.IsNullOrWhiteSpace(staleReason) ? "join_party_delayed_close" : staleReason);
			}
			catch
			{
			}
		}
	}

	private static TavernMercenaryPoolJoinResolution ResolveTavernMercenaryPoolJoin(CharacterObject joiningCharacter, int targetAgentIndex, out RecruitmentCampaignBehavior.TownMercenaryData mercenaryData, out int count)
	{
		mercenaryData = null;
		count = 0;
		if (joiningCharacter == null || !IsTavernMercenaryLike(joiningCharacter) || !IsCurrentJoinTargetInTavern(joiningCharacter, targetAgentIndex))
		{
			return TavernMercenaryPoolJoinResolution.NotPoolTarget;
		}
		Settlement settlement = Settlement.CurrentSettlement ?? PlayerEncounter.EncounterSettlement ?? MobileParty.MainParty?.CurrentSettlement;
		if (settlement == null || !settlement.IsTown)
		{
			return TavernMercenaryPoolJoinResolution.NotPoolTarget;
		}
		try
		{
			RecruitmentCampaignBehavior recruitmentCampaignBehavior = Campaign.Current?.GetCampaignBehavior<RecruitmentCampaignBehavior>();
			mercenaryData = recruitmentCampaignBehavior?.GetMercenaryData(settlement.Town);
			if (mercenaryData == null || mercenaryData.TroopType != joiningCharacter)
			{
				mercenaryData = null;
				return TavernMercenaryPoolJoinResolution.NotPoolTarget;
			}
			count = mercenaryData.Number;
			return count > 0 ? TavernMercenaryPoolJoinResolution.Ready : TavernMercenaryPoolJoinResolution.Stale;
		}
		catch
		{
			mercenaryData = null;
			count = 0;
			return TavernMercenaryPoolJoinResolution.Stale;
		}
	}

	private static bool TryPromoteNonHeroToCompanion(CharacterObject joiningCharacter, int targetAgentIndex, string promptGivenName, string promptDisplayName, string latestReply, bool asCompanion, out string statusText, out Hero promotedHero)
	{
		statusText = "";
		promotedHero = null;
		Agent agent = ResolveAgentForIndex(targetAgentIndex);
		if (agent == null || !agent.IsActive())
		{
			statusText = "执行失败：找不到当前说话 NPC 的 live Agent，无法复制外观与装备，已取消升格。";
			return false;
		}
		CharacterObject template = agent.Character as CharacterObject;
		if (template == null)
		{
			template = joiningCharacter;
		}
		if (template == null || template.IsHero)
		{
			statusText = "执行失败：当前 Agent 不是可升格的非英雄兵种模板。";
			return false;
		}
		if (!TryCaptureAgentBodyProperties(agent, out var bodyProperties, out var bodyError))
		{
			statusText = "执行失败：复制当前 NPC 外观失败（" + bodyError + "），已取消升格。";
			return false;
		}
		if (!TryCaptureAgentEquipment(agent, out var capturedEquipment, out var equipmentError))
		{
			statusText = "执行失败：复制当前 NPC 装备失败（" + equipmentError + "），已取消升格。";
			return false;
		}
		string originalFullName = ResolveNonHeroFullDisplayName(template, promptDisplayName, promptGivenName, targetAgentIndex);
		string originalTroopName = template.Name?.ToString() ?? joiningCharacter.Name?.ToString() ?? "非英雄NPC";
		string personalName = ResolveNonHeroPersonalName(promptGivenName, originalFullName, originalTroopName);
		if (string.IsNullOrWhiteSpace(personalName))
		{
			statusText = "执行失败：无法确定升格 Hero 的个人名，已取消升格。";
			return false;
		}
		PlayerOwnedTroopPromotionReservation reservedPlayerTroop;
		if (!TryReservePlayerOwnedTroopForPromotion(agent, template, out reservedPlayerTroop, out var reserveError))
		{
			statusText = reserveError;
			return false;
		}
		bool promotionCompleted = false;
		try
		{
			Settlement bornSettlement = Settlement.CurrentSettlement ?? PlayerEncounter.EncounterSettlement ?? MobileParty.MainParty?.CurrentSettlement;
			int age = ResolvePromotedHeroAge(bodyProperties, template);
			Hero hero = HeroCreator.CreateSpecialHero(template, bornSettlement, null, null, age);
			TextObject heroName = new TextObject(personalName);
			hero.SetName(heroName, heroName);
			hero.StaticBodyProperties = bodyProperties.StaticProperties;
			hero.Weight = ClampBodyShape01(bodyProperties.DynamicProperties.Weight);
			hero.Build = ClampBodyShape01(bodyProperties.DynamicProperties.Build);
			TryActivatePromotedCompanionHero(hero, "new_nonhero_promotion");
			ApplyTemplateSkillsToHero(hero, template);
			ApplyPromotedCompanionRandomTraits(hero, template);
			CopyCapturedEquipmentToHero(hero, capturedEquipment);
			bool moved = asCompanion
				? TryMoveHeroToPlayerClanAsCompanionAndMainParty(hero, "nonhero_join_party_companion_promotion", out statusText)
				: TryMoveHeroToPlayerClanAsLordAndMainParty(hero, "nonhero_join_party_lord_promotion", out statusText);
			if (!moved)
			{
				return false;
			}
			promotionCompleted = true;
			promotedHero = hero;
			RememberPromotedNonHeroCompanion(targetAgentIndex, hero);
			LogPromotedCompanionGovernorEligibility(hero, "after_nonhero_promotion");
			bool sceneFollowStarted = ShoutBehavior.TryForceSceneFollowPlayerForExternal(targetAgentIndex, transient: true, reason: "nonhero_join_party_promotion");
			RemoveJoinedNonHeroLocationCharacters(template, targetAgentIndex, removeAllMatchingTavernMercenaries: false);
			string cultureName = template.Culture?.Name?.ToString() ?? template.Culture?.StringId ?? "";
			string sceneLabel = BuildCurrentSceneLabelForPrompt();
			List<string> dialogueHistory = ShoutBehavior.GetAuxiliarySceneDialogueHistoryLinesForExternal(targetAgentIndex, 40) ?? new List<string>();
			string cleanLatestReply = StripNonHeroJoinTag(latestReply);
			if (!string.IsNullOrWhiteSpace(cleanLatestReply))
			{
				dialogueHistory.Add((string.IsNullOrWhiteSpace(originalFullName) ? "NPC" : originalFullName) + ": " + cleanLatestReply);
			}
			string joinFact = asCompanion
				? $"{hero.Name} 原为{originalTroopName}，在 {sceneLabel} 同意追随玩家，成为玩家同伴并加入玩家队伍。"
				: $"{hero.Name} 原为{originalTroopName}，在 {sceneLabel} 同意追随玩家，成为玩家家族成员并加入玩家队伍。";
			MyBehavior.AppendExternalDialogueHistory(hero, null, null, "[AFEF NPC行为补充] " + joinFact);
			RecordHeroJoinedPlayerClanForExternal(hero, asCompanion ? "nonhero_join_party_companion_promotion" : "nonhero_join_party_lord_promotion", asCompanion);
			AppendPromotedHeroPriorHistory(hero, dialogueHistory);
			string equipmentSummary = BuildEquipmentSummaryForPrompt(capturedEquipment);
			_ = MyBehavior.GeneratePromotedNonHeroCompanionProfileForExternalAsync(hero, personalName, originalFullName, originalTroopName, template.StringId ?? "", cultureName, sceneLabel, joinFact, BuildDialogueHistoryForPrompt(dialogueHistory), equipmentSummary);
			statusText = asCompanion
				? $"执行成功：{originalFullName} 已升格为玩家同伴“{hero.Name}”，并加入玩家队伍{(sceneFollowStarted ? "，当前场景中已开始跟随玩家" : "")}。"
				: $"执行成功：{originalFullName} 已升格为玩家家族 Hero“{hero.Name}”，并加入玩家队伍{(sceneFollowStarted ? "，当前场景中已开始跟随玩家" : "")}。";
			return true;
		}
		finally
		{
			if (!promotionCompleted && reservedPlayerTroop != null)
			{
				RestoreReservedPlayerOwnedTroopAfterFailedPromotion(reservedPlayerTroop);
			}
		}
	}

	private sealed class PlayerOwnedTroopPromotionReservation
	{
		public CharacterObject Troop;

		public bool IsPrisoner;

		public bool WasWounded;
	}

	private static bool TryReservePlayerOwnedTroopForPromotion(Agent agent, CharacterObject troop, out PlayerOwnedTroopPromotionReservation reservedTroop, out string statusText)
	{
		reservedTroop = null;
		statusText = "";
		bool isPrisoner = agent?.Origin is PrisonerAgentOrigin;
		PartyBase sourceParty = agent?.Origin?.BattleCombatant as PartyBase;
		if (!isPrisoner && sourceParty != PartyBase.MainParty && sourceParty?.MobileParty != MobileParty.MainParty)
		{
			return true;
		}
		TroopRoster roster = isPrisoner ? PartyBase.MainParty?.PrisonRoster : MobileParty.MainParty?.MemberRoster;
		if (troop == null || roster == null)
		{
			statusText = isPrisoner
				? "执行失败：玩家队伍俘虏名册不可用，无法消耗被升格的原俘虏。"
				: "执行失败：玩家队伍花名册不可用，无法消耗被升格的原士兵。";
			return false;
		}
		int index = roster.FindIndexOfTroop(troop);
		int total = index >= 0 ? roster.GetElementNumber(index) : 0;
		int wounded = index >= 0 ? roster.GetElementWoundedNumber(index) : 0;
		int healthy = Math.Max(0, total - wounded);
		if (total <= 0)
		{
			statusText = isPrisoner
				? $"执行失败：玩家队伍俘虏名册中不存在可被升格的{troop.Name}，已取消生成 Hero。"
				: $"执行失败：玩家队伍中不存在可被升格的{troop.Name}，已取消生成 Hero。";
			return false;
		}
		if (!isPrisoner && healthy <= 0)
		{
			statusText = $"执行失败：玩家队伍中没有可被升格的健康{troop.Name}，已取消生成 Hero。";
			return false;
		}
		bool consumeWounded = isPrisoner && healthy <= 0 && wounded > 0;
		roster.AddToCounts(troop, -1, insertAtFront: false, woundedCount: consumeWounded ? -1 : 0, xpChange: 0, removeDepleted: true, index: -1);
		int remaining = roster.GetTroopCount(troop);
		int remainingIndex = roster.FindIndexOfTroop(troop);
		int remainingWounded = remainingIndex >= 0 ? roster.GetElementWoundedNumber(remainingIndex) : 0;
		int expectedWounded = wounded - (consumeWounded ? 1 : 0);
		if (remaining != total - 1 || remainingWounded != expectedWounded)
		{
			int countDelta = total - remaining;
			int woundedDelta = wounded - remainingWounded;
			if (countDelta != 0 || woundedDelta != 0)
			{
				roster.AddToCounts(troop, countDelta, insertAtFront: false, woundedCount: woundedDelta, xpChange: 0, removeDepleted: true, index: -1);
			}
			statusText = isPrisoner
				? $"执行失败：未能从玩家队伍俘虏名册扣除被升格的{troop.Name}，已取消生成 Hero。"
				: $"执行失败：未能从玩家队伍扣除被升格的{troop.Name}，已取消生成 Hero。";
			return false;
		}
		reservedTroop = new PlayerOwnedTroopPromotionReservation
		{
			Troop = troop,
			IsPrisoner = isPrisoner,
			WasWounded = consumeWounded
		};
		Logger.Log("RewardSystemBehavior", "[NonHeroJoin] reserved player troop for promotion troop=" + (troop.StringId ?? "") + " roster=" + (isPrisoner ? "prisoner" : "member") + " before=" + total + " woundedBefore=" + wounded + " consumedWounded=" + consumeWounded + " after=" + remaining + " woundedAfter=" + remainingWounded + " agentIndex=" + (agent?.Index ?? -1));
		return true;
	}

	private static void RestoreReservedPlayerOwnedTroopAfterFailedPromotion(PlayerOwnedTroopPromotionReservation reservation)
	{
		try
		{
			CharacterObject troop = reservation?.Troop;
			TroopRoster roster = reservation?.IsPrisoner == true ? PartyBase.MainParty?.PrisonRoster : MobileParty.MainParty?.MemberRoster;
			if (troop == null || roster == null)
			{
				return;
			}
			roster.AddToCounts(troop, 1, insertAtFront: false, woundedCount: reservation.WasWounded ? 1 : 0, xpChange: 0, removeDepleted: true, index: -1);
			Logger.Log("RewardSystemBehavior", "[NonHeroJoin] restored player troop after failed promotion troop=" + (troop.StringId ?? "") + " roster=" + (reservation.IsPrisoner ? "prisoner" : "member") + " wounded=" + reservation.WasWounded);
		}
		catch (Exception ex)
		{
			Logger.Log("RewardSystemBehavior", "[NonHeroJoin] restore player troop after failed promotion failed troop=" + (reservation?.Troop?.StringId ?? "") + " error=" + ex.Message);
		}
	}

	private static bool IsTavernMercenaryLike(CharacterObject character)
	{
		if (character == null)
		{
			return false;
		}
		return character.Occupation == Occupation.Mercenary || character.Occupation == Occupation.CaravanGuard || character.Occupation == Occupation.Gangster;
	}

	private static bool IsCurrentJoinTargetInTavern(CharacterObject character, int targetAgentIndex)
	{
		try
		{
			if (CampaignMission.Current?.Location?.StringId == "tavern")
			{
				return true;
			}
			LocationCharacter locationCharacter = ResolveLocationCharacterForAgentIndex(targetAgentIndex) ?? LocationComplex.Current?.GetFirstLocationCharacterOfCharacter(character);
			Location location = (locationCharacter == null) ? null : LocationComplex.Current?.GetLocationOfCharacter(locationCharacter);
			return string.Equals(location?.StringId, "tavern", StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	private static Agent ResolveAgentForIndex(int targetAgentIndex)
	{
		try
		{
			Mission mission = Mission.Current;
			var agents = mission?.Agents;
			if (targetAgentIndex < 0 || agents == null)
			{
				return null;
			}
			return agents.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex);
		}
		catch
		{
			return null;
		}
	}

	private static bool TryCaptureAgentBodyProperties(Agent agent, out BodyProperties bodyProperties, out string error)
	{
		bodyProperties = default(BodyProperties);
		error = "";
		try
		{
			if (agent == null || !agent.IsActive())
			{
				error = "Agent 不存在或已失效";
				return false;
			}
			bodyProperties = agent.BodyPropertiesValue;
			if (bodyProperties.StaticProperties.Equals(default(StaticBodyProperties)) && bodyProperties.DynamicProperties.Age <= 0f)
			{
				error = "Agent BodyProperties 为空";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private static bool TryCaptureAgentEquipment(Agent agent, out Equipment equipment, out string error)
	{
		equipment = null;
		error = "";
		try
		{
			if (agent == null || !agent.IsActive())
			{
				error = "Agent 不存在或已失效";
				return false;
			}
			equipment = agent.SpawnEquipment?.Clone(false);
			if (equipment == null)
			{
				error = "SpawnEquipment 为空";
				return false;
			}
			try
			{
				for (int i = 0; i < 12; i++)
				{
					EquipmentIndex index = (EquipmentIndex)i;
					MissionWeapon missionWeapon = agent.Equipment[index];
					if (missionWeapon.Item != null)
					{
						equipment[index] = new EquipmentElement(missionWeapon.Item, missionWeapon.ItemModifier, null, false);
					}
				}
			}
			catch
			{
			}
			if (equipment.IsEmpty())
			{
				error = "当前装备为空";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			equipment = null;
			return false;
		}
	}

	private static void CopyCapturedEquipmentToHero(Hero hero, Equipment capturedEquipment)
	{
		if (hero == null || capturedEquipment == null)
		{
			return;
		}
		CopyEquipmentSlots(hero.BattleEquipment, capturedEquipment);
		CopyEquipmentSlots(hero.CivilianEquipment, capturedEquipment);
		CopyEquipmentSlots(hero.StealthEquipment, capturedEquipment);
	}

	private static void ApplyPromotedCompanionRandomTraits(Hero hero, CharacterObject template)
	{
		if (hero == null)
		{
			return;
		}
		try
		{
			List<TraitObject> personalityTraits = new List<TraitObject>
			{
				DefaultTraits.Mercy,
				DefaultTraits.Valor,
				DefaultTraits.Honor,
				DefaultTraits.Generosity,
				DefaultTraits.Calculating
			}.Where((TraitObject trait) => trait != null).ToList();
			if (personalityTraits.Count <= 0)
			{
				return;
			}
			foreach (TraitObject trait in personalityTraits)
			{
				int templateLevel = 0;
				try
				{
					templateLevel = template?.GetTraitLevel(trait) ?? 0;
				}
				catch
				{
					templateLevel = 0;
				}
				hero.SetTraitLevel(trait, MBMath.ClampInt(templateLevel, trait.MinValue, trait.MaxValue));
			}
			int currentNonZeroCount = personalityTraits.Count((TraitObject trait) => hero.GetTraitLevel(trait) != 0);
			float roll = MBRandom.RandomFloat;
			int targetNonZeroCount = (roll < 0.2f) ? 1 : ((roll < 0.85f) ? 2 : 3);
			List<TraitObject> candidates = personalityTraits.Where((TraitObject trait) => hero.GetTraitLevel(trait) == 0).ToList();
			while (currentNonZeroCount < targetNonZeroCount && candidates.Count > 0)
			{
				int index = MBRandom.RandomInt(candidates.Count);
				TraitObject trait = candidates[index];
				candidates.RemoveAt(index);
				int magnitude = (MBRandom.RandomFloat < 0.85f) ? 1 : 2;
				int signedLevel = (MBRandom.RandomFloat < 0.5f) ? (-magnitude) : magnitude;
				signedLevel = MBMath.ClampInt(signedLevel, trait.MinValue, trait.MaxValue);
				if (signedLevel == 0)
				{
					signedLevel = trait.MaxValue > 0 ? 1 : ((trait.MinValue < 0) ? (-1) : 0);
				}
				if (signedLevel == 0)
				{
					continue;
				}
				hero.SetTraitLevel(trait, signedLevel);
				currentNonZeroCount++;
			}
			Logger.Log("RewardSystemBehavior", "[NonHeroJoin] promoted_traits hero=" + (hero.StringId ?? "") + " " + BuildPromotedCompanionTraitSummary(hero, personalityTraits));
		}
		catch (Exception ex)
		{
			Logger.Log("RewardSystemBehavior", "[NonHeroJoin] promoted_traits_failed hero=" + (hero.StringId ?? "null") + " error=" + ex.Message);
		}
	}

	private static string BuildPromotedCompanionTraitSummary(Hero hero, IEnumerable<TraitObject> traits)
	{
		if (hero == null || traits == null)
		{
			return "";
		}
		List<string> parts = new List<string>();
		foreach (TraitObject trait in traits)
		{
			if (trait == null)
			{
				continue;
			}
			try
			{
				parts.Add((trait.StringId ?? "trait") + "=" + hero.GetTraitLevel(trait));
			}
			catch
			{
			}
		}
		return string.Join(",", parts);
	}

	private static void CopyEquipmentSlots(Equipment target, Equipment source)
	{
		if (target == null || source == null)
		{
			return;
		}
		for (int i = 0; i < 12; i++)
		{
			target[i] = new EquipmentElement(source[i].Item, source[i].ItemModifier, null, false);
		}
	}

	private static string ResolveNonHeroFullDisplayName(CharacterObject joiningCharacter, string promptDisplayName, string promptGivenName, int targetAgentIndex)
	{
		string text = (promptDisplayName ?? "").Trim();
		string troopName = (joiningCharacter?.Name?.ToString() ?? "").Trim();
		string given = (promptGivenName ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, given, StringComparison.OrdinalIgnoreCase))
		{
			if (!string.IsNullOrWhiteSpace(troopName) && !text.Contains(troopName) && !string.IsNullOrWhiteSpace(given))
			{
				return troopName + given;
			}
			return text;
		}
		if (!string.IsNullOrWhiteSpace(troopName) && !string.IsNullOrWhiteSpace(given))
		{
			return troopName + given;
		}
		try
		{
			string agentName = ResolveAgentForIndex(targetAgentIndex)?.Name;
			if (!string.IsNullOrWhiteSpace(agentName))
			{
				return agentName.Trim();
			}
		}
		catch
		{
		}
		return string.IsNullOrWhiteSpace(troopName) ? given : troopName;
	}

	private static string ResolveNonHeroPersonalName(string promptGivenName, string fullDisplayName, string originalTroopName)
	{
		string given = SanitizeHeroPersonalName(promptGivenName);
		if (!string.IsNullOrWhiteSpace(given))
		{
			return given;
		}
		string full = (fullDisplayName ?? "").Trim();
		string troop = (originalTroopName ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(full) && !string.IsNullOrWhiteSpace(troop))
		{
			string candidate = RemoveNamePrefix(full, troop);
			candidate = SanitizeHeroPersonalName(candidate);
			if (!string.IsNullOrWhiteSpace(candidate) && !string.Equals(candidate, full, StringComparison.OrdinalIgnoreCase))
			{
				return candidate;
			}
		}
		if (!string.IsNullOrWhiteSpace(full))
		{
			string[] parts = Regex.Split(full, "\\s+").Where((string x) => !string.IsNullOrWhiteSpace(x)).ToArray();
			if (parts.Length > 1)
			{
				return SanitizeHeroPersonalName(parts[parts.Length - 1]);
			}
			string compact = SanitizeHeroPersonalName(full);
			if (compact.Length > 3)
			{
				return compact.Substring(compact.Length - Math.Min(3, compact.Length));
			}
			return compact;
		}
		return "";
	}

	private static string RemoveNamePrefix(string fullDisplayName, string prefix)
	{
		string full = (fullDisplayName ?? "").Trim();
		string pref = (prefix ?? "").Trim();
		if (string.IsNullOrWhiteSpace(full) || string.IsNullOrWhiteSpace(pref))
		{
			return full;
		}
		if (full.StartsWith(pref, StringComparison.OrdinalIgnoreCase))
		{
			return full.Substring(pref.Length).Trim();
		}
		string compactFull = Regex.Replace(full, "\\s+", "");
		string compactPrefix = Regex.Replace(pref, "\\s+", "");
		if (compactFull.StartsWith(compactPrefix, StringComparison.OrdinalIgnoreCase))
		{
			return compactFull.Substring(compactPrefix.Length).Trim();
		}
		int index = full.IndexOf(pref, StringComparison.OrdinalIgnoreCase);
		if (index >= 0)
		{
			return full.Remove(index, pref.Length).Trim();
		}
		return full;
	}

	private static string SanitizeHeroPersonalName(string name)
	{
		string text = (name ?? "").Replace("\r", "").Replace("\n", " ").Trim();
		text = Regex.Replace(text, "^[\\s:：,，;；\\-—_《》“”\"'\\[\\]【】（）()]+|[\\s:：,，;；\\-—_《》“”\"'\\[\\]【】（）()]+$", "");
		return text.Trim();
	}

	private static float ClampBodyShape01(float value)
	{
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			return 0.5f;
		}
		return Math.Max(0f, Math.Min(1f, value));
	}

	private static int ResolvePromotedHeroAge(BodyProperties bodyProperties, CharacterObject template)
	{
		float age = bodyProperties.DynamicProperties.Age;
		if (age < 16f || age > 80f)
		{
			age = template?.Age ?? 25f;
		}
		if (age < 18f)
		{
			age = 18f;
		}
		if (age > 70f)
		{
			age = 70f;
		}
		return Math.Max(18, (int)Math.Round(age));
	}

	private static SkillObject[] GetCompanionSkillObjects()
	{
		return new SkillObject[18]
		{
			DefaultSkills.OneHanded,
			DefaultSkills.TwoHanded,
			DefaultSkills.Polearm,
			DefaultSkills.Bow,
			DefaultSkills.Crossbow,
			DefaultSkills.Throwing,
			DefaultSkills.Riding,
			DefaultSkills.Athletics,
			DefaultSkills.Crafting,
			DefaultSkills.Scouting,
			DefaultSkills.Tactics,
			DefaultSkills.Roguery,
			DefaultSkills.Charm,
			DefaultSkills.Leadership,
			DefaultSkills.Trade,
			DefaultSkills.Steward,
			DefaultSkills.Medicine,
			DefaultSkills.Engineering
		};
	}

	private static void ApplyTemplateSkillsToHero(Hero hero, CharacterObject template)
	{
		if (hero == null || template == null)
		{
			return;
		}
		try
		{
			foreach (SkillObject skill in GetCompanionSkillObjects())
			{
				if (skill != null)
				{
					hero.SetSkillValue(skill, Math.Max(0, template.GetSkillValue(skill)));
				}
			}
		}
		catch
		{
		}
	}

	private static string StripNonHeroJoinTag(string text)
	{
		return HeroJoinPlayerPartyTagRegex.Replace((text ?? "").Replace("\r", ""), "").Trim();
	}

	private static string BuildCurrentSceneLabelForPrompt()
	{
		try
		{
			string settlement = Settlement.CurrentSettlement?.Name?.ToString() ?? PlayerEncounter.EncounterSettlement?.Name?.ToString() ?? "";
			string location = CampaignMission.Current?.Location?.Name?.ToString() ?? CampaignMission.Current?.Location?.StringId ?? "";
			string scene = Mission.Current?.SceneName ?? "";
			List<string> parts = new List<string>();
			if (!string.IsNullOrWhiteSpace(settlement))
			{
				parts.Add(settlement);
			}
			if (!string.IsNullOrWhiteSpace(location))
			{
				parts.Add(location);
			}
			if (!string.IsNullOrWhiteSpace(scene))
			{
				parts.Add(scene);
			}
			return parts.Count == 0 ? "当前场景" : string.Join(" / ", parts);
		}
		catch
		{
			return "当前场景";
		}
	}

	private static string BuildDialogueHistoryForPrompt(List<string> dialogueHistory)
	{
		if (dialogueHistory == null || dialogueHistory.Count == 0)
		{
			return "（无可用加入前对话历史）";
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string line in dialogueHistory)
		{
			string text = (line ?? "").Replace("\r", "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				stringBuilder.AppendLine(text);
			}
		}
		string result = stringBuilder.ToString().Trim();
		if (result.Length > 6000)
		{
			result = result.Substring(result.Length - 6000).Trim();
		}
		return string.IsNullOrWhiteSpace(result) ? "（无可用加入前对话历史）" : result;
	}

	private static void AppendPromotedHeroPriorHistory(Hero hero, List<string> dialogueHistory)
	{
		if (hero == null || dialogueHistory == null || dialogueHistory.Count == 0)
		{
			return;
		}
		foreach (string line in dialogueHistory.Where((string x) => !string.IsNullOrWhiteSpace(x)).Take(40))
		{
			MyBehavior.AppendExternalDialogueHistory(hero, null, null, "[加入前对话] " + line.Trim());
		}
	}

	private static string BuildEquipmentSummaryForPrompt(Equipment equipment)
	{
		if (equipment == null)
		{
			return "（无装备）";
		}
		List<string> list = new List<string>();
		for (int i = 0; i < 12; i++)
		{
			EquipmentIndex index = (EquipmentIndex)i;
			EquipmentElement element = equipment[index];
			if (element.Item != null)
			{
				string itemName = element.GetModifiedItemName()?.ToString() ?? element.Item.Name?.ToString() ?? element.Item.StringId;
				list.Add(index + "=" + itemName);
			}
		}
		return list.Count == 0 ? "（无装备）" : string.Join(", ", list);
	}

	private static int CountMatchingTavernLocationCharacters(CharacterObject character)
	{
		try
		{
			LocationComplex locationComplex = LocationComplex.Current;
			if (locationComplex == null || character == null)
			{
				return 0;
			}
			return locationComplex.GetListOfCharacters().Count((LocationCharacter x) => x != null && x.Character == character && string.Equals(locationComplex.GetLocationOfCharacter(x)?.StringId, "tavern", StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return 0;
		}
	}

	private static LocationCharacter ResolveLocationCharacterForAgentIndex(int targetAgentIndex)
	{
		try
		{
			if (targetAgentIndex < 0 || Mission.Current == null || LocationComplex.Current == null)
			{
				return null;
			}
			Agent agent = ResolveAgentForIndex(targetAgentIndex);
			return (agent == null) ? null : LocationComplex.Current.FindCharacter(agent);
		}
		catch
		{
			return null;
		}
	}

	private static void RemoveJoinedLiveAgent(int targetAgentIndex)
	{
		try
		{
			Agent agent = ResolveAgentForIndex(targetAgentIndex);
			if (agent != null && agent.IsActive())
			{
				agent.FadeOut(true, true);
				agent.AgentVisuals?.SetVisible(false);
			}
		}
		catch
		{
		}
	}

	private static void RemoveJoinedNonHeroLocationCharacters(CharacterObject joiningCharacter, int targetAgentIndex, bool removeAllMatchingTavernMercenaries)
	{
		try
		{
			LocationComplex locationComplex = LocationComplex.Current;
			if (locationComplex == null || joiningCharacter == null)
			{
				return;
			}
			if (removeAllMatchingTavernMercenaries)
			{
				List<LocationCharacter> list = locationComplex.GetListOfCharacters().Where((LocationCharacter x) => x != null && x.Character == joiningCharacter && string.Equals(locationComplex.GetLocationOfCharacter(x)?.StringId, "tavern", StringComparison.OrdinalIgnoreCase)).ToList();
				foreach (LocationCharacter item in list)
				{
					locationComplex.RemoveCharacterIfExists(item);
				}
				return;
			}
			LocationCharacter locationCharacter = ResolveLocationCharacterForAgentIndex(targetAgentIndex) ?? locationComplex.GetFirstLocationCharacterOfCharacter(joiningCharacter);
			if (locationCharacter != null)
			{
				locationComplex.RemoveCharacterIfExists(locationCharacter);
			}
		}
		catch
		{
		}
	}

	private static Kingdom ResolveKingdomByTag(string kingdomToken, Hero giver)
	{
		try
		{
			string text = (kingdomToken ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || text.Equals("self", StringComparison.OrdinalIgnoreCase) || text.Equals("npc", StringComparison.OrdinalIgnoreCase) || text.Equals("current", StringComparison.OrdinalIgnoreCase) || text.Equals("auto", StringComparison.OrdinalIgnoreCase))
			{
				return giver?.Clan?.Kingdom;
			}
			MBReadOnlyList<Kingdom> all = Kingdom.All;
			if (all == null)
			{
				return giver?.Clan?.Kingdom;
			}
			for (int i = 0; i < all.Count; i++)
			{
				Kingdom kingdom = all[i];
				if (kingdom != null && string.Equals((kingdom.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase))
				{
					return kingdom;
				}
			}
			for (int j = 0; j < all.Count; j++)
			{
				Kingdom kingdom2 = all[j];
				if (kingdom2 != null)
				{
					string text2 = (kingdom2.Name?.ToString() ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text2) && string.Equals(text2, text, StringComparison.OrdinalIgnoreCase))
					{
						return kingdom2;
					}
				}
			}
		}
		catch
		{
		}
		return giver?.Clan?.Kingdom;
	}

	private static Kingdom ResolveKingdomByIdTagStrict(string kingdomToken)
	{
		try
		{
			string text = (kingdomToken ?? "").Trim();
			if (text.StartsWith("kingdom:", StringComparison.OrdinalIgnoreCase))
			{
				text = text.Substring("kingdom:".Length).Trim();
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			MBReadOnlyList<Kingdom> all = Kingdom.All;
			if (all == null)
			{
				return null;
			}
			for (int i = 0; i < all.Count; i++)
			{
				Kingdom kingdom = all[i];
				if (kingdom != null && string.Equals((kingdom.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase))
				{
					return kingdom;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static string GetClanDisplayNameForNotification(Clan clan)
	{
		try
		{
			string text = clan?.Name?.ToString();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text.Trim();
			}
		}
		catch
		{
		}
		return clan?.StringId ?? "未知家族";
	}

	private static Clan ResolveClanByTag(string clanToken, Hero giver)
	{
		try
		{
			string text = (clanToken ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			if (text.Equals("self", StringComparison.OrdinalIgnoreCase) || text.Equals("npc", StringComparison.OrdinalIgnoreCase) || text.Equals("current", StringComparison.OrdinalIgnoreCase) || text.Equals("auto", StringComparison.OrdinalIgnoreCase))
			{
				return giver?.Clan;
			}
			MBReadOnlyList<Clan> all = Clan.All;
			if (all == null)
			{
				return null;
			}
			for (int i = 0; i < all.Count; i++)
			{
				Clan clan = all[i];
				if (clan != null && string.Equals((clan.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase))
				{
					return clan;
				}
			}
			for (int j = 0; j < all.Count; j++)
			{
				Clan clan2 = all[j];
				if (clan2 == null)
				{
					continue;
				}
				string text2 = (clan2.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text2) && string.Equals(text2, text, StringComparison.OrdinalIgnoreCase))
				{
					return clan2;
				}
				Hero leader = clan2.Leader;
				if (leader != null && (string.Equals((leader.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase) || string.Equals((leader.Name?.ToString() ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase)))
				{
					return clan2;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static bool IsPlayerWarsCompatibleWithKingdom(Kingdom offerKingdom)
	{
		try
		{
			if (offerKingdom == null || Clan.PlayerClan == null)
			{
				return false;
			}
			float num = 0f;
			try
			{
				num = (Campaign.Current?.Models?.DiplomacyModel?.GetStrengthThresholdForNonMutualWarsToBeIgnoredToJoinKingdom(offerKingdom)).GetValueOrDefault();
			}
			catch
			{
				num = 0f;
			}
			List<IFaction> list = new List<IFaction>();
			List<IFaction> list2 = new List<IFaction>();
			try
			{
				MBReadOnlyList<Kingdom> all = Kingdom.All;
				if (all != null)
				{
					for (int i = 0; i < all.Count; i++)
					{
						Kingdom kingdom = all[i];
						if (kingdom != null && Clan.PlayerClan.MapFaction != null && Clan.PlayerClan.MapFaction.IsAtWarWith(kingdom) && kingdom.CurrentTotalStrength > num)
						{
							list.Add(kingdom);
						}
					}
					for (int j = 0; j < all.Count; j++)
					{
						Kingdom kingdom2 = all[j];
						if (kingdom2 != null && offerKingdom.IsAtWarWith(kingdom2))
						{
							list2.Add(kingdom2);
						}
					}
				}
			}
			catch
			{
			}
			if (list.Count <= 0)
			{
				return true;
			}
			int num2 = list.Intersect(list2).Count();
			return num2 == list.Count;
		}
		catch
		{
			return false;
		}
	}

	private static bool CanPlayerOfferMercenaryServiceCompat(Kingdom offerKingdom)
	{
		try
		{
			Clan playerClan = Clan.PlayerClan;
			if (playerClan == null || offerKingdom == null)
			{
				return false;
			}
			int num2 = 0;
			try
			{
				num2 = (Campaign.Current?.Models?.DiplomacyModel?.MinimumRelationWithConversationCharacterToJoinKingdom).GetValueOrDefault();
			}
			catch
			{
				num2 = 0;
			}
			bool flag = playerClan.Kingdom == null;
			bool flag2 = !playerClan.IsAtWarWith(offerKingdom);
			bool flag3 = offerKingdom.Leader != null && offerKingdom.Leader.GetRelationWithPlayer() >= (float)num2;
			bool flag4 = playerClan.Settlements == null || playerClan.Settlements.Count <= 0;
			bool flag5 = IsPlayerWarsCompatibleWithKingdom(offerKingdom);
			return flag && flag2 && flag3 && flag4 && flag5;
		}
		catch
		{
			return false;
		}
	}

	private static bool CanPlayerOfferVassalageCompat(Kingdom offerKingdom)
	{
		try
		{
			Clan playerClan = Clan.PlayerClan;
			if (playerClan == null || offerKingdom == null)
			{
				return false;
			}
			int num = 0;
			try
			{
				num = (Campaign.Current?.Models?.DiplomacyModel?.MinimumRelationWithConversationCharacterToJoinKingdom).GetValueOrDefault();
			}
			catch
			{
				num = 0;
			}
			bool flag = playerClan.Kingdom == null || playerClan.IsUnderMercenaryService;
			bool flag2 = !playerClan.IsAtWarWith(offerKingdom);
			bool flag3 = !offerKingdom.IsEliminated;
			bool flag4 = offerKingdom.Leader != null && offerKingdom.Leader.GetRelationWithPlayer() >= (float)num;
			bool flag5 = IsPlayerWarsCompatibleWithKingdom(offerKingdom);
			return flag && flag2 && flag3 && flag4 && flag5;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryApplyKingAbdicateToPlayerAction(Hero giver, out string statusText)
	{
		statusText = "";
		try
		{
			Clan playerClan = Clan.PlayerClan;
			if (playerClan == null || Hero.MainHero == null)
			{
				statusText = "执行失败：未找到玩家家族。";
				return false;
			}
			if (giver == null || giver == Hero.MainHero || giver.Clan == null)
			{
				statusText = "执行失败：当前对话对象不是可让位的王国国王。";
				return false;
			}
			Kingdom targetKingdom = giver.Clan.Kingdom ?? giver.MapFaction as Kingdom;
			if (targetKingdom == null || targetKingdom.IsEliminated)
			{
				statusText = "执行失败：当前对话对象没有有效王国。";
				return false;
			}
			string kingdomName = targetKingdom.Name?.ToString() ?? targetKingdom.StringId ?? "该王国";
			string playerClanName = GetClanDisplayNameForNotification(playerClan);
			string playerDisplayName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(giver);
			if (string.IsNullOrWhiteSpace(playerDisplayName))
			{
				playerDisplayName = "玩家";
			}
			if (targetKingdom.RulingClan == playerClan)
			{
				statusText = "执行跳过：" + playerClanName + " 已经是 " + kingdomName + " 的执政家族。";
				return false;
			}
			if (targetKingdom.Leader != giver && targetKingdom.RulingClan?.Leader != giver)
			{
				statusText = "执行失败：" + (giver.Name?.ToString() ?? "当前NPC") + " 不是 " + kingdomName + " 的国王，不能让出王位。";
				return false;
			}
			if (giver.Clan == playerClan)
			{
				statusText = "执行失败：玩家家族不能通过对方让位重复取得王位。";
				return false;
			}
			Kingdom oldKingdom = playerClan.Kingdom;
			List<string> transitionNotes = new List<string>();
			bool destroyOldKingdomAfterDeparture = false;
			if (oldKingdom != null && oldKingdom != targetKingdom)
			{
				PrepareRulingClanTransitionForDepartingClan(oldKingdom, playerClan, transitionNotes, out destroyOldKingdomAfterDeparture);
				ChangeKingdomAction.ApplyByJoinToKingdomByDefection(playerClan, oldKingdom, targetKingdom, default(CampaignTime), showNotification: true);
				FinalizeRulingClanTransitionForDepartedClan(oldKingdom, destroyOldKingdomAfterDeparture, transitionNotes);
			}
			else if (playerClan.Kingdom == null || playerClan.IsUnderMercenaryService)
			{
				ChangeKingdomAction.ApplyByJoinToKingdom(playerClan, targetKingdom, default(CampaignTime), showNotification: true);
			}
			if (playerClan.Kingdom != targetKingdom)
			{
				statusText = "执行失败：玩家家族未能加入 " + kingdomName + "，王位没有转移。";
				return false;
			}
			ChangeRulingClanAction.Apply(targetKingdom, playerClan);
			string transitionSummary = BuildRecruitmentTransitionSummary(transitionNotes);
			statusText = "执行成功：" + (giver.Name?.ToString() ?? "国王") + " 已将 " + kingdomName + " 的王位让给" + playerDisplayName + "，" + playerClanName + " 成为新的执政家族" + transitionSummary + "。";
			return true;
		}
		catch (Exception ex)
		{
			statusText = "执行失败（异常）：" + ex.Message;
			return false;
		}
	}

	private static bool CanGiverAuthorizePlayerKingdomLeave(Hero giver, Kingdom currentKingdom)
	{
		try
		{
			if (giver == null || currentKingdom == null)
			{
				return false;
			}
			if (currentKingdom.Leader == giver || currentKingdom.RulingClan?.Leader == giver)
			{
				return true;
			}
			Clan giverClan = giver.Clan;
			return giverClan != null && !giverClan.IsEliminated && giverClan.Kingdom == currentKingdom;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Thin external bridge used by castle GCCZ after its independent policy has selected
	/// the clan-leader branch. Normal AF recruitment rules and callers are unchanged.
	/// </summary>
	public bool TryApplyClanLeaderJoinPlayerKingdomForExternal(Hero joiningLeader, out string statusText)
	{
		statusText = "";
		try
		{
			Clan playerClan = Clan.PlayerClan;
			Kingdom playerKingdom = playerClan?.Kingdom;
			if (joiningLeader == null || joiningLeader.Clan?.Leader != joiningLeader)
			{
				statusText = "执行失败：只有被招揽家族的现任族长才能代表全族归附。";
				return false;
			}
			if (playerKingdom == null || playerKingdom.IsEliminated)
			{
				statusText = "执行失败：玩家当前没有可接纳该家族的有效王国。";
				return false;
			}
			if (playerKingdom.Leader != Hero.MainHero && playerKingdom.RulingClan != playerClan)
			{
				statusText = "执行失败：玩家不是所属王国的统治者，不能在城堡现场直接批准全族归附。";
				return false;
			}
			return TryApplyClanJoinKingdomAction(joiningLeader, joiningLeader.Clan, playerKingdom, out statusText);
		}
		catch (Exception ex)
		{
			statusText = "执行失败（族长归附异常）：" + ex.Message;
			return false;
		}
	}

	private static bool TryApplyClanJoinKingdomAction(Hero giver, Clan targetClan, Kingdom targetKingdom, out string statusText)
	{
		statusText = "";
		try
		{
			if (targetKingdom == null || targetKingdom.IsEliminated)
			{
				statusText = "执行失败：目标王国无效或已经灭亡。";
				return false;
			}
			if (targetClan == null)
			{
				statusText = "执行失败：未找到当前NPC所属家族。";
				return false;
			}
			string clanDisplayName = GetClanDisplayNameForNotification(targetClan);
			if (targetClan == Clan.PlayerClan)
			{
				statusText = "执行失败：玩家家族不能通过当前NPC重复归附其他王国。";
				return false;
			}
			if (targetClan.IsEliminated)
			{
				statusText = "执行失败：" + clanDisplayName + " 已经灭亡或不可用。";
				return false;
			}
			if (giver == null || giver.Clan != targetClan || targetClan.Leader != giver)
			{
				statusText = "执行失败：只有当前家族族长本人才能代表全族选择效忠王国。";
				return false;
			}
			if (targetClan.Kingdom == targetKingdom)
			{
				statusText = "执行跳过：" + clanDisplayName + " 已经效力于 " + targetKingdom.Name + "。";
				return false;
			}
			Kingdom oldKingdom = targetClan.Kingdom;
			List<Settlement> carriedSettlements = (targetClan.Settlements ?? Enumerable.Empty<Settlement>())
				.Where((Settlement x) => x != null && (x.IsTown || x.IsCastle))
				.ToList();
			List<string> transitionNotes = new List<string>();
			bool destroyOldKingdomAfterDeparture = false;
			PrepareRulingClanTransitionForDepartingClan(oldKingdom, targetClan, transitionNotes, out destroyOldKingdomAfterDeparture);
			string carriedFiefSummary = BuildClanRecruitmentFiefSummary(carriedSettlements);
			if (targetClan.IsUnderMercenaryService)
			{
				ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(targetClan);
				ChangeKingdomAction.ApplyByJoinToKingdom(targetClan, targetKingdom, default(CampaignTime), showNotification: true);
				FinalizeRulingClanTransitionForDepartedClan(oldKingdom, destroyOldKingdomAfterDeparture, transitionNotes);
				if (targetClan.Kingdom != targetKingdom)
				{
					statusText = "执行失败：" + clanDisplayName + " 未能加入 " + targetKingdom.Name + "。";
					return false;
				}
				statusText = "执行成功：" + clanDisplayName + " 已结束旧雇佣关系，并作为领主加入 " + targetKingdom.Name + "（KingdomId=" + (targetKingdom.StringId ?? "") + "）" + carriedFiefSummary + BuildRecruitmentTransitionSummary(transitionNotes) + "。";
				return true;
			}
			if (oldKingdom != null)
			{
				ChangeKingdomAction.ApplyByJoinToKingdomByDefection(targetClan, oldKingdom, targetKingdom, default(CampaignTime), showNotification: true);
				FinalizeRulingClanTransitionForDepartedClan(oldKingdom, destroyOldKingdomAfterDeparture, transitionNotes);
				if (targetClan.Kingdom != targetKingdom)
				{
					statusText = "执行失败：" + clanDisplayName + " 未能加入 " + targetKingdom.Name + "。";
					return false;
				}
				statusText = "执行成功：" + clanDisplayName + " 已脱离 " + oldKingdom.Name + "，并作为领主加入 " + targetKingdom.Name + "（KingdomId=" + (targetKingdom.StringId ?? "") + "）" + carriedFiefSummary + BuildRecruitmentTransitionSummary(transitionNotes) + "。";
				return true;
			}
			ChangeKingdomAction.ApplyByJoinToKingdom(targetClan, targetKingdom, default(CampaignTime), showNotification: true);
			if (targetClan.Kingdom != targetKingdom)
			{
				statusText = "执行失败：" + clanDisplayName + " 未能加入 " + targetKingdom.Name + "。";
				return false;
			}
			statusText = "执行成功：" + clanDisplayName + " 已作为领主加入 " + targetKingdom.Name + "（KingdomId=" + (targetKingdom.StringId ?? "") + "）" + carriedFiefSummary + BuildRecruitmentTransitionSummary(transitionNotes) + "。";
			return true;
		}
		catch (Exception ex)
		{
			statusText = "执行失败（异常）：" + ex.Message;
			return false;
		}
	}

	private bool TryApplyKingdomServiceAction(Hero giver, string serviceType, string kingdomToken, out string statusText)
	{
		statusText = "";
		try
		{
			Clan playerClan = Clan.PlayerClan;
			if (playerClan == null)
			{
				statusText = "执行失败：未找到玩家家族。";
				return false;
			}
			string text = (serviceType ?? "").Trim().ToUpperInvariant();
			if (text == "LEAVE")
			{
				Kingdom kingdom = playerClan.Kingdom;
				if (kingdom == null)
				{
					statusText = "执行失败：玩家当前未加入任何势力，无需退出。";
					return false;
				}
				if (!CanGiverAuthorizePlayerKingdomLeave(giver, kingdom))
				{
					string giverKingdomName = giver?.Clan?.Kingdom?.Name?.ToString() ?? (giver?.MapFaction as Kingdom)?.Name?.ToString() ?? "非当前效力势力";
					statusText = "执行失败：当前对话对象属于 " + giverKingdomName + "，不能代表 " + kingdom.Name + " 批准玩家退出当前效力。";
					return false;
				}
				if (playerClan.IsUnderMercenaryService)
				{
					ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(playerClan);
					statusText = $"执行成功：玩家已结束与 {kingdom.Name} 的雇佣兵契约。";
					return true;
				}
				ChangeKingdomAction.ApplyByLeaveKingdom(playerClan);
				statusText = $"执行成功：玩家已退出 {kingdom.Name}，不再是其正式封臣。";
				return true;
			}
			if (text == "CLAN_JOIN_PLAYER_KINGDOM")
			{
				Kingdom playerKingdom = playerClan.Kingdom;
				if (playerKingdom == null || playerKingdom.IsEliminated)
				{
					statusText = "执行失败：玩家当前没有可供目标家族加入的王国。";
					return false;
				}
				Clan targetClan = ResolveClanByTag(kingdomToken, giver);
				return TryApplyClanJoinKingdomAction(giver, targetClan, playerKingdom, out statusText);
			}
			if (text == "CLAN_JOIN_KINGDOM")
			{
				Kingdom targetKingdom = ResolveKingdomByIdTagStrict(kingdomToken);
				if (targetKingdom == null)
				{
					statusText = "执行失败：目标王国ID无效（" + kingdomToken + "）。";
					return false;
				}
				return TryApplyClanJoinKingdomAction(giver, giver?.Clan, targetKingdom, out statusText);
			}
			Kingdom kingdom2 = ResolveKingdomByTag(kingdomToken, giver);
			if (kingdom2 == null || kingdom2.IsEliminated)
			{
				statusText = "执行失败：目标势力无效（" + kingdomToken + "）。";
				return false;
			}
			if (text == "MERCENARY")
			{
				if (giver?.Clan == null || giver.Clan.Kingdom != kingdom2 || giver.Clan.IsUnderMercenaryService)
				{
					statusText = "执行失败：当前对话对象并非该势力正式封臣，不能签订雇佣兵契约。";
					return false;
				}
				if (playerClan.Kingdom != null && !playerClan.IsUnderMercenaryService)
				{
					statusText = "执行失败：玩家已是某王国正式封臣，不能再作为雇佣兵加入。";
					return false;
				}
				int num2 = 50;
				try
				{
					num2 = Campaign.Current?.Models?.MinorFactionsModel?.GetMercenaryAwardFactorToJoinKingdom(playerClan, kingdom2) ?? 50;
				}
				catch
				{
					num2 = 50;
				}
				ChangeKingdomAction.ApplyByJoinFactionAsMercenary(playerClan, kingdom2, default(CampaignTime), num2);
				statusText = $"执行成功：玩家已作为雇佣兵加入 {kingdom2.Name}（KingdomId={kingdom2.StringId}）。";
				return true;
			}
			if (text == "VASSAL")
			{
				if (giver == null || kingdom2.Leader != giver)
				{
					statusText = "执行失败：只有目标王国的国王才能授予正式封臣身份。";
					return false;
				}
				if (playerClan.Kingdom == kingdom2 && !playerClan.IsUnderMercenaryService)
				{
					statusText = $"执行跳过：玩家已是 {kingdom2.Name} 的正式封臣。";
					return false;
				}
				if (playerClan.Kingdom == kingdom2 && playerClan.IsUnderMercenaryService)
				{
					EndMercenaryServiceAction.EndByBecomingVassal(playerClan);
				}
				else
				{
					if (playerClan.IsUnderMercenaryService)
					{
						EndMercenaryServiceAction.EndByLeavingKingdom(playerClan);
					}
					ChangeKingdomAction.ApplyByJoinToKingdom(playerClan, kingdom2);
				}
				if (playerClan.Kingdom != kingdom2 || playerClan.IsUnderMercenaryService)
				{
					statusText = $"执行失败：玩家未能成为 {kingdom2.Name} 的正式封臣。";
					return false;
				}
				statusText = $"执行成功：玩家已加入 {kingdom2.Name} 成为正式封臣（KingdomId={kingdom2.StringId}）。";
				return true;
			}
			statusText = "执行失败：未知势力效力类型 " + serviceType + "。";
			return false;
		}
		catch (Exception ex)
		{
			statusText = "执行失败（异常）：" + ex.Message;
			return false;
		}
	}

	private bool TryApplySettlementTransferAction(Hero giver, Hero receiver, string directionToken, string settlementToken, IDictionary<string, FixedAssetTokenResolution> fixedAssetResolutionCache, ISet<string> unresolvedFixedAssetTokens, out MyBehavior.SettlementTransferPromptEntry authorizedEntry, out string statusText)
	{
		return TryApplySettlementTransferAction(
			giver,
			receiver,
			directionToken,
			settlementToken,
			fixedAssetResolutionCache,
			unresolvedFixedAssetTokens,
			out authorizedEntry,
			out statusText,
			mutationObservation: null);
	}

	private bool TryApplySettlementTransferAction(Hero giver, Hero receiver, string directionToken, string settlementToken, IDictionary<string, FixedAssetTokenResolution> fixedAssetResolutionCache, ISet<string> unresolvedFixedAssetTokens, out MyBehavior.SettlementTransferPromptEntry authorizedEntry, out string statusText, EconomyMutationObservation mutationObservation)
	{
		authorizedEntry = null;
		statusText = "";
		try
		{
			if (giver == null || receiver == null)
			{
				statusText = "执行失败：缺少转移双方。";
				return false;
			}
			string text = (directionToken ?? "").Trim().ToUpperInvariant();
			if (!string.Equals(text, "TO_PLAYER", StringComparison.OrdinalIgnoreCase))
			{
				statusText = "执行失败：后处理仅允许 NPC 向玩家转移固定资产。";
				return false;
			}
			if (!TryResolveFixedAssetTokenForGiveAsset(giver, settlementToken, fixedAssetResolutionCache, unresolvedFixedAssetTokens, out authorizedEntry, out bool isPromptAuthorized))
			{
				statusText = "执行失败：未找到可转移的固定资产（" + settlementToken + "）。";
				return false;
			}
			if (!isPromptAuthorized)
			{
				Logger.Log("Logic", "[Reward] GIVE_ASSET fixed_asset_direct_id_execute giver=" + (giver.StringId ?? "") + " asset=" + (settlementToken ?? "").Trim());
			}
			return TryApplySettlementTransferEntryAction(
				giver,
				receiver,
				text,
				authorizedEntry,
				allowDirectFixedAssetIdOverride: !isPromptAuthorized,
				statusText: out statusText,
				mutationObservation: mutationObservation);
		}
		catch (Exception ex)
		{
			mutationObservation?.MarkUnknown("economy.settlement_transfer_exception");
			statusText = "执行失败（异常）：" + ex.Message;
			return false;
		}
	}

	private int AdjustTrust(Hero npc, int personalDelta, int publicDelta, string reason, out int appliedUnits)
	{
		appliedUnits = 0;
		if (npc == null)
		{
			return 0;
		}
		if (IsPrisonerTrustGainBlocked(npc) && (personalDelta > 0 || publicDelta > 0))
		{
			int blockedPersonalDelta = Math.Max(0, personalDelta);
			int blockedPublicDelta = Math.Max(0, publicDelta);
			personalDelta = Math.Min(0, personalDelta);
			publicDelta = Math.Min(0, publicDelta);
			Logger.Log("Trust", $"npc={npc.StringId} reason={reason} blocked=prisoner positivePersonalDelta={blockedPersonalDelta} positivePublicDelta={blockedPublicDelta}");
			if (personalDelta == 0 && publicDelta == 0)
			{
				return 0;
			}
		}
		int npcTrust = GetNpcTrust(npc);
		int publicTrust = GetPublicTrust(npc);
		int num = npcTrust;
		int num2 = publicTrust;
		string text = BuildPublicTrustKey(npc);
		if (personalDelta != 0)
		{
			if (_npcTrust == null)
			{
				_npcTrust = new Dictionary<string, int>();
			}
			string text2 = BuildNpcTrustKey(npc);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				int num6 = ApplyDirectTrustDeltaUnits(text2, npcTrust, personalDelta, out appliedUnits);
				num = ClampTrust(npcTrust + num6);
				if (num == 0)
				{
					_npcTrust.Remove(text2);
				}
				else
				{
					_npcTrust[text2] = num;
				}
			}
		}
		int num3 = 0;
		if (personalDelta != 0)
		{
			num3 += ApplyPublicTrustPoolDeltaByKey(text, appliedUnits, (reason ?? "external") + "_public_pool");
			num2 = GetPublicTrust(npc);
		}
		if (publicDelta != 0)
		{
			num3 += AdjustPublicTrustByKey(text, publicDelta, (reason ?? "external") + "_direct");
			num2 = GetPublicTrust(npc);
		}
		int num4 = ClampTrust(npcTrust + publicTrust);
		int num5 = ClampTrust(num + num2);
		Logger.Log("Trust", $"npc={npc.StringId} reason={reason} personal={npcTrust}->{num} rawDelta={personalDelta} appliedDelta={FormatTrustUnits(appliedUnits)} public={publicTrust}->{num2} deltaPublic={num3} requestedPublicDelta={publicDelta} effective={num4}->{num5}");
		Logger.Obs("Trust", "change", new Dictionary<string, object>
		{
			["npcId"] = npc.StringId ?? "",
			["reason"] = reason ?? "",
			["personalBefore"] = npcTrust,
			["personalAfter"] = num,
			["publicBefore"] = publicTrust,
			["publicAfter"] = num2,
			["effectiveBefore"] = num4,
			["effectiveAfter"] = num5,
			["personalDelta"] = personalDelta,
			["appliedPersonalDelta"] = FormatTrustUnits(appliedUnits),
			["publicDelta"] = num3,
			["requestedPublicDelta"] = publicDelta
		});
		Logger.Metric("trust.change");
		return num3;
	}

	private int AdjustTrustByExactUnits(Hero npc, int personalUnits, string reason, out int appliedUnits)
	{
		appliedUnits = 0;
		if (npc == null || personalUnits == 0)
		{
			return 0;
		}
		if (_npcTrust == null)
		{
			_npcTrust = new Dictionary<string, int>();
		}
		string trustKey = BuildNpcTrustKey(npc);
		if (string.IsNullOrWhiteSpace(trustKey))
		{
			return 0;
		}
		int trustBefore = GetNpcTrust(npc);
		int wholeDelta = ApplyExactDirectTrustDeltaUnits(trustKey, trustBefore, personalUnits, out appliedUnits);
		int trustAfter = ClampTrust(trustBefore + wholeDelta);
		if (trustAfter == 0)
		{
			_npcTrust.Remove(trustKey);
		}
		else
		{
			_npcTrust[trustKey] = trustAfter;
		}
		int publicDelta = ApplyPublicTrustPoolDeltaByKey(BuildPublicTrustKey(npc), appliedUnits, (reason ?? "exact_units") + "_public_pool");
		Logger.Log("Trust", $"npc={npc.StringId} reason={reason} personal={trustBefore}->{trustAfter} requestedUnits={personalUnits} appliedDelta={FormatTrustUnits(appliedUnits)} publicDelta={publicDelta}");
		return publicDelta;
	}

	public int AdjustPersonalTrustWholeDeltaForExternal(Hero npc, int exactDelta, string reason = "external_direct_whole")
	{
		if (npc == null || exactDelta == 0)
		{
			return 0;
		}
		if (exactDelta > 0 && IsPrisonerTrustGainBlocked(npc))
		{
			Logger.Log("Trust", $"npc={npc.StringId} reason={reason} blocked=prisoner positiveExactDelta={exactDelta}");
			return 0;
		}
		if (_npcTrust == null)
		{
			_npcTrust = new Dictionary<string, int>();
		}
		string text = BuildNpcTrustKey(npc);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		int npcTrust = GetNpcTrust(npc);
		int publicTrust = GetPublicTrust(npc);
		int num = ClampTrust(npcTrust + exactDelta);
		int num2 = num - npcTrust;
		if (num2 == 0)
		{
			return 0;
		}
		if (num == 0)
		{
			_npcTrust.Remove(text);
		}
		else
		{
			_npcTrust[text] = num;
		}
		int num3 = ClampTrust(npcTrust + publicTrust);
		int num4 = ClampTrust(num + publicTrust);
		Logger.Log("Trust", $"npc={npc.StringId} reason={reason} personal={npcTrust}->{num} exactDelta={exactDelta} appliedExactDelta={num2} public={publicTrust}->{publicTrust} effective={num3}->{num4}");
		Logger.Obs("Trust", "change", new Dictionary<string, object>
		{
			["npcId"] = npc.StringId ?? "",
			["reason"] = reason ?? "",
			["personalBefore"] = npcTrust,
			["personalAfter"] = num,
			["publicBefore"] = publicTrust,
			["publicAfter"] = publicTrust,
			["effectiveBefore"] = num3,
			["effectiveAfter"] = num4,
			["personalDelta"] = exactDelta,
			["appliedPersonalDelta"] = num2.ToString(),
			["publicDelta"] = 0,
			["requestedPublicDelta"] = 0
		});
		Logger.Metric("trust.change");
		return num2;
	}

	public void AdjustTrustForExternal(Hero npc, int personalDelta, int publicDelta, string reason = "external")
	{
		AdjustTrust(npc, personalDelta, publicDelta, reason ?? "external", out _);
	}

	public void AdjustSettlementMerchantTrustForExternal(Settlement settlement, SettlementMerchantKind kind, int personalDelta, string reason = "external")
	{
		AdjustSettlementMerchantTrust(settlement, kind, personalDelta, reason ?? "external", out _);
	}

	public int GetSettlementLocalPublicTrust(Settlement settlement)
	{
		if (settlement == null)
		{
			return 0;
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		string settlementPublicTrustKey = BuildSettlementLocalPublicTrustKey(settlement);
		if (string.IsNullOrWhiteSpace(settlementPublicTrustKey))
		{
			return 0;
		}
		if (_publicTrust.TryGetValue(settlementPublicTrustKey, out var value))
		{
			return ClampTrust(value);
		}
		return 0;
	}

	public int GetSettlementPublicTrust(Settlement settlement)
	{
		return GetSettlementLocalPublicTrust(settlement);
	}

	public int GetSettlementSharedPublicTrust(Settlement settlement)
	{
		if (settlement == null)
		{
			return 0;
		}
		if (_publicTrust == null)
		{
			_publicTrust = new Dictionary<string, int>();
		}
		string settlementSharedPublicTrustKey = BuildSettlementSharedPublicTrustKey(settlement);
		if (string.IsNullOrWhiteSpace(settlementSharedPublicTrustKey))
		{
			return 0;
		}
		if (_publicTrust.TryGetValue(settlementSharedPublicTrustKey, out var value))
		{
			return ClampTrust(value);
		}
		return 0;
	}

	public int GetSettlementFactionPublicTrust(Settlement settlement)
	{
		return GetSettlementSharedPublicTrust(settlement);
	}

	public int GetSettlementMerchantEffectiveTrust(Settlement settlement, SettlementMerchantKind kind)
	{
		return ClampTrust(GetSettlementMerchantTrust(settlement, kind) + GetSettlementLocalPublicTrust(settlement) + GetSettlementSharedPublicTrust(settlement));
	}

	public void AdjustSettlementLocalPublicTrustForExternal(Settlement settlement, int publicDelta, string reason = "external")
	{
		AdjustSettlementLocalTrustInternal(settlement, publicDelta, reason);
	}

	public void AdjustSettlementPublicTrustForExternal(Settlement settlement, int publicDelta, string reason = "external")
	{
		AdjustSettlementLocalPublicTrustForExternal(settlement, publicDelta, reason);
	}

	private void AdjustSettlementSharedPublicTrust(Settlement settlement, int publicDelta, string reason)
	{
		if (settlement == null || publicDelta == 0)
		{
			return;
		}
		string settlementSharedPublicTrustKey = BuildSettlementSharedPublicTrustKey(settlement);
		if (string.IsNullOrWhiteSpace(settlementSharedPublicTrustKey))
		{
			return;
		}
		AdjustPublicTrustByKey(settlementSharedPublicTrustKey, publicDelta, reason);
	}

	private void AdjustSettlementFactionPublicTrust(Settlement settlement, int publicDelta, string reason)
	{
		AdjustSettlementSharedPublicTrust(settlement, publicDelta, reason);
	}

	private void AdjustRelationWithPlayer(Hero npc, int delta, string reason)
	{
		if (npc == null || delta == 0 || Hero.MainHero == null)
		{
			return;
		}
		try
		{
			if (RomanceSystemBehavior.TryGetPrivateLoveAsPlayerRelation(npc, out var privateRelation))
			{
				RomanceSystemBehavior.Instance?.AdjustPrivateLove(npc, delta, (reason ?? "relation_change") + "_private_relation_redirect");
				int privateRelation2 = RomanceSystemBehavior.Instance?.GetPrivateLove(npc) ?? privateRelation;
				Logger.Log("Trust", $"npc={npc.StringId} relation_reason={reason} relation_private_redirect={privateRelation}->{privateRelation2} delta={delta}");
				Logger.Obs("Relation", "change_private_redirect", new Dictionary<string, object>
				{
					["npcId"] = npc.StringId ?? "",
					["reason"] = reason ?? "",
					["before"] = privateRelation,
					["after"] = privateRelation2,
					["delta"] = delta
				});
				Logger.Metric("relation.change_private_redirect");
				return;
			}
			int relation = npc.GetRelation(Hero.MainHero);
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, npc, delta);
			int relation2 = npc.GetRelation(Hero.MainHero);
			Logger.Log("Trust", $"npc={npc.StringId} relation_reason={reason} relation={relation}->{relation2} delta={delta}");
			Logger.Obs("Relation", "change", new Dictionary<string, object>
			{
				["npcId"] = npc.StringId ?? "",
				["reason"] = reason ?? "",
				["before"] = relation,
				["after"] = relation2,
				["delta"] = delta
			});
			Logger.Metric("relation.change");
		}
		catch (Exception ex)
		{
			Logger.Log("Trust", "[WARN] relation adjust failed: " + ex.Message);
			Logger.Obs("Relation", "change_error", new Dictionary<string, object>
			{
				["npcId"] = npc.StringId ?? "",
				["reason"] = reason ?? "",
				["delta"] = delta,
				["message"] = ex.Message
			});
			Logger.Metric("relation.change", ok: false);
		}
	}

	private void OnDailyTick()
	{
		try
		{
			RemoveGeneratedRewardItemsFromMarketRosters("daily_tick");
			RestoreDueNpcBattleEquipment("daily_tick");
			if (_debts == null || _debts.Count <= 0)
			{
				return;
			}
			float nowCampaignDay = GetNowCampaignDay();
			int campaignDayIndex = GetCampaignDayIndex();
			CleanupPendingPlayerTransfers(campaignDayIndex);
			List<string> list = _debts.Keys.ToList();
			foreach (string item in list)
			{
				if (string.IsNullOrWhiteSpace(item) || !_debts.TryGetValue(item, out var value) || value == null)
				{
					continue;
				}
				NormalizeDebtRecord(value);
				if (value.DebtLines == null || value.DebtLines.Count <= 0)
				{
					continue;
				}
				Hero hero = null;
				try
				{
					hero = Hero.Find(item);
				}
				catch
				{
					hero = null;
				}
				if (hero == null)
				{
					if (!TryParseSettlementMerchantDebtKey(item, out var settlementId, out var kind))
					{
						continue;
					}
					Settlement settlement = ResolveSettlementById(settlementId);
					if (settlement == null)
					{
						continue;
					}
					for (int j = 0; j < value.DebtLines.Count; j++)
					{
						DebtRecord.DebtLine debtLine2 = value.DebtLines[j];
						if (debtLine2 == null || debtLine2.RemainingAmount <= 0)
						{
							continue;
						}
						if (debtLine2.IsDueUnlimited)
						{
							int unlimitedDebtValue = EstimateDebtLineRemainingValueForSettlement(settlement, debtLine2);
							int penaltyUnits = ConsumeUnlimitedDebtTrustPenaltyUnits(debtLine2, unlimitedDebtValue, campaignDayIndex);
							if (penaltyUnits > 0)
							{
								int publicDelta = AdjustSettlementMerchantTrustByExactUnits(settlement, kind, -penaltyUnits, "merchant_unlimited_debt_daily_penalty", out var appliedUnits);
								Logger.Log("Trust", $"[UnlimitedDebtPenalty] settlement={settlement.StringId} market={kind} debtId={debtLine2.DebtId} value={unlimitedDebtValue} personal={FormatTrustUnits(appliedUnits)} public={publicDelta}");
							}
							continue;
						}
						if ((!debtLine2.IsGold && debtLine2.IsItemUnavailableDeclared) || debtLine2.DueDay <= 0f || nowCampaignDay <= debtLine2.DueDay + 0.01f)
						{
							continue;
						}
						int num = ComputeOverdueElapsedWeeks(nowCampaignDay, debtLine2.DueDay);
						int num2 = Math.Max(0, debtLine2.OverduePenaltyDaysApplied);
						if (num > num2 && !(debtLine2.BestPreDueCoverage >= 0.95f))
						{
							for (int k = num2 + 1; k <= num; k++)
							{
								int num3 = EstimateDebtLineRemainingValueForSettlement(settlement, debtLine2);
								int num4 = ComputeWeeklyOverdueTrustPenaltyByDebtValue(num3);
								int num5 = 0;
								if (num4 > 0)
								{
									num5 = AdjustSettlementMerchantTrust(settlement, kind, -num4, "merchant_overdue_weekly_penalty_by_amount", out _);
								}
								Logger.Log("Trust", string.Format("[OverduePenalty] settlement={0} market={1} debtId={2} mode={3} value={4} trust={5} public={6} week={7}/{8}", settlement.StringId, kind, debtLine2.DebtId, "amount_weekly", num3, num4, num5, k, OverduePenaltyMaxWeeks));
							}
							debtLine2.OverduePenaltyDaysApplied = num;
							debtLine2.LastOverduePenaltyDay = campaignDayIndex;
						}
					}
					NormalizeDebtRecord(value);
					if (!HasDebtContent(value))
					{
						_debts.Remove(item);
						continue;
					}
					// The persistent quest journal replaces recurring merchant debt pop-ups; overdue penalties above still apply.
					continue;
				}
				for (int i = 0; i < value.DebtLines.Count; i++)
				{
					DebtRecord.DebtLine debtLine = value.DebtLines[i];
					if (debtLine == null || debtLine.RemainingAmount <= 0)
					{
						continue;
					}
					if (debtLine.IsDueUnlimited)
					{
						int unlimitedDebtValue = EstimateDebtLineRemainingValue(hero, debtLine);
						int penaltyUnits = ConsumeUnlimitedDebtTrustPenaltyUnits(debtLine, unlimitedDebtValue, campaignDayIndex);
						if (penaltyUnits > 0)
						{
							int publicDelta = AdjustTrustByExactUnits(hero, -penaltyUnits, "unlimited_debt_daily_penalty", out var appliedUnits);
							Logger.Log("Trust", $"[UnlimitedDebtPenalty] npc={hero.StringId} debtId={debtLine.DebtId} value={unlimitedDebtValue} personal={FormatTrustUnits(appliedUnits)} public={publicDelta}");
						}
						continue;
					}
					if ((!debtLine.IsGold && debtLine.IsItemUnavailableDeclared) || debtLine.DueDay <= 0f || nowCampaignDay <= debtLine.DueDay + 0.01f)
					{
						continue;
					}
					int num = ComputeOverdueElapsedWeeks(nowCampaignDay, debtLine.DueDay);
					int num2 = Math.Max(0, debtLine.OverduePenaltyDaysApplied);
					if (num > num2 && !(debtLine.BestPreDueCoverage >= 0.95f))
					{
						for (int k = num2 + 1; k <= num; k++)
						{
							int num3 = EstimateDebtLineRemainingValue(hero, debtLine);
							int num4 = ComputeWeeklyOverdueTrustPenaltyByDebtValue(num3);
							int num5 = 0;
							if (num4 > 0)
							{
								num5 = AdjustTrust(hero, -num4, 0, "overdue_weekly_penalty_by_amount", out _);
							}
							int num6 = ComputeWeeklyOverdueRelationPenaltyDelta(k - 1, k, num4);
							if (num6 > 0)
							{
								AdjustRelationWithPlayer(hero, -num6, "overdue_weekly_penalty_by_amount");
							}
							Logger.Log("Trust", string.Format("[OverduePenalty] npc={0} debtId={1} mode={2} value={3} trustPersonal={4} trustPublic={5} relation={6} week={7}/{8}", hero.StringId, debtLine.DebtId, "amount_weekly", num3, num4, num5, num6, k, OverduePenaltyMaxWeeks));
						}
						debtLine.OverduePenaltyDaysApplied = num;
						debtLine.LastOverduePenaltyDay = campaignDayIndex;
					}
				}
				NormalizeDebtRecord(value);
				if (!HasDebtContent(value))
				{
					_debts.Remove(item);
					continue;
				}
				// The persistent quest journal replaces recurring hero debt pop-ups; overdue penalties above still apply.
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Trust", "[WARN] OnDailyTick overdue penalty failed: " + ex.Message);
		}
	}

	private void RestoreDueNpcBattleEquipment(string source)
	{
		if (_pendingNpcBattleEquipmentRestoreRecords == null || _pendingNpcBattleEquipmentRestoreRecords.Count == 0)
		{
			return;
		}
		if (!DuelSettings.IsNpcBattleEquipmentRestoreEnabled())
		{
			return;
		}
		float nowCampaignDay = GetNowCampaignDay();
		foreach (KeyValuePair<string, PendingNpcBattleEquipmentRestoreRecord> pending in _pendingNpcBattleEquipmentRestoreRecords.ToList())
		{
			string heroId = (pending.Key ?? "").Trim();
			PendingNpcBattleEquipmentRestoreRecord record = NormalizePendingNpcBattleEquipmentRestoreRecord(heroId, pending.Value);
			if (record == null)
			{
				_pendingNpcBattleEquipmentRestoreRecords.Remove(pending.Key);
				continue;
			}
			Hero hero = null;
			try
			{
				hero = Hero.Find(heroId);
			}
			catch
			{
				hero = null;
			}
			if (hero == null || hero == Hero.MainHero || hero.IsDead || hero.BattleEquipment == null)
			{
				_pendingNpcBattleEquipmentRestoreRecords.Remove(pending.Key);
				Logger.Log("RewardSystem", "[NpcEquipmentRestore] discarded hero=" + heroId + " reason=" + ((hero == null) ? "hero_unavailable" : ((hero.IsDead) ? "hero_dead" : "not_restorable")) + " source=" + (source ?? ""));
				continue;
			}
			List<PendingNpcBattleEquipmentRestoreSlot> remainingSlots = new List<PendingNpcBattleEquipmentRestoreSlot>(record.Slots.Count);
			foreach (PendingNpcBattleEquipmentRestoreSlot slot in record.Slots)
			{
				if (slot == null || !IsValidNpcBattleEquipmentRestoreSlot(slot.SlotIndex))
				{
					continue;
				}
				if (nowCampaignDay + 0.01f < slot.RestoreOnOrAfterDay)
				{
					remainingSlots.Add(slot);
					continue;
				}
				EquipmentIndex equipmentIndex = (EquipmentIndex)slot.SlotIndex;
				ItemObject currentItem = hero.BattleEquipment[equipmentIndex].Item;
				if (currentItem != null && currentItem != DefaultItems.Trash)
				{
					Logger.Log("RewardSystem", "[NpcEquipmentRestore] skipped hero=" + heroId + " slot=" + slot.SlotIndex.ToString(CultureInfo.InvariantCulture) + " reason=slot_already_filled source=" + (source ?? ""));
					continue;
				}
				ItemObject item = ResolveItemById(slot.ItemId);
				if (item == null || slot.IsQuestItem || item.IsBannerItem || !Equipment.IsItemFitsToSlot(equipmentIndex, item))
				{
					Logger.Log("RewardSystem", "[NpcEquipmentRestore] discarded hero=" + heroId + " slot=" + slot.SlotIndex.ToString(CultureInfo.InvariantCulture) + " item=" + (slot.ItemId ?? "") + " reason=item_unavailable_or_incompatible source=" + (source ?? ""));
					continue;
				}
				ItemModifier modifier = null;
				if (!string.IsNullOrWhiteSpace(slot.ModifierId))
				{
					try
					{
						modifier = Game.Current?.ObjectManager?.GetObject<ItemModifier>(slot.ModifierId);
					}
					catch
					{
						modifier = null;
					}
					if (modifier == null)
					{
						Logger.Log("RewardSystem", "[NpcEquipmentRestore] modifier fallback hero=" + heroId + " slot=" + slot.SlotIndex.ToString(CultureInfo.InvariantCulture) + " modifier=" + slot.ModifierId);
					}
				}
				ItemObject cosmeticItem = null;
				if (!string.IsNullOrWhiteSpace(slot.CosmeticItemId))
				{
					cosmeticItem = ResolveItemById(slot.CosmeticItemId);
					if (cosmeticItem == null)
					{
						Logger.Log("RewardSystem", "[NpcEquipmentRestore] cosmetic fallback hero=" + heroId + " slot=" + slot.SlotIndex.ToString(CultureInfo.InvariantCulture) + " cosmetic=" + slot.CosmeticItemId);
					}
				}
				try
				{
					hero.BattleEquipment[equipmentIndex] = new EquipmentElement(item, modifier, cosmeticItem, slot.IsQuestItem);
					Logger.Log("RewardSystem", "[NpcEquipmentRestore] restored hero=" + heroId + " slot=" + slot.SlotIndex.ToString(CultureInfo.InvariantCulture) + " item=" + (item.StringId ?? "") + " source=" + (source ?? ""));
				}
				catch (Exception ex)
				{
					remainingSlots.Add(slot);
					Logger.Log("RewardSystem", "[NpcEquipmentRestore] restore failed hero=" + heroId + " slot=" + slot.SlotIndex.ToString(CultureInfo.InvariantCulture) + " error=" + ex.Message);
				}
			}
			if (remainingSlots.Count == 0)
			{
				_pendingNpcBattleEquipmentRestoreRecords.Remove(pending.Key);
			}
			else
			{
				record.Slots = remainingSlots;
				_pendingNpcBattleEquipmentRestoreRecords[pending.Key] = record;
			}
		}
	}

	private void NormalizeDebtRecord(DebtRecord rec)
	{
		if (rec == null)
		{
			return;
		}
		float nowCampaignDay = GetNowCampaignDay();
		if (rec.DebtLines == null)
		{
			rec.DebtLines = new List<DebtRecord.DebtLine>();
		}
		if (rec.OwedItems == null)
		{
			rec.OwedItems = new Dictionary<string, int>();
		}
		if (rec.DebtLines.Count == 0)
		{
			if (rec.OwedGold > 0)
			{
				float num = ((rec.CreatedDay > 0f) ? rec.CreatedDay : nowCampaignDay);
				float dueDay = ((rec.DueDay > 0f) ? rec.DueDay : (num + 1f));
				rec.DebtLines.Add(new DebtRecord.DebtLine
				{
					DebtId = BuildDebtId(),
					IsGold = true,
					ItemId = null,
					IsDueUnlimited = false,
					IsItemUnavailableDeclared = false,
					InitialAmount = rec.OwedGold,
					RemainingAmount = rec.OwedGold,
					CreatedDay = num,
					DueDay = dueDay,
					BestPreDueCoverage = 0f,
					OnTimePenaltyTierApplied = 0,
					OverduePenaltyDaysApplied = 0,
					LastOverduePenaltyDay = -1,
					OverdueTrustPenaltyPerDay = 0,
					OverdueRelationPenaltyPerDay = 0,
					CompensationUnitPrice = 0,
					CompensationGoldCredit = 0
				});
			}
			foreach (KeyValuePair<string, int> owedItem in rec.OwedItems)
			{
				if (!string.IsNullOrWhiteSpace(owedItem.Key) && owedItem.Value > 0)
				{
					float num2 = ((rec.CreatedDay > 0f) ? rec.CreatedDay : nowCampaignDay);
					float dueDay2 = ((rec.DueDay > 0f) ? rec.DueDay : (num2 + 1f));
					rec.DebtLines.Add(new DebtRecord.DebtLine
					{
						DebtId = BuildDebtId(),
						IsGold = false,
						ItemId = owedItem.Key,
						IsDueUnlimited = false,
						IsItemUnavailableDeclared = false,
						InitialAmount = owedItem.Value,
						RemainingAmount = owedItem.Value,
						CreatedDay = num2,
						DueDay = dueDay2,
						BestPreDueCoverage = 0f,
						OnTimePenaltyTierApplied = 0,
						OverduePenaltyDaysApplied = 0,
						LastOverduePenaltyDay = -1,
						OverdueTrustPenaltyPerDay = 0,
						OverdueRelationPenaltyPerDay = 0,
						CompensationUnitPrice = 0,
						CompensationGoldCredit = 0
					});
				}
			}
		}
		List<DebtRecord.DebtLine> list = new List<DebtRecord.DebtLine>();
		for (int i = 0; i < rec.DebtLines.Count; i++)
		{
			DebtRecord.DebtLine debtLine = rec.DebtLines[i];
			if (debtLine == null)
			{
				continue;
			}
			debtLine.RemainingAmount = Math.Max(0, debtLine.RemainingAmount);
			if (debtLine.RemainingAmount > 0 && (debtLine.IsGold || !string.IsNullOrWhiteSpace(debtLine.ItemId)))
			{
				if (string.IsNullOrWhiteSpace(debtLine.DebtId))
				{
					debtLine.DebtId = BuildDebtId();
				}
				if (debtLine.InitialAmount <= 0)
				{
					debtLine.InitialAmount = debtLine.RemainingAmount;
				}
				if (debtLine.InitialAmount < debtLine.RemainingAmount)
				{
					debtLine.InitialAmount = debtLine.RemainingAmount;
				}
				if (debtLine.CreatedDay <= 0f)
				{
					debtLine.CreatedDay = nowCampaignDay;
				}
				if (debtLine.IsGold)
				{
					debtLine.IsItemUnavailableDeclared = false;
				}
				if (debtLine.IsDueUnlimited)
				{
					debtLine.DueDay = 0f;
				}
				else if (debtLine.DueDay <= 0f)
				{
					debtLine.DueDay = debtLine.CreatedDay + 1f;
				}
				debtLine.BestPreDueCoverage = Clamp01(debtLine.BestPreDueCoverage);
				debtLine.OnTimePenaltyTierApplied = Math.Max(0, Math.Min(5, debtLine.OnTimePenaltyTierApplied));
				debtLine.OverduePenaltyDaysApplied = Math.Max(0, Math.Min(OverduePenaltyMaxWeeks, debtLine.OverduePenaltyDaysApplied));
				if (debtLine.LastOverduePenaltyDay < -1)
				{
					debtLine.LastOverduePenaltyDay = -1;
				}
				debtLine.OverdueTrustPenaltyPerDay = NormalizeLlmPenaltyValue(debtLine.OverdueTrustPenaltyPerDay);
				debtLine.OverdueRelationPenaltyPerDay = NormalizeLlmPenaltyValue(debtLine.OverdueRelationPenaltyPerDay);
				debtLine.CompensationUnitPrice = Math.Max(0, debtLine.CompensationUnitPrice);
				debtLine.CompensationGoldCredit = Math.Max(0, debtLine.CompensationGoldCredit);
				debtLine.UnlimitedTrustPenaltyNumeratorCarry = Math.Max(0L, Math.Min(UnlimitedDebtPenaltyReferenceValue - 1L, debtLine.UnlimitedTrustPenaltyNumeratorCarry));
				debtLine.DebtNote = NormalizeDebtNote(debtLine.DebtNote);
				list.Add(debtLine);
			}
		}
		rec.DebtLines = list;
		rec.OwedGold = 0;
		rec.OwedItems = new Dictionary<string, int>();
		float num3 = 0f;
		float num4 = 0f;
		for (int j = 0; j < rec.DebtLines.Count; j++)
		{
			DebtRecord.DebtLine debtLine2 = rec.DebtLines[j];
			if (debtLine2 == null || debtLine2.RemainingAmount <= 0)
			{
				continue;
			}
			if (debtLine2.IsGold)
			{
				rec.OwedGold += debtLine2.RemainingAmount;
			}
			else
			{
				string text = debtLine2.ItemId ?? "";
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				if (rec.OwedItems.TryGetValue(text, out var value))
				{
					rec.OwedItems[text] = value + debtLine2.RemainingAmount;
				}
				else
				{
					rec.OwedItems[text] = debtLine2.RemainingAmount;
				}
			}
			if (num3 <= 0f || debtLine2.CreatedDay < num3)
			{
				num3 = debtLine2.CreatedDay;
			}
			if (!debtLine2.IsDueUnlimited && debtLine2.DueDay > 0f && (num4 <= 0f || debtLine2.DueDay < num4))
			{
				num4 = debtLine2.DueDay;
			}
		}
		rec.CreatedDay = num3;
		rec.DueDay = num4;
		if (!HasDebtContent(rec))
		{
			rec.CreatedDay = 0f;
			rec.DueDay = 0f;
		}
	}

	private static string BuildDebtDueStatusText(float dueDay, bool isDueUnlimited = false)
	{
		if (isDueUnlimited)
		{
			return "还款期限：无限期（债务仍有效）";
		}
		if (dueDay <= 0f)
		{
			return "";
		}
		float nowCampaignDay = GetNowCampaignDay();
		float num = dueDay - nowCampaignDay;
		int absDay = ToDisplayDay(dueDay);
		string text = FormatAbsDayAsGameDate(absDay);
		if (num > 0.01f)
		{
			int num2 = Math.Max(1, (int)Math.Ceiling(num));
			return $"还款期限：约 {num2} 天内（截止 {text}）";
		}
		if (num >= -0.01f)
		{
			return "还款期限：今日到期（" + text + "）";
		}
		int num3 = Math.Max(1, (int)Math.Ceiling(0f - num));
		return $"还款期限：已逾期 {num3} 天（截止 {text}）";
	}

	private static string BuildDebtPromiseDeadlineText(float dueDay, bool isDueUnlimited)
	{
		string text = BuildDebtDueStatusText(dueDay, isDueUnlimited);
		const string prefix = "还款期限：";
		if (text.StartsWith(prefix, StringComparison.Ordinal))
		{
			text = text.Substring(prefix.Length).Trim();
		}
		return string.IsNullOrWhiteSpace(text) ? "未设定" : text;
	}

	private static string NormalizeDebtNote(string note)
	{
		string text = (note ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		if (text.Length > 120)
		{
			text = text.Substring(0, 120);
		}
		return text;
	}

	private int EstimateDebtLineRemainingValue(Hero npc, DebtRecord.DebtLine line)
	{
		if (line == null || line.RemainingAmount <= 0)
		{
			return 0;
		}
		if (line.IsGold)
		{
			return Math.Max(0, line.RemainingAmount);
		}
		if (string.IsNullOrWhiteSpace(line.ItemId))
		{
			return 0;
		}
		int val = Math.Max(1, line.CompensationUnitPrice);
		if (line.CompensationUnitPrice <= 0)
		{
			ItemObject item = ResolveItemById(line.ItemId);
			ItemGuidePriceInfo guidePriceForItemNearHero = GetGuidePriceForItemNearHero(npc, item);
			val = Math.Max(1, guidePriceForItemNearHero.UnitPrice);
		}
		long num = (long)Math.Max(0, line.RemainingAmount) * (long)Math.Max(1, val);
		if (num <= 0)
		{
			return 0;
		}
		if (num > int.MaxValue)
		{
			return int.MaxValue;
		}
		return (int)num;
	}

	private int EstimateDebtLineRemainingValueForSettlement(Settlement settlement, DebtRecord.DebtLine line)
	{
		if (line == null || line.RemainingAmount <= 0)
		{
			return 0;
		}
		if (line.IsGold)
		{
			return Math.Max(0, line.RemainingAmount);
		}
		ItemObject item = ResolveItemById(line.ItemId);
		int val = Math.Max(1, line.CompensationUnitPrice);
		if (line.CompensationUnitPrice <= 0)
		{
			try
			{
				if (settlement != null && item != null && TryGetSettlementBuyPrice(settlement, item, out var price))
				{
					val = Math.Max(1, price);
				}
				else
				{
					val = Math.Max(1, item?.Value ?? 1);
				}
			}
			catch
			{
				val = Math.Max(1, item?.Value ?? 1);
			}
		}
		long num = (long)Math.Max(0, line.RemainingAmount) * (long)Math.Max(1, val);
		if (num > int.MaxValue)
		{
			return int.MaxValue;
		}
		return Math.Max(0, (int)num);
	}

	private string BuildDailyDebtReminderText(Hero npc, DebtRecord rec, int campaignDayIndex, int maxLines = 2)
	{
		if (npc == null || rec == null)
		{
			return string.Empty;
		}
		NormalizeDebtRecord(rec);
		if (!HasDebtContent(rec))
		{
			return string.Empty;
		}
		List<DebtRecord.DebtLine> list = (from x in rec.DebtLines?.Where((DebtRecord.DebtLine x) => ShouldIncludeDebtLineInScheduledReminder(x, campaignDayIndex))
			orderby x.IsDueUnlimited ? 0 : 1, x.DueDay, x.CreatedDay
			select x).ToList() ?? new List<DebtRecord.DebtLine>();
		if (list.Count <= 0)
		{
			return string.Empty;
		}
		if (maxLines < 1)
		{
			maxLines = 1;
		}
		StringBuilder stringBuilder = new StringBuilder();
		string value = npc.Name?.ToString() ?? "该NPC";
		stringBuilder.Append("【承诺或欠款提醒】你对 ").Append(value).Append(" 的承诺或欠款：");
		int num = Math.Min(maxLines, list.Count);
		for (int num2 = 0; num2 < num; num2++)
		{
			DebtRecord.DebtLine debtLine = list[num2];
			int debtValue = EstimateDebtLineRemainingValue(npc, debtLine);
			string deadline = BuildDebtPromiseDeadlineText(debtLine.DueDay, debtLine.IsDueUnlimited);
			string note = string.IsNullOrWhiteSpace(debtLine.DebtNote) ? "无" : debtLine.DebtNote;
			stringBuilder.Append(" [ID:").Append(debtLine.DebtId).Append("] 承诺或欠款价值 ")
				.Append(debtValue)
				.Append(" 第纳尔，达成期限为：")
				.Append(deadline)
				.Append("，备注：")
				.Append(note);
			if (num2 < num - 1)
			{
				stringBuilder.Append("；");
			}
		}
		if (list.Count > num)
		{
			stringBuilder.Append("；...还有 ").Append(list.Count - num).Append(" 笔");
		}
		return stringBuilder.ToString();
	}

	private string BuildDailyMerchantDebtReminderText(Settlement settlement, SettlementMerchantKind kind, DebtRecord rec, int campaignDayIndex, int maxLines = 2)
	{
		if (settlement == null || kind == SettlementMerchantKind.None || rec == null)
		{
			return string.Empty;
		}
		NormalizeDebtRecord(rec);
		if (!HasDebtContent(rec))
		{
			return string.Empty;
		}
		List<DebtRecord.DebtLine> list = (from x in rec.DebtLines?.Where((DebtRecord.DebtLine x) => ShouldIncludeDebtLineInScheduledReminder(x, campaignDayIndex))
			orderby x.IsDueUnlimited ? 0 : 1, x.DueDay, x.CreatedDay
			select x).ToList() ?? new List<DebtRecord.DebtLine>();
		if (list.Count <= 0)
		{
			return string.Empty;
		}
		if (maxLines < 1)
		{
			maxLines = 1;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("【承诺或欠款提醒】你对 ").Append(BuildSettlementMerchantDebtLabel(settlement, kind)).Append(" 的承诺或欠款：");
		int num = Math.Min(maxLines, list.Count);
		for (int i = 0; i < num; i++)
		{
			DebtRecord.DebtLine debtLine = list[i];
			int debtValue = EstimateDebtLineRemainingValueForSettlement(settlement, debtLine);
			string deadline = BuildDebtPromiseDeadlineText(debtLine.DueDay, debtLine.IsDueUnlimited);
			string note = string.IsNullOrWhiteSpace(debtLine.DebtNote) ? "无" : debtLine.DebtNote;
			stringBuilder.Append(" [ID:").Append(debtLine.DebtId).Append("] 承诺或欠款价值 ")
				.Append(debtValue)
				.Append(" 第纳尔，达成期限为：")
				.Append(deadline)
				.Append("，备注：")
				.Append(note);
			if (i < num - 1)
			{
				stringBuilder.Append("；");
			}
		}
		if (list.Count > num)
		{
			stringBuilder.Append("；...还有 ").Append(list.Count - num).Append(" 笔");
		}
		return stringBuilder.ToString();
	}

	public Dictionary<string, DebtExportEntry> ExportDebtEntries()
	{
		Dictionary<string, DebtExportEntry> dictionary = new Dictionary<string, DebtExportEntry>();
		if (_debts == null)
		{
			return dictionary;
		}
		foreach (KeyValuePair<string, DebtRecord> debt in _debts)
		{
			if (string.IsNullOrEmpty(debt.Key) || debt.Value == null)
			{
				continue;
			}
			DebtRecord value = debt.Value;
			NormalizeDebtRecord(value);
			bool flag = value.OwedGold > 0;
			if (!flag && value.OwedItems != null)
			{
				foreach (KeyValuePair<string, int> owedItem in value.OwedItems)
				{
					if (owedItem.Value > 0)
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				continue;
			}
			DebtExportEntry debtExportEntry = new DebtExportEntry();
			debtExportEntry.OwedGold = Math.Max(0, value.OwedGold);
			debtExportEntry.OwedItems = new Dictionary<string, int>();
			debtExportEntry.CreatedDay = value.CreatedDay;
			debtExportEntry.DueDay = value.DueDay;
			debtExportEntry.DebtLines = new List<DebtLineExportEntry>();
			if (value.DebtLines != null)
			{
				for (int i = 0; i < value.DebtLines.Count; i++)
				{
					DebtRecord.DebtLine debtLine = value.DebtLines[i];
					if (debtLine != null && debtLine.RemainingAmount > 0)
					{
						debtExportEntry.DebtLines.Add(new DebtLineExportEntry
						{
							DebtId = debtLine.DebtId,
							IsGold = debtLine.IsGold,
							ItemId = debtLine.ItemId,
							IsDueUnlimited = debtLine.IsDueUnlimited,
							IsItemUnavailableDeclared = debtLine.IsItemUnavailableDeclared,
							InitialAmount = debtLine.InitialAmount,
							RemainingAmount = debtLine.RemainingAmount,
							CreatedDay = debtLine.CreatedDay,
							DueDay = debtLine.DueDay,
							BestPreDueCoverage = debtLine.BestPreDueCoverage,
							OnTimePenaltyTierApplied = debtLine.OnTimePenaltyTierApplied,
							OverduePenaltyDaysApplied = debtLine.OverduePenaltyDaysApplied,
							LastOverduePenaltyDay = debtLine.LastOverduePenaltyDay,
							OverdueTrustPenaltyPerDay = debtLine.OverdueTrustPenaltyPerDay,
							OverdueRelationPenaltyPerDay = debtLine.OverdueRelationPenaltyPerDay,
							CompensationUnitPrice = debtLine.CompensationUnitPrice,
							CompensationGoldCredit = debtLine.CompensationGoldCredit,
							UnlimitedTrustPenaltyNumeratorCarry = debtLine.UnlimitedTrustPenaltyNumeratorCarry,
							DebtNote = debtLine.DebtNote
						});
					}
				}
			}
			if (value.OwedItems != null)
			{
				foreach (KeyValuePair<string, int> owedItem2 in value.OwedItems)
				{
					if (!string.IsNullOrEmpty(owedItem2.Key) && owedItem2.Value > 0)
					{
						debtExportEntry.OwedItems[owedItem2.Key] = owedItem2.Value;
					}
				}
			}
			dictionary[debt.Key] = debtExportEntry;
		}
		return dictionary;
	}

	public void ImportDebtEntries(Dictionary<string, DebtExportEntry> entries)
	{
		if (entries == null)
		{
			return;
		}
		if (_debts == null)
		{
			_debts = new Dictionary<string, DebtRecord>();
		}
		_debts.Clear();
		foreach (KeyValuePair<string, DebtExportEntry> entry in entries)
		{
			if (string.IsNullOrEmpty(entry.Key) || entry.Value == null)
			{
				continue;
			}
			DebtExportEntry value = entry.Value;
			int num = Math.Max(0, value.OwedGold);
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			if (value.OwedItems != null)
			{
				foreach (KeyValuePair<string, int> owedItem in value.OwedItems)
				{
					if (!string.IsNullOrEmpty(owedItem.Key) && owedItem.Value > 0)
					{
						dictionary[owedItem.Key] = owedItem.Value;
					}
				}
			}
			bool flag = num > 0 || dictionary.Count > 0;
			bool flag2 = value.DebtLines != null && value.DebtLines.Count > 0;
			if (!flag && !flag2)
			{
				continue;
			}
			DebtRecord debtRecord = new DebtRecord();
			debtRecord.DebtLines = new List<DebtRecord.DebtLine>();
			if (flag2)
			{
				for (int i = 0; i < value.DebtLines.Count; i++)
				{
					DebtLineExportEntry debtLineExportEntry = value.DebtLines[i];
					if (debtLineExportEntry != null && debtLineExportEntry.RemainingAmount > 0)
					{
						debtRecord.DebtLines.Add(new DebtRecord.DebtLine
						{
							DebtId = debtLineExportEntry.DebtId,
							IsGold = debtLineExportEntry.IsGold,
							ItemId = debtLineExportEntry.ItemId,
							IsDueUnlimited = debtLineExportEntry.IsDueUnlimited,
							IsItemUnavailableDeclared = debtLineExportEntry.IsItemUnavailableDeclared,
							InitialAmount = debtLineExportEntry.InitialAmount,
							RemainingAmount = debtLineExportEntry.RemainingAmount,
							CreatedDay = debtLineExportEntry.CreatedDay,
							DueDay = debtLineExportEntry.DueDay,
							BestPreDueCoverage = debtLineExportEntry.BestPreDueCoverage,
							OnTimePenaltyTierApplied = debtLineExportEntry.OnTimePenaltyTierApplied,
							OverduePenaltyDaysApplied = debtLineExportEntry.OverduePenaltyDaysApplied,
							LastOverduePenaltyDay = debtLineExportEntry.LastOverduePenaltyDay,
							OverdueTrustPenaltyPerDay = debtLineExportEntry.OverdueTrustPenaltyPerDay,
							OverdueRelationPenaltyPerDay = debtLineExportEntry.OverdueRelationPenaltyPerDay,
							CompensationUnitPrice = debtLineExportEntry.CompensationUnitPrice,
							CompensationGoldCredit = debtLineExportEntry.CompensationGoldCredit,
							UnlimitedTrustPenaltyNumeratorCarry = debtLineExportEntry.UnlimitedTrustPenaltyNumeratorCarry,
							DebtNote = debtLineExportEntry.DebtNote
						});
					}
				}
			}
			else
			{
				debtRecord.OwedGold = num;
				debtRecord.OwedItems = dictionary;
				debtRecord.CreatedDay = value.CreatedDay;
				debtRecord.DueDay = value.DueDay;
			}
			NormalizeDebtRecord(debtRecord);
			_debts[entry.Key] = debtRecord;
		}
		// Imported active lines receive the same task reconciliation as debts restored from a campaign save.
		QueueDebtPromiseQuestsForActiveDebts();
	}

	public List<string> GetAllDebtorHeroIds()
	{
		List<string> list = new List<string>();
		if (_debts == null)
		{
			return list;
		}
		foreach (KeyValuePair<string, DebtRecord> debt in _debts)
		{
			DebtRecord value = debt.Value;
			if (value != null)
			{
				NormalizeDebtRecord(value);
				if (HasDebtContent(value) && !string.IsNullOrEmpty(debt.Key))
				{
					list.Add(debt.Key);
				}
			}
		}
		return list;
	}

	public int GetHeroGold(Hero hero)
	{
		return hero?.Gold ?? 0;
	}

	public int GetRewardPostprocessGoldForHero(Hero hero)
	{
		Settlement settlement = ResolveNotableMarketSettlement(hero);
		if (IsNotableMarketHero(hero, settlement))
		{
			return GetSettlementMarketTradeGold(settlement);
		}
		return GetHeroGold(hero);
	}

	public int GetSettlementMarketTradeGold(Settlement settlement = null)
	{
		SettlementComponent settlementMarketComponent = ResolveSettlementMarketComponent(settlement);
		return Math.Max(0, settlementMarketComponent?.Gold ?? 0);
	}

	private static bool IsSupportedSettlementMarket(Settlement settlement)
	{
		return settlement != null && (settlement.IsTown || settlement.IsVillage);
	}

	private static string GetSettlementMarketTypeLabel(Settlement settlement)
	{
		return settlement?.IsVillage == true ? "村庄" : "城镇";
	}

	private static string GetSettlementMarketItemNamePrefix(Settlement settlement)
	{
		return "[" + GetSettlementMarketTypeLabel(settlement) + "市场] ";
	}

	private static Settlement ResolveSettlementMarketSettlement(Settlement settlement = null)
	{
		try
		{
			if (IsSupportedSettlementMarket(settlement))
			{
				return settlement;
			}
			settlement = Settlement.CurrentSettlement;
			if (IsSupportedSettlementMarket(settlement))
			{
				return settlement;
			}
		}
		catch
		{
		}
		try
		{
			settlement = PlayerEncounter.EncounterSettlement;
			if (IsSupportedSettlementMarket(settlement))
			{
				return settlement;
			}
		}
		catch
		{
		}
		try
		{
			settlement = MobileParty.MainParty?.CurrentSettlement;
			if (IsSupportedSettlementMarket(settlement))
			{
				return settlement;
			}
		}
		catch
		{
		}
		return null;
	}

	private static SettlementComponent ResolveSettlementMarketComponent(Settlement settlement = null)
	{
		Settlement settlement2 = ResolveSettlementMarketSettlement(settlement);
		if (settlement2 == null)
		{
			return null;
		}
		return settlement2.Town != null ? settlement2.Town : settlement2.SettlementComponent;
	}

	private static bool TryResolveHeroMapOrigin(Hero hero, out Vec2 origin)
	{
		origin = Vec2.Invalid;
		try
		{
			if (hero?.CurrentSettlement != null && hero.CurrentSettlement.GatePosition.IsValid())
			{
				origin = hero.CurrentSettlement.GatePosition.ToVec2();
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (hero?.PartyBelongedTo != null && hero.PartyBelongedTo.Position.IsValid())
			{
				origin = hero.PartyBelongedTo.Position.ToVec2();
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (MobileParty.MainParty != null && MobileParty.MainParty.Position.IsValid())
			{
				origin = MobileParty.MainParty.Position.ToVec2();
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static ItemObject ResolveItemById(string itemId)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(itemId))
			{
				return null;
			}
			string key = itemId.Trim();
			if (IsGeneratedRewardItemStringId(key) && TryResolveGeneratedRewardItemForStringId(key, out var generatedItem, "resolve_item_id"))
			{
				return generatedItem;
			}
			return Game.Current?.ObjectManager?.GetObject<ItemObject>(key);
		}
		catch
		{
			return null;
		}
	}

	private static bool TryGetSettlementBuyPrice(Settlement settlement, ItemObject item, out int price)
	{
		price = 0;
		try
		{
			if (settlement == null || item == null)
			{
				return false;
			}
			SettlementComponent settlementComponent = settlement.SettlementComponent;
			if (settlementComponent == null)
			{
				return false;
			}
			MobileParty mainParty = MobileParty.MainParty;
			price = settlementComponent.GetItemPrice(item, mainParty);
			if (price > 0)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool TryGetSettlementBuyPrice(Settlement settlement, EquipmentElement equipmentElement, out int price)
	{
		price = 0;
		if (equipmentElement.Item == null)
		{
			return false;
		}
		if (!TryGetSettlementBuyPrice(settlement, equipmentElement.Item, out price))
		{
			return false;
		}
		ItemModifier itemModifier = equipmentElement.ItemModifier;
		if (itemModifier != null)
		{
			price = Math.Max(1, (int)Math.Round((float)price * itemModifier.PriceMultiplier, MidpointRounding.AwayFromZero));
		}
		return price > 0;
	}

	private static int ApplyItemModifierPriceMultiplier(int unitPrice, EquipmentElement equipmentElement)
	{
		int num = Math.Max(1, unitPrice);
		try
		{
			ItemModifier itemModifier = equipmentElement.ItemModifier;
			if (itemModifier != null)
			{
				num = Math.Max(1, (int)Math.Round((float)num * itemModifier.PriceMultiplier, MidpointRounding.AwayFromZero));
			}
		}
		catch
		{
		}
		return num;
	}

	private int GetGuidePriceForRewardItem(Hero hero, ItemObject item, EquipmentElement equipmentElement)
	{
		try
		{
			if (item == null)
			{
				return 0;
			}
			ItemGuidePriceInfo guidePriceForItemNearHero = GetGuidePriceForItemNearHero(hero ?? Hero.MainHero, item);
			return ApplyItemModifierPriceMultiplier(Math.Max(1, guidePriceForItemNearHero.UnitPrice), equipmentElement);
		}
		catch
		{
			return Math.Max(1, item?.Value ?? 1);
		}
	}

	private static bool MatchesItemLookupToken(EquipmentElement equipmentElement, string itemToken)
	{
		ItemObject item = equipmentElement.Item;
		if (item == null || string.IsNullOrWhiteSpace(itemToken))
		{
			return false;
		}
		string text = itemToken.Trim();
		if (string.Equals(item.StringId ?? "", text, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (TryParseSettlementMerchantPromptStringId(text, out var itemId, out var modifierId) && !string.IsNullOrWhiteSpace(modifierId))
		{
			return string.Equals(item.StringId ?? "", itemId, StringComparison.OrdinalIgnoreCase) && string.Equals(equipmentElement.ItemModifier?.StringId ?? "", modifierId, StringComparison.OrdinalIgnoreCase);
		}
		string modifiedName = equipmentElement.GetModifiedItemName()?.ToString();
		if (!string.IsNullOrWhiteSpace(modifiedName) && string.Equals(modifiedName.Trim(), text, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		string itemName = item.Name?.ToString();
		return !string.IsNullOrWhiteSpace(itemName) && string.Equals(itemName.Trim(), text, StringComparison.OrdinalIgnoreCase);
	}

	private static string BuildSettlementMerchantInventoryKey(EquipmentElement equipmentElement)
	{
		string text = equipmentElement.Item?.StringId ?? "";
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string text2 = BuildSettlementMerchantModifierKey(equipmentElement);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		return text + "@" + text2;
	}

	private static string BuildSettlementMerchantModifierKey(EquipmentElement equipmentElement)
	{
		if (equipmentElement.ItemModifier == null)
		{
			return "";
		}
		string text = equipmentElement.ItemModifier.StringId ?? "";
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		string text2 = BuildSettlementMerchantDisplayName(equipmentElement);
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = equipmentElement.ItemModifier.Name?.ToString() ?? "";
		}
		return string.IsNullOrWhiteSpace(text2) ? "" : ("name_" + StablePromptKeyHash(text2.Trim()));
	}

	private static string StablePromptKeyHash(string text)
	{
		unchecked
		{
			uint num = 2166136261u;
			foreach (char c in text ?? "")
			{
				num ^= c;
				num *= 16777619u;
			}
			return num.ToString("X8");
		}
	}

	private static string BuildSettlementMerchantDisplayName(EquipmentElement equipmentElement)
	{
		if (equipmentElement.Item == null)
		{
			return "";
		}
		return equipmentElement.GetModifiedItemName()?.ToString() ?? equipmentElement.Item.Name?.ToString() ?? equipmentElement.Item.StringId ?? "";
	}

	private static string FormatRewardItemResolutionScore(float score)
	{
		return score.ToString("0.0000", CultureInfo.InvariantCulture);
	}

	private static bool TryResolveGeneratedRpEquipmentSuffix(string assetName, out GeneratedRpEquipmentKind kind, out string matchedSuffix)
	{
		kind = GeneratedRpEquipmentKind.None;
		matchedSuffix = "";
		string text = (assetName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		text = text.TrimEnd(GeneratedRpTrailingPunctuation).TrimEnd();
		int bestSuffixLength = -1;
		foreach (GeneratedRpEquipmentSuffixRule rule in GeneratedRpEquipmentSuffixRules)
		{
			if (rule == null || rule.Kind == GeneratedRpEquipmentKind.None)
			{
				continue;
			}
			foreach (string suffix in rule.Suffixes)
			{
				if (!MatchesGeneratedRpSuffix(text, suffix, rule.RequiresEnglishWordBoundary)
					|| IsGeneratedRpEquipmentNonEquipmentEndingException(text)
					|| suffix.Length <= bestSuffixLength)
				{
					continue;
				}
				kind = rule.Kind;
				matchedSuffix = suffix;
				bestSuffixLength = suffix.Length;
			}
		}
		return bestSuffixLength >= 0;
	}

	private static bool IsGeneratedRpEquipmentNonEquipmentEndingException(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		foreach (string exception in GeneratedRpEquipmentNonEquipmentEndingExceptions)
		{
			if (!string.IsNullOrWhiteSpace(exception)
				&& text.EndsWith(exception, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static bool TryResolveGeneratedRpFoodSuffix(string assetName, out GeneratedRpFoodKind kind, out string matchedSuffix)
	{
		kind = GeneratedRpFoodKind.None;
		matchedSuffix = "";
		string text = (assetName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		text = text.TrimEnd(GeneratedRpTrailingPunctuation).TrimEnd();
		int bestSuffixLength = -1;
		foreach (GeneratedRpFoodSuffixRule rule in GeneratedRpFoodSuffixRules)
		{
			if (rule == null || rule.Kind == GeneratedRpFoodKind.None)
			{
				continue;
			}
			foreach (string suffix in rule.Suffixes)
			{
				if (!MatchesGeneratedRpSuffix(text, suffix, rule.RequiresEnglishWordBoundary)
					|| IsGeneratedRpFoodNonFoodEndingException(text, suffix)
					|| suffix.Length <= bestSuffixLength)
				{
					continue;
				}
				kind = rule.Kind;
				matchedSuffix = suffix;
				bestSuffixLength = suffix.Length;
			}
		}
		return bestSuffixLength >= 0;
	}

	private static bool IsGeneratedRpFoodNonFoodEndingException(string text, string matchedSuffix)
	{
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(matchedSuffix))
		{
			return false;
		}
		foreach (string exception in GeneratedRpFoodNonFoodEndingExceptions)
		{
			if (!string.IsNullOrWhiteSpace(exception) && text.EndsWith(exception, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static bool MatchesGeneratedRpSuffix(string text, string suffix, bool requiresEnglishWordBoundary)
	{
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(suffix) || !text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (!requiresEnglishWordBoundary)
		{
			return true;
		}
		int suffixStart = text.Length - suffix.Length;
		if (suffixStart <= 0)
		{
			return true;
		}
		char preceding = text[suffixStart - 1];
		if (!IsAsciiLetterOrDigit(preceding))
		{
			return true;
		}
		char firstSuffixCharacter = text[suffixStart];
		return char.IsLower(preceding) && char.IsUpper(firstSuffixCharacter);
	}

	private static bool IsAsciiLetterOrDigit(char value)
	{
		return (value >= 'a' && value <= 'z') || (value >= 'A' && value <= 'Z') || (value >= '0' && value <= '9');
	}

	private static bool TryResolveGeneratedRpEquipmentTemplate(string assetName, out ItemObject templateItem, out GeneratedRpEquipmentKind kind, out string matchedSuffix, out float matchScore, out int candidateCount)
	{
		templateItem = null;
		matchScore = 0f;
		candidateCount = 0;
		if (!TryResolveGeneratedRpEquipmentSuffix(assetName, out kind, out matchedSuffix))
		{
			return false;
		}
		Dictionary<GeneratedRpEquipmentKind, List<GeneratedRpEquipmentTemplateCandidate>> templatesByKind = GetGeneratedRpEquipmentTemplateCache();
		if (!templatesByKind.TryGetValue(kind, out List<GeneratedRpEquipmentTemplateCandidate> candidates) || candidates == null || candidates.Count == 0)
		{
			return false;
		}
		candidateCount = candidates.Count;
		GeneratedRpEquipmentTemplateCandidate best = null;
		float bestScore = -1f;
		int bestSuitability = int.MinValue;
		float bestTieBreaker = -1f;
		foreach (GeneratedRpEquipmentTemplateCandidate candidate in candidates)
		{
			ItemObject item = candidate?.Item;
			if (!IsCloneSafeGeneratedRewardTemplateItem(item))
			{
				continue;
			}
			float score = WorldEntityRetrievalService.CalculateBestAliasScoreForExternal(assetName, candidate.Aliases);
			int suitability = GetGeneratedRpTemplateSuitability(item);
			float tieBreaker = GetGeneratedRpTemplateTieBreaker(assetName, item);
			bool isBetter = score > bestScore + 0.00001f;
			if (!isBetter && Math.Abs(score - bestScore) <= 0.00001f)
			{
				isBetter = suitability > bestSuitability
					|| (suitability == bestSuitability && tieBreaker > bestTieBreaker + 0.00001f)
					|| (suitability == bestSuitability && Math.Abs(tieBreaker - bestTieBreaker) <= 0.00001f && string.Compare(item.StringId ?? "", best?.Item?.StringId ?? "", StringComparison.OrdinalIgnoreCase) < 0);
			}
			if (!isBetter)
			{
				continue;
			}
			best = candidate;
			bestScore = score;
			bestSuitability = suitability;
			bestTieBreaker = tieBreaker;
		}
		templateItem = best?.Item;
		matchScore = Math.Max(0f, bestScore);
		return templateItem != null;
	}

	private static bool TryResolveGeneratedRpFoodTemplate(string assetName, out ItemObject templateItem, out GeneratedRpFoodKind kind, out string matchedSuffix, out float matchScore, out int candidateCount)
	{
		templateItem = null;
		matchScore = 0f;
		candidateCount = 0;
		if (!TryResolveGeneratedRpFoodSuffix(assetName, out kind, out matchedSuffix))
		{
			return false;
		}
		Dictionary<GeneratedRpFoodKind, List<GeneratedRpFoodTemplateCandidate>> templatesByKind = GetGeneratedRpFoodTemplateCache();
		if (!templatesByKind.TryGetValue(kind, out List<GeneratedRpFoodTemplateCandidate> candidates) || candidates == null || candidates.Count == 0)
		{
			templatesByKind.TryGetValue(GeneratedRpFoodKind.AnyFood, out candidates);
		}
		if (candidates == null || candidates.Count == 0)
		{
			return false;
		}
		candidateCount = candidates.Count;
		GeneratedRpFoodTemplateCandidate best = null;
		float bestScore = -1f;
		int bestSuitability = int.MinValue;
		float bestTieBreaker = -1f;
		foreach (GeneratedRpFoodTemplateCandidate candidate in candidates)
		{
			ItemObject item = candidate?.Item;
			if (!IsCloneSafeGeneratedRpFoodTemplateItem(item))
			{
				continue;
			}
			float score = WorldEntityRetrievalService.CalculateBestAliasScoreForExternal(assetName, candidate.Aliases);
			int suitability = GetGeneratedRpFoodTemplateSuitability(item, kind);
			float tieBreaker = GetGeneratedRpTemplateTieBreaker(assetName, item);
			bool isBetter = score > bestScore + 0.00001f;
			if (!isBetter && Math.Abs(score - bestScore) <= 0.00001f)
			{
				isBetter = suitability > bestSuitability
					|| (suitability == bestSuitability && tieBreaker > bestTieBreaker + 0.00001f)
					|| (suitability == bestSuitability && Math.Abs(tieBreaker - bestTieBreaker) <= 0.00001f && string.Compare(item.StringId ?? "", best?.Item?.StringId ?? "", StringComparison.OrdinalIgnoreCase) < 0);
			}
			if (!isBetter)
			{
				continue;
			}
			best = candidate;
			bestScore = score;
			bestSuitability = suitability;
			bestTieBreaker = tieBreaker;
		}
		templateItem = best?.Item;
		matchScore = Math.Max(0f, bestScore);
		return templateItem != null;
	}

	private static int GetGeneratedRpTemplateSuitability(ItemObject item)
	{
		if (item == null)
		{
			return int.MinValue;
		}
		int score = 0;
		try
		{
			if (item.ItemCategory != null)
			{
				score += 2;
			}
			if (!item.NotMerchandise)
			{
				score++;
			}
			if (item.Value > 0)
			{
				score++;
			}
		}
		catch
		{
		}
		return score;
	}

	private static int GetGeneratedRpFoodTemplateSuitability(ItemObject item, GeneratedRpFoodKind kind)
	{
		int score = GetGeneratedRpTemplateSuitability(item);
		switch (kind)
		{
		case GeneratedRpFoodKind.Meat:
			return score + (IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Meat) ? 30 : 0);
		case GeneratedRpFoodKind.Fish:
			return score + (IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Fish) ? 30 : 0);
		case GeneratedRpFoodKind.Grain:
			return score + (IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Grain) ? 30 : 0);
		case GeneratedRpFoodKind.Fruit:
			return score + ((IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Grape)
				|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.DateFruit)
				|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Olives)) ? 30 : 0);
		case GeneratedRpFoodKind.Vegetable:
			return score + (IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Olives) ? 30 : 0);
		case GeneratedRpFoodKind.Dairy:
			return score + ((IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Cheese)
				|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Butter)) ? 30 : 0);
		case GeneratedRpFoodKind.Egg:
			if (ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodEggTemplateTokens))
			{
				return score + 40;
			}
			return score + (IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Cheese) ? 25 : 15);
		case GeneratedRpFoodKind.Sweet:
			if (IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Grain))
			{
				return score + 30;
			}
			if (IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Grape) || IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.DateFruit))
			{
				return score + 20;
			}
			return score + 10;
		case GeneratedRpFoodKind.Water:
			if (ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodWaterTemplateTokens))
			{
				return score + 40;
			}
			return score + (IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Beer) ? 30 : 0);
		case GeneratedRpFoodKind.Medicine:
			if (ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodMedicineTemplateTokens))
			{
				return score + 40;
			}
			return score + (IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Butter) ? 30 : 0);
		case GeneratedRpFoodKind.Beer:
			return score + (IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Beer) ? 30 : 0);
		case GeneratedRpFoodKind.Wine:
			return score + (IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Wine) ? 30 : 0);
		case GeneratedRpFoodKind.Drink:
			return score + ((IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Beer)
				|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Wine)) ? 20 : 0);
		case GeneratedRpFoodKind.PreparedMeal:
			return score + ((IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Meat)
				|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Fish)
				|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Grain)) ? 20 : 10);
		default:
			return score;
		}
	}

	private static float GetGeneratedRpTemplateTieBreaker(string assetName, ItemObject item)
	{
		string key = ((assetName ?? "").Trim() + "|" + (item?.StringId ?? "")).ToLowerInvariant();
		if (!uint.TryParse(StablePromptKeyHash(key), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint hash))
		{
			return 0f;
		}
		return (hash & 0xffffu) / 65535f;
	}

	private static Dictionary<GeneratedRpEquipmentKind, List<GeneratedRpEquipmentTemplateCandidate>> GetGeneratedRpEquipmentTemplateCache()
	{
		object owner = (object)Game.Current?.ObjectManager ?? MBObjectManager.Instance;
		lock (GeneratedRpEquipmentTemplateCacheLock)
		{
			if (owner != null
				&& ReferenceEquals(owner, GeneratedRpEquipmentTemplateCacheOwner))
			{
				if (GeneratedRpEquipmentTemplateCacheReady)
				{
					return GeneratedRpEquipmentTemplatesByKind;
				}
				if (DateTime.UtcNow < GeneratedRpEquipmentTemplateCacheRetryAfterUtc)
				{
					return GeneratedRpEquipmentTemplatesByKind;
				}
			}
			Dictionary<GeneratedRpEquipmentKind, List<GeneratedRpEquipmentTemplateCandidate>> result = new Dictionary<GeneratedRpEquipmentKind, List<GeneratedRpEquipmentTemplateCandidate>>();
			foreach (GeneratedRpEquipmentKind equipmentKind in Enum.GetValues(typeof(GeneratedRpEquipmentKind)))
			{
				result[equipmentKind] = new List<GeneratedRpEquipmentTemplateCandidate>();
			}
			bool scanCompleted = false;
			int scannedCount = 0;
			int rejectedByExceptionCount = 0;
			try
			{
				IEnumerable<ItemObject> items = Game.Current?.ObjectManager?.GetObjectTypeList<ItemObject>() ?? MBObjectManager.Instance?.GetObjectTypeList<ItemObject>();
				foreach (ItemObject item in items ?? Enumerable.Empty<ItemObject>())
				{
					scannedCount++;
					try
					{
						if (!IsCloneSafeGeneratedRewardTemplateItem(item)
							|| IsGeneratedRewardItemStringId(item.StringId))
						{
							continue;
						}
						bool isWeapon = IsSettlementWeaponLikeItem(item);
						bool isArmor = IsSettlementArmorLikeItem(item);
						bool isHorse = item.Type == ItemObject.ItemTypeEnum.Horse;
						bool isHorseHarness = item.Type == ItemObject.ItemTypeEnum.HorseHarness;
						bool isBanner = item.Type == ItemObject.ItemTypeEnum.Banner;
						if (!isWeapon && !isArmor && !isHorse && !isHorseHarness && !isBanner)
						{
							continue;
						}
						GeneratedRpEquipmentTemplateCandidate candidate = new GeneratedRpEquipmentTemplateCandidate
						{
							Item = item,
							Aliases = BuildGeneratedRpTemplateAliases(item)
						};
						AddGeneratedRpEquipmentTemplate(result, GeneratedRpEquipmentKind.AnyEquipment, candidate);
						if (isWeapon)
						{
							AddGeneratedRpEquipmentTemplate(result, GeneratedRpEquipmentKind.AnyWeapon, candidate);
						}
						if (isArmor)
						{
							AddGeneratedRpEquipmentTemplate(result, GeneratedRpEquipmentKind.AnyArmor, candidate);
						}
						IndexGeneratedRpEquipmentSpecificKinds(result, candidate);
					}
					catch
					{
						rejectedByExceptionCount++;
					}
				}
				scanCompleted = true;
			}
			catch (Exception ex)
			{
				try
				{
					Logger.Log("Logic", "[RewardItemResolve] rp_equipment_template_cache_failed error=" + ex.GetType().Name + ":" + ex.Message);
				}
				catch
				{
				}
			}
			bool hasSafeCandidates =
				result.TryGetValue(
					GeneratedRpEquipmentKind.AnyEquipment,
					out List<GeneratedRpEquipmentTemplateCandidate> anyEquipment)
				&& anyEquipment != null
				&& anyEquipment.Count > 0;
			GeneratedRpEquipmentTemplateCacheOwner = owner;
			GeneratedRpEquipmentTemplatesByKind = result;
			GeneratedRpEquipmentTemplateCacheReady =
				scanCompleted && hasSafeCandidates;
			GeneratedRpEquipmentTemplateCacheRetryAfterUtc =
				GeneratedRpEquipmentTemplateCacheReady
					? DateTime.MinValue
					: DateTime.UtcNow.AddSeconds(1d);
			if (!GeneratedRpEquipmentTemplateCacheReady
				|| rejectedByExceptionCount > 0)
			{
				try
				{
					Logger.Log(
						"Logic",
						"[RewardItemResolve] rp_equipment_template_cache_status"
							+ " ready=" + GeneratedRpEquipmentTemplateCacheReady
							+ " scan_completed=" + scanCompleted
							+ " scanned=" + scannedCount.ToString(CultureInfo.InvariantCulture)
							+ " accepted=" + (anyEquipment?.Count ?? 0).ToString(CultureInfo.InvariantCulture)
							+ " item_errors=" + rejectedByExceptionCount.ToString(CultureInfo.InvariantCulture));
				}
				catch
				{
				}
			}
			return GeneratedRpEquipmentTemplatesByKind;
		}
	}

	private static void ClearGeneratedRpEquipmentTemplateCache()
	{
		lock (GeneratedRpEquipmentTemplateCacheLock)
		{
			GeneratedRpEquipmentTemplateCacheOwner = null;
			GeneratedRpEquipmentTemplatesByKind = new Dictionary<GeneratedRpEquipmentKind, List<GeneratedRpEquipmentTemplateCandidate>>();
			GeneratedRpEquipmentTemplateCacheReady = false;
			GeneratedRpEquipmentTemplateCacheRetryAfterUtc = DateTime.MinValue;
		}
	}

	private static Dictionary<GeneratedRpFoodKind, List<GeneratedRpFoodTemplateCandidate>> GetGeneratedRpFoodTemplateCache()
	{
		object owner = (object)Game.Current?.ObjectManager ?? MBObjectManager.Instance;
		lock (GeneratedRpFoodTemplateCacheLock)
		{
			if (owner != null && ReferenceEquals(owner, GeneratedRpFoodTemplateCacheOwner) && GeneratedRpFoodTemplatesByKind.Count > 0)
			{
				return GeneratedRpFoodTemplatesByKind;
			}
			Dictionary<GeneratedRpFoodKind, List<GeneratedRpFoodTemplateCandidate>> result = new Dictionary<GeneratedRpFoodKind, List<GeneratedRpFoodTemplateCandidate>>();
			foreach (GeneratedRpFoodKind foodKind in Enum.GetValues(typeof(GeneratedRpFoodKind)))
			{
				result[foodKind] = new List<GeneratedRpFoodTemplateCandidate>();
			}
			try
			{
				IEnumerable<ItemObject> items = Game.Current?.ObjectManager?.GetObjectTypeList<ItemObject>() ?? MBObjectManager.Instance?.GetObjectTypeList<ItemObject>();
				foreach (ItemObject item in items ?? Enumerable.Empty<ItemObject>())
				{
					if (!IsCloneSafeGeneratedRpFoodTemplateItem(item)
						|| IsGeneratedRewardItemStringId(item.StringId))
					{
						continue;
					}
					GeneratedRpFoodTemplateCandidate candidate = new GeneratedRpFoodTemplateCandidate
					{
						Item = item,
						Aliases = BuildGeneratedRpTemplateAliases(item)
					};
					AddGeneratedRpFoodTemplate(result, GeneratedRpFoodKind.AnyFood, candidate);
					IndexGeneratedRpFoodSpecificKinds(result, candidate);
				}
			}
			catch (Exception ex)
			{
				try
				{
					Logger.Log("Logic", "[RewardItemResolve] rp_food_template_cache_failed error=" + ex.GetType().Name + ":" + ex.Message);
				}
				catch
				{
				}
			}
			GeneratedRpFoodTemplateCacheOwner = owner;
			GeneratedRpFoodTemplatesByKind = result;
			return GeneratedRpFoodTemplatesByKind;
		}
	}

	private static void ClearGeneratedRpFoodTemplateCache()
	{
		lock (GeneratedRpFoodTemplateCacheLock)
		{
			GeneratedRpFoodTemplateCacheOwner = null;
			GeneratedRpFoodTemplatesByKind = new Dictionary<GeneratedRpFoodKind, List<GeneratedRpFoodTemplateCandidate>>();
		}
	}

	private static void AddGeneratedRpFoodTemplate(Dictionary<GeneratedRpFoodKind, List<GeneratedRpFoodTemplateCandidate>> templatesByKind, GeneratedRpFoodKind kind, GeneratedRpFoodTemplateCandidate candidate)
	{
		if (candidate?.Item == null || kind == GeneratedRpFoodKind.None || !templatesByKind.TryGetValue(kind, out List<GeneratedRpFoodTemplateCandidate> candidates))
		{
			return;
		}
		candidates.Add(candidate);
	}

	private static void IndexGeneratedRpFoodSpecificKinds(Dictionary<GeneratedRpFoodKind, List<GeneratedRpFoodTemplateCandidate>> templatesByKind, GeneratedRpFoodTemplateCandidate candidate)
	{
		ItemObject item = candidate?.Item;
		if (item == null)
		{
			return;
		}
		bool isMeat = IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Meat) || ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodMeatTemplateTokens);
		bool isFish = IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Fish) || ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodFishTemplateTokens);
		bool isGrain = IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Grain) || ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodGrainTemplateTokens);
		bool isFruit = IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Grape)
			|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.DateFruit)
			|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Olives)
			|| ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodFruitTemplateTokens);
		bool isVegetable = IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Olives) || ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodVegetableTemplateTokens);
		bool isDairy = IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Cheese)
			|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Butter)
			|| ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodDairyTemplateTokens);
		bool isEgg = ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodEggTemplateTokens);
		bool isSweet = isGrain || isFruit || isDairy || ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodSweetTemplateTokens);
		bool isBeer = IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Beer) || ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodBeerTemplateTokens);
		bool isWine = IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Wine) || ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodWineTemplateTokens);
		bool isWater = item.IsFood && (IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Beer) || ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodWaterTemplateTokens));
		bool isMedicine = item.IsFood && (IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Butter) || ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodMedicineTemplateTokens));
		bool isDrink = isBeer || isWine || isWater || ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodDrinkTemplateTokens);
		if (isMeat)
		{
			AddGeneratedRpFoodTemplate(templatesByKind, GeneratedRpFoodKind.Meat, candidate);
		}
		if (isFish)
		{
			AddGeneratedRpFoodTemplate(templatesByKind, GeneratedRpFoodKind.Fish, candidate);
		}
		if (isGrain)
		{
			AddGeneratedRpFoodTemplate(templatesByKind, GeneratedRpFoodKind.Grain, candidate);
		}
		if (isFruit)
		{
			AddGeneratedRpFoodTemplate(templatesByKind, GeneratedRpFoodKind.Fruit, candidate);
		}
		if (isVegetable)
		{
			AddGeneratedRpFoodTemplate(templatesByKind, GeneratedRpFoodKind.Vegetable, candidate);
		}
		if (isDairy)
		{
			AddGeneratedRpFoodTemplate(templatesByKind, GeneratedRpFoodKind.Dairy, candidate);
		}
		if (isEgg || isDairy)
		{
			AddGeneratedRpFoodTemplate(templatesByKind, GeneratedRpFoodKind.Egg, candidate);
		}
		if (isSweet)
		{
			AddGeneratedRpFoodTemplate(templatesByKind, GeneratedRpFoodKind.Sweet, candidate);
		}
		if (isWater)
		{
			AddGeneratedRpFoodTemplate(templatesByKind, GeneratedRpFoodKind.Water, candidate);
		}
		if (isMedicine)
		{
			AddGeneratedRpFoodTemplate(templatesByKind, GeneratedRpFoodKind.Medicine, candidate);
		}
		if (isBeer)
		{
			AddGeneratedRpFoodTemplate(templatesByKind, GeneratedRpFoodKind.Beer, candidate);
		}
		if (isWine)
		{
			AddGeneratedRpFoodTemplate(templatesByKind, GeneratedRpFoodKind.Wine, candidate);
		}
		if (isDrink)
		{
			AddGeneratedRpFoodTemplate(templatesByKind, GeneratedRpFoodKind.Drink, candidate);
		}
		if (!isDrink && !isMedicine)
		{
			AddGeneratedRpFoodTemplate(templatesByKind, GeneratedRpFoodKind.PreparedMeal, candidate);
		}
	}

	private static string[] BuildGeneratedRpTemplateAliases(ItemObject item)
	{
		List<string> aliases = new List<string>(8);
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		AddGeneratedRpTemplateAlias(aliases, seen, item?.StringId);
		AddGeneratedRpTemplateAlias(aliases, seen, item?.Name?.ToString());
		if (IsGeneratedRpWhipWeaponTemplateItem(item))
		{
			AddGeneratedRpTemplateAlias(aliases, seen, "鞭");
			AddGeneratedRpTemplateAlias(aliases, seen, "whip");
			return aliases.ToArray();
		}
		AddGeneratedRpTemplateAlias(aliases, seen, item?.ItemCategory?.StringId);
		AddGeneratedRpTemplateAlias(aliases, seen, item?.ItemCategory?.GetName()?.ToString());
		AddGeneratedRpTemplateAlias(aliases, seen, item?.Type.ToString());
		AddGeneratedRpTemplateAlias(aliases, seen, GetItemPromptTypeLabel(item));
		try
		{
			AddGeneratedRpTemplateAlias(aliases, seen, item?.PrimaryWeapon?.WeaponClass.ToString());
		}
		catch
		{
		}
		return aliases.ToArray();
	}

	private static void AddGeneratedRpTemplateAlias(List<string> aliases, HashSet<string> seen, string value)
	{
		string text = (value ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text) && seen.Add(text))
		{
			aliases.Add(text);
		}
	}

	private static void AddGeneratedRpEquipmentTemplate(Dictionary<GeneratedRpEquipmentKind, List<GeneratedRpEquipmentTemplateCandidate>> templatesByKind, GeneratedRpEquipmentKind kind, GeneratedRpEquipmentTemplateCandidate candidate)
	{
		if (candidate?.Item == null || kind == GeneratedRpEquipmentKind.None || !templatesByKind.TryGetValue(kind, out List<GeneratedRpEquipmentTemplateCandidate> candidates))
		{
			return;
		}
		candidates.Add(candidate);
	}

	private static void IndexGeneratedRpEquipmentSpecificKinds(Dictionary<GeneratedRpEquipmentKind, List<GeneratedRpEquipmentTemplateCandidate>> templatesByKind, GeneratedRpEquipmentTemplateCandidate candidate)
	{
		ItemObject item = candidate?.Item;
		if (item == null)
		{
			return;
		}
		bool isExplicitWhip = IsGeneratedRpWhipWeaponTemplateItem(item);
		if (isExplicitWhip)
		{
			AddGeneratedRpEquipmentTemplate(
				templatesByKind,
				GeneratedRpEquipmentKind.Whip,
				candidate);
			return;
		}
		switch (item.Type)
		{
		case ItemObject.ItemTypeEnum.OneHandedWeapon:
		case ItemObject.ItemTypeEnum.TwoHandedWeapon:
			IndexGeneratedRpEquipmentWeaponClass(
				templatesByKind,
				candidate,
				item.PrimaryWeapon?.WeaponClass);
			break;
		case ItemObject.ItemTypeEnum.Polearm:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.Polearm, candidate);
			break;
		case ItemObject.ItemTypeEnum.Arrows:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.Arrows, candidate);
			break;
		case ItemObject.ItemTypeEnum.Bolts:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.Bolts, candidate);
			break;
		case ItemObject.ItemTypeEnum.Shield:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.Shield, candidate);
			break;
		case ItemObject.ItemTypeEnum.Bow:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.Bow, candidate);
			break;
		case ItemObject.ItemTypeEnum.Crossbow:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.Crossbow, candidate);
			break;
		case ItemObject.ItemTypeEnum.Sling:
		case ItemObject.ItemTypeEnum.SlingStones:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.Sling, candidate);
			break;
		case ItemObject.ItemTypeEnum.Thrown:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.Thrown, candidate);
			IndexGeneratedRpEquipmentWeaponClass(templatesByKind, candidate, item.PrimaryWeapon?.WeaponClass);
			break;
		case ItemObject.ItemTypeEnum.Pistol:
		case ItemObject.ItemTypeEnum.Musket:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.Firearm, candidate);
			break;
		case ItemObject.ItemTypeEnum.Bullets:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.Bullets, candidate);
			break;
		case ItemObject.ItemTypeEnum.HeadArmor:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.HeadArmor, candidate);
			break;
		case ItemObject.ItemTypeEnum.BodyArmor:
		case ItemObject.ItemTypeEnum.ChestArmor:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.BodyArmor, candidate);
			break;
		case ItemObject.ItemTypeEnum.LegArmor:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.LegArmor, candidate);
			break;
		case ItemObject.ItemTypeEnum.HandArmor:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.HandArmor, candidate);
			break;
		case ItemObject.ItemTypeEnum.Cape:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.Cape, candidate);
			break;
		case ItemObject.ItemTypeEnum.Horse:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.Horse, candidate);
			break;
		case ItemObject.ItemTypeEnum.HorseHarness:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.HorseHarness, candidate);
			break;
		case ItemObject.ItemTypeEnum.Banner:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.Banner, candidate);
			break;
		}
	}

	private static void IndexGeneratedRpEquipmentWeaponClass(Dictionary<GeneratedRpEquipmentKind, List<GeneratedRpEquipmentTemplateCandidate>> templatesByKind, GeneratedRpEquipmentTemplateCandidate candidate, WeaponClass? weaponClass)
	{
		if (!weaponClass.HasValue)
		{
			return;
		}
		switch (weaponClass.Value)
		{
		case WeaponClass.OneHandedSword:
		case WeaponClass.TwoHandedSword:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.Sword, candidate);
			break;
		case WeaponClass.OneHandedAxe:
		case WeaponClass.TwoHandedAxe:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.Axe, candidate);
			break;
		case WeaponClass.Mace:
		case WeaponClass.TwoHandedMace:
		case WeaponClass.Pick:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.Mace, candidate);
			break;
		case WeaponClass.Dagger:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.Dagger, candidate);
			break;
		case WeaponClass.OneHandedPolearm:
		case WeaponClass.TwoHandedPolearm:
		case WeaponClass.LowGripPolearm:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.Polearm, candidate);
			break;
		case WeaponClass.ThrowingAxe:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.ThrowingAxe, candidate);
			break;
		case WeaponClass.ThrowingKnife:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.ThrowingKnife, candidate);
			break;
		case WeaponClass.Javelin:
			AddGeneratedRpEquipmentTemplate(templatesByKind, GeneratedRpEquipmentKind.Javelin, candidate);
			break;
		}
	}

	private static float CalculateGeneratedRewardTemplateScore(string lookup, RewardItemInfo info, float aliasScore)
	{
		ItemObject item = info?.Item;
		if (IsGeneratedRewardAutoConsumedTemplateItem(item))
		{
			return 0f;
		}
		float score = Math.Max(0f, aliasScore);
		if (IsGeneratedRewardMiscItemType(item))
		{
			score = score * GeneratedRewardTemplateMiscScoreMultiplier + GeneratedRewardTemplateMiscScoreBonus;
		}
		else if (IsGeneratedRewardWeaponOrArmorTemplateItem(item))
		{
			score = score * GeneratedRewardTemplateWeaponArmorScoreMultiplier - GeneratedRewardTemplateWeaponArmorScorePenalty;
		}
		score += CalculateGeneratedRewardTemplateSemanticHintScore(lookup, item);
		return Math.Max(0f, Math.Min(1f, score));
	}

	private static float CalculateGeneratedRewardTemplateDiversityTieBreaker(string lookup, RewardItemInfo info)
	{
		string key = ((lookup ?? "").Trim() + "|" + BuildRewardItemResolutionCandidateKey(info)).ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(key))
		{
			return 0f;
		}
		if (!uint.TryParse(StablePromptKeyHash(key), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hash))
		{
			return 0f;
		}
		return ((hash & 0xffffu) / 65535f) * GeneratedRewardTemplateDiversityTieBreaker;
	}

	private static float CalculateGeneratedRewardTemplateSemanticHintScore(string lookup, ItemObject item)
	{
		if (item == null || string.IsNullOrWhiteSpace(lookup))
		{
			return 0f;
		}
		string text = lookup.Trim();
		if (TextContainsAny(text, "\u8089", "\u732a", "\u725b", "\u7f8a", "meat", "pork", "beef", "mutton"))
		{
			if (ItemCategoryIsAny(item, DefaultItemCategories.Meat) || ContainsGeneratedRewardItemTextAny(item, "meat", "pork", "beef", "mutton"))
			{
				return GeneratedRewardTemplateSemanticHintBonus;
			}
			if (item.Type == ItemObject.ItemTypeEnum.Animal || item.IsFood)
			{
				return GeneratedRewardTemplateWeakSemanticHintBonus;
			}
		}
		if (TextContainsAny(text, "\u9152", "\u5564", "\u8461\u8404\u9152", "beer", "wine", "ale", "liquor"))
		{
			if (ItemCategoryIsAny(item, DefaultItemCategories.Beer, DefaultItemCategories.Wine) || ContainsGeneratedRewardItemTextAny(item, "beer", "wine", "ale"))
			{
				return GeneratedRewardTemplateSemanticHintBonus;
			}
		}
		if (TextContainsAny(text, "\u9c7c", "fish"))
		{
			if (ItemCategoryIsAny(item, DefaultItemCategories.Fish) || ContainsGeneratedRewardItemTextAny(item, "fish"))
			{
				return GeneratedRewardTemplateSemanticHintBonus;
			}
		}
		if (TextContainsAny(text, "\u7cae", "\u9ea6", "\u9762\u5305", "\u98df", "food", "grain", "wheat", "bread"))
		{
			if (item.IsFood || ItemCategoryIsAny(item, DefaultItemCategories.Grain) || ContainsGeneratedRewardItemTextAny(item, "grain", "wheat", "bread", "food"))
			{
				return GeneratedRewardTemplateSemanticHintBonus;
			}
		}
		if (TextContainsAny(text, "\u4e66", "\u4fe1", "\u624b\u8c15", "\u5377", "\u6587\u4e66", "\u5951\u7ea6", "book", "letter", "scroll", "decree", "paper", "contract"))
		{
			if (item.Type == ItemObject.ItemTypeEnum.Book || ContainsGeneratedRewardItemTextAny(item, "book", "letter", "scroll", "decree", "paper", "parchment", "ledger"))
			{
				return GeneratedRewardTemplateSemanticHintBonus;
			}
		}
		if (TextContainsAny(text, "\u5c4e", "\u7caa", "poop", "dung", "shit", "manure"))
		{
			if (ItemCategoryIsAny(item, DefaultItemCategories.Clay, DefaultItemCategories.Pottery) || ContainsGeneratedRewardItemTextAny(item, "clay", "pottery"))
			{
				return GeneratedRewardTemplateSemanticHintBonus;
			}
		}
		if (TextContainsAny(text, "\u5de5\u5177", "\u5668\u5177", "\u94a5\u5319", "\u9524", "tool", "tools", "key", "hammer"))
		{
			if (ItemCategoryIsAny(item, DefaultItemCategories.Tools) || ContainsGeneratedRewardItemTextAny(item, "tool", "tools", "hammer"))
			{
				return GeneratedRewardTemplateSemanticHintBonus;
			}
		}
		if (TextContainsAny(text, "\u5b9d", "\u73e0", "\u94f6", "\u6212", "\u91d1\u5e01", "jewel", "jewelry", "silver", "ring", "coin"))
		{
			if (ItemCategoryIsAny(item, DefaultItemCategories.Jewelry, DefaultItemCategories.Silver) || ContainsGeneratedRewardItemTextAny(item, "jewel", "jewelry", "silver", "ring"))
			{
				return GeneratedRewardTemplateSemanticHintBonus;
			}
		}
		if (TextContainsAny(text, "\u6728", "\u67f4", "\u6728\u677f", "wood", "plank"))
		{
			if (ItemCategoryIsAny(item, DefaultItemCategories.Wood, DefaultItemCategories.Planks) || ContainsGeneratedRewardItemTextAny(item, "wood", "plank"))
			{
				return GeneratedRewardTemplateSemanticHintBonus;
			}
		}
		if (TextContainsAny(text, "\u76d0", "salt"))
		{
			if (ItemCategoryIsAny(item, DefaultItemCategories.Salt) || ContainsGeneratedRewardItemTextAny(item, "salt"))
			{
				return GeneratedRewardTemplateSemanticHintBonus;
			}
		}
		if (TextContainsAny(text, "\u6cb9", "oil"))
		{
			if (ItemCategoryIsAny(item, DefaultItemCategories.Oil) || ContainsGeneratedRewardItemTextAny(item, "oil"))
			{
				return GeneratedRewardTemplateSemanticHintBonus;
			}
		}
		if (TextContainsAny(text, "\u5e03", "\u8863", "\u4e1d", "\u7ef8", "cloth", "linen", "velvet", "garment", "felt"))
		{
			if (ItemCategoryIsAny(item, DefaultItemCategories.Cloth, DefaultItemCategories.Linen, DefaultItemCategories.Velvet, DefaultItemCategories.Garment, DefaultItemCategories.Felt) || ContainsGeneratedRewardItemTextAny(item, "cloth", "linen", "velvet", "garment", "felt"))
			{
				return GeneratedRewardTemplateSemanticHintBonus;
			}
		}
		if (TextContainsAny(text, "\u76ae", "\u6bdb", "hide", "hides", "leather", "fur", "wool"))
		{
			if (ItemCategoryIsAny(item, DefaultItemCategories.Hides, DefaultItemCategories.Leather, DefaultItemCategories.Fur, DefaultItemCategories.Wool) || ContainsGeneratedRewardItemTextAny(item, "hide", "hides", "leather", "fur", "wool"))
			{
				return GeneratedRewardTemplateSemanticHintBonus;
			}
		}
		if (TextContainsAny(text, "\u9676", "\u7f50", "\u58f6", "clay", "pottery", "jar"))
		{
			if (ItemCategoryIsAny(item, DefaultItemCategories.Clay, DefaultItemCategories.Pottery) || ContainsGeneratedRewardItemTextAny(item, "clay", "pottery", "jar"))
			{
				return GeneratedRewardTemplateSemanticHintBonus;
			}
		}
		return 0f;
	}

	private static bool TextContainsAny(string text, params string[] tokens)
	{
		if (string.IsNullOrWhiteSpace(text) || tokens == null)
		{
			return false;
		}
		foreach (string token in tokens)
		{
			if (!string.IsNullOrWhiteSpace(token) && text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static bool ContainsGeneratedRewardItemTextAny(ItemObject item, params string[] tokens)
	{
		if (item == null || tokens == null)
		{
			return false;
		}
		return TextContainsAny(item.StringId, tokens)
			|| TextContainsAny(item.Name?.ToString(), tokens)
			|| TextContainsAny(item.ItemCategory?.StringId, tokens)
			|| TextContainsAny(item.ItemCategory?.GetName()?.ToString(), tokens);
	}

	private static bool IsGeneratedRewardMiscItemType(ItemObject item)
	{
		return item != null && (item.Type == ItemObject.ItemTypeEnum.Goods || item.Type == ItemObject.ItemTypeEnum.Book);
	}

	private static bool IsGeneratedRewardTemplateCompatibleWithRequestedName(ItemObject item, string requestedName)
	{
		if (item == null)
		{
			return false;
		}
		if (TryResolveGeneratedRpEquipmentSuffix(requestedName, out GeneratedRpEquipmentKind equipmentKind, out _))
		{
			return IsCloneSafeGeneratedRewardTemplateItem(item)
				&& DoesGeneratedRpEquipmentTemplateMatchKind(item, equipmentKind);
		}
		if (TryResolveGeneratedRpFoodSuffix(requestedName, out GeneratedRpFoodKind foodKind, out _))
		{
			return IsCloneSafeGeneratedRpFoodTemplateItem(item)
				&& DoesGeneratedRpFoodTemplateMatchKind(item, foodKind);
		}
		return IsCloneSafeGeneratedRewardTemplateItem(item) && IsGeneratedRewardMiscItemType(item);
	}

	private static bool IsSafePlayerRpCraftGenerationTemplate(ItemObject item)
	{
		if (item == null)
		{
			return false;
		}
		if (IsCloneSafeGeneratedRewardTemplateItem(item)
			&& PlayerRpCraftItemComponentService.IsSafeEquipmentTemplate(item, out _))
		{
			return true;
		}
		return IsCloneSafeGeneratedRpFoodTemplateItem(item)
			|| (IsCloneSafeGeneratedRewardTemplateItem(item)
				&& IsGeneratedRewardMiscItemType(item));
	}

	private static bool DoesGeneratedRpFoodTemplateMatchKind(ItemObject item, GeneratedRpFoodKind kind)
	{
		if (!IsGeneratedRpFoodTemplateItem(item) || kind == GeneratedRpFoodKind.None)
		{
			return false;
		}
		switch (kind)
		{
		case GeneratedRpFoodKind.AnyFood:
			return true;
		case GeneratedRpFoodKind.PreparedMeal:
			return !IsGeneratedRpDrinkTemplateItem(item) && !IsGeneratedRpMedicineTemplateItem(item);
		case GeneratedRpFoodKind.Egg:
			return IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Cheese)
				|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Butter)
				|| ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodEggTemplateTokens);
		case GeneratedRpFoodKind.Meat:
			return IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Meat) || ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodMeatTemplateTokens);
		case GeneratedRpFoodKind.Fish:
			return IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Fish) || ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodFishTemplateTokens);
		case GeneratedRpFoodKind.Grain:
			return IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Grain) || ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodGrainTemplateTokens);
		case GeneratedRpFoodKind.Fruit:
			return IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Grape)
				|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.DateFruit)
				|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Olives)
				|| ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodFruitTemplateTokens);
		case GeneratedRpFoodKind.Vegetable:
			return IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Olives) || ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodVegetableTemplateTokens);
		case GeneratedRpFoodKind.Dairy:
			return IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Cheese)
				|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Butter)
				|| ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodDairyTemplateTokens);
		case GeneratedRpFoodKind.Sweet:
			return IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Grain)
				|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Grape)
				|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.DateFruit)
				|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Olives)
				|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Cheese)
				|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Butter)
				|| ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodSweetTemplateTokens);
		case GeneratedRpFoodKind.Water:
			return item.IsFood
				&& (IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Beer)
					|| ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodWaterTemplateTokens));
		case GeneratedRpFoodKind.Medicine:
			return item.IsFood
				&& (IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Butter)
					|| ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodMedicineTemplateTokens));
		case GeneratedRpFoodKind.Beer:
			return IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Beer) || ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodBeerTemplateTokens);
		case GeneratedRpFoodKind.Wine:
			return IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Wine) || ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodWineTemplateTokens);
		case GeneratedRpFoodKind.Drink:
			return IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Beer)
				|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Wine)
				|| ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodDrinkTemplateTokens);
		default:
			return false;
		}
	}

	private static bool IsGeneratedRpDrinkTemplateItem(ItemObject item)
	{
		return IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Beer)
			|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Wine)
			|| ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodDrinkTemplateTokens)
			|| ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodWaterTemplateTokens);
	}

	private static bool IsGeneratedRpMedicineTemplateItem(ItemObject item)
	{
		return item != null && item.IsFood
			&& (IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Butter)
				|| ContainsGeneratedRewardItemTextAny(item, GeneratedRpFoodMedicineTemplateTokens));
	}

	private static bool DoesGeneratedRpEquipmentTemplateMatchKind(ItemObject item, GeneratedRpEquipmentKind kind)
	{
		if (item == null || kind == GeneratedRpEquipmentKind.None)
		{
			return false;
		}
		bool isWeapon = IsSettlementWeaponLikeItem(item);
		bool isArmor = IsSettlementArmorLikeItem(item);
		switch (kind)
		{
		case GeneratedRpEquipmentKind.AnyEquipment:
			return isWeapon || isArmor || item.Type == ItemObject.ItemTypeEnum.Horse || item.Type == ItemObject.ItemTypeEnum.HorseHarness || item.Type == ItemObject.ItemTypeEnum.Banner;
		case GeneratedRpEquipmentKind.AnyWeapon:
			return isWeapon;
		case GeneratedRpEquipmentKind.AnyArmor:
			return isArmor;
		case GeneratedRpEquipmentKind.Arrows:
			return item.Type == ItemObject.ItemTypeEnum.Arrows;
		case GeneratedRpEquipmentKind.Bolts:
			return item.Type == ItemObject.ItemTypeEnum.Bolts;
		case GeneratedRpEquipmentKind.Bow:
			return item.Type == ItemObject.ItemTypeEnum.Bow;
		case GeneratedRpEquipmentKind.Crossbow:
			return item.Type == ItemObject.ItemTypeEnum.Crossbow;
		case GeneratedRpEquipmentKind.Shield:
			return item.Type == ItemObject.ItemTypeEnum.Shield;
		case GeneratedRpEquipmentKind.Thrown:
			return item.Type == ItemObject.ItemTypeEnum.Thrown;
		case GeneratedRpEquipmentKind.Sling:
			return item.Type == ItemObject.ItemTypeEnum.Sling || item.Type == ItemObject.ItemTypeEnum.SlingStones;
		case GeneratedRpEquipmentKind.Firearm:
			return item.Type == ItemObject.ItemTypeEnum.Pistol || item.Type == ItemObject.ItemTypeEnum.Musket;
		case GeneratedRpEquipmentKind.Bullets:
			return item.Type == ItemObject.ItemTypeEnum.Bullets;
		case GeneratedRpEquipmentKind.HeadArmor:
			return item.Type == ItemObject.ItemTypeEnum.HeadArmor;
		case GeneratedRpEquipmentKind.BodyArmor:
			return item.Type == ItemObject.ItemTypeEnum.BodyArmor || item.Type == ItemObject.ItemTypeEnum.ChestArmor;
		case GeneratedRpEquipmentKind.LegArmor:
			return item.Type == ItemObject.ItemTypeEnum.LegArmor;
		case GeneratedRpEquipmentKind.HandArmor:
			return item.Type == ItemObject.ItemTypeEnum.HandArmor;
		case GeneratedRpEquipmentKind.Cape:
			return item.Type == ItemObject.ItemTypeEnum.Cape;
		case GeneratedRpEquipmentKind.Horse:
			return item.Type == ItemObject.ItemTypeEnum.Horse;
		case GeneratedRpEquipmentKind.HorseHarness:
			return item.Type == ItemObject.ItemTypeEnum.HorseHarness;
		case GeneratedRpEquipmentKind.Banner:
			return item.Type == ItemObject.ItemTypeEnum.Banner;
		case GeneratedRpEquipmentKind.Whip:
			return IsGeneratedRpWhipWeaponTemplateItem(item);
		case GeneratedRpEquipmentKind.Polearm:
			if (item.Type == ItemObject.ItemTypeEnum.Polearm)
			{
				return true;
			}
			break;
		}
		WeaponClass? weaponClass = null;
		try
		{
			weaponClass = item.PrimaryWeapon?.WeaponClass;
		}
		catch
		{
		}
		if (!weaponClass.HasValue)
		{
			return false;
		}
		switch (kind)
		{
		case GeneratedRpEquipmentKind.Sword:
			return !IsGeneratedRpWhipTemplateItem(item)
				&& (weaponClass == WeaponClass.OneHandedSword
					|| weaponClass == WeaponClass.TwoHandedSword);
		case GeneratedRpEquipmentKind.Axe:
			return weaponClass == WeaponClass.OneHandedAxe || weaponClass == WeaponClass.TwoHandedAxe;
		case GeneratedRpEquipmentKind.Mace:
			return weaponClass == WeaponClass.Mace || weaponClass == WeaponClass.TwoHandedMace || weaponClass == WeaponClass.Pick;
		case GeneratedRpEquipmentKind.Dagger:
			return weaponClass == WeaponClass.Dagger;
		case GeneratedRpEquipmentKind.Polearm:
			return weaponClass == WeaponClass.OneHandedPolearm || weaponClass == WeaponClass.TwoHandedPolearm || weaponClass == WeaponClass.LowGripPolearm;
		case GeneratedRpEquipmentKind.ThrowingAxe:
			return weaponClass == WeaponClass.ThrowingAxe;
		case GeneratedRpEquipmentKind.ThrowingKnife:
			return weaponClass == WeaponClass.ThrowingKnife;
		case GeneratedRpEquipmentKind.Javelin:
			return weaponClass == WeaponClass.Javelin;
		default:
			return false;
		}
	}

	private static bool IsSameGeneratedRewardTemplateItem(ItemObject left, ItemObject right)
	{
		if (ReferenceEquals(left, right))
		{
			return left != null;
		}
		return left != null && right != null && !string.IsNullOrWhiteSpace(left.StringId)
			&& string.Equals(left.StringId, right.StringId, StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsGeneratedRewardWeaponOrArmorTemplateItem(ItemObject item)
	{
		return IsSettlementWeaponLikeItem(item) || IsSettlementArmorLikeItem(item);
	}

	private static string BuildRewardItemResolutionCandidateKey(RewardItemInfo item)
	{
		if (item == null)
		{
			return "";
		}
		string promptStringId = (item.PromptStringId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(promptStringId))
		{
			return "prompt:" + promptStringId;
		}
		string stringId = (item.StringId ?? item.Item?.StringId ?? "").Trim();
		string modifierStringId = (item.ModifierStringId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(stringId))
		{
			return string.IsNullOrWhiteSpace(modifierStringId) ? ("item:" + stringId) : ("item:" + stringId + "@" + modifierStringId);
		}
		string name = (item.Name ?? item.Item?.Name?.ToString() ?? "").Trim();
		return string.IsNullOrWhiteSpace(name) ? "" : ("name:" + name);
	}

	private static void AddRewardItemResolutionCandidate(Dictionary<string, RewardItemResolutionCandidate> candidates, RewardItemInfo item, bool isContext, ref int order)
	{
		if (candidates == null || item?.Item == null)
		{
			return;
		}
		string key = BuildRewardItemResolutionCandidateKey(item);
		if (string.IsNullOrWhiteSpace(key))
		{
			return;
		}
		if (candidates.TryGetValue(key, out var existing) && existing != null && existing.IsContext)
		{
			return;
		}
		candidates[key] = new RewardItemResolutionCandidate
		{
			Info = item,
			IsContext = isContext,
			Order = order++
		};
	}

	private List<RewardItemInfo> BuildGlobalRewardItemResolutionItems()
	{
		List<RewardItemInfo> result = new List<RewardItemInfo>();
		try
		{
			IEnumerable<ItemObject> items = Game.Current?.ObjectManager?.GetObjectTypeList<ItemObject>();
			foreach (ItemObject item in items ?? Enumerable.Empty<ItemObject>())
			{
				if (item == null)
				{
					continue;
				}
				string stringId = item.StringId ?? "";
				result.Add(new RewardItemInfo
				{
					Item = item,
					StringId = stringId,
					PromptStringId = stringId,
					Name = item.Name?.ToString() ?? stringId,
					Count = 0,
					GuidePrice = Math.Max(1, item.Value),
					EquipmentElement = new EquipmentElement(item, null, null, false)
				});
			}
		}
		catch
		{
		}
		return result;
	}

	private List<RewardItemInfo> BuildRewardItemResolutionContextFromRoster(ItemRoster itemRoster, Settlement settlement = null, Hero guideHero = null)
	{
		Dictionary<string, RewardItemInfo> dictionary = new Dictionary<string, RewardItemInfo>(StringComparer.OrdinalIgnoreCase);
		if (itemRoster == null)
		{
			return new List<RewardItemInfo>();
		}
		try
		{
			for (int i = 0; i < itemRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
				EquipmentElement equipmentElement = elementCopyAtIndex.EquipmentElement;
				ItemObject item = equipmentElement.Item;
				if (item == null || elementCopyAtIndex.Amount <= 0 || (settlement != null && IsGeneratedRewardMarketExcludedItem(item)))
				{
					continue;
				}
				string key = BuildSettlementMerchantInventoryKey(equipmentElement);
				if (string.IsNullOrWhiteSpace(key))
				{
					key = item.StringId ?? "";
				}
				if (string.IsNullOrWhiteSpace(key))
				{
					continue;
				}
				if (!dictionary.TryGetValue(key, out var value))
				{
					int guidePrice = Math.Max(1, item.Value);
					if (settlement != null && TryGetSettlementBuyPrice(settlement, equipmentElement, out var settlementPrice))
					{
						guidePrice = Math.Max(1, settlementPrice);
					}
					else
					{
						guidePrice = GetGuidePriceForRewardItem(guideHero ?? Hero.MainHero, item, equipmentElement);
					}
					value = (dictionary[key] = new RewardItemInfo
					{
						Item = item,
						StringId = item.StringId ?? "",
						PromptStringId = key,
						ModifierStringId = equipmentElement.ItemModifier?.StringId ?? "",
						Name = BuildSettlementMerchantDisplayName(equipmentElement),
						Count = 0,
						GuidePrice = guidePrice,
						EquipmentElement = equipmentElement
					});
				}
				value.Count += elementCopyAtIndex.Amount;
			}
		}
		catch
		{
		}
		return dictionary.Values.ToList();
	}

	private List<RewardItemInfo> BuildHeroRewardItemResolutionContext(Hero hero)
	{
		try
		{
			return BuildHeroRewardPostprocessItems(hero)
				.Where((RewardItemInfo x) => x != null && x.Item != null)
				.ToList();
		}
		catch
		{
			return new List<RewardItemInfo>();
		}
	}

	private List<RewardItemInfo> BuildPartyRewardItemResolutionContext(PartyBase party)
	{
		try
		{
			return BuildPartyRewardPostprocessItems(party)
				.Where((RewardItemInfo x) => x != null && x.Item != null)
				.ToList();
		}
		catch
		{
			return new List<RewardItemInfo>();
		}
	}

	private List<RewardItemInfo> BuildSettlementRewardItemResolutionContext(Settlement settlement)
	{
		return BuildRewardItemResolutionContextFromRoster(settlement?.ItemRoster, settlement);
	}

	private static IEnumerable<string> GetRewardItemResolutionAliases(RewardItemInfo item)
	{
		List<string> aliases = PromptListRetrievalService.GetRewardItemAliases(item).ToList();
		string promptStringId = (item?.PromptStringId ?? "").Trim();
		if (TryParseNotableMarketPromptStringId(promptStringId, out var settlementPromptStringId))
		{
			aliases.Add(settlementPromptStringId);
		}
		if (TryParseSettlementMerchantPromptStringId(promptStringId, out var itemId, out var modifierId))
		{
			aliases.Add(itemId);
			if (!string.IsNullOrWhiteSpace(modifierId))
			{
				aliases.Add(itemId + "@" + modifierId);
			}
		}
		return aliases.Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase);
	}

	private static string BuildRewardItemResolutionActionKey(RewardItemInfo item)
	{
		string text = item?.PromptStringId;
		if (string.IsNullOrWhiteSpace(text))
		{
			text = item?.StringId;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = item?.Item?.StringId;
		}
		return text ?? "";
	}

	private static string BuildRewardItemTransferLookup(RewardItemResolution resolution)
	{
		string text = resolution?.ActionKey ?? "";
		if (TryParseNotableMarketPromptStringId(text, out var settlementPromptStringId))
		{
			text = settlementPromptStringId;
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		return resolution?.MatchedStringId ?? "";
	}

	private bool TryFindBestRewardItemResolution(string lookup, IEnumerable<RewardItemInfo> contextItems, bool includeZeroScore, out RewardItemResolution resolution, string logSource = null, bool logMatch = true, bool logMiss = true)
	{
		resolution = null;
		string text = (lookup ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		Dictionary<string, RewardItemResolutionCandidate> dictionary = new Dictionary<string, RewardItemResolutionCandidate>(StringComparer.OrdinalIgnoreCase);
		int order = 0;
		foreach (RewardItemInfo item in contextItems ?? Enumerable.Empty<RewardItemInfo>())
		{
			AddRewardItemResolutionCandidate(dictionary, item, isContext: true, ref order);
		}
		foreach (RewardItemInfo item2 in BuildGlobalRewardItemResolutionItems())
		{
			AddRewardItemResolutionCandidate(dictionary, item2, isContext: false, ref order);
		}
		ItemObject forcedGeneratedTemplate = null;
		if (includeZeroScore)
		{
			TryResolveGeneratedRpEquipmentTemplate(text, out forcedGeneratedTemplate, out GeneratedRpEquipmentKind equipmentKind, out _, out _, out _);
			if (forcedGeneratedTemplate == null && equipmentKind == GeneratedRpEquipmentKind.None)
			{
				TryResolveGeneratedRpFoodTemplate(text, out forcedGeneratedTemplate, out _, out _, out _, out _);
			}
		}
		var scored = dictionary.Values
			.Where((RewardItemResolutionCandidate x) => x?.Info?.Item != null)
			.Where((RewardItemResolutionCandidate x) => !includeZeroScore
				|| (forcedGeneratedTemplate != null
					? IsSameGeneratedRewardTemplateItem(x.Info.Item, forcedGeneratedTemplate)
					: (IsGeneratedRewardMiscItemType(x.Info.Item) && IsCloneSafeGeneratedRewardTemplateItem(x.Info.Item))))
			.Select(delegate(RewardItemResolutionCandidate x)
			{
				float score = WorldEntityRetrievalService.CalculateBestAliasScoreForExternal(text, GetRewardItemResolutionAliases(x.Info));
				float templateScore = includeZeroScore ? CalculateGeneratedRewardTemplateScore(text, x.Info, score) : score;
				float templateTieBreaker = includeZeroScore ? CalculateGeneratedRewardTemplateDiversityTieBreaker(text, x.Info) : 0f;
				return new
				{
					Candidate = x,
					Score = score,
					TemplateScore = templateScore,
					TemplateTieBreaker = templateTieBreaker
				};
			})
			.Where(x => includeZeroScore || x.Score > 0f)
			.OrderByDescending(x => x.TemplateScore)
			.ThenByDescending(x => x.Score)
			.ThenByDescending(x => x.Candidate.IsContext)
			.ThenByDescending(x => x.TemplateTieBreaker)
			.ThenBy(x => x.Candidate.Info.Name ?? "", StringComparer.OrdinalIgnoreCase)
			.ThenBy(x => x.Candidate.Info.StringId ?? x.Candidate.Info.Item.StringId ?? "", StringComparer.OrdinalIgnoreCase)
			.ThenBy(x => x.Candidate.Order)
			.ToList();
		if (scored.Count == 0)
		{
			if (logMiss)
			{
				try
				{
					Logger.Log("Logic", "[RewardItemResolve] miss source=" + (logSource ?? "") + " lookup=" + text + " score=0.0000 second=0.0000 threshold=" + FormatRewardItemResolutionScore(RewardItemNameMatchThreshold));
				}
				catch
				{
				}
			}
			return false;
		}
		var best = scored[0];
		float secondScore = (scored.Count > 1) ? scored[1].Score : 0f;
		RewardItemInfo info = best.Candidate.Info;
		ItemObject itemObject = info.Item;
		resolution = new RewardItemResolution
		{
			Info = info,
			Item = itemObject,
			EquipmentElement = info.EquipmentElement.Item != null ? info.EquipmentElement : new EquipmentElement(itemObject, null, null, false),
			ActionKey = BuildRewardItemResolutionActionKey(info),
			MatchedName = info.Name ?? itemObject.Name?.ToString() ?? itemObject.StringId ?? text,
			MatchedStringId = itemObject.StringId ?? info.StringId ?? "",
			BestScore = best.Score,
			SecondScore = secondScore,
			IsContext = best.Candidate.IsContext
		};
		if (logMatch)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] source=" + (logSource ?? "") + " lookup=" + text + " matched=" + (resolution.MatchedName ?? "") + " stringId=" + (resolution.MatchedStringId ?? "") + " score=" + FormatRewardItemResolutionScore(resolution.BestScore) + " second=" + FormatRewardItemResolutionScore(resolution.SecondScore) + " context=" + resolution.IsContext);
			}
			catch
			{
			}
		}
		return resolution.Item != null && !string.IsNullOrWhiteSpace(resolution.MatchedStringId);
	}

	private bool TryResolveRewardItemByNameOrId(string lookup, IEnumerable<RewardItemInfo> contextItems, out RewardItemResolution resolution, string logSource = null)
	{
		if (!TryFindBestRewardItemResolution(lookup, contextItems, includeZeroScore: false, out resolution, logSource))
		{
			return false;
		}
		if (resolution.BestScore + 0.00001f < RewardItemNameMatchThreshold)
		{
			resolution = null;
			return false;
		}
		return resolution.Item != null && !string.IsNullOrWhiteSpace(resolution.MatchedStringId);
	}

	private bool TryResolveRewardItemForForcedGeneration(string lookup, IEnumerable<RewardItemInfo> contextItems, out RewardItemResolution resolution, string logSource = null)
	{
		if (TryResolveRewardItemByNameOrId(lookup, contextItems, out resolution, logSource))
		{
			return true;
		}
		if (!TryFindBestRewardItemResolution(lookup, contextItems, includeZeroScore: true, out var templateResolution, logSource, logMatch: false, logMiss: false))
		{
			resolution = null;
			return false;
		}
		return TryCreateGeneratedRewardItemResolution(lookup, templateResolution, out resolution, logSource);
	}

	private static bool TryCreateGeneratedRewardItemResolution(string lookup, RewardItemResolution templateResolution, out RewardItemResolution resolution, string logSource = null, string identityKey = null)
	{
		resolution = null;
		string requestedName = (lookup ?? "").Trim();
		ItemObject templateItem = templateResolution?.Item;
		string normalizedIdentityKey = (identityKey ?? "").Trim();
		bool isPlayerRpCraftGeneration =
			IsAuthorizedPlayerRpCraftGenerationKey(normalizedIdentityKey);
		if (string.IsNullOrWhiteSpace(requestedName))
		{
			return false;
		}
		if (!isPlayerRpCraftGeneration
			&& !IsGeneratedRewardTemplateCompatibleWithRequestedName(templateItem, requestedName))
		{
			templateItem = ResolveGeneratedInventoryTemplateItem(templateResolution?.MatchedStringId, requestedName);
		}
		if (!isPlayerRpCraftGeneration
			&& !IsGeneratedRewardTemplateCompatibleWithRequestedName(templateItem, requestedName))
		{
			templateItem = ResolveGeneratedInventoryTemplateItem(null, requestedName);
		}
		if (isPlayerRpCraftGeneration
			? !IsSafePlayerRpCraftGenerationTemplate(templateItem)
			: !IsGeneratedRewardTemplateCompatibleWithRequestedName(templateItem, requestedName))
		{
			return false;
		}
		string templateStringId = templateItem.StringId ?? templateResolution.MatchedStringId ?? "";
		string generatedStringId = isPlayerRpCraftGeneration
			? normalizedIdentityKey
			: BuildGeneratedRewardItemStringId(string.IsNullOrWhiteSpace(normalizedIdentityKey) ? requestedName : normalizedIdentityKey, templateStringId);
		ItemObject generatedItem = TryGetOrCreateGeneratedRewardItem(generatedStringId, requestedName, templateItem, logSource);
		if (generatedItem == null || !TryEnsureGeneratedRewardItemCategory(generatedItem, templateItem, logSource))
		{
			return false;
		}
		EquipmentElement equipmentElement = new EquipmentElement(generatedItem, null, null, false);
		resolution = new RewardItemResolution
		{
			Info = new RewardItemInfo
			{
				Item = generatedItem,
				StringId = generatedStringId,
				PromptStringId = generatedStringId,
				Name = requestedName,
				Count = 0,
				GuidePrice = Math.Max(1, generatedItem.Value),
				EquipmentElement = equipmentElement
			},
			Item = generatedItem,
			EquipmentElement = equipmentElement,
			ActionKey = generatedStringId,
			MatchedName = requestedName,
			MatchedStringId = generatedStringId,
			BestScore = templateResolution.BestScore,
			SecondScore = templateResolution.SecondScore,
			IsContext = templateResolution.IsContext,
			IsGeneratedFromLowScore = true,
			TemplateItem = templateItem,
			RequestedName = requestedName
		};
		try
		{
			Logger.Log("Logic", "[RewardItemResolve] generated_low_score source=" + (logSource ?? "") + " lookup=" + FormatGeneratedRewardNameForLog(generatedStringId, requestedName) + " generated=" + generatedStringId + " template=" + (templateResolution.MatchedName ?? templateItem.Name?.ToString() ?? "") + " templateStringId=" + (templateItem.StringId ?? "") + " score=" + FormatRewardItemResolutionScore(resolution.BestScore) + " second=" + FormatRewardItemResolutionScore(resolution.SecondScore));
		}
		catch
		{
		}
		return true;
	}

	public static int GenerateNamedInventoryItemToRosterForExternal(ItemRoster targetRoster, string requestedName, int amount, out string generatedStringId, out string itemName, string logSource = null, string identityKey = null, string preferredTemplateItemId = null)
	{
		return GenerateNamedInventoryItemToRosterForExternal(
			targetRoster,
			requestedName,
			amount,
			out generatedStringId,
			out itemName,
			logSource,
			identityKey,
			preferredTemplateItemId,
			mutationObservation: null);
	}

	private static int GenerateNamedInventoryItemToRosterForExternal(ItemRoster targetRoster, string requestedName, int amount, out string generatedStringId, out string itemName, string logSource, string identityKey, string preferredTemplateItemId, EconomyMutationObservation mutationObservation)
	{
		generatedStringId = null;
		itemName = null;
		try
		{
			if (targetRoster == null || string.IsNullOrWhiteSpace(requestedName) || amount <= 0)
			{
				return 0;
			}
			RewardItemResolution templateResolution = null;
			ItemObject preferredTemplate = ResolveItemById(preferredTemplateItemId);
			bool isPlayerRpCraftGeneration =
				IsAuthorizedPlayerRpCraftGenerationKey(identityKey);
			if (isPlayerRpCraftGeneration
				? IsSafePlayerRpCraftGenerationTemplate(preferredTemplate)
				: IsGeneratedRewardTemplateCompatibleWithRequestedName(preferredTemplate, requestedName))
			{
				EquipmentElement preferredTemplateEquipment = new EquipmentElement(preferredTemplate, null, null, false);
				templateResolution = new RewardItemResolution
				{
					Info = new RewardItemInfo
					{
						Item = preferredTemplate,
						StringId = preferredTemplate.StringId ?? "",
						PromptStringId = preferredTemplate.StringId ?? "",
						Name = preferredTemplate.Name?.ToString() ?? preferredTemplate.StringId ?? "",
						Count = 0,
						GuidePrice = Math.Max(1, preferredTemplate.Value),
						EquipmentElement = preferredTemplateEquipment
					},
					Item = preferredTemplate,
					EquipmentElement = preferredTemplateEquipment,
					ActionKey = preferredTemplate.StringId ?? "",
					MatchedName = preferredTemplate.Name?.ToString() ?? preferredTemplate.StringId ?? "",
					MatchedStringId = preferredTemplate.StringId ?? "",
					BestScore = 1f,
					SecondScore = 0f,
					IsContext = false,
					TemplateItem = preferredTemplate
				};
			}
			else if (!string.IsNullOrWhiteSpace(preferredTemplateItemId))
			{
				string rejectionReason = IsCloneSafeGeneratedRewardAnyTemplateItem(preferredTemplate) ? "name_category_mismatch" : GetGeneratedRewardTemplateThumbnailRejectionReason(preferredTemplate);
				Logger.Log("Logic", "[RewardItemResolve] preferred_template_rejected source=" + (logSource ?? "") + " template=" + preferredTemplateItemId.Trim() + " lookup=" + FormatGeneratedRewardNameForLog(identityKey, requestedName) + " fallback=auto reason=" + rejectionReason);
			}
			RewardSystemBehavior instance = Instance;
			if (templateResolution == null && instance != null)
			{
				try
				{
					List<RewardItemInfo> contextItems = instance.BuildRewardItemResolutionContextFromRoster(targetRoster);
					instance.TryFindBestRewardItemResolution(requestedName, contextItems, includeZeroScore: true, out templateResolution, logSource ?? "external_generate_named", logMatch: false, logMiss: false);
					if (!IsGeneratedRewardTemplateCompatibleWithRequestedName(templateResolution?.Item, requestedName))
					{
						templateResolution = null;
					}
				}
				catch
				{
				}
			}
			if (templateResolution?.Item == null)
			{
				ItemObject fallbackTemplate = ResolveGeneratedInventoryTemplateItem(null, requestedName);
				if (fallbackTemplate == null)
				{
					return 0;
				}
				EquipmentElement templateEquipment = new EquipmentElement(fallbackTemplate, null, null, false);
				templateResolution = new RewardItemResolution
				{
					Info = new RewardItemInfo
					{
						Item = fallbackTemplate,
						StringId = fallbackTemplate.StringId ?? "",
						PromptStringId = fallbackTemplate.StringId ?? "",
						Name = fallbackTemplate.Name?.ToString() ?? fallbackTemplate.StringId ?? "",
						Count = 0,
						GuidePrice = Math.Max(1, fallbackTemplate.Value),
						EquipmentElement = templateEquipment
					},
					Item = fallbackTemplate,
					EquipmentElement = templateEquipment,
					ActionKey = fallbackTemplate.StringId ?? "",
					MatchedName = fallbackTemplate.Name?.ToString() ?? fallbackTemplate.StringId ?? "",
					MatchedStringId = fallbackTemplate.StringId ?? "",
					BestScore = 0f,
					SecondScore = 0f,
					IsContext = false,
					TemplateItem = fallbackTemplate
				};
			}
			if (!TryCreateGeneratedRewardItemResolution(requestedName, templateResolution, out RewardItemResolution resolution, logSource ?? "external_generate_named", identityKey))
			{
				return 0;
			}
			generatedStringId = resolution.MatchedStringId;
			int generated = GenerateResolvedItemsToRoster(
				targetRoster,
				resolution,
				amount,
				out itemName,
				mutationObservation);
			if (generated > 0)
			{
				TryPrimeGeneratedInventoryItemForExternal(generatedStringId, requestedName, resolution.TemplateItem?.StringId, resolution.Item?.Id.InternalValue ?? 0u, out _, out _, out _, (logSource ?? "external_generate_named") + "_prime");
			}
			return generated;
		}
		catch (Exception ex)
		{
			mutationObservation?.MarkUnknown("economy.generated_item_exception");
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generate_named_inventory_failed source=" + (logSource ?? "") + " error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
			return 0;
		}
	}

	public static bool TryPrimeGeneratedInventoryItemForExternal(string generatedStringId, string displayName, string templateItemId, uint objectId, out string normalizedStringId, out string normalizedTemplateItemId, out uint normalizedObjectId, string logSource = null)
	{
		normalizedStringId = null;
		normalizedTemplateItemId = null;
		normalizedObjectId = 0u;
		try
		{
			string key = (generatedStringId ?? "").Trim();
			string name = NormalizeGeneratedInventoryDisplayName(displayName);
			if (!IsGeneratedRewardItemStringId(key) || string.IsNullOrWhiteSpace(name))
			{
				return false;
			}
			GeneratedRewardItemRecord existingRecord = Instance?.GetGeneratedRewardItemRecord(key);
			if (existingRecord == null)
			{
				EnsureGeneratedRewardManifestLoaded();
				lock (GeneratedRewardItemRegistrationLock)
				{
					GeneratedRewardManifestByStringId.TryGetValue(key, out existingRecord);
				}
			}
			if (existingRecord != null)
			{
				if (string.IsNullOrWhiteSpace(templateItemId))
				{
					templateItemId = existingRecord.TemplateStringId;
				}
				if (objectId == 0u)
				{
					objectId = existingRecord.ObjectId;
				}
				if (string.IsNullOrWhiteSpace(name))
				{
					name = existingRecord.DisplayName;
				}
			}
			ItemObject registered = TryGetRegisteredGeneratedRewardItemByStringId(key);
			if (registered != null && objectId == 0u)
			{
				objectId = registered.Id.InternalValue;
			}
			bool isPlayerRpCraftGeneration = IsAuthorizedPlayerRpCraftGenerationKey(key);
			ItemObject templateItem;
			if (existingRecord?.PlayerCraft != null)
			{
				templateItem = ResolveGeneratedRewardRecordTemplateItem(
					existingRecord,
					logSource ?? "external_prime_player");
			}
			else if (isPlayerRpCraftGeneration)
			{
				// During a player-craft transaction the persistent PlayerCraft payload is
				// attached immediately after the item is created. Do not run its exact,
				// caller-selected equipment template through the ordinary RP-name suffix
				// resolver in this short pre-registration window.
				templateItem = ResolveItemById(templateItemId);
				if (!IsSafePlayerRpCraftGenerationTemplate(templateItem))
				{
					return false;
				}
			}
			else
			{
				templateItem = ResolveGeneratedInventoryTemplateItem(templateItemId, name);
				if (!IsGeneratedRewardTemplateCompatibleWithRequestedName(templateItem, name))
				{
					templateItem = ResolveGeneratedInventoryTemplateItem(null, name);
				}
				if (!IsGeneratedRewardTemplateCompatibleWithRequestedName(templateItem, name))
				{
					return false;
				}
			}
			if (templateItem == null)
			{
				return false;
			}
			GeneratedRewardItemRecord record = new GeneratedRewardItemRecord
			{
				GeneratedStringId = key,
				DisplayName = name,
				TemplateStringId = (templateItem.StringId ?? "").Trim(),
				ObjectId = objectId,
				LegacyObjectIds = existingRecord?.LegacyObjectIds != null ? existingRecord.LegacyObjectIds.ToList() : new List<uint>(),
				LastTouchedDay = Math.Max(existingRecord?.LastTouchedDay ?? 0, GetCampaignDayIndex()),
				RpItemIntroductionText = existingRecord?.RpItemIntroductionText,
				RpItemIntroductionSource = existingRecord?.RpItemIntroductionSource,
				RpItemIntroductionLastTouchedDay = existingRecord?.RpItemIntroductionLastTouchedDay ?? 0,
				PlayerCraft = existingRecord?.PlayerCraft
			};
			if (record.ObjectId == 0u && TryGetGeneratedRewardItemId(key, templateItem, 0u, out var stableObjectId, logSource ?? "external_prime"))
			{
				record.ObjectId = stableObjectId.InternalValue;
			}
			GeneratedRewardItemRecord normalizedRecord = NormalizeGeneratedRewardItemRecord(key, record);
			if (normalizedRecord == null)
			{
				return false;
			}
			DiscardGeneratedRewardRecordObjectIdIfPolluted(normalizedRecord, templateItem, logSource ?? "external_prime");
			RewardSystemBehavior instance = Instance;
			if (instance != null)
			{
				instance.EnsureGeneratedRewardItemData();
				instance._generatedRewardItemRecords[normalizedRecord.GeneratedStringId] = normalizedRecord;
			}
			RegisterGeneratedRewardManifestRecord(normalizedRecord);
			ItemObject generatedItem = TryGetOrCreateGeneratedRewardItem(normalizedRecord.GeneratedStringId, normalizedRecord.DisplayName, templateItem, logSource ?? "external_prime");
			if (generatedItem != null)
			{
				normalizedRecord.ObjectId = generatedItem.Id.InternalValue != 0u ? generatedItem.Id.InternalValue : normalizedRecord.ObjectId;
				Instance?.RememberGeneratedRewardItemRecord(normalizedRecord.GeneratedStringId, normalizedRecord.DisplayName, templateItem, generatedItem);
			}
			else
			{
				SaveGeneratedRewardManifest(logSource ?? "external_prime");
			}
			GeneratedRewardItemRecord finalRecord = Instance?.GetGeneratedRewardItemRecord(normalizedRecord.GeneratedStringId) ?? normalizedRecord;
			normalizedStringId = finalRecord.GeneratedStringId;
			normalizedTemplateItemId = finalRecord.TemplateStringId;
			normalizedObjectId = finalRecord.ObjectId;
			return true;
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_external_prime_failed source=" + (logSource ?? "") + " generated=" + (generatedStringId ?? "") + " error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
			return false;
		}
	}

	public static int GenerateKnownInventoryItemToRosterForExternal(ItemRoster targetRoster, string generatedStringId, string displayName, string templateItemId, uint objectId, int amount, out string itemName, out string normalizedStringId, out string normalizedTemplateItemId, out uint normalizedObjectId, string logSource = null)
	{
		itemName = null;
		normalizedStringId = null;
		normalizedTemplateItemId = null;
		normalizedObjectId = 0u;
		try
		{
			if (targetRoster == null || amount <= 0)
			{
				return 0;
			}
			if (!TryPrepareGeneratedRewardInventoryElementForRoster(generatedStringId, displayName, templateItemId, objectId, out EquipmentElement equipmentElement, out itemName, out normalizedStringId, out normalizedTemplateItemId, out normalizedObjectId, logSource ?? "external_known_generate"))
			{
				return 0;
			}
			int generated = AddEquipmentElementToRosterAndCountDelta(targetRoster, equipmentElement, amount, logSource ?? "external_known_generate");
			return generated;
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_external_known_generate_failed source=" + (logSource ?? "") + " generated=" + (generatedStringId ?? "") + " error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
			return 0;
		}
	}

	private static bool TryPrepareGeneratedRewardInventoryElementForRoster(string generatedStringId, string displayName, string templateItemId, uint objectId, out EquipmentElement equipmentElement, out string itemName, out string normalizedStringId, out string normalizedTemplateItemId, out uint normalizedObjectId, string logSource = null)
	{
		equipmentElement = default(EquipmentElement);
		itemName = null;
		normalizedStringId = null;
		normalizedTemplateItemId = null;
		normalizedObjectId = 0u;
		try
		{
			string name = NormalizeGeneratedInventoryDisplayName(displayName);
			if (string.IsNullOrWhiteSpace(name))
			{
				LogGeneratedRewardInventoryGuard("empty_name", generatedStringId, "", templateItemId, null, null, logSource);
				return false;
			}
			if (!TryPrimeGeneratedInventoryItemForExternal(generatedStringId, name, templateItemId, objectId, out normalizedStringId, out normalizedTemplateItemId, out normalizedObjectId, logSource ?? "prepare_generated_inventory"))
			{
				LogGeneratedRewardInventoryGuard("prime_failed", generatedStringId, name, templateItemId, null, null, logSource);
				return false;
			}
			GeneratedRewardItemRecord preparedRecord =
				Instance?.GetGeneratedRewardItemRecord(normalizedStringId);
			bool isPlayerRpCraftGeneration =
				preparedRecord?.PlayerCraft != null
				|| IsAuthorizedPlayerRpCraftGenerationKey(normalizedStringId);
			ItemObject templateItem = preparedRecord?.PlayerCraft != null
				? ResolveGeneratedRewardRecordTemplateItem(
					preparedRecord,
					logSource ?? "prepare_generated_inventory_player")
				: (isPlayerRpCraftGeneration
					? ResolveItemById(normalizedTemplateItemId)
					: ResolveGeneratedInventoryTemplateItem(normalizedTemplateItemId, name));
			bool isCompatibleTemplate = isPlayerRpCraftGeneration
				? IsSafePlayerRpCraftGenerationTemplate(templateItem)
				: IsGeneratedRewardTemplateCompatibleWithRequestedName(templateItem, name);
			if (!isCompatibleTemplate)
			{
				LogGeneratedRewardInventoryGuard("bad_template", normalizedStringId, name, normalizedTemplateItemId, null, templateItem, logSource);
				return false;
			}
			ItemObject generatedItem = TryGetOrCreateGeneratedRewardItem(normalizedStringId, name, templateItem, logSource ?? "prepare_generated_inventory");
			if (!TryValidateGeneratedRewardInventoryItemForRoster(generatedItem, normalizedStringId, name, templateItem, logSource ?? "prepare_generated_inventory"))
			{
				return false;
			}
			normalizedObjectId = generatedItem.Id.InternalValue != 0u ? generatedItem.Id.InternalValue : normalizedObjectId;
			Instance?.RememberGeneratedRewardItemRecord(normalizedStringId, name, templateItem, generatedItem);
			equipmentElement = new EquipmentElement(generatedItem, null, null, false);
			itemName = name;
			return true;
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_inventory_prepare_failed source=" + (logSource ?? "") + " generated=" + (generatedStringId ?? "") + " error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
			return false;
		}
	}

	public static List<GeneratedInventoryItemSnapshot> ExportGeneratedInventoryItemsForExternal(string logSource = null)
	{
		List<GeneratedInventoryItemSnapshot> result = new List<GeneratedInventoryItemSnapshot>();
		try
		{
			EnsureGeneratedRewardManifestLoaded();
			Dictionary<string, GeneratedRewardItemRecord> records = new Dictionary<string, GeneratedRewardItemRecord>(StringComparer.OrdinalIgnoreCase);
			if (Instance?._generatedRewardItemRecords != null)
			{
				foreach (KeyValuePair<string, GeneratedRewardItemRecord> pair in Instance._generatedRewardItemRecords.ToList())
				{
					GeneratedRewardItemRecord record = NormalizeGeneratedRewardItemRecord(pair.Key, pair.Value);
					if (record != null)
					{
						records[record.GeneratedStringId] = record;
					}
				}
			}
			foreach (GeneratedRewardItemRecord record in records.Values)
			{
				result.Add(new GeneratedInventoryItemSnapshot
				{
					GeneratedStringId = record.GeneratedStringId,
					DisplayName = record.DisplayName,
					TemplateStringId = record.TemplateStringId,
					ObjectId = record.ObjectId
				});
			}
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] export_generated_inventory_failed source=" + (logSource ?? "") + " error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
		}
		return result;
	}

	public static bool TryCreateGeneratedInventoryItemForExternal(string displayName, string identityKey, out ItemObject generatedItem, string templateItemId = null, string logSource = null)
	{
		generatedItem = null;
		try
		{
			string name = NormalizeGeneratedInventoryDisplayName(displayName);
			if (string.IsNullOrWhiteSpace(name))
			{
				return false;
			}
			ItemObject templateItem = ResolveGeneratedInventoryTemplateItem(templateItemId, name);
			if (templateItem == null)
			{
				return false;
			}
			string identity = string.IsNullOrWhiteSpace(identityKey) ? name : identityKey.Trim();
			string generatedStringId = BuildGeneratedRewardItemStringId(identity, templateItem.StringId ?? "");
			generatedItem = TryGetOrCreateGeneratedRewardItem(generatedStringId, name, templateItem, logSource ?? "external_generated_inventory");
			return generatedItem != null && TryEnsureGeneratedRewardItemCategory(generatedItem, templateItem, logSource ?? "external_generated_inventory");
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_external_inventory_failed source=" + (logSource ?? "") + " error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
			return false;
		}
	}

	private static string NormalizeGeneratedInventoryDisplayName(string displayName)
	{
		string name = AnimusForgeTextInputSanitizer.SanitizeMultiline(displayName ?? "", AnimusForgeTextInputSanitizer.MaxCourierLetterChars + 512).Trim();
		if (string.IsNullOrWhiteSpace(name))
		{
			return "";
		}
		return name;
	}

	private static ItemObject ResolveGeneratedInventoryTemplateItem(string templateItemId, string displayName)
	{
		ItemObject explicitTemplate = ResolveItemById(templateItemId);
		if (IsGeneratedRewardTemplateCompatibleWithRequestedName(explicitTemplate, displayName))
		{
			return explicitTemplate;
		}
		if (TryResolveGeneratedRpEquipmentTemplate(displayName, out ItemObject equipmentTemplate, out GeneratedRpEquipmentKind equipmentKind, out _, out _, out _)
			&& IsCloneSafeGeneratedRewardTemplateItem(equipmentTemplate))
		{
			return equipmentTemplate;
		}
		if (equipmentKind != GeneratedRpEquipmentKind.None)
		{
			return null;
		}
		if (TryResolveGeneratedRpFoodTemplate(displayName, out ItemObject foodTemplate, out GeneratedRpFoodKind foodKind, out _, out _, out _)
			&& IsCloneSafeGeneratedRpFoodTemplateItem(foodTemplate))
		{
			return foodTemplate;
		}
		if (foodKind != GeneratedRpFoodKind.None)
		{
			return null;
		}
		try
		{
			IEnumerable<ItemObject> items = Game.Current?.ObjectManager?.GetObjectTypeList<ItemObject>() ?? MBObjectManager.Instance?.GetObjectTypeList<ItemObject>();
			ItemObject goods = null;
			ItemObject book = null;
			foreach (ItemObject item in items ?? Enumerable.Empty<ItemObject>())
			{
				if (!IsGeneratedRewardMiscItemType(item) || !IsCloneSafeGeneratedRewardTemplateItem(item))
				{
					continue;
				}
				if (item.Type == ItemObject.ItemTypeEnum.Goods)
				{
					if (ContainsGeneratedRewardItemTextAny(item, "book", "letter", "scroll", "decree", "paper", "parchment", "ledger", "document"))
					{
						return item;
					}
					if (goods == null && item.ItemCategory != null)
					{
						goods = item;
					}
				}
				else if (book == null && item.Type == ItemObject.ItemTypeEnum.Book)
				{
					book = item;
				}
			}
			return goods ?? book ?? GetGeneratedRewardFallbackTemplateItem();
		}
		catch
		{
		}
		return GetGeneratedRewardFallbackTemplateItem();
	}

	private static ItemObject ResolveCloneSafeGeneratedRewardTemplateItem(ItemObject templateItem, string displayName, string logSource, string generatedStringId = null)
	{
		if (IsGeneratedRewardTemplateCompatibleWithRequestedName(templateItem, displayName))
		{
			return templateItem;
		}
		string rejectedTemplateId = (templateItem?.StringId ?? "").Trim();
		string rejectionReason = IsCloneSafeGeneratedRewardAnyTemplateItem(templateItem) ? "name_category_mismatch" : GetGeneratedRewardTemplateThumbnailRejectionReason(templateItem);
		ItemObject replacement = ResolveGeneratedInventoryTemplateItem(null, displayName);
		if (!IsGeneratedRewardTemplateCompatibleWithRequestedName(replacement, displayName))
		{
			replacement = null;
		}
		if (replacement != null)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_template_guard source=" + (logSource ?? "") + " name=" + FormatGeneratedRewardNameForLog(generatedStringId, displayName) + " rejected=" + rejectedTemplateId + " reason=" + rejectionReason + " replacement=" + (replacement.StringId ?? ""));
			}
			catch
			{
			}
		}
		return replacement;
	}

	private static bool IsCloneSafeGeneratedRewardTemplateItem(ItemObject item)
	{
		return IsStableGeneratedRewardTemplateItem(item) && HasCloneSafeGeneratedRewardThumbnailSource(item);
	}

	private static bool IsCloneSafeGeneratedRpFoodTemplateItem(ItemObject item)
	{
		return IsStableGeneratedRewardTemplateIdentity(item)
			&& IsGeneratedRpFoodTemplateItem(item)
			&& HasCloneSafeGeneratedRewardThumbnailSource(item);
	}

	private static bool IsCloneSafeGeneratedRewardAnyTemplateItem(ItemObject item)
	{
		return IsCloneSafeGeneratedRewardTemplateItem(item) || IsCloneSafeGeneratedRpFoodTemplateItem(item);
	}

	private static bool IsGeneratedRpFoodTemplateItem(ItemObject item)
	{
		if (item == null || item.Type != ItemObject.ItemTypeEnum.Goods)
		{
			return false;
		}
		try
		{
			// A food suffix must always result in an actually consumable ItemObject.
			// Category alone is insufficient because conversion mods can reuse native
			// food categories for non-consumable goods.
			return item.IsFood;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsGeneratedRpKnownFoodCategory(ItemObject item)
	{
		return IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Grain)
			|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Meat)
			|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Cheese)
			|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Fish)
			|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Grape)
			|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.DateFruit)
			|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Olives)
			|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Beer)
			|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Wine)
			|| IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Butter);
	}

	private static bool IsGeneratedRpFoodItemCategory(ItemObject item, ItemCategory category)
	{
		ItemCategory itemCategory = item?.ItemCategory;
		if (itemCategory == null || category == null)
		{
			return false;
		}
		if (ReferenceEquals(itemCategory, category))
		{
			return true;
		}
		string itemCategoryId = itemCategory.StringId;
		string categoryId = category.StringId;
		return !string.IsNullOrWhiteSpace(itemCategoryId)
			&& string.Equals(itemCategoryId, categoryId, StringComparison.OrdinalIgnoreCase);
	}

	private static bool HasCloneSafeGeneratedRewardThumbnailSource(ItemObject item)
	{
		if (item == null)
		{
			return false;
		}
		try
		{
			if (item.IsCraftedWeapon)
			{
				return item.WeaponDesign?.Template != null && item.WeaponComponent?.PrimaryWeapon != null;
			}
			if (IsSettlementWeaponLikeItem(item) && item.WeaponComponent?.PrimaryWeapon == null)
			{
				return false;
			}
			if ((IsSettlementArmorLikeItem(item) || item.Type == ItemObject.ItemTypeEnum.HorseHarness) && item.ArmorComponent == null)
			{
				return false;
			}
			if (item.Type == ItemObject.ItemTypeEnum.Arrows || item.Type == ItemObject.ItemTypeEnum.Bolts || item.Type == ItemObject.ItemTypeEnum.SlingStones)
			{
				return !string.IsNullOrWhiteSpace(item.HolsterMeshName);
			}
			return !string.IsNullOrWhiteSpace(item.MultiMeshName);
		}
		catch
		{
			return false;
		}
	}

	private static string GetGeneratedRewardTemplateThumbnailRejectionReason(ItemObject item)
	{
		if (item == null)
		{
			return "missing_template";
		}
		try
		{
			if (item.IsCraftedWeapon)
			{
				if (item.WeaponDesign?.Template == null)
				{
					return "crafted_weapon_missing_template";
				}
				return item.WeaponComponent?.PrimaryWeapon == null ? "crafted_weapon_missing_primary_weapon" : "unstable_template";
			}
			if (IsSettlementWeaponLikeItem(item) && item.WeaponComponent?.PrimaryWeapon == null)
			{
				return "missing_primary_weapon";
			}
			if ((IsSettlementArmorLikeItem(item) || item.Type == ItemObject.ItemTypeEnum.HorseHarness) && item.ArmorComponent == null)
			{
				return "missing_armor_component";
			}
			if (item.Type == ItemObject.ItemTypeEnum.Arrows || item.Type == ItemObject.ItemTypeEnum.Bolts || item.Type == ItemObject.ItemTypeEnum.SlingStones)
			{
				return string.IsNullOrWhiteSpace(item.HolsterMeshName) ? "missing_holster_mesh" : "unstable_template";
			}
			return string.IsNullOrWhiteSpace(item.MultiMeshName) ? "missing_multi_mesh" : "unstable_template";
		}
		catch
		{
			return "template_inspection_failed";
		}
	}

	private static bool IsStableGeneratedRewardTemplateItem(ItemObject item)
	{
		return IsStableGeneratedRewardTemplateIdentity(item) && !IsGeneratedRewardAutoConsumedTemplateItem(item);
	}

	private static bool IsStableGeneratedRewardTemplateIdentity(ItemObject item)
	{
		if (item == null || item.Id.InternalValue == 0u)
		{
			return false;
		}
		string stringId = (item.StringId ?? "").Trim();
		return !IsGeneratedRewardItemStringId(stringId) && !stringId.StartsWith("af_generated_reward_pending_", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsGeneratedRewardAutoConsumedTemplateItem(ItemObject item)
	{
		if (item == null)
		{
			return false;
		}
		return item.Type == ItemObject.ItemTypeEnum.Animal
			|| item.IsFood
			|| IsGeneratedRpKnownFoodCategory(item);
	}

	private GeneratedRewardRosterItemRecord NormalizeGeneratedRewardRosterItemRecord(string fallbackKey, GeneratedRewardRosterItemRecord record)
	{
		if (record == null)
		{
			return null;
		}
		string generatedStringId = (record.GeneratedStringId ?? fallbackKey ?? "").Trim();
		if (!IsGeneratedRewardItemStringId(generatedStringId))
		{
			return null;
		}
		string displayName = AnimusForgeTextInputSanitizer.SanitizeMultiline(record.DisplayName ?? "", AnimusForgeTextInputSanitizer.MaxCourierLetterChars + 512).Trim();
		GeneratedRewardItemRecord itemRecord = GetGeneratedRewardItemRecord(generatedStringId);
		if (string.IsNullOrWhiteSpace(displayName))
		{
			displayName = (itemRecord?.DisplayName ?? generatedStringId).Trim();
		}
		if (string.IsNullOrWhiteSpace(displayName))
		{
			return null;
		}
		string templateStringId = (record.TemplateStringId ?? itemRecord?.TemplateStringId ?? "").Trim();
		uint objectId = record.ObjectId != 0u ? record.ObjectId : (itemRecord?.ObjectId ?? 0u);
		record.GeneratedStringId = generatedStringId;
		record.DisplayName = displayName;
		record.TemplateStringId = templateStringId;
		record.ObjectId = objectId;
		record.Amount = Math.Max(0, Math.Min(record.Amount, 9999));
		record.LastTouchedDay = Math.Max(0, record.LastTouchedDay);
		return record;
	}

	private void CaptureGeneratedRewardPlayerRosterItems(string reason)
	{
		try
		{
			EnsureGeneratedRewardItemData();
			ItemRoster roster = PartyBase.MainParty?.ItemRoster ?? MobileParty.MainParty?.ItemRoster;
			if (roster == null)
			{
				try
				{
					Logger.Log("Logic", "[RewardItemResolve] generated_player_roster_capture_skipped reason=" + (reason ?? "") + " player_roster=null");
				}
				catch
				{
				}
				return;
			}
			Dictionary<string, GeneratedRewardRosterItemRecord> captured = new Dictionary<string, GeneratedRewardRosterItemRecord>(StringComparer.OrdinalIgnoreCase);
			List<string> sample = new List<string>();
			for (int i = 0; i < roster.Count; i++)
			{
				ItemRosterElement element = roster.GetElementCopyAtIndex(i);
				ItemObject item = element.EquipmentElement.Item;
				if (item == null || element.Amount <= 0)
				{
					continue;
				}
				string generatedStringId = (item.StringId ?? "").Trim();
				if (!IsGeneratedRewardItemStringId(generatedStringId))
				{
					continue;
				}
				GeneratedRewardItemRecord itemRecord = GetGeneratedRewardItemRecord(generatedStringId);
				string displayName = element.EquipmentElement.GetModifiedItemName()?.ToString() ?? item.Name?.ToString() ?? itemRecord?.DisplayName ?? generatedStringId;
				string templateStringId = itemRecord?.TemplateStringId ?? "";
				uint objectId = item.Id.InternalValue != 0u ? item.Id.InternalValue : (itemRecord?.ObjectId ?? 0u);
				if (captured.TryGetValue(generatedStringId, out GeneratedRewardRosterItemRecord existing) && existing != null)
				{
					existing.Amount += element.Amount;
					existing.ObjectId = objectId != 0u ? objectId : existing.ObjectId;
					if (string.IsNullOrWhiteSpace(existing.TemplateStringId))
					{
						existing.TemplateStringId = templateStringId;
					}
					continue;
				}
				GeneratedRewardRosterItemRecord record = NormalizeGeneratedRewardRosterItemRecord(generatedStringId, new GeneratedRewardRosterItemRecord
				{
					GeneratedStringId = generatedStringId,
					DisplayName = displayName,
					TemplateStringId = templateStringId,
					ObjectId = objectId,
					Amount = element.Amount,
					LastTouchedDay = GetCampaignDayIndex()
				});
				if (record != null && record.Amount > 0)
				{
					captured[record.GeneratedStringId] = record;
					if (sample.Count < 8)
					{
						sample.Add(record.GeneratedStringId + ":" + record.Amount.ToString(CultureInfo.InvariantCulture));
					}
				}
			}
			_generatedRewardPlayerRosterRecords = captured;
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_player_roster_capture reason=" + (reason ?? "") + " rosterSlots=" + roster.Count + " records=" + captured.Count + " amount=" + captured.Values.Sum((GeneratedRewardRosterItemRecord x) => Math.Max(0, x.Amount)).ToString(CultureInfo.InvariantCulture) + " sample=" + string.Join(",", sample));
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_player_roster_capture_failed reason=" + (reason ?? "") + " error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
		}
	}

	private static int CountGeneratedRewardRosterItem(ItemRoster roster, GeneratedRewardRosterItemRecord record)
	{
		if (roster == null || record == null)
		{
			return 0;
		}
		string generatedStringId = (record.GeneratedStringId ?? "").Trim();
		uint objectId = record.ObjectId;
		if (string.IsNullOrWhiteSpace(generatedStringId) && objectId == 0u)
		{
			return 0;
		}
		int count = 0;
		for (int i = 0; i < roster.Count; i++)
		{
			ItemRosterElement element = roster.GetElementCopyAtIndex(i);
			ItemObject item = element.EquipmentElement.Item;
			if (item == null || element.Amount <= 0)
			{
				continue;
			}
			bool stringMatches = !string.IsNullOrWhiteSpace(generatedStringId) && string.Equals((item.StringId ?? "").Trim(), generatedStringId, StringComparison.OrdinalIgnoreCase);
			bool objectMatches = objectId != 0u && item.Id.InternalValue == objectId;
			if (stringMatches || objectMatches)
			{
				count += element.Amount;
			}
		}
		return count;
	}

	private void RememberGeneratedRewardPlayerRosterItem(EquipmentElement equipmentElement, int amount, string source)
	{
		try
		{
			if (amount <= 0 || equipmentElement.Item == null)
			{
				return;
			}
			EnsureGeneratedRewardItemData();
			string generatedStringId = (equipmentElement.Item.StringId ?? "").Trim();
			if (!IsGeneratedRewardItemStringId(generatedStringId))
			{
				return;
			}
			GeneratedRewardItemRecord itemRecord = GetGeneratedRewardItemRecord(generatedStringId);
			string displayName = equipmentElement.GetModifiedItemName()?.ToString() ?? equipmentElement.Item.Name?.ToString() ?? itemRecord?.DisplayName ?? generatedStringId;
			string templateStringId = itemRecord?.TemplateStringId ?? "";
			uint objectId = equipmentElement.Item.Id.InternalValue != 0u ? equipmentElement.Item.Id.InternalValue : (itemRecord?.ObjectId ?? 0u);
			GeneratedRewardRosterItemRecord existing = null;
			_generatedRewardPlayerRosterRecords?.TryGetValue(generatedStringId, out existing);
			GeneratedRewardRosterItemRecord record = NormalizeGeneratedRewardRosterItemRecord(generatedStringId, existing ?? new GeneratedRewardRosterItemRecord
			{
				GeneratedStringId = generatedStringId
			});
			record ??= new GeneratedRewardRosterItemRecord
			{
				GeneratedStringId = generatedStringId
			};
			record.DisplayName = displayName;
			if (!string.IsNullOrWhiteSpace(templateStringId))
			{
				record.TemplateStringId = templateStringId;
			}
			record.ObjectId = objectId;
			int rosterCount = 0;
			ItemRoster playerRoster = GetPlayerMainItemRoster();
			if (playerRoster != null)
			{
				GeneratedRewardRosterItemRecord countRecord = NormalizeGeneratedRewardRosterItemRecord(generatedStringId, new GeneratedRewardRosterItemRecord
				{
					GeneratedStringId = generatedStringId,
					DisplayName = displayName,
					TemplateStringId = record.TemplateStringId,
					ObjectId = objectId,
					Amount = Math.Max(1, amount),
					LastTouchedDay = GetCampaignDayIndex()
				});
				rosterCount = CountGeneratedRewardRosterItem(playerRoster, countRecord);
			}
			record.Amount = Math.Max(rosterCount, Math.Max(0, record.Amount) + amount);
			if (rosterCount > 0)
			{
				record.Amount = rosterCount;
			}
			record.LastTouchedDay = GetCampaignDayIndex();
			_generatedRewardPlayerRosterRecords[record.GeneratedStringId] = record;
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_player_roster_remember source=" + (source ?? "") + " generated=" + record.GeneratedStringId + " amount=" + amount.ToString(CultureInfo.InvariantCulture) + " recordAmount=" + record.Amount.ToString(CultureInfo.InvariantCulture) + " rosterCount=" + rosterCount.ToString(CultureInfo.InvariantCulture) + " template=" + (record.TemplateStringId ?? "") + " objectId=" + record.ObjectId.ToString(CultureInfo.InvariantCulture));
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_player_roster_remember_failed source=" + (source ?? "") + " error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
		}
	}

	private void RestoreGeneratedRewardPlayerRosterItems(string reason)
	{
		try
		{
			EnsureGeneratedRewardItemData();
			if (_generatedRewardPlayerRosterRecords == null || _generatedRewardPlayerRosterRecords.Count == 0)
			{
				try
				{
					Logger.Log("Logic", "[RewardItemResolve] generated_player_roster_restore_skipped reason=" + (reason ?? "") + " records=0");
				}
				catch
				{
				}
				return;
			}
			ItemRoster roster = PartyBase.MainParty?.ItemRoster ?? MobileParty.MainParty?.ItemRoster;
			if (roster == null)
			{
				try
				{
					Logger.Log("Logic", "[RewardItemResolve] generated_player_roster_restore_skipped reason=" + (reason ?? "") + " player_roster=null records=" + _generatedRewardPlayerRosterRecords.Count);
				}
				catch
				{
				}
				return;
			}
			int restored = 0;
			int checkedRecords = 0;
			int totalExpected = 0;
			int totalCurrent = 0;
			int totalMissing = 0;
			List<string> sample = new List<string>();
			foreach (string key in _generatedRewardPlayerRosterRecords.Keys.ToList())
			{
				GeneratedRewardRosterItemRecord record = NormalizeGeneratedRewardRosterItemRecord(key, _generatedRewardPlayerRosterRecords[key]);
				if (record == null || record.Amount <= 0)
				{
					_generatedRewardPlayerRosterRecords.Remove(key);
					continue;
				}
				checkedRecords++;
				TryPrimeGeneratedInventoryItemForExternal(record.GeneratedStringId, record.DisplayName, record.TemplateStringId, record.ObjectId, out string normalizedStringId, out string normalizedTemplateStringId, out uint normalizedObjectId, "generated_player_roster_restore_prime_" + (reason ?? ""));
				if (!string.IsNullOrWhiteSpace(normalizedStringId) && !string.Equals(record.GeneratedStringId, normalizedStringId, StringComparison.OrdinalIgnoreCase))
				{
					_generatedRewardPlayerRosterRecords.Remove(key);
					record.GeneratedStringId = normalizedStringId;
				}
				if (!string.IsNullOrWhiteSpace(normalizedTemplateStringId))
				{
					record.TemplateStringId = normalizedTemplateStringId;
				}
				if (normalizedObjectId != 0u)
				{
					record.ObjectId = normalizedObjectId;
				}
				int current = CountGeneratedRewardRosterItem(roster, record);
				int missing = Math.Max(0, record.Amount - current);
				totalExpected += Math.Max(0, record.Amount);
				totalCurrent += Math.Max(0, current);
				totalMissing += missing;
				if (missing <= 0)
				{
					_generatedRewardPlayerRosterRecords[record.GeneratedStringId] = record;
					if (sample.Count < 8)
					{
						sample.Add(record.GeneratedStringId + ":ok:" + current.ToString(CultureInfo.InvariantCulture) + "/" + record.Amount.ToString(CultureInfo.InvariantCulture));
					}
					continue;
				}
				int generated = GenerateKnownInventoryItemToRosterForExternal(roster, record.GeneratedStringId, record.DisplayName, record.TemplateStringId, record.ObjectId, missing, out _, out string restoredStringId, out string restoredTemplateStringId, out uint restoredObjectId, "generated_player_roster_restore");
				if (generated <= 0 || string.IsNullOrWhiteSpace(restoredStringId))
				{
					if (sample.Count < 8)
					{
						sample.Add(record.GeneratedStringId + ":failed:" + missing.ToString(CultureInfo.InvariantCulture));
					}
					_generatedRewardPlayerRosterRecords[record.GeneratedStringId] = record;
					continue;
				}
				restored += generated;
				if (!string.Equals(record.GeneratedStringId, restoredStringId, StringComparison.OrdinalIgnoreCase))
				{
					_generatedRewardPlayerRosterRecords.Remove(record.GeneratedStringId);
					record.GeneratedStringId = restoredStringId;
				}
				if (!string.IsNullOrWhiteSpace(restoredTemplateStringId))
				{
					record.TemplateStringId = restoredTemplateStringId;
				}
				if (restoredObjectId != 0u)
				{
					record.ObjectId = restoredObjectId;
				}
				record.Amount = CountGeneratedRewardRosterItem(roster, record);
				record.LastTouchedDay = GetCampaignDayIndex();
				_generatedRewardPlayerRosterRecords[record.GeneratedStringId] = record;
				if (sample.Count < 8)
				{
					sample.Add(record.GeneratedStringId + ":restored:" + generated.ToString(CultureInfo.InvariantCulture) + "/" + missing.ToString(CultureInfo.InvariantCulture));
				}
			}
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_player_roster_restore reason=" + (reason ?? "") + " checked=" + checkedRecords + " expected=" + totalExpected + " currentBefore=" + totalCurrent + " missing=" + totalMissing + " restored=" + restored + " sample=" + string.Join(",", sample));
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_player_roster_restore_failed reason=" + (reason ?? "") + " error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
		}
	}

	private static bool TryEnsureGeneratedRewardItemCategory(ItemObject item, ItemObject templateItem = null, string logSource = null)
	{
		if (item == null)
		{
			return false;
		}
		ItemCategory category = templateItem?.ItemCategory;
		if (category != null && IsGeneratedRewardItemStringId(item.StringId) && !ReferenceEquals(item.ItemCategory, category))
		{
			if (TrySetGeneratedRewardItemCategory(item, category))
			{
				return true;
			}
		}
		if (item.ItemCategory != null)
		{
			return true;
		}
		if (category != null)
		{
			if (TrySetGeneratedRewardItemCategory(item, category))
			{
				return true;
			}
		}
		try
		{
			item.DetermineItemCategoryForItem();
		}
		catch
		{
		}
		if (item.ItemCategory != null)
		{
			return true;
		}
		try
		{
			category = DefaultItemCategories.Unassigned;
		}
		catch
		{
			category = null;
		}
		if (category != null && TrySetGeneratedRewardItemCategory(item, category))
		{
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_category_fallback source=" + (logSource ?? "") + " item=" + (item.StringId ?? "") + " category=" + (category.StringId ?? ""));
			}
			catch
			{
			}
			return true;
		}
		try
		{
			Logger.Log("Logic", "[RewardItemResolve] generated_category_failed source=" + (logSource ?? "") + " item=" + (item.StringId ?? "") + " template=" + (templateItem?.StringId ?? ""));
		}
		catch
		{
		}
		return false;
	}

	private static bool TrySetGeneratedRewardItemCategory(ItemObject item, ItemCategory category)
	{
		if (item == null || category == null)
		{
			return false;
		}
		try
		{
			RewardItemObjectCategoryProperty?.SetValue(item, category, null);
			return item.ItemCategory != null;
		}
		catch
		{
			return false;
		}
	}

	private static void RepairGeneratedRewardItemCategories(string reason)
	{
		try
		{
			IEnumerable<ItemObject> items = Game.Current?.ObjectManager?.GetObjectTypeList<ItemObject>() ?? MBObjectManager.Instance?.GetObjectTypeList<ItemObject>();
			int repaired = 0;
			List<string> ids = new List<string>();
			foreach (ItemObject item in items?.ToList() ?? new List<ItemObject>())
			{
				string stringId = (item?.StringId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(stringId) || !stringId.StartsWith("af_generated_reward_", StringComparison.OrdinalIgnoreCase) || item.ItemCategory != null)
				{
					continue;
				}
				if (TryEnsureGeneratedRewardItemCategory(item, null, reason))
				{
					repaired++;
					if (ids.Count < 12)
					{
						ids.Add(stringId + ":" + (item.ItemCategory?.StringId ?? "null"));
					}
				}
			}
			if (repaired > 0)
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_category_repaired reason=" + (reason ?? "") + " count=" + repaired + " items=" + string.Join(",", ids));
			}
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_category_repair_failed reason=" + (reason ?? "") + " error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
		}
	}

	private void EnsureGeneratedRewardItemData()
	{
		if (_generatedRewardItemRecords == null)
		{
			_generatedRewardItemRecords = new Dictionary<string, GeneratedRewardItemRecord>(StringComparer.OrdinalIgnoreCase);
		}
		if (_generatedRewardItemStorage == null)
		{
			_generatedRewardItemStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		if (_generatedRewardPlayerRosterRecords == null)
		{
			_generatedRewardPlayerRosterRecords = new Dictionary<string, GeneratedRewardRosterItemRecord>(StringComparer.OrdinalIgnoreCase);
		}
		if (_generatedRewardPlayerRosterStorage == null)
		{
			_generatedRewardPlayerRosterStorage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
	}

	private static void MBObjectManagerGetObjectPostfix(MBGUID objectId, ref MBObjectBase __result)
	{
		try
		{
			if (SuppressGeneratedRewardObjectLookup)
			{
				return;
			}
			if (objectId.InternalValue == 0u)
			{
				return;
			}
			if (TryResolveGeneratedRewardItemForObjectId(objectId.InternalValue, out var generatedItem, "object_lookup") && generatedItem != null)
			{
				__result = generatedItem;
				return;
			}
			if (__result == null && !SuppressGeneratedRewardPendingLookup && IsGeneratedRewardReservedObjectId(objectId))
			{
				ItemObject pendingItem = GetOrCreateGeneratedRewardPendingItem(objectId, "object_lookup_pending");
				if (pendingItem != null)
				{
					__result = pendingItem;
				}
			}
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("RewardSystem", "[GeneratedRewardItem] object lookup postfix failed: " + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
		}
	}

	private static void MBObjectManagerGetObjectByStringPostfix<T>(string objectName, ref T __result) where T : MBObjectBase
	{
		try
		{
			if (SuppressGeneratedRewardObjectLookup || __result != null || typeof(T) != typeof(ItemObject))
			{
				return;
			}
			if (TryResolveGeneratedRewardItemForStringId(objectName, out var generatedItem, "object_string_lookup") && generatedItem != null)
			{
				__result = generatedItem as T;
			}
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("RewardSystem", "[GeneratedRewardItem] string lookup postfix failed: " + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
		}
	}

	private static ItemObject TryGetRegisteredGeneratedRewardItemByStringId(string generatedStringId)
	{
		if (!IsGeneratedRewardItemStringId(generatedStringId))
		{
			return null;
		}
		string key = generatedStringId.Trim();
		bool previousSuppressObjectLookup = SuppressGeneratedRewardObjectLookup;
		bool previousSuppressPendingLookup = SuppressGeneratedRewardPendingLookup;
		try
		{
			SuppressGeneratedRewardObjectLookup = true;
			SuppressGeneratedRewardPendingLookup = true;
			ItemObject item = MBObjectManager.Instance?.GetObject<ItemObject>(key);
			if (item == null)
			{
				item = Game.Current?.ObjectManager?.GetObject<ItemObject>(key);
			}
			return item != null && string.Equals((item.StringId ?? "").Trim(), key, StringComparison.OrdinalIgnoreCase) ? item : null;
		}
		catch
		{
			return null;
		}
		finally
		{
			SuppressGeneratedRewardObjectLookup = previousSuppressObjectLookup;
			SuppressGeneratedRewardPendingLookup = previousSuppressPendingLookup;
		}
	}

	private static bool TryResolveGeneratedRewardItemForObjectId(uint objectIdValue, out ItemObject item, string source = null)
	{
		item = null;
		if (objectIdValue == 0u)
		{
			return false;
		}
		EnsureGeneratedRewardManifestLoaded();
		GeneratedRewardItemRecord record = null;
		ItemObject cachedItem = null;
		lock (GeneratedRewardItemRegistrationLock)
		{
			if (GeneratedRewardDetachedItemsByObjectId.TryGetValue(objectIdValue, out var cached) && cached != null)
			{
				cachedItem = cached;
			}
			GeneratedRewardManifestByObjectId.TryGetValue(objectIdValue, out record);
		}
		if (record == null)
		{
			try
			{
				Dictionary<uint, GeneratedRewardItemRecord> recordsByObjectId = Instance?.BuildGeneratedRewardItemRecordsByObjectId();
				recordsByObjectId?.TryGetValue(objectIdValue, out record);
			}
			catch
			{
			}
		}
		if (record == null)
		{
			if (cachedItem != null)
			{
				item = cachedItem;
				return true;
			}
			return false;
		}
		PlayerRpCraftItemStatsSnapshot cachedSnapshot =
			string.Equals(record.PlayerCraft?.CraftKind, "remnant", StringComparison.OrdinalIgnoreCase)
				? null
				: record.PlayerCraft?.StatsSnapshot;
		string cachedCraftKind = (record.PlayerCraft?.CraftKind ?? "").Trim();
		if (string.Equals(
			cachedCraftKind,
			PlayerRpCraftTerminalInvalidKind,
			StringComparison.OrdinalIgnoreCase))
		{
			if (cachedItem != null)
			{
				lock (GeneratedRewardItemRegistrationLock)
				{
					RemoveGeneratedRewardItemCacheReference(
						cachedItem,
						record.GeneratedStringId,
						objectIdValue,
						removePending: false);
				}
			}
			return false;
		}
		bool cachedSnapshotRequired = string.Equals(
			cachedCraftKind,
			"equipment",
			StringComparison.OrdinalIgnoreCase);
		bool cachedRemnantRequiresRepair = cachedItem != null
			&& string.Equals(
				cachedCraftKind,
				"remnant",
				StringComparison.OrdinalIgnoreCase)
			&& !IsGeneratedRewardRosterItemCanonical(cachedItem, record);
		if (cachedItem != null
			&& !cachedRemnantRequiresRepair
			&& HasExpectedPlayerRpCraftItemValue(cachedItem, record)
			&& (!cachedSnapshotRequired
				|| (cachedSnapshot != null
					&& PlayerRpCraftItemComponentService.MatchesSnapshot(cachedItem, cachedSnapshot))))
		{
			item = cachedItem;
			return true;
		}
		item = TryGetOrCreateGeneratedRewardDetachedItem(record, source);
		return item != null;
	}

	private static bool TryResolveGeneratedRewardItemForStringId(string generatedStringId, out ItemObject item, string source = null)
	{
		item = null;
		if (!IsGeneratedRewardItemStringId(generatedStringId))
		{
			return false;
		}
		string key = generatedStringId.Trim();
		EnsureGeneratedRewardManifestLoaded();
		ItemObject registeredItem = TryGetRegisteredGeneratedRewardItemByStringId(key);
		if (registeredItem != null)
		{
			GeneratedRewardItemRecord registeredRecord = Instance?.GetGeneratedRewardItemRecord(key);
			if (registeredRecord == null)
			{
				lock (GeneratedRewardItemRegistrationLock)
				{
					GeneratedRewardManifestByStringId.TryGetValue(key, out registeredRecord);
				}
			}
			string registeredCraftKind =
				(registeredRecord?.PlayerCraft?.CraftKind ?? "").Trim();
			if (string.Equals(
				registeredCraftKind,
				PlayerRpCraftTerminalInvalidKind,
				StringComparison.OrdinalIgnoreCase))
			{
				lock (GeneratedRewardItemRegistrationLock)
				{
					RemoveGeneratedRewardItemCacheReference(
						registeredItem,
						key,
						registeredItem.Id.InternalValue,
						removePending: false);
				}
				return false;
			}
			PlayerRpCraftItemStatsSnapshot registeredSnapshot =
				string.Equals(registeredCraftKind, "remnant", StringComparison.OrdinalIgnoreCase)
					? null
					: registeredRecord?.PlayerCraft?.StatsSnapshot;
			bool registeredSnapshotRequired = string.Equals(
				registeredCraftKind,
				"equipment",
				StringComparison.OrdinalIgnoreCase);
			bool registeredRemnantRequiresReplay =
				string.Equals(
					registeredCraftKind,
					"remnant",
					StringComparison.OrdinalIgnoreCase)
				&& !IsGeneratedRewardRosterItemCanonical(
					registeredItem,
					registeredRecord);
			bool registeredValueRequiresReplay =
				!HasExpectedPlayerRpCraftItemValue(
					registeredItem,
					registeredRecord);
			if ((registeredSnapshotRequired
					&& (registeredSnapshot == null
						|| !PlayerRpCraftItemComponentService.MatchesSnapshot(
							registeredItem,
							registeredSnapshot)))
				|| registeredRemnantRequiresReplay
				|| registeredValueRequiresReplay)
			{
				ItemObject registeredTemplate = ResolveGeneratedRewardRecordTemplateItem(
					registeredRecord,
					source ?? "registered_snapshot_replay");
				bool templateApplied = registeredTemplate != null
					&& ApplyGeneratedRewardItemTemplateState(
						registeredItem,
						registeredTemplate,
						registeredRecord.DisplayName);
				if (!templateApplied
					&& string.Equals(
						registeredRecord?.PlayerCraft?.CraftKind,
						"remnant",
						StringComparison.OrdinalIgnoreCase))
				{
					registeredTemplate = ResolveGeneratedRewardRecordTemplateItem(
						registeredRecord,
						(source ?? "registered_snapshot_replay") + "_remnant");
					templateApplied = registeredTemplate != null
						&& ApplyGeneratedRewardItemTemplateState(
							registeredItem,
							registeredTemplate,
							registeredRecord.DisplayName);
				}
				if (!templateApplied)
				{
					lock (GeneratedRewardItemRegistrationLock)
					{
						RemoveGeneratedRewardItemCacheReference(
							registeredItem,
							key,
							registeredItem.Id.InternalValue,
							removePending: false);
					}
					return false;
				}
				try
				{
					registeredItem.Initialize();
					registeredItem.IsReady = true;
				}
				catch
				{
					lock (GeneratedRewardItemRegistrationLock)
					{
						RemoveGeneratedRewardItemCacheReference(
							registeredItem,
							key,
							registeredItem.Id.InternalValue,
							removePending: false);
					}
					return false;
				}
				if (!IsGeneratedRewardRosterItemCanonical(
					registeredItem,
					registeredRecord))
				{
					lock (GeneratedRewardItemRegistrationLock)
					{
						RemoveGeneratedRewardItemCacheReference(
							registeredItem,
							key,
							registeredItem.Id.InternalValue,
							removePending: false);
					}
					return false;
				}
			}
			lock (GeneratedRewardItemRegistrationLock)
			{
				if (registeredItem.Id.InternalValue != 0u)
				{
					GeneratedRewardDetachedItemsByObjectId[registeredItem.Id.InternalValue] = registeredItem;
				}
				GeneratedRewardDetachedItemsByStringId[key] = registeredItem;
			}
			item = registeredItem;
			return true;
		}
		GeneratedRewardItemRecord record = null;
		ItemObject cachedItem = null;
		lock (GeneratedRewardItemRegistrationLock)
		{
			if (GeneratedRewardDetachedItemsByStringId.TryGetValue(key, out var cached) && cached != null)
			{
				cachedItem = cached;
			}
			GeneratedRewardManifestByStringId.TryGetValue(key, out record);
		}
		record ??= Instance?.GetGeneratedRewardItemRecord(key);
		if (record == null)
		{
			if (cachedItem != null)
			{
				item = cachedItem;
				return true;
			}
			return false;
		}
		item = TryGetOrCreateGeneratedRewardDetachedItem(record, source);
		if (item != null)
		{
			return true;
		}
		if (cachedItem != null)
		{
			PlayerRpCraftItemStatsSnapshot cachedSnapshot =
				string.Equals(record.PlayerCraft?.CraftKind, "remnant", StringComparison.OrdinalIgnoreCase)
					? null
					: record.PlayerCraft?.StatsSnapshot;
			string cachedCraftKind = (record.PlayerCraft?.CraftKind ?? "").Trim();
			bool cachedSnapshotRequired = string.Equals(
				cachedCraftKind,
				"equipment",
				StringComparison.OrdinalIgnoreCase);
			bool cachedTerminalInvalid = string.Equals(
				cachedCraftKind,
				PlayerRpCraftTerminalInvalidKind,
				StringComparison.OrdinalIgnoreCase);
			bool cachedRemnantCanonical =
				!string.Equals(
					cachedCraftKind,
					"remnant",
					StringComparison.OrdinalIgnoreCase)
				|| IsGeneratedRewardRosterItemCanonical(cachedItem, record);
			if (!cachedTerminalInvalid
				&& cachedRemnantCanonical
				&& HasExpectedPlayerRpCraftItemValue(cachedItem, record)
				&& (!cachedSnapshotRequired
					|| (cachedSnapshot != null
					&& PlayerRpCraftItemComponentService.MatchesSnapshot(cachedItem, cachedSnapshot))))
			{
				item = cachedItem;
				return true;
			}
		}
		return false;
	}

	private static ItemObject TryGetOrCreateGeneratedRewardDetachedItem(GeneratedRewardItemRecord record, string source = null)
	{
		record = NormalizeGeneratedRewardItemRecord(record?.GeneratedStringId, record);
		if (record == null)
		{
			return null;
		}
		ItemObject templateItem = ResolveGeneratedRewardRecordTemplateItem(record, source ?? "detached_template_guard");
		if (templateItem == null)
		{
			return null;
		}
		record.TemplateStringId = (templateItem.StringId ?? "").Trim();
		if (record.ObjectId != 0u
			&& TryRetargetGeneratedRewardPendingItem(
				new MBGUID(record.ObjectId),
				record.GeneratedStringId,
				record.DisplayName,
				templateItem,
				out ItemObject retargetedPending,
				source ?? "detached_pending_retarget")
			&& retargetedPending != null)
		{
			record.ObjectId = retargetedPending.Id.InternalValue;
			lock (GeneratedRewardItemRegistrationLock)
			{
				if (record.ObjectId != 0u)
				{
					GeneratedRewardDetachedItemsByObjectId[record.ObjectId] = retargetedPending;
				}
				GeneratedRewardDetachedItemsByStringId[record.GeneratedStringId] = retargetedPending;
				RegisterGeneratedRewardManifestRecordNoLock(record);
			}
			return retargetedPending;
		}
		ItemObject registeredExisting = TryGetRegisteredGeneratedRewardItemByStringId(record.GeneratedStringId);
		if (registeredExisting != null)
		{
			if (!ApplyGeneratedRewardItemTemplateState(registeredExisting, templateItem, record.DisplayName))
			{
				return null;
			}
			registeredExisting.Initialize();
			registeredExisting.IsReady = true;
			if (!TryEnsureGeneratedRewardItemCategory(registeredExisting, templateItem, source))
			{
				return null;
			}
			lock (GeneratedRewardItemRegistrationLock)
			{
				if (registeredExisting.Id.InternalValue != 0u)
				{
					record.ObjectId = registeredExisting.Id.InternalValue;
					GeneratedRewardDetachedItemsByObjectId[registeredExisting.Id.InternalValue] = registeredExisting;
				}
				GeneratedRewardDetachedItemsByStringId[record.GeneratedStringId] = registeredExisting;
				RegisterGeneratedRewardManifestRecordNoLock(record);
			}
			return registeredExisting;
		}
		ItemObject cachedItem = null;
		lock (GeneratedRewardItemRegistrationLock)
		{
			if (record.ObjectId != 0u && GeneratedRewardDetachedItemsByObjectId.TryGetValue(record.ObjectId, out var cachedByObjectId) && cachedByObjectId != null)
			{
				cachedItem = cachedByObjectId;
			}
			if (GeneratedRewardDetachedItemsByStringId.TryGetValue(record.GeneratedStringId, out var cachedByStringId) && cachedByStringId != null)
			{
				cachedItem = cachedByStringId;
			}
		}
		uint objectIdValue = record.ObjectId;
		MBGUID objectId = objectIdValue != 0u ? new MBGUID(objectIdValue) : default(MBGUID);
		if (objectIdValue == 0u)
		{
			if (!TryGetGeneratedRewardItemId(record.GeneratedStringId, templateItem, 0u, out objectId, source))
			{
				return null;
			}
			objectIdValue = objectId.InternalValue;
			record.ObjectId = objectIdValue;
		}
		if (cachedItem != null)
		{
			cachedItem.StringId = record.GeneratedStringId;
			cachedItem.Id = objectId;
			if (!ApplyGeneratedRewardItemTemplateState(cachedItem, templateItem, record.DisplayName))
			{
				return null;
			}
			cachedItem.Initialize();
			cachedItem.IsReady = true;
			if (TrySetRewardItemObjectName(cachedItem, record.DisplayName) && TryEnsureGeneratedRewardItemCategory(cachedItem, templateItem, source))
			{
				ItemObject registeredCachedItem = TryRegisterGeneratedRewardItemWithStableId(cachedItem, source ?? "cached_detached_runtime");
				if (registeredCachedItem != null)
				{
					objectIdValue = registeredCachedItem.Id.InternalValue != 0u ? registeredCachedItem.Id.InternalValue : objectIdValue;
					record.ObjectId = objectIdValue;
					lock (GeneratedRewardItemRegistrationLock)
					{
						GeneratedRewardDetachedItemsByObjectId[objectIdValue] = registeredCachedItem;
						GeneratedRewardDetachedItemsByStringId[record.GeneratedStringId] = registeredCachedItem;
						RegisterGeneratedRewardManifestRecordNoLock(record);
					}
					return registeredCachedItem;
				}
			}
		}
		ItemObject generatedItem = new ItemObject(templateItem)
		{
			StringId = record.GeneratedStringId,
			Id = objectId
		};
		if (!ApplyGeneratedRewardItemTemplateState(generatedItem, templateItem, record.DisplayName))
		{
			return null;
		}
		generatedItem.Initialize();
		generatedItem.IsReady = true;
		if (!TrySetRewardItemObjectName(generatedItem, record.DisplayName))
		{
			return null;
		}
		if (!TryEnsureGeneratedRewardItemCategory(generatedItem, templateItem, source))
		{
			return null;
		}
		ItemObject registeredItem = TryRegisterGeneratedRewardItemWithStableId(generatedItem, source ?? "detached_runtime");
		if (registeredItem != null)
		{
			generatedItem = registeredItem;
			if (!ApplyGeneratedRewardItemTemplateState(generatedItem, templateItem, record.DisplayName))
			{
				return null;
			}
			generatedItem.Initialize();
			generatedItem.IsReady = true;
		}
		objectIdValue = generatedItem.Id.InternalValue != 0u ? generatedItem.Id.InternalValue : objectIdValue;
		record.ObjectId = objectIdValue;
		lock (GeneratedRewardItemRegistrationLock)
		{
			GeneratedRewardDetachedItemsByObjectId[objectIdValue] = generatedItem;
			GeneratedRewardDetachedItemsByStringId[record.GeneratedStringId] = generatedItem;
			RegisterGeneratedRewardManifestRecordNoLock(record);
		}
		return generatedItem;
	}

	private static string GetGeneratedRewardItemManifestPath()
	{
		try
		{
			return Path.Combine(AnimusForgeModulePaths.GetLogsDirectory(), GeneratedRewardItemManifestFileName);
		}
		catch
		{
			return GeneratedRewardItemManifestFileName;
		}
	}

	private static void EnsureGeneratedRewardManifestLoaded()
	{
		lock (GeneratedRewardItemRegistrationLock)
		{
			if (GeneratedRewardManifestLoaded)
			{
				return;
			}
			GeneratedRewardManifestLoaded = true;
		}
	}

	private static void ClearGeneratedRewardRuntimeState(
		string reason,
		bool preservePendingItems = false)
	{
		try
		{
			lock (GeneratedRewardItemRegistrationLock)
			{
				if (!preservePendingItems)
				{
					GeneratedRewardPendingItemsByObjectId.Clear();
				}
				GeneratedRewardDetachedItemsByObjectId.Clear();
				GeneratedRewardDetachedItemsByStringId.Clear();
				GeneratedRewardManifestByObjectId.Clear();
				GeneratedRewardManifestByStringId.Clear();
				GeneratedRewardManifestLoaded = true;
			}
			ClearGeneratedRpEquipmentTemplateCache();
			ClearGeneratedRpFoodTemplateCache();
			ClearPlayerRpCraftTemplateCaches();
			PlayerRpForgePopup.ClearDraft();
			ClearGeneratedRewardEconomicPoolCache();
			ClearRpItemIntroductionRuntimeState(reason);
			GeneratedRewardLastInventoryVmLogSignature = "";
			GeneratedRewardLastInventoryVmLogUtc = DateTime.MinValue;
			Logger.Log(
				"Logic",
				"[RewardItemResolve] generated_runtime_state_cleared reason="
					+ (reason ?? "")
					+ " pending="
					+ (preservePendingItems ? "preserved" : "cleared")
					+ " global_manifest_io=disabled");
		}
		catch
		{
		}
	}

	private static void ClearPlayerRpCraftTemplateCaches()
	{
		lock (PlayerRpMiscTemplateCacheLock)
		{
			PlayerRpMiscTemplateCacheOwner = null;
			PlayerRpMiscTemplateCandidates = new List<GeneratedRpFoodTemplateCandidate>();
		}
		lock (PlayerRpPriceCacheLock)
		{
			PlayerRpPriceCacheOwner = null;
			PlayerRpMedianPriceByItemType = new Dictionary<int, int>();
		}
		ClearPlayerRpExactTemplateLookupCache();
	}

	private static bool IsGeneratedRpWhipTemplateItem(ItemObject item)
	{
		// Bannerlord has no native whip WeaponClass. Some weapon mods therefore
		// implement whips as OneHandedSword. Preserve those templates for explicit
		// whip names without allowing the borrowed class to pollute the sword pool.
		return item != null
			&& (ContainsGeneratedRpWhipIdentity(item.StringId)
				|| ContainsGeneratedRpWhipIdentity(item.Name?.ToString()));
	}

	private static bool IsGeneratedRpWhipWeaponTemplateItem(ItemObject item)
	{
		return IsSettlementWeaponLikeItem(item)
			&& IsGeneratedRpWhipTemplateItem(item);
	}

	private static bool ContainsGeneratedRpWhipIdentity(string value)
	{
		string text = (value ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (text.IndexOf('鞭') >= 0)
		{
			return true;
		}
		// Mod IDs commonly append tier tokens (for example chainwhip_tier3), so
		// terminal suffix matching alone is insufficient here. This helper is
		// only accepted after the item has already been proven weapon-like.
		int searchStart = 0;
		while (searchStart < text.Length)
		{
			int index = text.IndexOf(
				"whip",
				searchStart,
				StringComparison.OrdinalIgnoreCase);
			if (index < 0)
			{
				return false;
			}
			int boundary = index + 4;
			if (boundary < text.Length
				&& (text[boundary] == 's' || text[boundary] == 'S'))
			{
				boundary++;
			}
			if (boundary >= text.Length
				|| !((text[boundary] >= 'a' && text[boundary] <= 'z')
					|| (text[boundary] >= 'A' && text[boundary] <= 'Z')))
			{
				return true;
			}
			searchStart = index + 4;
		}
		return false;
	}

	private static void RegisterGeneratedRewardManifestRecord(GeneratedRewardItemRecord record)
	{
		record = NormalizeGeneratedRewardItemRecord(record?.GeneratedStringId, record);
		if (record == null)
		{
			return;
		}
		EnsureGeneratedRewardManifestLoaded();
		lock (GeneratedRewardItemRegistrationLock)
		{
			RegisterGeneratedRewardManifestRecordNoLock(record);
		}
	}

	private static void RegisterGeneratedRewardManifestRecordNoLock(GeneratedRewardItemRecord record)
	{
		record = NormalizeGeneratedRewardItemRecord(record?.GeneratedStringId, record);
		if (record == null)
		{
			return;
		}
		if (GeneratedRewardManifestByStringId.TryGetValue(record.GeneratedStringId, out var existing) && existing != null)
		{
			uint objectId = record.ObjectId != 0u ? record.ObjectId : existing.ObjectId;
			HashSet<uint> legacyIds = new HashSet<uint>();
			foreach (uint legacyObjectId in existing.LegacyObjectIds ?? new List<uint>())
			{
				if (legacyObjectId != 0u && legacyObjectId != objectId)
				{
					legacyIds.Add(legacyObjectId);
				}
			}
			foreach (uint legacyObjectId2 in record.LegacyObjectIds ?? new List<uint>())
			{
				if (legacyObjectId2 != 0u && legacyObjectId2 != objectId)
				{
					legacyIds.Add(legacyObjectId2);
				}
			}
			if (existing.ObjectId != 0u && existing.ObjectId != objectId)
			{
				legacyIds.Add(existing.ObjectId);
			}
			if (record.ObjectId != 0u && record.ObjectId != objectId)
			{
				legacyIds.Add(record.ObjectId);
			}
			record.ObjectId = objectId;
			record.LegacyObjectIds = legacyIds.Take(16).ToList();
			if (string.IsNullOrWhiteSpace(record.DisplayName))
			{
				record.DisplayName = existing.DisplayName;
			}
			if (string.IsNullOrWhiteSpace(record.TemplateStringId))
			{
				record.TemplateStringId = existing.TemplateStringId;
			}
			MergeRpItemIntroductionFromFallback(record, existing);
			record.PlayerCraft = MergePlayerRpCraftData(record.PlayerCraft, existing.PlayerCraft);
			record.LastTouchedDay = Math.Max(record.LastTouchedDay, existing.LastTouchedDay);
			record = NormalizeGeneratedRewardItemRecord(record.GeneratedStringId, record);
			if (record == null)
			{
				return;
			}
		}
		GeneratedRewardManifestByStringId[record.GeneratedStringId] = record;
		if (record.ObjectId != 0u)
		{
			GeneratedRewardManifestByObjectId[record.ObjectId] = record;
		}
		foreach (uint legacyObjectId3 in record.LegacyObjectIds ?? new List<uint>())
		{
			if (legacyObjectId3 != 0u)
			{
				GeneratedRewardManifestByObjectId[legacyObjectId3] = record;
			}
		}
	}

	private static void SaveGeneratedRewardManifest(string reason = null)
	{
		EnsureGeneratedRewardManifestLoaded();
	}

	private void SyncGeneratedRewardRecordsToManifest(string reason)
	{
		EnsureGeneratedRewardItemData();
		if (_generatedRewardItemRecords == null || _generatedRewardItemRecords.Count == 0)
		{
			return;
		}
		EnsureGeneratedRewardManifestLoaded();
		foreach (KeyValuePair<string, GeneratedRewardItemRecord> pair in _generatedRewardItemRecords.ToList())
		{
			GeneratedRewardItemRecord record = NormalizeGeneratedRewardItemRecord(pair.Key, pair.Value);
			if (record == null)
			{
				continue;
			}
			_generatedRewardItemRecords[record.GeneratedStringId] = record;
			RegisterGeneratedRewardManifestRecord(record);
		}
	}

	private void MergeGeneratedRewardManifestIntoRecords()
	{
		EnsureGeneratedRewardItemData();
		EnsureGeneratedRewardManifestLoaded();
	}

	private static bool IsGeneratedRewardReservedObjectId(MBGUID objectId)
	{
		if (objectId.InternalValue == 0u || (objectId.SubId & GeneratedRewardReservedSubIdMask) != GeneratedRewardReservedSubIdBits)
		{
			return false;
		}
		ItemObject template = GetGeneratedRewardFallbackTemplateItem();
		return template != null && template.Id.InternalValue != 0u && objectId.GetTypeIndex() == template.Id.GetTypeIndex();
	}

	private static bool IsGeneratedRewardPendingItem(ItemObject item)
	{
		return item != null && !string.IsNullOrWhiteSpace(item.StringId) && item.StringId.Trim().StartsWith("af_generated_reward_pending_", StringComparison.OrdinalIgnoreCase);
	}

	private static ItemObject GetOrCreateGeneratedRewardPendingItem(MBGUID objectId, string source)
	{
		lock (GeneratedRewardItemRegistrationLock)
		{
			if (GeneratedRewardPendingItemsByObjectId.TryGetValue(objectId.InternalValue, out var cached) && cached != null)
			{
				return cached;
			}
			ItemObject template = GetGeneratedRewardFallbackTemplateItem();
			if (template == null)
			{
				return null;
			}
			string pendingStringId = "af_generated_reward_pending_" + objectId.InternalValue.ToString("X8", CultureInfo.InvariantCulture);
			ItemObject pending = new ItemObject(template)
			{
				StringId = pendingStringId,
				Id = objectId
			};
			if (!ApplyGeneratedRewardItemTemplateState(
				pending,
				template,
				"AnimusForge generated item"))
			{
				return null;
			}
			try
			{
				pending.Initialize();
				pending.IsReady = true;
			}
			catch (Exception ex)
			{
				LogGeneratedRewardPendingRetargetFailure(
					pending,
					pendingStringId,
					source,
					"pending_initialize_failed:" + ex.GetType().Name + ":" + ex.Message);
				return null;
			}
			if (TryEnsureGeneratedRewardItemCategory(pending, template, source ?? "pending"))
			{
				GeneratedRewardPendingItemsByObjectId[objectId.InternalValue] = pending;
				try
				{
					Logger.Log("Logic", "[RewardItemResolve] generated_pending_created source=" + (source ?? "") + " id=" + objectId.InternalValue.ToString(CultureInfo.InvariantCulture) + " stringId=" + pendingStringId);
				}
				catch
				{
				}
				return pending;
			}
			return null;
		}
	}

	private static ItemObject GetGeneratedRewardFallbackTemplateItem()
	{
		try
		{
			IEnumerable<ItemObject> items = Game.Current?.ObjectManager?.GetObjectTypeList<ItemObject>() ?? MBObjectManager.Instance?.GetObjectTypeList<ItemObject>();
			ItemObject book = null;
			foreach (ItemObject item in items ?? Enumerable.Empty<ItemObject>())
			{
				if (!IsGeneratedRewardMiscItemType(item) || !IsCloneSafeGeneratedRewardTemplateItem(item))
				{
					continue;
				}
				if (item.Type == ItemObject.ItemTypeEnum.Goods && item.ItemCategory != null)
				{
					return item;
				}
				if (book == null && item.Type == ItemObject.ItemTypeEnum.Book)
				{
					book = item;
				}
			}
			return book;
		}
		catch
		{
			return null;
		}
	}

	private static bool TryRetargetGeneratedRewardPendingItem(MBGUID objectId, string generatedStringId, string displayName, ItemObject templateItem, out ItemObject item, string logSource = null)
	{
		item = null;
		if (templateItem == null || string.IsNullOrWhiteSpace(generatedStringId) || objectId.InternalValue == 0u)
		{
			return false;
		}
		lock (GeneratedRewardItemRegistrationLock)
		{
			MBObjectManager manager = MBObjectManager.Instance;
			ItemObject fallbackTemplate = GetGeneratedRewardFallbackTemplateItem();
			if (manager == null || fallbackTemplate == null)
			{
				return false;
			}
			string generatedKey = generatedStringId.Trim();
			ItemObject registeredAtId = GetGeneratedRewardRawItem(objectId);
			GeneratedRewardPendingItemsByObjectId.TryGetValue(
				objectId.InternalValue,
				out ItemObject cachedPending);
			ItemObject existing = IsGeneratedRewardPendingItem(registeredAtId)
				? registeredAtId
				: (registeredAtId == null && IsGeneratedRewardPendingItem(cachedPending)
					? cachedPending
					: null);
			if (!IsGeneratedRewardPendingItem(existing))
			{
				return false;
			}
			string pendingStringId = existing.StringId.Trim();
			ItemObject registeredAtPendingString = GetGeneratedRewardRawItem(pendingStringId);
			ItemObject registeredAtGeneratedString = GetGeneratedRewardRawItem(generatedKey);
			bool registeredById = ReferenceEquals(registeredAtId, existing);
			bool registeredByPendingString = ReferenceEquals(
				registeredAtPendingString,
				existing);
			if ((registeredAtId != null && !registeredById)
				|| (registeredAtPendingString != null && !registeredByPendingString)
				|| registeredAtGeneratedString != null
				|| (registeredById != registeredByPendingString))
			{
				LogGeneratedRewardPendingRetargetFailure(
					existing,
					generatedKey,
					logSource,
					"preflight_index_collision");
				return false;
			}
			bool restorePendingRegistration = registeredById && registeredByPendingString;
			if (restorePendingRegistration
				&& !TryUnregisterGeneratedRewardItemReference(
					existing,
					pendingStringId,
					objectId,
					out string unregisterFailure))
			{
				LogGeneratedRewardPendingRetargetFailure(
					existing,
					generatedKey,
					logSource,
					"pending_unregister_failed:" + unregisterFailure);
				return false;
			}
			if (GetGeneratedRewardRawItem(objectId) != null
				|| GetGeneratedRewardRawItem(pendingStringId) != null
				|| GetGeneratedRewardRawItem(generatedKey) != null)
			{
				TryRestoreGeneratedRewardPendingAfterFailedRetarget(
					existing,
					pendingStringId,
					objectId,
					fallbackTemplate,
					restorePendingRegistration,
					generatedKey,
					logSource,
					out _);
				LogGeneratedRewardPendingRetargetFailure(
					existing,
					generatedKey,
					logSource,
					"post_unregister_index_collision");
				return false;
			}

			existing.StringId = generatedKey;
			existing.Id = objectId;
			if (!ApplyGeneratedRewardItemTemplateState(existing, templateItem, displayName))
			{
				TryRestoreGeneratedRewardPendingAfterFailedRetarget(
					existing,
					pendingStringId,
					objectId,
					fallbackTemplate,
					restorePendingRegistration,
					generatedKey,
					logSource,
					out string restoreFailure);
				LogGeneratedRewardPendingRetargetFailure(
					existing,
					generatedKey,
					logSource,
					"template_apply_failed:" + restoreFailure);
				return false;
			}
			ItemObject registered = TryRegisterGeneratedRewardItemWithStableId(existing, logSource ?? "pending_retarget");
			bool registrationIsExactReference = ReferenceEquals(registered, existing)
				&& ReferenceEquals(GetGeneratedRewardRawItem(objectId), existing)
				&& ReferenceEquals(GetGeneratedRewardRawItem(generatedKey), existing)
				&& GetGeneratedRewardRawItem(pendingStringId) == null;
			if (!registrationIsExactReference
				|| !IsGeneratedRewardItemVisibleToObjectManager(existing))
			{
				TryRestoreGeneratedRewardPendingAfterFailedRetarget(
					existing,
					pendingStringId,
					objectId,
					fallbackTemplate,
					restorePendingRegistration,
					generatedKey,
					logSource,
					out string restoreFailure);
				LogGeneratedRewardPendingRetargetFailure(
					existing,
					generatedKey,
					logSource,
					"stable_registration_reference_mismatch:" + restoreFailure);
				return false;
			}
			string finalizationFailure = "";
			try
			{
				existing.IsReady = true;
				if (!IsGeneratedRewardPendingRetargetFinalStateValid(
					existing,
					objectId,
					generatedKey,
					displayName,
					templateItem))
				{
					finalizationFailure = "post_registration_state_validation_failed";
				}
			}
			catch (Exception ex)
			{
				finalizationFailure = "post_registration_finalize_failed:"
					+ ex.GetType().Name
					+ ":"
					+ ex.Message;
			}
			if (!string.IsNullOrWhiteSpace(finalizationFailure))
			{
				TryRestoreGeneratedRewardPendingAfterFailedRetarget(
					existing,
					pendingStringId,
					objectId,
					fallbackTemplate,
					restorePendingRegistration,
					generatedKey,
					logSource,
					out string restoreFailure);
				LogGeneratedRewardPendingRetargetFailure(
					existing,
					generatedKey,
					logSource,
					finalizationFailure + ":" + restoreFailure);
				return false;
			}
			if (GeneratedRewardPendingItemsByObjectId.TryGetValue(
					objectId.InternalValue,
					out ItemObject pendingAtId)
				&& ReferenceEquals(pendingAtId, existing))
			{
				GeneratedRewardPendingItemsByObjectId.Remove(objectId.InternalValue);
			}
			GeneratedRewardDetachedItemsByObjectId[objectId.InternalValue] = existing;
			GeneratedRewardDetachedItemsByStringId[generatedKey] = existing;
			item = existing;
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_pending_retargeted source="
					+ (logSource ?? "")
					+ " generated="
					+ generatedKey
					+ " id="
					+ objectId.InternalValue.ToString(CultureInfo.InvariantCulture));
			}
			catch
			{
			}
			return true;
		}
	}

	private static bool IsGeneratedRewardPendingRetargetFinalStateValid(
		ItemObject item,
		MBGUID objectId,
		string generatedStringId,
		string displayName,
		ItemObject templateItem)
	{
		if (item == null
			|| templateItem == null
			|| !item.IsReady
			|| item.Id.InternalValue != objectId.InternalValue
			|| !string.Equals(
				(item.StringId ?? "").Trim(),
				(generatedStringId ?? "").Trim(),
				StringComparison.OrdinalIgnoreCase)
			|| !string.Equals(
				item.Name?.ToString() ?? "",
				(displayName ?? "").Trim(),
				StringComparison.Ordinal)
			|| item.Type != templateItem.Type
			|| !HasCloneSafeGeneratedRewardThumbnailSource(item))
		{
			return false;
		}
		if (templateItem.ItemCategory != null
			&& !ReferenceEquals(item.ItemCategory, templateItem.ItemCategory))
		{
			return false;
		}
		GeneratedRewardItemRecord record = Instance?.GetGeneratedRewardItemRecord(
			generatedStringId);
		if (record == null)
		{
			lock (GeneratedRewardItemRegistrationLock)
			{
				GeneratedRewardManifestByStringId.TryGetValue(
					(generatedStringId ?? "").Trim(),
					out record);
			}
		}
		PlayerRpCraftItemStatsSnapshot snapshot =
			string.Equals(
				record?.PlayerCraft?.CraftKind,
				"equipment",
				StringComparison.OrdinalIgnoreCase)
				? record.PlayerCraft.StatsSnapshot
				: null;
		if (!HasExpectedPlayerRpCraftItemValue(item, record))
		{
			return false;
		}
		if (snapshot != null)
		{
			if (!PlayerRpCraftItemComponentService.MatchesSnapshot(item, snapshot))
			{
				return false;
			}
		}
		else if (!ReferenceEquals(item.ItemComponent, templateItem.ItemComponent))
		{
			return false;
		}
		return ReferenceEquals(GetGeneratedRewardRawItem(objectId), item)
			&& ReferenceEquals(GetGeneratedRewardRawItem(generatedStringId), item);
	}

	private static ItemObject GetGeneratedRewardRawItem(MBGUID objectId)
	{
		bool previousSuppressObjectLookup = SuppressGeneratedRewardObjectLookup;
		bool previousSuppressPendingLookup = SuppressGeneratedRewardPendingLookup;
		try
		{
			SuppressGeneratedRewardObjectLookup = true;
			SuppressGeneratedRewardPendingLookup = true;
			return MBObjectManager.Instance?.GetObject(objectId) as ItemObject;
		}
		catch
		{
			return null;
		}
		finally
		{
			SuppressGeneratedRewardObjectLookup = previousSuppressObjectLookup;
			SuppressGeneratedRewardPendingLookup = previousSuppressPendingLookup;
		}
	}

	private static ItemObject GetGeneratedRewardRawItem(string stringId)
	{
		if (string.IsNullOrWhiteSpace(stringId))
		{
			return null;
		}
		bool previousSuppressObjectLookup = SuppressGeneratedRewardObjectLookup;
		bool previousSuppressPendingLookup = SuppressGeneratedRewardPendingLookup;
		try
		{
			SuppressGeneratedRewardObjectLookup = true;
			SuppressGeneratedRewardPendingLookup = true;
			return MBObjectManager.Instance?.GetObject<ItemObject>(stringId.Trim());
		}
		catch
		{
			return null;
		}
		finally
		{
			SuppressGeneratedRewardObjectLookup = previousSuppressObjectLookup;
			SuppressGeneratedRewardPendingLookup = previousSuppressPendingLookup;
		}
	}

	private static bool TryUnregisterGeneratedRewardItemReference(
		ItemObject existing,
		string stringId,
		MBGUID objectId,
		out string failure)
	{
		failure = "";
		if (existing == null || MBObjectManager.Instance == null)
		{
			failure = "missing_object_manager_or_item";
			return false;
		}
		ItemObject registeredAtId = GetGeneratedRewardRawItem(objectId);
		ItemObject registeredAtString = GetGeneratedRewardRawItem(stringId);
		if ((registeredAtId != null && !ReferenceEquals(registeredAtId, existing))
			|| (registeredAtString != null && !ReferenceEquals(registeredAtString, existing)))
		{
			failure = "foreign_index_owner";
			return false;
		}
		if (registeredAtId == null && registeredAtString == null)
		{
			return true;
		}
		try
		{
			MBObjectManager.Instance.UnregisterObject(existing);
		}
		catch (Exception ex)
		{
			failure = ex.GetType().Name + ":" + ex.Message;
			return false;
		}
		if (ReferenceEquals(GetGeneratedRewardRawItem(objectId), existing)
			|| ReferenceEquals(GetGeneratedRewardRawItem(stringId), existing))
		{
			failure = "owned_index_remained_after_unregister";
			return false;
		}
		return true;
	}

	private static bool TryRestoreGeneratedRewardPendingAfterFailedRetarget(
		ItemObject existing,
		string pendingStringId,
		MBGUID objectId,
		ItemObject fallbackTemplate,
		bool restoreRegistration,
		string generatedStringId,
		string source,
		out string failure)
	{
		failure = "";
		if (existing == null || fallbackTemplate == null)
		{
			failure = "missing_pending_or_fallback";
			return false;
		}
		string currentStringId = existing.StringId ?? generatedStringId ?? "";
		if (!TryUnregisterGeneratedRewardItemReference(
			existing,
			currentStringId,
			objectId,
			out string unregisterFailure))
		{
			bool existingStillIndexed =
				ReferenceEquals(GetGeneratedRewardRawItem(objectId), existing)
				|| ReferenceEquals(
					GetGeneratedRewardRawItem(currentStringId),
					existing);
			if (existingStillIndexed)
			{
				failure = "generated_unregister_failed:" + unregisterFailure;
				return false;
			}
		}
		existing.StringId = pendingStringId;
		existing.Id = objectId;
		if (!ApplyGeneratedRewardItemTemplateState(
			existing,
			fallbackTemplate,
			"AnimusForge generated item"))
		{
			failure = "fallback_state_restore_failed";
			RemoveGeneratedRewardItemCacheReference(
				existing,
				generatedStringId,
				objectId.InternalValue,
				removePending: true);
			return false;
		}
		try
		{
			existing.Initialize();
			existing.IsReady = true;
		}
		catch (Exception ex)
		{
			failure = "fallback_initialize_failed:"
				+ ex.GetType().Name
				+ ":"
				+ ex.Message;
			RemoveGeneratedRewardItemCacheReference(
				existing,
				generatedStringId,
				objectId.InternalValue,
				removePending: true);
			return false;
		}
		bool registrationRestored = true;
		if (restoreRegistration)
		{
			ItemObject restoredRegistration = TryRegisterGeneratedRewardItemWithStableId(
				existing,
				(source ?? "pending_retarget") + "_restore_pending");
			registrationRestored = ReferenceEquals(restoredRegistration, existing)
				&& ReferenceEquals(GetGeneratedRewardRawItem(objectId), existing)
				&& ReferenceEquals(GetGeneratedRewardRawItem(pendingStringId), existing);
			if (!registrationRestored)
			{
				TryUnregisterGeneratedRewardItemReference(
					existing,
					pendingStringId,
					objectId,
					out _);
			}
		}
		else
		{
			ItemObject idOwner = GetGeneratedRewardRawItem(objectId);
			ItemObject stringOwner = GetGeneratedRewardRawItem(pendingStringId);
			registrationRestored =
				(idOwner == null && stringOwner == null)
				|| (ReferenceEquals(idOwner, existing)
					&& ReferenceEquals(stringOwner, existing));
			if (!registrationRestored
				&& (ReferenceEquals(idOwner, existing)
					|| ReferenceEquals(stringOwner, existing)))
			{
				TryUnregisterGeneratedRewardItemReference(
					existing,
					pendingStringId,
					objectId,
					out _);
			}
		}
		if (!registrationRestored)
		{
			RemoveGeneratedRewardItemCacheReference(
				existing,
				generatedStringId,
				objectId.InternalValue,
				removePending: true);
			failure = "pending_registration_restore_failed";
			return false;
		}
		RemoveGeneratedRewardItemCacheReference(
			existing,
			generatedStringId,
			objectId.InternalValue,
			removePending: false);
		GeneratedRewardPendingItemsByObjectId[objectId.InternalValue] = existing;
		return true;
	}

	private static void RemoveGeneratedRewardItemCacheReference(
		ItemObject item,
		string generatedStringId,
		uint objectId,
		bool removePending)
	{
		if (GeneratedRewardDetachedItemsByObjectId.TryGetValue(
				objectId,
				out ItemObject detachedById)
			&& ReferenceEquals(detachedById, item))
		{
			GeneratedRewardDetachedItemsByObjectId.Remove(objectId);
		}
		string key = (generatedStringId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(key)
			&& GeneratedRewardDetachedItemsByStringId.TryGetValue(
				key,
				out ItemObject detachedByString)
			&& ReferenceEquals(detachedByString, item))
		{
			GeneratedRewardDetachedItemsByStringId.Remove(key);
		}
		if (removePending
			&& GeneratedRewardPendingItemsByObjectId.TryGetValue(
				objectId,
				out ItemObject pendingById)
			&& ReferenceEquals(pendingById, item))
		{
			GeneratedRewardPendingItemsByObjectId.Remove(objectId);
		}
	}

	private static void LogGeneratedRewardPendingRetargetFailure(
		ItemObject pending,
		string generatedStringId,
		string source,
		string failure)
	{
		try
		{
			Logger.Log("Logic", "[RewardItemResolve] generated_pending_retarget_failed source="
				+ (source ?? "")
				+ " generated="
				+ (generatedStringId ?? "")
				+ " id="
				+ (pending?.Id.InternalValue ?? 0u).ToString(CultureInfo.InvariantCulture)
				+ " failure="
				+ (failure ?? ""));
		}
		catch
		{
		}
	}

	private static int GetStoredPlayerRpCraftItemValue(
		PlayerRpCraftData craftData)
	{
		if (craftData == null)
		{
			return 0;
		}
		if (craftData.CraftedItemValue > 0)
		{
			return craftData.CraftedItemValue;
		}
		return craftData.SchemaVersion < 3
			? Math.Max(0, craftData.InvestedDenars)
			: 0;
	}

	private static bool TryGetPlayerRpCraftStoredItemValue(
		ItemObject item,
		out int expectedValue)
	{
		expectedValue = 0;
		string key = (item?.StringId ?? "").Trim();
		if (!IsGeneratedRewardItemStringId(key))
		{
			return false;
		}
		GeneratedRewardItemRecord record =
			Instance?.GetGeneratedRewardItemRecord(key);
		if (record == null)
		{
			lock (GeneratedRewardItemRegistrationLock)
			{
				GeneratedRewardManifestByStringId.TryGetValue(key, out record);
			}
		}
		expectedValue =
			GetStoredPlayerRpCraftItemValue(record?.PlayerCraft);
		return expectedValue > 0;
	}

	private static bool HasExpectedPlayerRpCraftItemValue(
		ItemObject item,
		GeneratedRewardItemRecord record)
	{
		PlayerRpCraftData craftData = record?.PlayerCraft;
		if (craftData == null)
		{
			return true;
		}
		int expectedValue =
			GetStoredPlayerRpCraftItemValue(craftData);
		if (expectedValue <= 0)
		{
			return craftData.SchemaVersion < 3
				&& craftData.InvestedDenars <= 0
				&& item != null;
		}
		return expectedValue > 0
			&& item != null
			&& item.Value == expectedValue;
	}

	private static bool ApplyGeneratedRewardItemTemplateState(ItemObject target, ItemObject templateItem, string displayName)
	{
		if (target == null || templateItem == null)
		{
			return false;
		}
		ItemObject templateCopy;
		try
		{
			templateCopy = new ItemObject(templateItem);
		}
		catch (Exception ex)
		{
			LogGeneratedRewardTemplateStateFailure(
				target,
				templateItem,
				"template_copy_failed:" + ex.GetType().Name + ":" + ex.Message,
				rollbackSucceeded: true);
			return false;
		}

		int propertyCount = GeneratedRewardItemTemplateStateProperties.Length;
		object[] previousValues = new object[propertyCount];
		object[] desiredValues = new object[propertyCount];
		object previousName = null;
		ItemObject.ItemTypeEnum previousType = target.Type;
		bool stateCaptured = false;
		string failure = "";
		try
		{
			bool hasPlayerCraftValueOverride =
				TryGetPlayerRpCraftStoredItemValue(
					target,
					out int playerCraftValue);
			if (RewardItemObjectNameProperty?.GetGetMethod(nonPublic: true) == null
				|| RewardItemObjectNameProperty.GetSetMethod(nonPublic: true) == null)
			{
				throw new InvalidOperationException("name_property_unavailable");
			}
			previousName = RewardItemObjectNameProperty.GetValue(target, null);
			for (int i = 0; i < propertyCount; i++)
			{
				PropertyInfo property = GeneratedRewardItemTemplateStateProperties[i];
				if (property?.GetGetMethod(nonPublic: true) == null)
				{
					throw new InvalidOperationException(
						"property_unavailable:" + GeneratedRewardItemTemplateStatePropertyNames[i]);
				}
				ItemObject source = string.Equals(
					GeneratedRewardItemTemplateStatePropertyNames[i],
					"WeaponDesign",
					StringComparison.Ordinal)
					? templateItem
					: templateCopy;
				previousValues[i] = property.GetValue(target, null);
				desiredValues[i] = hasPlayerCraftValueOverride
					&& string.Equals(
						GeneratedRewardItemTemplateStatePropertyNames[i],
						"Value",
						StringComparison.Ordinal)
					? playerCraftValue
					: property.GetValue(source, null);
				if (!AreGeneratedRewardTemplatePropertyValuesEquivalent(
						previousValues[i],
						desiredValues[i])
					&& property.GetSetMethod(nonPublic: true) == null)
				{
					throw new InvalidOperationException(
						"property_setter_unavailable:" + GeneratedRewardItemTemplateStatePropertyNames[i]);
				}
			}
			stateCaptured = true;

			for (int i = 0; i < propertyCount; i++)
			{
				PropertyInfo property = GeneratedRewardItemTemplateStateProperties[i];
				if (AreGeneratedRewardTemplatePropertyValuesEquivalent(
					previousValues[i],
					desiredValues[i]))
				{
					continue;
				}
				property.SetValue(target, desiredValues[i], null);
				if (!AreGeneratedRewardTemplatePropertyValuesEquivalent(
					property.GetValue(target, null),
					desiredValues[i]))
				{
					throw new InvalidOperationException(
						"property_commit_validation_failed:" + GeneratedRewardItemTemplateStatePropertyNames[i]);
				}
			}
			target.Type = templateItem.Type;
			if (target.Type != templateItem.Type)
			{
				throw new InvalidOperationException("item_type_commit_validation_failed");
			}
			if (!TrySetRewardItemObjectName(target, displayName))
			{
				throw new InvalidOperationException("display_name_commit_validation_failed");
			}
			if (!TryEnsureGeneratedRewardItemTemplateCategory(target, templateItem, "template_state"))
			{
				throw new InvalidOperationException("item_category_commit_validation_failed");
			}
			if (!TryApplyPlayerRpCraftSnapshot(target, templateItem, "template_state"))
			{
				throw new InvalidOperationException("player_snapshot_commit_failed");
			}
			if (!HasCloneSafeGeneratedRewardThumbnailSource(target))
			{
				throw new InvalidOperationException("thumbnail_source_commit_validation_failed");
			}
			bool snapshotOverridesComponentAndWeight =
				HasActivePlayerRpCraftEquipmentSnapshot(target);
			for (int i = 0; i < propertyCount; i++)
			{
				string propertyName = GeneratedRewardItemTemplateStatePropertyNames[i];
				if (string.Equals(propertyName, "ItemCategory", StringComparison.Ordinal)
					|| (snapshotOverridesComponentAndWeight
						&& (string.Equals(propertyName, "ItemComponent", StringComparison.Ordinal)
							|| string.Equals(propertyName, "Weight", StringComparison.Ordinal))))
				{
					continue;
				}
				PropertyInfo property = GeneratedRewardItemTemplateStateProperties[i];
				if (!AreGeneratedRewardTemplatePropertyValuesEquivalent(
					property.GetValue(target, null),
					desiredValues[i]))
				{
					throw new InvalidOperationException(
						"property_post_commit_validation_failed:" + propertyName);
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			failure = ex.GetType().Name + ":" + ex.Message;
		}

		bool rollbackSucceeded = !stateCaptured
			|| TryRestoreGeneratedRewardItemTemplateState(
				target,
				previousType,
				previousName,
				previousValues);
		LogGeneratedRewardTemplateStateFailure(
			target,
			templateItem,
			failure,
			rollbackSucceeded);
		return false;
	}

	private static bool AreGeneratedRewardTemplatePropertyValuesEquivalent(
		object left,
		object right)
	{
		if (ReferenceEquals(left, right))
		{
			return true;
		}
		return left != null && right != null && left.Equals(right);
	}

	private static bool TryRestoreGeneratedRewardItemTemplateState(
		ItemObject target,
		ItemObject.ItemTypeEnum previousType,
		object previousName,
		object[] previousValues)
	{
		if (target == null
			|| previousValues == null
			|| previousValues.Length != GeneratedRewardItemTemplateStateProperties.Length)
		{
			return false;
		}
		bool restored = true;
		for (int i = GeneratedRewardItemTemplateStateProperties.Length - 1; i >= 0; i--)
		{
			PropertyInfo property = GeneratedRewardItemTemplateStateProperties[i];
			if (property?.GetGetMethod(nonPublic: true) == null)
			{
				restored = false;
				continue;
			}
			try
			{
				object previousValue = previousValues[i];
				if (!AreGeneratedRewardTemplatePropertyValuesEquivalent(
					property.GetValue(target, null),
					previousValue))
				{
					MethodInfo setter = property.GetSetMethod(nonPublic: true);
					if (setter == null)
					{
						restored = false;
						continue;
					}
					property.SetValue(target, previousValue, null);
				}
				if (!AreGeneratedRewardTemplatePropertyValuesEquivalent(
					property.GetValue(target, null),
					previousValue))
				{
					restored = false;
				}
			}
			catch
			{
				restored = false;
			}
		}
		try
		{
			target.Type = previousType;
			restored &= target.Type == previousType;
		}
		catch
		{
			restored = false;
		}
		try
		{
			if (!AreGeneratedRewardTemplatePropertyValuesEquivalent(
				RewardItemObjectNameProperty?.GetValue(target, null),
				previousName))
			{
				RewardItemObjectNameProperty?.SetValue(target, previousName, null);
			}
			if (!AreGeneratedRewardTemplatePropertyValuesEquivalent(
				RewardItemObjectNameProperty?.GetValue(target, null),
				previousName))
			{
				restored = false;
			}
		}
		catch
		{
			restored = false;
		}
		return restored;
	}

	private static bool TryEnsureGeneratedRewardItemTemplateCategory(
		ItemObject item,
		ItemObject templateItem,
		string source)
	{
		if (item == null || templateItem == null)
		{
			return false;
		}
		ItemCategory templateCategory = templateItem.ItemCategory;
		if (templateCategory == null)
		{
			return TryEnsureGeneratedRewardItemCategory(item, null, source);
		}
		if (!ReferenceEquals(item.ItemCategory, templateCategory)
			&& !TrySetGeneratedRewardItemCategory(item, templateCategory))
		{
			return false;
		}
		return ReferenceEquals(item.ItemCategory, templateCategory);
	}

	private static bool HasActivePlayerRpCraftEquipmentSnapshot(ItemObject target)
	{
		if (target == null || !IsGeneratedRewardItemStringId(target.StringId))
		{
			return false;
		}
		GeneratedRewardItemRecord record = Instance?.GetGeneratedRewardItemRecord(target.StringId);
		if (record == null)
		{
			lock (GeneratedRewardItemRegistrationLock)
			{
				GeneratedRewardManifestByStringId.TryGetValue(target.StringId.Trim(), out record);
			}
		}
		return string.Equals(
				record?.PlayerCraft?.CraftKind,
				"equipment",
				StringComparison.OrdinalIgnoreCase)
			&& record.PlayerCraft.StatsSnapshot != null;
	}

	private static void LogGeneratedRewardTemplateStateFailure(
		ItemObject target,
		ItemObject templateItem,
		string failure,
		bool rollbackSucceeded)
	{
		try
		{
			Logger.Log("Logic", "[RewardItemResolve] generated_template_state_failed item="
				+ (target?.StringId ?? "")
				+ " template="
				+ (templateItem?.StringId ?? "")
				+ " failure="
				+ (failure ?? "")
				+ " rollback="
				+ rollbackSucceeded);
		}
		catch
		{
		}
	}

	private static bool TryApplyPlayerRpCraftSnapshot(ItemObject target, ItemObject templateItem, string source)
	{
		if (target == null || templateItem == null || !IsGeneratedRewardItemStringId(target.StringId))
		{
			return false;
		}
		GeneratedRewardItemRecord record = Instance?.GetGeneratedRewardItemRecord(target.StringId);
		if (record == null)
		{
			lock (GeneratedRewardItemRegistrationLock)
			{
				GeneratedRewardManifestByStringId.TryGetValue(target.StringId.Trim(), out record);
			}
		}
		if (record?.PlayerCraft == null)
		{
			return true;
		}
		if (string.Equals(record.PlayerCraft.CraftKind, "remnant", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		PlayerRpCraftItemStatsSnapshot snapshot = record.PlayerCraft.StatsSnapshot;
		if (snapshot == null)
		{
			if (string.Equals(record.PlayerCraft.CraftKind, "equipment", StringComparison.OrdinalIgnoreCase))
			{
				TryTransitionPlayerRpCraftEquipmentToRemnant(
					record,
					record.PlayerCraft,
					source,
					"missing_snapshot",
					out _);
				return false;
			}
			return true;
		}
		if (PlayerRpCraftItemComponentService.TryApplySnapshot(target, templateItem, snapshot, out string error))
		{
			return true;
		}
		TryTransitionPlayerRpCraftEquipmentToRemnant(
			record,
			record.PlayerCraft,
			source,
			"snapshot_apply_failed:" + (error ?? ""),
			out _);
		try
		{
			Logger.Log("Logic", "[PlayerRpCraft] snapshot_apply_failed source=" + (source ?? "")
				+ " item=" + (target.StringId ?? "")
				+ " template=" + (templateItem.StringId ?? "")
				+ " error=" + (error ?? ""));
		}
		catch
		{
		}
		return false;
	}

	private static void ApplyGeneratedRewardItemRpState(ItemObject target, string displayName)
	{
		if (target == null)
		{
			return;
		}
		try
		{
			string text = NormalizeGeneratedInventoryDisplayName(displayName);
			if (string.IsNullOrWhiteSpace(text))
			{
				text = target.Name?.ToString() ?? target.StringId ?? "";
			}
			TrySetRewardItemObjectName(target, text);
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_rp_state_failed item=" + (target.StringId ?? "") + " error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
		}
	}

	private static bool IsGeneratedRewardItemStringId(string stringId)
	{
		return !string.IsNullOrWhiteSpace(stringId) && stringId.Trim().StartsWith("af_generated_reward_", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsGeneratedRewardMarketExcludedItem(ItemObject item)
	{
		return IsGeneratedRewardItemStringId(item?.StringId);
	}

	private static void CampaignAllItemsGetterPostfix(ref MBReadOnlyList<ItemObject> __result)
	{
		try
		{
			__result = GetGeneratedRewardFilteredEconomicPool(__result);
		}
		catch
		{
		}
	}

	private static MBReadOnlyList<ItemObject> GetGeneratedRewardFilteredEconomicPool(MBReadOnlyList<ItemObject> source)
	{
		if (source == null)
		{
			return null;
		}
		int observedSourceCount = source.Count;
		MBReadOnlyList<ItemObject> cachedFiltered = GeneratedRewardEconomicPoolFiltered;
		if (ReferenceEquals(source, GeneratedRewardEconomicPoolSource)
			&& observedSourceCount == GeneratedRewardEconomicPoolSourceCount
			&& cachedFiltered != null)
		{
			return cachedFiltered;
		}
		lock (GeneratedRewardEconomicPoolCacheLock)
		{
			int sourceCount = source.Count;
			if (ReferenceEquals(source, GeneratedRewardEconomicPoolSource)
				&& sourceCount == GeneratedRewardEconomicPoolSourceCount
				&& GeneratedRewardEconomicPoolFiltered != null)
			{
				return GeneratedRewardEconomicPoolFiltered;
			}
			bool hasGeneratedItems = false;
			for (int i = 0; i < sourceCount; i++)
			{
				if (IsGeneratedRewardMarketExcludedItem(source[i]))
				{
					hasGeneratedItems = true;
					break;
				}
			}
			MBReadOnlyList<ItemObject> filtered = source;
			if (hasGeneratedItems)
			{
				MBList<ItemObject> items = new MBList<ItemObject>(sourceCount);
				for (int j = 0; j < sourceCount; j++)
				{
					ItemObject item = source[j];
					if (!IsGeneratedRewardMarketExcludedItem(item))
					{
						items.Add(item);
					}
				}
				filtered = items;
			}
			GeneratedRewardEconomicPoolSource = source;
			GeneratedRewardEconomicPoolSourceCount = sourceCount;
			GeneratedRewardEconomicPoolFiltered = filtered;
			return filtered;
		}
	}

	private static void ClearGeneratedRewardEconomicPoolCache()
	{
		lock (GeneratedRewardEconomicPoolCacheLock)
		{
			GeneratedRewardEconomicPoolSource = null;
			GeneratedRewardEconomicPoolFiltered = null;
			GeneratedRewardEconomicPoolSourceCount = -1;
		}
	}

	private static void WorkshopsCampaignBehaviorFillItemsInAllCategoriesPostfix(WorkshopsCampaignBehavior __instance)
	{
		try
		{
			if (!(WorkshopsItemsInCategoryField?.GetValue(__instance) is Dictionary<ItemCategory, List<ItemObject>> itemsByCategory))
			{
				return;
			}
			int removed = 0;
			foreach (List<ItemObject> items in itemsByCategory.Values)
			{
				if (items != null)
				{
					removed += items.RemoveAll(IsGeneratedRewardMarketExcludedItem);
				}
			}
			if (removed > 0)
			{
				Logger.Log("Logic", "[RewardItemEconomicGuard] workshop_cache_pruned count=" + removed.ToString(CultureInfo.InvariantCulture));
			}
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemEconomicGuard] workshop_cache_prune_failed error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
		}
	}

	private static void WorkshopsCampaignBehaviorGetRandomItemPostfix(WorkshopsCampaignBehavior __instance, ItemCategory itemGroupBase, Town townComponent, ref EquipmentElement __result)
	{
		ItemObject generatedItem = __result.Item;
		if (!IsGeneratedRewardMarketExcludedItem(generatedItem))
		{
			return;
		}
		try
		{
			ItemObject replacement = ResolveGeneratedRewardWorkshopOutputReplacement(__instance, generatedItem, itemGroupBase, townComponent);
			if (replacement == null)
			{
				__result = default(EquipmentElement);
				Logger.Log("Logic", "[RewardItemEconomicGuard] workshop_output_blocked generated=" + (generatedItem.StringId ?? "") + " category=" + (itemGroupBase?.StringId ?? "") + " reason=no_normal_candidate");
				return;
			}
			ItemModifier modifier = __result.ItemModifier;
			if (!ReferenceEquals(generatedItem.ItemComponent?.ItemModifierGroup, replacement.ItemComponent?.ItemModifierGroup))
			{
				modifier = replacement.ItemComponent?.ItemModifierGroup?.GetRandomItemModifierProductionScoreBased();
			}
			__result = new EquipmentElement(replacement, modifier, __result.CosmeticItem, __result.IsQuestItem);
			Logger.Log("Logic", "[RewardItemEconomicGuard] workshop_output_replaced generated=" + (generatedItem.StringId ?? "") + " replacement=" + (replacement.StringId ?? "") + " category=" + (itemGroupBase?.StringId ?? ""));
		}
		catch (Exception ex)
		{
			__result = default(EquipmentElement);
			try
			{
				Logger.Log("Logic", "[RewardItemEconomicGuard] workshop_output_guard_failed generated=" + (generatedItem.StringId ?? "") + " error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
		}
	}

	private static ItemObject ResolveGeneratedRewardWorkshopOutputReplacement(WorkshopsCampaignBehavior behavior, ItemObject generatedItem, ItemCategory category, Town town)
	{
		GeneratedRewardItemRecord record = Instance?.GetGeneratedRewardItemRecord(generatedItem?.StringId);
		ItemObject templateItem = ResolveItemById(record?.TemplateStringId);
		if (IsEligibleNormalWorkshopOutput(templateItem, category))
		{
			return templateItem;
		}
		IEnumerable<ItemObject> candidates = null;
		try
		{
			if (WorkshopsItemsInCategoryField?.GetValue(behavior) is Dictionary<ItemCategory, List<ItemObject>> itemsByCategory
				&& category != null
				&& itemsByCategory.TryGetValue(category, out List<ItemObject> categoryItems))
			{
				candidates = categoryItems;
			}
		}
		catch
		{
		}
		candidates ??= Game.Current?.ObjectManager?.GetObjectTypeList<ItemObject>() ?? MBObjectManager.Instance?.GetObjectTypeList<ItemObject>();
		ItemObject preferred = ChooseNormalWorkshopOutput(candidates, category, town, preferredCultureOnly: true);
		return preferred ?? ChooseNormalWorkshopOutput(candidates, category, town, preferredCultureOnly: false);
	}

	private static ItemObject ChooseNormalWorkshopOutput(IEnumerable<ItemObject> candidates, ItemCategory category, Town town, bool preferredCultureOnly)
	{
		List<(ItemObject, float)> weighted = new List<(ItemObject, float)>();
		foreach (ItemObject item in candidates ?? Enumerable.Empty<ItemObject>())
		{
			if (!IsEligibleNormalWorkshopOutput(item, category)
				|| (preferredCultureOnly && !IsWorkshopOutputPreferredForTown(item, town)))
			{
				continue;
			}
			weighted.Add((item, 1f / (Math.Max(100, item.Value) + 100f)));
		}
		return weighted.Count > 0 ? MBRandom.ChooseWeighted(weighted) : null;
	}

	private static bool IsEligibleNormalWorkshopOutput(ItemObject item, ItemCategory category)
	{
		return item != null
			&& category != null
			&& ReferenceEquals(item.ItemCategory, category)
			&& !IsGeneratedRewardMarketExcludedItem(item)
			&& !item.MultiplayerItem
			&& !item.NotMerchandise
			&& !item.IsCraftedByPlayer;
	}

	private static bool IsWorkshopOutputPreferredForTown(ItemObject item, Town town)
	{
		if (town == null || item?.Culture == null)
		{
			return true;
		}
		string cultureId = item.Culture.StringId ?? "";
		return string.Equals(cultureId, "neutral_culture", StringComparison.OrdinalIgnoreCase) || ReferenceEquals(item.Culture, town.Culture);
	}

	private static void HideoutCampaignBehaviorOnSessionLaunchedPostfix(HideoutCampaignBehavior __instance)
	{
		try
		{
			if (!(HideoutPotentialLootItemsField?.GetValue(__instance) is List<ItemObject> items))
			{
				return;
			}
			int removed = items.RemoveAll(IsGeneratedRewardMarketExcludedItem);
			if (removed > 0)
			{
				Logger.Log("Logic", "[RewardItemEconomicGuard] hideout_loot_cache_pruned count=" + removed.ToString(CultureInfo.InvariantCulture));
			}
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemEconomicGuard] hideout_loot_cache_prune_failed error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
		}
	}

	private static void InventoryScreenHelperOpenScreenAsTradePrefix(ItemRoster leftRoster, SettlementComponent settlementComponent, ref Action doneLogicExtrasDelegate)
	{
		try
		{
			RemoveGeneratedRewardItemsFromSettlementMarket(settlementComponent?.Settlement, "trade_open");
			RemoveGeneratedRewardItemsFromRoster(leftRoster, BuildGeneratedRewardMarketRosterLabel(settlementComponent?.Settlement, "trade_roster"), "trade_open_roster");
			Action originalDone = doneLogicExtrasDelegate;
			doneLogicExtrasDelegate = delegate
			{
				try
				{
					originalDone?.Invoke();
				}
				finally
				{
					RemoveGeneratedRewardItemsFromSettlementMarket(settlementComponent?.Settlement, "trade_close");
					RemoveGeneratedRewardItemsFromRoster(leftRoster, BuildGeneratedRewardMarketRosterLabel(settlementComponent?.Settlement, "trade_roster"), "trade_close_roster");
				}
			};
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemMarketGuard] trade_open_cleanup_failed error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
		}
	}

	private void OnDailyTickSettlement(Settlement settlement)
	{
		RemoveGeneratedRewardItemsFromSettlementMarket(settlement, "daily_tick_settlement");
	}

	private static bool InventoryLogicAddTransferCommandPrefix(InventoryLogic __instance, TransferCommand command)
	{
		return !ShouldBlockGeneratedRewardMarketTransfer(__instance, command, notify: true);
	}

	private static bool InventoryLogicAddTransferCommandsPrefix(InventoryLogic __instance, IEnumerable<TransferCommand> commands)
	{
		if (commands == null)
		{
			return true;
		}
		List<TransferCommand> list = commands.ToList();
		if (!list.Any((TransferCommand command) => ShouldBlockGeneratedRewardMarketTransfer(__instance, command, notify: false)))
		{
			return true;
		}
		bool notified = false;
		foreach (TransferCommand command in list)
		{
			if (ShouldBlockGeneratedRewardMarketTransfer(__instance, command, notify: !notified))
			{
				notified = true;
				continue;
			}
			__instance?.AddTransferCommand(command);
		}
		return false;
	}

	private static bool SellItemsActionApplyPrefix(PartyBase receiverParty, PartyBase payerParty, ItemRosterElement subject)
	{
		if (!IsGeneratedRewardMarketExcludedItem(subject.EquipmentElement.Item))
		{
			return true;
		}
		bool involvesSettlementMarket = receiverParty?.IsSettlement == true || payerParty?.IsSettlement == true;
		if (!involvesSettlementMarket)
		{
			return true;
		}
		try
		{
			Logger.Log("Logic", "[RewardItemMarketGuard] blocked_party_settlement_sale item=" + (subject.EquipmentElement.Item.StringId ?? "") + " seller=" + (receiverParty?.Name?.ToString() ?? "") + " buyer=" + (payerParty?.Name?.ToString() ?? ""));
		}
		catch
		{
		}
		return false;
	}

	private static void VillagerSellGoodsPrefix(MobileParty villagerParty, out List<ItemRosterElement> __state)
	{
		__state = TakeGeneratedRewardItemsFromRoster(villagerParty?.ItemRoster);
	}

	private static Exception VillagerSellGoodsFinalizer(MobileParty villagerParty, List<ItemRosterElement> __state, Exception __exception)
	{
		RestoreGeneratedRewardItemsToRoster(villagerParty?.ItemRoster, __state);
		return __exception;
	}

	private static void CaravanSellGoodsPrefix(MobileParty mobileParty, out List<ItemRosterElement> __state)
	{
		__state = TakeGeneratedRewardItemsFromRoster(mobileParty?.ItemRoster);
	}

	private static Exception CaravanSellGoodsFinalizer(MobileParty mobileParty, List<ItemRosterElement> __state, Exception __exception)
	{
		RestoreGeneratedRewardItemsToRoster(mobileParty?.ItemRoster, __state);
		return __exception;
	}

	private static List<ItemRosterElement> TakeGeneratedRewardItemsFromRoster(ItemRoster roster)
	{
		List<ItemRosterElement> hidden = new List<ItemRosterElement>();
		if (roster == null)
		{
			return hidden;
		}
		List<ItemRosterElement> candidates = new List<ItemRosterElement>();
		try
		{
			for (int i = 0; i < roster.Count; i++)
			{
				ItemRosterElement element = roster.GetElementCopyAtIndex(i);
				if (element.Amount > 0 && IsGeneratedRewardMarketExcludedItem(element.EquipmentElement.Item))
				{
					candidates.Add(element);
				}
			}
			foreach (ItemRosterElement element in candidates)
			{
				roster.AddToCounts(element.EquipmentElement, -element.Amount);
				hidden.Add(element);
			}
		}
		catch (Exception ex)
		{
			RestoreGeneratedRewardItemsToRoster(roster, hidden);
			hidden.Clear();
			try
			{
				Logger.Log("Logic", "[RewardItemMarketGuard] market_party_hide_failed error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
		}
		return hidden;
	}

	private static void RestoreGeneratedRewardItemsToRoster(ItemRoster roster, IEnumerable<ItemRosterElement> hidden)
	{
		if (roster == null || hidden == null)
		{
			return;
		}
		try
		{
			foreach (ItemRosterElement element in hidden)
			{
				if (element.Amount > 0)
				{
					roster.AddToCounts(element.EquipmentElement, element.Amount);
				}
			}
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemMarketGuard] market_party_restore_failed error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
		}
	}

	private static bool ShouldBlockGeneratedRewardMarketTransfer(InventoryLogic inventoryLogic, TransferCommand command, bool notify)
	{
		try
		{
			if (inventoryLogic == null || !inventoryLogic.IsTrading)
			{
				return false;
			}
			ItemObject item = command.ElementToTransfer.EquipmentElement.Item;
			if (!IsGeneratedRewardMarketExcludedItem(item))
			{
				return false;
			}
			bool involvesMarketSide = command.FromSide == InventoryLogic.InventorySide.OtherInventory || command.ToSide == InventoryLogic.InventorySide.OtherInventory;
			bool involvesPlayerSide = command.FromSide == InventoryLogic.InventorySide.PlayerInventory
				|| command.ToSide == InventoryLogic.InventorySide.PlayerInventory
				|| InventoryLogic.IsEquipmentSide(command.FromSide)
				|| InventoryLogic.IsEquipmentSide(command.ToSide);
			if (!involvesMarketSide || !involvesPlayerSide)
			{
				return false;
			}
			if (notify)
			{
				InformationManager.DisplayMessage(new InformationMessage("RP生成物品不会进入市场交易。"));
			}
			try
			{
				Logger.Log("Logic", "[RewardItemMarketGuard] blocked_trade_transfer item=" + (item.StringId ?? "") + " from=" + command.FromSide + " to=" + command.ToSide + " amount=" + command.Amount.ToString(CultureInfo.InvariantCulture));
			}
			catch
			{
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static string BuildGeneratedRewardMarketRosterLabel(Settlement settlement, string fallback)
	{
		string text = (settlement?.StringId ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? (fallback ?? "") : text;
	}

	private static int RemoveGeneratedRewardItemsFromRoster(ItemRoster roster, string ownerLabel, string reason)
	{
		if (roster == null)
		{
			return 0;
		}
		int removed = 0;
		List<ItemRosterElement> toRemove = new List<ItemRosterElement>();
		try
		{
			for (int i = 0; i < roster.Count; i++)
			{
				ItemRosterElement element = roster.GetElementCopyAtIndex(i);
				if (element.Amount > 0 && IsGeneratedRewardMarketExcludedItem(element.EquipmentElement.Item))
				{
					toRemove.Add(element);
				}
			}
			foreach (ItemRosterElement element in toRemove)
			{
				int amount = Math.Max(0, element.Amount);
				if (amount <= 0)
				{
					continue;
				}
				roster.AddToCounts(element.EquipmentElement, -amount);
				removed += amount;
			}
			if (removed > 0)
			{
				Logger.Log("Logic", "[RewardItemMarketGuard] removed_market_generated_items reason=" + (reason ?? "") + " owner=" + (ownerLabel ?? "") + " count=" + removed.ToString(CultureInfo.InvariantCulture) + " slots=" + toRemove.Count.ToString(CultureInfo.InvariantCulture));
			}
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemMarketGuard] cleanup_failed reason=" + (reason ?? "") + " owner=" + (ownerLabel ?? "") + " error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
		}
		return removed;
	}

	private static int RemoveGeneratedRewardItemsFromSettlementMarket(Settlement settlement, string reason)
	{
		try
		{
			return RemoveGeneratedRewardItemsFromRoster(settlement?.ItemRoster, BuildGeneratedRewardMarketRosterLabel(settlement, "settlement_market"), reason);
		}
		catch
		{
			return 0;
		}
	}

	private static int RemoveGeneratedRewardItemsFromMarketRosters(string reason)
	{
		int removed = 0;
		IEnumerable<Settlement> settlements = null;
		try
		{
			settlements = Campaign.Current?.Settlements;
		}
		catch
		{
		}
		foreach (Settlement settlement in settlements ?? Enumerable.Empty<Settlement>())
		{
			removed += RemoveGeneratedRewardItemsFromSettlementMarket(settlement, reason);
		}
		return removed;
	}

	private GeneratedRewardItemRecord GetGeneratedRewardItemRecord(string generatedStringId)
	{
		if (!IsGeneratedRewardItemStringId(generatedStringId))
		{
			return null;
		}
		EnsureGeneratedRewardItemData();
		string key = generatedStringId.Trim();
		_generatedRewardItemRecords.TryGetValue(key, out var record);
		if (record != null)
		{
			return record;
		}
		EnsureGeneratedRewardManifestLoaded();
		lock (GeneratedRewardItemRegistrationLock)
		{
			GeneratedRewardManifestByStringId.TryGetValue(key, out record);
		}
		return record;
	}

	private static GeneratedRewardItemRecord NormalizeGeneratedRewardItemRecord(string fallbackKey, GeneratedRewardItemRecord record)
	{
		if (record == null)
		{
			return null;
		}
		string generatedStringId = (record.GeneratedStringId ?? fallbackKey ?? "").Trim();
		if (!IsGeneratedRewardItemStringId(generatedStringId))
		{
			return null;
		}
		string displayName = (record.DisplayName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(displayName))
		{
			displayName = generatedStringId;
		}
		string templateStringId = (record.TemplateStringId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(templateStringId))
		{
			return null;
		}
		record.GeneratedStringId = generatedStringId;
		record.DisplayName = displayName;
		record.TemplateStringId = templateStringId;
		if (record.LegacyObjectIds == null)
		{
			record.LegacyObjectIds = new List<uint>();
		}
		record.LegacyObjectIds = record.LegacyObjectIds.Where((uint x) => x != 0u && x != record.ObjectId).Distinct().Take(16).ToList();
		record.LastTouchedDay = Math.Max(0, record.LastTouchedDay);
		record.RpItemIntroductionText = AnimusForgeTextInputSanitizer.SanitizeMultiline(record.RpItemIntroductionText ?? "", AnimusForgeTextInputSanitizer.MaxCourierLetterChars).Trim();
		if (string.IsNullOrWhiteSpace(record.RpItemIntroductionText))
		{
			record.RpItemIntroductionText = "";
			record.RpItemIntroductionSource = "";
			record.RpItemIntroductionLastTouchedDay = 0;
		}
		else
		{
			record.RpItemIntroductionSource = string.Equals((record.RpItemIntroductionSource ?? "").Trim(), "player", StringComparison.OrdinalIgnoreCase) ? "player" : "npc";
			record.RpItemIntroductionLastTouchedDay = Math.Max(0, record.RpItemIntroductionLastTouchedDay);
		}
		record.PlayerCraft = NormalizePlayerRpCraftData(record.PlayerCraft, record);
		return record;
	}

	private static PlayerRpCraftData NormalizePlayerRpCraftData(PlayerRpCraftData data, GeneratedRewardItemRecord owner)
	{
		if (data == null)
		{
			return null;
		}
		int storedSchemaVersion = data.SchemaVersion;
		data.SchemaVersion = data.SchemaVersion <= 0 ? PlayerRpCraftData.CurrentSchemaVersion : data.SchemaVersion;
		data.FormulaVersion = data.FormulaVersion <= 0 ? PlayerRpCraftData.CurrentFormulaVersion : data.FormulaVersion;
		data.BatchId = (data.BatchId ?? "").Trim();
		data.CreatorHeroId = (data.CreatorHeroId ?? "").Trim();
		data.CrafterHeroId = string.IsNullOrWhiteSpace(data.CrafterHeroId)
			? data.CreatorHeroId
			: data.CrafterHeroId.Trim();
		data.CrafterDisplayNameSnapshot =
			(data.CrafterDisplayNameSnapshot ?? "").Trim();
		data.CrafterCraftingStaminaSnapshot =
			Math.Max(0, data.CrafterCraftingStaminaSnapshot);
		data.CrafterMaxCraftingStaminaSnapshot = Math.Max(
			data.CrafterCraftingStaminaSnapshot,
			Math.Max(0, data.CrafterMaxCraftingStaminaSnapshot));
		data.CraftingStaminaCost =
			Math.Max(0, data.CraftingStaminaCost);
		data.CrafterCraftingStaminaAfterSnapshot =
			Math.Max(0, data.CrafterCraftingStaminaAfterSnapshot);
		data.CraftingExperienceBaseAmount =
			Math.Max(0, data.CraftingExperienceBaseAmount);
		data.OriginalRequestedName = string.IsNullOrWhiteSpace(data.OriginalRequestedName)
			? owner?.DisplayName ?? ""
			: data.OriginalRequestedName.Trim();
		data.OriginalTemplateStringId = string.IsNullOrWhiteSpace(data.OriginalTemplateStringId)
			? owner?.TemplateStringId ?? ""
			: data.OriginalTemplateStringId.Trim();
		data.EffectiveTemplateStringId = string.IsNullOrWhiteSpace(data.EffectiveTemplateStringId)
			? owner?.TemplateStringId ?? ""
			: data.EffectiveTemplateStringId.Trim();
		data.CraftKind = (data.CraftKind ?? "").Trim();
		data.Outcome = (data.Outcome ?? "").Trim();
		data.InvestedDenars = Math.Max(0, data.InvestedDenars);
		if (data.CraftedItemValue <= 0
			&& storedSchemaVersion < 3
			&& data.InvestedDenars > 0)
		{
			// Schema 1/2 items were intentionally worth the full investment.
			// Persist that legacy value instead of repricing old saves.
			data.CraftedItemValue = data.InvestedDenars;
		}
		else
		{
			data.CraftedItemValue =
				Math.Max(0, data.CraftedItemValue);
		}
		data.TemplateBaseValue = Math.Max(1, data.TemplateBaseValue);
		data.GoodWeight = Math.Max(0, data.GoodWeight);
		data.NormalWeight = Math.Max(0, data.NormalWeight);
		data.BadWeight = Math.Max(0, data.BadWeight);
		data.Roll = Math.Max(0, data.Roll);
		data.UpgradeLevel = Math.Max(0, data.UpgradeLevel);
		data.AppliedBonus = Math.Max(0, data.AppliedBonus);
		data.CreatedDay = Math.Max(0, data.CreatedDay);
		data.InitialQuantity = Math.Max(1, data.InitialQuantity);
		if (data.Inspections == null)
		{
			data.Inspections = new Dictionary<string, PlayerRpCraftInspectionRecord>(StringComparer.OrdinalIgnoreCase);
		}
		else
		{
			Dictionary<string, PlayerRpCraftInspectionRecord> normalizedInspections =
				new Dictionary<string, PlayerRpCraftInspectionRecord>(StringComparer.OrdinalIgnoreCase);
			foreach (KeyValuePair<string, PlayerRpCraftInspectionRecord> pair in data.Inspections)
			{
				string observerKey = (pair.Key ?? pair.Value?.ObserverKey ?? "").Trim();
				if (string.IsNullOrWhiteSpace(observerKey) || pair.Value == null)
				{
					continue;
				}
				pair.Value.ObserverKey = observerKey;
				normalizedInspections[observerKey] = pair.Value;
			}
			data.Inspections = normalizedInspections;
		}
		return data;
	}

	private static PlayerRpCraftData MergePlayerRpCraftData(PlayerRpCraftData preferred, PlayerRpCraftData fallback)
	{
		if (preferred == null)
		{
			return fallback;
		}
		if (fallback != null)
		{
			if (preferred.CraftedItemValue <= 0
				&& fallback.CraftedItemValue > 0)
			{
				preferred.CraftedItemValue =
					fallback.CraftedItemValue;
			}
			if (preferred.CraftingStaminaCost <= 0
				&& fallback.CraftingStaminaCost > 0)
			{
				preferred.CraftingStaminaCost =
					fallback.CraftingStaminaCost;
				preferred.CrafterCraftingStaminaAfterSnapshot =
					Math.Max(
						0,
						fallback.CrafterCraftingStaminaAfterSnapshot);
			}
			if (preferred.CraftingExperienceBaseAmount <= 0
				&& fallback.CraftingExperienceBaseAmount > 0)
			{
				preferred.CraftingExperienceBaseAmount =
					fallback.CraftingExperienceBaseAmount;
			}
		}
		if (fallback?.Inspections == null || fallback.Inspections.Count == 0)
		{
			return preferred;
		}
		if (preferred.Inspections == null)
		{
			preferred.Inspections = new Dictionary<string, PlayerRpCraftInspectionRecord>(StringComparer.OrdinalIgnoreCase);
		}
		foreach (KeyValuePair<string, PlayerRpCraftInspectionRecord> pair in fallback.Inspections)
		{
			if (!string.IsNullOrWhiteSpace(pair.Key) && pair.Value != null && !preferred.Inspections.ContainsKey(pair.Key))
			{
				preferred.Inspections[pair.Key] = pair.Value;
			}
		}
		return preferred;
	}

	private void RememberGeneratedRewardItemRecord(string generatedStringId, string displayName, ItemObject templateItem, ItemObject generatedItem)
	{
		if (IsGeneratedRewardItemStringId(generatedStringId) && templateItem != null && generatedItem != null)
		{
			EnsureGeneratedRewardItemData();
			string key = generatedStringId.Trim();
			if (!_generatedRewardItemRecords.TryGetValue(key, out var record) || record == null)
			{
				record = GetGeneratedRewardItemRecord(key) ?? new GeneratedRewardItemRecord();
			}
			uint oldObjectId = record.ObjectId;
			uint newObjectId = generatedItem.Id.InternalValue;
			record.GeneratedStringId = key;
			record.DisplayName = string.IsNullOrWhiteSpace(displayName) ? (generatedItem.Name?.ToString() ?? key) : displayName.Trim();
			record.TemplateStringId = (templateItem.StringId ?? "").Trim();
			if (record.LegacyObjectIds == null)
			{
				record.LegacyObjectIds = new List<uint>();
			}
			if (oldObjectId != 0u && newObjectId != 0u && oldObjectId != newObjectId && !record.LegacyObjectIds.Contains(oldObjectId))
			{
				record.LegacyObjectIds.Add(oldObjectId);
			}
			record.LegacyObjectIds = record.LegacyObjectIds.Where((uint x) => x != 0u && x != newObjectId).Distinct().Take(16).ToList();
			record.ObjectId = newObjectId;
			record.LastTouchedDay = GetCampaignDayIndex();
			_generatedRewardItemRecords[key] = record;
			RegisterGeneratedRewardManifestRecord(record);
			SaveGeneratedRewardManifest("remember");
		}
	}

	private void RestoreGeneratedRewardItemDefinitions(string reason)
	{
		EnsureGeneratedRewardItemData();
		MergeGeneratedRewardManifestIntoRecords();
		if (_generatedRewardItemRecords.Count == 0)
		{
			return;
		}
		int restored = 0;
		int alreadyLoaded = 0;
		int failed = 0;
		List<string> sample = new List<string>();
		foreach (KeyValuePair<string, GeneratedRewardItemRecord> pair in _generatedRewardItemRecords.ToList())
		{
			GeneratedRewardItemRecord record = NormalizeGeneratedRewardItemRecord(pair.Key, pair.Value);
			if (record == null)
			{
				failed++;
				continue;
			}
			_generatedRewardItemRecords[record.GeneratedStringId] = record;
			ItemObject templateItem = ResolveGeneratedRewardRecordTemplateItem(record, (reason ?? "") + "_definition_restore");
			if (templateItem == null)
			{
				failed++;
				if (sample.Count < 8)
				{
					sample.Add(record.GeneratedStringId + ":missing_template:" + record.TemplateStringId);
				}
				continue;
			}
			record.TemplateStringId = (templateItem.StringId ?? "").Trim();
			bool wasAlreadyLoaded = TryGetRegisteredGeneratedRewardItemByStringId(record.GeneratedStringId) != null;
			ItemObject restoredItem = TryGetOrCreateGeneratedRewardItem(record.GeneratedStringId, record.DisplayName, templateItem, reason);
			if (restoredItem != null)
			{
				if (wasAlreadyLoaded)
				{
					alreadyLoaded++;
				}
				else
				{
					restored++;
				}
				if (sample.Count < 8)
				{
					sample.Add(record.GeneratedStringId + ":" + restoredItem.Id.InternalValue.ToString(CultureInfo.InvariantCulture));
				}
			}
			else
			{
				failed++;
				if (sample.Count < 8)
				{
					sample.Add(record.GeneratedStringId + ":restore_failed");
				}
			}
		}
		if (restored > 0 || failed > 0)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_restore reason=" + (reason ?? "") + " restored=" + restored + " already=" + alreadyLoaded + " failed=" + failed + " sample=" + string.Join(",", sample));
			}
			catch
			{
			}
		}
		RepairGeneratedRewardItemRosters((reason ?? "") + "_restore");
	}

	private static void ItemRosterReplaceInvalidItemsWithTrashPrefix(ItemRoster itemRoster)
	{
		try
		{
			Instance?.RepairGeneratedRewardItemRoster(itemRoster, "before_trash", out _);
		}
		catch
		{
		}
	}

	private void RepairGeneratedRewardItemRosters(string reason)
	{
		EnsureGeneratedRewardItemData();
		MergeGeneratedRewardManifestIntoRecords();
		if (_generatedRewardItemRecords.Count == 0)
		{
			return;
		}
		Dictionary<uint, GeneratedRewardItemRecord> recordsByObjectId = BuildGeneratedRewardItemRecordsByObjectId();
		if (recordsByObjectId.Count == 0)
		{
			return;
		}
		int repaired = 0;
		int scanned = 0;
		HashSet<ItemRoster> seen = new HashSet<ItemRoster>();
		foreach (ItemRoster roster in EnumerateGeneratedRewardCandidateRosters())
		{
			if (roster == null || !seen.Add(roster))
			{
				continue;
			}
			scanned++;
			if (RepairGeneratedRewardItemRoster(roster, reason, out var rosterRepaired, recordsByObjectId))
			{
				repaired += rosterRepaired;
			}
		}
		if (repaired > 0)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_roster_repaired reason=" + (reason ?? "") + " scanned=" + scanned + " repaired=" + repaired);
			}
			catch
			{
			}
		}
	}

	private bool RepairGeneratedRewardItemRoster(ItemRoster roster, string reason, out int repaired, Dictionary<uint, GeneratedRewardItemRecord> recordsByObjectId = null)
	{
		repaired = 0;
		if (roster == null)
		{
			return false;
		}
		EnsureGeneratedRewardItemData();
		recordsByObjectId ??= BuildGeneratedRewardItemRecordsByObjectId();
		if (recordsByObjectId.Count == 0)
		{
			return false;
		}
		List<Tuple<ItemRosterElement, ItemObject, GeneratedRewardItemRecord>> replacements = new List<Tuple<ItemRosterElement, ItemObject, GeneratedRewardItemRecord>>();
		for (int i = 0; i < roster.Count; i++)
		{
			ItemRosterElement element = roster.GetElementCopyAtIndex(i);
			ItemObject currentItem = element.EquipmentElement.Item;
			if (currentItem == null || element.Amount <= 0)
			{
				continue;
			}
			GeneratedRewardItemRecord record = null;
			uint currentObjectId = currentItem.Id.InternalValue;
			if (currentObjectId != 0u)
			{
				recordsByObjectId.TryGetValue(currentObjectId, out record);
			}
			if (record == null && IsGeneratedRewardItemStringId(currentItem.StringId))
			{
				record = GetGeneratedRewardItemRecord(currentItem.StringId);
			}
			if (record == null || string.IsNullOrWhiteSpace(record.GeneratedStringId))
			{
				continue;
			}
			string previousTemplateStringId = (record.TemplateStringId ?? "").Trim();
			ItemObject templateItem = ResolveGeneratedRewardRecordTemplateItem(record, (reason ?? "") + "_roster_guard");
			if (templateItem == null)
			{
				continue;
			}
			record.TemplateStringId = (templateItem.StringId ?? "").Trim();
			bool alreadyCorrect = IsGeneratedRewardRosterItemCanonical(currentItem, record);
			if (alreadyCorrect)
			{
				ApplyGeneratedRewardItemRpState(currentItem, record.DisplayName);
				TryEnsureGeneratedRewardItemCategory(currentItem, templateItem, reason);
				if (!string.Equals(previousTemplateStringId, record.TemplateStringId, StringComparison.OrdinalIgnoreCase))
				{
					RememberGeneratedRewardItemRecord(record.GeneratedStringId, record.DisplayName, templateItem, currentItem);
				}
				continue;
			}
			ItemObject generatedItem = TryGetOrCreateGeneratedRewardItem(record.GeneratedStringId, record.DisplayName, templateItem, reason);
			if (generatedItem == null)
			{
				continue;
			}
			if (ReferenceEquals(currentItem, generatedItem))
			{
				ApplyGeneratedRewardItemRpState(generatedItem, record.DisplayName);
				generatedItem.IsReady = true;
				TryEnsureGeneratedRewardItemCategory(generatedItem, templateItem, reason);
				if (!IsGeneratedRewardRosterItemCanonical(generatedItem, record))
				{
					LogGeneratedRewardInventoryGuard("repair_same_reference_not_canonical", record.GeneratedStringId, record.DisplayName, record.TemplateStringId, generatedItem, templateItem, reason);
				}
				continue;
			}
			replacements.Add(Tuple.Create(element, generatedItem, record));
		}
		foreach (Tuple<ItemRosterElement, ItemObject, GeneratedRewardItemRecord> replacement in replacements)
		{
			ItemRosterElement oldElement = replacement.Item1;
			ItemObject generatedItem = replacement.Item2;
			if (oldElement.Amount <= 0 || generatedItem == null)
			{
				continue;
			}
			EquipmentElement oldEquipment = oldElement.EquipmentElement;
			generatedItem.Initialize();
			generatedItem.IsReady = true;
			TryEnsureGeneratedRewardItemCategory(generatedItem, ResolveItemById(replacement.Item3.TemplateStringId), reason);
			EquipmentElement newEquipment = new EquipmentElement(generatedItem, oldEquipment.ItemModifier, oldEquipment.CosmeticItem, oldEquipment.IsQuestItem);
			int addedIndex = roster.AddToCounts(newEquipment, oldElement.Amount);
			if (addedIndex >= 0)
			{
				roster.AddToCounts(oldEquipment, -oldElement.Amount);
				repaired += oldElement.Amount;
				ItemObject replacedItem = oldEquipment.Item;
				if (replacedItem != null && replacedItem.Id.InternalValue != 0u)
				{
					lock (GeneratedRewardItemRegistrationLock)
					{
						if (GeneratedRewardPendingItemsByObjectId.TryGetValue(
								replacedItem.Id.InternalValue,
								out ItemObject pendingAtId)
							&& ReferenceEquals(pendingAtId, replacedItem))
						{
							GeneratedRewardPendingItemsByObjectId.Remove(
								replacedItem.Id.InternalValue);
						}
					}
				}
				RememberGeneratedRewardItemRecord(replacement.Item3.GeneratedStringId, replacement.Item3.DisplayName, ResolveItemById(replacement.Item3.TemplateStringId), generatedItem);
			}
		}
		return repaired > 0;
	}

	private static bool IsGeneratedRewardRosterItemCanonical(ItemObject item, GeneratedRewardItemRecord record)
	{
		if (item == null || record == null || !IsGeneratedRewardItemStringId(record.GeneratedStringId))
		{
			return false;
		}
		if (!string.Equals((item.StringId ?? "").Trim(), record.GeneratedStringId.Trim(), StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (!item.IsReady)
		{
			return false;
		}
		if (!HasExpectedPlayerRpCraftItemValue(item, record))
		{
			return false;
		}
		if (!HasCloneSafeGeneratedRewardThumbnailSource(item))
		{
			return false;
		}
		if (item.ItemComponent is GeneratedRewardRpItemComponent)
		{
			return false;
		}
		ItemObject templateItem = ResolveItemById(record.TemplateStringId);
		if (templateItem != null && item.Type != templateItem.Type)
		{
			return false;
		}
		PlayerRpCraftItemStatsSnapshot playerSnapshot =
			string.Equals(record.PlayerCraft?.CraftKind, "remnant", StringComparison.OrdinalIgnoreCase)
				? null
				: record.PlayerCraft?.StatsSnapshot;
		if (string.Equals(record.PlayerCraft?.CraftKind, "equipment", StringComparison.OrdinalIgnoreCase)
			&& playerSnapshot == null)
		{
			return false;
		}
		if (templateItem != null && playerSnapshot == null && !ReferenceEquals(item.ItemComponent, templateItem.ItemComponent))
		{
			return false;
		}
		if (templateItem != null && playerSnapshot != null
			&& (ReferenceEquals(item.ItemComponent, templateItem.ItemComponent)
				|| !PlayerRpCraftItemComponentService.IsStructurallyCompatible(templateItem, playerSnapshot)
				|| !PlayerRpCraftItemComponentService.MatchesSnapshot(item, playerSnapshot)))
		{
			return false;
		}
		if (templateItem != null && !string.Equals(item.MultiMeshName ?? "", templateItem.MultiMeshName ?? "", StringComparison.Ordinal))
		{
			return false;
		}
		if (templateItem != null && !string.Equals(item.HolsterMeshName ?? "", templateItem.HolsterMeshName ?? "", StringComparison.Ordinal))
		{
			return false;
		}
		if (templateItem?.ItemCategory != null && !ReferenceEquals(item.ItemCategory, templateItem.ItemCategory))
		{
			return false;
		}
		return IsGeneratedRewardItemVisibleToObjectManager(item);
	}

	private static bool IsGeneratedRewardItemVisibleToObjectManager(ItemObject item)
	{
		if (item == null || !IsGeneratedRewardItemStringId(item.StringId))
		{
			return false;
		}
		bool previousSuppressObjectLookup = SuppressGeneratedRewardObjectLookup;
		bool previousSuppressPendingLookup = SuppressGeneratedRewardPendingLookup;
		try
		{
			SuppressGeneratedRewardObjectLookup = true;
			SuppressGeneratedRewardPendingLookup = true;
			ItemObject stringItem = MBObjectManager.Instance?.GetObject<ItemObject>(item.StringId);
			if (!ReferenceEquals(stringItem, item))
			{
				return false;
			}
			ItemObject idItem = item.Id.InternalValue != 0u ? MBObjectManager.Instance?.GetObject(item.Id) as ItemObject : null;
			return item.Id.InternalValue == 0u || ReferenceEquals(idItem, item);
		}
		catch
		{
			return false;
		}
		finally
		{
			SuppressGeneratedRewardObjectLookup = previousSuppressObjectLookup;
			SuppressGeneratedRewardPendingLookup = previousSuppressPendingLookup;
		}
	}

	private Dictionary<uint, GeneratedRewardItemRecord> BuildGeneratedRewardItemRecordsByObjectId()
	{
		MergeGeneratedRewardManifestIntoRecords();
		Dictionary<uint, GeneratedRewardItemRecord> recordsByObjectId = new Dictionary<uint, GeneratedRewardItemRecord>();
		foreach (KeyValuePair<string, GeneratedRewardItemRecord> pair in _generatedRewardItemRecords.ToList())
		{
			GeneratedRewardItemRecord record = NormalizeGeneratedRewardItemRecord(pair.Key, pair.Value);
			if (record == null)
			{
				continue;
			}
			_generatedRewardItemRecords[record.GeneratedStringId] = record;
			if (record.ObjectId != 0u && !recordsByObjectId.ContainsKey(record.ObjectId))
			{
				recordsByObjectId[record.ObjectId] = record;
			}
			foreach (uint legacyObjectId in record.LegacyObjectIds ?? new List<uint>())
			{
				if (legacyObjectId != 0u && !recordsByObjectId.ContainsKey(legacyObjectId))
				{
					recordsByObjectId[legacyObjectId] = record;
				}
			}
		}
		return recordsByObjectId;
	}

	private static IEnumerable<ItemRoster> EnumerateGeneratedRewardCandidateRosters()
	{
		ItemRoster mainRoster = null;
		try
		{
			mainRoster = PartyBase.MainParty?.ItemRoster;
		}
		catch
		{
		}
		if (mainRoster != null)
		{
			yield return mainRoster;
		}
		IEnumerable<MobileParty> mobileParties = null;
		try
		{
			mobileParties = Campaign.Current?.MobileParties;
		}
		catch
		{
		}
		foreach (MobileParty mobileParty in mobileParties ?? Enumerable.Empty<MobileParty>())
		{
			ItemRoster roster = null;
			try
			{
				roster = mobileParty?.ItemRoster;
			}
			catch
			{
			}
			if (roster != null)
			{
				yield return roster;
			}
		}
		IEnumerable<Settlement> settlements = null;
		try
		{
			settlements = Campaign.Current?.Settlements;
		}
		catch
		{
		}
		foreach (Settlement settlement in settlements ?? Enumerable.Empty<Settlement>())
		{
			ItemRoster stash = null;
			try
			{
				stash = settlement?.Stash;
			}
			catch
			{
			}
			if (stash != null)
			{
				yield return stash;
			}
		}
	}

	private static bool TryGetGeneratedRewardItemId(string generatedStringId, ItemObject templateItem, uint preferredObjectId, out MBGUID objectId, string logSource = null)
	{
		objectId = default(MBGUID);
		MBObjectManager objectManager = MBObjectManager.Instance;
		if (string.IsNullOrWhiteSpace(generatedStringId) || templateItem == null || objectManager == null)
		{
			return false;
		}
		if (preferredObjectId != 0u)
		{
			MBGUID preferredId = new MBGUID(preferredObjectId);
			if (IsGeneratedRewardItemIdUsable(preferredId, generatedStringId))
			{
				objectId = preferredId;
				return true;
			}
			MBObjectBase preferredExisting = objectManager.GetObject(preferredId);
			if (IsGeneratedRewardPendingItem(preferredExisting as ItemObject))
			{
				objectId = preferredId;
				return true;
			}
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_id_collision source=" + (logSource ?? "") + " generated=" + generatedStringId + " preferred=" + preferredObjectId.ToString(CultureInfo.InvariantCulture) + " collision=" + (preferredExisting?.StringId ?? "null"));
			}
			catch
			{
			}
		}
		if (templateItem.Id.InternalValue == 0u)
		{
			return false;
		}
		uint typeIndex = templateItem.Id.GetTypeIndex();
		for (int i = 0; i < 128; i++)
		{
			string key = "generated_reward_object_id|" + generatedStringId.Trim().ToLowerInvariant() + "|" + i.ToString(CultureInfo.InvariantCulture);
			if (!uint.TryParse(StablePromptKeyHash(key), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hash))
			{
				continue;
			}
			uint subId = (hash & 0x01ffffffu) | GeneratedRewardReservedSubIdBits;
			if (subId == 0u || subId > 0x03ffffffu)
			{
				continue;
			}
			MBGUID candidate = new MBGUID(typeIndex, subId);
			if (IsGeneratedRewardItemIdUsable(candidate, generatedStringId))
			{
				objectId = candidate;
				return true;
			}
		}
		return false;
	}

	private static bool IsGeneratedRewardItemIdUsable(MBGUID objectId, string generatedStringId)
	{
		if (objectId.InternalValue == 0u || string.IsNullOrWhiteSpace(generatedStringId))
		{
			return false;
		}
		try
		{
			bool previousSuppressObjectLookup = SuppressGeneratedRewardObjectLookup;
			bool previousSuppressPendingLookup = SuppressGeneratedRewardPendingLookup;
			SuppressGeneratedRewardObjectLookup = true;
			SuppressGeneratedRewardPendingLookup = true;
			MBObjectBase existing;
			try
			{
				existing = MBObjectManager.Instance?.GetObject(objectId);
			}
			finally
			{
				SuppressGeneratedRewardObjectLookup = previousSuppressObjectLookup;
				SuppressGeneratedRewardPendingLookup = previousSuppressPendingLookup;
			}
			return existing == null || string.Equals((existing.StringId ?? "").Trim(), generatedStringId.Trim(), StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	private static ItemObject TryRegisterGeneratedRewardItemWithStableId(ItemObject generatedItem, string logSource = null)
	{
		if (generatedItem == null || generatedItem.Id.InternalValue == 0u || RewardObjectManagerTryRegisterWithoutInitializationMethod == null || MBObjectManager.Instance == null)
		{
			return null;
		}
		bool previousSuppressObjectLookup = SuppressGeneratedRewardObjectLookup;
		bool previousSuppressPendingLookup = SuppressGeneratedRewardPendingLookup;
		try
		{
			SuppressGeneratedRewardObjectLookup = true;
			SuppressGeneratedRewardPendingLookup = true;
			MBObjectBase idCollision = MBObjectManager.Instance.GetObject(generatedItem.Id);
			if (idCollision != null)
			{
				if (!string.Equals((idCollision.StringId ?? "").Trim(), (generatedItem.StringId ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
				{
					return null;
				}
				ItemObject stringCollision = MBObjectManager.Instance.GetObject<ItemObject>(generatedItem.StringId);
				if (stringCollision != null && string.Equals((stringCollision.StringId ?? "").Trim(), (generatedItem.StringId ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
				{
					return stringCollision;
				}
				ItemObject idCollisionItem = idCollision as ItemObject;
				if (idCollisionItem != null)
				{
					return idCollisionItem;
				}
				try
				{
					Logger.Log("Logic", "[RewardItemResolve] generated_stable_collision_missing_string_index source=" + (logSource ?? "") + " generated=" + (generatedItem.StringId ?? "") + " id=" + generatedItem.Id.InternalValue.ToString(CultureInfo.InvariantCulture));
				}
				catch
				{
				}
				return null;
			}
			generatedItem.Initialize();
			RewardObjectManagerTryRegisterWithoutInitializationMethod.Invoke(MBObjectManager.Instance, new object[1] { generatedItem });
			generatedItem.OnRegistered();
			generatedItem.AfterInitialized();
			ItemObject registeredByString = MBObjectManager.Instance.GetObject<ItemObject>(generatedItem.StringId);
			if (registeredByString != null && string.Equals((registeredByString.StringId ?? "").Trim(), (generatedItem.StringId ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
			{
				return registeredByString;
			}
			ItemObject registeredById = MBObjectManager.Instance.GetObject(generatedItem.Id) as ItemObject;
			if (registeredById != null && string.Equals((registeredById.StringId ?? "").Trim(), (generatedItem.StringId ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
			{
				try
				{
					Logger.Log("Logic", "[RewardItemResolve] generated_stable_register_missing_string_index source=" + (logSource ?? "") + " generated=" + (generatedItem.StringId ?? "") + " id=" + generatedItem.Id.InternalValue.ToString(CultureInfo.InvariantCulture));
				}
				catch
				{
				}
				return registeredById;
			}
			return null;
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_stable_register_failed source=" + (logSource ?? "") + " generated=" + (generatedItem.StringId ?? "") + " id=" + generatedItem.Id.InternalValue.ToString(CultureInfo.InvariantCulture) + " error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
			return null;
		}
		finally
		{
			SuppressGeneratedRewardObjectLookup = previousSuppressObjectLookup;
			SuppressGeneratedRewardPendingLookup = previousSuppressPendingLookup;
		}
	}

	private static ItemObject TryGetOrCreateGeneratedRewardItem(string generatedStringId, string displayName, ItemObject templateItem, string logSource = null)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(generatedStringId) || string.IsNullOrWhiteSpace(displayName))
			{
				return null;
			}
			string key = generatedStringId.Trim();
			string name = displayName.Trim();
			GeneratedRewardItemRecord playerCraftRecord = Instance?.GetGeneratedRewardItemRecord(key);
			templateItem = playerCraftRecord?.PlayerCraft != null
				? ResolveGeneratedRewardRecordTemplateItem(playerCraftRecord, logSource ?? "generated_create_player_template_guard")
				: (IsAuthorizedPlayerRpCraftGenerationKey(key)
					&& IsSafePlayerRpCraftGenerationTemplate(templateItem)
						? templateItem
						: ResolveCloneSafeGeneratedRewardTemplateItem(
							templateItem,
							name,
							logSource ?? "generated_create_template_guard",
							key));
			if (templateItem == null)
			{
				return null;
			}
			if (TryResolveGeneratedRewardItemForStringId(key, out var cachedGeneratedItem, logSource ?? "generated_create_cached") && cachedGeneratedItem != null)
			{
				if (!ApplyGeneratedRewardItemTemplateState(cachedGeneratedItem, templateItem, name))
				{
					return null;
				}
				ApplyGeneratedRewardItemRpState(cachedGeneratedItem, name);
				TryEnsureGeneratedRewardItemCategory(cachedGeneratedItem, templateItem, logSource);
				ItemObject registeredCachedItem = TryRegisterGeneratedRewardItemWithStableId(cachedGeneratedItem, logSource ?? "generated_create_cached_promote");
				if (registeredCachedItem != null)
				{
					if (!ApplyGeneratedRewardItemTemplateState(registeredCachedItem, templateItem, name))
					{
						return null;
					}
					ApplyGeneratedRewardItemRpState(registeredCachedItem, name);
					TryEnsureGeneratedRewardItemCategory(registeredCachedItem, templateItem, logSource);
					registeredCachedItem.Initialize();
					registeredCachedItem.IsReady = true;
					Instance?.RememberGeneratedRewardItemRecord(key, name, templateItem, registeredCachedItem);
					return registeredCachedItem;
				}
				LogGeneratedRewardInventoryGuard("cached_register_failed", key, name, templateItem.StringId, cachedGeneratedItem, templateItem, logSource);
			}
			ItemObject existing = TryGetRegisteredGeneratedRewardItemByStringId(key);
			if (existing != null)
			{
				if (!ApplyGeneratedRewardItemTemplateState(existing, templateItem, name))
				{
					return null;
				}
				ApplyGeneratedRewardItemRpState(existing, name);
				TryEnsureGeneratedRewardItemCategory(existing, templateItem, logSource);
				existing.Initialize();
				existing.IsReady = true;
				Instance?.RememberGeneratedRewardItemRecord(key, name, templateItem, existing);
				return existing;
			}
			GeneratedRewardItemRecord record = Instance?.GetGeneratedRewardItemRecord(key);
			if (record == null)
			{
				EnsureGeneratedRewardManifestLoaded();
				lock (GeneratedRewardItemRegistrationLock)
				{
					GeneratedRewardManifestByStringId.TryGetValue(key, out record);
				}
			}
			record ??= new GeneratedRewardItemRecord();
			record.GeneratedStringId = key;
			record.DisplayName = name;
			record.TemplateStringId = (templateItem.StringId ?? "").Trim();
			if (record.LegacyObjectIds == null)
			{
				record.LegacyObjectIds = new List<uint>();
			}
			uint preferredObjectId = record.ObjectId;
			if (preferredObjectId == 0u && TryGetGeneratedRewardItemId(key, templateItem, 0u, out var generatedObjectId, logSource))
			{
				record.ObjectId = generatedObjectId.InternalValue;
			}
			if (record.ObjectId != 0u)
			{
				ItemObject detachedItem = TryGetOrCreateGeneratedRewardDetachedItem(record, logSource);
				if (detachedItem != null)
				{
					ApplyGeneratedRewardItemRpState(detachedItem, name);
					ItemObject registeredDetachedItem = TryRegisterGeneratedRewardItemWithStableId(detachedItem, logSource ?? "generated_create_detached_promote");
					if (registeredDetachedItem != null)
					{
						ApplyGeneratedRewardItemRpState(registeredDetachedItem, name);
						TryEnsureGeneratedRewardItemCategory(registeredDetachedItem, templateItem, logSource);
						registeredDetachedItem.Initialize();
						registeredDetachedItem.IsReady = true;
						Instance?.RememberGeneratedRewardItemRecord(key, name, templateItem, registeredDetachedItem);
						return registeredDetachedItem;
					}
					LogGeneratedRewardInventoryGuard("detached_register_failed", key, name, templateItem.StringId, detachedItem, templateItem, logSource);
				}
			}
			ItemObject generatedItem = new ItemObject(templateItem)
			{
				StringId = key
			};
			if (!ApplyGeneratedRewardItemTemplateState(generatedItem, templateItem, name))
			{
				return null;
			}
			if (!TryEnsureGeneratedRewardItemCategory(generatedItem, templateItem, logSource))
			{
				return null;
			}
			ItemObject registered = MBObjectManager.Instance?.RegisterObject<ItemObject>(generatedItem) ?? generatedItem;
			if (!ApplyGeneratedRewardItemTemplateState(registered, templateItem, name))
			{
				return null;
			}
			ApplyGeneratedRewardItemRpState(registered, name);
			TryEnsureGeneratedRewardItemCategory(registered, templateItem, logSource);
			registered.Initialize();
			registered.IsReady = true;
			Instance?.RememberGeneratedRewardItemRecord(key, name, templateItem, registered);
			LogGeneratedRewardObjectVisibility("generated_create_registered", registered, logSource);
			return registered;
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_low_score_failed source=" + (logSource ?? "") + " generated=" + (generatedStringId ?? "") + " templateStringId=" + (templateItem?.StringId ?? "") + " error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
			return null;
		}
	}

	private static bool TrySetRewardItemObjectName(ItemObject item, string displayName)
	{
		if (item == null || string.IsNullOrWhiteSpace(displayName))
		{
			return false;
		}
		try
		{
			RewardItemObjectNameProperty?.SetValue(item, new TextObject("{=!}" + displayName.Trim()), null);
			return item.Name != null && string.Equals(item.Name.ToString(), displayName.Trim(), StringComparison.Ordinal);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsRuntimeGeneratedRewardItemForKey(ItemObject item, string generatedStringId)
	{
		string key = (generatedStringId ?? "").Trim();
		string itemStringId = (item?.StringId ?? "").Trim();
		return item != null && IsGeneratedRewardItemStringId(key) && IsGeneratedRewardItemStringId(itemStringId) && string.Equals(itemStringId, key, StringComparison.OrdinalIgnoreCase);
	}

	private static void DiscardGeneratedRewardRecordObjectIdIfPolluted(GeneratedRewardItemRecord record, ItemObject templateItem, string logSource = null)
	{
		if (record == null || record.ObjectId == 0u)
		{
			return;
		}
		uint objectId = record.ObjectId;
		string reason = null;
		ItemObject collisionItem = null;
		if (templateItem != null && templateItem.Id.InternalValue != 0u && objectId == templateItem.Id.InternalValue)
		{
			reason = "record_object_id_is_template";
			collisionItem = templateItem;
		}
		else if (MBObjectManager.Instance != null)
		{
			bool previousSuppressObjectLookup = SuppressGeneratedRewardObjectLookup;
			bool previousSuppressPendingLookup = SuppressGeneratedRewardPendingLookup;
			try
			{
				SuppressGeneratedRewardObjectLookup = true;
				SuppressGeneratedRewardPendingLookup = true;
				collisionItem = MBObjectManager.Instance.GetObject(new MBGUID(objectId)) as ItemObject;
			}
			catch
			{
				collisionItem = null;
			}
			finally
			{
				SuppressGeneratedRewardObjectLookup = previousSuppressObjectLookup;
				SuppressGeneratedRewardPendingLookup = previousSuppressPendingLookup;
			}
			if (collisionItem != null && !IsGeneratedRewardPendingItem(collisionItem) && !IsRuntimeGeneratedRewardItemForKey(collisionItem, record.GeneratedStringId))
			{
				reason = "record_object_id_collision";
			}
		}
		if (reason == null)
		{
			return;
		}
		LogGeneratedRewardInventoryGuard(reason, record.GeneratedStringId, record.DisplayName, record.TemplateStringId, collisionItem, templateItem, logSource);
		record.ObjectId = 0u;
		if (record.LegacyObjectIds != null)
		{
			record.LegacyObjectIds.RemoveAll((uint x) => x == objectId);
		}
		lock (GeneratedRewardItemRegistrationLock)
		{
			GeneratedRewardDetachedItemsByObjectId.Remove(objectId);
			GeneratedRewardManifestByObjectId.Remove(objectId);
		}
	}

	private static bool TryValidateGeneratedRewardInventoryItemForRoster(ItemObject generatedItem, string generatedStringId, string displayName, ItemObject templateItem, string logSource = null)
	{
		string key = (generatedStringId ?? "").Trim();
		string name = NormalizeGeneratedInventoryDisplayName(displayName);
		if (!IsGeneratedRewardItemStringId(key) || string.IsNullOrWhiteSpace(name))
		{
			LogGeneratedRewardInventoryGuard("invalid_expected", key, name, templateItem?.StringId, generatedItem, templateItem, logSource);
			return false;
		}
		if (!IsRuntimeGeneratedRewardItemForKey(generatedItem, key))
		{
			LogGeneratedRewardInventoryGuard("wrong_runtime_item", key, name, templateItem?.StringId, generatedItem, templateItem, logSource);
			return false;
		}
		if (!HasCloneSafeGeneratedRewardThumbnailSource(generatedItem))
		{
			LogGeneratedRewardInventoryGuard("thumbnail_source_unsafe", key, name, templateItem?.StringId, generatedItem, templateItem, logSource);
			return false;
		}
		if (templateItem != null && (ReferenceEquals(generatedItem, templateItem) || (generatedItem.Id.InternalValue != 0u && generatedItem.Id.InternalValue == templateItem.Id.InternalValue)))
		{
			LogGeneratedRewardInventoryGuard("template_leak", key, name, templateItem.StringId, generatedItem, templateItem, logSource);
			return false;
		}
		if (!TrySetRewardItemObjectName(generatedItem, name))
		{
			LogGeneratedRewardInventoryGuard("name_set_failed", key, name, templateItem?.StringId, generatedItem, templateItem, logSource);
			return false;
		}
		if (!TryEnsureGeneratedRewardItemCategory(generatedItem, templateItem, logSource ?? "generated_inventory_guard"))
		{
			LogGeneratedRewardInventoryGuard("category_failed", key, name, templateItem?.StringId, generatedItem, templateItem, logSource);
			return false;
		}
		GeneratedRewardItemRecord record =
			Instance?.GetGeneratedRewardItemRecord(key);
		if (record == null)
		{
			EnsureGeneratedRewardManifestLoaded();
			lock (GeneratedRewardItemRegistrationLock)
			{
				GeneratedRewardManifestByStringId.TryGetValue(key, out record);
			}
		}
		if (!HasExpectedPlayerRpCraftItemValue(generatedItem, record))
		{
			LogGeneratedRewardInventoryGuard(
				"player_craft_value_mismatch",
				key,
				name,
				templateItem?.StringId,
				generatedItem,
				templateItem,
				logSource);
			return false;
		}
		try
		{
			generatedItem.Initialize();
			generatedItem.IsReady = true;
		}
		catch
		{
		}
		return true;
	}

	private static void LogGeneratedRewardInventoryGuard(string reason, string generatedStringId, string displayName, string templateStringId, ItemObject actualItem, ItemObject templateItem, string logSource = null)
	{
		try
		{
			Logger.Log("Logic", "[RewardItemResolve] generated_inventory_guard reason=" + (reason ?? "") + " source=" + (logSource ?? "") + " expected=" + (generatedStringId ?? "") + " display=" + FormatGeneratedRewardNameForLog(generatedStringId, displayName) + " template=" + (templateStringId ?? templateItem?.StringId ?? "") + " actualStringId=" + (actualItem?.StringId ?? "") + " actualName=" + FormatGeneratedRewardNameForLog(actualItem?.StringId, actualItem?.Name?.ToString()) + " actualId=" + (actualItem != null ? actualItem.Id.InternalValue.ToString(CultureInfo.InvariantCulture) : "0") + " templateName=" + (templateItem?.Name?.ToString() ?? "") + " templateId=" + (templateItem != null ? templateItem.Id.InternalValue.ToString(CultureInfo.InvariantCulture) : "0"));
		}
		catch
		{
		}
	}

	private static string FormatGeneratedRewardNameForLog(string generatedStringId, string displayName)
	{
		string value = displayName ?? "";
		if (!IsExactPlayerRpGeneratedTransactionKey(generatedStringId))
		{
			return value;
		}
		return "[hash="
			+ StablePromptKeyHash(value)
			+ ";length="
			+ value.Length.ToString(CultureInfo.InvariantCulture)
			+ "]";
	}

	private static void LogGeneratedRewardObjectVisibility(string reason, ItemObject item, string logSource = null)
	{
		if (item == null || !IsGeneratedRewardItemStringId(item.StringId))
		{
			return;
		}
		try
		{
			bool byString = false;
			bool byId = false;
			bool previousSuppressObjectLookup = SuppressGeneratedRewardObjectLookup;
			bool previousSuppressPendingLookup = SuppressGeneratedRewardPendingLookup;
			try
			{
				SuppressGeneratedRewardObjectLookup = true;
				SuppressGeneratedRewardPendingLookup = true;
				ItemObject stringItem = MBObjectManager.Instance?.GetObject<ItemObject>(item.StringId);
				byString = stringItem != null && string.Equals((stringItem.StringId ?? "").Trim(), (item.StringId ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
				ItemObject idItem = item.Id.InternalValue != 0u ? MBObjectManager.Instance?.GetObject(item.Id) as ItemObject : null;
				byId = idItem != null && string.Equals((idItem.StringId ?? "").Trim(), (item.StringId ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
			}
			finally
			{
				SuppressGeneratedRewardObjectLookup = previousSuppressObjectLookup;
				SuppressGeneratedRewardPendingLookup = previousSuppressPendingLookup;
			}
			Logger.Log("Logic", "[RewardItemResolve] generated_object_visibility reason=" + (reason ?? "") + " source=" + (logSource ?? "") + " item=" + (item.StringId ?? "") + " name=" + FormatGeneratedRewardNameForLog(item.StringId, item.Name?.ToString()) + " id=" + item.Id.InternalValue.ToString(CultureInfo.InvariantCulture) + " byString=" + byString + " byId=" + byId + " ready=" + item.IsReady + " initialized=" + item.IsInitialized + " type=" + item.Type + " category=" + (item.ItemCategory?.StringId ?? "null") + " component=" + (item.ItemComponent?.GetType().Name ?? "null") + " value=" + item.Value.ToString(CultureInfo.InvariantCulture) + " weight=" + item.Weight.ToString(CultureInfo.InvariantCulture));
		}
		catch
		{
		}
	}

	private static void SPInventoryVMRefreshInformationValuesPostfix(object __instance)
	{
		try
		{
			if (__instance == null)
			{
				return;
			}
			Type type = __instance.GetType();
			object rightList = type.GetProperty("RightItemListVM", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(__instance, null);
			if (!(rightList is System.Collections.IEnumerable enumerable))
			{
				return;
			}
			List<string> sample = new List<string>();
			int generatedCount = 0;
			foreach (object itemVm in enumerable)
			{
				if (itemVm == null)
				{
					continue;
				}
				PropertyInfo rosterElementProperty = itemVm.GetType().GetProperty("ItemRosterElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (rosterElementProperty == null)
				{
					continue;
				}
				ItemRosterElement element = (ItemRosterElement)rosterElementProperty.GetValue(itemVm, null);
				ItemObject item = element.EquipmentElement.Item;
				if (item == null || !IsGeneratedRewardItemStringId(item.StringId))
				{
					continue;
				}
				generatedCount++;
				if (sample.Count < 10)
				{
					bool isFiltered = false;
					try
					{
						object filteredValue = itemVm.GetType().GetProperty("IsFiltered", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(itemVm, null);
						if (filteredValue is bool filtered)
						{
							isFiltered = filtered;
						}
					}
					catch
					{
					}
					int typeId = -1;
					try
					{
						object typeIdValue = itemVm.GetType().GetProperty("TypeId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(itemVm, null);
						if (typeIdValue is int id)
						{
							typeId = id;
						}
					}
					catch
					{
					}
					sample.Add((item.StringId ?? "") + ":" + element.Amount.ToString(CultureInfo.InvariantCulture) + ":filtered=" + isFiltered + ":typeId=" + typeId.ToString(CultureInfo.InvariantCulture) + ":name=" + FormatGeneratedRewardNameForLog(item.StringId, item.Name?.ToString()));
				}
			}
			if (generatedCount <= 0)
			{
				return;
			}
			string signature = generatedCount.ToString(CultureInfo.InvariantCulture) + "|" + string.Join(",", sample);
			DateTime now = DateTime.UtcNow;
			if (string.Equals(signature, GeneratedRewardLastInventoryVmLogSignature, StringComparison.Ordinal) && (now - GeneratedRewardLastInventoryVmLogUtc).TotalSeconds < 5.0)
			{
				return;
			}
			GeneratedRewardLastInventoryVmLogSignature = signature;
			GeneratedRewardLastInventoryVmLogUtc = now;
			Logger.Log("Logic", "[RewardItemResolve] inventory_vm_generated_items count=" + generatedCount.ToString(CultureInfo.InvariantCulture) + " sample=" + string.Join(",", sample));
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] inventory_vm_generated_items_failed error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
		}
	}

	private static void ItemMenuVMRefreshItemTooltipsPostfix(object __instance, object item)
	{
		try
		{
			if (__instance == null || item == null)
			{
				return;
			}
			PropertyInfo rosterElementProperty = item.GetType().GetProperty("ItemRosterElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (rosterElementProperty == null)
			{
				return;
			}
			ItemRosterElement element = (ItemRosterElement)rosterElementProperty.GetValue(item, null);
			ItemObject itemObject = element.EquipmentElement.Item;
			if (itemObject == null || !IsGeneratedRewardItemStringId(itemObject.StringId))
			{
				return;
			}
			string text = Instance?.GetGeneratedRewardItemRecord(itemObject.StringId)?.DisplayName;
			if (string.IsNullOrWhiteSpace(text))
			{
				text = itemObject.Name?.ToString();
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			MethodInfo createProperty = __instance.GetType().GetMethod("CreateProperty", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			PropertyInfo targetItemPropertiesProperty = __instance.GetType().GetProperty("TargetItemProperties", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			object targetItemProperties = targetItemPropertiesProperty?.GetValue(__instance, null);
			if (createProperty == null || targetItemProperties == null)
			{
				return;
			}
			createProperty.Invoke(__instance, new object[5] { targetItemProperties, "", text, 0, null });
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("Logic", "[RewardItemResolve] generated_tooltip_failed error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
		}
	}

	private static bool SPInventoryVMProcessPreviewItemPrefix(ItemVM item)
	{
		try
		{
			ItemObject itemObject = item?.ItemRosterElement.EquipmentElement.Item;
			if (itemObject == null)
			{
				return true;
			}
			if (CourierDeliveryBehavior.TryGetCourierLetterInventoryDetailForExternal(itemObject.StringId, itemObject.Id.InternalValue, out string letterBody))
			{
				string title = itemObject.Name?.ToString();
				if (string.IsNullOrWhiteSpace(title))
				{
					title = "信件";
				}
				if (CourierLetterReplyPopup.Show("查看信件", title.Trim(), letterBody, null, "关闭"))
				{
					return false;
				}
				return true;
			}
			if (TryGetGeneratedRpItemIntroductionDetailForExternal(itemObject.StringId, itemObject.Id.InternalValue, out string generatedTitle, out string introduction, out bool isPending))
			{
				string title2 = string.IsNullOrWhiteSpace(generatedTitle) ? (itemObject.Name?.ToString() ?? "RP 物品") : generatedTitle;
				string body = introduction;
				if (string.IsNullOrWhiteSpace(body))
				{
					body = isPending ? "物品介绍正在生成，请稍后再查看。" : "该 RP 物品暂无介绍。";
				}
				if (CourierLetterReplyPopup.Show("查看物品介绍", title2.Trim(), body, null, "关闭"))
				{
					return false;
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("RewardSystem", ">>> Courier letter or RP item inventory preview failed: " + ex.Message);
		}
		return true;
	}

	private static string BuildGeneratedRewardItemStringId(string requestedName, string templateStringId)
	{
		string text = ((requestedName ?? "").Trim() + "|" + (templateStringId ?? "").Trim()).ToLowerInvariant();
		return "af_generated_reward_" + StablePromptKeyHash(text);
	}

	private bool TryResolveRewardItemStringId(string lookup, IEnumerable<RewardItemInfo> contextItems, out string itemId, out ItemObject item, string logSource = null)
	{
		itemId = "";
		item = null;
		if (!TryResolveRewardItemByNameOrId(lookup, contextItems, out var resolution, logSource))
		{
			return false;
		}
		item = resolution.Item;
		itemId = resolution.MatchedStringId;
		return item != null && !string.IsNullOrWhiteSpace(itemId);
	}

	private static int GenerateResolvedItemsToRoster(ItemRoster targetRoster, RewardItemResolution resolution, int amount, out string itemName, EconomyMutationObservation mutationObservation = null)
	{
		itemName = null;
		if (targetRoster == null || resolution?.Item == null || amount <= 0)
		{
			return 0;
		}
		EquipmentElement equipmentElement = resolution.EquipmentElement.Item != null ? resolution.EquipmentElement : new EquipmentElement(resolution.Item, null, null, false);
		TryEnsureGeneratedRewardItemCategory(equipmentElement.Item, resolution.TemplateItem, "generate_to_roster");
		int generated = AddEquipmentElementToRosterAndCountDelta(
			targetRoster,
			equipmentElement,
			amount,
			"generate_to_roster:" + (resolution.MatchedStringId ?? resolution.MatchedName ?? ""),
			mutationObservation);
		itemName = IsGeneratedRewardItemStringId(equipmentElement.Item?.StringId) ? (resolution.MatchedName ?? resolution.Item.Name?.ToString() ?? resolution.MatchedStringId) : (equipmentElement.GetModifiedItemName()?.ToString() ?? resolution.MatchedName ?? resolution.Item.Name?.ToString() ?? resolution.MatchedStringId);
		return generated;
	}

	private static string GetWeaponClassTypeLabel(WeaponClass weaponClass)
	{
		switch (weaponClass)
		{
		case WeaponClass.Dagger:
			return "匕首";
		case WeaponClass.OneHandedSword:
		case WeaponClass.TwoHandedSword:
			return "剑";
		case WeaponClass.OneHandedAxe:
		case WeaponClass.TwoHandedAxe:
			return "斧";
		case WeaponClass.Mace:
		case WeaponClass.TwoHandedMace:
			return "锤";
		case WeaponClass.Pick:
			return "镐";
		case WeaponClass.OneHandedPolearm:
		case WeaponClass.TwoHandedPolearm:
		case WeaponClass.LowGripPolearm:
			return "长柄";
		case WeaponClass.Arrow:
			return "箭";
		case WeaponClass.Bolt:
			return "弩矢";
		case WeaponClass.SlingStone:
		case WeaponClass.Stone:
		case WeaponClass.Boulder:
		case WeaponClass.BallistaStone:
		case WeaponClass.BallistaBoulder:
			return "石弹";
		case WeaponClass.Cartridge:
			return "弹药";
		case WeaponClass.Bow:
			return "弓";
		case WeaponClass.Crossbow:
			return "弩";
		case WeaponClass.Sling:
			return "投石索";
		case WeaponClass.ThrowingAxe:
			return "投斧";
		case WeaponClass.ThrowingKnife:
			return "飞刀";
		case WeaponClass.Javelin:
			return "标枪";
		case WeaponClass.Pistol:
		case WeaponClass.Musket:
			return "火器";
		case WeaponClass.SmallShield:
		case WeaponClass.LargeShield:
			return "盾牌";
		case WeaponClass.Banner:
			return "旗帜";
		default:
			return "";
		}
	}

	private static string GetGoodsTypeLabel(ItemObject item)
	{
		if (item == null)
		{
			return "";
		}
		ItemCategory itemCategory = item.ItemCategory;
		if (item.IsFood)
		{
			if (itemCategory == DefaultItemCategories.Beer || itemCategory == DefaultItemCategories.Wine)
			{
				return "酒类";
			}
			return "食物";
		}
		if (item.Type == ItemObject.ItemTypeEnum.Animal)
		{
			if (itemCategory == DefaultItemCategories.PackAnimal)
			{
				return "驮兽";
			}
			return "牲畜";
		}
		if (itemCategory == DefaultItemCategories.Wood || itemCategory == DefaultItemCategories.Planks)
		{
			return "木材";
		}
		if (itemCategory == DefaultItemCategories.Iron)
		{
			return "铁料";
		}
		if (itemCategory == DefaultItemCategories.Salt)
		{
			return "盐";
		}
		if (itemCategory == DefaultItemCategories.Tools)
		{
			return "工具";
		}
		if (itemCategory == DefaultItemCategories.Clay || itemCategory == DefaultItemCategories.Pottery)
		{
			return "陶器";
		}
		if (itemCategory == DefaultItemCategories.Cloth || itemCategory == DefaultItemCategories.Linen || itemCategory == DefaultItemCategories.Velvet)
		{
			return "布料";
		}
		if (itemCategory == DefaultItemCategories.Leather || itemCategory == DefaultItemCategories.Hides || itemCategory == DefaultItemCategories.Fur || itemCategory == DefaultItemCategories.Felt || itemCategory == DefaultItemCategories.Wool || itemCategory == DefaultItemCategories.Flax || itemCategory == DefaultItemCategories.Cotton)
		{
			return "原料";
		}
		if (itemCategory == DefaultItemCategories.Oil)
		{
			return "油料";
		}
		if (itemCategory == DefaultItemCategories.Silver || itemCategory == DefaultItemCategories.Jewelry)
		{
			return "贵重品";
		}
		return "货物";
	}

	private static string GetItemPromptTypeLabel(ItemObject item)
	{
		if (item == null)
		{
			return "";
		}
		if (IsGeneratedRpWhipWeaponTemplateItem(item))
		{
			return "鞭";
		}
		try
		{
			WeaponComponentData primaryWeapon = item.PrimaryWeapon;
			if (primaryWeapon != null)
			{
				string weaponClassTypeLabel = GetWeaponClassTypeLabel(primaryWeapon.WeaponClass);
				if (!string.IsNullOrWhiteSpace(weaponClassTypeLabel))
				{
					return weaponClassTypeLabel;
				}
			}
		}
		catch
		{
		}
		switch (item.Type)
		{
		case ItemObject.ItemTypeEnum.Horse:
			return "马匹";
		case ItemObject.ItemTypeEnum.OneHandedWeapon:
			return "单手武器";
		case ItemObject.ItemTypeEnum.TwoHandedWeapon:
			return "双手武器";
		case ItemObject.ItemTypeEnum.Polearm:
			return "长柄武器";
		case ItemObject.ItemTypeEnum.Arrows:
			return "箭";
		case ItemObject.ItemTypeEnum.Bolts:
			return "弩矢";
		case ItemObject.ItemTypeEnum.SlingStones:
		case ItemObject.ItemTypeEnum.Bullets:
			return "弹药";
		case ItemObject.ItemTypeEnum.Shield:
			return "盾牌";
		case ItemObject.ItemTypeEnum.Bow:
			return "弓";
		case ItemObject.ItemTypeEnum.Crossbow:
			return "弩";
		case ItemObject.ItemTypeEnum.Sling:
			return "投石索";
		case ItemObject.ItemTypeEnum.Thrown:
			return "投掷武器";
		case ItemObject.ItemTypeEnum.Goods:
		case ItemObject.ItemTypeEnum.Animal:
			return GetGoodsTypeLabel(item);
		case ItemObject.ItemTypeEnum.HeadArmor:
			return "头盔";
		case ItemObject.ItemTypeEnum.BodyArmor:
		case ItemObject.ItemTypeEnum.ChestArmor:
			return "身甲";
		case ItemObject.ItemTypeEnum.LegArmor:
			return "腿甲";
		case ItemObject.ItemTypeEnum.HandArmor:
			return "手甲";
		case ItemObject.ItemTypeEnum.Pistol:
		case ItemObject.ItemTypeEnum.Musket:
			return "火器";
		case ItemObject.ItemTypeEnum.Book:
			return "书籍";
		case ItemObject.ItemTypeEnum.Cape:
			return "披风";
		case ItemObject.ItemTypeEnum.HorseHarness:
			return "马具";
		case ItemObject.ItemTypeEnum.Banner:
			return "旗帜";
		default:
			return "物品";
		}
	}

	public static string GetItemPromptTypeLabelForExternal(ItemObject item)
	{
		return GetItemPromptTypeLabel(item);
	}

	private static void AppendItemTypeField(StringBuilder stringBuilder, ItemObject item)
	{
		string itemPromptTypeLabel = GetItemPromptTypeLabel(item);
		stringBuilder.Append("type=").Append(string.IsNullOrWhiteSpace(itemPromptTypeLabel) ? "物品" : itemPromptTypeLabel);
	}

	private static bool TryParseSettlementMerchantPromptStringId(string promptStringId, out string itemId, out string modifierId)
	{
		itemId = "";
		modifierId = "";
		if (string.IsNullOrWhiteSpace(promptStringId))
		{
			return false;
		}
		string text = promptStringId.Trim();
		int num = text.IndexOf('@');
		if (num < 0)
		{
			itemId = text;
			return !string.IsNullOrWhiteSpace(itemId);
		}
		itemId = text.Substring(0, num).Trim();
		modifierId = text.Substring(num + 1).Trim();
		return !string.IsNullOrWhiteSpace(itemId);
	}

	private static bool MatchesSettlementMerchantPromptStringId(EquipmentElement equipmentElement, string promptStringId)
	{
		if (equipmentElement.Item == null || IsGeneratedRewardMarketExcludedItem(equipmentElement.Item) || string.IsNullOrWhiteSpace(promptStringId))
		{
			return false;
		}
		return string.Equals(BuildSettlementMerchantInventoryKey(equipmentElement), promptStringId.Trim(), StringComparison.OrdinalIgnoreCase);
	}

	private static string ResolveSettlementMerchantDisplayNameFromPromptStringId(string promptStringId)
	{
		if (!TryParseSettlementMerchantPromptStringId(promptStringId, out var itemId, out var modifierId))
		{
			return promptStringId ?? "";
		}
		ItemObject itemObject = ResolveItemById(itemId);
		if (itemObject == null)
		{
			return promptStringId ?? "";
		}
		ItemModifier itemModifier = null;
		if (!string.IsNullOrWhiteSpace(modifierId))
		{
			try
			{
				itemModifier = Game.Current?.ObjectManager?.GetObject<ItemModifier>(modifierId);
			}
			catch
			{
				itemModifier = null;
			}
		}
		EquipmentElement equipmentElement = new EquipmentElement(itemObject, itemModifier, null, isQuestItem: false);
		string text = BuildSettlementMerchantDisplayName(equipmentElement);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return itemObject.Name?.ToString() ?? itemId;
	}

	private static string GetItemQuantityUnit(ItemObject item)
	{
		if (item == null)
		{
			return "个";
		}
		ItemCategory itemCategory = item.ItemCategory;
		if (itemCategory == DefaultItemCategories.Beer || itemCategory == DefaultItemCategories.Wine)
		{
			return "桶";
		}
		if (item.IsFood)
		{
			return "斤";
		}
		switch (item.Type)
		{
		case ItemObject.ItemTypeEnum.Arrows:
		case ItemObject.ItemTypeEnum.Bolts:
		case ItemObject.ItemTypeEnum.Thrown:
		case ItemObject.ItemTypeEnum.SlingStones:
		case ItemObject.ItemTypeEnum.Bullets:
			return "袋";
		case ItemObject.ItemTypeEnum.Polearm:
			return "支";
		case ItemObject.ItemTypeEnum.OneHandedWeapon:
			return "把";
		case ItemObject.ItemTypeEnum.TwoHandedWeapon:
			return "柄";
		case ItemObject.ItemTypeEnum.HeadArmor:
		case ItemObject.ItemTypeEnum.BodyArmor:
		case ItemObject.ItemTypeEnum.LegArmor:
		case ItemObject.ItemTypeEnum.HandArmor:
		case ItemObject.ItemTypeEnum.Cape:
			return "件";
		default:
			return "个";
		}
	}

	private static string FormatItemAmount(int amount, ItemObject item, string itemName)
	{
		return $"{amount}{GetItemQuantityUnit(item)}{itemName}";
	}

	private static string BuildItemValueFactSuffixCore(ItemObject item, int amount, int unitPrice)
	{
		if (item == null || amount <= 0 || unitPrice <= 0)
		{
			return "";
		}
		int num = Math.Max(1, amount);
		int num2 = Math.Max(1, unitPrice);
		long num3 = (long)num * (long)num2;
		return $"（指导单价约 {num2} 第纳尔/{GetItemQuantityUnit(item)}，总值约 {num3} 第纳尔）";
	}

	private static int GetInventoryActualItemUnitValueCore(EquipmentElement equipmentElement)
	{
		try
		{
			int num = equipmentElement.ItemValue;
			if (num > 0)
			{
				return num;
			}
		}
		catch
		{
		}
		try
		{
			int num2 = equipmentElement.Item?.Value ?? 0;
			if (num2 > 0)
			{
				return num2;
			}
		}
		catch
		{
		}
		return 1;
	}

	private static string BuildInventoryActualItemValueFactSuffixCore(ItemObject item, int amount, int unitValue)
	{
		if (item == null || amount <= 0 || unitValue <= 0)
		{
			return "";
		}
		int num = Math.Max(1, amount);
		int num2 = Math.Max(1, unitValue);
		long num3 = (long)num * (long)num2;
		return $"（库存实际单价约 {num2} 第纳尔/{GetItemQuantityUnit(item)}，该项总值约 {num3} 第纳尔）";
	}

	public int GetInventoryActualItemUnitValueForExternal(EquipmentElement equipmentElement)
	{
		try
		{
			return GetInventoryActualItemUnitValueCore(equipmentElement);
		}
		catch
		{
			return 1;
		}
	}

	public string BuildInventoryActualItemValueFactSuffixForExternal(ItemObject item, int amount, int inventoryUnitValue)
	{
		try
		{
			return BuildInventoryActualItemValueFactSuffixCore(item, amount, inventoryUnitValue);
		}
		catch
		{
			return "";
		}
	}

	public long EstimateInventoryActualItemValueForExternal(ItemObject item, int amount, int inventoryUnitValue)
	{
		try
		{
			if (item == null || amount <= 0 || inventoryUnitValue <= 0)
			{
				return 0L;
			}
			return (long)Math.Max(1, amount) * Math.Max(1, inventoryUnitValue);
		}
		catch
		{
			return 0L;
		}
	}

	public string BuildItemValueFactSuffixForExternal(Hero hero, string itemId, int amount)
	{
		try
		{
			return BuildItemValueFactSuffixForExternal(hero, ResolveItemById(itemId), amount);
		}
		catch
		{
			return "";
		}
	}

	public string BuildItemValueFactSuffixForExternal(Hero hero, ItemObject item, int amount)
	{
		try
		{
			if (item == null || amount <= 0)
			{
				return "";
			}
			ItemGuidePriceInfo guidePriceForItemNearHero = GetGuidePriceForItemNearHero(hero ?? Hero.MainHero, item);
			return BuildItemValueFactSuffixCore(item, amount, Math.Max(1, guidePriceForItemNearHero.UnitPrice));
		}
		catch
		{
			return "";
		}
	}

	public string BuildSettlementItemValueFactSuffixForExternal(Settlement settlement, string itemId, int amount)
	{
		try
		{
			string text = (itemId ?? "").Trim();
			int num = text.IndexOf('@');
			if (num > 0)
			{
				text = text.Substring(0, num);
			}
			return BuildSettlementItemValueFactSuffixForExternal(settlement, ResolveItemById(text), amount);
		}
		catch
		{
			return "";
		}
	}

	public string BuildSettlementItemValueFactSuffixForExternal(Settlement settlement, ItemObject item, int amount)
	{
		try
		{
			if (item == null || amount <= 0)
			{
				return "";
			}
			if (settlement != null && TryGetSettlementBuyPrice(settlement, item, out var price) && price > 0)
			{
				return BuildItemValueFactSuffixCore(item, amount, price);
			}
			return BuildItemValueFactSuffixForExternal(Hero.MainHero, item, amount);
		}
		catch
		{
			return "";
		}
	}

	public long EstimateItemValueForExternal(Hero hero, string itemId, int amount)
	{
		try
		{
			return EstimateItemValueForExternal(hero, ResolveItemById(itemId), amount);
		}
		catch
		{
			return 0L;
		}
	}

	public long EstimateItemValueForExternal(Hero hero, ItemObject item, int amount)
	{
		try
		{
			if (item == null || amount <= 0)
			{
				return 0L;
			}
			ItemGuidePriceInfo guidePriceForItemNearHero = GetGuidePriceForItemNearHero(hero ?? Hero.MainHero, item);
			return (long)Math.Max(1, amount) * Math.Max(1, guidePriceForItemNearHero.UnitPrice);
		}
		catch
		{
			return 0L;
		}
	}

	public long EstimateSettlementItemValueForExternal(Settlement settlement, string itemId, int amount)
	{
		try
		{
			string text = (itemId ?? "").Trim();
			int num = text.IndexOf('@');
			if (num > 0)
			{
				text = text.Substring(0, num);
			}
			return EstimateSettlementItemValueForExternal(settlement, ResolveItemById(text), amount);
		}
		catch
		{
			return 0L;
		}
	}

	public long EstimateSettlementItemValueForExternal(Settlement settlement, ItemObject item, int amount)
	{
		try
		{
			if (item == null || amount <= 0)
			{
				return 0L;
			}
			if (settlement != null && TryGetSettlementBuyPrice(settlement, item, out var price) && price > 0)
			{
				return (long)Math.Max(1, amount) * Math.Max(1, price);
			}
			return EstimateItemValueForExternal(Hero.MainHero, item, amount);
		}
		catch
		{
			return 0L;
		}
	}

	private static int GetSettlementItemStock(Settlement settlement, string itemId)
	{
		try
		{
			if (settlement == null || string.IsNullOrWhiteSpace(itemId) || IsGeneratedRewardItemStringId(itemId))
			{
				return 0;
			}
			ItemRoster itemRoster = settlement.ItemRoster;
			if (itemRoster == null)
			{
				return 0;
			}
			int num = 0;
			for (int i = 0; i < itemRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
				ItemObject item = elementCopyAtIndex.EquipmentElement.Item;
				if (item != null && elementCopyAtIndex.Amount > 0 && !IsGeneratedRewardMarketExcludedItem(item) && string.Equals(item.StringId ?? "", itemId, StringComparison.OrdinalIgnoreCase))
				{
					num += elementCopyAtIndex.Amount;
				}
			}
			return num;
		}
		catch
		{
			return 0;
		}
	}

	private ItemGuidePriceInfo GetGuidePriceForItemNearHero(Hero hero, ItemObject item)
	{
		ItemGuidePriceInfo itemGuidePriceInfo = new ItemGuidePriceInfo
		{
			UnitPrice = Math.Max(1, item?.Value ?? 1),
			SampleCount = 0,
			ExpandedSearch = false,
			UsedNoStockFallback = false,
			UsedBaseValueFallback = true
		};
		if (item == null || IsGeneratedRewardMarketExcludedItem(item))
		{
			return itemGuidePriceInfo;
		}
		Vec2 origin;
		bool flag = TryResolveHeroMapOrigin(hero, out origin);
		List<(Settlement, float)> list = new List<(Settlement, float)>();
		try
		{
			foreach (Settlement item3 in Settlement.All)
			{
				if (item3 != null && !item3.IsHideout && item3.IsTown && item3.SettlementComponent != null)
				{
					float item2 = 0f;
					if (flag)
					{
						Vec2 vec = item3.GatePosition.ToVec2();
						float num = vec.x - origin.x;
						float num2 = vec.y - origin.y;
						item2 = num * num + num2 * num2;
					}
					list.Add((item3, item2));
				}
			}
		}
		catch
		{
		}
		if (list.Count <= 0)
		{
			return itemGuidePriceInfo;
		}
		if (flag)
		{
			list = list.OrderBy<(Settlement, float), float>(((Settlement St, float D2) x) => x.D2).ToList();
		}
		float[] array = new float[6] { 20f, 40f, 80f, 140f, 240f, 400f };
		for (int num3 = 0; num3 < array.Length; num3++)
		{
			float num4 = array[num3] * array[num3];
			int num5 = 0;
			int num6 = 0;
			for (int num7 = 0; num7 < list.Count; num7++)
			{
				(Settlement, float) tuple = list[num7];
				if (flag && tuple.Item2 > num4)
				{
					break;
				}
				int settlementItemStock = GetSettlementItemStock(tuple.Item1, item.StringId);
				if (settlementItemStock > 0 && TryGetSettlementBuyPrice(tuple.Item1, item, out var price) && price > 0)
				{
					num5 += price;
					num6++;
					if (num6 >= 4)
					{
						break;
					}
				}
			}
			if (num6 > 0)
			{
				itemGuidePriceInfo.UnitPrice = Math.Max(1, (int)Math.Round((double)num5 / (double)num6));
				itemGuidePriceInfo.SampleCount = num6;
				itemGuidePriceInfo.ExpandedSearch = num3 > 0;
				itemGuidePriceInfo.UsedNoStockFallback = false;
				itemGuidePriceInfo.UsedBaseValueFallback = false;
				return itemGuidePriceInfo;
			}
		}
		int num8 = 0;
		int num9 = 0;
		for (int num10 = 0; num10 < list.Count; num10++)
		{
			if (TryGetSettlementBuyPrice(list[num10].Item1, item, out var price2) && price2 > 0)
			{
				num8 += price2;
				num9++;
				if (num9 >= 4)
				{
					break;
				}
			}
		}
		if (num9 > 0)
		{
			itemGuidePriceInfo.UnitPrice = Math.Max(1, (int)Math.Round((double)num8 / (double)num9));
			itemGuidePriceInfo.SampleCount = num9;
			itemGuidePriceInfo.ExpandedSearch = true;
			itemGuidePriceInfo.UsedNoStockFallback = true;
			itemGuidePriceInfo.UsedBaseValueFallback = false;
			return itemGuidePriceInfo;
		}
		return itemGuidePriceInfo;
	}

	public bool TryGetSettlementMerchantKind(CharacterObject character, out SettlementMerchantKind kind)
	{
		kind = SettlementMerchantKind.None;
		if (character == null || character.IsHero)
		{
			return false;
		}
		switch (character.Occupation)
		{
		case Occupation.Weaponsmith:
			kind = SettlementMerchantKind.Weapon;
			return true;
		case Occupation.Blacksmith:
			kind = SettlementMerchantKind.Blacksmith;
			return true;
		case Occupation.Armorer:
			kind = SettlementMerchantKind.Armor;
			return true;
		case Occupation.HorseTrader:
			kind = SettlementMerchantKind.Horse;
			return true;
		case Occupation.GoodsTrader:
			kind = SettlementMerchantKind.Goods;
			return true;
		default:
			return false;
		}
	}

	private static string GetSettlementMerchantRoleLabel(SettlementMerchantKind kind)
	{
		return kind switch
		{
			SettlementMerchantKind.Weapon => "武器商人",
			SettlementMerchantKind.Blacksmith => "铁匠",
			SettlementMerchantKind.Armor => "盔甲商人",
			SettlementMerchantKind.Horse => "马匹贩子",
			SettlementMerchantKind.Goods => "杂货商人",
			_ => "商贩",
		};
	}

	private static string GetSettlementMerchantMarketLabel(SettlementMerchantKind kind)
	{
		return kind switch
		{
			SettlementMerchantKind.Weapon => "武器市场",
			SettlementMerchantKind.Blacksmith => "铁匠铺",
			SettlementMerchantKind.Armor => "盔甲市场",
			SettlementMerchantKind.Horse => "马匹市场",
			SettlementMerchantKind.Goods => "杂货市场",
			_ => "城镇市场",
		};
	}

	private static string GetSettlementMerchantSpecialHint(SettlementMerchantKind kind)
	{
		return kind switch
		{
			SettlementMerchantKind.Weapon => "弓、弩、箭、弩矢、投掷武器和盾牌都归入你的武器市场。",
			SettlementMerchantKind.Blacksmith => "近战武器、投掷武器和盾牌都归入你的铁匠铺。",
			SettlementMerchantKind.Armor => "头盔、身甲、臂甲、腿甲、披风等护具都归入你的盔甲市场。",
			SettlementMerchantKind.Horse => "马匹与马具都归入你的马匹市场。",
			SettlementMerchantKind.Goods => "粮食、贸易品和一般杂货都归入你的杂货市场。",
			_ => "",
		};
	}

	private static bool MatchesSettlementMerchantKind(ItemObject item, SettlementMerchantKind kind)
	{
		if (item == null || IsGeneratedRewardMarketExcludedItem(item))
		{
			return false;
		}
		switch (kind)
		{
		case SettlementMerchantKind.Blacksmith:
		{
			ItemObject.ItemTypeEnum type3 = item.Type;
			ItemObject.ItemTypeEnum itemTypeEnum3 = type3;
			if ((uint)(itemTypeEnum3 - 2) <= 8u || itemTypeEnum3 == ItemObject.ItemTypeEnum.Shield)
			{
				return true;
			}
			return false;
		}
		case SettlementMerchantKind.Weapon:
		{
			ItemObject.ItemTypeEnum type2 = item.Type;
			ItemObject.ItemTypeEnum itemTypeEnum2 = type2;
			if ((uint)(itemTypeEnum2 - 2) <= 10u || (uint)(itemTypeEnum2 - 18) <= 2u)
			{
				return true;
			}
			return false;
		}
		case SettlementMerchantKind.Armor:
		{
			ItemObject.ItemTypeEnum type = item.Type;
			ItemObject.ItemTypeEnum itemTypeEnum = type;
			if ((uint)(itemTypeEnum - 14) <= 3u || (uint)(itemTypeEnum - 23) <= 1u)
			{
				return true;
			}
			return false;
		}
		case SettlementMerchantKind.Horse:
			return item.Type == ItemObject.ItemTypeEnum.Horse || item.Type == ItemObject.ItemTypeEnum.HorseHarness;
		case SettlementMerchantKind.Goods:
			return item.Type == ItemObject.ItemTypeEnum.Goods || item.Type == ItemObject.ItemTypeEnum.Animal;
		default:
			return false;
		}
	}

	private static Settlement ResolveNotableMarketSettlement(Hero hero, Settlement settlement = null)
	{
		if (hero == null)
		{
			return null;
		}
		if (IsSupportedSettlementMarket(settlement) && IsNotableInSettlement(hero, settlement))
		{
			return settlement;
		}
		Settlement currentSettlement = null;
		try
		{
			currentSettlement = Settlement.CurrentSettlement;
		}
		catch
		{
			currentSettlement = null;
		}
		if (IsSupportedSettlementMarket(currentSettlement) && IsNotableInSettlement(hero, currentSettlement))
		{
			return currentSettlement;
		}
		Settlement heroSettlement = null;
		try
		{
			heroSettlement = hero.CurrentSettlement;
		}
		catch
		{
			heroSettlement = null;
		}
		if (IsSupportedSettlementMarket(heroSettlement) && IsNotableInSettlement(hero, heroSettlement))
		{
			return heroSettlement;
		}
		return null;
	}

	private static bool IsNotableInSettlement(Hero hero, Settlement settlement)
	{
		if (hero == null || settlement == null)
		{
			return false;
		}
		try
		{
			if (settlement.Notables != null && settlement.Notables.Contains(hero))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (IsSameSettlement(hero.CurrentSettlement, settlement) || IsSameSettlement(hero.HomeSettlement, settlement))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsSameSettlement(Settlement left, Settlement right)
	{
		if (left == null || right == null)
		{
			return false;
		}
		if (ReferenceEquals(left, right) || left == right)
		{
			return true;
		}
		string leftId = (left.StringId ?? "").Trim();
		string rightId = (right.StringId ?? "").Trim();
		return leftId.Length > 0 && string.Equals(leftId, rightId, StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsVillageMarketNotable(Hero hero)
	{
		if (hero == null)
		{
			return false;
		}
		try
		{
			return hero.IsHeadman
				|| hero.IsRuralNotable
				|| hero.Occupation == Occupation.Headman
				|| hero.Occupation == Occupation.RuralNotable;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsNotableMarketHero(Hero hero, Settlement settlement = null)
	{
		settlement = ResolveNotableMarketSettlement(hero, settlement);
		if (hero == null || settlement == null || settlement.ItemRoster == null || !IsNotableInSettlement(hero, settlement))
		{
			return false;
		}
		return (settlement.IsTown && (hero.IsArtisan || hero.IsMerchant)) || (settlement.IsVillage && IsVillageMarketNotable(hero));
	}

	private static string BuildNotableMarketPromptStringId(EquipmentElement equipmentElement)
	{
		string text = BuildSettlementMerchantInventoryKey(equipmentElement);
		return string.IsNullOrWhiteSpace(text) ? "" : (NotableMarketPromptPrefix + text);
	}

	private static bool TryParseNotableMarketPromptStringId(string promptStringId, out string settlementPromptStringId)
	{
		settlementPromptStringId = "";
		if (string.IsNullOrWhiteSpace(promptStringId))
		{
			return false;
		}
		string text = promptStringId.Trim();
		if (!text.StartsWith(NotableMarketPromptPrefix, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		settlementPromptStringId = text.Substring(NotableMarketPromptPrefix.Length).Trim();
		return TryParseSettlementMerchantPromptStringId(settlementPromptStringId, out var itemId, out var _unused) && !string.IsNullOrWhiteSpace(itemId);
	}

	private static string NormalizeNotableTitleText(Hero hero)
	{
		if (hero == null)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		try
		{
			stringBuilder.Append(hero.CharacterObject?.OriginalCharacter?.StringId ?? "").Append(' ');
			stringBuilder.Append(hero.CharacterObject?.OriginalCharacter?.Name?.ToString() ?? "").Append(' ');
		}
		catch
		{
		}
		try
		{
			stringBuilder.Append(hero.CharacterObject?.StringId ?? "").Append(' ');
			stringBuilder.Append(hero.CharacterObject?.Name?.ToString() ?? "").Append(' ');
		}
		catch
		{
		}
		try
		{
			stringBuilder.Append(hero.Name?.ToString() ?? "");
		}
		catch
		{
		}
		return stringBuilder.ToString().ToLowerInvariant();
	}

	private static bool NotableTitleContainsAny(string titleText, params string[] tokens)
	{
		if (string.IsNullOrWhiteSpace(titleText) || tokens == null)
		{
			return false;
		}
		foreach (string token in tokens)
		{
			if (!string.IsNullOrWhiteSpace(token) && titleText.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static HashSet<ItemCategory> CreateItemCategorySet(params ItemCategory[] categories)
	{
		HashSet<ItemCategory> hashSet = new HashSet<ItemCategory>();
		if (categories == null)
		{
			return hashSet;
		}
		foreach (ItemCategory category in categories)
		{
			if (category != null)
			{
				hashSet.Add(category);
			}
		}
		return hashSet;
	}

	private static bool ItemCategoryIsAny(ItemObject item, params ItemCategory[] categories)
	{
		return item != null && CreateItemCategorySet(categories).Contains(item.ItemCategory);
	}

	private static bool IsSettlementWeaponLikeItem(ItemObject item)
	{
		if (item == null)
		{
			return false;
		}
		ItemObject.ItemTypeEnum type = item.Type;
		return (uint)(type - 2) <= 10u || (uint)(type - 18) <= 2u;
	}

	private static bool IsSettlementArmorLikeItem(ItemObject item)
	{
		if (item == null)
		{
			return false;
		}
		ItemObject.ItemTypeEnum type = item.Type;
		return (uint)(type - 14) <= 3u || (uint)(type - 23) <= 1u;
	}

	private static bool IsHorseNotableMarketItem(ItemObject item)
	{
		return item != null && (item.HasHorseComponent || ItemCategoryIsAny(item, DefaultItemCategories.Horse, DefaultItemCategories.WarHorse, DefaultItemCategories.NobleHorse, DefaultItemCategories.PackAnimal));
	}

	private static bool IsRegularMerchantNotableMarketItem(ItemObject item)
	{
		if (item == null || IsSettlementWeaponLikeItem(item) || IsSettlementArmorLikeItem(item) || item.Type == ItemObject.ItemTypeEnum.HorseHarness)
		{
			return false;
		}
		return item.Type == ItemObject.ItemTypeEnum.Goods || item.Type == ItemObject.ItemTypeEnum.Animal || IsHorseNotableMarketItem(item);
	}

	private static HashSet<ItemCategory> BuildWorkshopOutputCategorySet(WorkshopType workshopType)
	{
		HashSet<ItemCategory> hashSet = new HashSet<ItemCategory>();
		if (workshopType?.Productions == null)
		{
			return hashSet;
		}
		foreach (WorkshopType.Production production in workshopType.Productions)
		{
			if (production.Outputs == null)
			{
				continue;
			}
			foreach (ValueTuple<ItemCategory, int> output in production.Outputs)
			{
				if (output.Item1 != null)
				{
					hashSet.Add(output.Item1);
				}
			}
		}
		return hashSet;
	}

	private static HashSet<ItemCategory> BuildOwnedVisibleWorkshopOutputCategorySet(Hero hero, Settlement settlement)
	{
		HashSet<ItemCategory> hashSet = new HashSet<ItemCategory>();
		try
		{
			if (hero?.OwnedWorkshops == null)
			{
				return hashSet;
			}
			foreach (Workshop workshop in hero.OwnedWorkshops)
			{
				WorkshopType workshopType = workshop?.WorkshopType;
				if (workshopType == null || workshopType.IsHidden || (settlement != null && workshop.Settlement != settlement))
				{
					continue;
				}
				foreach (ItemCategory category in BuildWorkshopOutputCategorySet(workshopType))
				{
					hashSet.Add(category);
				}
			}
		}
		catch
		{
		}
		return hashSet;
	}

	private static HashSet<ItemCategory> BuildHiddenArtisanOutputCategorySet()
	{
		try
		{
			HashSet<ItemCategory> hashSet = BuildWorkshopOutputCategorySet(WorkshopType.Find("artisans"));
			if (hashSet.Count > 0)
			{
				return hashSet;
			}
		}
		catch
		{
		}
		return CreateItemCategorySet(DefaultItemCategories.Meat, DefaultItemCategories.Hides, DefaultItemCategories.Wine, DefaultItemCategories.Tools, DefaultItemCategories.Oil, DefaultItemCategories.Garment, DefaultItemCategories.LightArmor, DefaultItemCategories.MediumArmor, DefaultItemCategories.HeavyArmor, DefaultItemCategories.UltraArmor, DefaultItemCategories.MeleeWeapons1, DefaultItemCategories.MeleeWeapons2, DefaultItemCategories.MeleeWeapons3, DefaultItemCategories.MeleeWeapons4, DefaultItemCategories.MeleeWeapons5, DefaultItemCategories.RangedWeapons1, DefaultItemCategories.RangedWeapons2, DefaultItemCategories.RangedWeapons3, DefaultItemCategories.RangedWeapons4, DefaultItemCategories.RangedWeapons5, DefaultItemCategories.Arrows, DefaultItemCategories.Shield1, DefaultItemCategories.Shield2, DefaultItemCategories.Shield3, DefaultItemCategories.Shield4, DefaultItemCategories.Shield5, DefaultItemCategories.HorseEquipment, DefaultItemCategories.HorseEquipment2, DefaultItemCategories.HorseEquipment3, DefaultItemCategories.HorseEquipment4, DefaultItemCategories.HorseEquipment5);
	}

	private static bool HasSettlementNotableMarketItem(Settlement settlement, Func<ItemObject, bool> predicate)
	{
		if (settlement?.ItemRoster == null || predicate == null)
		{
			return false;
		}
		ItemRoster itemRoster = settlement.ItemRoster;
		for (int i = 0; i < itemRoster.Count; i++)
		{
			ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
			ItemObject item = elementCopyAtIndex.EquipmentElement.Item;
			if (item != null && elementCopyAtIndex.Amount > 0 && !IsGeneratedRewardMarketExcludedItem(item) && predicate(item))
			{
				return true;
			}
		}
		return false;
	}

	private static bool MatchesMerchantNotableMarketItem(Hero hero, ItemObject item, Settlement settlement)
	{
		string titleText = NormalizeNotableTitleText(hero);
		if (NotableTitleContainsAny(titleText, "vintner", "wineseller"))
		{
			return ItemCategoryIsAny(item, DefaultItemCategories.Wine);
		}
		if (NotableTitleContainsAny(titleText, "saltpanner"))
		{
			return ItemCategoryIsAny(item, DefaultItemCategories.Salt);
		}
		if (NotableTitleContainsAny(titleText, "dateseller"))
		{
			return ItemCategoryIsAny(item, DefaultItemCategories.DateFruit);
		}
		if (NotableTitleContainsAny(titleText, "horsetrader"))
		{
			return IsHorseNotableMarketItem(item);
		}
		if (NotableTitleContainsAny(titleText, "woolseller", "wooltrader"))
		{
			return ItemCategoryIsAny(item, DefaultItemCategories.Wool);
		}
		if (NotableTitleContainsAny(titleText, "dyer", "mercer"))
		{
			return ItemCategoryIsAny(item, DefaultItemCategories.Velvet, DefaultItemCategories.Linen, DefaultItemCategories.Cloth, DefaultItemCategories.Garment, DefaultItemCategories.Felt);
		}
		if (NotableTitleContainsAny(titleText, "silkseller", "silkvendor"))
		{
			return ItemCategoryIsAny(item, DefaultItemCategories.Velvet);
		}
		if (NotableTitleContainsAny(titleText, "minter", "coppermonger"))
		{
			bool hasPreciousStock = HasSettlementNotableMarketItem(settlement, x => ItemCategoryIsAny(x, DefaultItemCategories.Silver, DefaultItemCategories.Jewelry));
			return hasPreciousStock ? ItemCategoryIsAny(item, DefaultItemCategories.Silver, DefaultItemCategories.Jewelry) : ItemCategoryIsAny(item, DefaultItemCategories.Iron);
		}
		if (NotableTitleContainsAny(titleText, "furtrader"))
		{
			return ItemCategoryIsAny(item, DefaultItemCategories.Fur);
		}
		if (NotableTitleContainsAny(titleText, "mariner"))
		{
			return ItemCategoryIsAny(item, DefaultItemCategories.Fish);
		}
		if (NotableTitleContainsAny(titleText, "incensemonger", "incensetrader", "spicetrader", "appraiser", "benefactor", "heiress"))
		{
			return ItemCategoryIsAny(item, DefaultItemCategories.Jewelry, DefaultItemCategories.Velvet, DefaultItemCategories.Oil, DefaultItemCategories.Silver, DefaultItemCategories.Fur);
		}
		return IsRegularMerchantNotableMarketItem(item);
	}

	private static HashSet<ItemCategory> BuildArtisanTitleOutputCategorySet(Hero hero)
	{
		string titleText = NormalizeNotableTitleText(hero);
		if (NotableTitleContainsAny(titleText, "brewer", "酿酒", "酿酒工", "啤酒", "啤酒工"))
		{
			return CreateItemCategorySet(DefaultItemCategories.Beer);
		}
		if (NotableTitleContainsAny(titleText, "carpenter", "cooper", "wheeler", "turner", "木匠", "木工", "桶匠", "箍桶", "车轮", "轮匠", "旋木", "车床", "车匠"))
		{
			return CreateItemCategorySet(DefaultItemCategories.Wood, DefaultItemCategories.Planks, DefaultItemCategories.Tools);
		}
		if (NotableTitleContainsAny(titleText, "chandler", "蜡烛", "蜡烛匠", "皂", "油烛"))
		{
			return CreateItemCategorySet(DefaultItemCategories.Oil);
		}
		if (NotableTitleContainsAny(titleText, "dyer", "weaver", "织布", "织布工", "纺织", "染工", "染匠", "染织"))
		{
			return CreateItemCategorySet(DefaultItemCategories.Cloth, DefaultItemCategories.Linen, DefaultItemCategories.Velvet, DefaultItemCategories.Garment, DefaultItemCategories.Felt);
		}
		if (NotableTitleContainsAny(titleText, "miller", "磨坊", "磨坊主", "磨工"))
		{
			return CreateItemCategorySet(DefaultItemCategories.Grain);
		}
		if (NotableTitleContainsAny(titleText, "smith", "铁匠", "锻工", "锻造"))
		{
			return CreateItemCategorySet(DefaultItemCategories.Tools, DefaultItemCategories.MeleeWeapons1, DefaultItemCategories.MeleeWeapons2, DefaultItemCategories.MeleeWeapons3, DefaultItemCategories.MeleeWeapons4, DefaultItemCategories.MeleeWeapons5, DefaultItemCategories.RangedWeapons1, DefaultItemCategories.RangedWeapons2, DefaultItemCategories.RangedWeapons3, DefaultItemCategories.RangedWeapons4, DefaultItemCategories.RangedWeapons5, DefaultItemCategories.Arrows, DefaultItemCategories.Shield1, DefaultItemCategories.Shield2, DefaultItemCategories.Shield3, DefaultItemCategories.Shield4, DefaultItemCategories.Shield5, DefaultItemCategories.LightArmor, DefaultItemCategories.MediumArmor, DefaultItemCategories.HeavyArmor, DefaultItemCategories.UltraArmor, DefaultItemCategories.HorseEquipment, DefaultItemCategories.HorseEquipment2, DefaultItemCategories.HorseEquipment3, DefaultItemCategories.HorseEquipment4, DefaultItemCategories.HorseEquipment5);
		}
		if (NotableTitleContainsAny(titleText, "tanner", "鞣皮", "鞣皮匠", "揉皮", "揉皮匠", "制革", "制革匠", "皮革", "皮匠"))
		{
			return CreateItemCategorySet(DefaultItemCategories.Leather);
		}
		return new HashSet<ItemCategory>();
	}

	private static bool MatchesArtisanNotableMarketItem(Hero hero, ItemObject item, Settlement settlement)
	{
		if (item == null)
		{
			return false;
		}
		HashSet<ItemCategory> visibleWorkshopOutputs = BuildOwnedVisibleWorkshopOutputCategorySet(hero, settlement);
		if (visibleWorkshopOutputs.Count > 0)
		{
			return visibleWorkshopOutputs.Contains(item.ItemCategory);
		}
		HashSet<ItemCategory> titleOutputs = BuildArtisanTitleOutputCategorySet(hero);
		if (titleOutputs.Count > 0)
		{
			return titleOutputs.Contains(item.ItemCategory);
		}
		return BuildHiddenArtisanOutputCategorySet().Contains(item.ItemCategory);
	}

	private static bool MatchesNotableMarketInventory(Hero hero, ItemObject item, Settlement settlement)
	{
		if (hero == null || item == null || IsGeneratedRewardMarketExcludedItem(item))
		{
			return false;
		}
		if (settlement != null && settlement.IsVillage && IsVillageMarketNotable(hero))
		{
			return true;
		}
		if (hero.IsArtisan)
		{
			return MatchesArtisanNotableMarketItem(hero, item, settlement);
		}
		if (hero.IsMerchant)
		{
			return MatchesMerchantNotableMarketItem(hero, item, settlement);
		}
		return false;
	}

	private List<RewardItemInfo> BuildNotableMarketItems(Hero hero, Settlement settlement = null, int maxItems = 0)
	{
		List<RewardItemInfo> list = new List<RewardItemInfo>();
		settlement = ResolveNotableMarketSettlement(hero, settlement);
		if (!IsNotableMarketHero(hero, settlement))
		{
			return list;
		}
		Dictionary<string, RewardItemInfo> dictionary = new Dictionary<string, RewardItemInfo>(StringComparer.OrdinalIgnoreCase);
		ItemRoster itemRoster = settlement.ItemRoster;
		for (int i = 0; i < itemRoster.Count; i++)
		{
			ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
			EquipmentElement equipmentElement = elementCopyAtIndex.EquipmentElement;
			ItemObject item = equipmentElement.Item;
			if (item == null || elementCopyAtIndex.Amount <= 0 || !MatchesNotableMarketInventory(hero, item, settlement))
			{
				continue;
			}
			string text = BuildNotableMarketPromptStringId(equipmentElement);
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (!dictionary.TryGetValue(text, out var value))
			{
				value = (dictionary[text] = new RewardItemInfo
				{
					Item = item,
					StringId = item.StringId ?? "",
					PromptStringId = text,
					ModifierStringId = equipmentElement.ItemModifier?.StringId ?? "",
					Name = GetSettlementMarketItemNamePrefix(settlement) + BuildSettlementMerchantDisplayName(equipmentElement),
					Count = 0,
					GuidePrice = TryGetSettlementBuyPrice(settlement, equipmentElement, out var notableMarketPrice) ? Math.Max(1, notableMarketPrice) : GetGuidePriceForRewardItem(hero, item, equipmentElement),
					EquipmentElement = equipmentElement
				});
			}
			value.Count += elementCopyAtIndex.Amount;
		}
		IEnumerable<RewardItemInfo> enumerable = dictionary.Values.OrderByDescending((RewardItemInfo x) => x.Count).ThenBy((RewardItemInfo x) => x.Name, StringComparer.Ordinal);
		if (maxItems > 0)
		{
			enumerable = enumerable.Take(maxItems);
		}
		return enumerable.ToList();
	}

	public string BuildSettlementMerchantRewardInstruction(CharacterObject character)
	{
		if (!TryGetSettlementMerchantKind(character, out var kind))
		{
			return "";
		}
		string text = Settlement.CurrentSettlement?.Name?.ToString() ?? "当前城镇";
		string settlementMerchantRoleLabel = GetSettlementMerchantRoleLabel(kind);
		string settlementMerchantMarketLabel = GetSettlementMerchantMarketLabel(kind);
		string settlementMerchantSpecialHint = GetSettlementMerchantSpecialHint(kind);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【城镇商贩补充】你不是在卖你个人的私人物品。");
		stringBuilder.AppendLine("你是" + text + "里的" + settlementMerchantRoleLabel + "，代表这座城镇当前的" + settlementMerchantMarketLabel + "与玩家进行即时交易。");
		stringBuilder.AppendLine("你的真实可售货物只以你当前摊位资产清单中的内容为准。");
		if (!string.IsNullOrWhiteSpace(settlementMerchantSpecialHint))
		{
			stringBuilder.AppendLine(settlementMerchantSpecialHint);
		}
		return stringBuilder.ToString().Trim();
	}

	public string BuildNotableMarketRewardInstruction(Hero hero)
	{
		Settlement settlement = ResolveNotableMarketSettlement(hero);
		if (!IsNotableMarketHero(hero, settlement))
		{
			return "";
		}
		string settlementTypeLabel = GetSettlementMarketTypeLabel(settlement);
		string marketItemPrefix = GetSettlementMarketItemNamePrefix(settlement).Trim();
		string text = settlement?.Name?.ToString() ?? ("当前" + settlementTypeLabel);
		string text2 = settlement?.IsVillage == true ? "村庄头人" : (hero.IsArtisan ? "工匠要人" : "商人要人");
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【要人市场补充】你是" + text + "的" + text2 + "，商铺清单中的 " + marketItemPrefix + " 条目来自当前" + settlementTypeLabel + "市场总库存，不是你的私人财物。");
		stringBuilder.AppendLine("后处理清单中内部 token 以 market@ 开头的条目，成交时扣当前" + settlementTypeLabel + "市场库存；普通物品条目仍扣你的个人库存或装备。");
		if (settlement?.IsVillage == true)
		{
			stringBuilder.AppendLine("作为村庄头人，你可以代表本村市场出售当前村庄市场清单中的全部真实货物；清单无货时你什么都不卖，请不要虚构任何物品。");
		}
		else
		{
			stringBuilder.AppendLine("如果你的称号或工坊有强专精，你只能从商铺清单里出售符合该专精的货物；专精无货时你什么都不卖，请不要虚构任何物品。");
		}
		stringBuilder.AppendLine("金币交易代表" + settlementTypeLabel + "市场现金，优先从当前" + settlementTypeLabel + "金库扣除；债务、信任和事实仍记在你这个要人名下。");
		return stringBuilder.ToString().Trim();
	}

	public string BuildSettlementMerchantInventorySummaryForAI(CharacterObject character, Settlement settlement = null, int maxItems = 200, bool includeGuidePrice = true)
	{
		if (!TryGetSettlementMerchantKind(character, out var kind))
		{
			return "";
		}
		settlement = settlement ?? Settlement.CurrentSettlement;
		if (settlement == null || !settlement.IsTown || settlement.ItemRoster == null)
		{
			return "";
		}
		Dictionary<string, RewardItemInfo> dictionary = new Dictionary<string, RewardItemInfo>(StringComparer.OrdinalIgnoreCase);
		ItemRoster itemRoster = settlement.ItemRoster;
		for (int i = 0; i < itemRoster.Count; i++)
		{
			ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
			EquipmentElement equipmentElement = elementCopyAtIndex.EquipmentElement;
			ItemObject item = equipmentElement.Item;
			if (item == null || elementCopyAtIndex.Amount <= 0 || !MatchesSettlementMerchantKind(item, kind))
			{
				continue;
			}
			string text = BuildSettlementMerchantInventoryKey(equipmentElement);
			if (!string.IsNullOrWhiteSpace(text))
			{
				if (!dictionary.TryGetValue(text, out var value))
				{
					value = (dictionary[text] = new RewardItemInfo
					{
						Item = item,
						StringId = item.StringId ?? "",
						PromptStringId = text,
						ModifierStringId = (equipmentElement.ItemModifier?.StringId ?? ""),
						Name = BuildSettlementMerchantDisplayName(equipmentElement),
						Count = 0,
						GuidePrice = TryGetSettlementBuyPrice(settlement, equipmentElement, out var merchantInventoryPrice) ? Math.Max(1, merchantInventoryPrice) : Math.Max(1, item.Value),
						EquipmentElement = equipmentElement
					});
				}
				value.Count += elementCopyAtIndex.Amount;
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		int value2 = GetSettlementMarketTradeGold(settlement);
		stringBuilder.Append("第纳尔: ").Append(value2).AppendLine();
		if (includeGuidePrice)
		{
			stringBuilder.AppendLine("【价格说明】每个物品后面的 guidePrice 为当前城镇市场的即时指导单价（第纳尔/当前单位；箭矢、弩矢、标枪、飞刀等远程弹药按袋计）。");
		}
		stringBuilder.AppendLine("库存物品：");
		foreach (RewardItemInfo item2 in dictionary.Values.OrderByDescending((RewardItemInfo x) => x.Count).ThenBy((RewardItemInfo x) => x.Name, StringComparer.Ordinal))
		{
			stringBuilder.Append(item2.Name)
				.Append(" | ");
			AppendItemTypeField(stringBuilder, item2.EquipmentElement.Item);
			stringBuilder.Append(" | ")
				.Append(item2.Count)
				.Append(GetItemQuantityUnit(item2.EquipmentElement.Item));
			if (includeGuidePrice && TryGetSettlementBuyPrice(settlement, item2.EquipmentElement, out var price))
			{
				stringBuilder.Append(" | guidePrice=").Append(Math.Max(1, price));
			}
			stringBuilder.AppendLine();
		}
		if (dictionary.Count == 0)
		{
			stringBuilder.AppendLine("（当前没有可售货物）");
		}
		return stringBuilder.ToString().Trim();
	}

	public List<RewardItemInfo> GetHeroInventoryItems(Hero hero)
	{
		List<RewardItemInfo> list = new List<RewardItemInfo>();
		if (hero == null)
		{
			return list;
		}
		ItemRoster itemRoster = ((hero.PartyBelongedTo != null) ? hero.PartyBelongedTo.ItemRoster : null);
		if (itemRoster == null && hero.Clan?.Leader?.PartyBelongedTo != null)
		{
			itemRoster = hero.Clan.Leader.PartyBelongedTo.ItemRoster;
		}
		if (itemRoster == null && MobileParty.MainParty != null && hero == Hero.MainHero)
		{
			itemRoster = MobileParty.MainParty.ItemRoster;
		}
		if (itemRoster != null)
		{
			for (int i = 0; i < itemRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
				if (elementCopyAtIndex.EquipmentElement.Item != null)
				{
					ItemObject item = elementCopyAtIndex.EquipmentElement.Item;
					int amount = elementCopyAtIndex.Amount;
					if (amount > 0)
					{
						EquipmentElement equipmentElement = elementCopyAtIndex.EquipmentElement;
						list.Add(new RewardItemInfo
						{
							Item = item,
							StringId = item.StringId,
							Name = (equipmentElement.GetModifiedItemName()?.ToString() ?? item.Name?.ToString() ?? item.StringId),
							Count = amount,
							GuidePrice = GetGuidePriceForRewardItem(hero, item, equipmentElement),
							EquipmentElement = equipmentElement
						});
					}
				}
			}
		}
		return list;
	}

	private static bool TryResolvePromptEquipmentContext(Hero hero, out bool useCivilianEquipment)
	{
		useCivilianEquipment = false;
		try
		{
			Mission current = Mission.Current;
			if (current != null)
			{
				useCivilianEquipment = current.DoesMissionRequireCivilianEquipment;
				return true;
			}
		}
		catch
		{
		}
		try
		{
			Settlement settlement = Settlement.CurrentSettlement ?? hero?.CurrentSettlement;
			if (settlement != null)
			{
				useCivilianEquipment = !settlement.IsVillage;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private List<RewardItemInfo> GetHeroEquipmentItems(Hero hero, bool useCivilianEquipment)
	{
		List<RewardItemInfo> list = new List<RewardItemInfo>();
		if (hero == null)
		{
			return list;
		}
		EquipmentIndex[] array = new EquipmentIndex[12]
		{
			EquipmentIndex.NumAllWeaponSlots,
			EquipmentIndex.Body,
			EquipmentIndex.Leg,
			EquipmentIndex.Gloves,
			EquipmentIndex.Cape,
			EquipmentIndex.WeaponItemBeginSlot,
			EquipmentIndex.Weapon1,
			EquipmentIndex.Weapon2,
			EquipmentIndex.Weapon3,
			EquipmentIndex.ExtraWeaponSlot,
			EquipmentIndex.Horse,
			EquipmentIndex.HorseHarness
		};
		EquipmentIndex[] array2 = array;
		EquipmentIndex[] array3 = array2;
		foreach (EquipmentIndex index in array3)
		{
			Equipment equipment = useCivilianEquipment ? hero.CivilianEquipment : hero.BattleEquipment;
			EquipmentElement equipmentElement = equipment[index];
			if (equipmentElement.Item != null)
			{
				ItemObject item = equipmentElement.Item;
				list.Add(new RewardItemInfo
				{
					Item = item,
					StringId = item.StringId,
					Name = (equipmentElement.GetModifiedItemName()?.ToString() ?? item.Name?.ToString() ?? item.StringId),
					Count = 1,
					GuidePrice = GetGuidePriceForRewardItem(hero, item, equipmentElement),
					EquipmentElement = equipmentElement,
					IsPrivateEquipment = !useCivilianEquipment
				});
			}
		}
		return list;
	}

	public List<RewardItemInfo> GetHeroBattleEquipmentItems(Hero hero)
	{
		return GetHeroEquipmentItems(hero, useCivilianEquipment: false);
	}

	private List<RewardItemInfo> GetAgentEquipmentItems(Agent agent)
	{
		List<RewardItemInfo> list = new List<RewardItemInfo>();
		if (agent == null || !agent.IsActive())
		{
			return list;
		}
		EquipmentIndex[] array = new EquipmentIndex[12]
		{
			EquipmentIndex.NumAllWeaponSlots,
			EquipmentIndex.Body,
			EquipmentIndex.Leg,
			EquipmentIndex.Gloves,
			EquipmentIndex.Cape,
			EquipmentIndex.WeaponItemBeginSlot,
			EquipmentIndex.Weapon1,
			EquipmentIndex.Weapon2,
			EquipmentIndex.Weapon3,
			EquipmentIndex.ExtraWeaponSlot,
			EquipmentIndex.Horse,
			EquipmentIndex.HorseHarness
		};
		EquipmentIndex[] array2 = array;
		foreach (EquipmentIndex index in array2)
		{
			EquipmentElement equipmentElement = EquipmentElement.Invalid;
			ItemObject item = agent.SpawnEquipment[index].Item;
			if (item == null)
			{
				item = agent.Equipment[index].Item;
			}
			if (item != null)
			{
				string itemName = item.Name?.ToString() ?? item.StringId;
				list.Add(new RewardItemInfo
				{
					Item = item,
					StringId = item.StringId,
					Name = itemName,
					Count = 1,
					GuidePrice = Math.Max(1, item.Value),
					EquipmentElement = equipmentElement
				});
			}
		}
		return list;
	}

	private List<RewardItemInfo> GetHeroVisibleEquipmentItemsForPrompt(Hero hero)
	{
		if (hero == null)
		{
			return new List<RewardItemInfo>();
		}
		bool useCivilianEquipment = false;
		TryResolvePromptEquipmentContext(hero, out useCivilianEquipment);
		List<RewardItemInfo> heroEquipmentItems = GetHeroEquipmentItems(hero, useCivilianEquipment);
		if (heroEquipmentItems.Count > 0)
		{
			return heroEquipmentItems;
		}
		if (hero == Hero.MainHero && Agent.Main != null && Agent.Main.IsActive())
		{
			List<RewardItemInfo> agentEquipmentItems = GetAgentEquipmentItems(Agent.Main);
			if (agentEquipmentItems.Count > 0)
			{
				return agentEquipmentItems;
			}
		}
		return GetHeroBattleEquipmentItems(hero);
	}

	private int GetVisibleEquipmentActualUnitValue(RewardItemInfo itemInfo)
	{
		if (itemInfo == null)
		{
			return 0;
		}
		try
		{
			if (itemInfo.EquipmentElement.Item != null)
			{
				return Math.Max(1, GetInventoryActualItemUnitValueForExternal(itemInfo.EquipmentElement));
			}
		}
		catch
		{
		}
		try
		{
			return Math.Max(1, itemInfo.Item?.Value ?? 1);
		}
		catch
		{
			return 1;
		}
	}

	public long EstimateVisibleEquipmentActualValueForAI(Hero hero, int maxItems = 8)
	{
		if (hero == null)
		{
			return 0L;
		}
		List<RewardItemInfo> heroVisibleEquipmentItemsForPrompt = GetHeroVisibleEquipmentItemsForPrompt(hero);
		if (heroVisibleEquipmentItemsForPrompt == null || heroVisibleEquipmentItemsForPrompt.Count <= 0)
		{
			return 0L;
		}
		long num = 0L;
		int num2 = 0;
		List<RewardItemInfo> selectedItems = heroVisibleEquipmentItemsForPrompt
			.Where((RewardItemInfo x) => x != null && x.Item != null)
			.OrderByDescending((RewardItemInfo x) => x.Count)
			.ThenBy((RewardItemInfo x) => x.StringId, StringComparer.Ordinal)
			.Take(Math.Max(1, maxItems))
			.ToList();
		PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.PlayerVisibleEquipmentSnapshotScope, hero, hero?.CharacterObject, -1, selectedItems);
		foreach (RewardItemInfo item in selectedItems)
		{
			if (item == null || item.Item == null)
			{
				continue;
			}
			int visibleEquipmentActualUnitValue = Math.Max(1, GetVisibleEquipmentActualUnitValue(item));
			int num3 = Math.Max(1, item.Count);
			num += (long)visibleEquipmentActualUnitValue * (long)num3;
			num2++;
			if (num2 >= Math.Max(1, maxItems))
			{
				break;
			}
		}
		return Math.Max(0L, num);
	}

	public string BuildVisibleEquipmentActualValueInlineFactForAI(Hero hero, int maxItems = 8)
	{
		try
		{
			long num = EstimateVisibleEquipmentActualValueForAI(hero, maxItems);
			if (num <= 0L)
			{
				return "";
			}
			return "这身当前可见装备按玩家库存中的实际价值计算，总值约 " + num + " 第纳尔";
		}
		catch
		{
			return "";
		}
	}

	public string BuildVisibleEquipmentActualValueSummaryForAI(Hero hero, int maxItems = 8)
	{
		if (hero == null)
		{
			return string.Empty;
		}
		List<RewardItemInfo> heroVisibleEquipmentItemsForPrompt = GetHeroVisibleEquipmentItemsForPrompt(hero);
		if (heroVisibleEquipmentItemsForPrompt == null || heroVisibleEquipmentItemsForPrompt.Count <= 0)
		{
			return string.Empty;
		}
		long num = 0L;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【玩家可见装备实际估值】以下为玩家当前穿戴/携行装备按库存实际价值计算（第纳尔）：");
		int num2 = 0;
		List<RewardItemInfo> selectedItems = heroVisibleEquipmentItemsForPrompt
			.Where((RewardItemInfo x) => x != null && x.Item != null)
			.OrderByDescending((RewardItemInfo x) => x.Count)
			.ThenBy((RewardItemInfo x) => x.StringId, StringComparer.Ordinal)
			.Take(Math.Max(1, maxItems))
			.ToList();
		PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.PlayerVisibleEquipmentSnapshotScope, hero, hero?.CharacterObject, -1, selectedItems);
		foreach (RewardItemInfo item in selectedItems)
		{
			if (item == null || item.Item == null)
			{
				continue;
			}
			int visibleEquipmentActualUnitValue = Math.Max(1, GetVisibleEquipmentActualUnitValue(item));
			int num3 = Math.Max(1, item.Count);
			long num4 = (long)visibleEquipmentActualUnitValue * (long)num3;
			num += num4;
			stringBuilder.Append(item.StringId).Append("|").Append(item.Name ?? item.StringId)
				.Append("|type=")
				.Append(GetItemPromptTypeLabel(item.Item))
				.Append("|")
				.Append(num3)
				.Append(GetItemQuantityUnit(item.Item))
				.Append("|inventoryUnitValue=")
				.Append(visibleEquipmentActualUnitValue)
				.Append("|lineValue=")
				.Append(num4)
				.AppendLine();
			num2++;
			if (num2 >= Math.Max(1, maxItems))
			{
				break;
			}
		}
		stringBuilder.AppendLine("总估值约 " + Math.Max(0L, num) + " 第纳尔。");
		return stringBuilder.ToString().Trim();
	}

	public string BuildVisibleEquipmentGuidePriceSummaryForAI(Hero hero, int maxItems = 8)
	{
		if (hero == null)
		{
			return string.Empty;
		}
		List<RewardItemInfo> heroVisibleEquipmentItemsForPrompt = GetHeroVisibleEquipmentItemsForPrompt(hero);
		if (heroVisibleEquipmentItemsForPrompt == null || heroVisibleEquipmentItemsForPrompt.Count <= 0)
		{
			return string.Empty;
		}
		Dictionary<string, ItemGuidePriceInfo> dictionary = new Dictionary<string, ItemGuidePriceInfo>(StringComparer.OrdinalIgnoreCase);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("以下为玩家当前穿戴的装备和携带的武器：");
		List<RewardItemInfo> selectedItems = heroVisibleEquipmentItemsForPrompt
			.Where((RewardItemInfo x) => x != null && x.Item != null)
			.OrderByDescending((RewardItemInfo x) => x.Count)
			.ThenBy((RewardItemInfo x) => x.StringId, StringComparer.Ordinal)
			.Take(Math.Max(1, maxItems))
			.ToList();
		PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.PlayerVisibleEquipmentSnapshotScope, hero, hero?.CharacterObject, -1, selectedItems);
		foreach (RewardItemInfo item in selectedItems)
		{
			if (item == null || item.Item == null)
			{
				continue;
			}
			string key = item.StringId ?? "";
			if (!dictionary.TryGetValue(key, out var value))
			{
				ItemGuidePriceInfo itemGuidePriceInfo = (dictionary[key] = GetGuidePriceForItemNearHero(hero, item.Item));
				value = itemGuidePriceInfo;
			}
			stringBuilder.Append(item.Name ?? item.StringId)
				.Append(" | ");
			AppendItemTypeField(stringBuilder, item.Item);
			stringBuilder.Append(" | ")
				.Append(Math.Max(1, item.Count))
				.Append(GetItemQuantityUnit(item.Item))
				.Append(" | guidePrice=")
				.Append(Math.Max(1, value.UnitPrice))
				.AppendLine();
		}
		return stringBuilder.ToString().Trim();
	}

	public List<RewardItemInfo> BuildSettlementMerchantPostprocessItems(CharacterObject character, Settlement settlement = null, int maxItems = 0)
	{
		List<RewardItemInfo> list = new List<RewardItemInfo>();
		if (!TryGetSettlementMerchantKind(character, out var kind))
		{
			return list;
		}
		settlement = settlement ?? Settlement.CurrentSettlement;
		if (settlement == null || !settlement.IsTown || settlement.ItemRoster == null)
		{
			return list;
		}
		Dictionary<string, RewardItemInfo> dictionary = new Dictionary<string, RewardItemInfo>(StringComparer.OrdinalIgnoreCase);
		ItemRoster itemRoster = settlement.ItemRoster;
		for (int i = 0; i < itemRoster.Count; i++)
		{
			ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
			EquipmentElement equipmentElement = elementCopyAtIndex.EquipmentElement;
			ItemObject item = equipmentElement.Item;
			if (item == null || elementCopyAtIndex.Amount <= 0 || !MatchesSettlementMerchantKind(item, kind))
			{
				continue;
			}
			string text = BuildSettlementMerchantInventoryKey(equipmentElement);
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (!dictionary.TryGetValue(text, out var value))
			{
				value = (dictionary[text] = new RewardItemInfo
				{
					Item = item,
					StringId = item.StringId ?? "",
					PromptStringId = text,
					ModifierStringId = equipmentElement.ItemModifier?.StringId ?? "",
					Name = BuildSettlementMerchantDisplayName(equipmentElement),
					Count = 0,
					GuidePrice = TryGetSettlementBuyPrice(settlement, equipmentElement, out var merchantPostprocessPrice) ? Math.Max(1, merchantPostprocessPrice) : Math.Max(1, item.Value),
					EquipmentElement = equipmentElement
				});
			}
			value.Count += elementCopyAtIndex.Amount;
		}
		IEnumerable<RewardItemInfo> enumerable = dictionary.Values.OrderByDescending((RewardItemInfo x) => x.Count).ThenBy((RewardItemInfo x) => x.Name, StringComparer.Ordinal);
		if (maxItems > 0)
		{
			enumerable = enumerable.Take(maxItems);
		}
		return enumerable.ToList();
	}

	public List<RewardItemInfo> BuildHeroRewardPostprocessItems(Hero hero, int maxItems = 0)
	{
		List<RewardItemInfo> marketItems = BuildNotableMarketItems(hero)
			.Where((RewardItemInfo x) => x != null && x.Item != null && x.Count > 0)
			.ToList();
		List<RewardItemInfo> list = GetHeroInventoryItems(hero)
			.Where((RewardItemInfo x) => x != null && x.Item != null && x.Count > 0)
			.OrderByDescending((RewardItemInfo x) => x.Count)
			.ThenBy((RewardItemInfo x) => x.Name, StringComparer.Ordinal)
			.ToList();
		List<RewardItemInfo> list2 = GetHeroBattleEquipmentItems(hero)
			.Where((RewardItemInfo x) => x != null && x.Item != null)
			.Select(delegate(RewardItemInfo x)
			{
				x.IsPrivateEquipment = true;
				return x;
			})
			.OrderBy((RewardItemInfo x) => x.Name, StringComparer.Ordinal)
			.ToList();
		List<RewardItemInfo> list3 = new List<RewardItemInfo>(marketItems.Count + list.Count + list2.Count);
		list3.AddRange(marketItems);
		list3.AddRange(list);
		list3.AddRange(list2);
		if (maxItems > 0)
		{
			return list3.Take(maxItems).ToList();
		}
		return list3;
	}

	public int GetPartyTradeGoldForExternal(PartyBase party)
	{
		try
		{
			return Math.Max(0, party?.MobileParty?.PartyTradeGold ?? 0);
		}
		catch
		{
			return 0;
		}
	}

	public List<RewardItemInfo> BuildPartyRewardPostprocessItems(PartyBase party, int maxItems = 0)
	{
		Dictionary<string, RewardItemInfo> dictionary = new Dictionary<string, RewardItemInfo>(StringComparer.OrdinalIgnoreCase);
		try
		{
			ItemRoster itemRoster = party?.ItemRoster;
			if (itemRoster == null)
			{
				return new List<RewardItemInfo>();
			}
			for (int i = 0; i < itemRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
				EquipmentElement equipmentElement = elementCopyAtIndex.EquipmentElement;
				ItemObject item = equipmentElement.Item;
				if (item == null || elementCopyAtIndex.Amount <= 0)
				{
					continue;
				}
				string text = BuildSettlementMerchantInventoryKey(equipmentElement);
				if (string.IsNullOrWhiteSpace(text))
				{
					text = item.StringId ?? "";
				}
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				if (!dictionary.TryGetValue(text, out var value))
				{
					value = (dictionary[text] = new RewardItemInfo
					{
						Item = item,
						StringId = item.StringId ?? "",
						PromptStringId = text,
						ModifierStringId = equipmentElement.ItemModifier?.StringId ?? "",
						Name = BuildSettlementMerchantDisplayName(equipmentElement),
						Count = 0,
						GuidePrice = GetGuidePriceForRewardItem(Hero.MainHero, item, equipmentElement),
						EquipmentElement = equipmentElement
					});
				}
				value.Count += elementCopyAtIndex.Amount;
			}
		}
		catch
		{
			return new List<RewardItemInfo>();
		}
		IEnumerable<RewardItemInfo> enumerable = dictionary.Values.OrderByDescending((RewardItemInfo x) => x.Count).ThenBy((RewardItemInfo x) => x.Name, StringComparer.Ordinal);
		if (maxItems > 0)
		{
			enumerable = enumerable.Take(maxItems);
		}
		return enumerable.ToList();
	}

	public string BuildVisibleEquipmentValueSummaryForAI(Hero hero, int maxItems = 8, bool useGuidePrice = false)
	{
		if (useGuidePrice)
		{
			return BuildVisibleEquipmentGuidePriceSummaryForAI(hero, maxItems);
		}
		return BuildVisibleEquipmentActualValueSummaryForAI(hero, maxItems);
	}

	public string BuildVisibleEquipmentPostprocessListForAI(Hero hero, int maxItems = 64)
	{
		if (hero == null)
		{
			return "（无）";
		}
		List<RewardItemInfo> heroVisibleEquipmentItemsForPrompt = GetHeroVisibleEquipmentItemsForPrompt(hero);
		if (heroVisibleEquipmentItemsForPrompt == null || heroVisibleEquipmentItemsForPrompt.Count <= 0)
		{
			return "（无）";
		}
		List<RewardItemInfo> filteredItems;
		if (!PromptListRetrievalService.TryGetRewardItemSnapshot(PromptListRetrievalService.PlayerVisibleEquipmentSnapshotScope, hero, hero?.CharacterObject, -1, out filteredItems))
		{
			filteredItems = heroVisibleEquipmentItemsForPrompt
				.Where((RewardItemInfo x) => x != null && x.Item != null)
				.OrderByDescending((RewardItemInfo x) => x.Count)
				.ThenBy((RewardItemInfo x) => x.StringId, StringComparer.Ordinal)
				.Take(Math.Max(1, maxItems))
				.ToList();
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (RewardItemInfo item in filteredItems)
		{
			if (item == null || item.Item == null)
			{
				continue;
			}
			stringBuilder.Append(item.Name ?? item.StringId ?? "未知物品")
				.Append(" | type=")
				.Append(GetItemPromptTypeLabel(item.Item))
				.Append(" | x")
				.Append(Math.Max(1, item.Count))
				.Append(" | inventoryUnitValue=")
				.Append(Math.Max(1, GetVisibleEquipmentActualUnitValue(item)))
				.AppendLine();
		}
		return stringBuilder.Length > 0 ? stringBuilder.ToString().TrimEnd() : "（无）";
	}

	public string BuildVisibleEquipmentPostprocessListForAI(Hero hero, MentionedWorldEntities mentions, int maxItems = 0)
	{
		if (hero == null)
		{
			return "（无）";
		}
		List<RewardItemInfo> heroVisibleEquipmentItemsForPrompt = GetHeroVisibleEquipmentItemsForPrompt(hero);
		if (heroVisibleEquipmentItemsForPrompt == null || heroVisibleEquipmentItemsForPrompt.Count <= 0)
		{
			return "（无）";
		}
		List<RewardItemInfo> orderedItems = heroVisibleEquipmentItemsForPrompt.OrderByDescending((RewardItemInfo x) => x.Count).ThenBy((RewardItemInfo x) => x.StringId, StringComparer.Ordinal).ToList();
		List<RewardItemInfo> filteredItems;
		if (!PromptListRetrievalService.TryGetRewardItemSnapshot(PromptListRetrievalService.PlayerVisibleEquipmentSnapshotScope, hero, hero?.CharacterObject, -1, out filteredItems))
		{
			filteredItems = PromptListRetrievalService.FilterRewardItems(orderedItems, mentions, maxItems);
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (RewardItemInfo item in filteredItems)
		{
			if (item == null || item.Item == null)
			{
				continue;
			}
			stringBuilder.Append(item.Name ?? item.StringId ?? "未知物品")
				.Append(" | type=")
				.Append(GetItemPromptTypeLabel(item.Item))
				.Append(" | x")
				.Append(Math.Max(1, item.Count))
				.Append(" | inventoryUnitValue=")
				.Append(Math.Max(1, GetVisibleEquipmentActualUnitValue(item)))
				.AppendLine();
		}
		return stringBuilder.Length > 0 ? stringBuilder.ToString().TrimEnd() : "（无）";
	}

	public string BuildFilteredInventorySummaryForAI(Hero hero, MentionedWorldEntities mentions, int maxItems = 0, bool includeGuidePrice = true, bool includePrivateBattleEquipment = false)
	{
		if (hero == null)
		{
			return "";
		}
		try
		{
			List<RewardItemInfo> allOptions = BuildHeroRewardPostprocessItems(hero)
				.Where((RewardItemInfo x) => x != null && x.Item != null && x.Count > 0)
				.ToList();
			PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsAllSnapshotScope, hero, hero?.CharacterObject, -1, allOptions);
			List<RewardItemInfo> displayCandidates = allOptions;
			if (!includePrivateBattleEquipment)
			{
				displayCandidates = allOptions.Where((RewardItemInfo x) => !x.IsPrivateEquipment).ToList();
			}
			List<RewardItemInfo> options = includePrivateBattleEquipment
				? PromptListRetrievalService.FilterNpcRewardItemsForAssetTransfer(displayCandidates, mentions, maxItems)
				: PromptListRetrievalService.FilterRewardItems(displayCandidates, mentions, maxItems);
			PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsSnapshotScope, hero, hero?.CharacterObject, -1, options);
			int gold = IsNotableMarketHero(hero, ResolveNotableMarketSettlement(hero)) ? GetRewardPostprocessGoldForHero(hero) : GetHeroGold(hero);
			return BuildFilteredItemSummaryForAI(options, gold, includeGuidePrice, allOptions, "你");
		}
		catch
		{
			return "";
		}
	}

	public string BuildFilteredSettlementMerchantInventorySummaryForAI(CharacterObject character, MentionedWorldEntities mentions, int maxItems = 0, Settlement settlement = null, bool includeGuidePrice = true)
	{
		try
		{
			settlement = settlement ?? Settlement.CurrentSettlement;
			List<RewardItemInfo> allOptions = BuildSettlementMerchantPostprocessItems(character, settlement)
				.Where((RewardItemInfo x) => x != null && x.Item != null && x.Count > 0)
				.ToList();
			PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.SettlementMerchantItemsAllSnapshotScope, null, character, -1, allOptions);
			List<RewardItemInfo> options = PromptListRetrievalService.FilterRewardItems(allOptions, mentions, maxItems);
			PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.SettlementMerchantItemsSnapshotScope, null, character, -1, options);
			int gold = GetSettlementMarketTradeGold(settlement);
			return BuildFilteredItemSummaryForAI(options, gold, includeGuidePrice, allOptions, "你");
		}
		catch
		{
			return "";
		}
	}

	private static string BuildFilteredItemSummaryForAI(List<RewardItemInfo> options, int gold, bool includeGuidePrice, List<RewardItemInfo> allOptions = null, string ownerLabel = "你")
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("第纳尔: ").Append(Math.Max(0, gold)).AppendLine();
		if (options == null || options.Count == 0)
		{
			stringBuilder.AppendLine("（本轮检索清单无可显示物品）");
			stringBuilder.Append("全部可转物品总值: ").Append(CalculateRewardItemsTotalValueForExternal(allOptions)).AppendLine(" 第纳尔（不含第纳尔）");
			return stringBuilder.ToString().TrimEnd();
		}
		List<RewardItemInfo> publicItems = options.Where((RewardItemInfo x) => x != null && !x.IsPrivateEquipment).ToList();
		List<RewardItemInfo> privateItems = options.Where((RewardItemInfo x) => x != null && x.IsPrivateEquipment).ToList();
		AppendFilteredItemSummarySection(stringBuilder, "库存物品：", publicItems, includeGuidePrice);
		AppendFilteredItemSummarySection(stringBuilder, "私人战斗装备：", privateItems, includeGuidePrice);
		string remainder = PromptListRetrievalService.BuildRemainingRewardItemsSummary(allOptions, options, ownerLabel);
		if (!string.IsNullOrWhiteSpace(remainder))
		{
			stringBuilder.AppendLine(remainder);
		}
		stringBuilder.Append("全部可转物品总值: ").Append(CalculateRewardItemsTotalValueForExternal(allOptions ?? options)).AppendLine(" 第纳尔（不含第纳尔）");
		return stringBuilder.ToString().TrimEnd();
	}

	internal static long CalculateRewardItemsTotalValueForExternal(IEnumerable<RewardItemInfo> items)
	{
		long total = 0L;
		foreach (RewardItemInfo item in items ?? Enumerable.Empty<RewardItemInfo>())
		{
			if (item == null || item.Item == null || item.Count <= 0)
			{
				continue;
			}
			total = TransferQuantitySpec.AddProduct(total, item.Count, Math.Max(1, item.GuidePrice));
		}
		return total;
	}

	private static void AppendFilteredItemSummarySection(StringBuilder stringBuilder, string header, List<RewardItemInfo> items, bool includeGuidePrice)
	{
		if (stringBuilder == null || items == null || items.Count == 0)
		{
			return;
		}
		stringBuilder.AppendLine(header);
		foreach (RewardItemInfo item in items)
		{
			if (item == null)
			{
				continue;
			}
			stringBuilder.Append(item.Name ?? item.PromptStringId ?? item.StringId ?? "未知物品")
				.Append(" | type=")
				.Append(GetItemPromptTypeLabel(item.Item))
				.Append(" | ")
				.Append(Math.Max(1, item.Count))
				.Append(GetItemQuantityUnit(item.Item));
			if (!string.IsNullOrWhiteSpace(item.PromptStringId) && !string.Equals(item.PromptStringId, item.StringId, StringComparison.OrdinalIgnoreCase))
			{
				stringBuilder.Append(" | token=").Append(item.PromptStringId);
			}
			if (includeGuidePrice)
			{
				stringBuilder.Append(" | guidePrice=").Append(Math.Max(1, item.GuidePrice));
			}
			stringBuilder.AppendLine();
		}
	}

	public string BuildInventorySummaryForAI(Hero hero, int maxItems = 200, bool includeGuidePrice = true, bool includePrivateBattleEquipment = false, int maxPrivateEquipmentItems = 32)
	{
		Settlement notableMarketSettlement = ResolveNotableMarketSettlement(hero);
		bool isNotableMarketHero = IsNotableMarketHero(hero, notableMarketSettlement);
		int heroGold = isNotableMarketHero ? GetRewardPostprocessGoldForHero(hero) : GetHeroGold(hero);
		string notableMarketTypeLabel = GetSettlementMarketTypeLabel(notableMarketSettlement);
		int notableMarketPromptMaxItems = Math.Min(Math.Max(1, maxItems), NotableMarketInventoryPromptMaxItems);
		List<RewardItemInfo> notableMarketItems = isNotableMarketHero ? BuildNotableMarketItems(hero, notableMarketSettlement, notableMarketPromptMaxItems) : new List<RewardItemInfo>();
		List<RewardItemInfo> heroInventoryItems = GetHeroInventoryItems(hero);
		List<RewardItemInfo> heroBattleEquipmentItems = GetHeroBattleEquipmentItems(hero);
		string text = hero?.Name?.ToString() ?? "该NPC";
		Dictionary<string, ItemGuidePriceInfo> dictionary = (includeGuidePrice ? new Dictionary<string, ItemGuidePriceInfo>(StringComparer.OrdinalIgnoreCase) : null);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("第纳尔: ").Append(heroGold).AppendLine();
		if (includeGuidePrice)
		{
			stringBuilder.AppendLine(isNotableMarketHero ? ("【价格说明】市场物品后面的 guidePrice 为当前" + notableMarketTypeLabel + "市场即时指导单价；个人物品仍按目标附近价格估算（第纳尔/当前单位；箭矢、弩矢、标枪、飞刀等远程弹药按袋计）。") : "【价格说明】每个物品后面的 guidePrice 为指导单价（第纳尔/当前单位；箭矢、弩矢、标枪、飞刀等远程弹药按袋计）。");
		}
		int num = 0;
		if (isNotableMarketHero)
		{
			stringBuilder.AppendLine("【要人市场说明】以下市场库存来自当前" + notableMarketTypeLabel + "市场总库存，不是私人财物；成交时扣" + notableMarketTypeLabel + "库存。");
			stringBuilder.AppendLine("库存物品：");
			if (notableMarketItems != null && notableMarketItems.Count > 0)
			{
				foreach (RewardItemInfo marketItem in notableMarketItems)
				{
					stringBuilder.Append(marketItem.Name)
						.Append(" | ");
					AppendItemTypeField(stringBuilder, marketItem.Item);
					stringBuilder.Append(" | ")
						.Append(marketItem.Count)
						.Append(GetItemQuantityUnit(marketItem.Item));
					if (!string.IsNullOrWhiteSpace(marketItem.PromptStringId))
					{
						stringBuilder.Append(" | token=").Append(marketItem.PromptStringId);
					}
					if (includeGuidePrice && TryGetSettlementBuyPrice(notableMarketSettlement, marketItem.EquipmentElement, out var marketPrice))
					{
						stringBuilder.Append(" | guidePrice=").Append(Math.Max(1, marketPrice));
					}
					stringBuilder.AppendLine();
					num++;
					if (num >= maxItems)
					{
						break;
					}
				}
			}
			if (notableMarketItems == null || notableMarketItems.Count == 0)
			{
				stringBuilder.AppendLine("（你什么都不卖，请不要虚构任何物品。）");
			}
		}
		if (heroInventoryItems != null && heroInventoryItems.Count > 0)
		{
			stringBuilder.AppendLine(isNotableMarketHero ? "个人库存物品：" : "库存物品：");
			foreach (RewardItemInfo item in heroInventoryItems.OrderByDescending((RewardItemInfo i) => i.Count))
			{
				stringBuilder.Append(item.Name)
					.Append(" | ");
				AppendItemTypeField(stringBuilder, item.Item);
				stringBuilder.Append(" | ")
					.Append(item.Count)
					.Append(GetItemQuantityUnit(item.Item));
				if (includeGuidePrice)
				{
					string key = item.StringId ?? "";
					if (!dictionary.TryGetValue(key, out var value))
					{
						ItemGuidePriceInfo itemGuidePriceInfo = (dictionary[key] = GetGuidePriceForItemNearHero(hero, item.Item));
						value = itemGuidePriceInfo;
					}
					stringBuilder.Append(" | guidePrice=").Append(Math.Max(1, value.UnitPrice));
				}
				stringBuilder.AppendLine();
				num++;
				if (num >= maxItems)
				{
					break;
				}
			}
		}
		if (includePrivateBattleEquipment && heroBattleEquipmentItems != null && heroBattleEquipmentItems.Count > 0)
		{
			int num2 = 0;
			stringBuilder.AppendLine(text + "的私人战斗装备:");
			foreach (RewardItemInfo item2 in heroBattleEquipmentItems.OrderByDescending((RewardItemInfo i) => i.Count))
			{
				stringBuilder.Append(item2.Name)
					.Append(" | ");
				AppendItemTypeField(stringBuilder, item2.Item);
				stringBuilder.Append(" | ")
					.Append(item2.Count)
					.Append(GetItemQuantityUnit(item2.Item));
				if (includeGuidePrice)
				{
					string key2 = item2.StringId ?? "";
					if (!dictionary.TryGetValue(key2, out var value2))
					{
						ItemGuidePriceInfo itemGuidePriceInfo = (dictionary[key2] = GetGuidePriceForItemNearHero(hero, item2.Item));
						value2 = itemGuidePriceInfo;
					}
					stringBuilder.Append(" | guidePrice=").Append(Math.Max(1, value2.UnitPrice));
				}
				stringBuilder.AppendLine();
				num2++;
				if (num2 >= Math.Max(1, maxPrivateEquipmentItems))
				{
					break;
				}
			}
			ICampaignMission current3 = CampaignMission.Current;
			if (current3 != null && current3.Location != null)
			{
				string text2 = current3.Location.StringId ?? string.Empty;
				switch (text2)
				{
				default:
					if (!(text2 == "tavern"))
					{
						break;
					}
					goto case "center";
				case "center":
				case "lordshall":
				case "castle":
					stringBuilder.AppendLine("【" + text + "的战斗装备栏说明】当前" + text + "所在的是城镇、领主大厅或城堡等日常场景，外表通常只穿着日常衣物，玩家无法直接看见这些战斗装备。你可以把这些武器盔甲理解为" + text + "随身携带的备用武装，可结合当下关系与谈判情况，酌情决定是否将其作为赌注或交易物品。");
					break;
				}
			}
		}
		return stringBuilder.ToString();
	}

	public List<DuelStakeOption> BuildDuelStakeOptionsForAI(Hero hero, int maxItems = 12)
	{
		List<DuelStakeOption> list = new List<DuelStakeOption>();
		if (hero == null)
		{
			return list;
		}
		try
		{
			List<DuelStakeOption> buildOptions(IEnumerable<RewardItemInfo> source, bool isPrivateEquipment)
			{
				Dictionary<string, DuelStakeOption> dictionary = new Dictionary<string, DuelStakeOption>(System.StringComparer.OrdinalIgnoreCase);
				if (source == null)
				{
					return new List<DuelStakeOption>();
				}
				foreach (RewardItemInfo item in source)
				{
					if (item == null || item.Item == null || item.Count <= 0)
					{
						continue;
					}
					string text = (item.StringId ?? item.Item.StringId ?? "").Trim();
					if (string.IsNullOrWhiteSpace(text))
					{
						continue;
					}
					if (!dictionary.TryGetValue(text, out var value))
					{
						ItemGuidePriceInfo guidePriceForItemNearHero = GetGuidePriceForItemNearHero(hero, item.Item);
						value = new DuelStakeOption
						{
							ItemId = text,
							Name = ((item.Name ?? item.Item.Name?.ToString() ?? text).Trim()),
							Count = 0,
							GuidePrice = System.Math.Max(1, guidePriceForItemNearHero.UnitPrice),
							Item = item.Item,
							IsPrivateEquipment = isPrivateEquipment
						};
						dictionary[text] = value;
					}
					value.Count += System.Math.Max(1, item.Count);
				}
				return dictionary.Values.Where((DuelStakeOption x) => x != null && x.Count > 0 && !string.IsNullOrWhiteSpace(x.Name)).OrderByDescending((DuelStakeOption x) => (long)System.Math.Max(1, x.GuidePrice) * System.Math.Max(1, x.Count)).ThenByDescending((DuelStakeOption x) => x.GuidePrice).ThenBy((DuelStakeOption x) => x.Name ?? "", System.StringComparer.OrdinalIgnoreCase).ToList();
			}
			List<DuelStakeOption> list2 = buildOptions(GetHeroInventoryItems(hero), isPrivateEquipment: false);
			List<DuelStakeOption> list3 = buildOptions(GetHeroBattleEquipmentItems(hero), isPrivateEquipment: true);
			list = list2.Concat(list3).Take(System.Math.Max(1, maxItems)).ToList();
		}
		catch
		{
			list = new List<DuelStakeOption>();
		}
		return list;
	}

	public string BuildDuelStakeSummaryForAI(Hero hero, out List<DuelStakeOption> options, int maxItems = 12)
	{
		options = BuildDuelStakeOptionsForAI(hero, maxItems);
		if (options == null || options.Count <= 0)
		{
			if (GetHeroGold(hero) > 0)
			{
				return "";
			}
			return "【可作为决斗赌注的物品】\n当前没有可用于赌注的财物。";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【可作为决斗赌注的物品】");
		List<DuelStakeOption> list = options.Where((DuelStakeOption x) => x != null && !x.IsPrivateEquipment).ToList();
		List<DuelStakeOption> list2 = options.Where((DuelStakeOption x) => x != null && x.IsPrivateEquipment).ToList();
		if (list.Count > 0)
		{
			stringBuilder.AppendLine("库存物品：");
			foreach (DuelStakeOption option in list)
			{
				stringBuilder.Append("- ")
					.Append(option.Name ?? option.ItemId ?? "未知物品")
					.Append(" | type=")
					.Append(GetItemPromptTypeLabel(option.Item))
					.Append(" | guidePrice=")
					.Append(System.Math.Max(1, option.GuidePrice))
					.Append(" | ")
					.Append(System.Math.Max(1, option.Count))
					.Append(GetItemQuantityUnit(option.Item))
					.AppendLine();
			}
		}
		if (list2.Count > 0)
		{
			stringBuilder.AppendLine("私人装备：");
			foreach (DuelStakeOption option2 in list2)
			{
				stringBuilder.Append("- ")
					.Append(option2.Name ?? option2.ItemId ?? "未知物品")
					.Append(" | type=")
					.Append(GetItemPromptTypeLabel(option2.Item))
					.Append(" | guidePrice=")
					.Append(System.Math.Max(1, option2.GuidePrice))
					.Append(" | ")
					.Append(System.Math.Max(1, option2.Count))
					.Append(GetItemQuantityUnit(option2.Item))
					.AppendLine();
			}
		}
		return stringBuilder.ToString().TrimEnd();
	}

	public string BuildDebtHintForAI(Hero npc)
	{
		DebtRecord debtRecord = GetDebtRecord(npc);
		if (debtRecord == null)
		{
			return string.Empty;
		}
		NormalizeDebtRecord(debtRecord);
		if (!HasDebtContent(debtRecord))
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【系统账目提示】玩家对你有以下承诺或欠款（分笔记录）：");
		if (debtRecord.DebtLines != null)
		{
			List<DebtRecord.DebtLine> list = (from x in debtRecord.DebtLines
				where x != null && x.RemainingAmount > 0
				orderby x.DueDay, x.CreatedDay
				select x).ToList();
			for (int num = 0; num < list.Count; num++)
			{
				DebtRecord.DebtLine debtLine = list[num];
				int debtValue = EstimateDebtLineRemainingValue(npc, debtLine);
				string deadline = BuildDebtPromiseDeadlineText(debtLine.DueDay, debtLine.IsDueUnlimited);
				string note = string.IsNullOrWhiteSpace(debtLine.DebtNote) ? "无" : debtLine.DebtNote;
				stringBuilder.Append("- [债务ID:").Append(debtLine.DebtId).Append("] 玩家的承诺或欠款价值 ")
					.Append(debtValue)
					.Append(" 第纳尔，达成期限为：")
					.Append(deadline)
					.Append("，备注：")
					.Append(note)
					.AppendLine();
			}
		}
		return stringBuilder.ToString().Trim();
	}

	public string BuildSettlementMerchantDebtHintForAI(CharacterObject character, Settlement settlement = null)
	{
		if (!TryGetSettlementMerchantKind(character, out var kind))
		{
			return "";
		}
		settlement = settlement ?? Settlement.CurrentSettlement;
		DebtRecord settlementMerchantDebtRecord = GetSettlementMerchantDebtRecord(settlement, kind);
		if (settlementMerchantDebtRecord == null)
		{
			return "";
		}
		NormalizeDebtRecord(settlementMerchantDebtRecord);
		if (!HasDebtContent(settlementMerchantDebtRecord))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【系统账目提示】玩家对你代表的" + BuildSettlementMerchantDebtLabel(settlement, kind) + "有以下承诺或欠款（分笔记录）：");
		stringBuilder.AppendLine("【债务解除确认】若玩家本轮行为已被系统事实明确记录为偿还、豁免或免除");
		List<DebtRecord.DebtLine> list = (from x in settlementMerchantDebtRecord.DebtLines
			where x != null && x.RemainingAmount > 0
			orderby x.DueDay, x.CreatedDay
			select x).ToList();
		for (int i = 0; i < list.Count; i++)
		{
			DebtRecord.DebtLine debtLine = list[i];
			int debtValue = EstimateDebtLineRemainingValueForSettlement(settlement, debtLine);
			string deadline = BuildDebtPromiseDeadlineText(debtLine.DueDay, debtLine.IsDueUnlimited);
			string note = string.IsNullOrWhiteSpace(debtLine.DebtNote) ? "无" : debtLine.DebtNote;
			stringBuilder.Append("- [债务ID:").Append(debtLine.DebtId).Append("] 玩家的承诺或欠款价值 ")
				.Append(debtValue)
				.Append(" 第纳尔，达成期限为：")
				.Append(deadline)
				.Append("，备注：")
				.Append(note)
				.AppendLine();
		}
		return stringBuilder.ToString().Trim();
	}

	public string BuildDebtEditorSummary(Hero npc, int maxLines = 12)
	{
		try
		{
			if (maxLines < 1)
			{
				maxLines = 1;
			}
			DebtRecord debtRecord = GetDebtRecord(npc);
			if (debtRecord == null)
			{
				return "当前无欠款。";
			}
			NormalizeDebtRecord(debtRecord);
			if (!HasDebtContent(debtRecord))
			{
				return "当前无欠款。";
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("金币总欠款：").Append(debtRecord.OwedGold).AppendLine();
			if (debtRecord.OwedItems != null && debtRecord.OwedItems.Count > 0)
			{
				List<string> list = new List<string>();
				foreach (KeyValuePair<string, int> owedItem in debtRecord.OwedItems)
				{
					if (!string.IsNullOrWhiteSpace(owedItem.Key) && owedItem.Value > 0)
					{
						list.Add(owedItem.Key + "x" + owedItem.Value);
					}
				}
				if (list.Count > 0)
				{
					stringBuilder.AppendLine("物品总欠款：" + string.Join("，", list));
				}
			}
			List<DebtRecord.DebtLine> list2 = (from x in debtRecord.DebtLines?.Where((DebtRecord.DebtLine x) => x != null && x.RemainingAmount > 0)
				orderby x.IsDueUnlimited ? 1 : 0, x.DueDay, x.CreatedDay
				select x).ToList() ?? new List<DebtRecord.DebtLine>();
			stringBuilder.Append("分笔未清：").Append(list2.Count).Append(" 笔")
				.AppendLine();
			int num = 0;
			for (int num2 = 0; num2 < list2.Count; num2++)
			{
				if (num >= maxLines)
				{
					break;
				}
				DebtRecord.DebtLine debtLine = list2[num2];
				string value = BuildDebtDueStatusText(debtLine.DueDay, debtLine.IsDueUnlimited);
				string value2 = (debtLine.IsGold ? "金币" : ("物品:" + debtLine.ItemId));
				string value3 = (debtLine.IsGold ? (debtLine.RemainingAmount + " 第纳尔") : ("x" + debtLine.RemainingAmount));
				stringBuilder.Append("- [").Append(debtLine.DebtId).Append("] ")
					.Append(value2)
					.Append("，剩余 ")
					.Append(value3);
				if (!string.IsNullOrWhiteSpace(value))
				{
					stringBuilder.Append("，").Append(value);
				}
				if (!debtLine.IsGold && debtLine.IsItemUnavailableDeclared)
				{
					stringBuilder.Append("，已标记无法归还原物");
				}
				stringBuilder.AppendLine();
				num++;
			}
			if (list2.Count > num)
			{
				stringBuilder.Append("... 还有 ").Append(list2.Count - num).Append(" 笔未显示。");
			}
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "欠款摘要读取失败。";
		}
	}

	private DebtRecord GetOrCreateDebtRecord(Hero npc)
	{
		if (npc == null)
		{
			return null;
		}
		string stringId = npc.StringId;
		if (string.IsNullOrEmpty(stringId))
		{
			return null;
		}
		if (_debts == null)
		{
			_debts = new Dictionary<string, DebtRecord>();
		}
		if (!_debts.TryGetValue(stringId, out var value))
		{
			value = new DebtRecord();
			_debts[stringId] = value;
		}
		return value;
	}

	private DebtRecord GetDebtRecord(Hero npc)
	{
		if (npc == null)
		{
			return null;
		}
		string stringId = npc.StringId;
		if (string.IsNullOrEmpty(stringId))
		{
			return null;
		}
		if (_debts == null)
		{
			_debts = new Dictionary<string, DebtRecord>();
		}
		if (_debts.TryGetValue(stringId, out var value))
		{
			return value;
		}
		return null;
	}

	private DebtRecord GetDebtRecordByKey(string debtKey)
	{
		if (string.IsNullOrWhiteSpace(debtKey))
		{
			return null;
		}
		if (_debts == null)
		{
			_debts = new Dictionary<string, DebtRecord>();
		}
		if (_debts.TryGetValue(debtKey, out var value))
		{
			return value;
		}
		return null;
	}

	private DebtRecord GetOrCreateDebtRecordByKey(string debtKey)
	{
		if (string.IsNullOrWhiteSpace(debtKey))
		{
			return null;
		}
		if (_debts == null)
		{
			_debts = new Dictionary<string, DebtRecord>();
		}
		if (!_debts.TryGetValue(debtKey, out var value) || value == null)
		{
			value = new DebtRecord();
			_debts[debtKey] = value;
		}
		return value;
	}

	private DebtRecord GetSettlementMerchantDebtRecord(Settlement settlement, SettlementMerchantKind kind)
	{
		return GetDebtRecordByKey(BuildSettlementMerchantDebtKey(settlement, kind));
	}

	public bool HasUnpaidDebtForInteraction(Hero targetHero, CharacterObject targetCharacter = null, Settlement settlement = null)
	{
		Hero hero = targetHero ?? targetCharacter?.HeroObject;
		if (hero != null && HasUnpaidDebt(hero))
		{
			return true;
		}
		if (targetCharacter == null || !TryGetSettlementMerchantKind(targetCharacter, out var kind))
		{
			return false;
		}
		DebtRecord settlementMerchantDebtRecord = GetSettlementMerchantDebtRecord(settlement ?? Settlement.CurrentSettlement, kind);
		if (settlementMerchantDebtRecord == null)
		{
			return false;
		}
		NormalizeDebtRecord(settlementMerchantDebtRecord);
		return HasDebtContent(settlementMerchantDebtRecord);
	}

	public bool HasUnpaidDebt(Hero npc)
	{
		DebtRecord debtRecord = GetDebtRecord(npc);
		if (debtRecord == null)
		{
			return false;
		}
		NormalizeDebtRecord(debtRecord);
		if (debtRecord.OwedGold > 0)
		{
			return true;
		}
		if (debtRecord.OwedItems == null)
		{
			return false;
		}
		foreach (KeyValuePair<string, int> owedItem in debtRecord.OwedItems)
		{
			if (owedItem.Value > 0)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsDebtOverdue(Hero npc)
	{
		DebtRecord debtRecord = GetDebtRecord(npc);
		if (debtRecord == null)
		{
			return false;
		}
		NormalizeDebtRecord(debtRecord);
		if (!HasDebtContent(debtRecord) || debtRecord.DueDay <= 0f)
		{
			return false;
		}
		return GetNowCampaignDay() > debtRecord.DueDay + 0.01f;
	}

	public int GetDebtDaysToDue(Hero npc)
	{
		DebtRecord debtRecord = GetDebtRecord(npc);
		if (debtRecord == null)
		{
			return 0;
		}
		NormalizeDebtRecord(debtRecord);
		if (!HasDebtContent(debtRecord) || debtRecord.DueDay <= 0f)
		{
			return 0;
		}
		float num = debtRecord.DueDay - GetNowCampaignDay();
		if (num >= 0f)
		{
			return (int)Math.Ceiling(num);
		}
		return -(int)Math.Ceiling(0f - num);
	}

	public void GetDebtSnapshot(Hero npc, out int owedGold, out Dictionary<string, int> owedItems)
	{
		owedGold = 0;
		owedItems = new Dictionary<string, int>();
		DebtRecord debtRecord = GetDebtRecord(npc);
		if (debtRecord == null)
		{
			return;
		}
		NormalizeDebtRecord(debtRecord);
		owedGold = debtRecord.OwedGold;
		if (debtRecord.OwedItems == null)
		{
			return;
		}
		foreach (KeyValuePair<string, int> owedItem in debtRecord.OwedItems)
		{
			if (owedItem.Value > 0)
			{
				owedItems[owedItem.Key] = owedItem.Value;
			}
		}
	}

	public void SetDebt(Hero npc, int owedGold, Dictionary<string, int> owedItems, float dueDay = 0f)
	{
		if (npc == null)
		{
			return;
		}
		if (_debts == null)
		{
			_debts = new Dictionary<string, DebtRecord>();
		}
		string stringId = npc.StringId;
		if (string.IsNullOrEmpty(stringId))
		{
			return;
		}
		if (!_debts.TryGetValue(stringId, out var value))
		{
			value = new DebtRecord();
			_debts[stringId] = value;
		}
		value.OwedGold = 0;
		if (value.OwedItems == null)
		{
			value.OwedItems = new Dictionary<string, int>();
		}
		else
		{
			value.OwedItems.Clear();
		}
		value.CreatedDay = 0f;
		value.DueDay = 0f;
		float nowCampaignDay = GetNowCampaignDay();
		float dueDay2 = ((dueDay > 0f) ? dueDay : (nowCampaignDay + 1f));
		value.DebtLines = new List<DebtRecord.DebtLine>();
		if (owedGold > 0)
		{
			value.DebtLines.Add(new DebtRecord.DebtLine
			{
				DebtId = BuildDebtId(),
				IsGold = true,
				ItemId = null,
				IsDueUnlimited = false,
				IsItemUnavailableDeclared = false,
				InitialAmount = Math.Max(0, owedGold),
				RemainingAmount = Math.Max(0, owedGold),
				CreatedDay = nowCampaignDay,
				DueDay = dueDay2,
				BestPreDueCoverage = 0f,
				OnTimePenaltyTierApplied = 0,
				OverduePenaltyDaysApplied = 0,
				LastOverduePenaltyDay = -1,
				OverdueTrustPenaltyPerDay = 0,
				OverdueRelationPenaltyPerDay = 0,
				CompensationUnitPrice = 0,
				CompensationGoldCredit = 0
			});
		}
		if (owedItems != null)
		{
			foreach (KeyValuePair<string, int> owedItem in owedItems)
			{
				if (!string.IsNullOrWhiteSpace(owedItem.Key) && owedItem.Value > 0)
				{
					value.DebtLines.Add(new DebtRecord.DebtLine
					{
						DebtId = BuildDebtId(),
						IsGold = false,
						ItemId = owedItem.Key,
						IsDueUnlimited = false,
						IsItemUnavailableDeclared = false,
						InitialAmount = owedItem.Value,
						RemainingAmount = owedItem.Value,
						CreatedDay = nowCampaignDay,
						DueDay = dueDay2,
						BestPreDueCoverage = 0f,
						OnTimePenaltyTierApplied = 0,
						OverduePenaltyDaysApplied = 0,
						LastOverduePenaltyDay = -1,
						OverdueTrustPenaltyPerDay = 0,
						OverdueRelationPenaltyPerDay = 0,
						CompensationUnitPrice = 0,
						CompensationGoldCredit = 0
					});
				}
			}
		}
		NormalizeDebtRecord(value);
		if (!HasDebtContent(value))
		{
			_debts.Remove(stringId);
			return;
		}
		// Public SetDebt callers bypass AD parsing; queue only their new lines instead of scanning every debtor.
		for (int i = 0; i < value.DebtLines.Count; i++)
		{
			DebtRecord.DebtLine debtLine = value.DebtLines[i];
			if (debtLine != null && debtLine.RemainingAmount > 0)
			{
				QueueDebtPromiseQuest(stringId, debtLine.DebtId);
			}
		}
	}

	private DebtRecord.DebtLine SetDebtForNpc(Hero npc, int debtValue, int dueDays, string debtNote)
	{
		if (npc == null || debtValue <= 0)
		{
			return null;
		}
		DebtRecord orCreateDebtRecord = GetOrCreateDebtRecord(npc);
		if (orCreateDebtRecord == null)
		{
			return null;
		}
		NormalizeDebtRecord(orCreateDebtRecord);
		float nowCampaignDay = GetNowCampaignDay();
		int campaignDayIndex = GetCampaignDayIndex();
		bool dueUnlimited = dueDays <= 0;
		float dueDay = dueUnlimited ? 0f : nowCampaignDay + (float)NormalizeDueDays(dueDays);
		if (!dueUnlimited && dueDay <= 0f)
		{
			dueDay = nowCampaignDay + 1f;
		}
		DebtRecord.DebtLine debtLine = new DebtRecord.DebtLine
		{
			DebtId = BuildDebtId(),
			IsGold = true,
			ItemId = null,
			IsDueUnlimited = dueUnlimited,
			IsItemUnavailableDeclared = false,
			InitialAmount = debtValue,
			RemainingAmount = debtValue,
			CreatedDay = nowCampaignDay,
			DueDay = dueDay,
			BestPreDueCoverage = 0f,
			OnTimePenaltyTierApplied = 0,
			OverduePenaltyDaysApplied = 0,
			LastOverduePenaltyDay = dueUnlimited ? campaignDayIndex : -1,
			OverdueTrustPenaltyPerDay = 0,
			OverdueRelationPenaltyPerDay = 0,
			CompensationUnitPrice = 0,
			CompensationGoldCredit = 0,
			UnlimitedTrustPenaltyNumeratorCarry = 0L,
			DebtNote = NormalizeDebtNote(debtNote)
		};
		orCreateDebtRecord.DebtLines.Add(debtLine);
		NormalizeDebtRecord(orCreateDebtRecord);
		// Defer QuestBase.StartQuest until the current AD-tag conversation has finished naturally.
		QueueDebtPromiseQuest(npc.StringId, debtLine.DebtId);
		return debtLine;
	}

	public bool RecordDeferredDuelDebtForNpc(Hero npc, int goldAmount, int dueDays, string debtNote, out string debtId, out string dueStatusText)
	{
		debtId = "";
		dueStatusText = "";
		try
		{
			DebtRecord.DebtLine debtLine = SetDebtForNpc(npc, goldAmount, dueDays, debtNote);
			if (debtLine == null)
			{
				return false;
			}
			debtId = debtLine.DebtId ?? "";
			dueStatusText = BuildDebtDueStatusText(debtLine.DueDay, debtLine.IsDueUnlimited) ?? "";
			return true;
		}
		catch
		{
			debtId = "";
			dueStatusText = "";
			return false;
		}
	}

	private DebtRecord.DebtLine SetDebtForSettlementMerchant(Settlement settlement, SettlementMerchantKind kind, int debtValue, int dueDays, string debtNote)
	{
		if (settlement == null || kind == SettlementMerchantKind.None || debtValue <= 0)
		{
			return null;
		}
		string settlementMerchantDebtKey = BuildSettlementMerchantDebtKey(settlement, kind);
		DebtRecord orCreateDebtRecordByKey = GetOrCreateDebtRecordByKey(settlementMerchantDebtKey);
		if (orCreateDebtRecordByKey == null)
		{
			return null;
		}
		NormalizeDebtRecord(orCreateDebtRecordByKey);
		float nowCampaignDay = GetNowCampaignDay();
		int campaignDayIndex = GetCampaignDayIndex();
		bool dueUnlimited = dueDays <= 0;
		float dueDay = dueUnlimited ? 0f : nowCampaignDay + (float)NormalizeDueDays(dueDays);
		if (!dueUnlimited && dueDay <= 0f)
		{
			dueDay = nowCampaignDay + 1f;
		}
		DebtRecord.DebtLine debtLine = new DebtRecord.DebtLine
		{
			DebtId = BuildDebtId(),
			IsGold = true,
			ItemId = null,
			IsDueUnlimited = dueUnlimited,
			IsItemUnavailableDeclared = false,
			InitialAmount = debtValue,
			RemainingAmount = debtValue,
			CreatedDay = nowCampaignDay,
			DueDay = dueDay,
			BestPreDueCoverage = 0f,
			OnTimePenaltyTierApplied = 0,
			OverduePenaltyDaysApplied = 0,
			LastOverduePenaltyDay = dueUnlimited ? campaignDayIndex : -1,
			OverdueTrustPenaltyPerDay = 0,
			OverdueRelationPenaltyPerDay = 0,
			CompensationUnitPrice = 0,
			CompensationGoldCredit = 0,
			UnlimitedTrustPenaltyNumeratorCarry = 0L,
			DebtNote = NormalizeDebtNote(debtNote)
		};
		orCreateDebtRecordByKey.DebtLines.Add(debtLine);
		NormalizeDebtRecord(orCreateDebtRecordByKey);
		// Market debts use the same deferred queue so all three conversation channels create tasks safely.
		QueueDebtPromiseQuest(settlementMerchantDebtKey, debtLine.DebtId);
		return debtLine;
	}

	private bool TryFindDebtLineById(Hero npc, string debtId, out DebtRecord rec, out DebtRecord.DebtLine line, out string statusText)
	{
		statusText = "";
		rec = null;
		line = null;
		if (npc == null || string.IsNullOrWhiteSpace(debtId))
		{
			statusText = "参数无效：缺少NPC或债务ID。";
			return false;
		}
		rec = GetDebtRecord(npc);
		if (rec == null)
		{
			statusText = "未找到该NPC的债务记录。";
			return false;
		}
		NormalizeDebtRecord(rec);
		if (rec.DebtLines == null || rec.DebtLines.Count <= 0)
		{
			statusText = "该NPC当前没有可还债务。";
			return false;
		}
		line = rec.DebtLines.FirstOrDefault((DebtRecord.DebtLine x) => x != null && string.Equals(x.DebtId ?? "", debtId.Trim(), StringComparison.OrdinalIgnoreCase));
		if (line == null || line.RemainingAmount <= 0)
		{
			statusText = "未找到可结算的债务ID，或该债务已清。";
			return false;
		}
		return true;
	}

	public bool ResolveDebtByIdByAgreement(Hero npc, string debtId, out string statusText)
	{
		statusText = "";
		if (!TryFindDebtLineById(npc, debtId, out var rec, out var line, out statusText))
		{
			return false;
		}
		int remainingAmount = Math.Max(0, line.RemainingAmount);
		line.RemainingAmount = 0;
		NormalizeDebtRecord(rec);
		// Complete this exact debt line's task even when the same NPC still has other unpaid lines.
		CompleteDebtPromiseQuest(npc.StringId, line.DebtId);
		statusText = $"债务ID {line.DebtId} 已按协商解除（{remainingAmount} -> 0）。";
		if (!HasDebtContent(rec) && !string.IsNullOrWhiteSpace(npc?.StringId))
		{
			_debts.Remove(npc.StringId);
			return true;
		}
		return false;
	}

	private bool TryFindSettlementMerchantDebtLineById(Settlement settlement, SettlementMerchantKind kind, string debtId, out DebtRecord rec, out DebtRecord.DebtLine line, out string statusText)
	{
		statusText = "";
		rec = null;
		line = null;
		if (settlement == null || kind == SettlementMerchantKind.None || string.IsNullOrWhiteSpace(debtId))
		{
			statusText = "参数无效：缺少市场债主或债务ID。";
			return false;
		}
		rec = GetSettlementMerchantDebtRecord(settlement, kind);
		if (rec == null)
		{
			statusText = "未找到该市场的债务记录。";
			return false;
		}
		NormalizeDebtRecord(rec);
		line = rec.DebtLines.FirstOrDefault((DebtRecord.DebtLine x) => x != null && string.Equals(x.DebtId ?? "", debtId.Trim(), StringComparison.OrdinalIgnoreCase));
		if (line == null || line.RemainingAmount <= 0)
		{
			statusText = "未找到可结算的债务ID，或该债务已清。";
			return false;
		}
		return true;
	}

	public bool ResolveSettlementMerchantDebtByIdByAgreement(Settlement settlement, SettlementMerchantKind kind, string debtId, out string statusText)
	{
		statusText = "";
		if (!TryFindSettlementMerchantDebtLineById(settlement, kind, debtId, out var rec, out var line, out statusText))
		{
			return false;
		}
		int remainingAmount = Math.Max(0, line.RemainingAmount);
		line.RemainingAmount = 0;
		NormalizeDebtRecord(rec);
		// Market obligations also complete by line ID, rather than waiting for the whole market balance to be cleared.
		CompleteDebtPromiseQuest(BuildSettlementMerchantDebtKey(settlement, kind), line.DebtId);
		statusText = $"债务ID {line.DebtId} 已按协商解除（{remainingAmount} -> 0）。";
		if (!HasDebtContent(rec))
		{
			_debts.Remove(BuildSettlementMerchantDebtKey(settlement, kind));
			return true;
		}
		return false;
	}

	private static string StripHeroTradeActionTags(string text)
	{
		string text2 = text ?? "";
		text2 = GiveAssetTagCodec.StripTags(text2);

		text2 = Regex.Replace(text2, "\\[ACTION:TRADE_TRUST:[^\\]]*\\]", string.Empty, RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[AD:[^\\]]*\\]", string.Empty, RegexOptions.IgnoreCase);
		text2 = Regex.Replace(text2, "\\[ADP:[^\\]]*\\]", string.Empty, RegexOptions.IgnoreCase);
		return text2.Trim();
	}

	private static bool ShouldApplyAdDebtTag(Match match, string source)
	{
		string direction = "";
		if (match != null && match.Groups.Count > 3)
		{
			direction = (match.Groups[3].Value ?? "").Trim();
		}
		if (string.Equals(direction, "N", StringComparison.OrdinalIgnoreCase))
		{
			Logger.Log("Logic", "[Reward] AD tag ignored: direction=N means NPC owes player; no player debt is created. source=" + (source ?? ""));
			return false;
		}
		return true;
	}

	internal static bool IsCanonicalDebtActionTagForExternal(string tag)
	{
		string text = (tag ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		Match match = DebtCreationTagRegex.Match(text);
		if (match.Success && match.Index == 0 && match.Length == text.Length)
		{
			return true;
		}
		match = DebtResolutionTagRegex.Match(text);
		return match.Success && match.Index == 0 && match.Length == text.Length;
	}

	internal static bool ContainsCanonicalDebtActionTagForExternal(string text)
	{
		string value = text ?? "";
		return DebtCreationTagRegex.IsMatch(value) || DebtResolutionTagRegex.IsMatch(value);
	}

	private static string GetAdDebtNote(Match match)
	{
		if (match == null)
		{
			return "";
		}
		if (match.Groups.Count > 4)
		{
			return (match.Groups[4].Value ?? "").Trim();
		}
		return "";
	}

	private static string GetRewardItemTransferKey(RewardItemInfo item)
	{
		string key = (item?.PromptStringId ?? "").Trim();
		return string.IsNullOrWhiteSpace(key) ? (item?.StringId ?? "").Trim() : key;
	}

	public static bool IsGoldAssetTokenForExternal(string token)
	{
		string text = (token ?? "").Trim();
		return string.Equals(text, "GOLD", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "钱", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "金币", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "第纳尔", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "DENAR", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "DENARS", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "MONEY", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "COIN", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(text, "COINS", StringComparison.OrdinalIgnoreCase);
	}

	public static bool TryResolveKnownItemAssetTokenForExternal(string assetToken, out string itemStringId)
	{
		itemStringId = "";
		string text = (assetToken ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || IsGoldAssetTokenForExternal(text) || TransferQuantitySpec.IsAllValue(text))
		{
			return false;
		}
		try
		{
			IEnumerable<ItemObject> items = Game.Current?.ObjectManager?.GetObjectTypeList<ItemObject>() ?? MBObjectManager.Instance?.GetObjectTypeList<ItemObject>();
			string matchedNameId = "";
			bool ambiguousName = false;
			foreach (ItemObject item in items ?? Enumerable.Empty<ItemObject>())
			{
				if (item == null || IsGeneratedRewardItemStringId(item.StringId))
				{
					continue;
				}
				string stringId = (item.StringId ?? "").Trim();
				if (string.Equals(stringId, text, StringComparison.OrdinalIgnoreCase))
				{
					itemStringId = stringId;
					return !string.IsNullOrWhiteSpace(itemStringId);
				}
				if (!string.Equals((item.Name?.ToString() ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				if (string.IsNullOrWhiteSpace(matchedNameId))
				{
					matchedNameId = stringId;
				}
				else if (!string.Equals(matchedNameId, stringId, StringComparison.OrdinalIgnoreCase))
				{
					ambiguousName = true;
				}
			}
			if (!ambiguousName && !string.IsNullOrWhiteSpace(matchedNameId))
			{
				itemStringId = matchedNameId;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	public static bool IsValidGeneratedRpAssetNameForExternal(string assetToken)
	{
		string text = (assetToken ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || text.Length > 160 || IsGoldAssetTokenForExternal(text) || TransferQuantitySpec.IsAllValue(text))
		{
			return false;
		}
		if (Regex.IsMatch(text, "^\\d+$", RegexOptions.CultureInvariant)
			|| text.IndexOfAny(new char[2] { '\r', '\n' }) >= 0)
		{
			return false;
		}
		// Finite GIVE_ASSET values are literal RP item labels. Keep every printable symbol,
		// including "[ROT]", and avoid world-wide entity scans on this postprocess path.
		// A real fixed asset is resolved first from its explicit runtime entry.
		return true;
	}

	private int GenerateRpAssetToPlayer(string assetName, int amount, string giverName, BasicCharacterObject giverCharacter, out string itemName, out ItemObject item, string logSource, RpItemIntroductionContext rpItemIntroductionContext = null, EconomyMutationObservation mutationObservation = null)
	{
		itemName = null;
		item = null;
		if (!IsValidGeneratedRpAssetNameForExternal(assetName) || amount <= 0)
		{
			Logger.Log("Logic", "[RewardRpLiteral] rejected source=" + (logSource ?? "") + " asset=" + (assetName ?? "") + " amount=" + amount + " reason=invalid_literal");
			return 0;
		}
		ItemRoster targetRoster = ResolveReceiverItemRosterForGive(Hero.MainHero);
		if (targetRoster == null)
		{
			Logger.Log("Logic", "[RewardRpLiteral] rejected source=" + (logSource ?? "") + " asset=" + assetName.Trim() + " reason=player_roster_null");
			return 0;
		}
		string requestedName = assetName.Trim();
		string preferredTemplateItemId = null;
		bool resolvedEquipmentTemplate = TryResolveGeneratedRpEquipmentTemplate(requestedName, out ItemObject equipmentTemplate, out GeneratedRpEquipmentKind equipmentKind, out string matchedSuffix, out float equipmentMatchScore, out int equipmentCandidateCount);
		if (resolvedEquipmentTemplate)
		{
			preferredTemplateItemId = equipmentTemplate.StringId;
			Logger.Log("Logic", "[RewardItemResolve] rp_equipment_template source=" + (logSource ?? "") + " asset=" + requestedName + " suffix=" + matchedSuffix + " kind=" + equipmentKind + " template=" + (equipmentTemplate.StringId ?? "") + " templateName=" + (equipmentTemplate.Name?.ToString() ?? "") + " score=" + FormatRewardItemResolutionScore(equipmentMatchScore) + " candidates=" + equipmentCandidateCount.ToString(CultureInfo.InvariantCulture));
		}
		else if (equipmentKind != GeneratedRpEquipmentKind.None)
		{
			Logger.Log("Logic", "[RewardItemResolve] rp_equipment_template_missing source=" + (logSource ?? "") + " asset=" + requestedName + " suffix=" + matchedSuffix + " kind=" + equipmentKind + " candidates=" + equipmentCandidateCount.ToString(CultureInfo.InvariantCulture) + " fallback=blocked");
		}
		else
		{
			bool resolvedFoodTemplate = TryResolveGeneratedRpFoodTemplate(requestedName, out ItemObject foodTemplate, out GeneratedRpFoodKind foodKind, out string foodMatchedSuffix, out float foodMatchScore, out int foodCandidateCount);
			if (resolvedFoodTemplate)
			{
				preferredTemplateItemId = foodTemplate.StringId;
				Logger.Log("Logic", "[RewardItemResolve] rp_food_template source=" + (logSource ?? "") + " asset=" + requestedName + " suffix=" + foodMatchedSuffix + " kind=" + foodKind + " template=" + (foodTemplate.StringId ?? "") + " templateName=" + (foodTemplate.Name?.ToString() ?? "") + " score=" + FormatRewardItemResolutionScore(foodMatchScore) + " candidates=" + foodCandidateCount.ToString(CultureInfo.InvariantCulture));
			}
			else if (foodKind != GeneratedRpFoodKind.None)
			{
				Logger.Log("Logic", "[RewardItemResolve] rp_food_template_missing source=" + (logSource ?? "") + " asset=" + requestedName + " suffix=" + foodMatchedSuffix + " kind=" + foodKind + " candidates=" + foodCandidateCount.ToString(CultureInfo.InvariantCulture) + " fallback=blocked");
			}
		}
		int generated = GenerateNamedInventoryItemToRosterForExternal(
			targetRoster,
			requestedName,
			amount,
			out var generatedStringId,
			out itemName,
			logSource,
			identityKey: null,
			preferredTemplateItemId: preferredTemplateItemId,
			mutationObservation: mutationObservation);
		Logger.Log("Logic", "[RewardRpLiteral] generated source=" + (logSource ?? "") + " asset=" + requestedName + " requested=" + amount + " actual=" + generated + " generatedId=" + (generatedStringId ?? ""));
		if (generated > 0 && !string.IsNullOrWhiteSpace(generatedStringId))
		{
			TryResolveGeneratedRewardItemForStringId(generatedStringId, out item, logSource + "_resolve");
			RpItemIntroductionContext effectiveIntroductionContext = rpItemIntroductionContext ?? CreateRpItemIntroductionContextForExternal(
				(giverCharacter as CharacterObject)?.HeroObject,
				null,
				giverName,
				null,
				null);
			QueueNpcRpItemIntroductionForExternal(generatedStringId, requestedName, effectiveIntroductionContext, logSource);
		}
		if (generated > 0)
		{
			itemName = requestedName;
			string sourceName = string.IsNullOrWhiteSpace(giverName) ? "对方" : giverName.Trim();
			string displayName = string.IsNullOrWhiteSpace(itemName) ? assetName.Trim() : itemName;
			ShowRewardMessage($"{sourceName} 给了你 {FormatItemAmount(generated, item, displayName)}。", giverCharacter);
		}
		return generated;
	}

	private static bool TryResolveExactAuthorizedRewardItem(IEnumerable<RewardItemInfo> authorizedItems, string assetToken, out RewardItemInfo item, out string transferKey)
	{
		item = null;
		transferKey = "";
		string token = (assetToken ?? "").Trim();
		if (string.IsNullOrWhiteSpace(token))
		{
			return false;
		}
		List<RewardItemInfo> candidates = (authorizedItems ?? Enumerable.Empty<RewardItemInfo>())
			.Where((RewardItemInfo x) => x != null && x.Item != null && x.Count > 0)
			.ToList();
		if (TrySelectAuthorizedRewardItem(candidates.Where((RewardItemInfo x) => string.Equals((x.PromptStringId ?? "").Trim(), token, StringComparison.OrdinalIgnoreCase)), allowSharedBaseItemKey: false, out item, out transferKey))
		{
			return true;
		}
		if (TrySelectAuthorizedRewardItem(candidates.Where((RewardItemInfo x) => string.Equals((x.StringId ?? x.Item?.StringId ?? "").Trim(), token, StringComparison.OrdinalIgnoreCase)), allowSharedBaseItemKey: true, out item, out transferKey))
		{
			return true;
		}
		return TrySelectAuthorizedRewardItem(candidates.Where((RewardItemInfo x) => string.Equals((x.Name ?? "").Trim(), token, StringComparison.OrdinalIgnoreCase)), allowSharedBaseItemKey: true, out item, out transferKey);
	}

	private static bool TryResolveAuthorizedRewardItem(IEnumerable<RewardItemInfo> authorizedItems, string assetToken, out RewardItemInfo item, out string transferKey)
	{
		item = null;
		transferKey = "";
		string token = (assetToken ?? "").Trim();
		if (string.IsNullOrWhiteSpace(token))
		{
			return false;
		}
		List<RewardItemInfo> candidates = (authorizedItems ?? Enumerable.Empty<RewardItemInfo>())
			.Where((RewardItemInfo x) => x != null && x.Item != null && x.Count > 0)
			.ToList();
		if (TrySelectAuthorizedRewardItem(candidates.Where((RewardItemInfo x) => string.Equals((x.PromptStringId ?? "").Trim(), token, StringComparison.OrdinalIgnoreCase)), allowSharedBaseItemKey: false, out item, out transferKey))
		{
			return true;
		}
		if (TrySelectAuthorizedRewardItem(candidates.Where((RewardItemInfo x) => string.Equals((x.StringId ?? x.Item?.StringId ?? "").Trim(), token, StringComparison.OrdinalIgnoreCase)), allowSharedBaseItemKey: true, out item, out transferKey))
		{
			return true;
		}
		if (TrySelectAuthorizedRewardItem(candidates.Where((RewardItemInfo x) => string.Equals((x.Name ?? "").Trim(), token, StringComparison.OrdinalIgnoreCase)), allowSharedBaseItemKey: true, out item, out transferKey))
		{
			return true;
		}
		string looseToken = Regex.Replace(token, "[\\s\\u3000]+", "").Replace("的", "");
		if (!string.IsNullOrWhiteSpace(looseToken)
			&& TrySelectAuthorizedRewardItem(candidates.Where((RewardItemInfo x) => string.Equals(Regex.Replace((x.Name ?? "").Trim(), "[\\s\\u3000]+", "").Replace("的", ""), looseToken, StringComparison.OrdinalIgnoreCase)), allowSharedBaseItemKey: true, out item, out transferKey))
		{
			return true;
		}
		return false;
	}

	private static bool TrySelectAuthorizedRewardItem(IEnumerable<RewardItemInfo> source, bool allowSharedBaseItemKey, out RewardItemInfo item, out string transferKey)
	{
		item = null;
		transferKey = "";
		List<RewardItemInfo> matches = (source ?? Enumerable.Empty<RewardItemInfo>()).Where((RewardItemInfo x) => x != null && x.Item != null && x.Count > 0).ToList();
		if (matches.Count == 0)
		{
			return false;
		}
		List<string> keys = matches.Select(GetRewardItemTransferKey).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		if (keys.Count != 1)
		{
			if (!allowSharedBaseItemKey)
			{
				return false;
			}
			List<string> baseItemKeys = matches.Select((RewardItemInfo x) => (x.StringId ?? x.Item?.StringId ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			if (baseItemKeys.Count != 1)
			{
				return false;
			}
			item = matches[0];
			transferKey = baseItemKeys[0];
			return true;
		}
		string selectedKey = keys[0];
		item = matches.FirstOrDefault((RewardItemInfo x) => string.Equals(GetRewardItemTransferKey(x), selectedKey, StringComparison.OrdinalIgnoreCase));
		transferKey = selectedKey;
		return item != null;
	}

	private bool TryResolveAuthorizedHeroRewardItem(Hero giver, string assetToken, out List<RewardItemInfo> authorizedItems, out string transferKey)
	{
		authorizedItems = GetHeroInventoryItems(giver);
		if (TryResolveExactAuthorizedRewardItem(authorizedItems, assetToken, out var _, out transferKey))
		{
			Logger.Log("Logic", "[Reward] GIVE_ASSET authorization_live source=hero_inventory token=" + (assetToken ?? "") + " liveCount=" + authorizedItems.Count + " resolved=True");
			return true;
		}
		authorizedItems = BuildHeroRewardPostprocessItems(giver);
		if (authorizedItems.Count > 0)
		{
			PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.NpcRewardItemsAllSnapshotScope, giver, giver?.CharacterObject, -1, authorizedItems);
		}
		bool resolved = TryResolveExactAuthorizedRewardItem(authorizedItems, assetToken, out var _, out transferKey);
		Logger.Log("Logic", "[Reward] GIVE_ASSET authorization_live source=hero token=" + (assetToken ?? "") + " liveCount=" + authorizedItems.Count + " resolved=" + resolved);
		return resolved;
	}

	private bool TryResolveAuthorizedPartyRewardItem(PartyBase giverParty, BasicCharacterObject giverCharacter, string assetToken, out List<RewardItemInfo> authorizedItems, out string transferKey)
	{
		authorizedItems = BuildPartyRewardPostprocessItems(giverParty);
		if (authorizedItems.Count > 0)
		{
			PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.PartyRewardItemsAllSnapshotScope, null, giverCharacter as CharacterObject, -1, authorizedItems);
		}
		bool resolved = TryResolveExactAuthorizedRewardItem(authorizedItems, assetToken, out var _, out transferKey);
		Logger.Log("Logic", "[RewardParty] GIVE_ASSET authorization_live token=" + (assetToken ?? "") + " liveCount=" + authorizedItems.Count + " resolved=" + resolved);
		return resolved;
	}

	private bool TryResolveAuthorizedMerchantRewardItem(CharacterObject giverCharacter, string assetToken, out List<RewardItemInfo> authorizedItems, out string transferKey)
	{
		authorizedItems = BuildSettlementMerchantPostprocessItems(giverCharacter);
		if (authorizedItems.Count > 0)
		{
			PromptListRetrievalService.PublishRewardItemSnapshot(PromptListRetrievalService.SettlementMerchantItemsAllSnapshotScope, null, giverCharacter, -1, authorizedItems);
		}
		bool resolved = TryResolveExactAuthorizedRewardItem(authorizedItems, assetToken, out var _, out transferKey);
		Logger.Log("Logic", "[RewardMerchant] GIVE_ASSET authorization_live token=" + (assetToken ?? "") + " liveCount=" + authorizedItems.Count + " resolved=" + resolved);
		return resolved;
	}

	private static bool TryResolveAuthorizedNpcFixedAsset(Hero giver, string assetToken, out MyBehavior.SettlementTransferPromptEntry entry)
	{
		entry = null;
		if (giver == null || string.IsNullOrWhiteSpace(assetToken))
		{
			return false;
		}
		if (!PromptListRetrievalService.TryGetSettlementTransferSnapshot(PromptListRetrievalService.SettlementTransferAllNpcAssetsSnapshotScope, giver, giver.CharacterObject, -1, out var authorizedEntries))
		{
			return false;
		}
		string token = assetToken.Trim();
		entry = (authorizedEntries ?? new List<MyBehavior.SettlementTransferPromptEntry>()).FirstOrDefault((MyBehavior.SettlementTransferPromptEntry x) => x != null &&
			(string.Equals(MyBehavior.GetSettlementTransferAssetIdForExternal(x), token, StringComparison.OrdinalIgnoreCase)
				|| string.Equals((x.AssetId ?? "").Trim(), token, StringComparison.OrdinalIgnoreCase)
				|| string.Equals((x.SettlementId ?? "").Trim(), token, StringComparison.OrdinalIgnoreCase)
				|| string.Equals((x.DisplayName ?? "").Trim(), token, StringComparison.OrdinalIgnoreCase)
				|| (x.PromptIndex > 0 && string.Equals(x.PromptIndex.ToString(), token, StringComparison.OrdinalIgnoreCase))));
		return entry != null;
	}

	private sealed class FixedAssetTokenResolution
	{
		public MyBehavior.SettlementTransferPromptEntry Entry;

		public bool IsPromptAuthorized;
	}

	/// <summary>
	/// Resolves a GIVE_ASSET token as a fixed asset. The prompt snapshot remains the first
	/// choice; only an exact, canonical runtime asset ID may use the global fallback. The
	/// per-response caches avoid repeated lookup/scans when a postprocess emits duplicate IDs.
	/// </summary>
	private static bool TryResolveFixedAssetTokenForGiveAsset(Hero giver, string assetToken, IDictionary<string, FixedAssetTokenResolution> fixedAssetResolutionCache, ISet<string> unresolvedFixedAssetTokens, out MyBehavior.SettlementTransferPromptEntry entry, out bool isPromptAuthorized)
	{
		entry = null;
		isPromptAuthorized = false;
		string token = (assetToken ?? "").Trim();
		if (giver == null || string.IsNullOrWhiteSpace(token))
		{
			return false;
		}
		if (fixedAssetResolutionCache != null && fixedAssetResolutionCache.TryGetValue(token, out FixedAssetTokenResolution cached) && cached?.Entry != null)
		{
			entry = cached.Entry;
			isPromptAuthorized = cached.IsPromptAuthorized;
			return true;
		}
		if (unresolvedFixedAssetTokens != null && unresolvedFixedAssetTokens.Contains(token))
		{
			return false;
		}
		if (TryResolveAuthorizedNpcFixedAsset(giver, token, out entry))
		{
			isPromptAuthorized = true;
			if (fixedAssetResolutionCache != null)
			{
				fixedAssetResolutionCache[token] = new FixedAssetTokenResolution
				{
					Entry = entry,
					IsPromptAuthorized = true
				};
			}
			return true;
		}
		if (MyBehavior.TryResolveFixedAssetTransferEntryByIdForExternal(token, out entry))
		{
			if (fixedAssetResolutionCache != null)
			{
				fixedAssetResolutionCache[token] = new FixedAssetTokenResolution
				{
					Entry = entry,
					IsPromptAuthorized = false
				};
			}
			Logger.Log("Logic", "[Reward] GIVE_ASSET fixed_asset_direct_id_resolved giver=" + (giver.StringId ?? "") + " asset=" + token + " kind=" + entry.AssetKind);
			return true;
		}
		unresolvedFixedAssetTokens?.Add(token);
		return false;
	}

	private int ResolveAllRewardItemAmount(string itemToken, IEnumerable<RewardItemInfo> contextItems)
	{
		string token = (itemToken ?? "").Trim();
		List<RewardItemInfo> items = (contextItems ?? Enumerable.Empty<RewardItemInfo>()).Where((RewardItemInfo x) => x != null && x.Item != null && x.Count > 0).ToList();
		if (string.IsNullOrWhiteSpace(token) || items.Count == 0)
		{
			return 0;
		}
		string resolvedKey = token;
		if (!items.Any((RewardItemInfo x) => string.Equals(GetRewardItemTransferKey(x), token, StringComparison.OrdinalIgnoreCase)) && TryResolveRewardItemByNameOrId(token, items, out var resolution, "all_count"))
		{
			resolvedKey = GetRewardItemTransferKey(resolution?.Info);
		}
		long total = items.Where((RewardItemInfo x) => string.Equals(GetRewardItemTransferKey(x), resolvedKey, StringComparison.OrdinalIgnoreCase)).Sum((RewardItemInfo x) => (long)Math.Max(0, x.Count));
		return (int)Math.Min(int.MaxValue, Math.Max(0L, total));
	}

	public void ApplyRewardTags(Hero giver, Hero receiver, ref string responseText, RpItemIntroductionContext rpItemIntroductionContext = null)
	{
		SetLastGeneratedNpcFactLines(null);
		if (giver == null || receiver == null || string.IsNullOrEmpty(responseText))
		{
			return;
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		try
		{
			Regex regex17 = new Regex("\\[ACTION:KINGDOM_SERVICE:(MERCENARY|VASSAL|LEAVE|CLAN_JOIN_PLAYER_KINGDOM|CLAN_JOIN_KINGDOM):([a-zA-Z0-9_.\\-]+)\\]", RegexOptions.IgnoreCase);
			Regex regex18 = new Regex("\\[ACTION:JOIN_MERCENARY:([a-zA-Z0-9_\\-]+)\\]", RegexOptions.IgnoreCase);
			Regex regex19 = new Regex("\\[ACTION:JOIN_VASSAL:([a-zA-Z0-9_\\-]+)\\]", RegexOptions.IgnoreCase);
			Regex regex20 = new Regex("\\[ACTION:KINGDOM_SERVICE:LEAVE\\]", RegexOptions.IgnoreCase);
			Regex regex21 = new Regex("\\[ACTION:TRADE_TRUST:(-?\\d+)\\]", RegexOptions.IgnoreCase);
			Regex regex23 = DebtCreationTagRegex;
			Regex regex24 = DebtResolutionTagRegex;
			Regex regex25 = HeroJoinPlayerPartyTagRegex;
			Regex regexClanJoinPlayerKingdom = new Regex("\\[A:C_J_P_K\\]", RegexOptions.IgnoreCase);
			Regex regexClanJoinKingdom = new Regex("\\[A:C_J_K:([a-zA-Z0-9_.\\-]+)\\]", RegexOptions.IgnoreCase);
			Regex regexPlayerJoinKingdomMercenary = new Regex("\\[A:P_J_K_M\\]", RegexOptions.IgnoreCase);
			Regex regexPlayerJoinKingdomVassal = new Regex("\\[A:P_J_K_V\\]", RegexOptions.IgnoreCase);
			Regex regexPlayerLeaveKingdom = new Regex("\\[A:P_L_K\\]", RegexOptions.IgnoreCase);
			Regex regexKingAbdicateToPlayer = new Regex("\\[ACTION:KING_ABDICATE_TO_PLAYER\\]", RegexOptions.IgnoreCase);
			Regex regexVassalageSubmit = new Regex("\\[ACTION:VASSALAGE:SUBMIT:(TRIBUTARY|GARRISON|VASSAL|MILITARY|PROTECTORATE):([a-zA-Z0-9_\\-]+)\\]", RegexOptions.IgnoreCase);
			Regex regexVassalageAny = new Regex("\\[ACTION:VASSALAGE:[^\\]\\r\\n]*\\]", RegexOptions.IgnoreCase);
			Regex regexKingdomAnnex = new Regex("\\[ACTION:KINGDOM_ANNEX:target_kingdom_id=([a-zA-Z0-9_\\-]+)\\]", RegexOptions.IgnoreCase);
			Regex regexKingdomAnnexAny = new Regex("\\[ACTION:KINGDOM_ANNEX:[^\\]\r\n]*\\]", RegexOptions.IgnoreCase);
			HashSet<string> settledDebtIdsThisRound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			int num = 0;
			try
			{
				List<GiveAssetTag> giveAssetTagsForCount = GiveAssetTagCodec.Extract(responseText);
				string responseWithoutGiveAssetTagsForCount = GiveAssetTagCodec.StripTags(responseText);
				num = giveAssetTagsForCount.Count
					+ Regex.Matches(responseWithoutGiveAssetTagsForCount, "\\[ACTION:[^\\]]+\\]", RegexOptions.IgnoreCase).Count
					+ Regex.Matches(responseWithoutGiveAssetTagsForCount, "\\[A:(?:H_J_P_P_(?:C(?:&L)?|L)|C_J_P_K|C_J_K:[^\\]]+|P_J_K_[MV]|P_L_K)\\]", RegexOptions.IgnoreCase).Count;
			}
			catch
			{
				num = 0;
			}
			bool hasDuelActionTagInCurrentReply = false;
			try
			{
				hasDuelActionTagInCurrentReply = (responseText ?? "").IndexOf("[ACTION:DUEL]", StringComparison.OrdinalIgnoreCase) >= 0;
			}
			catch
			{
				hasDuelActionTagInCurrentReply = false;
			}
			Logger.Obs("Action", "apply_reward_tags_start", new Dictionary<string, object>
			{
				["giverId"] = giver?.StringId ?? "",
				["receiverId"] = receiver?.StringId ?? "",
				["textLen"] = (responseText ?? "").Length,
				["actionTagCount"] = num
			});
			int? num2 = null;
			MatchCollection matchCollection5 = regex21.Matches(responseText);
			if (matchCollection5 != null && matchCollection5.Count > 0)
			{
				Match match3 = matchCollection5[matchCollection5.Count - 1];
				if (match3 != null && int.TryParse(match3.Groups[1].Value, out var result7))
				{
					num2 = NormalizeLlmTrustDeltaValue(result7);
				}
			}
			responseText = regex21.Replace(responseText, string.Empty);
			string giverName = giver?.Name?.ToString() ?? "某人";
			string receiverName = receiver?.Name?.ToString() ?? "某人";
			List<string> giverFacts = new List<string>();
			List<string> receiverFacts = new List<string>();
			bool anyActualGiveToPlayer = false;
			bool anyDebtRecorded = false;
			bool anyDebtPaymentApplied = false;

			bool anyRoyalAbdicationApplied = false;
			bool anyKingdomServiceApplied = false;
			bool anyVassalageApplied = false;
			bool anyKingdomAnnexationApplied = false;
			bool anySettlementTransferApplied = false;
			bool anySettlementTransferToPlayerApplied = false;
			bool anyHeroJoinPlayerPartyApplied = false;
			int itemTransferAttempted = 0;
			int itemTransferSucceeded = 0;
			int itemTransferFailedOrPartial = 0;
			long itemTransferActualQuantity = 0L;
			long itemTransferActualValue = 0L;
			int goldTransferAttempted = 0;
			int goldTransferSucceeded = 0;
			int goldTransferFailedOrPartial = 0;
			long goldTransferActualAmount = 0L;
			int settlementTransferAttempted = 0;
			int settlementTransferSucceeded = 0;
			int settlementTransferFailed = 0;
			long settlementTransferActualValue = 0L;
			Dictionary<string, FixedAssetTokenResolution> fixedAssetResolutionCache = new Dictionary<string, FixedAssetTokenResolution>(StringComparer.OrdinalIgnoreCase);
			HashSet<string> unresolvedFixedAssetTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			Settlement notableMarketSettlement = ResolveNotableMarketSettlement(giver);
			bool giverUsesNotableMarket = IsNotableMarketHero(giver, notableMarketSettlement);
			string notableMarketLabel = GetSettlementMarketTypeLabel(notableMarketSettlement) + "市场";
			responseText = GiveAssetTagCodec.ReplaceTags(responseText, delegate(GiveAssetTag tag)
			{
				if (!IsGoldAssetTokenForExternal(tag.AssetToken))
				{
					return tag.RawTag;
				}
				if (int.TryParse(tag.QuantityToken, out var result8))
				{
					goldTransferAttempted++;
					Logger.Log("Logic", $"[Reward] GIVE_ASSET gold 捕获: giver={giver?.Name} receiver={receiver?.Name} amount={result8}");
					bool forceCompleteGoldTransfer = receiver == Hero.MainHero && giver != Hero.MainHero;
					int num4 = giverUsesNotableMarket ? TransferGoldFromSettlement(notableMarketSettlement, receiver, result8, giverName, giver?.CharacterObject, forceComplete: forceCompleteGoldTransfer) : TransferGold(giver, receiver, result8, forceComplete: forceCompleteGoldTransfer);
					if (num4 > 0)
					{
						goldTransferSucceeded++;
						goldTransferActualAmount = TransferQuantitySpec.AddValue(goldTransferActualAmount, num4);
						giverFacts.Add(giverUsesNotableMarket ? $"你已经代表{notableMarketLabel}将 {num4} 第纳尔交给 {receiverName}。并进入了{receiverName}的库存" : $"你已经将 {num4} 第纳尔交给 {receiverName}。并进入了{receiverName}的库存");
						receiverFacts.Add(giverUsesNotableMarket ? $"你从 {giverName} 代表的{notableMarketLabel}收到了 {num4} 第纳尔。" : $"你从 {giverName} 收到了 {num4} 第纳尔。");
						if (num4 < result8)
						{
							goldTransferFailedOrPartial++;
							giverFacts.Add($"你原计划交付 {result8} 第纳尔，但余额变化后实际只交付了 {num4} 第纳尔。");
							receiverFacts.Add($"{giverName} 原计划交付 {result8} 第纳尔，但实际只交付了 {num4} 第纳尔。");
						}
						if (giver != Hero.MainHero && receiver == Hero.MainHero)
						{
							anyActualGiveToPlayer = true;
							ApplyAutoTrustGainFromHeroGiftValue(giver, num4, giverFacts, receiverFacts, giverName);
						}
					}
					else if (giverUsesNotableMarket)
					{
						goldTransferFailedOrPartial++;
						giverFacts.Add($"你试图代表{notableMarketLabel}交付 {result8} 第纳尔，但当前{notableMarketLabel}现钱不足，本轮未实际支付。");
					}
					else
					{
						goldTransferFailedOrPartial++;
						giverFacts.Add($"你试图交付 {result8} 第纳尔，但当前余额不足，本轮未实际支付。");
						receiverFacts.Add($"{giverName} 试图交付 {result8} 第纳尔，但当前余额不足，本轮未实际支付。");
					}
				}
				return string.Empty;
			});
			if (goldTransferAttempted > 0)
			{
				string goldBatchSummary = "金币转移汇总：尝试项" + goldTransferAttempted + "，成功项" + goldTransferSucceeded + "，失败或不足项" + goldTransferFailedOrPartial + "，实际转移" + goldTransferActualAmount + "第纳尔，实际价值" + goldTransferActualAmount + "第纳尔。";
				giverFacts.Add(goldBatchSummary);
				receiverFacts.Add(goldBatchSummary);
				Logger.Log("Logic", "[Reward] gold_batch_done attempted=" + goldTransferAttempted + " succeeded=" + goldTransferSucceeded + " failedOrPartial=" + goldTransferFailedOrPartial + " actualAmount=" + goldTransferActualAmount);
			}
			responseText = GiveAssetTagCodec.ReplaceTags(responseText, delegate(GiveAssetTag tag)
			{
				string value4 = (tag.AssetToken ?? "").Trim();
				if (TryResolveFixedAssetTokenForGiveAsset(giver, value4, fixedAssetResolutionCache, unresolvedFixedAssetTokens, out var _, out var _)
					|| MyBehavior.LooksLikeFixedAssetTransferIdForExternal(value4))
				{
					return tag.RawTag;
				}
				itemTransferAttempted++;
				bool hasFiniteRequestedQuantity = int.TryParse(tag.QuantityToken, out var requestedQuantity) && requestedQuantity > 0;
				List<RewardItemInfo> authorizedItems = null;
				string authorizedItemKey = "";
				bool isAuthorizedInventoryItem = TryResolveAuthorizedHeroRewardItem(giver, value4, out authorizedItems, out authorizedItemKey);
				bool isGeneratedRpItem = !isAuthorizedInventoryItem
					&& hasFiniteRequestedQuantity
					&& receiver == Hero.MainHero
					&& giver != Hero.MainHero
					&& IsValidGeneratedRpAssetNameForExternal(value4);
				Logger.Log("Logic", "[Reward] GIVE_ASSET route source=hero token=" + value4 + " inventoryExact=" + isAuthorizedInventoryItem + " rpFallback=" + isGeneratedRpItem);
				if (!isAuthorizedInventoryItem && !isGeneratedRpItem)
				{
					itemTransferFailedOrPartial++;
					return string.Empty;
				}
				if (isAuthorizedInventoryItem)
				{
					value4 = authorizedItemKey;
				}
				if (TransferQuantitySpec.TryParse(tag.QuantityToken, out var quantity))
				{
					if (TransferQuantitySpec.IsAllValue(value4) || (isGeneratedRpItem && quantity.IsAll))
					{
						itemTransferFailedOrPartial++;
						return string.Empty;
					}
					string itemName;
					string settlementPromptStringId = "";
					bool isNotableMarketItem = giverUsesNotableMarket && TryParseNotableMarketPromptStringId(value4, out settlementPromptStringId);
					if (!isGeneratedRpItem && giverUsesNotableMarket && !isNotableMarketItem && TryResolveRewardItemByNameOrId(value4, BuildSettlementRewardItemResolutionContext(notableMarketSettlement), out var notableMarketResolution, "notable_market_give_item"))
					{
						string resolvedMarketLookup = BuildRewardItemTransferLookup(notableMarketResolution);
						if (!string.IsNullOrWhiteSpace(resolvedMarketLookup))
						{
							settlementPromptStringId = resolvedMarketLookup;
							isNotableMarketItem = true;
						}
					}
					string itemIdForFacts = isNotableMarketItem ? settlementPromptStringId : value4;
					List<RewardItemInfo> itemFactContext = isNotableMarketItem ? BuildSettlementRewardItemResolutionContext(notableMarketSettlement) : BuildHeroRewardItemResolutionContext(giver);
					int result8 = quantity.Amount;
					if (quantity.IsAll)
					{
						result8 = ResolveAllRewardItemAmount(itemIdForFacts, authorizedItems);
					}
					Logger.Log("Logic", $"[Reward] GIVE_ASSET item 捕获: giver={giver?.Name} receiver={receiver?.Name} itemId={value4} amount={(quantity.IsAll ? "ALL" : result8.ToString())} resolvedAmount={result8}");
					if (result8 <= 0)
					{
						itemTransferFailedOrPartial++;
						return string.Empty;
					}
					ItemObject generatedRpItem = null;
					bool forceCompleteItemTransfer = !quantity.IsAll && receiver == Hero.MainHero && giver != Hero.MainHero;
					int num4 = isGeneratedRpItem
						? GenerateRpAssetToPlayer(value4, result8, giverName, giver?.CharacterObject, out itemName, out generatedRpItem, "give_asset_rp_hero", rpItemIntroductionContext)
						: (isNotableMarketItem ? TransferItemFromSettlement(notableMarketSettlement, receiver, settlementPromptStringId, result8, giverName, out itemName, giver?.CharacterObject, forceComplete: forceCompleteItemTransfer) : TransferItemById(giver, receiver, value4, result8, out itemName, forceComplete: forceCompleteItemTransfer));
					if (num4 > 0)
					{
						itemTransferSucceeded++;
						itemTransferActualQuantity = TransferQuantitySpec.AddValue(itemTransferActualQuantity, num4);
						string text3 = (string.IsNullOrEmpty(itemName) ? value4 : itemName);
						string text4 = text3;
						ItemObject itemObject3 = generatedRpItem ?? ResolveItemById(itemIdForFacts.Split('@')[0]);
						if (itemObject3 == null && TryResolveRewardItemStringId(value4, itemFactContext, out var _, out var resolvedFactItem, "give_item_fact"))
						{
							itemObject3 = resolvedFactItem;
						}
						if (itemObject3 == null && receiver == Hero.MainHero && giver != Hero.MainHero && TryResolveRewardItemForForcedGeneration(value4, itemFactContext, out var generatedFactItem, "give_item_fact_generate"))
						{
							itemObject3 = generatedFactItem.Item;
						}
						string text5 = isNotableMarketItem ? BuildSettlementItemValueFactSuffixForExternal(notableMarketSettlement, itemObject3, num4) : BuildItemValueFactSuffixForExternal(giver ?? receiver, itemObject3, num4);
						giverFacts.Add(isNotableMarketItem ? $"你已经代表{notableMarketLabel}将 {FormatItemAmount(num4, itemObject3, text4)} 交给 {receiverName}{text5}。并进入了{receiverName}的库存" : $"你已经将 {FormatItemAmount(num4, itemObject3, text4)} 交给 {receiverName}{text5}。并进入了{receiverName}的库存");
						receiverFacts.Add(isNotableMarketItem ? $"你从 {giverName} 代表的{notableMarketLabel}收到了 {FormatItemAmount(num4, itemObject3, text4)}{text5}。" : $"你从 {giverName} 收到了 {FormatItemAmount(num4, itemObject3, text4)}{text5}。");
						if (num4 < result8)
						{
							itemTransferFailedOrPartial++;
							string text6 = isNotableMarketItem ? BuildSettlementItemValueFactSuffixForExternal(notableMarketSettlement, itemObject3, result8) : BuildItemValueFactSuffixForExternal(giver ?? receiver, itemObject3, result8);
							giverFacts.Add(isGeneratedRpItem ? $"你本轮原计划交付 {FormatItemAmount(result8, itemObject3, text4)}{text6}，但实际仅生成并交付 {FormatItemAmount(num4, itemObject3, text4)}{text5}。" : $"你本轮原计划交付 {FormatItemAmount(result8, itemObject3, text4)}{text6}，但实际仅交付 {FormatItemAmount(num4, itemObject3, text4)}{text5}（库存不足）。");
							receiverFacts.Add($"{giverName} 原计划交付 {FormatItemAmount(result8, itemObject3, text4)}{text6}，实际仅交付 {FormatItemAmount(num4, itemObject3, text4)}{text5}。");
						}
						if (giver != Hero.MainHero && receiver == Hero.MainHero)
						{
							anyActualGiveToPlayer = true;
							int itemTrustValue = isNotableMarketItem ? GetItemTrustValueForMerchantGift(notableMarketSettlement, itemObject3, num4) : GetItemTrustValueForHeroGift(giver ?? receiver, itemObject3, num4);
							ApplyAutoTrustGainFromHeroGiftValue(giver, itemTrustValue, giverFacts, receiverFacts, giverName);
						}
						long actualItemValue = isNotableMarketItem ? GetItemGuideValueForMerchantGift(notableMarketSettlement, itemObject3, num4) : GetItemGuideValueForHeroGift(giver ?? receiver, itemObject3, num4);
						itemTransferActualValue = TransferQuantitySpec.AddValue(itemTransferActualValue, actualItemValue);
					}
					else
					{
						itemTransferFailedOrPartial++;
						string text5 = isGeneratedRpItem ? value4 : ResolveItemById(itemIdForFacts.Split('@')[0])?.Name?.ToString();
						string text6 = (string.IsNullOrWhiteSpace(text5) ? "该物品" : text5);
						ItemObject itemObject2 = ResolveItemById(itemIdForFacts.Split('@')[0]);
						string text7 = isNotableMarketItem ? BuildSettlementItemValueFactSuffixForExternal(notableMarketSettlement, itemObject2, result8) : BuildItemValueFactSuffixForExternal(giver ?? receiver, itemObject2, result8);
						giverFacts.Add(isGeneratedRpItem ? $"你尝试交付 RP 物品 {FormatItemAmount(result8, itemObject2, text6)}，但生成失败，本轮未实际交付。" : (isNotableMarketItem ? $"你尝试代表{notableMarketLabel}交付 {FormatItemAmount(result8, itemObject2, text6)}{text7}，但当前{notableMarketLabel}库存不足，本轮未实际交付。" : $"你尝试交付 {FormatItemAmount(result8, itemObject2, text6)}{text7}，但库存不足，本轮未实际交付。"));
						receiverFacts.Add(isGeneratedRpItem ? $"{giverName} 试图交付 RP 物品 {FormatItemAmount(result8, itemObject2, text6)}，但生成失败，本轮未实际交付。" : (isNotableMarketItem ? $"{giverName} 试图代表{notableMarketLabel}交付 {FormatItemAmount(result8, itemObject2, text6)}{text7}，但市场库存不足，本轮未实际交付。" : $"{giverName} 试图交付 {FormatItemAmount(result8, itemObject2, text6)}{text7}，但其库存不足，本轮未实际交付。"));
					}
				}
				else
				{
					itemTransferFailedOrPartial++;
				}
				return string.Empty;
			});
			if (itemTransferAttempted > 0)
			{
				string itemBatchSummary = "物品转移汇总：尝试项" + itemTransferAttempted + "，成功项" + itemTransferSucceeded + "，失败或不足项" + itemTransferFailedOrPartial + "，实际转移" + itemTransferActualQuantity + "件，实际指导总值约" + itemTransferActualValue + "第纳尔。";
				giverFacts.Add(itemBatchSummary);
				receiverFacts.Add(itemBatchSummary);
				Logger.Log("Logic", "[Reward] item_batch_done attempted=" + itemTransferAttempted + " succeeded=" + itemTransferSucceeded + " failedOrPartial=" + itemTransferFailedOrPartial + " actualQuantity=" + itemTransferActualQuantity + " actualValue=" + itemTransferActualValue);
			}
			responseText = regex23.Replace(responseText, delegate(Match m)
			{
				if (!int.TryParse(m.Groups[1].Value, out var result8) || !int.TryParse(m.Groups[2].Value, out var result9))
				{
					return string.Empty;
				}
				string text3 = GetAdDebtNote(m);
				if (!ShouldApplyAdDebtTag(m, "hero"))
				{
					return string.Empty;
				}
				if (receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					if (hasDuelActionTagInCurrentReply)
					{
						if (result8 > 0)
						{
							DuelBehavior.CachePendingDuelDebtTag(giver, result8, result9, text3);
						}
						Logger.Log("Logic", "[Reward] AD 标签与 [ACTION:DUEL] 同轮出现，已延后到决斗结算后再判定。");
						return string.Empty;
					}
					bool flag2 = DuelBehavior.TryConsumeDuelDebtTagPermission(giver, out var allowDebtTag);
					if (flag2 && !allowDebtTag)
					{
						Logger.Log("Logic", "[Reward] AD 标签被决斗门控阻止：仅在玩家战败后才允许触发。");
						return string.Empty;
					}
					if (result8 > 0)
					{
						DebtRecord.DebtLine debtLine = SetDebtForNpc(giver, result8, result9, text3);
						if (debtLine == null)
						{
							return string.Empty;
						}
						anyDebtRecorded = true;
						string text4 = BuildDebtPromiseDeadlineText(debtLine.DueDay, debtLine.IsDueUnlimited);
						string text5 = debtLine.DebtId ?? "";
						string text6 = string.IsNullOrWhiteSpace(text3) ? "无" : text3;
						giverFacts.Add($"你已经记下：玩家的承诺或欠款价值 {result8} 第纳尔，达成期限为：{text4}，备注：{text6}（债务ID:{text5}）。");
						receiverFacts.Add($"你对 {giverName} 的承诺或欠款价值 {result8} 第纳尔，达成期限为：{text4}，备注：{text6}（债务ID:{text5}）。");
						ShowRewardMessage($"【承诺或欠款记录】{text6}，承诺等价：{result8}", Color.FromUint(4294936576u), giver);
					}
				}
				return string.Empty;
			});
			responseText = regex24.Replace(responseText, delegate(Match m)
			{
				string value4 = (m.Groups[1].Value ?? "").Trim();
				if (receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					if (hasDuelActionTagInCurrentReply)
					{
						Logger.Log("Logic", "[Reward] ADP 标签与 [ACTION:DUEL] 同轮出现，已延后到决斗结算后再判定。");
						return string.Empty;
					}
					if (!string.IsNullOrWhiteSpace(value4) && !settledDebtIdsThisRound.Add(value4))
					{
						Logger.Log("Logic", "[Reward] 跳过重复债务解除标签: debtId=" + value4 + " tag=ADP");
						return string.Empty;
					}
					string statusText;
					bool flag2 = ResolveDebtByIdByAgreement(giver, value4, out statusText);
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						anyDebtPaymentApplied = true;
						giverFacts.Add($"你确认债务ID {value4} 已解除。{statusText}");
						receiverFacts.Add($"你的债务ID {value4} 已解除。{statusText}");
						bool flag3 = statusText.IndexOf("已按协商解除", StringComparison.OrdinalIgnoreCase) >= 0;
						ShowRewardMessage((flag3 ? "【债务解除】" : "【债务解除失败】") + statusText, flag3 ? Color.FromUint(4278255360u) : Color.FromUint(4294923605u), giver);
						if (flag2)
						{
							ShowRewardMessage("【欠款已清】你对 " + giverName + " 的全部欠款已还清！", Color.FromUint(4278255360u), giver);
						}
					}
				}
				return string.Empty;
			});
			responseText = regexKingAbdicateToPlayer.Replace(responseText, delegate
			{
				if (receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					string statusText;
					bool flag2 = TryApplyKingAbdicateToPlayerAction(giver, out statusText);
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						if (flag2)
						{
							anyRoyalAbdicationApplied = true;
						}
						giverFacts.Add(statusText);
						receiverFacts.Add(statusText);
						InformationManager.DisplayMessage(new InformationMessage((flag2 ? "【王位转移】" : "【王位转移失败】") + statusText, flag2 ? Color.FromUint(4278242559u) : Color.FromUint(4294936661u)));
					}
				}
				return string.Empty;
			});
			responseText = regexClanJoinPlayerKingdom.Replace(responseText, "[ACTION:KINGDOM_SERVICE:CLAN_JOIN_PLAYER_KINGDOM:current]");
			responseText = regexClanJoinKingdom.Replace(responseText, delegate(Match m)
			{
				return "[ACTION:KINGDOM_SERVICE:CLAN_JOIN_KINGDOM:" + (m.Groups[1].Value ?? "").Trim() + "]";
			});
			responseText = regexPlayerJoinKingdomMercenary.Replace(responseText, "[ACTION:KINGDOM_SERVICE:MERCENARY:current]");
			responseText = regexPlayerJoinKingdomVassal.Replace(responseText, "[ACTION:KINGDOM_SERVICE:VASSAL:current]");
			responseText = regexPlayerLeaveKingdom.Replace(responseText, "[ACTION:KINGDOM_SERVICE:LEAVE:current]");
			responseText = regex17.Replace(responseText, delegate(Match m)
			{
				string serviceType = (m.Groups[1].Value ?? "").Trim();
				string kingdomToken = (m.Groups[2].Value ?? "").Trim();
				if (receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					string statusText;
					bool flag2 = TryApplyKingdomServiceAction(giver, serviceType, kingdomToken, out statusText);
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						if (flag2)
						{
							anyKingdomServiceApplied = true;
						}
						giverFacts.Add(statusText);
						receiverFacts.Add(statusText);
						InformationManager.DisplayMessage(new InformationMessage((flag2 ? "【势力身份】" : "【势力身份失败】") + statusText, flag2 ? Color.FromUint(4278242559u) : Color.FromUint(4294936661u)));
					}
				}
				return string.Empty;
			});
			responseText = regex18.Replace(responseText, delegate(Match m)
			{
				string kingdomToken = (m.Groups[1].Value ?? "").Trim();
				if (receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					string statusText;
					bool flag2 = TryApplyKingdomServiceAction(giver, "MERCENARY", kingdomToken, out statusText);
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						if (flag2)
						{
							anyKingdomServiceApplied = true;
						}
						giverFacts.Add(statusText);
						receiverFacts.Add(statusText);
						InformationManager.DisplayMessage(new InformationMessage((flag2 ? "【势力身份】" : "【势力身份失败】") + statusText, flag2 ? Color.FromUint(4278242559u) : Color.FromUint(4294936661u)));
					}
				}
				return string.Empty;
			});
			responseText = regex19.Replace(responseText, delegate(Match m)
			{
				string kingdomToken = (m.Groups[1].Value ?? "").Trim();
				if (receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					string statusText;
					bool flag2 = TryApplyKingdomServiceAction(giver, "VASSAL", kingdomToken, out statusText);
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						if (flag2)
						{
							anyKingdomServiceApplied = true;
						}
						giverFacts.Add(statusText);
						receiverFacts.Add(statusText);
						InformationManager.DisplayMessage(new InformationMessage((flag2 ? "【势力身份】" : "【势力身份失败】") + statusText, flag2 ? Color.FromUint(4278242559u) : Color.FromUint(4294936661u)));
					}
				}
				return string.Empty;
			});
			responseText = regex20.Replace(responseText, delegate
			{
				if (receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					string statusText;
					bool flag2 = TryApplyKingdomServiceAction(giver, "LEAVE", "current", out statusText);
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						if (flag2)
						{
							anyKingdomServiceApplied = true;
						}
						giverFacts.Add(statusText);
						receiverFacts.Add(statusText);
						InformationManager.DisplayMessage(new InformationMessage((flag2 ? "【势力身份】" : "【势力身份失败】") + statusText, flag2 ? Color.FromUint(4278242559u) : Color.FromUint(4294936661u)));
					}
				}
				return string.Empty;
			});
			if (ApplyVassalageRewardTags(giver, receiver, ref responseText, regexVassalageSubmit, regexVassalageAny, giverFacts, receiverFacts))
			{
				anyVassalageApplied = true;
			}
			if (ApplyKingdomAnnexationRewardTags(giver, receiver, ref responseText, regexKingdomAnnex, regexKingdomAnnexAny, giverFacts, receiverFacts))
			{
				anyKingdomAnnexationApplied = true;
			}
			responseText = GiveAssetTagCodec.ReplaceTags(responseText, delegate(GiveAssetTag tag)
			{
				settlementTransferAttempted++;
				string settlementToken = (tag.AssetToken ?? "").Trim();
				string quantityToken = (tag.QuantityToken ?? "").Trim();
				if (receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					string statusText = "";
					MyBehavior.SettlementTransferPromptEntry entry = null;
					bool validQuantity = string.Equals(quantityToken, "1", StringComparison.Ordinal);
					bool flag2 = false;
					if (validQuantity)
					{
						flag2 = TryApplySettlementTransferAction(giver, receiver, "TO_PLAYER", settlementToken, fixedAssetResolutionCache, unresolvedFixedAssetTokens, out entry, out statusText);
					}
					else
					{
						statusText = "执行失败：固定资产数量必须为 1。";
					}
					string text3 = MyBehavior.GetSettlementTransferAssetDisplayNameForExternal(entry);
					if (string.IsNullOrWhiteSpace(text3) || text3 == "未知资产")
					{
						text3 = settlementToken;
					}
					if (flag2)
					{
						settlementTransferSucceeded++;
						settlementTransferActualValue = TransferQuantitySpec.AddValue(settlementTransferActualValue, Math.Max(0L, entry?.GuidePriceDenars ?? 0L));
						anySettlementTransferApplied = true;
						anySettlementTransferToPlayerApplied = true;
						giverFacts.Add($"你已经将固定资产 {text3} 转交给玩家。");
						receiverFacts.Add($"你已经从 {giverName} 那里取得了固定资产 {text3}。");
					}
					else if (!string.IsNullOrWhiteSpace(statusText))
					{
						settlementTransferFailed++;
						giverFacts.Add(statusText);
						receiverFacts.Add(statusText);
					}
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						InformationManager.DisplayMessage(new InformationMessage((flag2 ? "【固定资产转移】" : "【固定资产转移失败】") + statusText, flag2 ? Color.FromUint(4278242559u) : Color.FromUint(4294936661u)));
					}
				}
				else
				{
					settlementTransferFailed++;
				}
				return string.Empty;
			});
			responseText = GiveAssetTagCodec.StripTags(responseText);
			if (settlementTransferAttempted > 0)
			{
				string settlementBatchSummary = "固定资产转移汇总：尝试项" + settlementTransferAttempted + "，成功项" + settlementTransferSucceeded + "，失败项" + settlementTransferFailed + "，实际转移" + settlementTransferSucceeded + "项，实际指导总值约" + settlementTransferActualValue + "第纳尔。";
				giverFacts.Add(settlementBatchSummary);
				receiverFacts.Add(settlementBatchSummary);
				Logger.Log("Logic", "[Reward] settlement_batch_done attempted=" + settlementTransferAttempted + " succeeded=" + settlementTransferSucceeded + " failed=" + settlementTransferFailed + " actualValue=" + settlementTransferActualValue);
			}
			responseText = regex25.Replace(responseText, delegate(Match joinMatch)
			{
				if (receiver == Hero.MainHero && giver != Hero.MainHero)
				{
					bool asCompanion = joinMatch.Groups[1].Value.StartsWith("C", StringComparison.OrdinalIgnoreCase);
					bool preservePlayerFamilyIdentity = asCompanion && ShouldPreservePlayerFamilyIdentityForCompanionJoin(giver);
					bool preservePlayerSpouseIdentity = preservePlayerFamilyIdentity && (giver.Spouse == Hero.MainHero || Hero.MainHero?.Spouse == giver);
					string statusText;
					bool flag2 = TryApplyHeroJoinPlayerPartyCore(giver, asCompanion, out statusText, out bool joinedWildernessParty, out int joinedWildernessMembers, out int joinedWildernessPrisoners);
					bool joinedAsCompanion = flag2 && giver.CompanionOf == Clan.PlayerClan && giver.Occupation == Occupation.Wanderer;
					if (!string.IsNullOrWhiteSpace(statusText))
					{
						if (flag2)
						{
							anyHeroJoinPlayerPartyApplied = true;
							string giverJoinFact = preservePlayerFamilyIdentity
								? (preservePlayerSpouseIdentity ? $"你仍是 {receiverName} 的配偶，并已随玩家队伍行动。" : $"你仍是 {receiverName} 家族的成员，并已随玩家队伍行动。")
								: (joinedAsCompanion ? $"你已经成为 {receiverName} 的同伴，并随玩家队伍行动。" : $"你已经加入了 {receiverName} 的家族，并随玩家队伍行动。");
							string receiverJoinFact = preservePlayerFamilyIdentity
								? (preservePlayerSpouseIdentity ? $"{giverName} 仍是你的配偶，并已加入你的队伍。" : $"{giverName} 仍是你的家族成员，并已加入你的队伍。")
								: (joinedAsCompanion ? $"{giverName} 已成为你的同伴，并随你的队伍行动。" : $"{giverName} 已加入你的家族，并随你的队伍行动。");
							if (joinedWildernessParty)
							{
								string countText = BuildWildernessHeroPartyTransferCountText(joinedWildernessMembers, joinedWildernessPrisoners);
								giverJoinFact += string.IsNullOrEmpty(countText)
									? " 你的原野外队伍已随你归并至玩家主队，没有额外成员或俘虏。"
									: " 你原野外队伍中的 " + countText + "已一并转入玩家主队。";
								receiverJoinFact += string.IsNullOrEmpty(countText)
									? " " + giverName + "的原野外队伍已随本人归并至你的主队，没有额外成员或俘虏。"
									: " " + giverName + "原野外队伍中的 " + countText + "已一并转入你的主队。";
							}
							giverFacts.Add(giverJoinFact);
							receiverFacts.Add(receiverJoinFact);
						}
						else
						{
							giverFacts.Add(statusText);
							receiverFacts.Add(statusText);
						}
						string notificationPrefix = flag2 ? (preservePlayerFamilyIdentity ? "【加入队伍】" : (joinedAsCompanion ? "【成为同伴】" : "【加入家族】")) : "【加入队伍失败】";
						InformationManager.DisplayMessage(new InformationMessage(notificationPrefix + statusText, flag2 ? Color.FromUint(4278242559u) : Color.FromUint(4294936661u)));
					}
				}
				return string.Empty;
			});
			if (num2.HasValue)
			{
				Logger.Log("Logic", "[Reward] 提示: 检测到 [ACTION:TRADE_TRUST]，但即时交易信任现已改为按NPC实际交付价值自动累计，本标签已忽略。");
			}
			responseText = responseText.Trim();
			stopwatch.Stop();
			Logger.Obs("Action", "apply_reward_tags_done", new Dictionary<string, object>
			{
				["giverId"] = giver?.StringId ?? "",
				["receiverId"] = receiver?.StringId ?? "",
				["anyActualGiveToPlayer"] = anyActualGiveToPlayer,
				["anyDebtRecorded"] = anyDebtRecorded,
				["anyDebtPaymentApplied"] = anyDebtPaymentApplied,

				["anyRoyalAbdicationApplied"] = anyRoyalAbdicationApplied,
				["anyKingdomServiceApplied"] = anyKingdomServiceApplied,
				["anyVassalageApplied"] = anyVassalageApplied,
				["anyKingdomAnnexationApplied"] = anyKingdomAnnexationApplied,
				["anySettlementTransferApplied"] = anySettlementTransferApplied,
				["anyHeroJoinPlayerPartyApplied"] = anyHeroJoinPlayerPartyApplied,
				["giverFactsCount"] = giverFacts.Count,
				["receiverFactsCount"] = receiverFacts.Count,
				["textLenAfter"] = (responseText ?? "").Length,
				["latencyMs"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)
			});
			Logger.Metric("action.apply_reward_tags", ok: true, stopwatch.Elapsed.TotalMilliseconds);
			if (giverFacts.Count > 0)
			{
				SetLastGeneratedNpcFactLines(new string[1] { "[AFEF NPC行为补充] " + giverName + ": " + string.Join(" ", giverFacts) });
				MyBehavior.AppendExternalNpcFact(giver, string.Join(" ", giverFacts));
			}
			if (receiverFacts.Count > 0)
			{
				if (receiver == Hero.MainHero)
				{
					MyBehavior.AppendExternalPlayerFact(receiver, string.Join(" ", receiverFacts));
				}
				else
				{
					MyBehavior.AppendExternalNpcFact(receiver, string.Join(" ", receiverFacts));
				}
			}
			TryRecordRewardActionHistory(giver, receiver, giverName, receiverName, giverFacts, receiverFacts, anyActualGiveToPlayer, anyDebtRecorded, anyDebtPaymentApplied, anyRoyalAbdicationApplied, anyKingdomServiceApplied, anyVassalageApplied, anyKingdomAnnexationApplied, anySettlementTransferApplied, anySettlementTransferToPlayerApplied);
		}
		catch (Exception ex)
		{
			SetLastGeneratedNpcFactLines(null);
			stopwatch.Stop();
			Logger.Log("Logic", "[ERROR] ApplyRewardTags 异常: " + ex.ToString());
			Logger.Obs("Action", "apply_reward_tags_error", new Dictionary<string, object>
			{
				["giverId"] = giver?.StringId ?? "",
				["receiverId"] = receiver?.StringId ?? "",
				["latencyMs"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
				["message"] = ex.Message,
				["type"] = ex.GetType().Name
			});
			Logger.Metric("action.apply_reward_tags", ok: false, stopwatch.Elapsed.TotalMilliseconds);
		}
	}

	private static void TryRecordRewardActionHistory(Hero giver, Hero receiver, string giverName, string receiverName, List<string> giverFacts, List<string> receiverFacts, bool anyActualGiveToPlayer, bool anyDebtRecorded, bool anyDebtPaymentApplied, bool anyRoyalAbdicationApplied, bool anyKingdomServiceApplied, bool anyVassalageApplied, bool anyKingdomAnnexationApplied, bool anySettlementTransferApplied, bool anySettlementTransferToPlayerApplied)
	{
		try
		{
			bool economicAction = anyActualGiveToPlayer || anyDebtRecorded || anyDebtPaymentApplied || anySettlementTransferApplied;
			bool politicalAction = anyRoyalAbdicationApplied || anyKingdomServiceApplied || anyVassalageApplied || anyKingdomAnnexationApplied;
			if (!economicAction && !politicalAction)
			{
				return;
			}
			Hero player = Hero.MainHero;
			Hero npc = giver == player ? receiver : giver;
			if (player == null || npc == null || npc == player)
			{
				return;
			}
			string actionKind = ResolveRewardActionHistoryKind(anyActualGiveToPlayer, anyDebtRecorded, anyDebtPaymentApplied, anyRoyalAbdicationApplied, anyKingdomServiceApplied, anyVassalageApplied, anyKingdomAnnexationApplied, anySettlementTransferApplied, anySettlementTransferToPlayerApplied);
			string summary = BuildRewardActionHistorySummary(giverFacts, receiverFacts);
			if (string.IsNullOrWhiteSpace(summary))
			{
				summary = politicalAction ? "双方完成了一项政治承诺。" : "双方完成了一项交易或债务约定。";
			}
			string npcName = string.IsNullOrWhiteSpace(giverName) ? (npc.Name?.ToString() ?? "对方") : giverName.Trim();
			string playerName = string.IsNullOrWhiteSpace(receiverName) ? (player.Name?.ToString() ?? "玩家") : receiverName.Trim();
			string npcText = BuildRewardNpcActionHistoryText(actionKind, playerName, summary);
			string playerText = BuildRewardPlayerActionHistoryText(actionKind, npcName, summary);
			Settlement settlement = ResolveRewardActionSettlement(npc);
			string locationText = settlement?.Name?.ToString() ?? "";
			string stableKey = BuildRewardActionHistoryStableKey(actionKind, npc, player, summary);
			MyBehavior.RecordNpcActionForExternal(npc, npcText, stableKey + ":npc", actionKind, isMajor: true, isRecent: true, targetHero: player, settlement: settlement, locationText: locationText, allowNonLordHero: true, won: true);
			MyBehavior.RecordPlayerActionForExternal(playerText, stableKey + ":player", actionKind, isMajor: true, targetHero: npc, settlement: settlement, locationText: locationText, won: true);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] TryRecordRewardActionHistory: " + ex.Message);
		}
	}

	private static string ResolveRewardActionHistoryKind(bool anyActualGiveToPlayer, bool anyDebtRecorded, bool anyDebtPaymentApplied, bool anyRoyalAbdicationApplied, bool anyKingdomServiceApplied, bool anyVassalageApplied, bool anyKingdomAnnexationApplied, bool anySettlementTransferApplied, bool anySettlementTransferToPlayerApplied)
	{
		if (anyKingdomAnnexationApplied)
		{
			return "kingdom_annexation";
		}
		if (anyRoyalAbdicationApplied)
		{
			return "royal_abdication";
		}
		if (anyVassalageApplied || anyKingdomServiceApplied)
		{
			return "persuasion_defection";
		}
		if (anySettlementTransferApplied)
		{
			if (anySettlementTransferToPlayerApplied)
			{
				return "asset_transfer_to_player";
			}
			return "asset_transfer";
		}
		if (anyDebtPaymentApplied)
		{
			return "debt_payment";
		}
		if (anyDebtRecorded)
		{
			return "debt_recorded";
		}
		return anyActualGiveToPlayer ? "major_exchange" : "reward_action";
	}

	private static string BuildRewardNpcActionHistoryText(string actionKind, string playerName, string summary)
	{
		string prefix;
		switch ((actionKind ?? "").Trim().ToLowerInvariant())
		{
		case "kingdom_annexation":
		case "royal_abdication":
		case "persuasion_defection":
			prefix = "你与" + playerName + "完成了一项势力归附或政治承诺：";
			break;
		case "asset_transfer_to_player":
			prefix = "你将固定资产转交给" + playerName + "：";
			break;
		case "asset_transfer_to_npc":
			prefix = "玩家将固定资产转交给你或你的家族：";
			break;
		case "asset_transfer":
			prefix = "你与" + playerName + "完成了一项固定资产转移：";
			break;
		case "debt_payment":
			prefix = "你确认了" + playerName + "的一项债务履约：";
			break;
		case "debt_recorded":
			prefix = "你与" + playerName + "确立了一项债务约定：";
			break;
		default:
			prefix = "你与" + playerName + "完成了一项重要交易或交付：";
			break;
		}
		return LimitRewardHistoryText(prefix + summary, 260);
	}

	private static string BuildRewardPlayerActionHistoryText(string actionKind, string npcName, string summary)
	{
		string prefix;
		switch ((actionKind ?? "").Trim().ToLowerInvariant())
		{
		case "kingdom_annexation":
		case "royal_abdication":
		case "persuasion_defection":
			prefix = "你与" + npcName + "完成了一项势力归附或政治承诺：";
			break;
		case "asset_transfer_to_player":
			prefix = "你从" + npcName + "那里取得了固定资产：";
			break;
		case "asset_transfer_to_npc":
			prefix = "你主动将固定资产交给" + npcName + "：";
			break;
		case "asset_transfer":
			prefix = "你与" + npcName + "完成了一项固定资产转移：";
			break;
		case "debt_payment":
			prefix = "你完成了与" + npcName + "有关的一项债务履约：";
			break;
		case "debt_recorded":
			prefix = "你与" + npcName + "确立了一项债务约定：";
			break;
		default:
			prefix = "你与" + npcName + "完成了一项重要交易或交付：";
			break;
		}
		return LimitRewardHistoryText(prefix + summary, 260);
	}

	private static string BuildRewardActionHistorySummary(List<string> giverFacts, List<string> receiverFacts)
	{
		IEnumerable<string> lines = (receiverFacts ?? new List<string>()).Concat(giverFacts ?? new List<string>());
		string text = string.Join(" ", lines.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Replace("\r", " ").Replace("\n", " ").Trim()));
		return LimitRewardHistoryText(text, 190);
	}

	private static string BuildRewardActionHistoryStableKey(string actionKind, Hero npc, Hero player, string summary)
	{
		int day = 0;
		try
		{
			day = Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
		}
		catch
		{
			day = 0;
		}
		return "reward_action:" + NormalizeRewardActionKeyPart(actionKind) + ":" + (npc?.StringId ?? "") + ":" + (player?.StringId ?? "") + ":" + day + ":" + NormalizeRewardActionKeyPart(summary);
	}

	private static Settlement ResolveRewardActionSettlement(Hero npc)
	{
		try
		{
			return Settlement.CurrentSettlement ?? PlayerEncounter.EncounterSettlement ?? MobileParty.MainParty?.CurrentSettlement ?? npc?.CurrentSettlement ?? npc?.StayingInSettlement ?? npc?.HomeSettlement;
		}
		catch
		{
			return null;
		}
	}

	private static string NormalizeRewardActionKeyPart(string value)
	{
		string text = (value ?? "").Trim().ToLowerInvariant();
		if (text.Length > 80)
		{
			text = text.Substring(0, 80);
		}
		return Regex.Replace(text, "[\\s:|]+", "_");
	}

	private static string LimitRewardHistoryText(string value, int maxChars)
	{
		string text = (value ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (maxChars <= 0 || text.Length <= maxChars)
		{
			return text;
		}
		return text.Substring(0, maxChars).TrimEnd() + "...";
	}

	private static void TryRecordNonHeroRewardActionHistory(string giverName, Hero receiver, List<string> playerFacts, string sourceKind, Settlement settlement)
	{
		try
		{
			if (receiver != Hero.MainHero || playerFacts == null || playerFacts.Count == 0)
			{
				return;
			}
			string summary = BuildRewardActionHistorySummary(null, playerFacts);
			if (string.IsNullOrWhiteSpace(summary))
			{
				return;
			}
			string actionKind = ResolveNonHeroRewardActionKind(summary, sourceKind);
			string displayName = string.IsNullOrWhiteSpace(giverName) ? "对方" : giverName.Trim();
			string text = "你与" + displayName + "完成了一项交易、交付或债务履约：" + summary;
			string stableKey = "nonhero_reward_action:" + NormalizeRewardActionKeyPart(sourceKind) + ":" + NormalizeRewardActionKeyPart(displayName) + ":" + NormalizeRewardActionKeyPart(summary);
			MyBehavior.RecordPlayerActionForExternal(LimitRewardHistoryText(text, 260), stableKey, actionKind, isMajor: true, targetHero: null, settlement: settlement, locationText: settlement?.Name?.ToString() ?? displayName, won: true);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] TryRecordNonHeroRewardActionHistory: " + ex.Message);
		}
	}

	private static string ResolveNonHeroRewardActionKind(string summary, string sourceKind)
	{
		string text = (summary ?? "").Trim();
		if (text.IndexOf("还款", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("偿还", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("已解除", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "debt_payment";
		}
		if (text.IndexOf("欠", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("债务", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "debt_recorded";
		}
		return string.Equals(sourceKind, "merchant_reward_action", StringComparison.OrdinalIgnoreCase) ? "merchant_exchange" : "major_exchange";
	}

	private static bool ApplyVassalageRewardTags(Hero giver, Hero receiver, ref string responseText, Regex regexVassalageSubmit, Regex regexVassalageAny, List<string> giverFacts, List<string> receiverFacts)
	{
		bool anyVassalageApplied = false;
		responseText = regexVassalageSubmit.Replace(responseText, delegate(Match m)
		{
			string typeToken = (m.Groups[1].Value ?? "").Trim();
			string kingdomToken = (m.Groups[2].Value ?? "").Trim();
			VassalageDiagnosticLog.Event("reward_tags.vassalage_submit.matched", new Dictionary<string, object>
			{
				["tag"] = m.Value ?? "",
				["giver"] = VassalageDiagnosticLog.DescribeHero(giver),
				["receiver"] = VassalageDiagnosticLog.DescribeHero(receiver),
				["receiverIsMainHero"] = receiver == Hero.MainHero,
				["giverIsMainHero"] = giver == Hero.MainHero,
				["typeToken"] = typeToken,
				["kingdomToken"] = kingdomToken
			});
			if (receiver == Hero.MainHero && giver != Hero.MainHero)
			{
				string statusText = "";
				bool flag2 = false;
				VassalageBehavior vassalageBehavior = VassalageBehavior.Instance;
				if (vassalageBehavior != null)
				{
					flag2 = vassalageBehavior.TryApplyVassalageAction(giver, "SUBMIT", typeToken, kingdomToken, out statusText);
				}
				else
				{
					statusText = "臣属条款未执行：臣属国系统尚未初始化。";
				}
				VassalageDiagnosticLog.Event("reward_tags.vassalage_submit.applied", new Dictionary<string, object>
				{
					["ok"] = flag2,
					["tag"] = m.Value ?? "",
					["giver"] = VassalageDiagnosticLog.DescribeHero(giver),
					["receiver"] = VassalageDiagnosticLog.DescribeHero(receiver),
					["typeToken"] = typeToken,
					["kingdomToken"] = kingdomToken,
					["statusText"] = statusText
				});
				if (!string.IsNullOrWhiteSpace(statusText))
				{
					if (flag2)
					{
						anyVassalageApplied = true;
					}
					giverFacts.Add(statusText);
					receiverFacts.Add(statusText);
					InformationManager.DisplayMessage(new InformationMessage((flag2 ? "【臣属国条约】" : "【臣属国条约失败】") + statusText, flag2 ? Color.FromUint(4278242559u) : Color.FromUint(4294936661u)));
				}
			}
			else
			{
				VassalageDiagnosticLog.Event("reward_tags.vassalage_submit.skipped", new Dictionary<string, object>
				{
					["reason"] = "not_npc_to_main_hero",
					["tag"] = m.Value ?? "",
					["giver"] = VassalageDiagnosticLog.DescribeHero(giver),
					["receiver"] = VassalageDiagnosticLog.DescribeHero(receiver)
				});
			}
			return string.Empty;
		});
		responseText = regexVassalageAny.Replace(responseText, delegate(Match m)
		{
			VassalageDiagnosticLog.Event("reward_tags.vassalage_unsupported.matched", new Dictionary<string, object>
			{
				["tag"] = m.Value ?? "",
				["giver"] = VassalageDiagnosticLog.DescribeHero(giver),
				["receiver"] = VassalageDiagnosticLog.DescribeHero(receiver)
			});
			if (receiver == Hero.MainHero && giver != Hero.MainHero)
			{
				string statusText = "臣属条款未执行：不支持该 VASSALAGE 动作。";
				giverFacts.Add(statusText);
				receiverFacts.Add(statusText);
				InformationManager.DisplayMessage(new InformationMessage("【臣属国条约失败】" + statusText, Color.FromUint(4294936661u)));
				Logger.Log("Vassalage", "Unsupported tag ignored: " + (m.Value ?? ""));
			}
			return string.Empty;
		});
		return anyVassalageApplied;
	}

	private static bool ApplyKingdomAnnexationRewardTags(Hero giver, Hero receiver, ref string responseText, Regex regexKingdomAnnex, Regex regexKingdomAnnexAny, List<string> giverFacts, List<string> receiverFacts)
	{
		bool anyKingdomAnnexationApplied = false;
		responseText = regexKingdomAnnex.Replace(responseText, delegate(Match m)
		{
			string kingdomToken = (m.Groups[1].Value ?? "").Trim();
			Logger.Obs("KingdomAnnexation", "reward_tags.matched", new Dictionary<string, object>
			{
				["tag"] = m.Value ?? "",
				["giverId"] = giver?.StringId ?? "",
				["receiverId"] = receiver?.StringId ?? "",
				["targetKingdomId"] = kingdomToken
			});
			KingdomAnnexationDiagnosticLog.Event("reward_tags.matched", new Dictionary<string, object>
			{
				["tag"] = m.Value ?? "",
				["giver"] = KingdomAnnexationDiagnosticLog.DescribeHero(giver),
				["receiver"] = KingdomAnnexationDiagnosticLog.DescribeHero(receiver),
				["targetKingdomId"] = kingdomToken
			});
			if (receiver == Hero.MainHero && giver != Hero.MainHero)
			{
				string statusText = "";
				bool flag2 = KingdomAnnexationBehavior.Instance?.TryApplyKingdomAnnexation(giver, kingdomToken, out statusText) ?? false;
				Logger.Obs("KingdomAnnexation", "reward_tags.applied", new Dictionary<string, object>
				{
					["ok"] = flag2,
					["tag"] = m.Value ?? "",
					["giverId"] = giver?.StringId ?? "",
					["receiverId"] = receiver?.StringId ?? "",
					["targetKingdomId"] = kingdomToken,
					["statusText"] = statusText
				});
				KingdomAnnexationDiagnosticLog.Event("reward_tags.applied", new Dictionary<string, object>
				{
					["ok"] = flag2,
					["tag"] = m.Value ?? "",
					["giver"] = KingdomAnnexationDiagnosticLog.DescribeHero(giver),
					["receiver"] = KingdomAnnexationDiagnosticLog.DescribeHero(receiver),
					["targetKingdomId"] = kingdomToken,
					["statusText"] = statusText
				});
				if (string.IsNullOrWhiteSpace(statusText) && KingdomAnnexationBehavior.Instance == null)
				{
					statusText = "国家吞并未执行：吞并系统尚未初始化。";
				}
				if (!string.IsNullOrWhiteSpace(statusText))
				{
					if (flag2)
					{
						anyKingdomAnnexationApplied = true;
					}
					giverFacts.Add(statusText);
					receiverFacts.Add(statusText);
					InformationManager.DisplayMessage(new InformationMessage((flag2 ? "【国家吞并】" : "【国家吞并失败】") + statusText, flag2 ? Color.FromUint(4278242559u) : Color.FromUint(4294936661u)));
				}
			}
			return string.Empty;
		});
		responseText = regexKingdomAnnexAny.Replace(responseText, delegate(Match m)
		{
			Logger.Log("KingdomAnnexation", "Unsupported tag ignored: " + (m.Value ?? ""));
			KingdomAnnexationDiagnosticLog.Event("reward_tags.unsupported", new Dictionary<string, object>
			{
				["tag"] = m.Value ?? "",
				["giver"] = KingdomAnnexationDiagnosticLog.DescribeHero(giver),
				["receiver"] = KingdomAnnexationDiagnosticLog.DescribeHero(receiver)
			});
			if (receiver == Hero.MainHero && giver != Hero.MainHero)
			{
				string statusText = "国家吞并未执行：不支持该 KINGDOM_ANNEX 标签格式。";
				giverFacts.Add(statusText);
				receiverFacts.Add(statusText);
				InformationManager.DisplayMessage(new InformationMessage("【国家吞并失败】" + statusText, Color.FromUint(4294936661u)));
			}
			return string.Empty;
		});
		return anyKingdomAnnexationApplied;
	}

	public void ApplyPartyRewardTags(PartyBase giverParty, Hero receiver, string giverName, BasicCharacterObject giverCharacter, ref string responseText, RpItemIntroductionContext rpItemIntroductionContext = null)
	{
		SetLastGeneratedNpcFactLines(null);
		if (giverParty == null || receiver == null || string.IsNullOrEmpty(responseText))
		{
			return;
		}
		string text = string.IsNullOrWhiteSpace(giverName) ? "对方部队" : giverName.Trim();
		List<string> npcFacts = new List<string>();
		List<string> playerFacts = new List<string>();
		int itemTransferAttempted = 0;
		int itemTransferSucceeded = 0;
		int itemTransferFailedOrPartial = 0;
		long itemTransferActualQuantity = 0L;
		long itemTransferActualValue = 0L;
		int goldTransferAttempted = 0;
		int goldTransferSucceeded = 0;
		int goldTransferFailedOrPartial = 0;
		long goldTransferActualAmount = 0L;
		try
		{
			responseText = GiveAssetTagCodec.ReplaceTags(responseText, delegate(GiveAssetTag tag)
			{
				if (!IsGoldAssetTokenForExternal(tag.AssetToken))
				{
					return tag.RawTag;
				}
				if (int.TryParse(tag.QuantityToken, out var result))
				{
					goldTransferAttempted++;
					int num = TransferGoldFromParty(giverParty, receiver, result, text, giverCharacter, forceComplete: receiver == Hero.MainHero);
					if (num > 0)
					{
						goldTransferSucceeded++;
						goldTransferActualAmount = TransferQuantitySpec.AddValue(goldTransferActualAmount, num);
						npcFacts.Add($"你已经将 {num} 第纳尔交给玩家，并从你所在部队的资金中扣除。");
						playerFacts.Add($"你从 {text} 收到了 {num} 第纳尔。");
						if (num < result)
						{
							goldTransferFailedOrPartial++;
							npcFacts.Add($"你原计划交付 {result} 第纳尔，但部队资金变化后实际只交付了 {num} 第纳尔。");
							playerFacts.Add($"{text} 原计划交付 {result} 第纳尔，但实际只交付了 {num} 第纳尔。");
						}
					}
					else
					{
						goldTransferFailedOrPartial++;
						npcFacts.Add($"你试图交付 {result} 第纳尔，但你所在部队当前资金不足，本轮未实际支付。");
						playerFacts.Add($"{text} 试图交付 {result} 第纳尔，但部队当前资金不足，本轮未实际支付。");
					}
				}
				return string.Empty;
			});
			if (goldTransferAttempted > 0)
			{
				string goldBatchSummary = "金币转移汇总：尝试项" + goldTransferAttempted + "，成功项" + goldTransferSucceeded + "，失败或不足项" + goldTransferFailedOrPartial + "，实际转移" + goldTransferActualAmount + "第纳尔，实际价值" + goldTransferActualAmount + "第纳尔。";
				npcFacts.Add(goldBatchSummary);
				playerFacts.Add(goldBatchSummary);
				Logger.Log("Logic", "[RewardParty] gold_batch_done attempted=" + goldTransferAttempted + " succeeded=" + goldTransferSucceeded + " failedOrPartial=" + goldTransferFailedOrPartial + " actualAmount=" + goldTransferActualAmount);
			}
			responseText = GiveAssetTagCodec.ReplaceTags(responseText, delegate(GiveAssetTag tag)
			{
				string value = (tag.AssetToken ?? "").Trim();
				itemTransferAttempted++;
				bool hasFiniteRequestedQuantity = int.TryParse(tag.QuantityToken, out var requestedQuantity) && requestedQuantity > 0;
				List<RewardItemInfo> authorizedItems = null;
				string authorizedItemKey = "";
				bool isAuthorizedInventoryItem = TryResolveAuthorizedPartyRewardItem(giverParty, giverCharacter, value, out authorizedItems, out authorizedItemKey);
				bool isGeneratedRpItem = !isAuthorizedInventoryItem
					&& hasFiniteRequestedQuantity
					&& receiver == Hero.MainHero
					&& IsValidGeneratedRpAssetNameForExternal(value);
				Logger.Log("Logic", "[RewardParty] GIVE_ASSET route token=" + value + " inventoryExact=" + isAuthorizedInventoryItem + " rpFallback=" + isGeneratedRpItem);
				if (!isAuthorizedInventoryItem && !isGeneratedRpItem)
				{
					itemTransferFailedOrPartial++;
					return string.Empty;
				}
				if (isAuthorizedInventoryItem)
				{
					value = authorizedItemKey;
				}
				if (TransferQuantitySpec.TryParse(tag.QuantityToken, out var quantity))
				{
					if (TransferQuantitySpec.IsAllValue(value) || (isGeneratedRpItem && quantity.IsAll))
					{
						itemTransferFailedOrPartial++;
						return string.Empty;
					}
					List<RewardItemInfo> allContext = BuildPartyRewardItemResolutionContext(giverParty);
					int result = quantity.Amount;
					if (quantity.IsAll)
					{
						result = ResolveAllRewardItemAmount(value, authorizedItems);
					}
					if (result <= 0)
					{
						itemTransferFailedOrPartial++;
						return string.Empty;
					}
					string itemName;
					ItemObject generatedRpItem = null;
					bool forceCompleteItemTransfer = !quantity.IsAll && receiver == Hero.MainHero;
					int num = isGeneratedRpItem
						? GenerateRpAssetToPlayer(value, result, text, giverCharacter, out itemName, out generatedRpItem, "give_asset_rp_party", rpItemIntroductionContext)
						: TransferItemFromParty(giverParty, receiver, value, result, text, out itemName, giverCharacter, forceComplete: forceCompleteItemTransfer);
					ItemObject itemObject = generatedRpItem ?? ResolveItemById((value ?? "").Split('@')[0]);
					if (itemObject == null && TryResolveRewardItemStringId(value, allContext, out var _, out var resolvedPartyFactItem, "party_give_item_fact"))
					{
						itemObject = resolvedPartyFactItem;
					}
					if (itemObject == null && num > 0 && receiver == Hero.MainHero && TryResolveRewardItemForForcedGeneration(value, BuildPartyRewardItemResolutionContext(giverParty), out var generatedPartyFactItem, "party_give_item_fact_generate"))
					{
						itemObject = generatedPartyFactItem.Item;
					}
					string text2 = string.IsNullOrWhiteSpace(itemName) ? (itemObject?.Name?.ToString() ?? value) : itemName;
					if (num > 0)
					{
						itemTransferSucceeded++;
						itemTransferActualQuantity = TransferQuantitySpec.AddValue(itemTransferActualQuantity, num);
						itemTransferActualValue = TransferQuantitySpec.AddValue(itemTransferActualValue, GetItemGuideValueForHeroGift(Hero.MainHero, itemObject, num));
						string text3 = BuildItemValueFactSuffixForExternal(Hero.MainHero, itemObject, num);
						npcFacts.Add(isGeneratedRpItem ? $"你已经将 RP 物品 {FormatItemAmount(num, itemObject, text2)} 交给玩家{text3}。" : $"你已经将 {FormatItemAmount(num, itemObject, text2)} 交给玩家{text3}，并从你所在部队的库存中扣除。");
						playerFacts.Add($"你从 {text} 收到了 {FormatItemAmount(num, itemObject, text2)}{text3}。");
						if (num < result)
						{
							itemTransferFailedOrPartial++;
							npcFacts.Add(isGeneratedRpItem ? $"你原本打算交付 {FormatItemAmount(result, itemObject, text2)}，但实际只生成并交付了 {FormatItemAmount(num, itemObject, text2)}。" : $"你原本打算交付 {FormatItemAmount(result, itemObject, text2)}，但你所在部队库存不足，实际只交付了 {FormatItemAmount(num, itemObject, text2)}。");
							playerFacts.Add($"{text} 原本打算交付 {FormatItemAmount(result, itemObject, text2)}，但实际只交付了 {FormatItemAmount(num, itemObject, text2)}。");
						}
					}
					else
					{
						itemTransferFailedOrPartial++;
						string text3 = BuildItemValueFactSuffixForExternal(Hero.MainHero, itemObject, result);
						npcFacts.Add(isGeneratedRpItem ? $"你试图交付 RP 物品 {FormatItemAmount(result, itemObject, text2)}，但生成失败，本轮未实际交付。" : $"你试图交付 {FormatItemAmount(result, itemObject, text2)}{text3}，但你所在部队库存不足，本轮未实际交付。");
					}
				}
				else
				{
					itemTransferFailedOrPartial++;
				}
				return string.Empty;
			});
			if (itemTransferAttempted > 0)
			{
				string itemBatchSummary = "物品转移汇总：尝试项" + itemTransferAttempted + "，成功项" + itemTransferSucceeded + "，失败或不足项" + itemTransferFailedOrPartial + "，实际转移" + itemTransferActualQuantity + "件，实际指导总值约" + itemTransferActualValue + "第纳尔。";
				npcFacts.Add(itemBatchSummary);
				playerFacts.Add(itemBatchSummary);
				Logger.Log("Logic", "[RewardParty] item_batch_done attempted=" + itemTransferAttempted + " succeeded=" + itemTransferSucceeded + " failedOrPartial=" + itemTransferFailedOrPartial + " actualQuantity=" + itemTransferActualQuantity + " actualValue=" + itemTransferActualValue);
			}
			responseText = Regex.Replace(responseText, "\\[ACTION:TRADE_TRUST:[^\\]]*\\]", string.Empty, RegexOptions.IgnoreCase);
			responseText = Regex.Replace(responseText, "\\[AD:[^\\]]+\\]", string.Empty, RegexOptions.IgnoreCase);
			responseText = Regex.Replace(responseText, "\\[ADP:[^\\]]+\\]", string.Empty, RegexOptions.IgnoreCase).Trim();
			if (npcFacts.Count > 0)
			{
				SetLastGeneratedNpcFactLines(new string[1] { "[AFEF NPC行为补充] " + text + ": " + string.Join(" ", npcFacts) });
			}
			if (playerFacts.Count > 0 && receiver == Hero.MainHero)
			{
				MyBehavior.AppendExternalPlayerFact(receiver, string.Join(" ", playerFacts));
				TryRecordNonHeroRewardActionHistory(text, receiver, playerFacts, "party_reward_action", ResolveRewardActionSettlement(null));
			}
		}
		catch (Exception ex)
		{
			SetLastGeneratedNpcFactLines(null);
			Logger.Log("Logic", "[ERROR] ApplyPartyRewardTags 异常: " + ex);
		}
	}

	public void ApplyMerchantRewardTags(CharacterObject giverCharacter, Hero receiver, ref string responseText, RpItemIntroductionContext rpItemIntroductionContext = null)
	{
		SetLastGeneratedNpcFactLines(null);
		if (giverCharacter == null || receiver == null || string.IsNullOrEmpty(responseText) || !TryGetSettlementMerchantKind(giverCharacter, out var kind))
		{
			return;
		}
		Settlement currentSettlement = Settlement.CurrentSettlement;
		if (currentSettlement == null || !currentSettlement.IsTown)
		{
			responseText = Regex.Replace(responseText ?? "", "\\[ACTION:[^\\]]+\\]", string.Empty, RegexOptions.IgnoreCase).Trim();
			responseText = Regex.Replace(responseText, "\\[AD:[^\\]]+\\]", string.Empty, RegexOptions.IgnoreCase).Trim();
			responseText = Regex.Replace(responseText, "\\[ADP:[^\\]]+\\]", string.Empty, RegexOptions.IgnoreCase).Trim();
			return;
		}
		string giverName = giverCharacter.Name?.ToString() ?? GetSettlementMerchantRoleLabel(kind);
		Regex regex13 = new Regex("\\[ACTION:TRADE_TRUST:(-?\\d+)\\]", RegexOptions.IgnoreCase);
		Regex regex14 = DebtCreationTagRegex;
		Regex regex15 = DebtResolutionTagRegex;
		List<string> merchantFacts = new List<string>();
		List<string> playerFacts = new List<string>();
		int itemTransferAttempted = 0;
		int itemTransferSucceeded = 0;
		int itemTransferFailedOrPartial = 0;
		long itemTransferActualQuantity = 0L;
		long itemTransferActualValue = 0L;
		int goldTransferAttempted = 0;
		int goldTransferSucceeded = 0;
		int goldTransferFailedOrPartial = 0;
		long goldTransferActualAmount = 0L;
		responseText = GiveAssetTagCodec.ReplaceTags(responseText, delegate(GiveAssetTag tag)
		{
			if (!IsGoldAssetTokenForExternal(tag.AssetToken))
			{
				return tag.RawTag;
			}
			if (int.TryParse(tag.QuantityToken, out var result7))
			{
				goldTransferAttempted++;
				int num = TransferGoldFromSettlement(currentSettlement, receiver, result7, giverName, giverCharacter, forceComplete: receiver == Hero.MainHero);
				if (num > 0)
				{
					goldTransferSucceeded++;
					goldTransferActualAmount = TransferQuantitySpec.AddValue(goldTransferActualAmount, num);
					merchantFacts.Add($"你已经将 {num} 第纳尔交给玩家。并进入了玩家的的库存");
					playerFacts.Add($"你从 {giverName} 收到了 {num} 第纳尔。");
					ApplyAutoTrustGainFromMerchantGiftValue(currentSettlement, kind, num, merchantFacts, playerFacts, giverName, giverCharacter);
					if (num < result7)
					{
						goldTransferFailedOrPartial++;
						merchantFacts.Add($"你原计划交付 {result7} 第纳尔，但商铺资金变化后实际只交付了 {num} 第纳尔。");
						playerFacts.Add($"{giverName} 原计划交付 {result7} 第纳尔，但实际只交付了 {num} 第纳尔。");
					}
				}
				else
				{
					goldTransferFailedOrPartial++;
					merchantFacts.Add($"你试图交付 {result7} 第纳尔，但当前商铺现钱不足，本轮未实际支付。");
					playerFacts.Add($"{giverName} 试图交付 {result7} 第纳尔，但当前商铺现钱不足，本轮未实际支付。");
				}
			}
			return string.Empty;
		});
		if (goldTransferAttempted > 0)
		{
			string goldBatchSummary = "金币转移汇总：尝试项" + goldTransferAttempted + "，成功项" + goldTransferSucceeded + "，失败或不足项" + goldTransferFailedOrPartial + "，实际转移" + goldTransferActualAmount + "第纳尔，实际价值" + goldTransferActualAmount + "第纳尔。";
			merchantFacts.Add(goldBatchSummary);
			playerFacts.Add(goldBatchSummary);
			Logger.Log("Logic", "[RewardMerchant] gold_batch_done attempted=" + goldTransferAttempted + " succeeded=" + goldTransferSucceeded + " failedOrPartial=" + goldTransferFailedOrPartial + " actualAmount=" + goldTransferActualAmount);
		}
		responseText = GiveAssetTagCodec.ReplaceTags(responseText, delegate(GiveAssetTag tag)
		{
			string value = (tag.AssetToken ?? "").Trim();
			itemTransferAttempted++;
			bool hasFiniteRequestedQuantity = int.TryParse(tag.QuantityToken, out var requestedQuantity) && requestedQuantity > 0;
			List<RewardItemInfo> authorizedItems = null;
			string authorizedItemKey = "";
			bool isAuthorizedInventoryItem = TryResolveAuthorizedMerchantRewardItem(giverCharacter, value, out authorizedItems, out authorizedItemKey);
			bool isGeneratedRpItem = !isAuthorizedInventoryItem
				&& hasFiniteRequestedQuantity
				&& receiver == Hero.MainHero
				&& IsValidGeneratedRpAssetNameForExternal(value);
			Logger.Log("Logic", "[RewardMerchant] GIVE_ASSET route token=" + value + " inventoryExact=" + isAuthorizedInventoryItem + " rpFallback=" + isGeneratedRpItem);
			if (!isAuthorizedInventoryItem && !isGeneratedRpItem)
			{
				itemTransferFailedOrPartial++;
				return string.Empty;
			}
			if (isAuthorizedInventoryItem)
			{
				value = authorizedItemKey;
			}
			if (TransferQuantitySpec.TryParse(tag.QuantityToken, out var quantity))
			{
				if (TransferQuantitySpec.IsAllValue(value) || (isGeneratedRpItem && quantity.IsAll))
				{
					itemTransferFailedOrPartial++;
					return string.Empty;
				}
				List<RewardItemInfo> allContext = BuildSettlementRewardItemResolutionContext(currentSettlement);
				int result = quantity.Amount;
				if (quantity.IsAll)
				{
					result = ResolveAllRewardItemAmount(value, authorizedItems);
				}
				if (result <= 0)
				{
					itemTransferFailedOrPartial++;
					return string.Empty;
				}
				string itemName;
				ItemObject generatedRpItem = null;
				bool forceCompleteItemTransfer = !quantity.IsAll && receiver == Hero.MainHero;
				int num = isGeneratedRpItem
					? GenerateRpAssetToPlayer(value, result, giverName, giverCharacter, out itemName, out generatedRpItem, "give_asset_rp_merchant", rpItemIntroductionContext)
					: TransferItemFromSettlement(currentSettlement, receiver, value, result, giverName, out itemName, giverCharacter, forceComplete: forceCompleteItemTransfer);
				string text = ((!string.IsNullOrWhiteSpace(itemName)) ? itemName : ResolveSettlementMerchantDisplayNameFromPromptStringId(value));
				ItemObject itemObject = generatedRpItem ?? ResolveItemById(value.Split('@')[0]);
				if (itemObject == null && TryResolveRewardItemStringId(value, allContext, out var _, out var resolvedMerchantFactItem, "merchant_give_item_fact"))
				{
					itemObject = resolvedMerchantFactItem;
					if (string.IsNullOrWhiteSpace(itemName))
					{
						text = itemObject?.Name?.ToString() ?? text;
					}
				}
				if (itemObject == null && num > 0 && receiver == Hero.MainHero && TryResolveRewardItemForForcedGeneration(value, BuildSettlementRewardItemResolutionContext(currentSettlement), out var generatedMerchantFactItem, "merchant_give_item_fact_generate"))
				{
					itemObject = generatedMerchantFactItem.Item;
					if (string.IsNullOrWhiteSpace(itemName))
					{
						text = itemObject?.Name?.ToString() ?? text;
					}
				}
				if (num > 0)
				{
					itemTransferSucceeded++;
					itemTransferActualQuantity = TransferQuantitySpec.AddValue(itemTransferActualQuantity, num);
					itemTransferActualValue = TransferQuantitySpec.AddValue(itemTransferActualValue, GetItemGuideValueForMerchantGift(currentSettlement, itemObject, num));
					string text2 = BuildSettlementItemValueFactSuffixForExternal(currentSettlement, itemObject, num);
					merchantFacts.Add(isGeneratedRpItem ? $"你已经将 RP 物品 {FormatItemAmount(num, itemObject, text)} 交给玩家{text2}，并进入了玩家库存。" : $"你已经将 {FormatItemAmount(num, itemObject, text)} 交给玩家{text2}。并进入了玩家的的库存");
					playerFacts.Add($"你从 {giverName} 收到了 {FormatItemAmount(num, itemObject, text)}{text2}。");
					ApplyAutoTrustGainFromMerchantGiftValue(currentSettlement, kind, GetItemTrustValueForMerchantGift(currentSettlement, itemObject, num), merchantFacts, playerFacts, giverName, giverCharacter);
					if (num < result)
					{
						itemTransferFailedOrPartial++;
						string text3 = BuildSettlementItemValueFactSuffixForExternal(currentSettlement, itemObject, result);
						merchantFacts.Add(isGeneratedRpItem ? $"你原本打算交付 {FormatItemAmount(result, itemObject, text)}{text3}，但实际只生成并交付了 {FormatItemAmount(num, itemObject, text)}{text2}。" : $"你原本打算交付 {FormatItemAmount(result, itemObject, text)}{text3}，但当前商铺库存不足，实际只交付了 {FormatItemAmount(num, itemObject, text)}{text2}。");
						playerFacts.Add($"{giverName} 原本打算交付 {FormatItemAmount(result, itemObject, text)}{text3}，但实际只交付了 {FormatItemAmount(num, itemObject, text)}{text2}。");
					}
				}
				else
				{
					itemTransferFailedOrPartial++;
					string text4 = BuildSettlementItemValueFactSuffixForExternal(currentSettlement, itemObject, result);
					merchantFacts.Add(isGeneratedRpItem ? $"你试图交付 RP 物品 {FormatItemAmount(result, itemObject, text)}，但生成失败，本轮未实际交货。" : $"你试图交付 {FormatItemAmount(result, itemObject, text)}{text4}，但当前商铺库存不足，本轮未实际交货。");
				}
			}
			else
			{
				itemTransferFailedOrPartial++;
			}
			return string.Empty;
		});
		if (itemTransferAttempted > 0)
		{
			string itemBatchSummary = "物品转移汇总：尝试项" + itemTransferAttempted + "，成功项" + itemTransferSucceeded + "，失败或不足项" + itemTransferFailedOrPartial + "，实际转移" + itemTransferActualQuantity + "件，实际指导总值约" + itemTransferActualValue + "第纳尔。";
			merchantFacts.Add(itemBatchSummary);
			playerFacts.Add(itemBatchSummary);
			Logger.Log("Logic", "[RewardMerchant] item_batch_done attempted=" + itemTransferAttempted + " succeeded=" + itemTransferSucceeded + " failedOrPartial=" + itemTransferFailedOrPartial + " actualQuantity=" + itemTransferActualQuantity + " actualValue=" + itemTransferActualValue);
		}
		responseText = regex14.Replace(responseText, delegate(Match m)
		{
			if (!int.TryParse(m.Groups[1].Value, out var result8) || !int.TryParse(m.Groups[2].Value, out var result9))
			{
				return string.Empty;
			}
			string text = GetAdDebtNote(m);
			if (!ShouldApplyAdDebtTag(m, "merchant"))
			{
				return string.Empty;
			}
			if (receiver == Hero.MainHero)
			{
				if (result8 > 0)
				{
					DebtRecord.DebtLine debtLine = SetDebtForSettlementMerchant(currentSettlement, kind, result8, result9, text);
					if (debtLine == null)
					{
						return string.Empty;
					}
					string debtId = debtLine.DebtId ?? "";
					string deadline = BuildDebtPromiseDeadlineText(debtLine.DueDay, debtLine.IsDueUnlimited);
					string note = string.IsNullOrWhiteSpace(text) ? "无" : text;
					string marketLabel = BuildSettlementMerchantDebtLabel(currentSettlement, kind);
					merchantFacts.Add($"你已经记下：玩家的承诺或欠款价值 {result8} 第纳尔，达成期限为：{deadline}，备注：{note}（债务ID:{debtId}）。");
					playerFacts.Add($"你对 {marketLabel} 的承诺或欠款价值 {result8} 第纳尔，达成期限为：{deadline}，备注：{note}（债务ID:{debtId}）。");
				}
			}
			return string.Empty;
		});
		responseText = regex15.Replace(responseText, delegate(Match m)
		{
			string value = (m.Groups[1].Value ?? "").Trim();
			if (receiver == Hero.MainHero)
			{
				string statusText;
				bool flag = ResolveSettlementMerchantDebtByIdByAgreement(currentSettlement, kind, value, out statusText);
				if (!string.IsNullOrWhiteSpace(statusText))
				{
					merchantFacts.Add(statusText);
					playerFacts.Add(statusText);
					bool flag2 = statusText.IndexOf("已按协商解除", StringComparison.OrdinalIgnoreCase) >= 0;
					ShowRewardMessage((flag2 ? "【市场债务解除】" : "【市场债务解除失败】") + statusText, flag2 ? Color.FromUint(4278255360u) : Color.FromUint(4294923605u), giverCharacter);
					if (flag)
					{
						ShowRewardMessage("【市场欠款】当前市场债务已全部结清。", Color.FromUint(4278255360u), giverCharacter);
					}
				}
			}
			return string.Empty;
		});
		if (regex13.IsMatch(responseText))
		{
			MatchCollection matchCollection5 = regex13.Matches(responseText);
			if (matchCollection5 != null && matchCollection5.Count > 0 && int.TryParse(matchCollection5[matchCollection5.Count - 1].Groups[1].Value, out var result8))
			{
				_ = NormalizeLlmTrustDeltaValue(result8);
				Logger.Log("Logic", "[Reward] 提示: 非Hero商贩检测到 [ACTION:TRADE_TRUST]，但即时交易信任现已改为按实际交付价值自动累计，本标签已忽略。");
			}
			responseText = regex13.Replace(responseText, string.Empty);
		}
		responseText = responseText.Trim();
		if (merchantFacts.Count > 0)
		{
			SetLastGeneratedNpcFactLines(new string[1] { "[AFEF NPC行为补充] " + giverName + ": " + string.Join(" ", merchantFacts) });
			AppendSettlementMerchantNpcFact(currentSettlement, kind, string.Join(" ", merchantFacts), giverName);
		}
		if (playerFacts.Count > 0 && receiver == Hero.MainHero)
		{
			MyBehavior.AppendExternalPlayerFact(receiver, string.Join(" ", playerFacts));
			TryRecordNonHeroRewardActionHistory(giverName, receiver, playerFacts, "merchant_reward_action", currentSettlement);
		}
	}

	internal int TransferGold(Hero giver, Hero receiver, int amount)
	{
		return TransferGold(giver, receiver, amount, forceComplete: false);
	}

	internal int TransferGold(Hero giver, Hero receiver, int amount, bool forceComplete)
	{
		if (amount <= 0)
		{
			return 0;
		}
		if (giver == null || receiver == null)
		{
			return 0;
		}
		bool allowForceComplete = forceComplete && receiver == Hero.MainHero && giver != Hero.MainHero;
		int heroGold = GetHeroGold(giver);
		int num = Math.Min(amount, heroGold);
		int generated = allowForceComplete ? Math.Max(0, amount - num) : 0;
		int total = num + generated;
		if (total <= 0)
		{
			return 0;
		}
		if (num > 0)
		{
			GiveGoldAction.ApplyBetweenCharacters(giver, receiver, num);
		}
		if (generated > 0)
		{
			receiver.ChangeHeroGold(generated);
		}
		if (receiver == Hero.MainHero)
		{
			string arg = giver?.Name?.ToString() ?? "某人";
			ShowRewardMessage($"{arg} 给了你 {total} 第纳尔。", giver);
		}
		else if (giver == Hero.MainHero)
		{
			string arg2 = receiver?.Name?.ToString() ?? "某人";
			ShowRewardMessage($"你给了 {arg2} {total} 第纳尔。", receiver);
		}
		return total;
	}

	internal int TransferGoldFromSettlement(Settlement settlement, Hero receiver, int amount, string giverName = null, BasicCharacterObject giverCharacter = null)
	{
		return TransferGoldFromSettlement(settlement, receiver, amount, giverName, giverCharacter, forceComplete: false);
	}

	internal int TransferGoldFromSettlement(Settlement settlement, Hero receiver, int amount, string giverName, BasicCharacterObject giverCharacter, bool forceComplete)
	{
		if (settlement == null || receiver == null || amount <= 0)
		{
			return 0;
		}
		SettlementComponent settlementComponent = ResolveSettlementMarketComponent(settlement);
		if (settlementComponent == null)
		{
			return 0;
		}
		bool allowForceComplete = forceComplete && receiver == Hero.MainHero;
		int num = Math.Min(amount, Math.Max(0, settlementComponent.Gold));
		int generated = allowForceComplete ? Math.Max(0, amount - num) : 0;
		int total = num + generated;
		if (total <= 0)
		{
			return 0;
		}
		if (num > 0)
		{
			settlementComponent.ChangeGold(-num);
		}
		receiver.ChangeHeroGold(total);
		if (receiver == Hero.MainHero)
		{
			string arg = ((!string.IsNullOrWhiteSpace(giverName)) ? giverName : (settlement.Name?.ToString() ?? ("这座" + GetSettlementMarketTypeLabel(settlement) + "的商人")));
			InformationManager.DisplayMessage(new InformationMessage($"{arg} 给了你 {total} 第纳尔。"));
			AnimusForgeQuickInfo.ShowForDuration($"{arg} 给了你 {total} 第纳尔。", RewardQuickInfoDurationMs, giverCharacter);
		}
		return total;
	}

	internal int TransferGoldToSettlement(Settlement settlement, Hero giver, int amount)
	{
		if (settlement == null || giver == null || amount <= 0)
		{
			return 0;
		}
		int num = Math.Min(amount, Math.Max(0, giver.Gold));
		if (num <= 0)
		{
			return 0;
		}
		GiveGoldAction.ApplyBetweenCharacters(giver, null, num, disableNotification: true);
		ResolveSettlementMarketComponent(settlement)?.ChangeGold(num);
		return num;
	}

	internal int TransferGoldFromParty(PartyBase giverParty, Hero receiver, int amount, string giverName = null, BasicCharacterObject giverCharacter = null)
	{
		return TransferGoldFromParty(giverParty, receiver, amount, giverName, giverCharacter, forceComplete: false);
	}

	internal int TransferGoldFromParty(PartyBase giverParty, Hero receiver, int amount, string giverName, BasicCharacterObject giverCharacter, bool forceComplete)
	{
		if (giverParty?.MobileParty == null || receiver == null || amount <= 0)
		{
			return 0;
		}
		bool allowForceComplete = forceComplete && receiver == Hero.MainHero;
		int num = Math.Min(amount, Math.Max(0, giverParty.MobileParty.PartyTradeGold));
		int generated = allowForceComplete ? Math.Max(0, amount - num) : 0;
		int total = num + generated;
		if (total <= 0)
		{
			return 0;
		}
		if (num > 0)
		{
			giverParty.MobileParty.PartyTradeGold -= num;
		}
		receiver.ChangeHeroGold(total);
		if (receiver == Hero.MainHero)
		{
			string arg = string.IsNullOrWhiteSpace(giverName) ? "对方部队" : giverName.Trim();
			InformationManager.DisplayMessage(new InformationMessage($"{arg} 给了你 {total} 第纳尔。"));
			AnimusForgeQuickInfo.ShowForDuration($"{arg} 给了你 {total} 第纳尔。", RewardQuickInfoDurationMs, giverCharacter);
		}
		return total;
	}

	internal int TransferGoldToParty(PartyBase receiverParty, Hero giver, int amount)
	{
		if (receiverParty?.MobileParty == null || giver == null || amount <= 0)
		{
			return 0;
		}
		int num = Math.Min(amount, Math.Max(0, giver.Gold));
		if (num <= 0)
		{
			return 0;
		}
		GiveGoldAction.ApplyBetweenCharacters(giver, null, num, disableNotification: true);
		receiverParty.MobileParty.PartyTradeGold += num;
		return num;
	}

	private static int MoveMatchingItemsByStringId(ItemRoster sourceRoster, ItemRoster targetRoster, string itemStringId, int amount, out EquipmentElement firstTransferredElement, EconomyMutationObservation mutationObservation = null)
	{
		firstTransferredElement = EquipmentElement.Invalid;
		if (sourceRoster == null || targetRoster == null || string.IsNullOrWhiteSpace(itemStringId) || amount <= 0)
		{
			return 0;
		}
		string text = itemStringId.Trim();
		int num = amount;
		int num2 = 0;
		while (num > 0)
		{
			bool flag = false;
			for (int i = 0; i < sourceRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = sourceRoster.GetElementCopyAtIndex(i);
				EquipmentElement equipmentElement = elementCopyAtIndex.EquipmentElement;
				ItemObject item = equipmentElement.Item;
				if (item == null || elementCopyAtIndex.Amount <= 0 || !MatchesItemLookupToken(equipmentElement, text))
				{
					continue;
				}
				int num3 = Math.Min(elementCopyAtIndex.Amount, num);
				if (num3 <= 0)
				{
					continue;
				}
				if (firstTransferredElement.Item == null)
				{
					firstTransferredElement = equipmentElement;
				}
				int added = AddEquipmentElementToRosterAndCountDelta(
					targetRoster,
					equipmentElement,
					num3,
					"move_matching:" + text,
					mutationObservation);
				if (mutationObservation?.UnknownAfterStart == true)
				{
					return num2;
				}
				if (added <= 0)
				{
					LogRewardTransferRosterAdd("move_roster_add_failed", "move_matching:" + text, targetRoster, equipmentElement, num3, 0, 0, -1);
					return num2;
				}
				sourceRoster.AddToCounts(equipmentElement, -added);
				num -= added;
				num2 += added;
				flag = true;
				break;
			}
			if (!flag)
			{
				break;
			}
		}
		return num2;
	}

	private static ItemRoster GetPlayerMainItemRoster()
	{
		try
		{
			ItemRoster roster = PartyBase.MainParty?.ItemRoster;
			if (roster != null)
			{
				return roster;
			}
		}
		catch
		{
		}
		try
		{
			return MobileParty.MainParty?.ItemRoster;
		}
		catch
		{
			return null;
		}
	}

	private static bool IsPlayerMainItemRoster(ItemRoster roster)
	{
		if (roster == null)
		{
			return false;
		}
		try
		{
			if (PartyBase.MainParty?.ItemRoster != null && ReferenceEquals(roster, PartyBase.MainParty.ItemRoster))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			return MobileParty.MainParty?.ItemRoster != null && ReferenceEquals(roster, MobileParty.MainParty.ItemRoster);
		}
		catch
		{
			return false;
		}
	}

	private static ItemRoster ResolveReceiverItemRosterForGive(Hero receiver)
	{
		if (receiver == null)
		{
			return null;
		}
		if (receiver == Hero.MainHero)
		{
			return GetPlayerMainItemRoster();
		}
		ItemRoster roster = receiver.PartyBelongedTo?.ItemRoster;
		if (roster == null && receiver.Clan?.Leader?.PartyBelongedTo != null)
		{
			roster = receiver.Clan.Leader.PartyBelongedTo.ItemRoster;
		}
		return roster;
	}

	private static bool ItemObjectsMatchForRosterCount(ItemObject left, ItemObject right)
	{
		if (left == null || right == null)
		{
			return false;
		}
		if (ReferenceEquals(left, right))
		{
			return true;
		}
		try
		{
			uint leftId = left.Id.InternalValue;
			uint rightId = right.Id.InternalValue;
			if (leftId != 0u && leftId == rightId)
			{
				return true;
			}
		}
		catch
		{
		}
		return string.Equals((left.StringId ?? "").Trim(), (right.StringId ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
	}

	private static bool EquipmentElementsMatchForRosterCount(EquipmentElement left, EquipmentElement right)
	{
		if (!ItemObjectsMatchForRosterCount(left.Item, right.Item))
		{
			return false;
		}
		string leftModifier = left.ItemModifier?.StringId ?? "";
		string rightModifier = right.ItemModifier?.StringId ?? "";
		return string.Equals(leftModifier.Trim(), rightModifier.Trim(), StringComparison.OrdinalIgnoreCase);
	}

	private static int CountEquipmentElementInRoster(ItemRoster roster, EquipmentElement equipmentElement)
	{
		if (roster == null || equipmentElement.Item == null)
		{
			return 0;
		}
		int count = 0;
		for (int i = 0; i < roster.Count; i++)
		{
			ItemRosterElement element = roster.GetElementCopyAtIndex(i);
			if (element.Amount > 0 && EquipmentElementsMatchForRosterCount(element.EquipmentElement, equipmentElement))
			{
				count += element.Amount;
			}
		}
		return count;
	}

	private static int AddEquipmentElementToRosterAndCountDelta(ItemRoster targetRoster, EquipmentElement equipmentElement, int amount, string logSource, EconomyMutationObservation mutationObservation = null)
	{
		if (targetRoster == null || equipmentElement.Item == null || amount <= 0)
		{
			return 0;
		}
		int before = CountEquipmentElementInRoster(targetRoster, equipmentElement);
		int index = -1;
		try
		{
			index = targetRoster.AddToCounts(equipmentElement, amount);
		}
		catch (Exception ex)
		{
			mutationObservation?.MarkUnknown("economy.roster_add_exception");
			try
			{
				Logger.Log("Logic", "[RewardTransfer] roster_add_exception source=" + (logSource ?? "") + " requested=" + amount.ToString(CultureInfo.InvariantCulture) + " item=" + (equipmentElement.Item.StringId ?? "") + " error=" + ex.GetType().Name + ":" + ex.Message);
			}
			catch
			{
			}
			return 0;
		}
		int after = CountEquipmentElementInRoster(targetRoster, equipmentElement);
		int delta = Math.Max(0, after - before);
		if (delta <= 0 || delta < amount)
		{
			LogRewardTransferRosterAdd(delta <= 0 ? "roster_add_no_delta" : "roster_add_partial_delta", logSource, targetRoster, equipmentElement, amount, before, after, index);
		}
		if (delta > 0)
		{
			LogRewardTransferRosterAdd("roster_add_verified", logSource, targetRoster, equipmentElement, amount, before, after, index);
			LogGeneratedRewardObjectVisibility("roster_add_verified", equipmentElement.Item, logSource);
			RememberGeneratedRewardPlayerRosterItemIfNeeded(targetRoster, equipmentElement, delta, logSource);
		}
		return Math.Min(amount, delta);
	}

	private static void LogRewardTransferRosterAdd(string reason, string source, ItemRoster targetRoster, EquipmentElement equipmentElement, int requested, int before, int after, int index)
	{
		try
		{
			ItemObject item = equipmentElement.Item;
			Logger.Log("Logic", "[RewardTransfer] " + (reason ?? "") + " source=" + (source ?? "") + " requested=" + requested.ToString(CultureInfo.InvariantCulture) + " before=" + before.ToString(CultureInfo.InvariantCulture) + " after=" + after.ToString(CultureInfo.InvariantCulture) + " index=" + index.ToString(CultureInfo.InvariantCulture) + " rosterSlots=" + (targetRoster?.Count ?? 0).ToString(CultureInfo.InvariantCulture) + " isPlayerRoster=" + IsPlayerMainItemRoster(targetRoster) + " item=" + (item?.StringId ?? "") + " itemName=" + FormatGeneratedRewardNameForLog(item?.StringId, item?.Name?.ToString()) + " itemId=" + (item != null ? item.Id.InternalValue.ToString(CultureInfo.InvariantCulture) : "0") + " itemType=" + (item != null ? item.Type.ToString() : "") + " itemCategory=" + (item?.ItemCategory?.StringId ?? "null") + " component=" + (item?.ItemComponent?.GetType().Name ?? "null") + " modifier=" + (equipmentElement.ItemModifier?.StringId ?? ""));
		}
		catch
		{
		}
	}

	private static void RememberGeneratedRewardPlayerRosterItemIfNeeded(ItemRoster targetRoster, EquipmentElement equipmentElement, int amount, string source)
	{
		if (amount <= 0 || !IsPlayerMainItemRoster(targetRoster))
		{
			return;
		}
		Instance?.RememberGeneratedRewardPlayerRosterItem(equipmentElement, amount, source);
	}

	internal static bool TryQueueNpcBattleEquipmentRestoreForExternal(Hero hero, EquipmentIndex slot, EquipmentElement equipmentElement, string source)
	{
		RewardSystemBehavior instance = Instance;
		return instance != null && instance.TryQueueNpcBattleEquipmentRestore(hero, slot, equipmentElement, source);
	}

	private bool TryQueueNpcBattleEquipmentRestore(Hero hero, EquipmentIndex slot, EquipmentElement equipmentElement, string source, EconomyMutationObservation mutationObservation = null)
	{
		try
		{
			if (hero == null || hero == Hero.MainHero || hero.IsDead || !CanTransferNpcBattleEquipment(slot, equipmentElement))
			{
				return false;
			}
			string heroId = NormalizeHeroId(hero);
			string itemId = (equipmentElement.Item.StringId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(heroId) || string.IsNullOrWhiteSpace(itemId))
			{
				return false;
			}
			if (_pendingNpcBattleEquipmentRestoreRecords == null)
			{
				_pendingNpcBattleEquipmentRestoreRecords = new Dictionary<string, PendingNpcBattleEquipmentRestoreRecord>(StringComparer.OrdinalIgnoreCase);
			}
			float restoreDay = GetNowCampaignDay() + NpcBattleEquipmentRestoreDelayDays;
			PendingNpcBattleEquipmentRestoreSlot pendingSlot = new PendingNpcBattleEquipmentRestoreSlot
			{
				SlotIndex = (int)slot,
				ItemId = itemId,
				ModifierId = (equipmentElement.ItemModifier?.StringId ?? "").Trim(),
				CosmeticItemId = (equipmentElement.CosmeticItem?.StringId ?? "").Trim(),
				IsQuestItem = equipmentElement.IsQuestItem,
				RestoreOnOrAfterDay = restoreDay
			};
			if (!_pendingNpcBattleEquipmentRestoreRecords.TryGetValue(heroId, out PendingNpcBattleEquipmentRestoreRecord record) || record == null)
			{
				record = new PendingNpcBattleEquipmentRestoreRecord();
				_pendingNpcBattleEquipmentRestoreRecords[heroId] = record;
			}
			if (record.Slots == null)
			{
				record.Slots = new List<PendingNpcBattleEquipmentRestoreSlot>();
			}
			int existingSlotIndex = -1;
			for (int i = record.Slots.Count - 1; i >= 0; i--)
			{
				if (record.Slots[i] != null && record.Slots[i].SlotIndex == (int)slot)
				{
					existingSlotIndex = i;
					break;
				}
			}
			if (existingSlotIndex >= 0)
			{
				record.Slots[existingSlotIndex] = pendingSlot;
			}
			else
			{
				record.Slots.Add(pendingSlot);
			}
			Logger.Log("RewardSystem", "[NpcEquipmentRestore] queued hero=" + heroId + " slot=" + ((int)slot).ToString(CultureInfo.InvariantCulture) + " item=" + itemId + " restoreDay=" + restoreDay.ToString("0.00", CultureInfo.InvariantCulture) + " source=" + (source ?? ""));
			return true;
		}
		catch (Exception ex)
		{
			mutationObservation?.MarkUnknown("economy.equipment_restore_queue_exception");
			Logger.Log("RewardSystem", "[NpcEquipmentRestore] queue failed hero=" + (hero?.StringId ?? "") + " slot=" + ((int)slot).ToString(CultureInfo.InvariantCulture) + " error=" + ex.Message);
			return false;
		}
	}

	internal int TransferItemById(Hero giver, Hero receiver, string itemStringId, int amount, out string itemName)
	{
		return TransferItemById(giver, receiver, itemStringId, amount, out itemName, forceComplete: false);
	}

	internal int TransferItemById(Hero giver, Hero receiver, string itemStringId, int amount, out string itemName, bool forceComplete)
	{
		return TransferItemByIdCore(
			giver,
			receiver,
			itemStringId,
			amount,
			out itemName,
			forceComplete,
			mutationObservation: null);
	}

	private int TransferItemByIdForEconomyReplay(Hero giver, Hero receiver, string itemStringId, int amount, out string itemName, bool forceComplete, EconomyMutationObservation mutationObservation)
	{
		return TransferItemByIdCore(
			giver,
			receiver,
			itemStringId,
			amount,
			out itemName,
			forceComplete,
			mutationObservation);
	}

	private int TransferItemByIdCore(Hero giver, Hero receiver, string itemStringId, int amount, out string itemName, bool forceComplete, EconomyMutationObservation mutationObservation)
	{
		itemName = null;
		if (string.IsNullOrEmpty(itemStringId) || amount <= 0)
		{
			return 0;
		}
		if (giver == null || receiver == null)
		{
			return 0;
		}
		bool allowForceComplete = forceComplete && receiver == Hero.MainHero && giver != Hero.MainHero;
		bool useNameResolution = allowForceComplete || giver == Hero.MainHero;
		List<RewardItemInfo> contextItems = useNameResolution ? BuildHeroRewardItemResolutionContext(giver) : null;
		RewardItemResolution resolution = null;
		string lookup = itemStringId.Trim();
		if (useNameResolution && TryResolveRewardItemByNameOrId(lookup, contextItems, out resolution, "hero"))
		{
			string resolvedLookup = BuildRewardItemTransferLookup(resolution);
			if (!string.IsNullOrWhiteSpace(resolvedLookup))
			{
				lookup = resolvedLookup;
			}
		}
		ItemRoster itemRoster = ((giver.PartyBelongedTo != null) ? giver.PartyBelongedTo.ItemRoster : null);
		if (itemRoster == null && giver.Clan?.Leader?.PartyBelongedTo != null)
		{
			itemRoster = giver.Clan.Leader.PartyBelongedTo.ItemRoster;
		}
		if (itemRoster == null && MobileParty.MainParty != null && giver == Hero.MainHero)
		{
			itemRoster = MobileParty.MainParty.ItemRoster;
		}
		ItemRoster itemRoster2 = ResolveReceiverItemRosterForGive(receiver);
		if (itemRoster2 == null)
		{
			return 0;
		}
		ItemObject itemObject = null;
		int num = 0;
		if (itemRoster != null)
		{
			for (int i = 0; i < itemRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
				ItemObject item = elementCopyAtIndex.EquipmentElement.Item;
				if (item != null && MatchesItemLookupToken(elementCopyAtIndex.EquipmentElement, lookup))
				{
					itemObject = item;
					num += elementCopyAtIndex.Amount;
				}
			}
		}
		List<EquipmentIndex> list = new List<EquipmentIndex>();
		EquipmentIndex[] array = new EquipmentIndex[12]
		{
			EquipmentIndex.NumAllWeaponSlots,
			EquipmentIndex.Body,
			EquipmentIndex.Leg,
			EquipmentIndex.Gloves,
			EquipmentIndex.Cape,
			EquipmentIndex.WeaponItemBeginSlot,
			EquipmentIndex.Weapon1,
			EquipmentIndex.Weapon2,
			EquipmentIndex.Weapon3,
			EquipmentIndex.ExtraWeaponSlot,
			EquipmentIndex.Horse,
			EquipmentIndex.HorseHarness
		};
		EquipmentIndex[] array2 = array;
		EquipmentIndex[] array3 = array2;
		foreach (EquipmentIndex equipmentIndex in array3)
		{
			EquipmentElement equipmentElement2 = giver.BattleEquipment[equipmentIndex];
			ItemObject item2 = equipmentElement2.Item;
			if (CanTransferNpcBattleEquipment(equipmentIndex, equipmentElement2) && MatchesItemLookupToken(equipmentElement2, lookup))
			{
				if (itemObject == null)
				{
					itemObject = item2;
				}
				list.Add(equipmentIndex);
			}
		}
		num += list.Count;
		if (!allowForceComplete && (itemObject == null || num <= 0))
		{
			return 0;
		}
		int num2 = allowForceComplete ? amount : Math.Min(amount, num);
		if (num2 <= 0)
		{
			return 0;
		}
		int num3 = Math.Min(num2, Math.Max(0, num));
		int num4 = 0;
		EquipmentElement firstTransferredElement = EquipmentElement.Invalid;
		if (itemRoster != null)
		{
			int movedFromRoster = MoveMatchingItemsByStringId(
				itemRoster,
				itemRoster2,
				lookup,
				num3,
				out var equipmentElement,
				mutationObservation);
			num3 -= movedFromRoster;
			num4 += movedFromRoster;
			if (mutationObservation?.UnknownAfterStart == true)
			{
				return num4;
			}
			if (firstTransferredElement.Item == null && equipmentElement.Item != null)
			{
				firstTransferredElement = equipmentElement;
			}
			if (itemObject == null && equipmentElement.Item != null)
			{
				itemObject = equipmentElement.Item;
			}
			if (string.IsNullOrWhiteSpace(itemName) && equipmentElement.Item != null)
			{
				itemName = equipmentElement.GetModifiedItemName()?.ToString() ?? equipmentElement.Item.Name?.ToString() ?? itemStringId;
			}
		}
		bool queueNpcBattleEquipmentRestore = giver != Hero.MainHero && DuelSettings.IsNpcBattleEquipmentRestoreEnabled();
		for (int l = 0; l < list.Count; l++)
		{
			if (num3 <= 0)
			{
				break;
			}
			EquipmentIndex index = list[l];
			EquipmentElement equipmentElement2 = giver.BattleEquipment[index];
			ItemObject item4 = equipmentElement2.Item;
			if (CanTransferNpcBattleEquipment(index, equipmentElement2) && item4 != null && MatchesItemLookupToken(equipmentElement2, lookup))
			{
				int addedFromEquipment = AddEquipmentElementToRosterAndCountDelta(
					itemRoster2,
					equipmentElement2,
					1,
					"hero_equipment:" + lookup,
					mutationObservation);
				if (mutationObservation?.UnknownAfterStart == true)
				{
					return num4;
				}
				if (addedFromEquipment <= 0)
				{
					continue;
				}
				if (queueNpcBattleEquipmentRestore && !TryQueueNpcBattleEquipmentRestore(
					giver,
					index,
					equipmentElement2,
					"hero_item_transfer",
					mutationObservation))
				{
					int beforeRollback = mutationObservation != null
						? CountEquipmentElementInRoster(itemRoster2, equipmentElement2)
						: 0;
					try
					{
						itemRoster2.AddToCounts(equipmentElement2, -addedFromEquipment);
					}
					catch (Exception ex)
					{
						mutationObservation?.MarkUnknown("economy.equipment_restore_rollback_exception");
						Logger.Log("RewardSystem", "[NpcEquipmentRestore] rollback failed hero=" + (giver.StringId ?? "") + " slot=" + ((int)index).ToString(CultureInfo.InvariantCulture) + " error=" + ex.Message);
					}
					if (mutationObservation != null && !mutationObservation.UnknownAfterStart)
					{
						int afterRollback = CountEquipmentElementInRoster(itemRoster2, equipmentElement2);
						if (Math.Max(0, beforeRollback - afterRollback) < addedFromEquipment)
						{
							mutationObservation.MarkUnknown("economy.equipment_restore_rollback_unverified");
						}
					}
					Logger.Log("RewardSystem", "[NpcEquipmentRestore] transfer kept source equipped because queue failed hero=" + (giver.StringId ?? "") + " slot=" + ((int)index).ToString(CultureInfo.InvariantCulture));
					if (mutationObservation?.UnknownAfterStart == true)
					{
						return num4;
					}
					continue;
				}
				giver.BattleEquipment[index] = EquipmentElement.Invalid;
				num4 += addedFromEquipment;
				if (firstTransferredElement.Item == null)
				{
					firstTransferredElement = equipmentElement2;
				}
				if (itemObject == null)
				{
					itemObject = item4;
				}
				if (string.IsNullOrWhiteSpace(itemName))
				{
					itemName = equipmentElement2.GetModifiedItemName()?.ToString() ?? item4.Name?.ToString() ?? itemStringId;
				}
				num3--;
			}
		}
		int generated = 0;
		if (allowForceComplete && num4 < amount)
		{
			if (resolution == null && firstTransferredElement.Item != null)
			{
				ItemObject resolvedItem = firstTransferredElement.Item;
				resolution = new RewardItemResolution
				{
					Item = resolvedItem,
					EquipmentElement = firstTransferredElement,
					ActionKey = BuildSettlementMerchantInventoryKey(firstTransferredElement),
					MatchedName = firstTransferredElement.GetModifiedItemName()?.ToString() ?? resolvedItem.Name?.ToString() ?? resolvedItem.StringId,
					MatchedStringId = resolvedItem.StringId ?? "",
					BestScore = 1f,
					SecondScore = 0f,
					IsContext = true
				};
			}
			if (resolution == null)
			{
				TryResolveRewardItemForForcedGeneration(itemStringId, contextItems, out resolution, "hero_generate");
			}
			if (resolution?.Item != null)
			{
				generated = GenerateResolvedItemsToRoster(
					itemRoster2,
					resolution,
					amount - num4,
					out var generatedItemName,
					mutationObservation);
				if (generated > 0)
				{
					itemObject = resolution.Item;
					if (string.IsNullOrWhiteSpace(itemName))
					{
						itemName = generatedItemName;
					}
				}
			}
		}
		int total = num4 + generated;
		if (total <= 0)
		{
			return 0;
		}
		if (itemObject != null)
		{
			itemName = itemName ?? itemObject.Name?.ToString() ?? itemStringId;
		}
		if (receiver == Hero.MainHero)
		{
			string arg = giver?.Name?.ToString() ?? "某人";
			string arg2 = itemName ?? itemStringId;
			ShowRewardMessage($"{arg} 给了你 {FormatItemAmount(total, itemObject, arg2)}。", giver);
		}
		else if (giver == Hero.MainHero)
		{
			string arg3 = receiver?.Name?.ToString() ?? "某人";
			string arg4 = itemName ?? itemStringId;
			ShowRewardMessage($"你给了 {arg3} {FormatItemAmount(total, itemObject, arg4)}。", receiver);
		}
		return total;
	}

	internal int TransferItemFromParty(PartyBase giverParty, Hero receiver, string itemStringId, int amount, string giverName, out string itemName, BasicCharacterObject giverCharacter = null)
	{
		return TransferItemFromParty(giverParty, receiver, itemStringId, amount, giverName, out itemName, giverCharacter, forceComplete: false);
	}

	internal int TransferItemFromParty(PartyBase giverParty, Hero receiver, string itemStringId, int amount, string giverName, out string itemName, BasicCharacterObject giverCharacter, bool forceComplete)
	{
		return TransferItemFromPartyCore(
			giverParty,
			receiver,
			itemStringId,
			amount,
			giverName,
			out itemName,
			giverCharacter,
			forceComplete,
			mutationObservation: null);
	}

	private int TransferItemFromPartyForEconomyReplay(PartyBase giverParty, Hero receiver, string itemStringId, int amount, string giverName, out string itemName, BasicCharacterObject giverCharacter, bool forceComplete, EconomyMutationObservation mutationObservation)
	{
		return TransferItemFromPartyCore(
			giverParty,
			receiver,
			itemStringId,
			amount,
			giverName,
			out itemName,
			giverCharacter,
			forceComplete,
			mutationObservation);
	}

	private int TransferItemFromPartyCore(PartyBase giverParty, Hero receiver, string itemStringId, int amount, string giverName, out string itemName, BasicCharacterObject giverCharacter, bool forceComplete, EconomyMutationObservation mutationObservation)
	{
		itemName = null;
		if (giverParty == null || receiver == null || string.IsNullOrWhiteSpace(itemStringId) || amount <= 0)
		{
			return 0;
		}
		ItemRoster itemRoster = giverParty.ItemRoster;
		ItemRoster itemRoster2 = ResolveReceiverItemRosterForGive(receiver);
		if (itemRoster == null || itemRoster2 == null)
		{
			return 0;
		}
		bool allowForceComplete = forceComplete && receiver == Hero.MainHero;
		List<RewardItemInfo> contextItems = allowForceComplete ? BuildPartyRewardItemResolutionContext(giverParty) : null;
		RewardItemResolution resolution = null;
		string lookup = itemStringId.Trim();
		if (allowForceComplete && TryResolveRewardItemByNameOrId(lookup, contextItems, out resolution, "party"))
		{
			string resolvedLookup = BuildRewardItemTransferLookup(resolution);
			if (!string.IsNullOrWhiteSpace(resolvedLookup))
			{
				lookup = resolvedLookup;
			}
		}
		int num = MoveMatchingItemsByStringId(
			itemRoster,
			itemRoster2,
			lookup,
			amount,
			out var equipmentElement,
			mutationObservation);
		if (mutationObservation?.UnknownAfterStart == true)
		{
			return num;
		}
		ItemObject itemObject = equipmentElement.Item ?? ResolveItemById((itemStringId ?? "").Split('@')[0]);
		if (allowForceComplete && num < amount)
		{
			if (resolution == null && equipmentElement.Item != null)
			{
				ItemObject resolvedItem = equipmentElement.Item;
				resolution = new RewardItemResolution
				{
					Item = resolvedItem,
					EquipmentElement = equipmentElement,
					ActionKey = BuildSettlementMerchantInventoryKey(equipmentElement),
					MatchedName = equipmentElement.GetModifiedItemName()?.ToString() ?? resolvedItem.Name?.ToString() ?? resolvedItem.StringId,
					MatchedStringId = resolvedItem.StringId ?? "",
					BestScore = 1f,
					SecondScore = 0f,
					IsContext = true
				};
			}
			if (resolution == null)
			{
				TryResolveRewardItemForForcedGeneration(itemStringId, contextItems, out resolution, "party_generate");
			}
			if (resolution?.Item != null)
			{
				int generated = GenerateResolvedItemsToRoster(
					itemRoster2,
					resolution,
					amount - num,
					out var generatedItemName,
					mutationObservation);
				if (generated > 0)
				{
					num += generated;
					itemObject = resolution.Item;
					if (string.IsNullOrWhiteSpace(itemName))
					{
						itemName = generatedItemName;
					}
				}
			}
		}
		if (num > 0)
		{
			if (string.IsNullOrWhiteSpace(itemName))
			{
				itemName = (equipmentElement.Item != null) ? (equipmentElement.GetModifiedItemName()?.ToString() ?? equipmentElement.Item.Name?.ToString() ?? itemStringId) : (itemObject?.Name?.ToString() ?? itemStringId);
			}
			if (receiver == Hero.MainHero)
			{
				string arg = string.IsNullOrWhiteSpace(giverName) ? "对方部队" : giverName.Trim();
				InformationManager.DisplayMessage(new InformationMessage($"{arg} 给了你 {FormatItemAmount(num, itemObject, itemName)}。"));
				AnimusForgeQuickInfo.ShowForDuration($"{arg} 给了你 {FormatItemAmount(num, itemObject, itemName)}。", RewardQuickInfoDurationMs, giverCharacter);
			}
		}
		return num;
	}

	internal int TransferItemToParty(PartyBase receiverParty, Hero giver, string itemStringId, int amount, out string itemName)
	{
		itemName = null;
		if (receiverParty == null || giver == null || string.IsNullOrWhiteSpace(itemStringId) || amount <= 0)
		{
			return 0;
		}
		string lookup = itemStringId.Trim();
		if (giver == Hero.MainHero && TryResolveRewardItemByNameOrId(lookup, BuildHeroRewardItemResolutionContext(giver), out var resolution, "player_to_party"))
		{
			string resolvedLookup = BuildRewardItemTransferLookup(resolution);
			if (!string.IsNullOrWhiteSpace(resolvedLookup))
			{
				lookup = resolvedLookup;
			}
		}
		ItemRoster itemRoster = ((giver.PartyBelongedTo != null) ? giver.PartyBelongedTo.ItemRoster : null) ?? MobileParty.MainParty?.ItemRoster;
		ItemRoster itemRoster2 = receiverParty.ItemRoster;
		if (itemRoster == null || itemRoster2 == null)
		{
			return 0;
		}
		int num = MoveMatchingItemsByStringId(itemRoster, itemRoster2, lookup, amount, out var equipmentElement);
		ItemObject itemObject = equipmentElement.Item ?? ResolveItemById((lookup ?? "").Split('@')[0]);
		if (num > 0)
		{
			itemName = (equipmentElement.Item != null) ? (equipmentElement.GetModifiedItemName()?.ToString() ?? equipmentElement.Item.Name?.ToString() ?? itemStringId) : (itemObject?.Name?.ToString() ?? itemStringId);
			if (giver == Hero.MainHero)
			{
				ShowRewardMessage($"你给了对方部队 {FormatItemAmount(num, itemObject, itemName)}。", giver);
			}
		}
		return num;
	}

	internal int TransferItemFromSettlement(Settlement settlement, Hero receiver, string itemStringId, int amount, string giverName, out string itemName, BasicCharacterObject giverCharacter = null)
	{
		return TransferItemFromSettlement(settlement, receiver, itemStringId, amount, giverName, out itemName, giverCharacter, forceComplete: false);
	}

	internal int TransferItemFromSettlement(Settlement settlement, Hero receiver, string itemStringId, int amount, string giverName, out string itemName, BasicCharacterObject giverCharacter, bool forceComplete)
	{
		return TransferItemFromSettlementCore(
			settlement,
			receiver,
			itemStringId,
			amount,
			giverName,
			out itemName,
			giverCharacter,
			forceComplete,
			mutationObservation: null);
	}

	private int TransferItemFromSettlementForEconomyReplay(Settlement settlement, Hero receiver, string itemStringId, int amount, string giverName, out string itemName, BasicCharacterObject giverCharacter, bool forceComplete, EconomyMutationObservation mutationObservation)
	{
		return TransferItemFromSettlementCore(
			settlement,
			receiver,
			itemStringId,
			amount,
			giverName,
			out itemName,
			giverCharacter,
			forceComplete,
			mutationObservation);
	}

	private int TransferItemFromSettlementCore(Settlement settlement, Hero receiver, string itemStringId, int amount, string giverName, out string itemName, BasicCharacterObject giverCharacter, bool forceComplete, EconomyMutationObservation mutationObservation)
	{
		itemName = null;
		if (settlement == null || receiver == null || string.IsNullOrWhiteSpace(itemStringId) || amount <= 0)
		{
			return 0;
		}
		ItemRoster itemRoster = settlement.ItemRoster;
		ItemRoster itemRoster2 = ResolveReceiverItemRosterForGive(receiver);
		if (itemRoster == null || itemRoster2 == null)
		{
			return 0;
		}
		bool allowForceComplete = forceComplete && receiver == Hero.MainHero;
		List<RewardItemInfo> contextItems = allowForceComplete ? BuildSettlementRewardItemResolutionContext(settlement) : null;
		RewardItemResolution resolution = null;
		string lookup = itemStringId.Trim();
		if (allowForceComplete && TryResolveRewardItemByNameOrId(lookup, contextItems, out resolution, "settlement"))
		{
			string resolvedLookup = BuildRewardItemTransferLookup(resolution);
			if (!string.IsNullOrWhiteSpace(resolvedLookup))
			{
				lookup = resolvedLookup;
			}
		}
		string requestedItemId;
		string requestedModifierId;
		bool requestedHasModifier = TryParseSettlementMerchantPromptStringId(lookup, out requestedItemId, out requestedModifierId) && !string.IsNullOrWhiteSpace(requestedModifierId);
		if (!requestedHasModifier && !string.IsNullOrWhiteSpace(requestedItemId))
		{
			HashSet<string> variantKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			bool hasExactRequestedKey = false;
			for (int i = 0; i < itemRoster.Count; i++)
			{
				ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
				EquipmentElement equipmentElement2 = elementCopyAtIndex.EquipmentElement;
				ItemObject item = equipmentElement2.Item;
				if (item != null && elementCopyAtIndex.Amount > 0 && !IsGeneratedRewardMarketExcludedItem(item) && string.Equals(item.StringId ?? "", requestedItemId, StringComparison.OrdinalIgnoreCase))
				{
					string text = BuildSettlementMerchantInventoryKey(equipmentElement2);
					if (!string.IsNullOrWhiteSpace(text))
					{
						variantKeys.Add(text);
						if (string.Equals(text, lookup, StringComparison.OrdinalIgnoreCase))
						{
							hasExactRequestedKey = true;
						}
					}
				}
			}
			if (!hasExactRequestedKey && variantKeys.Count == 1)
			{
				lookup = variantKeys.First();
			}
			else if (!hasExactRequestedKey && variantKeys.Count > 1)
			{
				try
				{
					Logger.Log("Logic", "[RewardTransfer] ambiguous_settlement_item itemId=" + requestedItemId + " variants=" + string.Join("|", variantKeys) + " rawTag=" + itemStringId);
				}
				catch
				{
				}
				if (!allowForceComplete)
				{
					return 0;
				}
			}
		}
		EquipmentElement equipmentElement = EquipmentElement.Invalid;
		int num = 0;
		for (int i = 0; i < itemRoster.Count; i++)
		{
			ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
			if (MatchesSettlementMerchantPromptStringId(elementCopyAtIndex.EquipmentElement, lookup))
			{
				equipmentElement = elementCopyAtIndex.EquipmentElement;
				num += Math.Max(0, elementCopyAtIndex.Amount);
			}
		}
		if (!allowForceComplete && (equipmentElement.Item == null || num <= 0))
		{
			return 0;
		}
		int num2 = Math.Min(amount, Math.Max(0, num));
		if (num2 > 0 && equipmentElement.Item != null)
		{
			int movedFromSettlement = AddEquipmentElementToRosterAndCountDelta(
				itemRoster2,
				equipmentElement,
				num2,
				"fixed_asset_transfer:" + lookup,
				mutationObservation);
			if (mutationObservation?.UnknownAfterStart == true)
			{
				return 0;
			}
			if (movedFromSettlement > 0)
			{
				itemRoster.AddToCounts(equipmentElement, -movedFromSettlement);
				itemName = BuildSettlementMerchantDisplayName(equipmentElement);
			}
			num2 = movedFromSettlement;
		}
		if (allowForceComplete && num2 < amount)
		{
			if (resolution == null && equipmentElement.Item != null)
			{
				ItemObject resolvedItem = equipmentElement.Item;
				resolution = new RewardItemResolution
				{
					Item = resolvedItem,
					EquipmentElement = equipmentElement,
					ActionKey = BuildSettlementMerchantInventoryKey(equipmentElement),
					MatchedName = BuildSettlementMerchantDisplayName(equipmentElement),
					MatchedStringId = resolvedItem.StringId ?? "",
					BestScore = 1f,
					SecondScore = 0f,
					IsContext = true
				};
			}
			if (resolution == null)
			{
				TryResolveRewardItemForForcedGeneration(itemStringId, contextItems, out resolution, "settlement_generate");
			}
			if (resolution?.Item != null)
			{
				int generated = GenerateResolvedItemsToRoster(
					itemRoster2,
					resolution,
					amount - num2,
					out var generatedItemName,
					mutationObservation);
				if (generated > 0)
				{
					num2 += generated;
					equipmentElement = resolution.EquipmentElement.Item != null ? resolution.EquipmentElement : new EquipmentElement(resolution.Item, null, null, false);
					if (string.IsNullOrWhiteSpace(itemName))
					{
						itemName = generatedItemName;
					}
				}
			}
		}
		if (num2 <= 0)
		{
			return 0;
		}
		ItemObject displayItem = equipmentElement.Item ?? resolution?.Item;
		if (string.IsNullOrWhiteSpace(itemName))
		{
			itemName = resolution?.MatchedName ?? displayItem?.Name?.ToString() ?? itemStringId;
		}
		if (receiver == Hero.MainHero)
		{
			string arg = ((!string.IsNullOrWhiteSpace(giverName)) ? giverName : (settlement.Name?.ToString() ?? ("这座" + GetSettlementMarketTypeLabel(settlement) + "的商人")));
			InformationManager.DisplayMessage(new InformationMessage($"{arg} 给了你 {FormatItemAmount(num2, displayItem, itemName)}。"));
			AnimusForgeQuickInfo.ShowForDuration($"{arg} 给了你 {FormatItemAmount(num2, displayItem, itemName)}。", RewardQuickInfoDurationMs, giverCharacter);
		}
		return num2;
	}

	internal int TransferItemToSettlement(Settlement settlement, Hero giver, string itemStringId, int amount, out string itemName)
	{
		itemName = null;
		if (settlement == null || giver == null || string.IsNullOrWhiteSpace(itemStringId) || amount <= 0)
		{
			return 0;
		}
		string lookup = itemStringId.Trim();
		if (giver == Hero.MainHero && TryResolveRewardItemByNameOrId(lookup, BuildHeroRewardItemResolutionContext(giver), out var resolution, "player_to_settlement"))
		{
			string resolvedLookup = BuildRewardItemTransferLookup(resolution);
			if (!string.IsNullOrWhiteSpace(resolvedLookup))
			{
				lookup = resolvedLookup;
			}
		}
		ItemRoster itemRoster = ((giver.PartyBelongedTo != null) ? giver.PartyBelongedTo.ItemRoster : null) ?? MobileParty.MainParty?.ItemRoster;
		ItemRoster itemRoster2 = settlement.ItemRoster;
		if (itemRoster == null || itemRoster2 == null)
		{
			return 0;
		}
		ItemObject itemObject = null;
		int num = 0;
		for (int i = 0; i < itemRoster.Count; i++)
		{
			ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(i);
			ItemObject item = elementCopyAtIndex.EquipmentElement.Item;
			if (item != null && !IsGeneratedRewardMarketExcludedItem(item) && MatchesItemLookupToken(elementCopyAtIndex.EquipmentElement, lookup))
			{
				itemObject = item;
				num += Math.Max(0, elementCopyAtIndex.Amount);
			}
		}
		if (itemObject == null || num <= 0)
		{
			return 0;
		}
		int num2 = Math.Min(amount, num);
		if (num2 <= 0)
		{
			return 0;
		}
		int num3 = MoveMatchingItemsByStringId(itemRoster, itemRoster2, lookup, num2, out var equipmentElement);
		if (num3 > 0)
		{
			itemName = (equipmentElement.Item != null) ? (equipmentElement.GetModifiedItemName()?.ToString() ?? equipmentElement.Item.Name?.ToString() ?? itemStringId) : (itemObject.Name?.ToString() ?? itemStringId);
		}
		return num3;
	}
}
