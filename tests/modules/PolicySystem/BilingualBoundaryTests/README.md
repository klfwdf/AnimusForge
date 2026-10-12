# Policy bilingual boundary replay

Run from the repository root:

```powershell
python tests/modules/PolicySystem/BilingualBoundaryTests/run.py
```

This is a local verification entry for the 2026-10-11 diagnostic review. It requires
the saved pre-value directory at
`artifacts/localization/verification-20261011/policy-diagnostic-before-0926/`
to reproduce the prior exceptions and prove focused inverse preservation.

It compiles the actual parser, sanitizer and resource readers. UI language/module
path providers are test-only; UI projection methods and diagnostic expressions
are extracted unchanged from current production source. It checks the four
package/UI combinations and paired resource contracts; it does not initialize
Gauntlet, ONNX, Campaign or the complete AF dependency closure, and it does not
approve the full translated corpus semantically.
