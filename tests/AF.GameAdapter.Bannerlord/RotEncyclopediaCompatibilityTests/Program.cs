using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encyclopedia;
using AnimusForge;

internal static class Program
{
    private static int checks;
    private static void Check(bool ok, string name)
    {
        if (!ok) throw new InvalidOperationException(name);
        checks++;
    }

    public static void Main()
    {
        var rot = AccessTools.TypeByName("ROT.HarmonyPatches.CustomCompanions.EncyclopediaListItemPatches");
        if (rot == null)
        {
            RotEncyclopediaCompanionCompatibility.EnsurePatched();
            RotEncyclopediaCompanionCompatibility.EnsurePatched();
            Check(Logger.Messages == 0, "absent ROT is a quiet no-op");
            Console.WriteLine("PASS optional ROT absent; no hard reference, patch or repeated log.");
            return;
        }
        var clan = new Clan { Name = "Player" };
        Hero.MainHero = new Hero { Clan = clan };
        var recruit = new Hero { Clan = clan, IsWanderer = true, Name = "Former lord" };
        var postfix = AccessTools.DeclaredMethod(rot, "Postfix");
        var constructor = typeof(EncyclopediaListItem).GetConstructors()[0];
        // Install ROT first, then guard its already-active postfix, as in startup.
        new Harmony("test.rot").Patch(constructor, postfix: new HarmonyMethod(postfix));
        bool failed = false;
        try { Make(recruit); } catch (NullReferenceException) { failed = true; }
        Check(failed, "negative control reproduces the logged constructor failure");

        RotEncyclopediaCompanionCompatibility.EnsurePatched();
        Check(Logger.Messages == 1, "startup registration succeeded");
        Check(Make(recruit).Name == "Vanilla", "template-less recruited lord retains native item");
        Check(recruit.Template == null && recruit.IsWanderer && recruit.Clan == clan,
            "guard does not mutate recruited hero");
        Check(Make(recruit).Name == "Vanilla", "repeat encyclopedia open remains usable");
        recruit.Template = new CharacterObject { StringId = "ROTwanderer_test" };
        Check(Make(recruit).Name == "Former lord", "ROT generated companion naming preserved");
        recruit.Template = new CharacterObject { StringId = "ordinary_template" };
        Check(Make(recruit).Name == "Vanilla", "non-ROT template keeps original ROT filter");
        recruit.Template = null;
        recruit.Clan = new Clan();
        Check(Make(recruit).Name == "Vanilla", "other clan wanderer follows original short circuit");
        recruit.Clan = clan;
        recruit.IsWanderer = false;
        Check(Make(recruit).Name == "Vanilla", "ordinary lord unaffected");
        Check(Make(new object()).Name == "Vanilla", "non-hero item unaffected");
        Check(Make(null).Name == "Vanilla", "null item follows original path");
        Hero.MainHero = null;
        Check(RotEncyclopediaCompanionCompatibility.Prefix(recruit), "no main hero is not blanket suppression");
        RotEncyclopediaCompanionCompatibility.EnsurePatched();
        Check(Logger.Messages == 1, "repeated registration is idempotent");
        Check(Harmony.GetPatchInfo(postfix).Prefixes.Count == 1, "one guard on ROT postfix only");
        Check(Harmony.GetPatchInfo(constructor).Prefixes.Count == 0, "no global encyclopedia prefix");
        Console.WriteLine($"PASS {checks} real-Harmony constructor/postfix checks; production guard, source-derived ROT body, synthetic game objects; live game NOT RUN.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static EncyclopediaListItem Make(object obj) => new EncyclopediaListItem(obj, "Vanilla", "", "hero", "Hero", true, null);
}

namespace AnimusForge
{
    internal static class Logger
    {
        internal static int Messages;
        public static void LogTrace(string source, string message) { Messages++; Console.WriteLine(message); }
    }
}
namespace TaleWorlds.CampaignSystem
{
    public class Clan { public string Name; }
    public class CharacterObject { public string StringId; }
    public class Hero
    {
        public static Hero MainHero;
        public bool IsWanderer;
        public Clan Clan;
        public CharacterObject Template;
        public string Name;
    }
}
namespace TaleWorlds.CampaignSystem.Encyclopedia
{
    public struct EncyclopediaListItem
    {
        public readonly string Name;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public EncyclopediaListItem(object obj, string name, string description, string id,
            string typeName, bool playerCanSeeValues, Action onShowTooltip = null) { Name = name; }
    }
}

#if ROT_PRESENT
namespace ROT.HarmonyPatches.CustomCompanions
{
    // Exact filtering/name mutation from the installed ROT patch. This fixture
    // reproduces the missing-template bug without loading a campaign/native engine.
    internal static class EncyclopediaListItemPatches
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Postfix(ref EncyclopediaListItem __instance, object obj, string name,
            string description, string id, string typeName, bool playerCanSeeValues, Action onShowTooltip = null)
        {
            Hero hero = obj as Hero;
            if (hero != null && hero.IsWanderer && hero.Clan == Hero.MainHero.Clan
                && hero.Template.StringId.Contains("ROTwanderer"))
            {
                object boxed = __instance;
                typeof(EncyclopediaListItem).GetField("Name", BindingFlags.Instance | BindingFlags.Public)
                    .SetValue(boxed, hero.Name.ToString());
                __instance = (EncyclopediaListItem)boxed;
            }
        }
    }
}
#endif
