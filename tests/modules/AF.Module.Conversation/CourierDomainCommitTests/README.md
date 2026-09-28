# Courier domain commit tests

Validates the production Courier arrival-time domain commit and reply-wait owners after their
J10 relocation. The audit keeps the delivery/session identity gates, one-shot economy reservation,
single history fan-out, and pause-lock release ordering executable without launching Bannerlord.

```powershell
python tests/modules/AF.Module.Conversation/CourierDomainCommitTests/run.py
python tests/modules/AF.Module.Conversation/CourierDomainCommitTests/run.py --mutate drop-delivery-guard
python tests/modules/AF.Module.Conversation/CourierDomainCommitTests/run.py --mutate release-before-active-check
```

Both mutations must fail a named assertion.
