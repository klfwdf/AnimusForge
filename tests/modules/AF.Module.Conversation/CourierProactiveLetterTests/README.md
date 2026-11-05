# Courier proactive-letter checks

`run.py` checks the production Campaign tick order, incremental batch/frame
budget, current-scan identity, active-inbound guard, and notable sender rejection
before expensive candidate evaluation and again before courier party creation
(including test mode). These sender guards are source-contract checks, not live
gameplay evidence. It does not measure a
real Bannerlord frame or create a live courier; `LIVE/performance=NOT-RUN`.
