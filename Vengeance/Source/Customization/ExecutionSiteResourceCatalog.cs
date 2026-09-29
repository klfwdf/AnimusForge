using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using RichExecutions.Diagnostics;

namespace RichExecutions.Customization;

[DataContract]
public sealed class ExecutionSiteResourceDefinition : IExtensibleDataObject
{
    [DataMember(Name = "id", Order = 1)] public string Id { get; set; } = string.Empty;
    [DataMember(Name = "resourceKey", Order = 2)] public string ResourceKey { get; set; } = string.Empty;
    [DataMember(Name = "resourceType", Order = 3)] public ExecutionSiteResourceType ResourceType { get; set; }
    [DataMember(Name = "category", Order = 4)] public string Category { get; set; } = string.Empty;
    [DataMember(Name = "name", Order = 5)] public string Name { get; set; } = string.Empty;
    [DataMember(Name = "collision", Order = 6)] public ExecutionSiteCollisionPolicy Collision { get; set; }
    [DataMember(Name = "roleId", Order = 7, EmitDefaultValue = false)] public string? RoleId { get; set; }
    public ExtensionDataObject? ExtensionData { get; set; }
}

[DataContract]
internal sealed class ExecutionSiteResourceCatalogDocument
{
    [DataMember(Name = "resources", Order = 1)] public List<ExecutionSiteResourceDefinition> Resources { get; set; } = new();
}

public static class ExecutionSiteResourceCatalog
{
    private static readonly ExecutionSiteResourceDefinition[] BuiltIns =
    {
        Prefab(
            ExecutionSiteCrucifixDecoration.ResourceId,
            ExecutionSiteCrucifixDecoration.PrefabName,
            "decor",
            "{=REX_Builder_Decoration_Crucifix}Crucifix",
            ExecutionSiteCollisionPolicy.Authored,
            ExecutionSiteCrucifixDecoration.RoleId)
    };

    public static string ExternalCatalogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        @"Mount and Blade II Bannerlord\Configs\RichExecutions\mesh_catalog.json");

    public static IReadOnlyList<ExecutionSiteResourceDefinition> Load()
    {
        var merged = BuiltIns.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(ExternalCatalogPath))
        {
            MergeImpaledCorpseDisplays(merged);
            return Sort(merged.Values);
        }

        try
        {
            using var stream = new FileStream(
                ExternalCatalogPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
            var serializer = new DataContractJsonSerializer(typeof(ExecutionSiteResourceCatalogDocument));
            if (serializer.ReadObject(stream) is ExecutionSiteResourceCatalogDocument document)
            {
                foreach (var item in document.Resources ?? new List<ExecutionSiteResourceDefinition>())
                {
                    if (item is null || string.IsNullOrWhiteSpace(item.Id) ||
                        string.IsNullOrWhiteSpace(item.ResourceKey) ||
                        string.IsNullOrWhiteSpace(item.Category) ||
                        string.IsNullOrWhiteSpace(item.Name))
                    {
                        RexLog.Warning("Ignored an incomplete mesh_catalog.json entry.");
                        continue;
                    }

                    merged[item.Id] = item;
                }
            }
        }
        catch (Exception exception)
        {
            RexLog.Error(
                "The external execution-site mesh catalog could not be read; built-ins remain available.",
                exception);
        }

        MergeImpaledCorpseDisplays(merged);
        return Sort(merged.Values);
    }

    private static void MergeImpaledCorpseDisplays(
        IDictionary<string, ExecutionSiteResourceDefinition> resources)
    {
        foreach (var snapshot in ImpaledCorpseDisplayStore.LoadAll())
        {
            var key = ImpaledCorpseDisplayStore.ResourceIdPrefix + snapshot.Id.ToString("D");
            var displayName = string.IsNullOrWhiteSpace(snapshot.DisplayName)
                ? snapshot.HeroCharacterId
                : snapshot.DisplayName;
            var localizedName = new TaleWorlds.Localization.TextObject(
                "{=REX_Builder_Impaled_Remains_Decoration}Impaled remains: {VICTIM}");
            localizedName.SetTextVariable("VICTIM", displayName);
            resources[key] = Mesh(
                key,
                "verjianchi02",
                "decor",
                localizedName.ToString(),
                ImpaledCorpseDisplayStore.RoleIdPrefix + snapshot.Id.ToString("D"));
        }
    }

    private static IReadOnlyList<ExecutionSiteResourceDefinition> Sort(
        IEnumerable<ExecutionSiteResourceDefinition> resources) => resources
        .OrderBy(item => item.Category, StringComparer.OrdinalIgnoreCase)
        .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

    private static ExecutionSiteResourceDefinition Prefab(
        string id,
        string resourceKey,
        string category,
        string name,
        ExecutionSiteCollisionPolicy collision = ExecutionSiteCollisionPolicy.Authored,
        string? roleId = null) => new()
    {
        Id = id,
        ResourceKey = resourceKey,
        ResourceType = ExecutionSiteResourceType.Prefab,
        Category = category,
        Name = name,
        Collision = collision,
        RoleId = roleId
    };

    private static ExecutionSiteResourceDefinition Mesh(
        string id,
        string resourceKey,
        string category,
        string name,
        string? roleId = null) => new()
    {
        Id = id,
        ResourceKey = resourceKey,
        ResourceType = ExecutionSiteResourceType.Mesh,
        Category = category,
        Name = name,
        Collision = ExecutionSiteCollisionPolicy.Disabled,
        RoleId = roleId
    };
}
