using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Tableaus;

namespace AnimusForge.DialogueUI.Native;

// Event-only framing for registered DialogueUI portraits. No mission/global camera changes.
internal static class PortraitCamera
{
    private sealed class Owner { internal CharacterTableau Tableau; }
    private static ConditionalWeakTable<TextureWidget, Owner> _widgets = new();
    private static ConditionalWeakTable<CharacterTableau, Owner> _tableaus = new();
    private static FieldInfo _providerTableau;

    internal static void Install(Harmony harmony)
    {
        var provider = AccessTools.TypeByName("TaleWorlds.MountAndBlade.GauntletUI.TextureProviders.CharacterTableauTextureProvider");
        _providerTableau = AccessTools.Field(provider, "_characterTableau");
        var initialized = AccessTools.Method(typeof(TextureWidget), "SetTextureProviderProperties", Type.EmptyTypes);
        var adjusted = AccessTools.Method(typeof(CharacterTableau), "AdjustCharacterForStanceIndex", Type.EmptyTypes);
        if (_providerTableau?.FieldType != typeof(CharacterTableau) || initialized == null || adjusted == null
            || AccessTools.Field(typeof(CharacterTableau), "_camPos")?.FieldType != typeof(MatrixFrame)
            || AccessTools.Field(typeof(CharacterTableau), "_agentVisuals")?.FieldType != typeof(AgentVisuals))
            throw new MissingMemberException("DialogueUI portrait camera contract unavailable.");
        harmony.Patch(initialized, postfix: new HarmonyMethod(typeof(PortraitCamera), nameof(ProviderReady)));
        harmony.Patch(adjusted, postfix: new HarmonyMethod(typeof(PortraitCamera), nameof(FrameHead)));
    }

    internal static void Register(CharacterTableauWidget widget)
    {
        _widgets.GetValue(widget, _ => new Owner());
        ProviderReady(widget);
    }

    private static void ProviderReady(TextureWidget __instance)
    {
        if (!_widgets.TryGetValue(__instance, out var owner)) return;
        var provider = __instance.TextureProvider;
        if (provider == null || !_providerTableau.DeclaringType.IsInstanceOfType(provider)) return;
        // One field read per provider creation, never in the frame/appearance polling path.
        var tableau = _providerTableau.GetValue(provider) as CharacterTableau;
        if (tableau == null || ReferenceEquals(tableau, owner.Tableau)) return;
        if (owner.Tableau != null) _tableaus.Remove(owner.Tableau);
        owner.Tableau = tableau;
        _tableaus.Add(tableau, owner);
        // Also handles registration after an already-created provider's first render.
        tableau.SetStanceIndex((int)TaleWorlds.Core.ViewModelCollection.CharacterViewModel.StanceTypes.EmphasizeFace);
    }

    private static void FrameHead(CharacterTableau __instance, AgentVisuals ____agentVisuals, ref MatrixFrame ____camPos)
    {
        if (!_tableaus.TryGetValue(__instance, out _) || ____agentVisuals == null) return;
        try
        {
            if (!PortraitFraming.TryGetCameraOffsets(____agentVisuals.GetScale(), out float distance, out float eyeAboveCenter)) return;
            // The native stance method has refreshed and ticked the current model's skeleton.
            // Stable eye position includes sex, body height and race, without following idle sway.
            Vec3 eye = ____agentVisuals.GetGlobalStableEyePoint(true);
            if (!Finite(eye.x) || !Finite(eye.y) || !Finite(eye.z)) return;
            // Camera local axes: s = right, f = screen up, u = backwards. Keep native orientation.
            ____camPos.origin = eye + ____camPos.rotation.u * distance - ____camPos.rotation.f * eyeAboveCenter;
        }
        catch (Exception ex)
        {
            DialogueUiRuntime.LogOnce("portrait-camera", "Portrait framing failed: " + ex.GetType().Name + ": " + ex.Message);
        }
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    internal static void Unregister(CharacterTableauWidget widget)
    {
        if (widget == null || !_widgets.TryGetValue(widget, out var owner)) return;
        if (owner.Tableau != null) _tableaus.Remove(owner.Tableau);
        _widgets.Remove(widget);
    }

    internal static void Shutdown()
    {
        _widgets = new ConditionalWeakTable<TextureWidget, Owner>();
        _tableaus = new ConditionalWeakTable<CharacterTableau, Owner>();
    }
}
