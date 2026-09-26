# Courier session creation checks

`run.py` checks production outbound and inbound session publication order,
runtime-index registration, generation start, and the prewritten external
letter no-LLM path. Party creation and game UI remain `LIVE=NOT-RUN`.

`run_admission.py --dotnet <absolute SDK executable>` compiles the actual draft,
letter-window callbacks, confirmation and reset methods against explicit game/UI
fixtures. It checks old-window/new-draft and same-draft/new-step races, replaced
or sealed owners, save generation, main-thread admission, partial dispatch and
reentrant replacement. `--ref fb4af2d8` reproduces the pre-J14b defects;
`--mutate ignore-revision|ignore-generation|ignore-owner` must compile and fail a
named behavior assertion. Dispatch effects here are fixtures, not full transport
or public Courier API acceptance.
