using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge;

internal static class WorldDiplomacyPlayerDraftApplication
{
    internal static bool TryPrepare(string body, long generation, out WorldDiplomacyPlayerDraftInput input,
        out Func<CancellationToken, Task<WorldDiplomacyPlayerDraftResult>> run, out string error)
    {
        input = null;
        run = null;
        error = "";
        if (string.IsNullOrWhiteSpace(body)) { error = "请先写下标题或拟文要点。"; return false; }
        if (!SaveRuntimeGuard.IsCurrentGeneration(generation)) { error = "当前拟文窗口已因读档失效，请重新打开。"; return false; }
        WorldDiplomacyBehavior.GetDiplomaticDeclarationCharacterRange(out int minimum, out int maximum);
        WorldDiplomacyPlayerDraftInput snapshot = new WorldDiplomacyPlayerDraftInput(body, minimum, maximum, generation);
        string preference = DuelSettings.GetSettings()?.WorldDiplomacyPrompt ?? "";
        if (!WorldDiplomacyLlmClient.TryPrepareSingleCall(WorldDiplomacyPlayerDraftRules.BuildMessages(snapshot, preference),
            WorldDiplomacyPlayerDraftRules.GetOutputTokenBudget(snapshot), 90000, "WorldDiplomacyPlayerDraft", generation, out var call, out _))
        {
            error = "书记官暂时无法拟稿，请检查外交使用的事件/叛乱API或主API设置。";
            return false;
        }
        input = snapshot;
        run = async token =>
        {
            WorldDiplomacyApiCallResult result = await call(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            if (result?.IsOutputTruncated == true)
                return WorldDiplomacyPlayerDraftResult.Failed("稿件因输出上限被截断，原文已保留。请检查API输出上限。");
            if (result?.Success != true)
                return WorldDiplomacyPlayerDraftResult.Failed("书记官拟稿失败，原文已保留。请检查连接设置后重试。");
            return WorldDiplomacyPlayerDraftRules.Parse(result.Content);
        };
        return true;
    }
}
