"""Execute the production date adapter with alternate calendar renderers, no game/API calls."""
from pathlib import Path
import importlib.util, subprocess, sys
root=Path(__file__).resolve().parents[4]
out=Path(sys.argv[1]).resolve();out.mkdir(parents=True,exist_ok=True)
spec=importlib.util.spec_from_file_location('extract',root/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extract=importlib.util.module_from_spec(spec);spec.loader.exec_module(extract)
host=(root/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig')
policy=(root/'src/modules/AF.Module.Weekly/Panel/WeeklyReportArchivePolicy.cs').read_text(encoding='utf-8-sig')
source='using System;class Program {'+extract.declaration(host,'private static string FormatNewsCalendarDate(')+'''
static void Main() {
 int n=0;void Check(bool value){if(!value)throw new Exception("calendar adapter");n++;}
 CampaignTime.Render=day=> { Check(day==91118);return "1084年秋季21日"; };
 Check(FormatNewsCalendarDate(91118)=="卡拉迪亚1084年秋季21日");
 CampaignTime.Render=day=> { Check(day==396024);return "1084年12月31日"; };
 Check(FormatNewsCalendarDate(396024)=="卡拉迪亚1084年12月31日");
 CampaignTime.Render=day=>"1085年1月1日";Check(FormatNewsCalendarDate(396025)=="卡拉迪亚1085年1月1日");
 CampaignTime.Render=day=>throw new Exception();Check(FormatNewsCalendarDate(55)=="日期未知");
 Check(FormatNewsCalendarDate(-1)=="日期未知");
 Console.WriteLine("PASS "+n+" production date-adapter checks: game calendar forwarding, alternate 365-day renderer, boundary/failure; native mod NOT-RUN.");
}}
struct CampaignTime {
 public static Func<int,string> Render;private int day;
 public static CampaignTime Days(float day)=>new CampaignTime{day=(int)day};
 public override string ToString()=>Render(day);
}
class WeeklyReportArchivePolicy {
'''+extract.declaration(policy,'internal static string CalendarDate(')+'}'
(out/'Program.cs').write_text(source,encoding='utf-8')
(out/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>',encoding='utf-8')
subprocess.run(['dotnet','run','--project',str(out/'Test.csproj')],cwd=out,check=True)
