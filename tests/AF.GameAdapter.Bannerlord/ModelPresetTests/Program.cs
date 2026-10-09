using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using MCM.Common;
using MCM.Implementation;
using BUTR.DependencyInjection.Logger;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge;
using HarmonyLib;

namespace AnimusForge
{
    internal static class Logger { public static void Log(string area,string text) { throw new Exception(area+": "+text); } }
    internal static class AnimusForgeDataPaths
    {
        public static string GetCurrentRoot()=>Path.Combine(Environment.CurrentDirectory,"data");
        public static string GetCacheDirectory(string root)=>Path.Combine(root,"Cache");
        public static void EnsureWritableRoot(string root)=>Directory.CreateDirectory(root);
    }
}
static class Program
{
    static int checks,failures;
    static void Check(bool ok,string label) { checks++; if(!ok)failures++; Console.WriteLine((ok?"PASS ":"FAIL ")+label); }
    static readonly string[] Names={"Main","Auxiliary","ActionPostprocess","EventAndRebellion","TownAmbientAi"};
    static PropertyInfo Text(string n)=>typeof(DuelSettings).GetProperty(n=="Main"?"ModelName":n+"ModelName");
    static PropertyInfo Drop(string n)=>typeof(DuelSettings).GetProperty(n+"ModelDropdown");
    static Dropdown<string> D(DuelSettings s,string n)=>(Dropdown<string>)Drop(n).GetValue(s);
    static string N(DuelSettings s,string n)=>(string)Text(n).GetValue(s);
    static void Select(DuelSettings s,string n,string model) {var d=D(s,n);if(!d.Contains(model))d.Add(model);d.SelectedIndex=d.IndexOf(model);}
    static JsonSerializer Serializer(Func<string,object> existing)=>JsonSerializer.Create(new JsonSerializerSettings {Converters={new DropdownJsonConverter(new DefaultBUTRLogger<DropdownJsonConverter>(),existing)}});
    static JObject Save(DuelSettings s)
    {
        var result=new JObject();var serializer=Serializer(_=>null);
        foreach(var n in Names) {result[Text(n).Name]=N(s,n);result[Drop(n).Name]=JToken.FromObject(D(s,n),serializer);}
        return result;
    }
    static DuelSettings Load(JObject json)
    {
        var s=new DuelSettings();var refs=new Dictionary<string,object>();var serializer=Serializer(path=>refs[path]);
        foreach(var n in Names)
        {
            Text(n).SetValue(s,(string)json[Text(n).Name]);
            var token=json[Drop(n).Name];refs[token.CreateReader().Path]=D(s,n);
            Drop(n).SetValue(s,token.ToObject(typeof(Dropdown<string>),serializer));
        }
        return s;
    }
    static void Cache(bool reversed)
    {
        var j=new JObject();foreach(var n in Names){var key=n=="TownAmbientAi"?"TownAmbientAi":n;j[key+"Options"]=new JArray(reversed?new[]{"other","chosen-"+n,"old-"+n}:new[]{"old-"+n,"other","chosen-"+n});j[key+"Selected"]="old-"+n;}
        Directory.CreateDirectory(AnimusForgeDataPaths.GetCacheDirectory(AnimusForgeDataPaths.GetCurrentRoot()));
        File.WriteAllText(Path.Combine(AnimusForgeDataPaths.GetCacheDirectory(AnimusForgeDataPaths.GetCurrentRoot()),"ModelDropdownCache.json"),j.ToString());
    }
    static int Main()
    {
        Cache(false);var s=new DuelSettings();
        // Main getter hydrates all five fields before AuxiliaryModelName is loaded.
        _=s.MainModelDropdown;
        foreach(var n in Names)Select(s,n,"chosen-"+n);
        var json=Save(s);
        foreach(var n in Names)Check((string)json[Text(n).Name]=="chosen-"+n,n+" saves actual in-place selection");
        var restored=Load(json);
        foreach(var n in Names)Check(N(restored,n)=="chosen-"+n&&D(restored,n).SelectedValue=="chosen-"+n,n+" preset roundtrip");
        Cache(true);restored=Load(json);
        foreach(var n in Names)Check(D(restored,n).SelectedValue=="chosen-"+n,n+" restored after catalog reorder");
        string cachePath=Path.Combine(AnimusForgeDataPaths.GetCacheDirectory(AnimusForgeDataPaths.GetCurrentRoot()),"ModelDropdownCache.json");
        string before=File.ReadAllText(cachePath);
        var defaults=new DuelSettings();_=Save(defaults);_=Load(json);
        Check(before==File.ReadAllText(cachePath),"reading/loading presets does not write shared cache");
        Check(defaults.AuxiliaryModelName=="gpt-4o-mini","default preset independent of cached selected model");
        foreach(var n in Names)
        {
            Text(n).SetValue(s,"manual-"+n);D(s,n).SelectedIndex=0;
        }
        var manual=Load(Save(s));
        foreach(var n in Names)Check(D(manual,n).SelectedIndex==0&&N(manual,n)=="manual-"+n,n+" manual text and sentinel roundtrip");
        foreach(var n in Names)
        {
            Select(restored,n,"chosen-"+n);
            Check(N(restored,n)=="chosen-"+n,n+" terminal/UI in-place choice drives model name");
            Check(ReferenceEquals(D(restored,n),D(restored,n)),n+" repeated getters reuse dropdown");
            var property=new PropertyRef(Drop(n),restored);
            object incoming=new Dropdown<string>(new[]{"*手动填写*","preset-only"},1);
            var original=D(restored,n);var old=original.ToArray();
            McmDropdownRuntimeRefresh.ModelPresetIndexPrefix(property,ref incoming);
            original.SelectedIndex=((Dropdown<string>)incoming).SelectedIndex;
            Check(D(restored,n).SelectedValue=="preset-only",n+" UI preset action maps name across differing catalogs");
            Check(old.SequenceEqual(original.Take(old.Length)),n+" preset mapping preserves old undo indices");
        }
        Check(defaults.ActionPostprocessModelName==""&&defaults.EventAndRebellionModelName=="","blank fallback defaults preserved");
        defaults.ModelName="gpt-4o";
        Check(defaults.MainModelDropdown.SelectedIndex==0&&defaults.ModelName=="gpt-4o","retired main preset remains available through manual mode");
        var missing=(JObject)json.DeepClone();missing["AuxiliaryModelDropdown"]=9999;
        Check(Load(missing).GetEffectiveAuxiliaryModelName()=="chosen-Auxiliary","out-of-range persisted index retains configured name");
        // Exercise the real MCM action constructor and Harmony argument binding.
        string uiPath=Directory.GetFiles(AppContext.BaseDirectory,"Bannerlord.MBOptionScreen.v*.dll").Single();
        var actionType=Assembly.LoadFrom(uiPath).GetType("MCM.UI.Actions.SetSelectedIndexAction",true);
        var ctor=actionType.GetConstructor(new[]{typeof(IRef),typeof(object)});
        new Harmony("AnimusForge.tests.model-presets").Patch(ctor,prefix:new HarmonyMethod(typeof(McmDropdownRuntimeRefresh).GetMethod("ModelPresetIndexPrefix",BindingFlags.Static|BindingFlags.NonPublic)));
        var live=new DuelSettings();Select(live,"Auxiliary","live-choice");
        var refValue=new PropertyRef(Drop("Auxiliary"),live);int oldIndex=D(live,"Auxiliary").SelectedIndex;
        var action=ctor.Invoke(new object[]{refValue,new Dropdown<string>(new[]{"*手动填写*","actual-action-choice"},1)});
        actionType.GetMethod("DoAction").Invoke(action,null);
        Check(live.GetEffectiveAuxiliaryModelName()=="actual-action-choice","real patched MCM action selects preset name");
        actionType.GetMethod("UndoAction").Invoke(action,null);
        Check(D(live,"Auxiliary").SelectedIndex==oldIndex&&live.GetEffectiveAuxiliaryModelName()=="live-choice","real MCM action undo preserves previous model");
        Console.WriteLine($"{checks-failures}/{checks} passed");return failures==0?0:1;
    }
}
