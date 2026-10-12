# Illustrator bilingual package work

Scope: remove runtime Chinese literals throughout this module while completing
actual English context, director and image-request paths. UI remains selected by
the game language; prompt text is selected by the installed CN/EN package. Raw
protocol, mode, persisted and compatibility values retain their original meaning
and bytes. Unknown and player-authored text must not be translated by substring.

Baseline inventory: 1,431 string occurrences, 1,288 unique values. Source-only
counts are not proof of untranslated UI. Existing localization resources and
their consumers must be reused when applicable. No gameplay, persistence, paid
API requests, rendering, timing or capture behavior is in scope.

Implementation proceeds through the assigned module before consolidated static
validation. Product compilation, replay, packaging, deployment and live-game
acceptance remain deferred. Shared registration changes are handed to the main
owner in work/illustrator-package-registration.txt.
