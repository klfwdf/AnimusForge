# bekko ONNX source delivery — 2026-11-02

The user explicitly requested pushing the new bekko ONNX model, deleting the old embedding/reranker models, and removing the CPU/GPU setting.

## Shipped source payload

- AnimusForge/ONNX/bekko/model.onnx — 199,300,251 bytes, Git LFS.
- AnimusForge/ONNX/bekko/tokenizer.json — 34,363,442 bytes, Git LFS.
- AnimusForge/ONNX/bekko/config.json — 1,749 bytes.
- Git LFS is required when cloning or updating these assets; run git lfs pull after checkout if automatic downloads are disabled.
- No embeddings/vector cache, game data, credentials, old bge models, or reranker weights are included. No upstream release identifier or license is invented from the local filenames.

## Runtime and settings

Both CN and EN use the same bekko BPE model with mean_all pooling. Missing bekko files report a missing model; the loader no longer searches ONNX/en, ONNX/onnx, or the root legacy embedding files. Legacy cross-encoder initialization stays unavailable and never opens the model or starts a CUDA worker. Existing internal score callers continue using their embedding fallback.

The RerankerDeviceDropdown property/backing field, terminal registration, terminal language mappings, and associated CN/EN resource keys are removed. Old JSON/preset fields no longer have an active property or runtime reader; player configuration files are not rewritten. There is no replacement CPU/GPU setting or configuration copy.

The old installed ONNX root config, manifest, quantized model/weights and tokenizer files were explicitly removed; the installed reranker directory was already absent. bekko and its existing runtime vector cache are preserved. Historical test fixtures and prior build candidates are outside this deletion scope.

The repository manifest now describes only the bekko payload. The existing one-click build/cover scripts and the separate-model policy of the unified CN/EN ZIPs are unchanged; this task does not create release ZIPs or silently bundle models into them.

## Evidence and limits

This session explicitly prohibits tests, gates, checksum verification and installation readback. Only source/file/history reads and requested edits, Git LFS object preparation and uploads are performed here; no tests, model inference, SHA verification, game launch or new production compilation is claimed. The changed runtime/settings source has not been deployed as a new DLL by this source-push task.

Git LFS necessarily computes its content-addressed object identity as part of normal storage/upload; no separate verification hashes are generated or compared.

Rollback is by focused inverse commits relative to the local pre-change checkpoint 5caf0c74f, preserving unrelated authors' edits and avoiding history rewrites. Restoring deleted old runtime models requires an explicitly authorized prior package or candidate; there is no automatic deployment rollback.
