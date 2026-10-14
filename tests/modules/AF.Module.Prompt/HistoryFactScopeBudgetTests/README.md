# Current-fact identity and historical request budget

Run `python tests/modules/AF.Module.Prompt/HistoryFactScopeBudgetTests/run.py --out <new workspace output directory>`.

Links the complete production message assembler and DTO; extracts the real
history-copy, ordering and budget methods. CN and EN use their actual Prompt
resources. Checks same-text distinct events, next-turn scope, unknown identity,
explicit Native current facts, pending facts not yet in history, session identity,
count/character budgets, whole current-fact preservation and original-history
immutability. The actual news-mode predicates are exercised for all four settings
combinations; the two real application capture bindings are checked as source.

Formatting/projection leaves and game identity are controlled. This does not run
a real LLM, mission, old save or complete Native/Scene/Courier conversation. The
old broad history runners' unrelated missing dependencies are not suppressed.

Only historical facts in request copies are limited: at most the configured
history count (hard ceiling 64) and 12,000 characters. Current facts are kept whole.
The original histories and persistence records are not pruned or rewritten.
