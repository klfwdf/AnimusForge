# Streaming dialogue presentation

Run `python -X utf8 -B tests/AF.GameAdapter.Bannerlord/StreamingDialogueRevealTests/run.py` from the repository root.

Compiles the actual incremental reveal state, ConversationHelper, scroll widget and the two unchanged extracted Overlay presentation methods. Native VM, scrollbar measurement, logging and retry-error localization are explicit doubles; the sanitizer is extracted from production. Checks 20-glyph pacing (including fast one-glyph chunks and slow network stalls), final-text draining, Unicode boundaries, trusted final links, stale generation/owner/VM cleanup, stream-off legacy TTS/display/manual scroll, follow-after-layout, manual reading after completion and scene/map prefab binding. Outputs and source hashes stay under a fresh workspace artifact directory.

This is offline presentation evidence. It does not run Bannerlord, render text, validate real HTTP/TTS playback, change settings or test old saves. Existing native focus/pause, owner-retirement and 1.3/1.4 continue-hit replays cover adjacent boundaries.
