using System;
using System.Collections.Generic;
using System.IO;
using AnimusForge;
using AnimusForge.Illustrator.Core;
using AnimusForge.Illustrator.Engine;
using AnimusForge.Illustrator.UI.Gallery;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

internal static class Program
{
    private static int checks;
    private static void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
    private static void Reset()
    {
        Campaign.Current = new Campaign(); Hero.MainHero = new Hero();
        RewardSystemBehavior.Instance = new RewardSystemBehavior();
        MobileParty.MainParty = new MobileParty(); IllustratorRuntime.CampaignKey = "campaign";
        RewardSystemBehavior.FailCreate = RewardSystemBehavior.FailDescription = RewardSystemBehavior.FailAdd = false;
        RewardSystemBehavior.AddCalls = RewardSystemBehavior.CreateCalls = 0;
        RewardSystemBehavior.Descriptions.Clear();
    }
    public static void Main(string[] args)
    {
        // Explicit caller-owned workspace fixture, never a real cache or save.
        string fixture = Path.GetFullPath(args[0]);
        if (!File.Exists(fixture)) throw new Exception("Fixture missing");
        var image = new CachedIllustrationItem { Key = "one", CampaignKey = "campaign", FilePath = fixture, Title = "会面", Theme = "交谈", ActionSummary = "两名使者交换意见", Prompt = "PRIVATE GENERATION INSTRUCTIONS" };
        Reset();
        string intro = GalleryRpItemConverter.BuildIntroduction(image);
        Check(intro.Contains("交换意见") && !intro.Contains(image.Prompt), "Use metadata, never raw prompt");
        Check(GalleryRpItemConverter.TryConvert(image, "campaign", intro, out _), "Successful conversion");
        Check(MobileParty.MainParty.ItemRoster.Count == 1 && RewardSystemBehavior.AddCalls == 1, "Exactly one item");
        Check(GalleryRpItemConverter.TryConvert(image, "campaign", "更新介绍", out _), "Reconvert");
        Check(RewardSystemBehavior.AddCalls == 1, "Stable ID rejects duplicate despite new object instance");
        Check(RewardSystemBehavior.Descriptions[GalleryRpItemConverter.Identity(image)] == "更新介绍", "Description update retained");
        image.Key = "two";
        Check(GalleryRpItemConverter.TryConvert(image, "campaign", intro, out _) && MobileParty.MainParty.ItemRoster.Count == 2, "Same title distinct artwork");
        image.Key = "one";
        Reset(); RewardSystemBehavior.FailDescription = true;
        Check(!GalleryRpItemConverter.TryConvert(image, "campaign", intro, out _) && RewardSystemBehavior.AddCalls == 0, "No item on description failure");
        Reset(); RewardSystemBehavior.FailCreate = true;
        Check(!GalleryRpItemConverter.TryConvert(image, "campaign", intro, out _) && RewardSystemBehavior.AddCalls == 0, "No item on create failure");
        Reset(); RewardSystemBehavior.FailAdd = true;
        Check(!GalleryRpItemConverter.TryConvert(image, "campaign", intro, out _), "Report failed delivery");
        RewardSystemBehavior.FailAdd = false;
        Check(GalleryRpItemConverter.TryConvert(image, "campaign", intro, out _) && MobileParty.MainParty.ItemRoster.Count == 1, "Retry failed delivery");
        Reset();
        Check(!GalleryRpItemConverter.TryConvert(image, "campaign", "  ", out _) && RewardSystemBehavior.CreateCalls == 0, "Blank description rejected before mutation");
        image.Deleted = true;
        Check(!GalleryRpItemConverter.TryConvert(image, "campaign", intro, out _), "Deleted item rejected"); image.Deleted = false;
        image.FilePath = fixture + ".absent";
        Check(!GalleryRpItemConverter.TryConvert(image, "campaign", intro, out _) && RewardSystemBehavior.CreateCalls == 0, "Missing source rejected"); image.FilePath = fixture;
        image.CampaignKey = "other";
        Check(!GalleryRpItemConverter.TryConvert(image, "campaign", intro, out _), "Foreign image rejected"); image.CampaignKey = "campaign";
        IllustratorRuntime.CampaignKey = "other";
        Check(!GalleryRpItemConverter.TryConvert(image, "campaign", intro, out _), "Stale campaign rejected");
        Reset(); Campaign.Current = null;
        Check(!GalleryRpItemConverter.TryConvert(image, "campaign", intro, out _), "Missing campaign rejected");
        Reset(); MobileParty.MainParty = null;
        Check(!GalleryRpItemConverter.TryConvert(image, "campaign", intro, out _), "Missing roster rejected");
        Reset(); image.CampaignKey = "campaign_test"; IllustratorRuntime.CampaignKey = "campaign:test";
        Check(GalleryRpItemConverter.TryConvert(image, "campaign:test", intro, out _), "Normalized cache campaign accepted");
        Console.WriteLine("PASS " + checks + " production converter checks with fake game/RP dependencies; no game or real save.");
    }
}

