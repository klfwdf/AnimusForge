using AnimusForge;
using AnimusForge.SiegeAftermathIntervention;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

static class RecentMemoryReplay
{
    private static void Check(bool value, string message) => Program.Check(value, "recent: " + message);
    private static TownPromptTextCatalog Catalog(string language)
    {
        using var stream = typeof(TownPromptTextCatalog).Assembly.GetManifestResourceStream("AnimusForge.Defaults.GcczTownPrompt." + language + ".json");
        using var reader = new StreamReader(stream);
        return TownPromptTextCatalog.Resolve(JsonConvert.DeserializeObject<TownPromptTextCatalog>(reader.ReadToEnd()));
    }
    private static TownRecentMemoryFact Fact(string id, int day) => new() {
        Id = id, Day = day, TownId = "town", TownName = "波罗斯", VillageName = "测试", ActorName = "孛儿图",
        Action = "village.MassacreAndSeize", Year = 1086, Season = 2, DayOfSeason = 1
    };
    public static void Run()
    {
        var cn = Catalog("zh-CN"); var en = Catalog("en");
        Check(cn.RecentMemoryTexts.Keys.Order().SequenceEqual(en.RecentMemoryTexts.Keys.Order()), "CN/EN resource keys match");
        foreach (string key in TownRecentMemoryText.ActionKeys)
            Check(cn.RecentMemoryTexts.ContainsKey(key) && en.RecentMemoryTexts.ContainsKey(key), "both package action labels: " + key);
        var f = Fact("first", 100);
        Check(TownRecentMemoryText.FormatFact(f, cn) == "〔1086年秋季1日〕，孛儿图对波罗斯城的测试村庄执行了屠村。", "exact approved village/date/label format");
        f.VillageName = ""; f.Action = "native.Devastate"; f.TownName = "测试城";
        Check(TownRecentMemoryText.FormatFact(f, cn) == "〔1086年秋季1日〕，孛儿图对测试城执行了毁坏。", "town format without duplicated suffix");
        f.ActorName = "{town}";
        Check(TownRecentMemoryText.FormatFact(f, cn).Contains("，{town}对测试城"), "dynamic names are literal, not recursive template slots");
        Check(TownRecentMemoryText.FormatFact(f, en).Contains("autumn, day 1"), "English date has no clock time");
        foreach (var spec in VillageDispositionActionCatalog.All)
            Check(TownRecentMemoryStore.IsRecordedVillageAction(spec.Kind) == (spec.HasDefinedEffect && spec.OutcomeLevel < 0), "village allowlist " + spec.Kind);

        var store = new TownRecentMemoryStore(); store.Start(100.5);
        Check(store.Add(Fact("a", 100), 106.9), "first day retained through seventh calendar day");
        Check(!store.Add(Fact("a", 100), 106.9), "same receipt not repeated");
        Check(store.Add(Fact("b", 100), 106.9), "different real action on same day retained");
        Check(!store.Add(Fact("old", 99), 106.9) && !store.Add(Fact("future", 107), 106.9), "expired and future records rejected");
        Check(store.Read("town", 107).Facts.Count == 0, "eighth calendar day removes old records");
        Check(!store.HasCompleteArrivalWindow(106.99) && store.HasCompleteArrivalWindow(107), "partial first day never presented as full seven-day coverage");
        Check(store.Arrival("town", "private_party:1", false, 107.1) && !store.Arrival("town", "private_party:1", false, 107.1), "arrival callback deduplicated");
        store.Arrival("town", "private_party:2", false, 107.2); store.Arrival("town", "private_caravan:1", true, 107.2);
        string text = TownRecentMemoryText.Build(store.Read("town", 107.2), true, cn);
        Check(text.Contains("2队村民和1队商队") && !text.Contains("private_"), "arrival output is aggregate only and allows genuine repeat visits");
        Check(TownRecentMemoryText.Build(store.Read("town", 107.2), false, cn).Contains("完整七日总数未知"), "unknown coverage not replaced with zero total");
        var copy = JsonConvert.DeserializeObject<TownRecentMemorySnapshot>(JsonConvert.SerializeObject(store.State));
        Check(new TownRecentMemoryStore().Restore(copy), "primitive state round-trips with arrival dedup receipts");
        copy.Version = 2;
        Check(!store.Restore(copy) && store.State.Version == 1, "future schema rejected atomically");
        copy.Version = 1; copy.Towns[0].Arrivals[0].Caravans++;
        Check(!store.Restore(copy), "inconsistent arrival counts rejected");
        Check(store.Read("town", 114).Arrivals.Count == 0, "arrival buckets expire too");
        Host(cn);
    }

