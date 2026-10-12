using System; using System.IO; using System.Collections.Generic; using Newtonsoft.Json; using AnimusForge;
namespace AnimusForge { internal static class AnimusForgeModulePaths { internal static string Directory; internal static string GetModuleDataFilePath(string file)=>Path.Combine(Directory,file); } }
internal static class Program {
static void Check(bool yes,string what){if(!yes)throw new Exception(what);}
static void Main(string[] args){
AnimusForgeModulePaths.Directory=args[0]; bool en=args[1]=="en";
var texts=JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(Path.Combine(args[0],"PromptComposerTexts.json")));
const string dynamic="Lady {NAME} $1 玩家";
var heard=PromptComposerTexts.Format("residual_heard_target_speech",("NAME",dynamic),("TARGET","Alda"),("TEXT","hello {TEXT} $1"));
Check(heard.Contains(dynamic)&&heard.Contains("hello {TEXT} $1"),"single-pass literal inserts");
Check(heard==(en?"[You hear] "+dynamic+" says to Alda: hello {TEXT} $1":"【你听见】"+dynamic+"对Alda说：hello {TEXT} $1"),"speaker target and content");
Check(Projection.FormatScenePlayerDirectSpeechLabel(dynamic,50.1f).Contains("51"),"ceil distance unchanged");
Check(Projection.FormatScenePlayerDirectSpeechLabel(dynamic,5).Contains(en?"says to you":"对你说"),"five-meter boundary unchanged");
Check(Projection.FormatScenePlayerDirectSpeechLabel(dynamic,5.1f).Contains("6"),"near shout boundary unchanged");
Check(Projection.FormatScenePlayerDirectSpeechLabel(dynamic,float.NaN).Contains(dynamic),"nonfinite direct speech preserves actor");
var intro=PersonaIntroMessageComposer.ComposeSystemNpc(new SystemNpcIntroSnapshot {heroName=dynamic,clanName="Clan",factionDisplay="Realm",identityTitle="Identity",ageText="31",cultureText="Culture"});
Check(intro.Contains(dynamic)&&intro.Contains("31"),"persona data survives");
Check(intro.EndsWith(texts["residual_period"].Trim()),"persona punctuation follows package");
var original = new ConversationMessage {Role="assistant", SpeakerName="Borin", Content="Borin: body {TEXT} $1", GameDayIndex=7, GameHour=24};
Check(CourierProjection.TryConvertCourierMemoryMessageToChatMessage(original,"Alda","Player",out object message),"real Courier conversion");
string output=(string)message.GetType().GetProperty("content").GetValue(message);
Check((string)message.GetType().GetProperty("role").GetValue(message)=="user","overheard remains user, not viewer assistant");
Check(output.Contains(en?"[Previously heard]":"【过往听闻】") && output.Contains("body {TEXT} $1"),"Courier provenance and literal body");
Check(original.Content=="Borin: body {TEXT} $1" && original.GameHour==24,"source DTO not mutated");
Check(output.Contains("23"),"clamped hour unchanged");
Check(PromptComposerTexts.Raw("afef_player_action")=="[AFEF玩家行为补充]","AFEF protocol unchanged");
Check(PromptComposerTexts.Raw("history_section_scope_current")=="【当下行为】","scope protocol unchanged");
Check(texts["npc_initiated_fact_supplement"].Contains(en?"not words spoken by the player":"不是玩家说出的台词"),"NPC provenance negation preserved");
var latest=LatestReplyProjection.BuildActionPostprocessLatestReplyBlock(dynamic,"body {TEXT} $1","NPC");
Check(latest==(en?"Player: ":"玩家: ")+dynamic+Environment.NewLine+"NPC: body {TEXT} $1","latest reply package prefix and unchanged literal body");
var empty=LatestReplyProjection.BuildActionPostprocessLatestReplyBlock("","","NPC");
Check(empty==(en?"Player: (none)":"玩家: （无）")+Environment.NewLine+(en?"NPC: (none)":"NPC: （无）"),"latest reply missing-value package text");
Console.WriteLine("PASS "+args[1]+" actual Persona composer, extracted real distance method and production formatter; role/data/protocol preserved");}}
