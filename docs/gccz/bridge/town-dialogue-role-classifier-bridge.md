# Town dialogue role classifier bridge

- `TownDialogueRoleClassifier` is the reusable source of truth for the six GCCZ town dialogue roles: accompanying noble, noble prisoner, player companion, settlement notable, ordinary soldier, and ordinary civilian.
- The AF adapter supplies live Bannerlord facts only. Role priority is prisoner, companion, accompanying noble, settlement notable, ordinary soldier, ordinary civilian, then a safe unknown fallback.
- Ordinary soldier execution tags require both the `OrdinarySoldier` role and verified allied-soldier authority. A companion or noble cannot inherit soldier execution tags merely because the Hero belongs to the main party.
- Robbery tags are available only to settlement notables and ordinary civilians. Noble prisoners remain outside the ordinary robbery route.
- The core records the required memory lifetime policy: the four named roles use persistent personal memory, while ordinary soldiers and civilians use scene-local memory.
- `TownDialogueMemoryPolicy` is the executable memory-lifetime source of truth. Persistent access additionally requires a live named Hero; an unknown role cannot inherit Hero memory merely from a prompt marker.
- During an active GCCZ town stage, `AfGcczShoutBridge` applies that policy to compressed history, uncompressed memory messages, targeted facts, player speech, and NPC speech. Outside an active GCCZ town, normal AF memory behavior is unchanged.
- Main reply, immediate reaction, and postprocess prompts now consume the same role marker and memory-scope code. Localized prompt resources explain the six codes without adding a second classifier.