    private static void Host(TownPromptTextCatalog cn)
    {
        GcczTownRecentMemoryBridge.Reset();
        CampaignTime.Day = 200; CampaignTime.Fraction = 0.5;
        var player = new Hero { StringId = "player", Name = "玩家" }; Hero.MainHero = player;
        var npc = new Hero { StringId = "npc", Name = "领主甲" };
        var town = new Settlement { StringId = "recent-town", Name = "测试", IsTown = true };
        var village = new Settlement { StringId = "recent-village", Name = "村甲", IsVillage = true };
        village.Village = new Village { Settlement = village, Bound = town, VillageState = Village.VillageStates.Looted };
        var castle = new Settlement { StringId = "castle", Name = "堡" };
        var party = new MobileParty { StringId = "npc-party", LeaderHero = npc };
        var history = Campaign.Current.LogEntryHistory.GameActionLogs; history.Clear();
        history.Add(new VillageStateChangedLogEntry { Id = 1, GameTime = CampaignTime.Now, Village = village.Village, RaidLeader = npc, NewState = Village.VillageStates.Looted });
        history.Add(new SiegeAftermathLogEntry(npc) { Id = 2, GameTime = CampaignTime.Now, CapturedSettlement = town, SiegeAftermath = SiegeAftermathAction.SiegeAftermath.Devastate });
        history.Add(new SiegeAftermathLogEntry(player) { Id = 3, GameTime = CampaignTime.Now, CapturedSettlement = town, SiegeAftermath = SiegeAftermathAction.SiegeAftermath.ShowMercy });
        CampaignEvents.OnSessionLaunchedEvent.Raise(new());
        string text = GcczTownRecentMemoryBridge.Build(town, cn);
        Check(text.Contains("领主甲对测试城的村甲村庄执行了劫掠") && text.Contains("领主甲对测试城执行了毁坏"), "legacy logs retain actual actor and bound town");
        Check(!text.Contains("玩家对") && text.Contains("总数未知"), "ambiguous historical player aftermath and missing visitor history are not invented");
        CampaignEvents.OnSessionLaunchedEvent.Raise(new());
        Check(text == GcczTownRecentMemoryBridge.Build(town, cn), "legacy migration runs once");
        GcczTownRecentMemoryBridge.Reset(); Campaign.Current.LogEntryHistory.GameActionLogs.Clear();
        CampaignEvents.OnSessionLaunchedEvent.Raise(new());
        var raid = new RaidEventComponent { MapEventSettlement = village, AttackerSide = new Side { LeaderParty = party } };
        CampaignEvents.RaidCompletedEvent.Raise(BattleSideEnum.Defender, raid);
        Check(!GcczTownRecentMemoryBridge.Build(town, cn).Contains("执行了"), "failed raid has no success fact");
        village.Village.VillageState = Village.VillageStates.Normal;
        CampaignEvents.RaidCompletedEvent.Raise(BattleSideEnum.Attacker, raid);
        Check(!GcczTownRecentMemoryBridge.Build(town, cn).Contains("执行了"), "interrupted raid has no looted fact");
        village.Village.VillageState = Village.VillageStates.Looted;
        CampaignEvents.RaidCompletedEvent.Raise(BattleSideEnum.Attacker, raid);
        CampaignEvents.RaidCompletedEvent.Raise(BattleSideEnum.Attacker, raid);
        Check(GcczTownRecentMemoryBridge.Build(town, cn).Split("执行了劫掠").Length == 2, "real completed raid captured exactly once");
        GcczTownRecentMemoryBridge.OwnerChanged(town, npc, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.Default);
        Check(!GcczTownRecentMemoryBridge.Build(town, cn).Contains("执行了攻占"), "gift/inheritance does not invent a conquest");
        GcczTownRecentMemoryBridge.OwnerChanged(town, npc, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege);
        Check(GcczTownRecentMemoryBridge.Build(town, cn).Contains("领主甲对测试城执行了攻占"), "capture uses actual capturer");
        SiegeAiInterventionBehavior.Pending(town, true);
        CampaignEvents.OnSiegeAftermathAppliedEvent.Raise(party, town, SiegeAftermathAction.SiegeAftermath.ShowMercy, null, new());
        Check(!GcczTownRecentMemoryBridge.Build(town, cn).Contains("执行了宽恕"), "GCCZ internal mercy callback excluded");
        SiegeAiInterventionBehavior.Pending(town, false);
        CampaignEvents.OnSiegeAftermathAppliedEvent.Raise(party, town, SiegeAftermathAction.SiegeAftermath.Pillage, null, new());
        Check(GcczTownRecentMemoryBridge.Build(town, cn).Contains("执行了掠夺"), "native aftermath recorded by actual decision maker");
        GcczTownRecentMemoryBridge.VillageAction(village, VillageDispositionActionKind.Mercy, "excluded");
        GcczTownRecentMemoryBridge.VillageAction(village, VillageDispositionActionKind.MassacreAndSeize, "executed");
        GcczTownRecentMemoryBridge.VillageAction(village, VillageDispositionActionKind.MassacreAndSeize, "executed");
        Check(GcczTownRecentMemoryBridge.Build(town, cn).Split("执行了屠村").Length == 2, "committed village receipt is idempotent");
        Check(!GcczTownRecentMemoryBridge.Build(town, cn).Contains("执行了宽恕"), "positive village action excluded");
        village.Village.Bound = castle;
        GcczTownRecentMemoryBridge.VillageAction(village, VillageDispositionActionKind.Destroy, "castle-village");
        Check(GcczTownRecentMemoryBridge.Build(castle, cn) == "", "castle-bound villages not projected as towns");
        village.Village.Bound = town;
        GcczTownRecentMemoryBridge.TownAction(town, "plunder");
        GcczTownRecentMemoryBridge.TownAction(town, "plunder");
        Check(GcczTownRecentMemoryBridge.Build(town, cn).Split("执行了搜掠").Length == 2, "repeated town acknowledgement at the same campaign instant is deduplicated");
        CampaignTime.Fraction += 0.01;
        GcczTownRecentMemoryBridge.TownAction(town, "plunder");
        Check(GcczTownRecentMemoryBridge.Build(town, cn).Split("执行了搜掠").Length == 3, "later genuine town action on the same date remains distinct");
        var visitor = new MobileParty { StringId = "private-caravan", IsCaravan = true };
        CampaignEvents.SettlementEntered.Raise(visitor, town, null); CampaignEvents.SettlementEntered.Raise(visitor, town, null);
        Check(GcczTownRecentMemoryBridge.Build(town, cn).Contains("1次商队到访"), "real arrival listener deduplicates");
        // Both native forcing callbacks are distinct from GCCZ village tags.
        var map = new MapEvent { MapEventSettlement = village, AttackerSide = new Side { LeaderParty = party } };
        CampaignEvents.ForceSuppliesCompletedEvent.Raise(BattleSideEnum.Attacker, new() { MapEvent = map });
        CampaignEvents.ForceVolunteersCompletedEvent.Raise(BattleSideEnum.Attacker, new() { MapEvent = map });
        Check(GcczTownRecentMemoryBridge.Build(town, cn).Contains("执行了强迫提供物资") && GcczTownRecentMemoryBridge.Build(town, cn).Contains("执行了强迫征兵"), "native forced actions are captured independently");
        for (int i = 0; i < 250; i++) GcczTownRecentMemoryBridge.VillageAction(village, VillageDispositionActionKind.ForcedFood, "large-" + i);
        var save = new FixtureDataStore { IsSaving = true }; GcczTownRecentMemoryBridge.SyncData(save);
        Check((int)save.Values[GcczTownRecentMemoryBridge.SaveKey + "__af_chunk_count"] > 1
            && save.Values.Values.OfType<string>().All(s => System.Text.Encoding.UTF8.GetByteCount(s) <= 12000), "large recent history uses actual safe UTF8 chunks");
        text = GcczTownRecentMemoryBridge.Build(town, cn);
        save.IsSaving = false; save.IsLoading = true; GcczTownRecentMemoryBridge.SyncData(save);
        Check(text == GcczTownRecentMemoryBridge.Build(town, cn), "real adapter restores complete history and arrivals");
        save.Values.Remove(GcczTownRecentMemoryBridge.SaveKey + "__af_chunk_0");
        GcczTownRecentMemoryBridge.SyncData(save);
        Check(GcczTownRecentMemoryBridge.Build(town, cn).Contains("暂不可用"), "corrupt chunks disable rather than publish empty state");
        var preserve = new FixtureDataStore { IsSaving = true }; GcczTownRecentMemoryBridge.SyncData(preserve);
        Check(save.Values.Keys.Order().SequenceEqual(preserve.Values.Keys.Order()) && save.Values.All(p => Equals(p.Value, preserve.Values[p.Key])), "corrupt namespace retained unchanged on save");
        var future = new FixtureDataStore { IsSaving = true };
        CampaignSaveChunkHelper.SaveChunkedStringStrict(future, GcczTownRecentMemoryBridge.SaveKey, "{\"Version\":99}");
        future.IsSaving = false; future.IsLoading = true; GcczTownRecentMemoryBridge.SyncData(future);
        Check(GcczTownRecentMemoryBridge.Build(town, cn).Contains("暂不可用"), "future schema never overwritten as empty success");
        GcczTownRecentMemoryBridge.SyncData(new FixtureDataStore { IsLoading = true });
        Check(!GcczTownRecentMemoryBridge.Build(town, cn).Contains("执行了"), "loading legacy empty save clears previous campaign state");
        CampaignTime.Fraction = 0;
    }
}
