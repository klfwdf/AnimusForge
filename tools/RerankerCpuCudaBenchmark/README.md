# Independent CPU / CUDA reranker benchmark

Scope: standalone, offline benchmark of the existing AF reranker. No production source, model, rules, game deployment, build entry point, driver, global Python or CUDA installation may be changed. All downloaded packages and generated/build/run artifacts stay below the explicitly supplied workspace output directory.

The production reranker and model resolver are read live. A generated test-only reranker copy receives exactly one session-options hook; tokenizer, truncation, padding, score mapping, cache and batch/single fallback logic remain byte-for-byte unchanged. A second generated copy of the exact original verifies CPU identity. The harness provides only module-root and logger adapters; no game is loaded.

Compare deployed CPU runtime against matched-runtime CPU and CUDA separately. Keep ORT graph optimization at EXTENDED, default CPU thread settings, original FP32 model and disable CUDA TF32. Do not attribute an ORT-version improvement to CUDA.

Measure fresh-process initialization/first inference, cache-miss batch calls, cache-hit calls and a separate untimed provider-profiling pass. Snapshot source/model hashes before and after. GPU telemetry is device-wide nvidia-smi sampling, not exact per-process allocation; report both absolute peak and baseline delta. Never claim a different GPU or in-game FPS was tested.

Use synthetic public-lore-style Chinese inputs, not player conversation logs or exports. Save raw scores, ordering/top-k comparisons, numerical errors, raw timings and profiling provider assignments.

Implementation and commands are documented here when the runner is complete. This initial local checkpoint records test intent only.
