# YJ preset and shared API request regression tests

Run from the repository root:

```powershell
dotnet run --project tests/modules/AF.Module.Llm/YjThinkingCompatTests/YjThinkingCompatTests.csproj -c Release
```

Links the actual `YjThinkingCompat` and `LlmApiCompat` production sources. Only
DuelSettings reasoning-effort string constants are stubbed; this is not a live
MCM, network, game, or full transport acceptance test. Credentials are synthetic.

Covers new/legacy exact-host matching, Gemini thinking controls, negative hosts,
OpenAI and Anthropic authentication headers, payload conversion, preset chat/models
URLs, query/fragment placement, and proxy-prefix `/v1` retention. The final
`handshake-thinking` finding intentionally documents the remaining coverage gap:
with no max_tokens, Anthropic conversion defaults to 1024 and drops enabled thinking.

2026-10-02 evidence: new URL assertion failed against the original production
parser, then all assertions passed after repair; the existing Protocol runner's
13 cases also passed. Runtime MCM persistence and real-provider behavior remain
NOT_RUN. The optional `YjThinkingSource` MSBuild property can select an explicit
source file for negative-control runs.
