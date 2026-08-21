# Town dialogue role classifier bridge

- `TownDialogueRoleClassifier` is the reusable source of truth for the six GCCZ town dialogue roles: accompanying noble, noble prisoner, player companion, settlement notable, ordinary soldier, and ordinary civilian.
- The AF adapter supplies live Bannerlord facts only. Role priority is prisoner, companion, accompanying noble, settlement notable, ordinary soldier, ordinary civilian, then a safe unknown fallback.
- Ordinary soldier execution tags require both the `OrdinarySoldier` role and verified allied-soldier authority. A companion or noble cannot inherit soldier execution tags merely because the Hero belongs to the main party.
- Robbery tags are available only to settlement notables and ordinary civilians. Noble prisoners remain outside the ordinary robbery route.
- The core records the required memory lifetime policy: the four named roles use persistent personal memory, while ordinary soldiers and civilians use scene-local memory.
- Main reply, immediate reaction, and postprocess prompts now consume the same role marker and memory-scope code. Localized prompt resources explain the six codes without adding a second classifier.
