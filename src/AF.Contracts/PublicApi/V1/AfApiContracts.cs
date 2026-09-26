using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AnimusForge.Api.V1;

/// <summary>框架装配状态，不表示 Campaign/Mission 就绪或实机验收通过。</summary>
public enum AfFrameworkState
{
    NotInitialized = 0,
    Ready = 1,
    Degraded = 2,
    Stopped = 3
}

public enum AfCapabilityState
{
    Available = 0,
    NotSupported = 1,
    UnknownCapability = 2,
    VersionMismatch = 3,
    InvalidRequest = 4
}

/// <summary>内部适配器的目录状态。Available 不是对外执行授权，也不是某个 NPC 的资格判断。</summary>
public enum AfModuleCapabilityState
{
    Available = 0,
    NotInitialized = 1,
    Unavailable = 2,
    InvalidRegistration = 3
}

/// <summary>Courier transport outcome, independent of accepted dialogue effects.</summary>
public enum AfCourierTransportOutcome
{
    NotStarted = 0,
    InTransit = 1,
    Returned = 2,
    Destroyed = 3,
    Missing = 4,
    Unconfirmed = 5
}

/// <summary>
/// Immutable, game-object-free Courier observations. Stages need not occur in property order:
/// a reply may be prepared before arrival. False means not confirmed, not proof of rollback.
/// Overall success is reported only by AfDialogueResult.State after required return cleanup.
/// </summary>
public sealed class AfCourierReceipt
{
    public AfCourierTransportOutcome Transport { get; }
    public bool Dispatched { get; }
    public bool ReplyPrepared { get; }
    public bool Arrived { get; }
    public bool PayloadAccepted { get; }
    public bool DeliveryHistoryAccepted { get; }
    public bool ActionsAccepted { get; }
    public bool ReplyHistoryAccepted { get; }
    public bool ReplyDelivered { get; }
    public bool ContentsReturned { get; }

    internal AfCourierReceipt(AfCourierTransportOutcome transport, bool dispatched, bool replyPrepared,
        bool arrived, bool payloadAccepted, bool deliveryHistoryAccepted, bool actionsAccepted,
        bool replyHistoryAccepted, bool replyDelivered, bool contentsReturned)
    {
        Transport = transport; Dispatched = dispatched; ReplyPrepared = replyPrepared;
        Arrived = arrived; PayloadAccepted = payloadAccepted; DeliveryHistoryAccepted = deliveryHistoryAccepted;
        ActionsAccepted = actionsAccepted; ReplyHistoryAccepted = replyHistoryAccepted;
        ReplyDelivered = replyDelivered; ContentsReturned = contentsReturned;
    }
}

/// <summary>稳定英文 ID；列出未开放能力是为了让调用者探测，而非暗示存在对应执行方法。</summary>
public static class AfCapabilityIds
{
    public const string CatalogRead = "af.api.catalog.read";
    public const string NativeSubmit = "af.api.dialogue.native.submit";
    public const string SceneSubmit = "af.api.dialogue.scene.submit";
    public const string CourierSubmit = "af.api.dialogue.courier.submit";
    public const string ActionExecute = "af.api.actions.execute";
    public const string MemoryWrite = "af.api.memory.write";
    public const string ExtensionRegister = "af.api.extensions.register";
}

public sealed class AfCapabilityInfo
{
    public string Id { get; }
    public int ContractVersion { get; }
    public AfCapabilityState State { get; }
    public string ReasonCode { get; }

    internal AfCapabilityInfo(string id, AfCapabilityState state, string reasonCode)
    {
        Id = id ?? string.Empty;
        ContractVersion = 1;
        State = state;
        ReasonCode = reasonCode;
    }
}

public sealed class AfModuleCapabilityInfo
{
    public string Id { get; }
    public int ContractVersion { get; }
    public AfModuleCapabilityState State { get; }
    public string ReasonCode { get; }

    // V1 只发布目录，内部 port 从来不是子 MOD 可调用的执行接口。
    public bool IsExternallyCallable => false;

    internal AfModuleCapabilityInfo(string id, int contractVersion,
        AfModuleCapabilityState state, string reasonCode)
    {
        Id = id;
        ContractVersion = contractVersion;
        State = state;
        ReasonCode = reasonCode;
    }
}

public sealed class AfModuleInfo
{
    public string Id { get; }
    public int ContractVersion { get; }
    public IReadOnlyList<AfModuleCapabilityInfo> Capabilities { get; }

    internal AfModuleInfo(string id, int contractVersion, IEnumerable<AfModuleCapabilityInfo> capabilities)
    {
        Id = id;
        ContractVersion = contractVersion;
        Capabilities = new ReadOnlyCollection<AfModuleCapabilityInfo>(new List<AfModuleCapabilityInfo>(capabilities));
    }
}

/// <summary>
/// Detached、只读、没有游戏对象的目录快照。可在任意线程查询；不触发 LLM、游戏对象读取或动作。
/// Modules 仅包含首版接入的制作组接缝，不是整个 AF 功能总表。
/// </summary>
public sealed class AfFrameworkSnapshot
{
    public int ContractVersion => 1;
    public AfFrameworkState State { get; }
    public string ReasonCode { get; }
    public IReadOnlyList<AfCapabilityInfo> PublicCapabilities { get; }
    public IReadOnlyList<AfModuleInfo> Modules { get; }

    internal AfFrameworkSnapshot(AfFrameworkState state, string reasonCode,
        IEnumerable<AfCapabilityInfo> publicCapabilities, IEnumerable<AfModuleInfo> modules)
    {
        State = state;
        ReasonCode = reasonCode;
        PublicCapabilities = new ReadOnlyCollection<AfCapabilityInfo>(new List<AfCapabilityInfo>(publicCapabilities));
        Modules = new ReadOnlyCollection<AfModuleInfo>(new List<AfModuleInfo>(modules));
    }
}