namespace AnimusForge.Illustrator.Core
{
    public static class IllustratorRuntime { public static string CampaignKey; public static void AssertMainThread() { } }
}
namespace AnimusForge.Illustrator.Engine
{
    public class CachedIllustrationItem { public string Key, CampaignKey, FilePath, Title, Theme, ActionSummary, Prompt; public bool Deleted; }
    public static class DiskImageCacheManager { public static string SanitizeKey(string s) { return (s ?? "unknown").Replace(':', '_'); } }
}
namespace TaleWorlds.Core
{
    public class ItemObject { public string StringId; public ObjectId Id = new ObjectId(); }
    public class ObjectId { public uint InternalValue = 1; }
    public class EquipmentElement { public ItemObject Item; }
    public class ItemEntry { public int Amount; public EquipmentElement EquipmentElement; }
    public class ItemRoster
    {
        public List<ItemEntry> Entries = new List<ItemEntry>();
        public int Count { get { return Entries.Count; } }
        public ItemEntry GetElementCopyAtIndex(int i) { return Entries[i]; }
    }
}
namespace TaleWorlds.CampaignSystem
{
    public class Campaign { public static Campaign Current; }
    public class Hero { public static Hero MainHero; }
}
namespace TaleWorlds.CampaignSystem.Party
{
    public class MobileParty { public static MobileParty MainParty; public ItemRoster ItemRoster = new ItemRoster(); }
}
namespace AnimusForge
{
    public class RewardSystemBehavior
    {
        public static RewardSystemBehavior Instance;
        public static bool FailCreate, FailDescription, FailAdd;
        public static int AddCalls, CreateCalls;
        public static Dictionary<string,string> Descriptions = new Dictionary<string,string>();
        public static bool TryCreateGeneratedInventoryItemForExternal(string name, string id, out ItemObject item, string templateItemId = null, string logSource = null)
        { CreateCalls++; item = new ItemObject { StringId = id }; return !FailCreate; }
        public static bool TrySetGeneratedRpItemIntroductionForExternal(string id, string value, out string error)
        { error = "fixture"; if (FailDescription) return false; Descriptions[id] = value; return true; }
        public static int GenerateKnownInventoryItemToRosterForExternal(ItemRoster roster, string id, string name, string template, uint objectId, int count,
            out string outName, out string outId, out string outTemplate, out uint outObject, string logSource = null)
        {
            AddCalls++; outName = name; outId = id; outTemplate = template; outObject = objectId;
            if (FailAdd) return 0;
            if (!Descriptions.ContainsKey(id)) throw new Exception("Item delivered before description persisted");
            roster.Entries.Add(new ItemEntry { Amount = count, EquipmentElement = new EquipmentElement { Item = new ItemObject { StringId = id } } });
            return count;
        }
    }
}
