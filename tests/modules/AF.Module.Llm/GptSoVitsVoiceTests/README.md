# Simple GPT-SoVITS voice tests

Links the actual production catalog, local gateway, cloud gateway and contracts. No model or native playback is started. All HTTP replies use an injected handler.

Pass a fresh fixture directory under this repository's artifacts, then optionally the compiled implementation DLL, a read-only voice manifest, and exact read-only dependency directories:

```powershell
dotnet run --project tests/modules/AF.Module.Llm/GptSoVitsVoiceTests/GptSoVitsVoiceTests.csproj -c Release -- <fresh-fixture-directory> <implementation-dll> <manifest.json> <pinned-reference-directory> <game-bin-directory> <private-runtime-directory>
```

Coverage: gender-only random selection, retained IDs across catalog ordering/load, manual selection, missing voice/no opposite-gender fallback, traversal/duplicate/provenance rejection, reference tampering/size limits, single local HTTP attempt without cloud credentials, WAV/error/cancellation paths, unchanged cloud protocol, and actual compiled persona-record JSON/legacy JSON round trips.

Catalog-only verification of a prepared or installed pack (without loading a game DLL):

```powershell
dotnet run --project tests/modules/AF.Module.Llm/GptSoVitsVoiceTests -c Release -- <fresh-fixture-directory> --manifest <manifest.json>
```

The categorized pack tests additionally cover default-only random pools, explicit custom selection (including player-selected cross-gender custom voices), deterministic retired-ID aliases, same-voice manual mood/reference/transcript pairing, missing-mood fallback and actual reference hashes. `--manifest` does **not** validate compiled persona records; use the original DLL form for those checks.

`TtsRequestLifetimeRegressionTests/run.py` runs the complete engine/consumer lifetime suite, including the latest OpenAI streaming owner, local sentence jobs, postprocess-tone deadlines, cancellation and provider isolation. `run_local_producer.py` is only the smaller local-producer subset, not a replacement.

The current integration and evidence are recorded in the [main ledger](../../../../docs/animusforge-refactoring-and-repository-reorganization-plan.md#tts-final-integration-20261017). Main-reply `TTS_TONE` tokens are defensive stripping/legacy compatibility only; live local tone comes from the existing validated postprocess mood, via typed per-reply metadata. No extra AI request or main-prompt instruction is added. `LocalTtsTonePresentationTests` checks real text consumers; `GptSoVitsRuntimeTests` separately verifies managed L/U process ownership.

Not covered by this catalog suite: real Campaign `.sav` loading, encyclopedia rendering/clicks, the live Python API, speech quality or battle FPS. Runtime fixture and real-bundle synthesis results must be reported separately from live-game acceptance.

## Peaceful meeting gate regression

`BattleGateTests` links the production `GptSoVitsBattleSpeechGuard` and `GptSoVitsVoiceRuntime`.
Native Mission, meeting/duel owners and the final `CancelLocalPlayback` boundary are test doubles.
It covers the current-owner peaceful field-meeting exception, combat escalation/duel cancellation edges,
ordinary field battles, siege/sally-out exclusions, stale or missing meeting owners, repeated ticks,
late owner attachment, cross-mission removal and ordinary town/map cleanup. The production owner exposes
only the existing started-instance Mission reference, which is cleared by its existing removal lifecycle.
This suite does not prove real-game owner callback order, playback timing or hearing the resulting audio;
the separate complete TtsEngine lifetime suite covers real local-job cancellation without cancelling cloud jobs.
