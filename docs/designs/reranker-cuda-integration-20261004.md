# Optional CPU / CUDA reranker integration

User scope: MCM CPU/GPU selection, CPU default, restart applies; identical model, tokenizer, scoring and retrieval rules. Optional CUDA worker isolates ORT 1.22/CUDA libraries from game CPU ORT 1.18. CPU fallback on missing dependency, startup/runtime failure, timeout. Serial worker IPC; native CPU retains existing behavior. No game deployment, push, global installs or one-click build changes.

Baseline 42a5eaa1. Exit gates: production-linked selection/protocol/fallback/concurrency checks, real CUDA process and CPU score equivalence, optional pack with dependency receipts, dual API builds. Offline acceptance does not cover 5060, game frame-time/VRAM or real MCM UI.
