# CPU / CUDA device integration

Links the actual production reranker, model resolver, device selector, pipe protocol and process client. Only settings, module root and diagnostics are stubbed. Worker links the same production tokenizer/batching/scoring source, compiled with its own CUDA session factory. No game model or native DLL is overwritten.

Build `RerankerDeviceTests.csproj` with `CpuRuntimeDir` pointing to the game's existing CPU runtime directory, `RestorePackagesPath` and `BaseIntermediateOutputPath` under a fresh repository `artifacts` folder. Copy `onnxruntime.dll` and `onnxruntime_providers_shared.dll` from that CPU directory beside the test executable. Build the optional pack with `tools/RerankerCudaWorker/build_optional_pack.py`.

Run:

```powershell
python tests/modules/AF.Module.Knowledge/RerankerDeviceTests/run_integration.py --module-root "<game>/Modules/AnimusForge" --pack "<optional pack>/AnimusForge/OptionalRuntimes/RerankerCuda" --host "<test output>/RerankerDeviceTests.exe" --output artifacts/cuda-integration-run
```

The output directory must be new. Fixtures use hard links or copies of model and optional runtime files, always read-only as inputs. It checks CPU/invalid selection/missing component/real GPU/terminated GPU modes, concurrent initialization, serialized concurrent CUDA calls, cached scores, single/batch and 512-token fixture equivalence. Fault fixtures execute the real process client with wrong version, crash, timeout, malformed count and NaN replies. Profiling requires actual CUDA kernel events. Complete game-model and CPU-DLL hashes are compared before/after.

2026-10-04 evidence: `artifacts/reranker-cuda-integration-20261004/verified-integration/summary.json`. 22 fault checks; CPU/invalid/missing each17, GPU19, GPU-kill24. Initial kill test found a null result overwriting CPU's fallback score list; fixed and rerun. First profile lookup expected the configured prefix, but ORT wrote its default `onnxruntime_profile__*.json`; corrected discovery and rerun, not suppressed.

These are offline engine tests, not MCM UI, live Bannerlord FPS/VRAM, RTX 5060 or world-book acceptance. Integration timing includes the IPC path and is not a rigorous throughput benchmark.
