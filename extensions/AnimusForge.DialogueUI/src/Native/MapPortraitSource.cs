using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade.View;

namespace AnimusForge.DialogueUI.Native;

internal static class MapPortraitSource
{
    private static ConditionalWeakTable<object, MapPortraitAppearance> _appearances = new();
    private static Func<object, CharacterObject> _character;

    internal static void Install(Harmony harmony)
    {
        try
        {
            var type = AccessTools.TypeByName("SandBox.View.Map.MapConversationTableau");
            var dataField = AccessTools.Field(type, "_data");
            var visualsField = AccessTools.Field(type, "_agentVisuals");
            var spawn = AccessTools.Method(type, "SpawnOpponentLeader", Type.EmptyTypes);
            var setData = AccessTools.Method(type, "SetData", new[] { typeof(object) });
            var finalize = AccessTools.Method(type, "OnFinalize", new[] { typeof(bool) });
            if (dataField == null || visualsField?.FieldType != typeof(List<AgentVisuals>) || spawn == null || setData == null || finalize == null)
                throw new MissingMemberException("Map tableau appearance contract unavailable");
            var input = Expression.Parameter(typeof(object));
            var partner = Expression.Property(Expression.Convert(input, dataField.FieldType), "ConversationPartnerData");
            _character = Expression.Lambda<Func<object, CharacterObject>>(Expression.Field(partner, "Character"), input).Compile();
            harmony.Patch(spawn, prefix: new HarmonyMethod(typeof(MapPortraitSource), nameof(BeforeSpawn)),
                postfix: new HarmonyMethod(typeof(MapPortraitSource), nameof(CaptureSpawnedLeader)));
            harmony.Patch(setData, prefix: new HarmonyMethod(typeof(MapPortraitSource), nameof(DataChanging)));
            harmony.Patch(finalize, prefix: new HarmonyMethod(typeof(MapPortraitSource), nameof(Release)));
        }
        catch (Exception ex)
        {
            // Keep the conversation usable. Hide an unavailable map portrait rather than invent a face.
            DialogueUiRuntime.Log("Map portrait source unavailable: " + ex.Message);
        }
    }

    internal static bool TryGet(object tableauData, out MapPortraitAppearance appearance)
    {
        appearance = null;
        return tableauData != null && _appearances.TryGetValue(tableauData, out appearance);
    }

    private static void BeforeSpawn(List<AgentVisuals> ____agentVisuals, out int __state)
        => __state = ____agentVisuals?.Count ?? 0;

    private static void CaptureSpawnedLeader(object ____data, List<AgentVisuals> ____agentVisuals, int __state)
    {
        if (____data == null) return;
        _appearances.Remove(____data);
        try
        {
            if (____agentVisuals == null || ____agentVisuals.Count <= __state) return;
            // SpawnOpponentLeader appends exactly the leader; bodyguards spawn in separate calls.
            var leader = ____agentVisuals[____agentVisuals.Count - 1];
            var visual = leader.GetCopyAgentVisualsData();
            var character = _character(____data);
            if (character == null || visual.EquipmentData == null) return;
            leader.GetClothingColors(out uint color1, out uint color2);
            _appearances.Add(____data, new MapPortraitAppearance
            {
                Character = character, Body = leader.GetBodyProperties(),
                Equipment = visual.EquipmentData.Clone(), Race = visual.RaceData,
                Female = leader.GetIsFemale(), Color1 = color1, Color2 = color2, Banner = visual.BannerData
            });
            DialogueUiRuntime.Log("Captured map portrait from spawned leader: " + character.StringId);
        }
        catch (Exception ex) { DialogueUiRuntime.LogOnce("map-portrait-capture", "Map portrait capture failed: " + ex.Message); }
    }

    private static void DataChanging(object ____data, object data)
    {
        if (!ReferenceEquals(____data, data)) Release(____data);
    }

    private static void Release(object ____data)
    {
        if (____data != null) _appearances.Remove(____data);
    }

    internal static void Shutdown() => _appearances = new ConditionalWeakTable<object, MapPortraitAppearance>();
}
