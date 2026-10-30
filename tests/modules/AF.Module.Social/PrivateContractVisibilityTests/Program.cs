using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;

internal static class Program
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    static string resource;
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
    static object Get(object o, string name) => o.GetType().GetField(name, All).GetValue(o);
    static void Set(object o, string name, object value) => o.GetType().GetField(name, All).SetValue(o, value);
    static bool PathPrefix(string fileName, ref string __result) { __result = resource; return false; }
    static bool Yes(ref bool __result) { __result = true; return false; }
    static bool Day(ref int __result) { __result = 100; return false; }
    static int Main(string[] args)
    {
        try
        {
            var dirs = args[0].Split('|');
            AppDomain.CurrentDomain.AssemblyResolve += (_, e) => {
                var file = new AssemblyName(e.Name).Name + ".dll";
                foreach (var dir in dirs) if (File.Exists(Path.Combine(dir, file))) return Assembly.LoadFrom(Path.Combine(dir, file));
                return null;
            };
            var af = Assembly.LoadFrom(args[1]);
            resource = args[2];
            var h = new Harmony("AF.PrivateContractVisibilityTests");
            h.Patch(af.GetType("AnimusForge.AnimusForgeModulePaths").GetMethod("GetModuleDataFilePath"), prefix: new HarmonyMethod(typeof(Program), nameof(PathPrefix)));
            var host = af.GetType("AnimusForge.PlayerNotorietyBehavior", true);
            h.Patch(host.GetMethod("CanObserverKnowRecentActions", All, null, new[] { typeof(string), typeof(string), typeof(bool) }, null), prefix: new HarmonyMethod(typeof(Program), nameof(Yes)));
            h.Patch(host.GetMethod("GetCurrentGameDayIndex", All), prefix: new HarmonyMethod(typeof(Program), nameof(Day)));
            var owner = Activator.CreateInstance(host);
            var state = host.GetProperty("_state", All).GetValue(owner);
            var recent = (IList)Get(state, "RecentActions");
            var materials = (IList)Get(state, "MajorMaterials");
            var actionType = recent.GetType().GetGenericArguments()[0];
            var materialType = materials.GetType().GetGenericArguments()[0];
            object Action(string kind, string key, string text) {
                var x = Activator.CreateInstance(actionType); Set(x,"Day",100); Set(x,"GameDate","1093 winter 3"); Set(x,"ActionKind",kind); Set(x,"StableKey",key); Set(x,"Text",text); return x;
            }
            recent.Add(Action("debt_recorded", "fixture", "SECRET_CITY_PROMISE"));
            recent.Add(Action("", "reward_action:debt_payment:npc:player", "SECRET_PAYMENT"));
            recent.Add(Action("asset_transfer", "public", "PUBLIC_ACTUAL_TRANSFER"));
            string prompt = (string)host.GetMethod("BuildPlayerRecentRuntimeInstruction",All,null,new[]{typeof(string),typeof(string),typeof(bool),typeof(string)},null).Invoke(owner,new object[]{"observer","empire",false,"Ada"});
            Check(!prompt.Contains("SECRET"), "legacy contract terms excluded from real public request builder");
            Check(prompt.Contains("PUBLIC_ACTUAL_TRANSFER"), "actual public ownership action retained");
            Check(recent.Count == 3, "legacy source records preserved");
            recent = (IList)Get(state, "RecentActions");
            materials = (IList)Get(state, "MajorMaterials");
            var record = host.GetMethod("RecordPlayerAction",All);
            record.Invoke(owner,new object[]{"SECRET_NEW","fixture2","debt_recorded",true,100,"date",1,"","","","","","",null});
            Check(recent.Count == 3 && materials.Count == 0, "new private record blocked before fame/history writes");
            var m = Activator.CreateInstance(materialType); Set(m,"SourceKind","debt_recorded"); Set(m,"StableKey","old"); Set(m,"Text","SECRET_OLD_MATERIAL"); materials.Add(m);
            Check(!(bool)host.GetMethod("HasPendingMajorMaterials",All).Invoke(owner,null), "legacy private materials do not schedule public summary");
            var pub = Activator.CreateInstance(materialType); Set(pub,"SourceKind","battle"); Set(pub,"Text","PUBLIC_BATTLE"); materials.Add(pub);
            Check((bool)host.GetMethod("HasPendingMajorMaterials",All).Invoke(owner,null), "public materials still schedule summary");
            var policy=af.GetType("AnimusForge.ActionHistoryVisibilityPolicy",true).GetMethod("IsPrivateContract",All);
            foreach(var kind in new[]{"debt_recorded"," DEBT_PAYMENT "}) Check((bool)policy.Invoke(null,new object[]{kind,""}),"typed private kind "+kind);
            foreach(var kind in new[]{"asset_transfer","persuasion_defection","battle","major_exchange"}) Check(!(bool)policy.Invoke(null,new object[]{kind,"public"}),"public kind retained "+kind);
            var npcType=af.GetType("AnimusForge.NpcActionEntry",true);
            var list=(IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(npcType));
            var npc=Activator.CreateInstance(npcType); Set(npc,"ActionKind","debt_recorded"); Set(npc,"Text","SECRET_NPC_TERMS"); list.Add(npc);
            var render=af.GetType("AnimusForge.NpcActionRecordOwner",true).GetMethod("RenderEntries",All);
            var result=(string)render.Invoke(null,new object[]{new Func<string>(()=>"NPC"),list,null,new Func<string,string>(x=>x),null});
            Check(result == "" && list.Count == 1,"NPC public projection hides private terms without deleting participant record");
            Console.WriteLine("PASS assertions="+checks+"; candidate="+args[1]+"; resource="+Path.GetFileName(resource)+"; managed fixtures, no live game/save/provider");
            return 0;
        }
        catch(Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
