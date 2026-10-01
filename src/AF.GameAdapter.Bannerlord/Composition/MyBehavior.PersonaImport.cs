using System.Collections.Generic;

namespace AnimusForge;

public partial class MyBehavior
{
 private bool ApplyImportedSinglePersonaProfile(string heroId, NpcPersonaProfile imported, long generation)
 {
  if (!IsMemorySourceEditorCurrent(generation)) return false;
  if (imported != null) StampNpcPersonaProfile(heroId, imported);
  PersonaImportOwner.ApplySingleProfile(ref _npcPersonaProfiles, heroId, imported);
  return true;
 }
 private bool ApplyImportedPersonaProfiles(Dictionary<string, NpcPersonaProfile> imported, bool overwriteExisting, long generation)
 {
  if (!IsMemorySourceEditorCurrent(generation)) return false;
  PersonaImportOwner.ApplyProfiles(ref _npcPersonaProfiles, imported, overwriteExisting);
  return true;
 }
}
