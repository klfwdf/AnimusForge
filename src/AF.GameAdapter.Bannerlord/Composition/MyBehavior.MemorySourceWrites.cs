using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class MyBehavior
{
    // Editor callbacks outlive their window and can arrive after a same-owner load.
    // The caller must capture generation when opening, not when Save is clicked.
    private bool IsMemorySourceEditorCurrent(long generation)
    {
        return TWParallel.IsMainThread() && ReferenceEquals(Instance, this)
            && SaveRuntimeGuard.IsCurrentGeneration(generation)
            && ReferenceEquals(Campaign.Current?.GetCampaignBehavior<MyBehavior>(), this);
    }

    // Legacy void facades cannot report durable acceptance. Keep main-thread calls
    // synchronous; background calls only publish owner/generation-bound work.
    private static bool DeferMemorySourceWriteIfNeeded(Action<MyBehavior> write, string source)
    {
        if (TWParallel.IsMainThread()) return false;
        MyBehavior owner = Instance;
        long generation = SaveRuntimeGuard.CaptureGeneration();
        if (owner == null)
        {
            Logger.Log("CompressedMemory", "[WARN] memory write rejected without owner: " + source);
            return true;
        }
        _ = owner.RunMemorySummaryMainThreadAsync(generation, delegate
        {
            write(owner);
            return true;
        });
        return true;
    }

    // These DTO copies retain only game-object identity references. No properties
    // on Item/Character/Party/Settlement are read until the queued owner executes.
    private static List<RewardSystemBehavior.RewardItemInfo> CopyMemoryRewardOptions(List<RewardSystemBehavior.RewardItemInfo> values)
    {
        return values?.Select(x => x == null ? null : new RewardSystemBehavior.RewardItemInfo
        {
            Item = x.Item, StringId = x.StringId, PromptStringId = x.PromptStringId,
            ModifierStringId = x.ModifierStringId, Name = x.Name, Count = x.Count,
            GuidePrice = x.GuidePrice, EquipmentElement = x.EquipmentElement,
            IsPrivateEquipment = x.IsPrivateEquipment
        }).ToList();
    }

    private static List<PartyTransferPromptEntry> CopyMemoryPartyOptions(List<PartyTransferPromptEntry> values) => MemoryEntityIdentityBannerlordAdapter.CopyMemoryPartyOptions(values);

    private static List<SettlementTransferPromptEntry> CopyMemorySettlementOptions(List<SettlementTransferPromptEntry> values) => MemoryEntityIdentityBannerlordAdapter.CopyMemorySettlementOptions(values);
}
