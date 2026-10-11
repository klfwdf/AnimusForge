using System;
using System.Reflection;
using AnimusForge;
using HarmonyLib;
using TaleWorlds.CampaignSystem;

internal static class Program
{
    private static int _checks;
    private static readonly FieldInfo Applied = typeof(DiplomacyBehavior).GetField(
        "s_globalPatchesApplied", BindingFlags.Static | BindingFlags.NonPublic);

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("FAIL " + name);
        _checks++;
    }

    private static void Reset()
    {
        Applied.SetValue(null, false);
        Harmony.ConstructorFailures = Harmony.PatchFailures = Harmony.Attempts = 0;
        Logger.ThrowOnSuccess = false;
    }

    private static void Register()
    {
        var owner = new DiplomacyBehavior();
        owner.RegisterEvents();
        Check(ReferenceEquals(DiplomacyBehavior.Instance, owner), "real campaign owner published");
    }

    private static int Main()
    {
        try
        {
            // The real CLR ModuleInitializer has already encountered the controlled first failure.
            Check(Harmony.Attempts == 1, "actual module initializer attempts installation");
            Check(!(bool)Applied.GetValue(null), "failed startup must not claim installed");
            Register();
            Check(Harmony.Attempts == 2 && (bool)Applied.GetValue(null), "campaign retries failed startup");
            DiplomacyBehavior.ModuleInit();
            Register();
            Check(Harmony.Attempts == 2, "successful installation is not duplicated across callbacks");

            Reset();
            Harmony.ConstructorFailures = 1;
            DiplomacyBehavior.ModuleInit();
            Check(Harmony.Attempts == 0 && !(bool)Applied.GetValue(null), "constructor failure leaves retry open");
            Register();
            Check(Harmony.Attempts == 1 && (bool)Applied.GetValue(null), "constructor failure recovers at campaign entry");

            Reset();
            Harmony.PatchFailures = 2;
            DiplomacyBehavior.ModuleInit();
            Register();
            Check(Harmony.Attempts == 2 && !(bool)Applied.GetValue(null), "repeated failures remain retryable");
            Register();
            Check(Harmony.Attempts == 3 && (bool)Applied.GetValue(null), "later successful installation closes retry");

            Reset();
            Logger.ThrowOnSuccess = true;
            DiplomacyBehavior.ModuleInit();
            Check((bool)Applied.GetValue(null), "success logging failure cannot undo installed state");
            Register();
            Check(Harmony.Attempts == 1, "logging failure cannot cause duplicate postfix installation");

            Check(Harmony.Target?.Name == "BuildShoutPromptContextForExternal", "real prompt entry selected");
            Check(Harmony.Postfix?.Name == "Patch_BuildDiplomacyContext_Postfix", "real diplomatic postfix selected");
            Console.WriteLine("PASS diplomacy patch registration checks=" + _checks);
            Console.WriteLine("Boundary: complete production class and actual CLR ModuleInitializer; fake Harmony/game/logging leaves, no live game.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}

namespace HarmonyLib
{
    public sealed class HarmonyMethod
    {
        internal readonly MethodInfo Method;
        public HarmonyMethod(Type type, string name)
        {
            Method = type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Missing postfix");
        }
    }

    public sealed class Harmony
    {
        internal static int ConstructorFailures, PatchFailures = 1, Attempts;
        internal static MethodInfo Target, Postfix;
        public Harmony(string id)
        {
            if (ConstructorFailures > 0) { ConstructorFailures--; throw new InvalidOperationException("Controlled constructor failure"); }
        }
        public void Patch(MethodInfo method, HarmonyMethod postfix)
        {
            if (method == null) throw new InvalidOperationException("Missing prompt entry");
            Attempts++;
            if (PatchFailures > 0) { PatchFailures--; throw new InvalidOperationException("Controlled patch failure"); }
            Target = method;
            Postfix = postfix.Method;
        }
    }
}

namespace TaleWorlds.CampaignSystem
{
    public interface IDataStore { }
    public class Hero { }
    public class CharacterObject { public Hero HeroObject; }
    public abstract class CampaignBehaviorBase
    {
        public abstract void RegisterEvents();
        public abstract void SyncData(IDataStore store);
    }
}

namespace AnimusForge
{
    public static class Logger
    {
        internal static bool ThrowOnSuccess;
        public static void Log(string category, string message)
        {
            if (ThrowOnSuccess && message == "[Harmony] Diplomacy context injection applied.")
            {
                ThrowOnSuccess = false;
                throw new InvalidOperationException("Controlled logging failure");
            }
        }
    }
    public sealed class MyBehavior
    {
        public sealed class ShoutPromptContext { public string Extras; }
        public static void BuildShoutPromptContextForExternal() { }
    }
    public struct DiplomacyPostprocessContextSource
    {
        public DiplomacyPostprocessContextSource(Hero hero) { }
    }
    public static class DiplomacyPostprocessContextApplication
    {
        public static string Build(ref DiplomacyPostprocessContextSource source) => "";
    }
    public static class DiplomacyConversationBridge
    {
        public static string BuildDiplomacyPrompt(Hero hero, string extras) => "diplomacy";
    }
}

// Namespace-only leaves satisfy the unchanged production using directives.
namespace AnimusForge.Refactor.Adapters { }
namespace AnimusForge.Refactor.Contracts { }
namespace AnimusForge.Refactor.Domain { }
namespace AnimusForge.Refactor.Modules { }
namespace TaleWorlds.CampaignSystem.Actions { }
namespace TaleWorlds.CampaignSystem.CampaignBehaviors { }
namespace TaleWorlds.CampaignSystem.Election { }
namespace TaleWorlds.CampaignSystem.Settlements { }
namespace TaleWorlds.Core { }
namespace TaleWorlds.Library { }
namespace TaleWorlds.Localization { }
