using AnimusForge;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json.Linq;

int assertions = 0;
void Check(bool value, string name) { assertions++; if (!value) throw new Exception(name); }
var lease = new WorldDiplomacyRequestLeaseCoordinator();
Check(lease.TryClaim(" job ", 7, 900, 8000, out var snapshot), "claim");
var messages = JArray.Parse("[{\"role\":\"system\",\"content\":\"stable\"},{\"role\":\"user\",\"content\":\"history\"},{\"role\":\"assistant\",\"content\":\"rejected\"},{\"content\":\"repair\"},{\"content\":\"  \"}]");
var request = WorldDiplomacyLlmApplication.PrepareRequest(snapshot, messages);
messages[0]["content"] = "mutated";
messages.Clear();
Check(request.Prompt.Messages.Count == 4 && request.Prompt.Messages[0].Content == "stable", "detached frozen prefix");
Check(request.Prompt.Messages[2].Role == "assistant" && request.Prompt.Messages[3].Role == "user", "repair roles and order");
Check(request.Provider.TimeoutMilliseconds == 8000 && request.Prompt.MaxTokens == 900, "request budget");
Check(request.Trace.RuntimeGeneration == 7 && request.Stage == InteractionStage.MainReply, "trace generation");
var gateway = new Gateway();
var metadata = new LlmGenerateMetadata(isOutputTruncated: true, promptCacheHitTokens: 80, promptCacheMissTokens: 20, promptCacheCreationTokens: 10, promptUncachedTokens: 10);
gateway.Callback = (r, t) => Task.FromResult(new LlmGenerateResult(LlmResultStatus.Succeeded, "raw", 100, 11, "", metadata));
var result = await WorldDiplomacyLlmApplication.ExecuteAsync(snapshot.JobId, request, gateway, CancellationToken.None);
Check(result.Success && result.Content == "raw" && result.JobId == "job" && result.RuntimeGeneration == 7, "success receipt");
Check(!result.IsServiceFailure && result.IsOutputTruncated && result.PromptTokens == 100 && result.CompletionTokens == 11, "usage and truncation");
Check(result.PromptCacheHitTokens == 80 && result.PromptCacheMissTokens == 20 && result.PromptCacheCreationTokens == 10 && result.PromptUncachedTokens == 10, "cache accounting");
foreach (LlmResultStatus status in Enum.GetValues<LlmResultStatus>())
{
    gateway.Callback = (r,t) => Task.FromResult(new LlmGenerateResult(status, "partial", 3, 4, "error"));
    result = await WorldDiplomacyLlmApplication.ExecuteAsync("job", request, gateway, CancellationToken.None);
    Check(result.Success == (status == LlmResultStatus.Succeeded), "status " + status);
    Check(result.IsServiceFailure == (status != LlmResultStatus.Succeeded), "failure policy " + status);
}
using (var cancel = new CancellationTokenSource())
{
    cancel.Cancel();
    gateway.Callback = (r,t) => { Check(t == cancel.Token, "caller token passed through"); t.ThrowIfCancellationRequested(); return null; };
    result = await WorldDiplomacyLlmApplication.ExecuteAsync("job", request, gateway, cancel.Token);
    Check(!result.Success && result.IsServiceFailure && result.Error.Contains("OperationCanceledException"), "exception mapping preserved");
}
gateway.Callback = (r,t) => throw new InvalidOperationException("provider fault");
result = await WorldDiplomacyLlmApplication.ExecuteAsync("job", request, gateway, CancellationToken.None);
Check(!result.Success && result.Error.Contains("provider fault"), "unexpected gateway exception");
Check(lease.TryRelease("job", 7), "release first generation");
Check(lease.TryClaim("job", 8, 900, 8000, out _), "same saved ID in next generation");
Check(!lease.TryRelease(result.JobId, result.RuntimeGeneration) && lease.IsRunning, "late completion cannot release new request");
Check(!WorldDiplomacyJobRuntimeCoordinator.IsCurrentCompletion("job", 7, 8, false), "late completion rejected");
var budget = new WorldDiplomacyLlmBudget();
var logs = new List<string>();
for (int i=0; i<12; i++) { Check(budget.TryConsume(1,12,false,logs.Add), "peek"); Check(budget.TryConsume(1,12,true,logs.Add), "consume"); }
Check(!budget.TryConsume(1,12,false,logs.Add) && !budget.TryConsume(1,12,true,logs.Add) && logs.Count == 1, "daily ceiling and one log");
Check(budget.TryConsume(2,12,true,logs.Add), "day reset");
budget.Reset();
Check(budget.TryConsume(2,1,true,logs.Add) && !budget.TryConsume(2,1,true,logs.Add), "runtime reset");
Console.WriteLine($"DPL-080 LLM application: {assertions} assertions passed");

sealed class Gateway : ILlmGateway
{
    public Func<LlmGenerateRequest,CancellationToken,Task<LlmGenerateResult>> Callback;
    public Task<LlmGenerateResult> GenerateAsync(LlmGenerateRequest request, CancellationToken token) => Callback(request, token);
}
