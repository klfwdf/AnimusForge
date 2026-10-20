using System;
using System.Text;
namespace AnimusForge;
// Event-time detached facts only. Bannerlord handles, participant capture and writes remain adapters.
internal static class WorldBulletinCampaignMaterialPolicy
{
    private static readonly System.Text.RegularExpressions.Regex BattleLoss = new System.Text.RegularExpressions.Regex(
        WeeklyMaterialTexts.Raw("BattleLossPattern"), System.Text.RegularExpressions.RegexOptions.Compiled);
    private static readonly System.Text.RegularExpressions.Regex BattlePartyKind = new System.Text.RegularExpressions.Regex(
        WeeklyMaterialTexts.Raw("BattlePartyKindPattern"), System.Text.RegularExpressions.RegexOptions.Compiled);
    private static readonly string[] BattlePartyKinds = { "NonLord", "Villager", "Caravan", "Patrol", "Militia", "Garrison", "Bandit", "Custom" };
    private static string BattlePartyKindId(string value)
    {
        foreach (string id in BattlePartyKinds) if (WeeklyMaterialTexts.Raw("BattlePartyKind_" + id) == value) return id;
        return "NonLord";
    }
    internal static string BattleLocation(string value) => value == WeeklyMaterialTexts.Raw("BattleFieldLocation") ? WeeklyMaterialTexts.Prompt("AF_WEEKLY_Fact_FieldLocation") : value;
    internal static string BattleActor(string value) => BattlePartyKind.Replace(value ?? "", match => " (" + WeeklyMaterialTexts.Prompt("AF_WEEKLY_Fact_PartyKind_" + BattlePartyKindId(match.Groups["kind"].Value)) + ")");
    internal static string BattleCasualties(string value) => BattleLoss.Replace(value ?? "", match => WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_Casualties", ("dead", match.Groups["dead"].Value), ("wounded", match.Groups["wounded"].Value)));
    // Only captured battle facts reach this boundary, not arbitrary generated or player prose.
    internal static string ProjectRecordedBattle(string value, string kind)
    {
        if (kind != "battle" && kind != "siege_battle" && kind != "sally_out_battle") return value;
        string text = BattleCasualties(BattleActor(value));
        string prefix = WeeklyMaterialTexts.Raw("BattleEnglishFieldPrefix");
        return text.StartsWith(prefix + ":", StringComparison.Ordinal)
            ? "Battle at " + BattleLocation(WeeklyMaterialTexts.Raw("BattleFieldLocation")) + text.Substring(prefix.Length) : text;
    }
    internal static int EventPriority(string kind) => kind switch {
        "war_declared"=>70, "peace_made"=>60, "fief_grant"=>20, "settlement_transfer"=>30,
        "kingdom_destroyed"=>100, "kingdom_rebellion"=>70, "kingdom_created"=>75, "raid"=>25, "civil_war"=>80,
        "alliance_formed"=>45, "alliance_ended"=>40, "ruler_changed"=>85, "clan_defection"=>45, "clan_destroyed"=>50,
        "army_gathered"=>35, "town_unrest"=>30, "town_rebellion"=>60, "kingdom_annexed"=>90, "vassalage_established"=>80, "vassalage_ended"=>60, _=>0 };
    // A temporary ruler appointed while the council elects a successor is news, but not a coronation.
    internal static int RulerChangedPriority(bool interim)=>interim?50:EventPriority("ruler_changed");
    internal static int MarriagePriority(bool royal)=>royal?65:40;
    internal static bool SameKingdom(string newId,string oldId)=>!string.IsNullOrEmpty(newId)&&string.Equals(newId,oldId,StringComparison.OrdinalIgnoreCase);
    internal static string KingdomContextLine(string kingdom,string ruler,int towns,int castles,bool stabilityEnabled,int stability,System.Collections.Generic.IReadOnlyCollection<string> enemies)
        => WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_KingdomContext", ("kingdom", kingdom), ("ruler", ruler), ("towns", towns.ToString()), ("castles", castles.ToString()),
            ("stability", stabilityEnabled?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_KingdomContextStability", ("stability", stability.ToString())):""),
            ("war", enemies.Count>0?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_KingdomContextAtWar", ("enemies", string.Join(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_ListSeparator"),enemies))):WeeklyMaterialTexts.Prompt("AF_WEEKLY_Fact_KingdomContextAtPeace")));
    internal static string PairKey(string x,string y) => string.CompareOrdinal(x,y)<=0?x+"|"+y:y+"|"+x;
    internal static string WarReason(string detail) => detail switch {
        "CausedByPlayerHostility" => WeeklyMaterialTexts.Prompt("AF_WEEKLY_Fact_WarReasonPlayerHostility"),
        "CausedByKingdomDecision" => WeeklyMaterialTexts.Prompt("AF_WEEKLY_Fact_WarReasonKingdomDecision"), "CausedByRebellion" => WeeklyMaterialTexts.Prompt("AF_WEEKLY_Fact_WarReasonRebellion"),
        "CausedByCrimeRatingChange" => WeeklyMaterialTexts.Prompt("AF_WEEKLY_Fact_WarReasonCrimeRating"), "CausedByKingdomCreation" => WeeklyMaterialTexts.Prompt("AF_WEEKLY_Fact_WarReasonKingdomCreation"),
        "CausedByClaimOnThrone" => WeeklyMaterialTexts.Prompt("AF_WEEKLY_Fact_WarReasonClaimOnThrone"), "CausedByCallToWarAgreement" => WeeklyMaterialTexts.Prompt("AF_WEEKLY_Fact_WarReasonCallToWar"), _ => WeeklyMaterialTexts.Prompt("AF_WEEKLY_Fact_WarReasonUnknown") };
    internal static string WarSentence(string first,string second) => WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_WarSentence", ("first", first), ("second", second));
    internal static string WarDetail(string firstLeader,string secondLeader,string reason) => WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_WarDetail", ("firstLeader", firstLeader), ("secondLeader", secondLeader), ("reason", reason));
    internal static string PeaceSentence(string first,string second) => WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_PeaceSentence", ("first", first), ("second", second));
    internal static string PeaceDetail(string first,string firstLeader,string second,string secondLeader,bool decision) => WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_PeaceDetail", ("first", first), ("firstLeader", firstLeader), ("second", second), ("secondLeader", secondLeader), ("how", WeeklyMaterialTexts.Prompt(decision?"AF_WEEKLY_Fact_PeaceByDecision":"AF_WEEKLY_Fact_PeaceByAgreement")));
    internal static string HeroTitle(bool ruler,string kingdom,string clan,bool clanLeader) => ruler?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_HeroTitleRuler", ("kingdom", kingdom)):clanLeader?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_HeroTitleClanLeader", ("kingdom", kingdom), ("clan", clan)):WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_HeroTitleClanMember", ("kingdom", kingdom), ("clan", clan));
    internal static string OwnerDetail(bool town,bool hasOld,string oldName,string oldTitle,bool hasNew,string newName,string newTitle) => WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_OwnerDetailBase", ("kind", WeeklyMaterialTexts.Prompt(town?"AF_WEEKLY_Fact_OwnerKindTown":"AF_WEEKLY_Fact_OwnerKindCastle")))+(hasOld?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_OwnerDetailOld", ("oldName", oldName), ("oldTitle", oldTitle)):"")+(hasNew?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_OwnerDetailNew", ("newName", newName), ("newTitle", newTitle)):"");
    internal static string SiegeSentence(string actor,string settlement,string oldOwner,string newOwner) => WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_SiegeSentence", ("actor", actor), ("settlement", settlement), ("oldOwner", oldOwner), ("newOwner", newOwner));
    internal static string GrantSentence(string settlement,string owner,string label) => WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_GrantSentence", ("settlement", settlement), ("owner", owner), ("label", label));
    internal static string TransferSentence(string settlement,string oldOwner,string newOwner,string label) => WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_TransferSentence", ("settlement", settlement), ("oldOwner", oldOwner), ("newOwner", newOwner), ("label", label));
    internal static int SiegePriority(bool town)=>town?65:45;
    // Softer than before: per-battle cost now also builds war weariness, which drains stability weekly.
    internal static int SiegeStability(bool town,bool winner)=>winner?(town?3:1):(town?-4:-2);
    internal static bool ShouldIncludeBattle(bool player,bool winnerLord,bool loserLord,bool raid,bool siege,int troops,int threshold)
        => (player||winnerLord||loserLord)&&(player||(!raid&&(siege||(winnerLord&&loserLord)||troops>threshold)));
    // Battles are frequent; only large or player/home fights should reach the headline bar once focus bonuses apply.
    internal static int BattlePriority(int troops,int threshold,bool lordVsLord,bool siege) => (troops>=1000?45:(troops>threshold?35:(lordVsLord?30:20)))+(siege?5:0);
    // Only large field battles swing stability, by one point; the rest is carried by war weariness.
    internal static int BattleStability(int troops,int threshold)=>troops>threshold?1:0;
    internal static string BattleSentence(string location,bool sallyOut,bool siege,string winner,string winnerFaction,string loser,string loserFaction,int troops)
        => WeeklyMaterialTexts.Fact(sallyOut?"AF_WEEKLY_Fact_BattleSentenceSally":siege?"AF_WEEKLY_Fact_BattleSentenceSiege":"AF_WEEKLY_Fact_BattleSentenceField", ("location", BattleLocation(location)), ("winner", BattleActor(winner)), ("winnerFaction", winnerFaction), ("loser", BattleActor(loser)), ("loserFaction", loserFaction));
    internal static string BattleDetail(int winnerTroops,string winnerLoss,int loserTroops,string loserLoss,bool sallyOut,bool winnerLord,string winnerTitle,bool loserLord,string loserTitle)
        => WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_BattleDetailBase", ("winnerTroops", winnerTroops.ToString()), ("winnerLoss", BattleCasualties(winnerLoss)), ("loserTroops", loserTroops.ToString()), ("loserLoss", BattleCasualties(loserLoss)))
            +(winnerLord?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_BattleDetailWinnerLord", ("winnerTitle", winnerTitle)):"")+(loserLord?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_BattleDetailLoserLord", ("loserTitle", loserTitle)):"")+(sallyOut?WeeklyMaterialTexts.Prompt("AF_WEEKLY_Fact_BattleDetailSallyNote"):"");
    internal static string CapturedWho(bool ruler,string kingdom,string name,string affiliation)=>ruler?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_CapturedWhoRuler", ("kingdom", kingdom), ("name", name)):WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_CapturedWhoOther", ("name", name), ("affiliation", affiliation));
    internal static string CapturedSentence(string who,bool hasCaptor,string captor)=>hasCaptor?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_CapturedSentenceBy", ("who", who), ("captor", captor)):WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_CapturedSentenceEnemy", ("who", who));
    internal static string CapturedDetail(string title,bool hasCaptor,string captor,string captorTitle)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_CapturedDetailBase", ("title", title))+(hasCaptor?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_CapturedDetailCaptor", ("captor", captor), ("captorTitle", captorTitle)):"");
    // Below the 30-point headline floor: an ordinary captive is a supporting line, never the lead.
    internal static int CapturedPriority(bool ruler)=>ruler?65:28;
    internal static int CapturedStability(bool ruler)=>ruler?-4:-1;
    internal static string DestroyedSentence(string kingdom)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_DestroyedSentence", ("kingdom", kingdom));
    internal static string DestroyedDetail(string ruler)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_DestroyedDetail", ("ruler", ruler));
    internal static string RebellionSentence(string clan,string kingdom)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_RebellionSentence", ("clan", clan), ("kingdom", kingdom));
    internal static string RebellionDetail(string clanLeader,string king)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_RebellionDetail", ("clanLeader", clanLeader), ("king", king));
    internal static string CreatedSentence(string clan,string kingdom)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_CreatedSentence", ("clan", clan), ("kingdom", kingdom));
    internal static string CreatedDetail(string founder,bool hadOld,string old)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_CreatedDetailBase", ("founder", founder))+(hadOld?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_CreatedDetailOld", ("old", old)):"");
    internal const int ArmyGatheredTroopThreshold = 1000;
    internal static string RaidSentence(string settlement,string raider,string faction)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_RaidSentence", ("settlement", settlement), ("raider", raider), ("faction", faction));
    internal static string RaidDetail(string owner)=>owner.Length>0?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_RaidDetail", ("owner", owner)):"";
    internal static string CivilWarSentence(string kingdom)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_CivilWarSentence", ("kingdom", kingdom));
    internal static string CivilWarDetail(string ruler)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_CivilWarDetail", ("ruler", ruler));
    internal static string AllianceFormedSentence(string first,string second)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_AllianceFormedSentence", ("first", first), ("second", second));
    internal static string AllianceEndedSentence(string first,string second)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_AllianceEndedSentence", ("first", first), ("second", second));
    internal static string AllianceDetail(string first,string firstLeader,string second,string secondLeader)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_AllianceDetail", ("first", first), ("firstLeader", firstLeader), ("second", second), ("secondLeader", secondLeader));
    internal static string RulerChangedSentence(string kingdom,string ruler,string clan,bool interim)
        => interim?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_RulerChangedInterim", ("kingdom", kingdom), ("clan", clan), ("ruler", ruler)):WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_RulerChangedNew", ("ruler", ruler), ("clan", clan), ("kingdom", kingdom));
    internal static string RulerChangedDetail(bool hasPrevious,string previous,string previousClan,bool previousDying)
        => hasPrevious?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_RulerChangedPrevious", ("previous", previous), ("previousClan", previousClan))+(previousDying?WeeklyMaterialTexts.Prompt("AF_WEEKLY_Fact_RulerChangedPreviousDying"):""):WeeklyMaterialTexts.Prompt("AF_WEEKLY_Fact_RulerChangedUnknown");
    internal static string MarriageSentence(string first,string firstTitle,string second,string secondTitle)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_MarriageSentence", ("first", first), ("firstTitle", firstTitle), ("second", second), ("secondTitle", secondTitle));
    internal static string DefectionSentence(string clan,int fiefs,bool hadOld,string oldKingdom,string newKingdom)
        => hadOld?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_DefectionSwitch", ("clan", clan), ("fiefs", fiefs.ToString()), ("oldKingdom", oldKingdom), ("newKingdom", newKingdom)):WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_DefectionNew", ("clan", clan), ("fiefs", fiefs.ToString()), ("newKingdom", newKingdom));
    internal static string DefectionDetail(string leader,string fiefs)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_DefectionDetail", ("leader", leader), ("fiefs", fiefs));
    internal static string ClanDestroyedSentence(string clan,bool hadKingdom,string kingdom)=>hadKingdom?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_ClanDestroyedKingdom", ("kingdom", kingdom), ("clan", clan)):WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_ClanDestroyedNoKingdom", ("clan", clan));
    internal static string ClanDestroyedDetail(string leader)=>leader.Length>0?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_ClanDestroyedDetail", ("leader", leader)):WeeklyMaterialTexts.Prompt("AF_WEEKLY_Fact_ClanDestroyedDetailUnknown");
    internal static string ArmyGatheredSentence(string kingdom,string leaderTitle,string leader,int troops,string place)
        => place.Length>0?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_ArmyGatheredPlace", ("kingdom", kingdom), ("leaderTitle", leaderTitle), ("leader", leader), ("troops", troops.ToString()), ("place", place)):WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_ArmyGathered", ("kingdom", kingdom), ("leaderTitle", leaderTitle), ("leader", leader), ("troops", troops.ToString()));
    internal static string ArmyGatheredDetail(int parties)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_ArmyGatheredDetail", ("parties", parties.ToString()));
    internal static string TownUnrestSentence(string town,string owner)=>owner.Length>0?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_TownUnrestOwner", ("town", town), ("owner", owner)):WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_TownUnrest", ("town", town));
    internal static string TownRebellionSentence(string town,string oldOwner,string rebels)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_TownRebellion", ("town", town), ("oldOwner", oldOwner), ("rebels", rebels));
    internal static string AnnexedSentence(string receiving,string annexed)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_AnnexedSentence", ("annexed", annexed), ("receiving", receiving));
    internal static string AnnexedDetail(string receivingRuler,string formerRuler,int clans)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_AnnexedDetail", ("receivingRuler", receivingRuler), ("formerRuler", formerRuler), ("clans", clans.ToString()));
    internal static string VassalageEstablishedSentence(string suzerain,string vassal,string type)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_VassalageEstablished", ("vassal", vassal), ("suzerain", suzerain), ("type", type));
    internal static string VassalageEndedSentence(string suzerain,string vassal,string type)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_VassalageEnded", ("vassal", vassal), ("suzerain", suzerain), ("type", type));
    internal static string VassalageDetail(string suzerainRuler,string vassalRuler)=>WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_VassalageDetail", ("suzerainRuler", suzerainRuler), ("vassalRuler", vassalRuler));
    internal static bool IsNaturalDeath(string detail)=>detail=="DiedOfOldAge"||detail=="DiedInLabor";
    internal static string KilledWho(bool ruler,string kingdom,string clan,string name)=>ruler?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_KilledWhoRuler", ("kingdom", kingdom), ("name", name)):WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_KilledWhoLord", ("kingdom", kingdom), ("clan", clan), ("name", name));
    internal static int DeathPriority(bool ruler,bool natural)=>ruler?(natural?70:90):(natural?25:50);
    internal static string DeathKind(bool ruler)=>ruler?"ruler_killed":"lord_killed";
    internal static int DeathStability(bool ruler,bool natural)=>ruler?-8:(natural?0:-2);
    internal static string DeathSentence(WorldBulletinDeathMaterialCapture c)
    {
        if(c.HasPublicExecutionFacts)return PublicExecutionSentence(c);
        if(c.Execution)return c.HasKiller?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_DeathExecutedBy", ("who", c.Who), ("killer", c.KillerName)):WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_DeathExecuted", ("who", c.Who));
        if(c.Detail=="DiedInBattle")return c.HasKiller?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_DeathBattleKiller", ("who", c.Who), ("killer", c.KillerName)):WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_DeathBattle", ("who", c.Who));
        if(c.Detail=="Murdered")return c.HasKiller?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_DeathMurderKiller", ("who", c.Who), ("killer", c.KillerName)):WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_DeathMurder", ("who", c.Who));
        if(c.Detail=="DiedOfOldAge")return WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_DeathOldAge", ("who", c.Who));
        return WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_DeathOther", ("who", c.Who));
    }
    // Whole-sentence templates keep each package language's own word order. Method and charge come from the invariant
    // source labels and are projected to the package language; unknown (extension) labels pass through unchanged.
    private static string PublicExecutionSentence(WorldBulletinDeathMaterialCapture c)
    {
        string venue=string.IsNullOrWhiteSpace(c.VenueName)?"":WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_Exec_Venue",("VENUE",c.VenueName));
        string method=string.IsNullOrWhiteSpace(c.MethodLabel)?WeeklyMaterialTexts.Prompt("AF_WEEKLY_Fact_Exec_MethodPlain"):WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_Exec_MethodBy",("METHOD",CampaignSystemFactProjection.ExecutionMethodForPackage(c.MethodLabel)));
        string charge=string.IsNullOrWhiteSpace(c.ChargeLabel)?"":WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_Exec_Charge",("CHARGE",CampaignSystemFactProjection.ExecutionValueForPackage(c.ChargeLabel)));
        return c.HasKiller
            ?WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_Exec_BulletinByKiller",("WHO",c.Who),("VENUE",venue),("KILLER",c.KillerName),("METHOD",method),("CHARGE",charge))
            :WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_Exec_BulletinNoKiller",("WHO",c.Who),("VENUE",venue),("METHOD",method),("CHARGE",charge));
    }
    internal static string DeathDetail(WorldBulletinDeathMaterialCapture c)
    {
        var text=new StringBuilder();text.Append(WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_DeathDetailBase", ("victimTitle", c.VictimTitle), ("age", c.Age.ToString())));
        if(c.HasKiller)text.Append(WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_DeathDetailKiller", ("killer", c.KillerName), ("killerTitle", c.KillerTitle)));
        if(c.HasPublicExecutionFacts)text.Append(WeeklyMaterialTexts.Fact(c.PlayerStruck?"AF_WEEKLY_Fact_DeathDetailTrialStruck":"AF_WEEKLY_Fact_DeathDetailTrialHeadsman", ("tone", c.ToneLabel), ("legitimacy", c.LegitimacyLabel)));
        else if(c.Execution&&!string.IsNullOrWhiteSpace(c.ExecutionPlace))text.Append(WeeklyMaterialTexts.Fact("AF_WEEKLY_Fact_DeathDetailPlace", ("place", c.ExecutionPlace)));
        return text.ToString();
    }
    internal static string DeathGroup(bool execution,string detail,string killerId,string victimId,int day,string pairKey)
        => execution?"execution:"+killerId+":"+day:detail=="Murdered"?"murder:"+killerId:detail=="DiedInBattle"?"clash:"+day+":"+pairKey:"death:"+victimId;
}
internal sealed class WorldBulletinDeathMaterialCapture
{
    internal string Who, VictimTitle, KillerName, KillerTitle, Detail, Venue, Method, Charge, ToneLabel, LegitimacyLabel, ExecutionPlace;
    // Public execution: invariant source labels. The package-language sentence is composed in DeathSentence.
    internal string VenueName, MethodLabel, ChargeLabel;
    internal int Age;
    internal bool HasKiller, HasPublicExecutionFacts, Execution, PlayerStruck;
}
