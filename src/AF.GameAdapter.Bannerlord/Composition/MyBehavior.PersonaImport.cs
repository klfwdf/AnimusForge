using System.Collections.Generic;

namespace AnimusForge;

public partial class MyBehavior
{
 private bool ApplyImportedPersonaProfiles(Dictionary<string, NpcPersonaProfile> imported, bool overwriteExisting, long generation)
 {
  if (!IsMemorySourceEditorCurrent(generation)) return false;
  PersonaImportOwner.ApplyProfiles(ref _npcPersonaProfiles, imported, overwriteExisting);
  return true;
 }
}
