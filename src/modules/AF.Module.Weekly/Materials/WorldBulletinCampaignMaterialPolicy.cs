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
        => kingdom+"：君主"+ruler+"，城镇"+towns+"座、城堡"+castles+"座"+(stabilityEnabled?"，稳定度"+stability:"")+"，"+(enemies.Count>0?"正与"+string.Join("、",enemies)+"交战":"眼下没有战事");
    internal static string PairKey(string x,string y) => string.CompareOrdinal(x,y)<=0?x+"|"+y:y+"|"+x;
    internal static string WarReason(string detail) => detail switch {
        "CausedByPlayerHostility" => "玩家一方的敌对行为",
        "CausedByKingdomDecision" => "王国议会决议", "CausedByRebellion" => "叛乱",
        "CausedByCrimeRatingChange" => "犯罪恶名", "CausedByKingdomCreation" => "新王国建立",
        "CausedByClaimOnThrone" => "争夺王位", "CausedByCallToWarAgreement" => "盟约参战", _ => "未载明" };
    internal static string WarSentence(string first,string second) => first+"向"+second+"宣战，两国进入战争状态。";
    internal static string WarDetail(string firstLeader,string secondLeader,string reason) => "宣战方君主："+firstLeader+"；被宣战方君主："+secondLeader+"；宣战缘由："+reason;
    internal static string PeaceSentence(string first,string second) => first+"与"+second+"停战议和，双方结束战争状态。";
    internal static string PeaceDetail(string first,string firstLeader,string second,string secondLeader,bool decision) => first+"君主："+firstLeader+"；"+second+"君主："+secondLeader+"；议和方式："+(decision?"王国议会决议":"双方议定");
    internal static string HeroTitle(bool ruler,string kingdom,string clan,bool clanLeader) => ruler?kingdom+"君主":kingdom+clan+"家族"+(clanLeader?"族长":"成员");
    internal static string OwnerDetail(bool town,bool hasOld,string oldName,string oldTitle,bool hasNew,string newName,string newTitle) => (town?"城镇":"城堡")+(hasOld?"；原领主："+oldName+"（"+oldTitle+"）":"")+(hasNew?"；新领主："+newName+"（"+newTitle+"）":"");
    internal static string SiegeSentence(string actor,string settlement,string oldOwner,string newOwner) => actor+"攻陷"+settlement+"，该地由"+oldOwner+"转归"+newOwner+"。";
    internal static string GrantSentence(string settlement,string owner,string label) => settlement+"被授予"+owner+"（方式："+label+"）。";
    internal static string TransferSentence(string settlement,string oldOwner,string newOwner,string label) => settlement+"以“"+label+"”的方式由"+oldOwner+"转归"+newOwner+"，并非攻城夺取。";
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
        => location+(sallyOut?"出城战：":siege?"攻城战：":"一战：")+winner+"（"+winnerFaction+"）击败"+loser+"（"+loserFaction+"）"+(sallyOut?"的本次参战部队":"")+(troops>0?"，双方约"+troops+"人参战。":"。");
    internal static string BattleDetail(int winnerTroops,string winnerLoss,int loserTroops,string loserLoss,bool sallyOut,bool winnerLord,string winnerTitle,bool loserLord,string loserTitle)
        => "胜方"+winnerTroops+"人，"+winnerLoss+"；败方"+loserTroops+"人，"+loserLoss+(winnerLord?"；胜方统帅："+winnerTitle:"")+(loserLord?"；败方统帅："+loserTitle:"")+(sallyOut?"；本次出城战仅记录本场交战的参战部队，不代表围城军或守军整支军团覆灭；未确认整支军团被击败":"");
    internal static string CapturedWho(bool ruler,string kingdom,string name,string affiliation)=>ruler?kingdom+"的君主"+name:name+"（"+affiliation+"）";
    internal static string CapturedSentence(string who,bool hasCaptor,string captor)=>who+(hasCaptor?"被"+captor+"俘虏。":"被敌方俘虏。");
    internal static string CapturedDetail(string title,bool hasCaptor,string captor,string captorTitle)=>"被俘者身份："+title+(hasCaptor?"；俘获者："+captor+"（"+captorTitle+"）":"");
    // Below the 30-point headline floor: an ordinary captive is a supporting line, never the lead.
    internal static int CapturedPriority(bool ruler)=>ruler?65:28;
    internal static int CapturedStability(bool ruler)=>ruler?-4:-1;
    internal static string DestroyedSentence(string kingdom)=>kingdom+"已经覆灭，这个王国不复存在。";
    internal static string DestroyedDetail(string ruler)=>"末代君主："+ruler;
    internal static string RebellionSentence(string clan,string kingdom)=>clan+"家族举兵反叛，脱离了"+kingdom+"。";
    internal static string RebellionDetail(string clanLeader,string king)=>"叛乱家族族长："+clanLeader+"；原王国君主："+king;
    internal static string CreatedSentence(string clan,string kingdom)=>clan+"家族建立了新王国"+kingdom+"。";
    internal static string CreatedDetail(string founder,bool hadOld,string old)=>"开国者："+founder+(hadOld?"；此前效忠："+old:"");
    internal const int ArmyGatheredTroopThreshold = 1000;
    internal static string RaidSentence(string settlement,string raider,string faction)=>settlement+"村遭"+raider+"（"+faction+"）劫掠得手。";
    internal static string RaidDetail(string owner)=>owner.Length>0?"该村隶属："+owner:"";
    internal static string CivilWarSentence(string kingdom)=>kingdom+"爆发内战，国内各家族兵戎相见。";
    internal static string CivilWarDetail(string ruler)=>"在位君主："+ruler;
    internal static string AllianceFormedSentence(string first,string second)=>first+"与"+second+"缔结同盟，两国结为盟友。";
    internal static string AllianceEndedSentence(string first,string second)=>first+"与"+second+"的同盟宣告解除，两国不再是盟友。";
    internal static string AllianceDetail(string first,string firstLeader,string second,string secondLeader)=>first+"君主："+firstLeader+"；"+second+"君主："+secondLeader;
    internal static string RulerChangedSentence(string kingdom,string ruler,string clan,bool interim)
        => interim?kingdom+"王位暂由"+clan+"家族的"+ruler+"代掌，新君将由王国议会推选。":ruler+"（"+clan+"家族）成为"+kingdom+"的新君主。";
    internal static string RulerChangedDetail(bool hasPrevious,string previous,string previousClan,bool previousDying)
        => hasPrevious?"前任君主："+previous+"（"+previousClan+"家族）"+(previousDying?"，已在本次变故中身故":""):"前任君主未载明";
    internal static string MarriageSentence(string first,string firstTitle,string second,string secondTitle)=>firstTitle+first+"与"+secondTitle+second+"成婚。";
    internal static string DefectionSentence(string clan,int fiefs,bool hadOld,string oldKingdom,string newKingdom)
        => clan+"家族携"+fiefs+"处封地"+(hadOld?"脱离"+oldKingdom+"，改投":"投效")+newKingdom+"。";
    internal static string DefectionDetail(string leader,string fiefs)=>"家族族长："+leader+"；所携封地："+fiefs;
    internal static string ClanDestroyedSentence(string clan,bool hadKingdom,string kingdom)=>(hadKingdom?kingdom+"的":"")+clan+"家族就此覆亡，不复存在。";
    internal static string ClanDestroyedDetail(string leader)=>leader.Length>0?"末任族长："+leader:"末任族长未载明";
    internal static string ArmyGatheredSentence(string kingdom,string leaderTitle,string leader,int troops,string place)
        => kingdom+leaderTitle+leader+"召集大军，约"+troops+"人应召"+(place.Length>0?"，集结于"+place:"")+"。";
    internal static string ArmyGatheredDetail(int parties)=>"应召部队："+parties+"支；这是集结而非交战，尚无战果";
    internal static string TownUnrestSentence(string town,string owner)=>town+"民心离散，城中酝酿叛乱"+(owner.Length>0?"，领主为"+owner:"")+"。";
    internal static string TownRebellionSentence(string town,string oldOwner,string rebels)=>town+"爆发民变，起义者驱逐了"+oldOwner+"，城池落入"+rebels+"之手。";
    internal static string AnnexedSentence(string receiving,string annexed)=>annexed+"并入"+receiving+"，"+annexed+"所有家族向"+receiving+"宣誓效忠，旧王国就此终结。";
    internal static string AnnexedDetail(string receivingRuler,string formerRuler,int clans)=>"吞并方君主："+receivingRuler+"；被吞并国末代君主："+formerRuler+"；转入家族："+clans+"个";
    internal static string VassalageEstablishedSentence(string suzerain,string vassal,string type)=>vassal+"承认"+suzerain+"的宗主权，成为其"+type+"。";
    internal static string VassalageEndedSentence(string suzerain,string vassal,string type)=>vassal+"与宗主"+suzerain+"的"+type+"条约终止，不再受其节制。";
    internal static string VassalageDetail(string suzerainRuler,string vassalRuler)=>"宗主国君主："+suzerainRuler+"；臣属国君主："+vassalRuler;
    internal static bool IsNaturalDeath(string detail)=>detail=="DiedOfOldAge"||detail=="DiedInLabor";
    internal static string KilledWho(bool ruler,string kingdom,string clan,string name)=>ruler?kingdom+"的君主"+name:kingdom+clan+"家族的"+name;
    internal static int DeathPriority(bool ruler,bool natural)=>ruler?(natural?70:90):(natural?25:50);
    internal static string DeathKind(bool ruler)=>ruler?"ruler_killed":"lord_killed";
    internal static int DeathStability(bool ruler,bool natural)=>ruler?-8:(natural?0:-2);
    internal static string DeathSentence(WorldBulletinDeathMaterialCapture c)
    {
        if(c.HasPublicExecutionFacts)return c.Who+c.Venue+(c.HasKiller?"被"+c.KillerName:"被")+c.Method+c.Charge+"。";
        if(c.Execution)return c.Who+(c.HasKiller?"被"+c.KillerName+"处决。":"被处决。");
        if(c.Detail=="DiedInBattle")return c.Who+"战死沙场"+(c.HasKiller?"，死于"+c.KillerName+"之手。":"。");
        if(c.Detail=="Murdered")return c.Who+(c.HasKiller?"遭"+c.KillerName+"谋杀。":"遭人谋杀。");
        if(c.Detail=="DiedOfOldAge")return c.Who+"寿终离世。";
        return c.Who+"离世。";
    }
    internal static string DeathDetail(WorldBulletinDeathMaterialCapture c)
    {
        var text=new StringBuilder();text.Append("死者身份：").Append(c.VictimTitle).Append("，年约").Append(c.Age).Append("岁");
        if(c.HasKiller)text.Append("；行事者：").Append(c.KillerName).Append("（").Append(c.KillerTitle).Append("）");
        if(c.HasPublicExecutionFacts)text.Append("；审判：").Append(c.ToneLabel).Append("，").Append(c.LegitimacyLabel).Append(c.PlayerStruck?"，由执行者亲自行刑":"，由行刑人行刑");
        else if(c.Execution&&!string.IsNullOrWhiteSpace(c.ExecutionPlace))text.Append("；地点：").Append(c.ExecutionPlace);
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
