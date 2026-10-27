using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;

// Run one process per real API reference line. No native scene/game/save opened.
internal static class Program
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static int _checks;
    private static void Check(bool ok, string label) { if (!ok) throw new Exception(label); _checks++; }
    private static object Field(object value, string name) => value.GetType().GetField(name, All)?.GetValue(value)
        ?? value.GetType().GetProperty(name, All).GetValue(value);
    private static object Call(Type type, object value, string name, params object[] args) => type.GetMethod(name, All).Invoke(value, args);
    private static object Blank(Type type) => FormatterServices.GetUninitializedObject(type);
    private static bool NoLog() => false;
    private static int Main(string[] args)
    {
        try
        {
            string[] paths=args[0].Split('|');
            AppDomain.CurrentDomain.AssemblyResolve+=(_,request)=>
            {
                foreach(string directory in paths)
                {
                    string path=Path.Combine(directory,new AssemblyName(request.Name).Name+".dll");
                    if(File.Exists(path))return Assembly.LoadFrom(path);
                }
                return null;
            };
            Run(Assembly.LoadFrom(args[1]), args[2], args.Length>3 && args[3]=="--reader-only");
            Console.WriteLine("PASS assertions="+_checks+" API="+args[2]+"; actual managed owner/cache fields, no live game/save/UI.");
            return 0;
        }
        catch(Exception ex) {Console.Error.WriteLine(ex);return 1;}
    }
    private static void Run(Assembly af, string api, bool readerOnly)
    {
        var harmony=new Harmony("AF.ConversationHistoryScopeTests");
        foreach(var method in af.GetType("AnimusForge.Logger",true).GetMethods(All).Where(x=>x.IsStatic&&x.ReturnType==typeof(void)&&!x.ContainsGenericParameters))
            harmony.Patch(method,prefix:new HarmonyMethod(typeof(Program),nameof(NoLog)));
        if(readerOnly) { VerifyReader(af); return; }
        Type scope=af.GetType("AnimusForge.PlayerConversationHistoryScope",true);
        bool Hidden(string line)=>(bool)Call(scope,null,"IsNpcPublicDocumentFact",line);
        string[] envelopes={
            "本人已正式发布外交公文，来源=diplomacy_document:doc，日期=今天，标题=声明。",
            "本人所在王庭已正式收到外交公文，来源=diplomacy_document:doc，日期=今天，标题=声明。",
            "经游戏机制确认的外交结果，来源=diplomacy_document:doc，动作=trade。",
            "I have officially published a diplomatic document, source=diplomacy_document:doc, date=today.",
            "The court where I reside has officially received a diplomatic document, source=diplomacy_document:doc, date=today.",
            "Diplomatic result confirmed by game mechanics, source=diplomacy_document:doc, action=trade."
        };
        foreach(string prefix in new[]{"[AFEF NPC行为补充]","[AFEF NPC_ACTION]","【AFEF NPC行为补充】"})
        foreach(string envelope in envelopes)
        {
            string fact=prefix+" "+envelope;
            Check(Hidden(fact),"NPC public document excluded for "+prefix);
            Check(!Hidden("Ada: "+fact),"player quotation retained");
            Check(!Hidden("Caladog: "+fact),"NPC utterance retained");
        }
        foreach(string line in new[]{"[AFEF PLAYER_ACTION] "+envelopes[0],"[AFEF NPC_ACTION] Accepted the player's gift.","[AFEF NPC_ACTION] A promise made to the player, source=diplomacy_dialogue:x", "[AFEF NPC_ACTION] unknown source=diplomacy_document:x", "Unknown old history", "", null})
            Check(!Hidden(line),"interaction/unknown text retained");

        VerifyReader(af);

        VerifyVisibilityCache(af,api);
    }
    private static void VerifyReader(Assembly af)
    {
        // Real reader: hidden facts must not consume the visible tail limit;
        // editable ordinals still address the original unmodified saved lines.
        string[] envelopes={
            "本人已正式发布外交公文，来源=diplomacy_document:doc，日期=今天，标题=声明。",
            "本人所在王庭已正式收到外交公文，来源=diplomacy_document:doc，日期=今天，标题=声明。",
            "Diplomatic result confirmed by game mechanics, source=diplomacy_document:doc, action=trade."
        };
        Type stateType=af.GetType("AnimusForge.MemoryBusinessStateOwner",true);
        object state=Activator.CreateInstance(stateType,true);
        Type adapterType=af.GetType("AnimusForge.MemoryHistoryCommitBannerlordAdapter",true);
        object adapter=Activator.CreateInstance(adapterType,All,null,new object[]{state,null},null);
        IDictionary history=(IDictionary)Field(state,"History");
        Type dayType=af.GetType("AnimusForge.MyBehavior+DialogueDay",true);
        object day=Activator.CreateInstance(dayType,true);
        dayType.GetField("GameDayIndex",All).SetValue(day,7);
        dayType.GetField("GameDate",All).SetValue(day,"Day 7");
        var raw=(List<string>)Field(day,"Lines");
        raw.AddRange(new[]{"Ada: First exchange", "[AFEF NPC_ACTION] "+envelopes[0], "Caladog: Second exchange", "[AF_SCENE_SESSION:9] [AFEF NPC_ACTION] "+envelopes[1], "[AFEF NPC_ACTION] Accepted the player's gift.", "[AFEF NPC_ACTION] "+envelopes[2]});
        IList days=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(dayType));days.Add(day);history["lord_5_1"]=days;
        string[] original=raw.ToArray();
        MethodInfo display=adapterType.GetMethod("GetPlayerFacingDialogueHistoryEntriesById",All,null,new[]{typeof(string),typeof(int)},null);
        MethodInfo model=adapterType.GetMethod("GetDialogueHistoryEntriesById",All,null,new[]{typeof(string),typeof(int)},null);
        Check(display!=null&&model!=null,"display and model-facing readers are distinct entry points");
        IList entries=(IList)display.Invoke(adapter,new object[]{"lord_5_1",2});
        Check(entries.Count==2,"UI tail counts two player interactions, ignoring unrelated court facts");
        Check((int)Field(entries[0],"LineOrdinal")==2&&(int)Field(entries[1],"LineOrdinal")==4,"edit/delete keeps original ordinals");
        Check((string)Field(entries[0],"Kind")=="npc"&&(string)Field(entries[1],"Kind")=="afef_npc","spoken reply and interaction fact retain roles");
        Check(raw.SequenceEqual(original)&&ReferenceEquals(history["lord_5_1"],days),"underlying NPC history and diplomatic memory retained");
        var all=(IList)display.Invoke(adapter,new object[]{"lord_5_1",260});
        Check(all.Count==3&&(int)Field(all[0],"LineOrdinal")==0,"all UI history remains chronological");
        Check(raw.SequenceEqual(original),"repeated UI open does not delete old memory");
        // Model-facing reader (ExecutionAddressLlm, RP item introduction, ...) is never scope-filtered.
        var modelAll=(IList)model.Invoke(adapter,new object[]{"lord_5_1",260});
        Check(modelAll.Count==6,"model-facing reader keeps court document facts; actual="+modelAll.Count);
        for(int i=0;i<6;i++) Check((int)Field(modelAll[i],"LineOrdinal")==i,"model-facing reader keeps every original ordinal");
        var modelTail=(IList)model.Invoke(adapter,new object[]{"lord_5_1",2});
        Check(modelTail.Count==2&&(int)Field(modelTail[0],"LineOrdinal")==4&&(int)Field(modelTail[1],"LineOrdinal")==5,"model-facing tail includes newest court fact");
        Check(raw.SequenceEqual(original),"model read does not mutate memory");
    }
    private static void VerifyVisibilityCache(Assembly af, string api)
    {
        // Actual 1.5 private cache bindings/list ownership, with detached native
        // objects. This is the same previous/current-list primitive used on Leave.
        Type compat=af.GetType("AnimusForge.CaptivityMapVisibilityCompat",true);
        Assembly game=Assembly.Load("TaleWorlds.CampaignSystem");
        Type campaignType=game.GetType("TaleWorlds.CampaignSystem.Campaign",true);
        Type partyType=game.GetType("TaleWorlds.CampaignSystem.Party.PartyBase",true);
        object campaign=Blank(campaignType),captor=Blank(partyType),unrelated=Blank(partyType);
        if(api=="1.5")
        {
            Type cacheType=game.GetType("TaleWorlds.CampaignSystem.CampaignTickCacheDataStore",true);
            object cache=Blank(cacheType);
            IList previous=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(partyType));
            IList current=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(partyType));
            previous.Add(captor);previous.Add(unrelated);current.Add(captor);current.Add(unrelated);
            campaignType.GetField("_tickData",All).SetValue(campaign,cache);
            cacheType.GetField("_previousFrameUpdatedPartyBases",All).SetValue(cache,previous);
            cacheType.GetField("_currentFrameUpdatedPartyBases",All).SetValue(cache,current);
            Call(compat,null,"ForgetTransientVisibility",campaign,captor);
            Check(previous.Count==1&&ReferenceEquals(previous[0],unrelated),"actual previous-frame list removes exact captor");
            Check(current.Count==1&&ReferenceEquals(current[0],unrelated),"actual current-frame list removes exact captor");
            Call(compat,null,"ForgetTransientVisibility",campaign,captor);
            Check(previous.Count==1&&current.Count==1,"cache removal idempotent, others untouched");
        }
        else
        {
            Call(compat,null,"ForgetTransientVisibility",campaign,captor);
            Check(true,"old API does not bind 1.5 private cache fields");
        }
    }
}
