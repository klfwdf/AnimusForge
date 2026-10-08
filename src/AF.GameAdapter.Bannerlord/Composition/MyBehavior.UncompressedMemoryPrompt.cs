using System;using System.Collections.Generic;using System.Diagnostics;using System.Linq;
using TaleWorlds.CampaignSystem;
namespace AnimusForge;
public partial class MyBehavior {
private List<ConversationMessage> CaptureAndBuildUncompressedMemoryRoleMessages(Hero hero, int targetAgentIndex = -1, bool includeCurrentActiveSceneSession = false) => _memoryHistoryCommit.BuildUncompressedMemoryRoleMessages(hero, targetAgentIndex, includeCurrentActiveSceneSession);
private List<ConversationMessage> CaptureAndBuildUncompressedMemoryRoleMessagesById(string memoryId, string memoryName, int targetAgentIndex = -1, bool includeCurrentActiveSceneSession = false) => _memoryHistoryCommit.BuildUncompressedMemoryRoleMessagesById(memoryId, memoryName, targetAgentIndex, includeCurrentActiveSceneSession);
private static string ResolveCapturedMemoryLineSceneForPrompt(DailyMemoryLine line)
    {
        return AnimusForge.Refactor.Adapters.MemoryRecallInputCaptureAdapter.ResolveCapturedMemoryLineSceneForPrompt(line);
    }
}
