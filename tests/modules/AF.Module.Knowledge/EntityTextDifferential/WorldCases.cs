using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge
{
    public static partial class WorldEntityRetrievalService
    {
        internal static readonly string[] WorldCases =
        {
            "world_settlement", "world_clan", "world_kingdom", "world_mixed", "world_resident",
            "world_player_resident", "world_resident_no_kingdom_main", "world_visible",
            "world_capture_fallback", "world_worker_fallback", "world_shared"
        };

        private static TextObject Text(string value) => new TextObject { Value = value };

        private static void ResetWorld()
        {
            Hero.MainHero = null;
            Hero.AllAliveHeroes = new List<Hero>();
            Hero.DeadOrDisabledHeroes = new List<Hero>();
            Clan.PlayerClan = null;
            Clan.All = new List<Clan>();
            Settlement.All = new List<Settlement>();
            Kingdom.All = new List<Kingdom>();
            MobileParty.MainParty = null;
            MobileParty.All = new List<MobileParty>();
        }

        private static MobileParty Party(string id, string name, float x, bool visible = false)
        {
            return new MobileParty { StringId = id, Name = Text(name), Position = new CampaignVec2(x, 0),
                Party = new PartyBase(), MemberRoster = new Roster { TotalManCount = 23 }, IsVisible = visible };
        }

        public static (string Main, string Post, string Meta) RenderWorld(string scenario)
        {
            ResetWorld();
            var culture = new CultureObject { Name = Text("Dawn culture"), StringId = "dawn_culture" };
            var npc = new Hero { Name = Text("Alda"), StringId = "npc_1", IsLord = true };
            var leader = new Hero { Name = Text("Borin"), StringId = "npc_2", IsLord = true };
            var kingdom = new Kingdom { Name = Text("DawnRealm"), StringId = "kingdom_dawn", Leader = leader,
                Culture = culture, CurrentTotalStrength = 900, ModCreated = true,
                EncyclopediaText = Text("First line.\nSecond line.") };
            var clan = new Clan { Name = Text("House Sunflare"), StringId = "clan_sunflare", Leader = npc,
                Kingdom = kingdom, Culture = culture, Gold = 1200, Tier = 3, Influence = 45,
                Heroes = new List<Hero> { npc, leader } };
            npc.Clan = clan;
            leader.Clan = clan;
            kingdom.Clans.Add(clan);
            var town = new Settlement { Name = Text("Praven"), StringId = "town_praven", OwnerClan = clan,
                MapFaction = kingdom, Culture = culture, IsTown = true, Militia = 50, Party = new PartyBase { NumberOfAllMembers = 40 } };
            town.Town = new Town { Settlement = town, Prosperity = 5000, Loyalty = 75, Security = 80 };
            var village = new Settlement { Name = Text("Azgad"), StringId = "village_azgad", IsVillage = true,
                MapFaction = kingdom, OwnerClan = clan, Culture = culture };
            village.Village = new Village { Settlement = village, Hearth = 600 };
            town.BoundVillages.Add(village.Village);
            clan.Fiefs.Add(town.Town);
            Hero.AllAliveHeroes.AddRange(new[] { npc, leader });
            Kingdom.All.Add(kingdom);
            Clan.All.Add(clan);
            Settlement.All.AddRange(new[] { town, village });

            Hero context = null;
            bool residentKingdom = false, residentPlayer = false;
            var mentions = new MentionedWorldEntities();
            string input = "Tell me about Praven, House Sunflare and DawnRealm; can we barter this item?";
            string[] expectedText, expectedIds, expectedKingdomIds = Array.Empty<string>();
            int expectedCount;
            switch (scenario)
            {
                case "world_settlement":
                    mentions.Entities.Add("Praven"); expectedCount = 1;
                    expectedText = new[] { "【地点】", "Praven", "5000", "75", "Azgad" };
                    expectedIds = new[] { town.StringId };
                    break;
                case "world_clan":
                    mentions.Entities.Add("House Sunflare"); expectedCount = 1;
                    expectedText = new[] { "【家族】", "House Sunflare", "1200", "Alda" };
                    expectedIds = new[] { clan.StringId };
                    break;
                case "world_kingdom":
                    mentions.Entities.Add("DawnRealm"); expectedCount = 1;
                    expectedText = new[] { "【王国】", "DawnRealm", "900", "First line. Second line.", "Praven" };
                    expectedIds = new[] { kingdom.StringId }; expectedKingdomIds = expectedIds;
                    break;
                case "world_shared":
                    context = npc;
                    mentions.Entities.AddRange(new[] { "Praven", "House Sunflare", "DawnRealm" }); expectedCount = 4;
                    expectedText = new[] { "【人物】", "【地点】", "【家族】", "【王国】", "5000", "1200", "900" };
                    expectedIds = new[] { npc.StringId, town.StringId, clan.StringId, kingdom.StringId };
                    expectedKingdomIds = new[] { kingdom.StringId };
                    break;
                case "world_mixed":
                case "world_capture_fallback":
                case "world_worker_fallback":
                    mentions.Entities.AddRange(new[] { "Praven", "House Sunflare", "DawnRealm" }); expectedCount = 3;
                    expectedText = new[] { "【地点】", "【家族】", "【王国】", "5000", "1200", "900" };
                    expectedIds = new[] { town.StringId, clan.StringId, kingdom.StringId };
                    expectedKingdomIds = new[] { kingdom.StringId };
                    break;
                case "world_resident":
                case "world_resident_no_kingdom_main":
                case "world_player_resident":
                    context = npc; residentKingdom = scenario != "world_resident_no_kingdom_main";
                    residentPlayer = scenario == "world_player_resident";
                    expectedCount = residentKingdom ? 3 : 2;
                    expectedText = new[] { "【人物】", "【家族】", "Alda", "House Sunflare" };
                    expectedIds = new[] { npc.StringId, clan.StringId, kingdom.StringId };
                    if (residentPlayer)
                    {
                        var playerKingdom = new Kingdom { Name = Text("PlayerRealm"), StringId = "kingdom_player", Culture = culture };
                        var playerClan = new Clan { Name = Text("House Player"), StringId = "clan_player", Kingdom = playerKingdom, Culture = culture };
                        Hero.MainHero = new Hero { StringId = "player_hero", Name = Text("Player"), Clan = playerClan, IsLord = true };
                        playerClan.Leader = Hero.MainHero;
                        playerKingdom.Leader = Hero.MainHero;
                        playerKingdom.Clans.Add(playerClan);
                        Clan.PlayerClan = playerClan;
                        Kingdom.All.Add(playerKingdom); Clan.All.Add(playerClan);
                        expectedCount += 2;
                        expectedIds = expectedIds.Concat(new[] { "player_hero", "clan_player", "kingdom_player" }).ToArray();
                    }
                    break;
                case "world_visible":
                    var observer = Party("party_player", "Player party", 0);
                    observer.IsMainParty = true; observer.SeeingRange = 10;
                    MobileParty.MainParty = observer;
                    var nearby = Party("party_scout", "Scouts", 5); nearby.ShipInfo = "1 cutter";
                    var visibleFar = Party("party_visible_far", "Visible caravan", 300, visible: true);
                    var hiddenFar = Party("party_hidden", "Hidden party", 200);
                    var garrison = Party("party_garrison", "Garrison", 2); garrison.IsGarrison = true;
                    var militia = Party("party_militia", "Militia", 3); militia.IsMilitia = true;
                    var inTown = Party("party_inside", "Inside town", 1); inTown.CurrentSettlement = town;
                    var inactive = Party("party_inactive", "Inactive", 1); inactive.IsActive = false;
                    var inBattle = Party("party_battle", "In battle", 4); inBattle.MapEvent = new MapEvent();
                    MobileParty.All.AddRange(new[] { observer, nearby, visibleFar, hiddenFar, garrison, militia, inTown, inactive, inBattle });
                    expectedCount = 2;
                    expectedText = new[] { "【附近可见部队】", "Scouts", "Visible caravan", "23", "1 cutter", "5.0", "300.0" };
                    expectedIds = new[] { nearby.StringId, visibleFar.StringId };
                    break;
                default: throw new ArgumentException("Unknown world scenario: " + scenario);
            }
#if CURRENT
            var capture = CaptureEntityCandidates(mentions, input, context);
            var selected = MatchDetachedCandidates(capture.Candidates, mentions, input, capture.MaxInjectedEntities);
            var result = BuildPromptContext(mentions, "Player", context, residentKingdom, null, input, residentPlayer,
                scenario == "world_capture_fallback" ? null : capture,
                scenario == "world_worker_fallback" ? null : selected);
            var fallback = BuildPromptContext(mentions, "Player", context, residentKingdom, null, input, residentPlayer, null, null);
#else
            var result = BuildPromptContext(mentions, "Player", context, residentKingdom, null, input, residentPlayer);
#endif
            if (result.MatchCount != expectedCount || expectedText.Any(text => !result.MainPromptBlock.Contains(text))
                || expectedIds.Any(id => !result.PostprocessPromptBlock.Contains(id))
                || !result.ExplicitMentionedKingdomIds.SequenceEqual(expectedKingdomIds))
                throw new Exception("world coverage failed scenario=" + scenario + " count=" + result.MatchCount + "/" + expectedCount
                    + " missingText=" + string.Join(",", expectedText.Where(text => !result.MainPromptBlock.Contains(text)))
                    + " missingIds=" + string.Join(",", expectedIds.Where(id => !result.PostprocessPromptBlock.Contains(id)))
                    + " kingdoms=" + string.Join(",", result.ExplicitMentionedKingdomIds));
            if (expectedIds.Any(id => result.MainPromptBlock.Contains(id)))
                throw new Exception("main prompt leaked entity IDs scenario=" + scenario);
            if (scenario == "world_resident_no_kingdom_main" && result.MainPromptBlock.Contains("【王国】"))
                throw new Exception("resident main kingdom suppression failed");
            if (scenario == "world_visible" && (result.PostprocessPromptBlock.Contains("party_hidden") || result.PostprocessPromptBlock.Contains("party_battle")))
                throw new Exception("visibility filter leaked rejected party");
#if CURRENT
            if (fallback.MainPromptBlock != result.MainPromptBlock || fallback.PostprocessPromptBlock != result.PostprocessPromptBlock
                || fallback.MatchCount != result.MatchCount || !fallback.ExplicitMentionedKingdomIds.SequenceEqual(result.ExplicitMentionedKingdomIds))
                throw new Exception("world detached/fallback differs scenario=" + scenario);
#endif
            return (result.MainPromptBlock, result.PostprocessPromptBlock, result.MatchCount + "|" + string.Join(",", result.ExplicitMentionedKingdomIds));
        }
    }
}
