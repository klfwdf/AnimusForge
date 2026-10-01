using System;
using System.Collections.Generic;

namespace AnimusForge;

// The legacy nested profile type stays in its host to preserve save/type identity.
// This operation mutates only the supplied authoritative dictionary; it retains no state.
internal static class PersonaImportOwner
{
 internal static void ApplyProfiles<TProfile>(ref Dictionary<string, TProfile> authority, Dictionary<string, TProfile> imported, bool overwriteExisting) where TProfile : class
 {
  if (imported == null) return;
  if (authority == null) authority = new Dictionary<string, TProfile>();
  foreach (var item in imported)
  {
   if (string.IsNullOrEmpty(item.Key) || item.Value == null) continue;
   if (!overwriteExisting && authority.ContainsKey(item.Key)) continue;
   if (overwriteExisting) authority.Remove(item.Key);
   authority[item.Key] = item.Value;
  }
 }
}
