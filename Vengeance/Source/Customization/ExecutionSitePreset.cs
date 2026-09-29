using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RichExecutions.Customization;

public enum ExecutionSiteResourceType
{
    Prefab = 0,
    Mesh = 1
}

public enum ExecutionSiteCollisionPolicy
{
    Authored = 0,
    Disabled = 1,
    Enabled = 2
}

public enum ExecutionSiteMarkerKind
{
    Actor = 0,
    Interaction = 1,
    Functional = 2,
    Crowd = 3
}

[DataContract]
public sealed class ExecutionPresetTransform : IExtensibleDataObject
{
    [DataMember(Name = "x", Order = 1)] public float X { get; set; }
    [DataMember(Name = "y", Order = 2)] public float Y { get; set; }
    [DataMember(Name = "z", Order = 3)] public float Z { get; set; }
    [DataMember(Name = "pitch", Order = 4)] public float Pitch { get; set; }
    [DataMember(Name = "yaw", Order = 5)] public float Yaw { get; set; }
    [DataMember(Name = "roll", Order = 6)] public float Roll { get; set; }
    [DataMember(Name = "scale", Order = 7)] public float Scale { get; set; } = 1f;
    public ExtensionDataObject? ExtensionData { get; set; }

    public ExecutionPresetTransform Clone() => new()
    {
        X = X,
        Y = Y,
        Z = Z,
        Pitch = Pitch,
        Yaw = Yaw,
        Roll = Roll,
        Scale = Scale
    };
}

[DataContract]
public sealed class ExecutionSitePiece : IExtensibleDataObject
{
    [DataMember(Name = "id", Order = 1)] public Guid Id { get; set; } = Guid.NewGuid();
    [DataMember(Name = "resourceKey", Order = 2)] public string ResourceKey { get; set; } = string.Empty;
    [DataMember(Name = "resourceType", Order = 3)] public ExecutionSiteResourceType ResourceType { get; set; }
    [DataMember(Name = "roleId", Order = 4, EmitDefaultValue = false)] public string? RoleId { get; set; }
    [DataMember(Name = "transform", Order = 5)] public ExecutionPresetTransform Transform { get; set; } = new();
    [DataMember(Name = "collision", Order = 6)] public ExecutionSiteCollisionPolicy Collision { get; set; }
    public ExtensionDataObject? ExtensionData { get; set; }
}

[DataContract]
public sealed class ExecutionSiteMarker : IExtensibleDataObject
{
    [DataMember(Name = "id", Order = 1)] public Guid Id { get; set; } = Guid.NewGuid();
    [DataMember(Name = "roleId", Order = 2)] public string RoleId { get; set; } = string.Empty;
    [DataMember(Name = "kind", Order = 3)] public ExecutionSiteMarkerKind Kind { get; set; }
    [DataMember(Name = "transform", Order = 4)] public ExecutionPresetTransform Transform { get; set; } = new();
    public ExtensionDataObject? ExtensionData { get; set; }
}

[DataContract]
public sealed class ExecutionSiteTroopSelection : IExtensibleDataObject
{
    [DataMember(Name = "executioner", Order = 1, EmitDefaultValue = false)] public string? ExecutionerCharacterId { get; set; }
    [DataMember(Name = "meleeGuard", Order = 2, EmitDefaultValue = false)] public string? MeleeGuardCharacterId { get; set; }
    [DataMember(Name = "rangedGuard", Order = 3, EmitDefaultValue = false)] public string? RangedGuardCharacterId { get; set; }
    public ExtensionDataObject? ExtensionData { get; set; }
}

[DataContract]
public sealed class ExecutionSitePreset : IExtensibleDataObject
{
    public const int CurrentSchemaVersion = 1;

    [DataMember(Name = "schemaVersion", Order = 1)] public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    [DataMember(Name = "id", Order = 2)] public Guid Id { get; set; } = Guid.NewGuid();
    [DataMember(Name = "name", Order = 3)] public string Name { get; set; } = string.Empty;
    [DataMember(Name = "methodId", Order = 4)] public string MethodId { get; set; } = string.Empty;
    [DataMember(Name = "rootAnchor", Order = 5)] public ExecutionPresetTransform RootAnchor { get; set; } = new();
    [DataMember(Name = "pieces", Order = 6)] public List<ExecutionSitePiece> Pieces { get; set; } = new();
    [DataMember(Name = "markers", Order = 7)] public List<ExecutionSiteMarker> Markers { get; set; } = new();
    [DataMember(Name = "troops", Order = 8)] public ExecutionSiteTroopSelection Troops { get; set; } = new();
    public ExtensionDataObject? ExtensionData { get; set; }
}