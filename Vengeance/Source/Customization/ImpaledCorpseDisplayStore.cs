using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using RichExecutions.Campaign;
using RichExecutions.Diagnostics;
using BannerlordCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace RichExecutions.Customization;

[DataContract]
internal sealed class ImpaledCorpseDisplaySnapshot : IExtensibleDataObject
{
    [DataMember(Name = "schemaVersion", Order = 1)] public int SchemaVersion { get; set; } = 1;
    [DataMember(Name = "id", Order = 2)] public Guid Id { get; set; } = Guid.NewGuid();
    [DataMember(Name = "heroCharacterId", Order = 3)] public string HeroCharacterId { get; set; } = string.Empty;
    [DataMember(Name = "displayName", Order = 4)] public string DisplayName { get; set; } = string.Empty;
    [DataMember(Name = "bodyProperties", Order = 5)] public string BodyProperties { get; set; } = string.Empty;
    [DataMember(Name = "equipmentCode", Order = 6)] public string EquipmentCode { get; set; } = string.Empty;
    [DataMember(Name = "isFemale", Order = 7)] public bool IsFemale { get; set; }
    [DataMember(Name = "age", Order = 8)] public int Age { get; set; }
    [DataMember(Name = "race", Order = 9)] public int Race { get; set; }
    [DataMember(Name = "clothingColor1", Order = 10)] public uint ClothingColor1 { get; set; }
    [DataMember(Name = "clothingColor2", Order = 11)] public uint ClothingColor2 { get; set; }
    [DataMember(Name = "actionName", Order = 12)] public string ActionName { get; set; } = string.Empty;
    [DataMember(Name = "actionProgress", Order = 13)] public float ActionProgress { get; set; }
    [DataMember(Name = "capturedUtc", Order = 14)] public string CapturedUtc { get; set; } = string.Empty;
    public ExtensionDataObject? ExtensionData { get; set; }
}

internal static class ImpaledCorpseDisplayStore
{
    internal const string ResourceIdPrefix = "impaled_corpse_display:";
    internal const string RoleIdPrefix = "impaled_corpse_display:";

    internal static bool TrySave(
        ImpaledCorpseDisplaySnapshot snapshot,
        out string saveReference)
    {
        saveReference = string.Empty;
        if (!IsValid(snapshot, out var validationError))
        {
            RexLog.Warning($"Refused to save an incomplete impaled-corpse snapshot: {validationError}.");
            return false;
        }

        try
        {
            var behavior = BannerlordCampaign.Current?.GetCampaignBehavior<RichExecutionCampaignBehavior>();
            if (behavior is null)
            {
                RexLog.Warning("Could not save the impaled-corpse snapshot because no active campaign behavior exists.");
                return false;
            }

            return behavior.TrySaveImpaledCorpseDisplay(snapshot, out saveReference);
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not add the impaled-corpse snapshot to the active campaign save data.", exception);
            return false;
        }
    }

    internal static IReadOnlyList<ImpaledCorpseDisplaySnapshot> LoadAll()
    {
        try
        {
            var behavior = BannerlordCampaign.Current?.GetCampaignBehavior<RichExecutionCampaignBehavior>();
            if (behavior is null)
            {
                return Array.Empty<ImpaledCorpseDisplaySnapshot>();
            }

            return behavior.GetImpaledCorpseDisplays()
                .Where(snapshot => IsValid(snapshot, out _))
                .OrderByDescending(snapshot => snapshot.CapturedUtc, StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception exception)
        {
            RexLog.Warning($"Could not read impaled-corpse snapshots from the active campaign save data: {exception.Message}");
            return Array.Empty<ImpaledCorpseDisplaySnapshot>();
        }
    }

    internal static bool TryLoad(Guid id, out ImpaledCorpseDisplaySnapshot snapshot)
    {
        snapshot = null!;
        foreach (var candidate in LoadAll())
        {
            if (candidate.Id == id)
            {
                snapshot = candidate;
                return true;
            }
        }

        return false;
    }

    internal static bool TryParseRoleId(string? roleId, out Guid snapshotId)
    {
        snapshotId = Guid.Empty;
        return roleId is not null &&
               !string.IsNullOrWhiteSpace(roleId) &&
               roleId.StartsWith(RoleIdPrefix, StringComparison.OrdinalIgnoreCase) &&
               Guid.TryParse(roleId.Substring(RoleIdPrefix.Length), out snapshotId);
    }

    private static bool IsValid(ImpaledCorpseDisplaySnapshot snapshot, out string error)
    {
        if (snapshot.SchemaVersion != 1)
        {
            error = $"unsupported schemaVersion {snapshot.SchemaVersion}";
            return false;
        }
        if (snapshot.Id == Guid.Empty ||
            string.IsNullOrWhiteSpace(snapshot.HeroCharacterId) ||
            string.IsNullOrWhiteSpace(snapshot.BodyProperties) ||
            string.IsNullOrWhiteSpace(snapshot.EquipmentCode) ||
            string.IsNullOrWhiteSpace(snapshot.ActionName))
        {
            error = "one or more required appearance or action fields are empty";
            return false;
        }
        if (float.IsNaN(snapshot.ActionProgress) ||
            float.IsInfinity(snapshot.ActionProgress) ||
            snapshot.ActionProgress < 0f ||
            snapshot.ActionProgress > 1f)
        {
            error = "actionProgress is outside the normalized range";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
