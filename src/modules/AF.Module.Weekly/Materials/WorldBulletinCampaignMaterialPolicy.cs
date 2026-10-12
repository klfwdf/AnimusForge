using System;
using System.Text;
namespace AnimusForge;
// Event-time detached facts only. Bannerlord handles, participant capture and writes remain adapters.
internal static class WorldBulletinCampaignMaterialPolicy
{
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
        => kingdom+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0736")+ruler+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0737")+towns+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0738")+castles+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0739")+(stabilityEnabled?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0740")+stability:"")+"，"+(enemies.Count>0?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0741")+string.Join("、",enemies)+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0742"):WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0743"));
    internal static string PairKey(string x,string y) => string.CompareOrdinal(x,y)<=0?x+"|"+y:y+"|"+x;
    internal static string WarReason(string detail) => detail switch {
        "CausedByPlayerHostility" => WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0744"),
        "CausedByKingdomDecision" => WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0745"), "CausedByRebellion" => WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0746"),
        "CausedByCrimeRatingChange" => WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0747"), "CausedByKingdomCreation" => WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0748"),
        "CausedByClaimOnThrone" => WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0749"), "CausedByCallToWarAgreement" => WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0750"), _ => WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0751") };
    internal static string WarSentence(string first,string second) => first+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0752")+second+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0753");
    internal static string WarDetail(string firstLeader,string secondLeader,string reason) => WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0754")+firstLeader+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0755")+secondLeader+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0756")+reason;
    internal static string PeaceSentence(string first,string second) => first+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0757")+second+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0758");
    internal static string PeaceDetail(string first,string firstLeader,string second,string secondLeader,bool decision) => first+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0759")+firstLeader+"；"+second+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0760")+secondLeader+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0761")+(decision?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0762"):WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0763"));
    internal static string HeroTitle(bool ruler,string kingdom,string clan,bool clanLeader) => ruler?kingdom+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0764"):kingdom+clan+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0765")+(clanLeader?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0766"):WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0767"));
    internal static string OwnerDetail(bool town,bool hasOld,string oldName,string oldTitle,bool hasNew,string newName,string newTitle) => (town?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0768"):WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0769"))+(hasOld?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0770")+oldName+"（"+oldTitle+"）":"")+(hasNew?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0771")+newName+"（"+newTitle+"）":"");
    internal static string SiegeSentence(string actor,string settlement,string oldOwner,string newOwner) => actor+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0772")+settlement+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0773")+oldOwner+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0774")+newOwner+"。";
    internal static string GrantSentence(string settlement,string owner,string label) => settlement+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0775")+owner+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0776")+label+"）。";
    internal static string TransferSentence(string settlement,string oldOwner,string newOwner,string label) => settlement+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0777")+label+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0778")+oldOwner+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0779")+newOwner+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0780");
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
        => location+(sallyOut?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0781"):siege?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0782"):WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0783"))+winner+"（"+winnerFaction+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0784")+loser+"（"+loserFaction+"）"+(sallyOut?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0785"):"")+"。";
    internal static string BattleDetail(int winnerTroops,string winnerLoss,int loserTroops,string loserLoss,bool sallyOut,bool winnerLord,string winnerTitle,bool loserLord,string loserTitle)
        => WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0786")+winnerTroops+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0787")+winnerLoss+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0788")+loserTroops+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0789")+loserLoss+(winnerLord?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0790")+winnerTitle:"")+(loserLord?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0791")+loserTitle:"")+(sallyOut?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0792"):"");
    internal static string CapturedWho(bool ruler,string kingdom,string name,string affiliation)=>ruler?kingdom+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0793")+name:name+"（"+affiliation+"）";
    internal static string CapturedSentence(string who,bool hasCaptor,string captor)=>who+(hasCaptor?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0794")+captor+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0795"):WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0796"));
    internal static string CapturedDetail(string title,bool hasCaptor,string captor,string captorTitle)=>WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0797")+title+(hasCaptor?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0798")+captor+"（"+captorTitle+"）":"");
    // Below the 30-point headline floor: an ordinary captive is a supporting line, never the lead.
    internal static int CapturedPriority(bool ruler)=>ruler?65:28;
    internal static int CapturedStability(bool ruler)=>ruler?-4:-1;
    internal static string DestroyedSentence(string kingdom)=>kingdom+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0799");
    internal static string DestroyedDetail(string ruler)=>WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0800")+ruler;
    internal static string RebellionSentence(string clan,string kingdom)=>clan+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0801")+kingdom+"。";
    internal static string RebellionDetail(string clanLeader,string king)=>WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0802")+clanLeader+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0803")+king;
    internal static string CreatedSentence(string clan,string kingdom)=>clan+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0804")+kingdom+"。";
    internal static string CreatedDetail(string founder,bool hadOld,string old)=>WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0805")+founder+(hadOld?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0806")+old:"");
    internal const int ArmyGatheredTroopThreshold = 1000;
    internal static string RaidSentence(string settlement,string raider,string faction)=>settlement+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0807")+raider+"（"+faction+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0808");
    internal static string RaidDetail(string owner)=>owner.Length>0?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0809")+owner:"";
    internal static string CivilWarSentence(string kingdom)=>kingdom+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0810");
    internal static string CivilWarDetail(string ruler)=>WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0811")+ruler;
    internal static string AllianceFormedSentence(string first,string second)=>first+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0812")+second+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0813");
    internal static string AllianceEndedSentence(string first,string second)=>first+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0814")+second+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0815");
    internal static string AllianceDetail(string first,string firstLeader,string second,string secondLeader)=>first+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0816")+firstLeader+"；"+second+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0817")+secondLeader;
    internal static string RulerChangedSentence(string kingdom,string ruler,string clan,bool interim)
        => interim?kingdom+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0818")+clan+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0819")+ruler+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0820"):ruler+"（"+clan+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0821")+kingdom+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0822");
    internal static string RulerChangedDetail(bool hasPrevious,string previous,string previousClan,bool previousDying)
        => hasPrevious?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0823")+previous+"（"+previousClan+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0824")+(previousDying?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0825"):""):WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0826");
    internal static string MarriageSentence(string first,string firstTitle,string second,string secondTitle)=>firstTitle+first+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0827")+secondTitle+second+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0828");
    internal static string DefectionSentence(string clan,int fiefs,bool hadOld,string oldKingdom,string newKingdom)
        => clan+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0829")+fiefs+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0830")+(hadOld?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0831")+oldKingdom+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0832"):WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0833"))+newKingdom+"。";
    internal static string DefectionDetail(string leader,string fiefs)=>WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0834")+leader+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0835")+fiefs;
    internal static string ClanDestroyedSentence(string clan,bool hadKingdom,string kingdom)=>(hadKingdom?kingdom+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0836"):"")+clan+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0837");
    internal static string ClanDestroyedDetail(string leader)=>leader.Length>0?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0838")+leader:WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0839");
    internal static string ArmyGatheredSentence(string kingdom,string leaderTitle,string leader,int troops,string place)
        => kingdom+leaderTitle+leader+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0840")+troops+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0841")+(place.Length>0?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0842")+place:"")+"。";
    internal static string ArmyGatheredDetail(int parties)=>WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0843")+parties+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0844");
    internal static string TownUnrestSentence(string town,string owner)=>town+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0845")+(owner.Length>0?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0846")+owner:"")+"。";
    internal static string TownRebellionSentence(string town,string oldOwner,string rebels)=>town+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0847")+oldOwner+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0848")+rebels+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0849");
    internal static string AnnexedSentence(string receiving,string annexed)=>annexed+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0850")+receiving+"，"+annexed+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0851")+receiving+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0852");
    internal static string AnnexedDetail(string receivingRuler,string formerRuler,int clans)=>WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0853")+receivingRuler+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0854")+formerRuler+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0855")+clans+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0856");
    internal static string VassalageEstablishedSentence(string suzerain,string vassal,string type)=>vassal+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0857")+suzerain+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0858")+type+"。";
    internal static string VassalageEndedSentence(string suzerain,string vassal,string type)=>vassal+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0859")+suzerain+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0860")+type+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0861");
    internal static string VassalageDetail(string suzerainRuler,string vassalRuler)=>WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0862")+suzerainRuler+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0863")+vassalRuler;
    internal static bool IsNaturalDeath(string detail)=>detail=="DiedOfOldAge"||detail=="DiedInLabor";
    internal static string KilledWho(bool ruler,string kingdom,string clan,string name)=>ruler?kingdom+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0864")+name:kingdom+clan+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0865")+name;
    internal static int DeathPriority(bool ruler,bool natural)=>ruler?(natural?70:90):(natural?25:50);
    internal static string DeathKind(bool ruler)=>ruler?"ruler_killed":"lord_killed";
    internal static int DeathStability(bool ruler,bool natural)=>ruler?-8:(natural?0:-2);
    internal static string DeathSentence(WorldBulletinDeathMaterialCapture c)
    {
        if(c.HasPublicExecutionFacts)return c.Who+c.Venue+(c.HasKiller?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0866")+c.KillerName:WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0867"))+c.Method+c.Charge+"。";
        if(c.Execution)return c.Who+(c.HasKiller?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0868")+c.KillerName+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0869"):WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0870"));
        if(c.Detail=="DiedInBattle")return c.Who+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0871")+(c.HasKiller?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0872")+c.KillerName+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0873"):"。");
        if(c.Detail=="Murdered")return c.Who+(c.HasKiller?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0874")+c.KillerName+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0875"):WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0876"));
        if(c.Detail=="DiedOfOldAge")return c.Who+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0877");
        return c.Who+WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0878");
    }
    internal static string DeathDetail(WorldBulletinDeathMaterialCapture c)
    {
        var text=new StringBuilder();text.Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0879")).Append(c.VictimTitle).Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0880")).Append(c.Age).Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0881"));
        if(c.HasKiller)text.Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0882")).Append(c.KillerName).Append("（").Append(c.KillerTitle).Append("）");
        if(c.HasPublicExecutionFacts)text.Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0883")).Append(c.ToneLabel).Append("，").Append(c.LegitimacyLabel).Append(c.PlayerStruck?WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0884"):WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0885"));
        else if(c.Execution&&!string.IsNullOrWhiteSpace(c.ExecutionPlace))text.Append(WeeklyMaterialTexts.Raw("AF_WEEKLY_Material_0886")).Append(c.ExecutionPlace);
        return text.ToString();
    }
    internal static string DeathGroup(bool execution,string detail,string killerId,string victimId,int day,string pairKey)
        => execution?"execution:"+killerId+":"+day:detail=="Murdered"?"murder:"+killerId:detail=="DiedInBattle"?"clash:"+day+":"+pairKey:"death:"+victimId;
}
internal sealed class WorldBulletinDeathMaterialCapture
{
    internal string Who, VictimTitle, KillerName, KillerTitle, Detail, Venue, Method, Charge, ToneLabel, LegitimacyLabel, ExecutionPlace;
    internal int Age;
    internal bool HasKiller, HasPublicExecutionFacts, Execution, PlayerStruck;
}
