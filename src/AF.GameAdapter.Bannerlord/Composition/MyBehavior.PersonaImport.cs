using System.Collections.Generic;

namespace AnimusForge;

public partial class MyBehavior
{
 private bool ApplyImportedSinglePersonaProfile(string heroId, NpcPersonaProfile imported, long generation)
  => PersonaProfileFiles.ApplyImportedSinglePersonaProfile(heroId, imported, generation);
 private bool ApplyImportedPersonaProfiles(Dictionary<string, NpcPersonaProfile> imported, bool overwriteExisting, long generation)
  => PersonaProfileFiles.ApplyImportedPersonaProfiles(imported, overwriteExisting, generation);
}
