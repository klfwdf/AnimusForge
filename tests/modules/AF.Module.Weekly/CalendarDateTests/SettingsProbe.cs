using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;

// Real compiled calendar and archive UI projection; no Campaign/native UI/provider.
internal static class CalendarDateProbe
{
    const BindingFlags All = BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
    static int checks;
    static void Check(bool ok,string message) {if(!ok)throw new Exception("FAIL "+message);checks++;Console.WriteLine("PASS "+message);}
    static void Set(object target,string name,object value)
    {
        var field=target.GetType().GetField(name,All);
        if(value is Delegate callback && !field.FieldType.IsInstanceOfType(value))
            value=Delegate.CreateDelegate(field.FieldType,callback.Target,callback.Method);
        field.SetValue(target,value);
    }
    static object Get(object target,string name) => target.GetType().GetField(name,All).GetValue(target);
    static int Main(string[] args)
    {
        try {
            Console.OutputEncoding=new System.Text.UTF8Encoding(false);
            AppDomain.CurrentDomain.AssemblyResolve += (_,e) => {
                string name=new AssemblyName(e.Name).Name;
                if(name=="AnimusForge")return Assembly.LoadFrom(args[0]);
                foreach(string dir in args.Skip(2)){string path=Path.Combine(dir,name+".dll");if(File.Exists(path))return Assembly.LoadFrom(path);}
                return null;
            };
            var af=Assembly.LoadFrom(args[0]);
            var localization=Assembly.Load("TaleWorlds.Localization");
            localization.GetType("TaleWorlds.Localization.LocalizedTextManager",true).GetMethod("LoadLocalizationXmls").Invoke(null,new object[]{new[]{args[1]}});
            var policy=af.GetType("AnimusForge.WeeklyReportArchivePolicy",true);
            string Calendar(string text)=>(string)policy.GetMethod("CalendarDate",All).Invoke(null,new object[]{text});
            string Display(string text)=>(string)policy.GetMethod("ProjectDateForDisplay",All).Invoke(null,new object[]{text});
            foreach(string language in new[]{"English","简体中文"}) {
                localization.GetType("TaleWorlds.Localization.MBTextManager",true).GetMethod("ChangeLanguage").Invoke(null,new object[]{language});
                string edition=language=="English"?"EN":"CN";
                Check(Calendar("1 八月 公元前218年")=="1 八月 公元前218年","third-party BCE date preserved "+edition);
                Check(Calendar("5 August, 218 BC")=="5 August, 218 BC","no added English world name "+edition);
                Check(Display("卡拉迪亚1 八月 公元前218年 — 卡拉迪亚5 八月 公元前218年")=="1 八月 公元前218年 — 5 八月 公元前218年","legacy CN range label removed "+edition);
                Check(Display("Calradia 1 August 218 BC — Calradia 5 August 218 BC")=="1 August 218 BC — 5 August 218 BC","legacy EN range label removed "+edition);
                Check(Display("  Custom calendar era X  ")=="  Custom calendar era X  ","unknown date bytes preserved "+edition);
                Check(Calendar("  ").Length>0,"empty calendar date keeps localized fallback "+edition);
                object Make(string name)=>Activator.CreateInstance(af.GetType("AnimusForge."+name,true),true);
                object port=Make("WeeklyEditorDisplayPort");
                Set(port,"FormatNewsDate",(Func<int,string>)(day=>Calendar(day+" 八月 公元前218年")));
                Set(port,"ResolveKingdomDisplay",(Func<string,string>)(id=>"马其顿王国"));
                object record=Make("MyBehavior+EventRecordEntry");
                Set(record,"EventId","weekly_report:kingdom:recent:1:macedon");Set(record,"EventKind","kingdom");Set(record,"ScopeKingdomId","macedon");Set(record,"WeekIndex",1);Set(record,"CreatedDay",5);
                Set(record,"Title","stored title");Set(record,"Summary","正文提到卡拉迪亚，不改写");
                const string stored="卡拉迪亚1 八月 公元前218年 — 卡拉迪亚5 八月 公元前218年";
                Set(record,"CreatedDate",stored);
                var materials=(IList)Get(record,"Materials");
                foreach(int day in new[]{1,5}) {var material=Make("MyBehavior+EventMaterialReference");Set(material,"ActionDay",day);Set(material,"Label","卡拉迪亚"+day+" 八月 公元前218年");materials.Add(material);}
                var records=(IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(record.GetType()));records.Add(record);
                var build=af.GetType("AnimusForge.WeeklyEditorProjection",true).GetMethod("BuildWeeklyReportBrowserEntries",All);
                object Entry()=>((IEnumerable)build.Invoke(null,new object[]{port,records,"kingdom","macedon",null})).Cast<object>().Single();
                object entry=Entry();
                Check(!((string)Get(entry,"Title")).Contains("卡拉迪亚") && (string)Get(entry,"CreatedDate")=="1 八月 公元前218年 — 5 八月 公元前218年","actual recent title and metadata use mod calendar "+edition);
                Check(((string)Get(entry,"BodyText")).Contains("卡拉迪亚") && (string)Get(record,"CreatedDate")==stored,"body and original archive remain intact "+edition);
                Set(materials[0],"ActionDay",null);entry=Entry();
                Check(!((string)Get(entry,"Title")).Contains("卡拉迪亚") && !((string)Get(entry,"CreatedDate")).Contains("卡拉迪亚"),"legacy fallback title and metadata cleaned "+edition);
                // Real RefreshRecentText: legacy material Labels (old saves) are date fields and lose
                // the CN/EN world prefix in both CreatedDate and the Summary body; snapshot prose stays intact.
                object recent=Make("MyBehavior+EventRecordEntry");
                var recentMaterials=(IList)Get(recent,"Materials");
                foreach(var pair in new[]{(1,"卡拉迪亚1 八月 公元前218年"),(5,"Calradia 5 August 218 BC")}) {
                    var material=Make("MyBehavior+EventMaterialReference");Set(material,"ActionDay",pair.Item1);Set(material,"Label",pair.Item2);
                    Set(material,"SnapshotText","卡拉迪亚的消息 "+pair.Item1);recentMaterials.Add(material);
                }
                policy.GetMethod("RefreshRecentText",All).Invoke(null,new object[]{recent});
                string summary=(string)Get(recent,"Summary");
                Check(summary.StartsWith("1 八月 公元前218年\n",StringComparison.Ordinal) && summary.Contains("\n\n5 August 218 BC\n") && !summary.Contains("Calradia "),"RefreshRecentText summary date lines drop legacy label "+edition+"; actual="+summary);
                Check(summary.Contains("卡拉迪亚的消息 1") && summary.Contains("卡拉迪亚的消息 5"),"RefreshRecentText keeps snapshot prose "+edition);
                Check((string)Get(recent,"CreatedDate")=="1 八月 公元前218年 — 5 August 218 BC","RefreshRecentText CreatedDate drops legacy label "+edition);
            }
            Console.WriteLine("Checks="+checks+"; real compiled calendar/UI projection; no game/save/provider");return 0;
        }catch(Exception ex){Console.WriteLine(ex);return 1;}
    }
}
