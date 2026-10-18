# Primary API streaming option replay

Runs the actual Debug `AnimusForge.dll` with both primary transport override seams
installed. No real HTTP request, provider credential, MCM disk save, or game process
is used. The candidate path/SHA/build marker and the copied dependency manifest
are checked by the shared `ReplayCandidateInput` and dependency tooling.

## Run

Build the dual implementation through the existing unified build script (without
Deploy; produce the matching local Stage). Then run `run.py` with explicit absolute paths:

```powershell
python -B tests/replay/PrimaryStreamingOptionReplayTests/run.py `
  --candidate "<current Debug 1.4 AnimusForge.dll>" `
  --module-data "<matching Stage ModuleData directory>" `
  --game-root "<Bannerlord root>" `
  --reference-dir "<the pinned 1.4 reference directory used by that build>" `
  --harmony-module "<Bannerlord.Harmony module root>" `
  --mcm-module "<Bannerlord.MBOptionScreen module root>" `
  --ui-module "<Bannerlord.UIExtenderEx module root>" `
  --private-runtime "<verified private runtime DLL directory>"
```

Each invocation creates a new output below repository artifacts. It never reuses
or removes an old bin/obj directory. The optional `--suite PrimaryLlmGatewayReplayTests`
uses the same validated candidate/dependencies for the existing primary regression suite.

## Coverage

18 actual-DLL cases: speech-forced SSE and preference restoration, sentence
typewriter hold/surrogate safety, Native incomplete-preview rejection, default-off and terminal/MCM shared setting, JSON/SSE routing,
complete reply accumulation, preview/final separation, max_tokens/temperature/forced
thinking overrides, finish_reason without DONE, silent EOF, malformed chunks,
partial preview rejection, caller cancellation, setting changes during send,
HTTP failure, stale response rejection, thinking-control retry and bounded
non-stream fallback before any visible content.

An isolated module fixture receives a copy of candidate static ModuleData. Runtime
logs/settings stay in that fixture, not the installed module. References must be
managed DLLs; pin any additional named dependencies to the same game version.

The option applies at the shared primary request entry. Existing preprocessing,
action postprocessing and event/weekly requests are not changed. Callbackless
channels use SSE when enabled but still publish only a complete reply. Turning the
option off disables the formerly callback-selected native-conversation stream only
when local/cloud speech is also off. Enabled speech forces the effective choice but
never changes the persisted manual option.
The option is captured per request; an already running request is not rerouted.

The existing PrimaryLlmGateway replay's partial-error expectation intentionally
changes from accepting partial completion to preview-only + error. Cancellation now
propagates without completing a partial reply. The NonStreamingTransport suite
executes the unchanged non-stream core with an exact inverse method-name projection;
its original reviewed hash remains enforced rather than refreshed.

## Acceptance boundaries

Synthetic HTTP and compiled candidate PASS are not Bannerlord UI, actual MCM
persistence, real-provider SSE, TTS or old-save acceptance. Those remain NOT_RUN.
No deployment, game overwrite, push, or one-click build-script modification.
