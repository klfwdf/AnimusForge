using System.Collections.Concurrent;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
namespace TaleWorlds.CampaignSystem
{
    public interface IDataStore { bool IsSaving { get; } bool IsLoading { get; } bool SyncData<T>(string key, ref T data); }
    internal sealed class Clan { internal string Name; internal Hero Leader; }
    internal sealed class Hero
    {
        internal string StringId, Name, EncyclopediaText;
        internal bool IsAlive = true;
        internal Hero Spouse, Father, Mother;
        internal List<Hero> Children = new(), Siblings = new();
        internal Clan Clan;
        internal static Hero MainHero;
        internal static Hero Find(string id) => AnimusForge.MemoryEntityIdentityBannerlordAdapter.FindHeroById(id);
        internal int GetRelation(Hero other) => 30;
    }
}
namespace AnimusForge.Refactor.Runtime
{
    internal static class SaveRuntimeGuard { internal static long Generation = 1; internal static long CaptureGeneration() => Generation; }
}
namespace AnimusForge.Refactor.Adapters
{
    internal static class ConfiguredChatApplicationAdapter
    {
        internal static int Calls;
        internal static string SystemPrompt, Input;
        internal static TaskCompletionSource<AnimusForge.MyBehavior.ApiCallResult> Pending;
        internal static Task<AnimusForge.MyBehavior.ApiCallResult> CallAuxiliaryGatewayDetailed(string system, string user, string source, int tokens, bool forceThinkingDisabled)
        {
            Calls++; SystemPrompt=system; Input=user;
            if (tokens != 1200 || !forceThinkingDisabled || source != "NpcPersonaEvolution") throw new Exception("wrong provider budget");
            Pending = new(TaskCreationOptions.RunContinuationsAsynchronously); return Pending.Task;
        }
    }
}
namespace AnimusForge
{
    internal static class Logger { public static bool IsModLogicEnabled => false; internal static List<string> Lines=new(); internal static void Log(string tag,string message) => Lines.Add(message); }
    internal sealed class DuelSettings { internal bool EnableNpcPersonaEventEvolution=true; internal static DuelSettings Current=new(); internal static int Reads; internal static DuelSettings GetSettings(){Reads++;return Current;} }
    internal sealed class WorldBulletinEvent { internal string Kind,Key,GameDate,Sentence,Detail; internal double Hour; internal List<WorldBulletinParticipant> Participants=new(); }
    internal sealed class WorldBulletinParticipant { internal string HeroId; }
    internal static class NpcPersonaTextRules { internal static IReadOnlyCollection<string> AllowedTraitIdsForPrompt=new[]{"cautious","bold","vengeful"}; }
    internal static class PersonaPromptTexts
    {
        internal static Dictionary<string,string> Values;
        internal static string Get(string id,string fallback)=>Values.TryGetValue(id,out var v)?v:fallback;
        internal static string Format(string id,string fallback,params (string Name,string Value)[] variables)
        { string s=Get(id,fallback);foreach(var v in variables)s=s.Replace("{"+v.Name+"}",v.Value);return s; }
    }
    internal static class EncyclopediaHeroPersonaPatch { internal static int Refreshes; internal static void QueueRefreshForHero(string id)=>Refreshes++; }
    internal static class MemoryEntityIdentityBannerlordAdapter
    { internal static Dictionary<string,Hero> Heroes=new(); internal static Hero FindHeroById(string id)=>id!=null&&Heroes.TryGetValue(id,out var h)?h:null; }
    public partial class MyBehavior
    {
        internal static MyBehavior Instance;
        internal static bool NewsEnabled=true;
        internal double Hour;
        private readonly PersonaProfileStateOwner _personaProfiles=new();
        internal sealed class ApiCallResult { internal bool Success; internal string Content; }
        private NpcPersonaProfile GetNpcPersonaProfile(Hero hero,bool createIfMissing)=>_personaProfiles.Get(hero?.StringId,createIfMissing);
        private void SaveNpcPersonaProfile(Hero hero,NpcPersonaProfile profile)=>_personaProfiles.Save(hero.StringId,profile,(_,_)=>{});
        private static bool IsWorldBulletinEnabled()=>NewsEnabled;
        private double GetCurrentCampaignHourSafe()=>Hour;
        private readonly ConcurrentQueue<Action> completions=new();
        private Task<bool> RunMemorySummaryCompletionAsync(long generation,Func<bool> operation)
        {
            var done=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            completions.Enqueue(()=> {try {done.SetResult(ReferenceEquals(Instance,this)&&generation==Refactor.Runtime.SaveRuntimeGuard.Generation&&operation());}catch(Exception ex){done.SetException(ex);}});
            return done.Task;
        }
        internal NpcPersonaProfile Profile(string id)=>_personaProfiles.Get(id,true);
        internal void Capture(WorldBulletinEvent e)=>OnWorldBulletinEventCaptured(e);
        internal void Tick(bool advanceThrottle=true) {if(advanceThrottle)_personaEvolutionNextStartUtc=DateTime.MinValue;TryStartPersonaEvolution();}
        internal int Queued=>_personaEvolutionQueue.Count;
        internal void Setting(bool enabled) {DuelSettings.Current.EnableNpcPersonaEventEvolution=enabled;NotifyPersonaEvolutionSettingChanged(DuelSettings.Current);}
        internal void Reload() {RestorePersonaEvolutionQueue();}
        internal async Task Finish(string json,bool success=true)
        {
            Refactor.Adapters.ConfiguredChatApplicationAdapter.Pending.SetResult(new(){Success=success,Content=json});
            for(int i=0;i<2000 && _personaEvolutionRequest?.IsCompleted!=true;i++)
            {while(completions.TryDequeue(out var action))action();await Task.Delay(1);}
            if(_personaEvolutionRequest?.IsCompleted!=true)throw new Exception("completion stalled");
            await _personaEvolutionRequest;
        }
    }
    internal sealed class Store : IDataStore
    {
        internal Dictionary<string,object> Values=new();
        public bool IsSaving { get; set; }
        public bool IsLoading=>!IsSaving;
        public bool SyncData<T>(string key, ref T data) {if(IsSaving){Values[key]=data;return true;}if(Values.TryGetValue(key,out var v)){data=(T)v;return true;}return false;}
    }
}
