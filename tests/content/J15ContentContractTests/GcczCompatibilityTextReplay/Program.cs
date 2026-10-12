using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AnimusForge.SiegeAftermathIntervention;
#if NET8_0_OR_GREATER
using System.Text.Json;
#else
using Newtonsoft.Json;
#endif

internal static class Program
{
    static int checks, negativeControls;
    static readonly List<object> evidence = new List<object>();
    static void Check(bool value,string label) { checks++; if(!value)throw new Exception(label); }
    static string Serialize(object value) {
#if NET8_0_OR_GREATER
        return JsonSerializer.Serialize(value,new JsonSerializerOptions{WriteIndented=true});
#else
        return JsonConvert.SerializeObject(value,Formatting.Indented);
#endif
    }
    static T Read<T>(string text) {
#if NET8_0_OR_GREATER
        return JsonSerializer.Deserialize<T>(text);
#else
        return JsonConvert.DeserializeObject<T>(text);
#endif
    }
    static Dictionary<string,string> Parse(Type type,string text) => (Dictionary<string,string>)type.GetMethod("Parse",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{text});
    static object Normalize(object value) {
        if(value==null)return null;
        if(value is string || value is char || value is bool || value.GetType().IsPrimitive)return value;
        if(value.GetType().IsEnum)return Convert.ToInt32(value);
        if(value is Regex regex)return new {pattern=regex.ToString(),options=(int)regex.Options};
        if(value is IEnumerable sequence)return sequence.Cast<object>().Select(Normalize).ToArray();
        return value.GetType().GetProperties(BindingFlags.Public|BindingFlags.Instance).Where(p=>p.GetIndexParameters().Length==0).OrderBy(p=>p.Name).ToDictionary(p=>p.Name,p=>Normalize(p.GetValue(value)));
    }
    static void Same(object a,object b,string label) => Check(Serialize(Normalize(a))==Serialize(Normalize(b)),label);
    static void JsonChecks(string folder) {
        int rows=0;
        foreach(var type in new[]{typeof(CastleAftermathCompatibilityTexts),typeof(SiegeAftermathCompatibilityTexts)}) {
            Type old=typeof(DeliveredBefore.CastleAftermathCompatibilityTexts).Assembly.GetType("DeliveredBefore."+type.Name);
            var expected=Read<Dictionary<string,string>>(File.ReadAllText(Path.Combine(folder,type.Name.Replace("Texts","")+".json")));
            var actual=Parse(type,Serialize(expected)); rows+=expected.Count;
            Check(expected.Count==actual.Count,"Actual resource row count");
            foreach(var pair in expected)Check(actual.TryGetValue(pair.Key,out string v)&&v==pair.Value,"Actual resource value "+pair.Key);
            var first=expected.First();string key="\\u"+((int)first.Key[0]).ToString("x4")+first.Key.Substring(1);
            string encoded="{\""+key+"\":"+Serialize(first.Value)+"}";
            Check(Parse(type,encoded)[first.Key]==first.Value,"Unicode-encoded real key");
            Check(!Parse(old,encoded).ContainsKey(first.Key),"Old loader Unicode-key negative control");negativeControls++;
            foreach(string value in new[]{"literal\\n","literal\\r","\\d+\\n","trailing\\","quote \" slash \\ end","$1/{name}","\b\f\n\r\t/","\uD83D\uDE00"}) {
                string json=Serialize(new Dictionary<string,string>{{"value",value}});
                Check(Parse(type,json)["value"]==value,"Single-pass escape decode");
                if(value=="literal\\n"||value=="literal\\r"||value=="\\d+\\n") { Check(Parse(old,json)["value"]!=value,"Old loader double-escape negative control");negativeControls++; }
            }
            Check(Parse(type,"{\"x\":\"a\",\"x\":\"b\"}")["x"]=="b","Duplicate last wins");
            Check(Parse(type,"{\"x\":\"\\uD83D\\uDE00\"}")["x"]=="\uD83D\uDE00","Escaped surrogate pair");
            for(int i=0;i<=0xffff;i++) {
                if(i>=0xd800&&i<=0xdfff)continue;
                string json="{\"v\":\"\\u"+i.ToString("x4")+"\"}";
                Check(Parse(type,json)["v"]==((char)i).ToString(),"Standard Unicode code unit "+i);
            }
            foreach(string invalid in new[]{"{","[]","{\"x\":null}","{\"x\":2}","{\"x\":\"\\q\"}","{\"x\":\"\\u123\"}","{\"x\":\"\\u12zz\"}","{\"x\":\"a\",}","{\"x\":\"a\"}garbage","{\"x\":\"a\n\"}"}) {
                bool failed=false;try{Parse(type,invalid);}catch(TargetInvocationException ex){failed=ex.InnerException is FormatException;}
                Check(failed,"Malformed flat JSON rejected");
            }
            Check(Parse(type,null).Count==0&&Parse(type,"").Count==0&&Parse(type," \r\n {}\t").Count==0,"Empty legacy contract");
            var cached=type.GetField("Cached",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);var valueProperty=cached.GetType().GetProperty("Value");
            object dictionary=valueProperty.GetValue(cached);Check(ReferenceEquals(dictionary,valueProperty.GetValue(cached)),"Lazy catalog same cached instance");
            string raw=(string)type.GetMethod("Raw").Invoke(null,new object[]{first.Key});Check(raw==first.Value,"Actual production Raw cache lookup");
            var timer=Stopwatch.StartNew();for(int i=0;i<100000;i++)Check((string)type.GetMethod("Raw").Invoke(null,new object[]{first.Key})==raw,"Cached lookup repeat");timer.Stop();
            evidence.Add(new {kind="cached-lookup",type=type.Name,lookups=100000,elapsedMs=timer.Elapsed.TotalMilliseconds});
        }
        evidence.Add(new{kind="resources",rows});
        var pattern=CastleAftermathCompatibilityTexts.Raw("letter_queued_pattern");
        Check(new Regex(pattern).Match("引见信已写好，将在 3 天后直接送达族长 Name{actor}/$1；不会生成AF信使部队，当前领主仍保持俘虏身份。").Groups["recipient"].Value=="Name{actor}/$1","Real regex dynamic recipient");
    }
    static bool Scalar(Type type) { if(type.IsByRef)type=type.GetElementType();return type.IsPrimitive||type.IsEnum||type==typeof(string); }
    static object Argument(Type type,string text,int variant,int index) {
        if(type.IsByRef)type=type.GetElementType();if(type==typeof(string))return text;
        if(type.IsEnum){var values=Enum.GetValues(type);return values.GetValue(variant%values.Length);}
        if(type==typeof(bool))return ((variant>>(index%3))&1)!=0;
        if(type==typeof(char))return 'x';
        int[] n={0,1,-1,50,999,10000};return Convert.ChangeType(n[variant%n.Length],type,CultureInfo.InvariantCulture);
    }
    static void MigrationChecks(string folder) {
        var texts=Read<string[]>(File.ReadAllText(Path.Combine(folder,"migration-values.json"))).Concat(new[]{null,""," ","Unknown{action}/{penalty}/$1","recruit prisoners","do not recruit prisoners","release everyone","start over","should we sell prisoners?"}).Distinct().ToArray();
        string[] names={"SiegeCastlePlayerAuthorizationPolicy","SiegeCastleSoldierAppeasementAuthorizationPolicy","SiegeCastlePostprocessContextProfile","SiegeCastleActionTagCatalog","SiegeCastlePrisonerAllocationProfile","SiegeCastlePostprocessRuleCatalog","VillageAftermathActionTagCatalog","SiegeCastleLordDuelProfile","LegacyTownTagAdapter","SiegeCastleLordRecruitmentBranchProfile","SiegeInterventionMemoryContextBuilder","SiegePostprocessRuleCatalog"};
        var covered=new List<string>();var skipped=new List<string>();
        foreach(string name in names) {
            Type current=typeof(CastleAftermathCompatibilityTexts).Assembly.GetType("AnimusForge.SiegeAftermathIntervention."+name);
            Type old=current.Assembly.GetType("DeliveredBefore."+name);
            foreach(var field in old.GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static)) {
                var newField=current.GetField(field.Name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
                if(newField!=null)Same(newField.GetValue(null),field.GetValue(null),name+" cached table/field "+field.Name);
            }
            foreach(var method in current.GetMethods(BindingFlags.Public|BindingFlags.Static|BindingFlags.DeclaredOnly)) {
                var parameters=method.GetParameters();
                if(!parameters.All(p=>Scalar(p.ParameterType))){skipped.Add(name+"."+method.Name);continue;}
                var oldMethod=old.GetMethods(BindingFlags.Public|BindingFlags.Static).Single(m=>m.Name==method.Name&&m.GetParameters().Length==parameters.Length&&m.GetParameters().Select(p=>p.ParameterType.Name).SequenceEqual(parameters.Select(p=>p.ParameterType.Name)));
                foreach(string text in parameters.Any(p=>p.ParameterType==typeof(string))?texts:new[]{""})for(int v=0;v<8;v++) {
                    object[] args=parameters.Select((p,i)=>Argument(p.ParameterType,text,v,i)).ToArray();object[] oldArgs=oldMethod.GetParameters().Select((p,i)=>Argument(p.ParameterType,text,v,i)).ToArray();
                    Same(method.Invoke(null,args),oldMethod.Invoke(null,oldArgs),name+"."+method.Name+" result");
                    Same(args,oldArgs,name+"."+method.Name+" out parameter/order");
                }
                covered.Add(name+"."+method.Name);
            }
        }
        for(int i=1;i<100;i++) {
            string[] digits={"零","一","二","三","四","五","六","七","八","九"};
            string count=i<10?digits[i]:(i/10==1?"十":digits[i/10]+"十")+(i%10==0?"":digits[i%10]);
            Check(SiegeCastlePrisonerAllocationProfile.TryParseExplicitCount(count+"名战俘",out int actual)&&actual==i,"Chinese count "+i);
        }
        foreach(string count in new[]{"两","二","〇二","一百","一百零一","九百九十九"})Same(SiegeCastlePrisonerAllocationProfile.Resolve(count+"名战俘",1000,.5),DeliveredBefore.SiegeCastlePrisonerAllocationProfile.Resolve(count+"名战俘",1000,.5),"Alternative digit/unit count");
        evidence.Add(new{kind="migration-behavior",textInputs=texts.Length,covered,skipped});
    }
    static void CompoundChecks() {
        foreach(string separator in new[]{"，",",","；",";","。","！","!","\r\n","然后","随后","再把","再将","再让"}) {
            string command="50名战俘收编"+separator+"其余释放";
            bool now=SiegeCastleCompoundDispositionPlanProfile.TryBuild(command,out var plan),prior=GitBaselineCompoundProfile.TryBuild(command,out var old);
            Check(now&&prior&&plan.Steps.Count==2,"CN baseline compound separator "+separator);Same(plan,old,"CN baseline compound exact step order");
        }
        foreach(string command in new[]{"50名强壮战俘修缮城堡，其余卖掉","50名战俘收编其余释放"}) {
            Check(SiegeCastleCompoundDispositionPlanProfile.TryBuild(command,out var p)&&p.Steps.Count==2,"Recovered CN remainder");
            Check(!DeliveredBefore.SiegeCastleCompoundDispositionPlanProfile.TryBuild(command,out _),"Delivered compound negative control");negativeControls++;
        }
        foreach(string command in new[]{"50 prisoners recruit, release the rest","recruit 50 prisoners and release the rest","recruit 50 prisoners then release the rest","recruit 50 prisoners and then release the rest"}) {
            Check(SiegeCastleCompoundDispositionPlanProfile.TryBuild(command,out var p)&&p.Steps.Count==2,"Retained English compound path");
        }
        foreach(string command in new[]{null,"","收编50名战俘","收编50名战俘再收编其余","收编50名战俘然后释放","收编50名战俘其余释放其余卖掉","反悔收编50名战俘其余释放","要不要收编50名战俘其余释放"})Check(!SiegeCastleCompoundDispositionPlanProfile.TryBuild(command,out _),"Compound fail-closed guard");
    }
    static SiegeCastleCompletedInterventionSummaryFacts Facts(string name,int penalty,bool required,bool applied,SiegeCastleActionKind action=SiegeCastleActionKind.SellPrisoners,int count=3,int gold=99) => new SiegeCastleCompletedInterventionSummaryFacts("Castle{player}/{gold}/$1",name,new[]{new SiegeCastleDispositionSummaryEntry(action,count,gold)},2,1,1,-2,3,4,5,6,7,required,applied,penalty,SiegeCastleActionKind.RecruitPrisonersForced,true,true,"Lord{action}/$1",6.25f);
    static DeliveredBefore.SiegeCastleCompletedInterventionSummaryFacts OldFacts(string name,int penalty,bool required,bool applied,SiegeCastleActionKind action=SiegeCastleActionKind.SellPrisoners,int count=3,int gold=99) => new DeliveredBefore.SiegeCastleCompletedInterventionSummaryFacts("Castle{player}/{gold}/$1",name,new[]{new DeliveredBefore.SiegeCastleDispositionSummaryEntry((DeliveredBefore.SiegeCastleActionKind)(int)action,count,gold)},2,1,1,-2,3,4,5,6,7,required,applied,penalty,DeliveredBefore.SiegeCastleActionKind.RecruitPrisonersForced,true,true,"Lord{action}/$1",6.25f);
    static void SummaryChecks() {
        foreach(string locale in new[]{"en-US","zh-CN","fr-FR"}) {
            CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(locale);
            foreach(string name in new[]{null,""," ","Ordinary Player","P{action}/{penalty}/{player}/{unknown}/$1"})foreach(int penalty in new[]{0,30,90})foreach(bool required in new[]{false,true})foreach(bool applied in new[]{false,true}) {
                var f=Facts(name,penalty,required,applied);string cn=SiegeCastleCompletedInterventionSummaryBuilder.Build(f,false);
                string git=GitBaselineCastleSummary.Build(f);
                if(cn!=git)File.WriteAllText("summary-difference.json",Serialize(new{locale,name,penalty,required,applied,current=cn,baseline=git}),new UTF8Encoding(false));
                Check(cn==git,"CN summary exact a911 identity "+locale+" name="+name+" penalty="+penalty+" required="+required+" applied="+applied);
                foreach(bool english in new[]{false,true}) {
                    string now=SiegeCastleCompletedInterventionSummaryBuilder.Build(f,english),old=DeliveredBefore.SiegeCastleCompletedInterventionSummaryBuilder.Build(OldFacts(name,penalty,required,applied),english);
                    if(!english) {
                        old=old.Replace("（99 第纳尔）","（+99 第纳尔）");
                        if(penalty>0)old=old.Replace("军心：因","军心：因 ");
                    }
                    if(name!=null&&name.StartsWith("P{")&&!english&&penalty>0) {
                        string nowMorale=now.Split('\n').First(x=>x.StartsWith("军心：")),oldMorale=old.Split('\n').First(x=>x.StartsWith("军心："));
                        Check(now!=old&&nowMorale.Contains(name)&&!oldMorale.Contains(name),"Real summary morale-line name negative control");negativeControls++;
                    }
                    else Check(now==old,"Ordinary/EN summary parity after exact legacy gold-sign restoration");
                }
            }
            Check(SiegeCastleCompletedInterventionSummaryBuilder.Build(null)==GitBaselineCastleSummary.Build(null),"Null summary a911 exact");
            foreach(SiegeCastleActionKind action in Enum.GetValues(typeof(SiegeCastleActionKind)))foreach(int count in new[]{0,1,3})foreach(int gold in new[]{0,99}) {
                var f=Facts("Player{action}/{penalty}/$1",90,true,false,action,count,gold);
                Check(SiegeCastleCompletedInterventionSummaryBuilder.Build(f,false)==GitBaselineCastleSummary.Build(f),"All summary Action branches CN a911 "+action+" count="+count+" gold="+gold);
                Check(SiegeCastleCompletedInterventionSummaryBuilder.Build(f,true)==DeliveredBefore.SiegeCastleCompletedInterventionSummaryBuilder.Build(OldFacts("Player{action}/{penalty}/$1",90,true,false,action,count,gold),true),"All summary Action branches EN delivery parity");
            }
            foreach(string kind in new[]{null,""," ","Memory{action}/$1"})foreach(string detail in new[]{null,""," ","detail\r\n value[ACTION:SIEGE_MERCY]/$1"}) {
                Check(SiegeInterventionMemoryEventFormatter.FormatEntry(kind,detail)==GitBaselineMemoryFormatter.FormatEntry(kind,detail),"Default memory a911 identity");
                Check(SiegeInterventionMemoryEventFormatter.FormatEntry(kind,detail,true)==DeliveredBefore.SiegeInterventionMemoryEventFormatter.FormatEntry(kind,detail,true),"English memory delivery parity");
            }
        }
        Check(SiegeInterventionMemoryEventFormatter.FormatEntry(null,null)=="处置：处置","Restored default kind");
        Check(DeliveredBefore.SiegeInterventionMemoryEventFormatter.FormatEntry(null,null)=="Disposition：Disposition","Delivered fallback negative control");negativeControls++;
        var formatter=typeof(SiegeCastleCompletedInterventionSummaryBuilder).GetMethod("FormatTemplate",BindingFlags.NonPublic|BindingFlags.Static);
        Check((string)formatter.Invoke(null,new object[]{"{player}/{unknown}/{PLAYER}",new[]{("{player}","P{penalty}/$1")}})=="P{penalty}/$1/{unknown}/{PLAYER}","Unknown slots/case/literal dynamic name");
    }
    static void Run(string[] args) {
        Console.OutputEncoding=Encoding.UTF8;JsonChecks(args[0]);MigrationChecks(args[0]);CompoundChecks();SummaryChecks();
        File.WriteAllText("result.json",Serialize(new{target=args[1],checks,negativeControls,evidence}),new UTF8Encoding(false));
        Console.WriteLine("PASS GCCZ captured production "+args[1]+" checks="+checks+" negative-controls="+negativeControls+"; live=NOT_RUN");
    }
    static void Main(string[] args) {
        Console.SetError(new StreamWriter(Console.OpenStandardError(),new UTF8Encoding(false)){AutoFlush=true});
        try { Run(args); }
        catch(Exception error) { Console.Error.WriteLine("FAIL after checks="+checks+": "+error.GetType().Name+" "+error.Message); if(error.InnerException!=null)Console.Error.WriteLine(error.InnerException.GetType().Name+" "+error.InnerException.Message);Environment.Exit(2); }
    }
}
