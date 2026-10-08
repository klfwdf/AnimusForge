using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BUTR.DependencyInjection;
using HarmonyLib;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Properties;
using Newtonsoft.Json.Linq;

// Real MCM discovery, JSON conversion and Harmony hooks. UI and provider disk
// boundary are replaced; only explicitly supplied test-directory files are used.
public static class PromptSettingsPersistenceAudit
{
    private static readonly BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static int checks;
    private static object format;
    private static Type settingsType;
    private static MethodInfo saveJson, loadJson;
    private static Provider provider;
    private static Action<string> editorSave;
    private static string editorInitial;
    private static readonly string[] Names = { "CustomStylePrompt", "NegativePrompt", "CustomDirectorPrompt" };
    private static void Check(bool ok,string text) { if(!ok) throw new Exception("FAIL " + text); checks++; }
    private static void Set(object obj,string name,object value) { obj.GetType().GetProperty(name).SetValue(obj,value,null); }
    private static string Get(object obj,string name) { return (string)obj.GetType().GetProperty(name).GetValue(obj,null); }
    private static BaseSettings New() { return (BaseSettings)Activator.CreateInstance(settingsType); }
    private static string Serialize(BaseSettings value) { return (string)saveJson.Invoke(format,new object[]{value}); }
    private static void Load(BaseSettings value,string json) { loadJson.Invoke(format,new object[]{value,json}); }
    public static bool SkipStorage() { return false; }
    public static bool CaptureEditor(object[] __args)
    { editorInitial=(string)__args[3]; editorSave=(Action<string>)__args[4]; return false; }
    public static void Run(string assemblyPath,string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        Assembly af=Assembly.LoadFrom(assemblyPath);
        settingsType=af.GetType("AnimusForge.Illustrator.IllustratorSettings",true);
        Type storage=af.GetType("AnimusForge.Illustrator.IllustratorSettingsStorage",true);
        Type persistence=af.GetType("AnimusForge.Illustrator.IllustratorPromptSettingsPersistence",true);
        var harmony=new Harmony("AnimusForge.audit.prompt-settings");
        harmony.Patch(AccessTools.Method(storage,"EnsureReady"),prefix:new HarmonyMethod(typeof(PromptSettingsPersistenceAudit),"SkipStorage"));
        harmony.Patch(AccessTools.Method(af.GetType("AnimusForge.DevTextEditorHelper",true),"ShowLongTextEditor"),prefix:new HarmonyMethod(typeof(PromptSettingsPersistenceAudit),"CaptureEditor"));
        Type formatType=typeof(BaseSettings).Assembly.GetType("MCM.Implementation.JsonSettingsFormat",true);
        Type baseFormat=formatType.BaseType;
        persistence.GetMethod("Install",AnyStatic).Invoke(null,new object[]{harmony,baseFormat});
        format=Activator.CreateInstance(formatType,new object[]{null});
        saveJson=baseFormat.GetMethod("SaveJson"); loadJson=baseFormat.GetMethod("LoadFromJson");
        typeof(GenericServiceProvider).GetField("GlobalServiceProvider",AnyStatic).SetValue(null,new Services());
        provider=new Provider(Path.Combine(outputDirectory,"settings.json"));
        typeof(BaseSettingsProvider).GetProperty("Instance").GetSetMethod(true).Invoke(null,new object[]{provider});
        BaseSettings original=New(); provider.Current=original;
        foreach(string name in Names)
            Check(!original.GetAllSettingPropertyDefinitions().Any(x=>x.Id==name),name+" remains editor-only");
        Set(original,"ApiBaseUrl","https://offline.invalid/v1");
        foreach(string name in Names) Set(original,name,"初始\n"+name+"\n\"引号\" \\ 路径 😀");
        foreach(string name in Names)
        {
            BaseSettings page=original.CopyAsNew();
            Check(Get(page,name)==Get(original,name),"MCM reopened copy preserves "+name);
            string actionName=name=="CustomStylePrompt"?"EditCustomStylePrompt":name=="NegativePrompt"?"EditNegativePrompt":"EditCustomDirectorPrompt";
            ((Action)page.GetType().GetProperty(actionName).GetValue(page,null))();
            Check(editorInitial==Get(original,name),"editor opens saved text "+name);
            int before=provider.Saves;
            string text="更新\n"+name+"\n水彩 \"文本\" \\ 路径 😀";
            editorSave(text);
            Check(provider.Saves==before+1,"editor save immediately persists "+name);
            Check(Get(original,name)==text && Get(page,name)==text,"editor updates live and page "+name);
            BaseSettings restarted=New(); Load(restarted,File.ReadAllText(provider.Path));
            Check(Get(restarted,name)==text,"disk restart round trip "+name);
            SettingsUtils.OverrideSettings(original,page);
            Check(Get(original,name)==text,"MCM apply does not revert "+name);
        }
        string json=Serialize(original);
        Check((string)JObject.Parse(json)["ApiBaseUrl"]=="https://offline.invalid/v1","normal API field preserved");
        BaseSettings reloaded=New(); Load(reloaded,json);
        foreach(string name in Names) Check(Get(reloaded,name)==Get(original,name),"all hidden text reloads "+name);
        BaseSettings legacy=New(); string oldStyle=Get(legacy,"CustomStylePrompt");
        Load(legacy,"{\"ApiBaseUrl\":\"https://legacy.invalid\"}");
        Check(Get(legacy,"CustomStylePrompt")==oldStyle,"missing legacy field keeps default");
        Check(Get(legacy,"CustomDirectorPrompt")=="","missing director defaults empty");
        foreach(string name in Names) Set(original,name,"");
        Load(reloaded,Serialize(original));
        foreach(string name in Names) Check(Get(reloaded,name)=="","explicit empty persists "+name);
        Load(reloaded,"{\"CustomStylePrompt\":12,\"NegativePrompt\":null,\"CustomDirectorPrompt\":[]}");
        foreach(string name in Names) Check(Get(reloaded,name)=="","invalid type does not replace "+name);
        Set(original,"CustomStylePrompt",new string('汉',24000)+"\nTAIL");
        Load(reloaded,Serialize(original));
        Check(Get(reloaded,"CustomStylePrompt")==Get(original,"CustomStylePrompt"),"long disk JSON not truncated");
        BaseSettings defaults=New(); SettingsUtils.OverrideSettings(original,defaults);
        foreach(string name in Names) Check(Get(original,name)==Get(defaults,name),"reset copies default "+name);
        var other=new OtherSettings();
        string otherJson=Serialize(other);
        Check(!Names.Any(n=>JObject.Parse(otherJson)[n]!=null),"other MCM settings remain untouched");
        Console.WriteLine("PASS "+checks+" real MCM/Harmony prompt persistence checks; editor/native file provider boundaries are fixtures.");
    }
    private sealed class Services : IGenericServiceProvider
    {
        private readonly ISettingsPropertyDiscoverer discoverer=(ISettingsPropertyDiscoverer)Activator.CreateInstance(typeof(BaseSettings).Assembly.GetType("MCM.Implementation.AttributeSettingsPropertyDiscoverer",true),true);
        public TService GetService<TService>() where TService:class
        { return typeof(TService)==typeof(IEnumerable<ISettingsPropertyDiscoverer>) ? new[]{discoverer} as TService : null; }
        public IGenericServiceProviderScope CreateScope() { return null; }
        public void Dispose() {}
    }
    private sealed class OtherSettings : BaseSettings
    { public override string Id {get{return "other";}} public override string DisplayName {get{return "other";}} }
    private sealed class Provider : BaseSettingsProvider
    {
        internal readonly string Path; internal BaseSettings Current; internal int Saves;
        internal Provider(string path){Path=path;}
        public override BaseSettings GetSettings(string id){return Current;}
        public override void SaveSettings(BaseSettings settings){ File.WriteAllText(Path,Serialize(settings)); Saves++; }
        public override void ResetSettings(BaseSettings settings){}
        public override void OverrideSettings(BaseSettings settings){}
        public override IEnumerable<SettingsDefinition> SettingsDefinitions {get{return new SettingsDefinition[0];}}
        public override IEnumerable<ISettingsPreset> GetPresets(string id){return new ISettingsPreset[0];}
        public override IEnumerable<UnavailableSetting> GetUnavailableSettings(){return new UnavailableSetting[0];}
        public override IEnumerable<SettingSnapshot> SaveAvailableSnapshots(){return new SettingSnapshot[0];}
        public override IEnumerable<BaseSettings> LoadAvailableSnapshots(IEnumerable<SettingSnapshot> snapshots){return new BaseSettings[0];}
    }
}
