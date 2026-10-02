# Bulletin / courier layout regression

Task-local scope: center the bulletin footer/archive and symmetric minor-news columns, remove the footer hint, compact reply buttons, and restore scrolling for complete impact summaries without altering content, links or commands. Keep Pen and mapped production XML aligned.

Exit gates: focused XML/layout/scroll-path tests, content contracts retaining approved baseline evidence, Pen screenshots and bounds verification, existing dual API + Bootstrap build without deployment. No game overwrite, no push. UI-only changes add no per-frame polling or new reflection.

Rollback checkpoint: current pre-change HEAD ab4b4b7f. Pen pre-edit subtrees captured through MCP in artifacts/bulletin-courier-ui-20261003/pen-before.txt. Other designs and worktree changes stay untouched.
