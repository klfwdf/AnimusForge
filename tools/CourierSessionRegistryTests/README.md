# Courier session registry checks

`run.py` checks the production loaded-session reset, copy-on-publish runtime
indexes, synchronized lookup, and persisted inbound receipt guard. It does not
load a real save or resolve live parties; `LIVE/SAVE=NOT-RUN`.
