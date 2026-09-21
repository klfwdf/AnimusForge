using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;

namespace AnimusForge.DialogueUI.Native;

/// <summary>
/// Copies the active conversation agent into the native CharacterTableau renderer.
/// The renderer owns a private tableau scene (it never changes the mission agent or
/// the mission camera), while the model, equipment and current action are sampled
/// from the speaker.  This is deliberately a small typed bridge for the overlay;
/// it does not search missions or use reflection.
/// </summary>
internal static class LiveSpeakerPortrait
{
    public static bool Apply(CharacterTableauWidget tableau, Agent speaker, CharacterObject character)
    {
        if (tableau == null || character == null)
            return false;

        try
        {
            tableau.IsVisible = true;
            tableau.DoNotAcceptEvents = true;
            tableau.StanceIndex = (int)CharacterViewModel.StanceTypes.EmphasizeFace;
            tableau.CustomRenderScale = 1.35f;
            tableau.CharStringId = character.StringId ?? string.Empty;
            tableau.BodyProperties = speaker != null
                ? speaker.BodyPropertiesValue.ToString()
                : character.HeroObject != null
                    ? character.HeroObject.BodyProperties.ToString()
                    : character.GetBodyProperties(character.Equipment).ToString();
            tableau.IsFemale = speaker != null ? speaker.IsFemale : character.IsFemale;
            tableau.Race = character.Race;
            tableau.EquipmentCode = speaker?.SpawnEquipment != null
                ? speaker.SpawnEquipment.CalculateEquipmentCode()
                : character.Equipment.CalculateEquipmentCode();
            tableau.ArmorColor1 = speaker != null ? speaker.ClothingColor1 : character.Culture?.Color ?? 0;
            tableau.ArmorColor2 = speaker != null ? speaker.ClothingColor2 : character.Culture?.Color2 ?? 0;
            tableau.BannerCodeText = character.HeroObject?.Clan?.Banner?.Serialize() ?? string.Empty;

            // CharacterTableau has no Agent handle.  Feeding the active action into
            // its idle action makes the private clone use the same animation clip
            // as the live speaker instead of the stock EmphasizeFace idle.  Action
            // progress remains renderer-local, so the mission animation is never
            // disturbed and this remains safe across conversation ticks.
            string actionName = GetCurrentActionName(speaker);
            if (!string.IsNullOrEmpty(actionName) && actionName != "act_none")
                tableau.IdleAction = actionName;
            else
                tableau.IdleAction = "act_inventory_idle_start";

            return true;
        }
        catch (Exception ex)
        {
            DialogueUiRuntime.Log("Live speaker portrait update failed: " + ex.GetType().Name + ": " + ex.Message);
            return false;
        }
    }

    private static string GetCurrentActionName(Agent speaker)
    {
        if (speaker == null)
            return null;

        try
        {
            // Channel 1 carries conversation/upper-body overrides in the scene;
            // prefer it while it has weight, then fall back to the base channel.
            if (speaker.GetCurrentAction(1) != ActionIndexCache.act_none && speaker.GetActionChannelWeight(1) > 0.001f)
                return speaker.GetCurrentAction(1).GetName();
            return speaker.GetCurrentAction(0).GetName();
        }
        catch
        {
            return null;
        }
    }
}
