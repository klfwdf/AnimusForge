using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

// Recording fakes model only method signatures. No Bannerlord gameplay is simulated.
namespace TaleWorlds.CampaignSystem
{
    public sealed class Hero { }
    public sealed class CharacterObject { }
}

namespace AnimusForge
{
    public sealed class PostprocessRuleEntry { }
    internal static class Recorder
    {
        internal static string LastMethod;
        internal static object[] LastArguments;
        internal static int Calls;
        internal static bool BoolResult;
        internal static bool HandledResult;
        internal static string TextResult;
        internal static string RefResult;
        internal static string FailureResult;
        internal static List<PostprocessRuleEntry> Rules;
        internal static List<string> Facts;
        internal static List<string> Notifications;
        internal static Exception Exception;
        internal static void Call(string method, params object[] args)
        {
            LastMethod = method; LastArguments = args; Calls++;
            if (Exception != null) throw Exception;
        }
    }
    internal static class KingdomAgendaCustomPolicyBehavior
    {
        public static bool IsEligibleTargetForExternal(Hero ruler, out string failureReason)
        { Recorder.Call("Policy.Eligible", ruler); failureReason = Recorder.FailureResult; return Recorder.BoolResult; }
        public static List<PostprocessRuleEntry> BuildRuntimePostprocessRulesForExternal(Hero ruler)
        { Recorder.Call("Policy.Rules", ruler); return Recorder.Rules; }
        public static bool TryProcessAcceptedAgendaTag(Hero ruler, string chainName, string playerProposalText,
            string npcReplyText, ref string content, out string failureReason)
        { Recorder.Call("Policy.Apply", ruler, chainName, playerProposalText, npcReplyText, content);
          content = Recorder.RefResult; failureReason = Recorder.FailureResult; return Recorder.BoolResult; }
    }
    internal static class NpcRulerPolicyBehavior
    {
        public static string BuildActivePolicyDialogueContextForExternal(Hero targetHero,
            CharacterObject targetCharacter, string kingdomIdOverride = null)
        { Recorder.Call("Policy.Context", targetHero, targetCharacter, kingdomIdOverride); return Recorder.TextResult; }
    }
    internal static class NobleGatheringBehavior
    {
        public static List<PostprocessRuleEntry> BuildRuntimePostprocessRulesForExternal(Hero targetHero)
        { Recorder.Call("Gathering.Rules", targetHero); return Recorder.Rules; }
        public static string BuildPostprocessContextForExternal(Hero conversationHero)
        { Recorder.Call("Gathering.Context", conversationHero); return Recorder.TextResult; }
        public static string NormalizeNobleGatheringPostprocessTagsForExternal(string raw)
        { Recorder.Call("Gathering.Normalize", raw); return Recorder.TextResult; }
        public static string BuildFeastAttendanceContext(Hero hero)
        { Recorder.Call("Gathering.Feast", hero); return Recorder.TextResult; }
        public static bool TryApplyNobleGatheringTagsForExternal(Hero conversationHero, ref string content,
            out List<string> generatedFacts, out List<string> notifications)
        { Recorder.Call("Gathering.Apply", conversationHero, content); content = Recorder.RefResult;
          generatedFacts = Recorder.Facts; notifications = Recorder.Notifications; return Recorder.BoolResult; }
    }
    internal static class AfGcczShoutBridge
    {
        internal static List<PostprocessRuleEntry> BuildPostprocessRules(bool selected, int targetAgentIndex,
            bool replyIsDirectPlayerResponse, string playerText)
        { Recorder.Call("Siege.Rules", selected, targetAgentIndex, replyIsDirectPlayerResponse, playerText); return Recorder.Rules; }
        internal static string BuildPostprocessContext(bool selected, int targetAgentIndex,
            bool replyIsDirectPlayerResponse, string playerText = null)
        { Recorder.Call("Siege.Context", selected, targetAgentIndex, replyIsDirectPlayerResponse, playerText); return Recorder.TextResult; }
        internal static string NormalizePostprocessTags(bool selected, string raw, List<PostprocessRuleEntry> rules)
        { Recorder.Call("Siege.Normalize", selected, raw, rules); return Recorder.TextResult; }
        internal static bool TryProcessActionTags(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex,
            ref string text, out bool actionHandled, bool replyIsDirectPlayerResponse = false,
            string playerText = null, string speakerReplyText = null)
        { Recorder.Call("Siege.Apply", targetHero, targetCharacter, targetAgentIndex, text,
              replyIsDirectPlayerResponse, playerText, speakerReplyText);
          text = Recorder.RefResult; actionHandled = Recorder.HandledResult; return Recorder.BoolResult; }
    }
}
