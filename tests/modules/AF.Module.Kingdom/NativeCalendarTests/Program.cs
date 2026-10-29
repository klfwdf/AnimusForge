using System;
using System.IO;
using System.Reflection;
class Program
{
 static int Main(string[] args)
 {
  AppDomain.CurrentDomain.AssemblyResolve += (_, e) => {
   string p = Path.Combine(args[0], new AssemblyName(e.Name).Name + ".dll");
   return File.Exists(p) ? Assembly.LoadFrom(p) : null;
  };
  try {
   var candidate = Assembly.LoadFrom(args[1]);
   var helper = candidate.GetType("AnimusForge.CivilWarUiText", true).GetMethod("Date", BindingFlags.Static | BindingFlags.NonPublic);
   var ct = Assembly.LoadFrom(Path.Combine(args[0], "TaleWorlds.CampaignSystem.dll")).GetType("TaleWorlds.CampaignSystem.CampaignTime", true);
   // Fixture campaign clock and game text registration only; production calendar methods are unchanged.
   Action<string, object> set = (n,v) => ct.GetField(n, BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).SetValue(null,v);
   set("DaysInWeek",7); set("WeeksInSeason",3); set("SeasonsInYear",4);
   long tick=10;
   foreach(var pair in new[] { new {N="TimeTicksPerMillisecond",M=1L},new {N="TimeTicksPerSecond",M=1000L},new {N="TimeTicksPerMinute",M=60L},new {N="TimeTicksPerHour",M=60L},new {N="TimeTicksPerDay",M=24L},new {N="TimeTicksPerWeek",M=7L},new {N="TimeTicksPerSeason",M=3L},new {N="TimeTicksPerYear",M=4L} }) { tick*=pair.M; set(pair.N,tick); }
   var core=Assembly.LoadFrom(Path.Combine(args[0],"TaleWorlds.Core.dll"));
   var managerType=core.GetType("TaleWorlds.Core.GameTextManager"); var manager=Activator.CreateInstance(managerType);
   var textType=Assembly.LoadFrom(Path.Combine(args[0],"TaleWorlds.Localization.dll")).GetType("TaleWorlds.Localization.TextObject");
   Action<string,string> add=(id,value)=> {
    object text=Activator.CreateInstance(textType,new object[]{value,null});
    var gameText=managerType.GetMethod("AddGameText").Invoke(manager,new object[]{id});
    var addMethod=gameText.GetType().GetMethod("AddVariationWithId");
    addMethod.Invoke(gameText,new object[]{"",text,Activator.CreateInstance(addMethod.GetParameters()[2].ParameterType)});
   };
   add("str_date_format","Year {YEAR}, {SEASON} {DAY}");
   foreach(var season in new[]{"Spring","Summer","Autumn","Winter"}) add("str_season_"+season,season);
   core.GetType("TaleWorlds.Core.GameTexts").GetField("_gameTextManager",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,manager);
   long ticksPerDay=(long)ct.GetField("TimeTicksPerDay",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).GetValue(null);
   int count=0;
   foreach (int day in new[] { 0, 20, 21, 83, 84, 91572, 91584 }) {
    object native = Activator.CreateInstance(ct,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{(long)day*ticksPerDay},null);
    string expected = native.ToString();
    string actual = (string)helper.Invoke(null, new object[] { day });
    if (day == 91572 && actual != "Year 1090, Spring 13") throw new Exception("Native epoch mismatch: " + actual);
    if (actual != expected || string.IsNullOrWhiteSpace(actual) || actual == day.ToString()) throw new Exception("Calendar mismatch: " + day + " -> " + actual + " expected " + expected);
    Console.WriteLine(day + " -> " + actual); count++;
   }
   Console.WriteLine("PASS " + count + " calendar projections against native CampaignTime; no live game."); return 0;
  } catch (Exception e) { Console.WriteLine(e); return 1; }
 }
}
