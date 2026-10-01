# Memory summary run ownership proof

> 根整合状态（2026-09-16）：生产源码 `6e419f6d` 已本地提交，最终六Stage与4DLL1056元数据通过；整体收尾仍ACTIVE。当前边界以[总交接](../../../../docs/handoffs/2026-09-16-parallel-closeout-handoff.md)为准，以下包内记录保留原验证上下文。


Production path: `MyBehavior.ProcessMemorySummaryQueueAsync` obtains one `MemorySummaryRunOwner.Lease`; the same token crosses planning, capture, retry/waves, parse, each Apply/Mark and notification. Reset removes authority; disposal compares the lease, so an old run cannot release its replacement. No persisted identity, HTTP cancellation, extra queue, Prompt or algorithm is introduced.

## Reproduce

Set `DOTNET_EXE` to an installed SDK runtime; use Python 3.10+ from the repository root.

```text
python tests/modules/AF.Module.Memory/MemorySummaryRunOwnerTests/run.py
python tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_business.py --run-scope-cases
python tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_business.py --run-scope-cases --run-owner-baseline
python tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_terminal.py --run-scope-cases
python tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_terminal.py --run-scope-cases --run-mutate release-replacement
python tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_terminal.py --run-scope-cases --run-mutate ignore-run-authority
python tests/modules/AF.Module.Memory/MemorySummaryRunOwnerTests/source_parity.py
```

- Owner: 47 checks including concurrent admission. Three primitive faults execute and fail.
- Business: original 36 cases plus three same-generation replacement cases. Reviewed `155f1b7a` baseline keeps 36 original passes but fails all three new cases. Current 39 pass.
- Terminal: 85 existing writer/capture/parse/Apply/Mark/recovery scenarios plus 10 same-generation retirement scenarios for daily, major and overview, failed requests, retries and a result parsed before reset. Current 95 pass. Both run-owner mutations compile and fail the new cases (the release-replacement fault has direct authority failures, not a tool timeout).
- Test-only old busy assertions are explicitly conditional: retired old instances may now release their own runtime-only lease; they may not release a replacement. Existing data, notification and write checks remain. The old `worker-release` mutation is retired because lease disposal is intentionally engine-free and safe on workers; actual write dispatch mutants remain.
- `source-review.json` contains precise reviewed hunks against `155f1b7a`. Its inverse validates the live source and preserves previous proof hashes instead of blindly refreshing their baselines. Windows separators are normalized before lookup.

The business fixture scripts provider completion; terminal/captured tests execute actual request/source/parser/writer methods with controlled game/provider/clock seams. These are not Bannerlord LIVE/SAVE or hard memory/time budget acceptance. Character copying, raw fingerprint and final atomic validation remain separately tracked performance work.


## 2026-10-01 历史入口维护

当前 source proof 绑定批准候选 `f6e2ead7` 的具名 owner 消费者及现有生产依赖审查 hash；不再以整个旧 MyBehavior/Shout/Courier 根文本阻塞迁移。历史逆向/快照只保留为历史材料，本轮未重验旧整类逆向。当前真实生产方法的运行/有效负控仍独立执行。`--run-root` 只创建新仓内输出，runner 安全改动以 `runnerSafetyChanges` 精确 before/after 记录，不刷新原生产 hash。
