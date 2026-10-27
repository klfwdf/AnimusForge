# Equipment prompt slot regression

Run `python tests/AF.GameAdapter.Bannerlord/EquipmentPromptCaptureTests/run.py`.
The runner compiles unchanged spans of the two production readers with explicit
equipment fakes: twelve spawn slots and five live weapon slots. It verifies empty
armor, populated armor, all five hero weapon fallback slots, spawn precedence,
NPC summaries, civilian context and absence of even caught bounds exceptions.
It does not run Bannerlord or validate native engine state or localized resources.

`--baseline` uses the task's preserved pre-edit files and must fail the empty-armor
weapon-read assertion. Generated projects stay in the task artifacts directory.
