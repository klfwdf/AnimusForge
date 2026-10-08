using System;using System.Collections.Generic;using System.Diagnostics;using System.Linq;
using TaleWorlds.CampaignSystem;
namespace AnimusForge;
public partial class MyBehavior {
// Observe existing session identity only. This capture must not allocate/start a dialogue session.
internal static string CaptureCurrentPromptMemorySessionKey()
{
    var owner = Instance;
    if (owner == null) return "";
    int scene = ShoutBehavior.TryGetCurrentSceneHistorySessionIdForHistoryPersistence();
    int dialogue = owner._memoryBusinessState._activeNativeConversationMemorySessionId;
    return scene >= 0 || dialogue >= 0 ? owner._memoryBusinessState.BuildCurrentMemorySessionKey(scene, dialogue) : "";
}

private List<ConversationMessage> CaptureAndBuildUncompressedMemoryRoleMessages(Hero hero, int targetAgentIndex = -1, bool includeCurrentActiveSceneSession = false) => _memoryHistoryCommit.BuildUncompressedMemoryRoleMessages(hero, targetAgentIndex, includeCurrentActiveSceneSession);
private List<ConversationMessage> CaptureAndBuildUncompressedMemoryRoleMessagesById(string memoryId, string memoryName, int targetAgentIndex = -1, bool includeCurrentActiveSceneSession = false) => _memoryHistoryCommit.BuildUncompressedMemoryRoleMessagesById(memoryId, memoryName, targetAgentIndex, includeCurrentActiveSceneSession);
private static string ResolveCapturedMemoryLineSceneForPrompt(DailyMemoryLine line)
    {
        return AnimusForge.Refactor.Adapters.MemoryRecallInputCaptureAdapter.ResolveCapturedMemoryLineSceneForPrompt(line);
    }
}
