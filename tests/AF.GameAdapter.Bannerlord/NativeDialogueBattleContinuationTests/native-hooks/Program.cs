using System;
using System.Linq;
using HarmonyLib;
using AnimusForge;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;

internal static class Program
{
    private static int Main()
    {
        try
        {
            var ensure = AccessTools.Method(typeof(NativeDialogueBattleContinuation), "EnsurePatched");
            if (!(bool)ensure.Invoke(null, null)) throw new Exception("Native hooks failed to install");
            foreach (var method in new[] {
                AccessTools.Method(typeof(ConversationManager), "ProcessSentence", new[] { typeof(ConversationSentenceOption) }),
                AccessTools.Method(typeof(ConversationManager), "EndConversation", Type.EmptyTypes) })
            {
                if (Harmony.GetPatchInfo(method).Postfixes.Count(p => p.owner == "AnimusForge.native_dialogue_combat_continuation") != 1)
                    throw new Exception("Missing actual native postfix: " + method);
                Console.WriteLine("PASS actual native Harmony hook: " + method);
            }
            ensure.Invoke(null, null);
            NativeDialogueBattleContinuation.SentenceProcessed(null, default, false);
            NativeDialogueBattleContinuation.ConversationEnded(null, false);
            NativeDialogueBattleContinuation.Tick();
            Console.WriteLine("PASS production hook installation and inactive/skipped entry smoke; no live Campaign.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
namespace AnimusForge
{
    internal static class MapSeaContextGuard { internal static bool IsCurrentPlayerEncounterAtSea(Hero target) => false; }
    internal static class LordEncounterBehavior
    {
        internal static bool IsNativeEncounterActivityContext(Hero target) => false;
        internal static void LogEncounterDiagnostic(string stage, string reason) { }
    }
    internal static class MeetingBattleRuntime { internal static bool IsMeetingActive => false; }
    internal static class PlayerEncounterCompat
    {
        internal static bool HasCampaignBattleResult() => false;
        internal static bool IsInPostBattleResultFlow() => false;
    }
    internal static class Logger { internal static void Log(string category, string text) => Console.WriteLine(category + ": " + text); }
}
