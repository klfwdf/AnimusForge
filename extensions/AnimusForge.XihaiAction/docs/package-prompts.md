# Scene prompt package selection

Implementation intent: scene action classification and battle speech consume the
CN or EN corpus selected into `ModuleData/SceneActionPromptTexts.xml` by packaging.
Both packages retain bilingual UI; changing the UI language does not switch this
corpus. Existing action IDs, parser rules, authorization and request snapshots stay
unchanged. Package text is read once during host initialization, never on Tick.

Product compilation, replay execution, packaging and game acceptance are deferred
by the current task. Static evidence will be recorded after the complete slice.
