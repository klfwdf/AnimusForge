using System;

namespace TaleWorlds.CampaignSystem
{
    public sealed class Hero { public string StringId; }
    public sealed class CultureObject { public string Name, StringId; }
    public sealed class CharacterObject
    {
        public string StringId, Name;
        public Hero HeroObject;
        public CultureObject Culture;
        public int Tier;
        public bool IsHero;
    }
    public sealed class Clan { public string Name, StringId; }
}

namespace TaleWorlds.CampaignSystem.Party
{
    public sealed class MobileParty { public string Name, StringId; }
}

namespace TaleWorlds.CampaignSystem.Settlements
{
    public sealed class Settlement
    {
        public static Settlement CurrentSettlement;
        public string Name, StringId;
        public TaleWorlds.CampaignSystem.Clan OwnerClan;
        public bool IsTown, IsCastle;
    }
}

namespace TaleWorlds.Core
{
    public sealed class ItemCategory { public string StringId; public string GetName() => StringId; }
    public sealed class ItemObject
    {
        public enum ItemTypeEnum
        {
            Horse, Animal, OneHandedWeapon, TwoHandedWeapon, Polearm, Bow, Crossbow,
            Arrows, Bolts, SlingStones, Bullets, Shield, Thrown, HeadArmor, BodyArmor,
            ChestArmor, LegArmor, HandArmor, Cape, HorseHarness, Goods
        }
        public string Name, StringId;
        public ItemTypeEnum Type;
        public ItemCategory ItemCategory;
        public bool IsFood, IsAnimal, HasHorseComponent;
    }
}

namespace AnimusForge
{
    internal sealed class DuelSettings
    {
        internal int PromptListCandidateMaxCount = 10;
        internal static DuelSettings Current = new DuelSettings();
        internal static DuelSettings GetSettings() => Current;
    }

    public static class RewardSystemBehavior
    {
        public sealed class RewardItemInfo
        {
            public string Name, StringId, PromptStringId, ModifierStringId;
            public TaleWorlds.Core.ItemObject Item;
            public bool IsPrivateEquipment;
            public int Count = 1;
        }
        public static string GetItemPromptTypeLabelForExternal(TaleWorlds.Core.ItemObject item) => item?.Type.ToString() ?? "";
    }

    public static class MyBehavior
    {
        public enum PartyTransferEntrySection { NpcPrisoners, PlayerPrisoners, NpcVolunteers, Other }
        public sealed class PartyTransferPromptEntry
        {
            public string DisplayName;
            public TaleWorlds.CampaignSystem.CharacterObject Character;
            public PartyTransferEntrySection Section;
            public bool IsHero;
            public int Count = 1;
        }
        public enum SettlementTransferAssetKind { Settlement, Workshop, Caravan, Other }
        public sealed class SettlementTransferPromptEntry
        {
            public string DisplayName, SettlementId, AssetId, TypeLabel;
            public TaleWorlds.CampaignSystem.Clan OwnerClan;
            public TaleWorlds.CampaignSystem.Settlements.Settlement Settlement;
            public Workshop Workshop;
            public TaleWorlds.CampaignSystem.Party.MobileParty CaravanParty;
            public SettlementTransferAssetKind AssetKind;
        }
        public sealed class Workshop { public string Name; }
        public static bool IsSettlementTransferEntryValidForExternal(SettlementTransferPromptEntry entry) => entry != null;
        public static string GetPartyTransferTroopTypeLabelForExternal(TaleWorlds.CampaignSystem.CharacterObject character) => "步兵";
        public static string GetSettlementTransferAssetIdForExternal(SettlementTransferPromptEntry entry) => entry?.AssetId;
        public static string GetSettlementTransferAssetDisplayNameForExternal(SettlementTransferPromptEntry entry) => entry?.DisplayName;
    }
}
