# Bulletin / courier layout regression

Task-local scope: center the bulletin footer/archive and symmetric minor-news columns, remove the footer hint, compact reply buttons, and restore scrolling for complete impact summaries without altering content, links or commands. Keep Pen and mapped production XML aligned.

Exit gates: focused XML/layout/scroll-path tests, content contracts retaining approved baseline evidence, Pen screenshots and bounds verification, existing dual API + Bootstrap build without deployment. No game overwrite, no push. UI-only changes add no per-frame polling or new reflection.

Rollback checkpoint: current pre-change HEAD ab4b4b7f. Pen pre-edit subtrees captured through MCP in artifacts/bulletin-courier-ui-20261003/pen-before.txt. Other designs and worktree changes stay untouched.

Additional approved scope: restore the missing lower-right parchment ornament by reflecting existing ink locally, including the lower inner vertical rule explicitly identified in the user red-box correction. `repair_parchment.py` is offline-only; it keeps 1730x1100 RGBA, preserves alpha and all pixels outside `(1390,790,1685,1068)`. Original PNG is also retained locally in the evidence directory. No runtime image processing.

Run with Python + Pillow: `E:\PYTHON\python.exe tests/content/BulletinCourierLayoutTests/run.py` (13 tests). Historical baseline hashes are not refreshed. The older J15 umbrella oracle has a pre-existing content-map count failure; it is not a PASS for this task.
