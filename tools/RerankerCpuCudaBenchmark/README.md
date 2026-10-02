# Independent CPU / CUDA reranker benchmark

Standalone offline test only: no production source, model, rules, game deployment, build entry point, driver, global Python or CUDA installation is changed. Downloads, NuGet caches, generated source, builds, profiling and results remain under the supplied repository `artifacts/` directory. The game model is opened read-only in its existing location.

## Reproduce on this machine or RTX 5060

Requirements: Windows x64, .NET Framework 4.7.2+, a .NET SDK, Python 3.10+ with `requests`, NVIDIA driver and `nvidia-smi`. No globally installed CUDA Toolkit is required. Do not run the game during the offline baseline; close unrelated GPU-heavy tasks. Each output directory should be dedicated to one run. Use the actual game module path on that machine.

```powershell
python tools/RerankerCpuCudaBenchmark/self_test.py
python tools/RerankerCpuCudaBenchmark/prepare_dependencies.py --output artifacts/reranker-cpu-cuda-20261003/deps
python tools/RerankerCpuCudaBenchmark/benchmark.py --module-root 'F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\AnimusForge' --output artifacts/reranker-cpu-cuda-20261003 --rounds 3 --repetitions 10
```

The commands above never call project build/deploy/package entry points or install system-wide packages. DLL lookup changes apply only to the independent child process. Downloads are pinned: ORT Windows GPU / managed 1.22.0, cuBLAS 12.8.4.1, cuDNN 9.8.0.87, CUDA runtime 12.8.90, cuFFT 11.3.3.83. NVIDIA wheels are unpacked rather than pip-installed; package URLs, SHA256 and file lists are recorded. Approximate downloads are 1.6 GiB compressed; the full local dependency/build/output footprint is larger.

`--build-only` builds independent executables without inference. `--skip-build` reuses those executables. Preserve each completed output directory before rerunning; raw result names are deterministic and a rerun can overwrite earlier test results in that directory.

## Fidelity and comparisons

- Compile a generated copy of the production C# reranker. Insert exactly one `BenchmarkHooks.Configure(sessionOptions)` call after its existing EXTENDED graph-optimization setting. Reversing this insertion recovers the original source bytes exactly. No tokenizer, 512-token truncation, padding, score mapping, cache or batch/single fallback logic is changed.
- Compile the exact original production model resolver. Host adapters only supply the read-only module root and capture logger calls. The production source has an unused `using TaleWorlds.Engine`; the harness supplies an empty marker namespace, not any simulated game API.
- Both executables target net472/x64, like the mod. The deployed CPU baseline uses the actual original ONNX DLLs. Matched CPU and CUDA use identical ORT 1.22.0 native and managed DLLs; compare those two to isolate the CUDA effect. Never attribute an ORT-version improvement to the GPU.
- Use the same original model, graph optimization, CPU default thread settings and input candidates. CUDA TF32 is disabled. No FP16, quantization, new retrieval rules, I/O binding or candidate reduction is used.
- Synthetic Chinese public-lore-style inputs avoid exporting player logs or conversations. Batch sizes are 1/4/8/16, with short, medium and long/truncated inputs. This is a workload fixture, not a live-world-book acceptance oracle.

## Measurements

Fresh processes measure initialization and first inference for each shape. Three unmeasured calls warm each shape before ten measured misses and ten measured hits. The harness clears the existing score dictionary outside the measured interval to force cache misses; cache-hit calls then use its unchanged production behavior. Every call must return all finite scores and must not silently fall back to per-document inference. Rotate variant order over three process rounds.

GPU telemetry uses device-wide `nvidia-smi` sampling every 200ms. Report absolute peak and delta from the immediately preceding baseline; these are estimates, not precise per-process VRAM, and short spikes or other processes can affect them. Profiling runs in a separate process so kernel instrumentation does not contaminate reported latency. Require actual `CUDAExecutionProvider` kernel events and list any CPU operators.

Compare raw scores with an explicitly diagnostic 1e-5 absolute tolerance; separately report top-1, top-2 set and full ordering. Numerical tolerance must never hide ranking differences. Preserve model/source hashes, raw scores, timings, telemetry, profiling and failed build/run diagnostics.

## Output and limitations

Read `REPORT.md`, `summary.json`, `source-derivation.json`, `protected-before.json` / `protected-after.json`, `dependency-receipt.json` under `deps/`, and `runs/` / `profiles/` in the chosen output directory. The local report location is `F:\AnimusForge-main\artifacts\reranker-cpu-cuda-20261003\REPORT.md`.

The development machine is RTX 4060 Laptop 8GB, not RTX 5060. Offline model speedup does not establish in-game FPS, GPU-memory safety under game load, concurrent request safety, missing-driver fallback or all-world-book equivalence. Actual model graph and dependency compatibility on RTX 5060 still requires rerunning there. Preserve CPU as the production default until a separately authorized integration and live-game test passes.

Rollback: only these tool files and this dedicated artifacts directory were added. Remove or archive them if unwanted; do not reset existing worktree changes or modify the game. Local intent checkpoint: `313a8d33`.
