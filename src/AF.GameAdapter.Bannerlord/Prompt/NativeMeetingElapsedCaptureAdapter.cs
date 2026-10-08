using System;
using TaleWorlds.CampaignSystem;

namespace AnimusForge.Refactor.Adapters;

internal static class NativeMeetingElapsedCaptureAdapter
{
    internal static NativeMeetingElapsedSnapshot Capture(Hero hero, CharacterObject character, NpcDataPacket npc, int agentIndex)
    {
        double hours = double.NaN;
        if (Campaign.Current?.GameStarted == true)
        {
            try { hours = CampaignTime.Now.ToHours; } catch { }
        }
        string memoryId = "";
        Hero subject = hero ?? character?.HeroObject;
        if (subject != null) memoryId = CampaignCharacterRecordCaptureAdapter.GetMemoryHeroId(subject);
        else SceneAgentIdentityPromptCaptureAdapter.TryResolveWildernessNonHeroMemory(npc, null, character, agentIndex, out memoryId, out _);
        return MyBehavior.Instance?.CaptureNativeMeetingElapsed(memoryId, hours) ?? new NativeMeetingElapsedSnapshot { NowHours = hours };
    }
}
