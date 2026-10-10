# Scene prompt package selection

Scene action classification and battle speech consume the
CN or EN corpus selected into `ModuleData/SceneActionPromptTexts.xml` by packaging.
Both packages retain bilingual UI; changing the UI language does not switch this
corpus. Existing action IDs, parser rules, authorization and request snapshots stay
unchanged. Package text is read once during host initialization, never on Tick.

Product compilation, replay execution, packaging and game acceptance are deferred
by the current task. Static evidence will be recorded after the complete slice.

## Implemented slice

- `SceneActionPromptPackage` reads the resolved package file once, publishes an
  immutable corpus, and never asks the game localization engine for its language.
- `SceneActionCatalogText` and `BattleSpeechText` use this corpus for the selected
  package language. Explicit other-language Core calls retain their embedded
  compatibility resources; the runtime adapters and unified postprocess route
  select the installed package.
- The pair has 221 entries: 215 existing catalog/speech entries and six runtime
  instructions/labels. The CN package deliberately removes the two historical
  instructions requiring Simplified Chinese output. Original embedded resources
  remain intact for explicit-language compatibility.
- Speech acceptance uses natural letters regardless of package language; Chinese
  and English sentence boundaries are both recognized. Protocol-only replies and
  the existing content/scope checks remain active.
- Speech system-prompt detection now matches the actual selected single/combined
  instruction. The former hardcoded Chinese headings were absent from both
  current resource versions, so neither edition reliably reached that replacement.
- The specialized preamble, failure instructions and battlefield fallback use the
  same package. English `(error:` responses are rejected beside the original
  failure markers.

## Evidence and remaining boundaries

`ScenePromptPackageContractTests.py`: PASS 270 static/data/simulation checks.
Existing scene catalog and battle-speech checks: PASS 494 and 131 respectively.
`git diff --check`: PASS. These checks do not execute the modified C#.

Root Compile registration, content mapping and EN XML selection are integrated by
the main owner, with exact instructions in `work/scene-package-registration.txt`.
That untracked note is a handoff aid, not a runtime dependency.

The ordinary-conversation preamble matcher still recognizes the upstream Chinese
producer in `ShoutBehavior.SceneConversationChains.cs`; its two raw matching
strings and three legacy error prefixes are retained compatibility literals.
When that upstream task contract gains package selection, its recognized English
form must be added to this adapter in the same change. This slice does not claim
all XihaiAction source literals, scene descriptions or external context facts are
translated. Old explicit-language Core overloads still exist by design.
