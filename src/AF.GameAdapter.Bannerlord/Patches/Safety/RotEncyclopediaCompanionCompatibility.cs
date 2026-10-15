using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encyclopedia;

namespace AnimusForge;

// ROT's name projection assumes player wanderers were cloned from a template.
// Former lords can be valid companions without one. Keep their vanilla list item.
internal static class RotEncyclopediaCompanionCompatibility
{
    private const string HarmonyId = "com.AnimusForge.spy.rot_encyclopedia_companion_guard";
    private const string TargetTypeName = "ROT.HarmonyPatches.CustomCompanions.EncyclopediaListItemPatches";
    private static bool _patched;

    internal static void EnsurePatched()
    {
        if (_patched) return;
        try
        {
            Type type = AccessTools.TypeByName(TargetTypeName);
            if (type == null) return; // Optional MOD; no runtime dependency on ROT.
            MethodInfo target = AccessTools.DeclaredMethod(type, "Postfix", new[]
            {
                typeof(EncyclopediaListItem).MakeByRefType(), typeof(object),
                typeof(string), typeof(string), typeof(string), typeof(string),
                typeof(bool), typeof(Action)
            });
            if (target == null || !target.IsStatic || target.ReturnType != typeof(void))
            {
                Logger.LogTrace("RotEncyclopediaCompat", "ROT encyclopedia postfix signature not supported; guard not installed.");
                return;
            }
            new Harmony(HarmonyId).Patch(target,
                prefix: new HarmonyMethod(typeof(RotEncyclopediaCompanionCompatibility), nameof(Prefix)));
            _patched = true;
            Logger.LogTrace("RotEncyclopediaCompat", "ROT encyclopedia null-template companion guard installed.");
        }
        catch (Exception ex)
        {
            Logger.LogTrace("RotEncyclopediaCompat", "ROT encyclopedia guard registration failed: " + ex);
        }
    }

    internal static bool Prefix(object obj)
    {
        if (!(obj is Hero hero) || !hero.IsWanderer) return true;
        Hero mainHero = Hero.MainHero;
        // Match ROT's own scope. A null template must not reach its StringId read.
        return mainHero == null || hero.Clan != mainHero.Clan || hero.Template != null;
    }
}
