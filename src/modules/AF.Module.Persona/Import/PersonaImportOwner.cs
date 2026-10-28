using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

// The legacy nested profile type stays in its host to preserve save/type identity.
// This operation mutates only the supplied authoritative dictionary; it retains no state.
internal static class PersonaImportOwner
{
 internal static void StampProfileMetadata(string heroId, MyBehavior.NpcPersonaProfile profile, Func<string,string> resolveHeroName)
 {
  if (profile == null) return;
  profile.NormalizeDescription();
  string id = (heroId ?? "").Trim();
  profile.HeroId = id;
  try { profile.HeroName = resolveHeroName(id) ?? ""; }
  catch { profile.HeroName = ""; }
 }

 internal static void ApplySingleProfile<TProfile>(ref Dictionary<string, TProfile> authority, string heroId, TProfile imported) where TProfile : class
 {
  if (authority == null) authority = new Dictionary<string, TProfile>();
  // Persona imports carry authored content, not another campaign's pending API work.
  if (imported is MyBehavior.NpcPersonaProfile persona) persona.Evolution = null;
  // Single-NPC null imports intentionally delete; bulk imports still skip null entries.
  if (imported == null) authority.Remove(heroId);
  else authority[heroId] = imported;
 }
 internal static void ApplyProfiles<TProfile>(ref Dictionary<string, TProfile> authority, Dictionary<string, TProfile> imported, bool overwriteExisting) where TProfile : class
 {
  if (imported == null) return;
  if (authority == null) authority = new Dictionary<string, TProfile>();
  foreach (var item in imported)
  {
   if (string.IsNullOrEmpty(item.Key) || item.Value == null) continue;
   if (!overwriteExisting && authority.ContainsKey(item.Key)) continue;
   if (item.Value is MyBehavior.NpcPersonaProfile persona) persona.Evolution = null;
   if (overwriteExisting) authority.Remove(item.Key);
   authority[item.Key] = item.Value;
  }
 }
 internal static void ApplyDeveloperText(MyBehavior.NpcPersonaProfile profile,string description,Action<MyBehavior.NpcPersonaProfile> save)
 { profile.Description=AnimusForge.Refactor.Runtime.NpcPersonaProfilePolicy.NormalizeGenerated(description);profile.SchemaVersion=Math.Max(profile.SchemaVersion,3);save(profile); }
 internal static void ApplyDeveloperVoice(MyBehavior.NpcPersonaProfile profile,string voice,Action<MyBehavior.NpcPersonaProfile> save)
 { profile.VoiceId=voice;save(profile); }
}
