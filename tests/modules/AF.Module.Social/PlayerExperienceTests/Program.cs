using System.Text.Json;
using AnimusForge;

int checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
PlayerExperienceMotive Recent(string key = "event-a", int day = 10) => new() {
    Kind = PlayerExperienceContactState.RecentKind, SourceKey = key,
    Evidence = "The player released prisoners; their names are not recorded.", Day = day, Urgency = 90 };
var state = new PlayerExperienceContactState();
var recent = Recent();
Check(state.CanOffer("npc-a", recent, 10), "fresh event eligible");
Check(!state.CanOffer("npc-a", Recent(day: 0), 10), "expired event excluded");
Check(!state.CanOffer("npc-a", Recent(day: 11), 10), "future event excluded");
var ticket = state.Reserve("map-1", "npc-a", "map", recent, 10, 12);
Check(ticket != null, "map reservation admitted");
Check(state.Reserve("letter-1", "npc-a", "courier", Recent("other"), 10, 12) == null, "concurrent channel cannot reserve same NPC");
state.Release(ticket.Id);
Check(state.CanOffer("npc-a", recent, 10), "failed generation releases without consuming event");
ticket = state.Reserve("map-2", "npc-a", "map", recent, 10, 12);
Check(state.Complete(ticket, 10), "visible reply commits receipt");
Check(!state.Complete(ticket, 10), "duplicate callback cannot commit twice");
Check(!state.CanOffer("npc-a", recent, 12), "same event cannot return by letter");
Check(!state.CanOffer("npc-b", recent, 10), "cross-NPC source fatigue prevents chorus");
Check(state.CanOffer("npc-b", recent, 11), "source fatigue expires before event");
Check(state.CanOffer("npc-a", Recent("different"), 11), "different event remains eligible");
string json = JsonSerializer.Serialize(state);
var restored = JsonSerializer.Deserialize<PlayerExperienceContactState>(json)!;
restored.Normalize();
Check(!restored.CanOffer("npc-a", recent, 12), "save reload retains delivered receipt");
var history = new PlayerExperienceMotive { Kind = PlayerExperienceContactState.HistoryKind, SourceKey = "history", Evidence = "Past campaigns.", Day = 10 };
ticket = restored.Reserve("companion-1", "npc-c", "companion", history, 10, 12);
Check(restored.Complete(ticket, 10), "history contact committed");
history.Evidence = "The same past, newly edited.";
history.SourceKey = "different-summary-hash";
Check(!restored.CanOffer("npc-c", history, 39), "editing biography cannot reset cooldown");
Check(restored.CanOffer("npc-c", history, 40), "history eligible after 30 days");
var pending = restored.Reserve("letter-2", "npc-d", "courier", Recent(), 11, 19);
var loaded = JsonSerializer.Deserialize<PlayerExperienceContactState>(JsonSerializer.Serialize(restored))!;
loaded.Normalize();
Check(loaded.Pending.Count == 1 && loaded.Pending[0].Id == pending.Id, "in-flight reservation survives reload");
Check(!loaded.Complete(pending, 12), "pre-load late callback cannot claim new state");
Check(loaded.Complete(loaded.Pending[0], 12), "restored current reservation can commit");
var empty = JsonSerializer.Deserialize<PlayerExperienceContactState>("{}")!;
empty.Normalize();
Check(empty.CanOffer("npc-a", recent, 10), "old save without new fields remains usable");
empty.Reserve("cancelled", "npc-a", "map", recent, 10, 11);
empty.Prune(11);
Check(empty.CanOffer("npc-a", recent, 11), "orphan expiration does not consume an event");
for (int i=0; i<PlayerExperienceContactState.MaxReceipts; i++) empty.DiscussedUntil["full-"+i] = 30;
Check(!empty.CanOffer("npc-e", recent, 11), "capacity rejects new contact instead of dropping valid dedupe receipts");
empty.Prune(31);
Check(empty.DiscussedUntil.Count == 0, "expired receipts pruned at boundary");

Check(PlayerExperienceContactState.BoundedEvidence("One event.", 100) == "One event.", "short evidence unchanged");
Check(PlayerExperienceContactState.BoundedEvidence("没有获胜。" + new string('长', 500), 20) == "没有获胜。", "sentence extraction retains negation");
Check(PlayerExperienceContactState.BoundedEvidence(new string('长', 500), 100) == "", "no sentence boundary skips evidence rather than truncating fact");
Check(PlayerExperienceContactState.BoundedEvidence("Complete event. " + new string('x', 500), 30) == "Complete event.", "English boundary preserved");
string evidence = "The player did not surrender.";
string opening = "Known context\n" + PlayerExperiencePrompt.Begin + "\n" + evidence + "\n" + PlayerExperiencePrompt.End;
Check(PlayerExperiencePrompt.Evidence(opening) == evidence, "extract actual readable evidence");
string extras = "Other NPC facts.\n- Yesterday: " + evidence + "\nOther player facts.";
string dedup = PlayerExperiencePrompt.RemoveRepeatedEvidence(extras, opening);
Check(!dedup.Contains(evidence) && dedup.Contains("Other NPC facts.") && dedup.Contains("Other player facts."), "only chosen evidence removed from other context");
Check(PlayerExperiencePrompt.RemoveRepeatedEvidence(extras, "normal player turn") == extras, "ordinary conversations retain all context");
Check(PlayerExperiencePrompt.Evidence(PlayerExperiencePrompt.Begin + "incomplete") == "", "incomplete marker not interpreted");

string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../../"));
if (args.Length > 0) root = args[0];
foreach (string suffix in new[] { "", ".en" })
{
    using var prompts = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "content/modules/AF.Module.Social/ModuleData/ProactiveNpcRequestPrompts"+suffix+".json")));
    var requests = prompts.RootElement.GetProperty("Requests");
    Check(requests.EnumerateObject().Count() == 32, "30 old + 2 new types retained " + suffix);
    foreach (string kind in new[] { PlayerExperienceContactState.HistoryKind, PlayerExperienceContactState.RecentKind })
        foreach (string field in new[] { "OpeningPrompt", "LetterIntent", "CompanionIntent" })
            Check(!string.IsNullOrWhiteSpace(requests.GetProperty(kind).GetProperty(field).GetString()), kind + " wired bilingual intent " + field + suffix);
}
var observer = new TaleWorlds.CampaignSystem.Hero { StringId = "observer" };
var source = PlayerNotorietyBehavior.Instance = new PlayerNotorietyBehavior();
source.Add(9, 1, "older", "PLAYER_RAW released prisoners.");
source.Add(10, 2, "newest", "PLAYER_RAW did not win the tournament.");
source.SetHistory("PLAYER_RAW once defended a town.");
Check(PlayerNotorietyBehavior.CaptureExperience(observer, false, false).Count == 0, "unknown observer receives neither source");
observer.KnowsRecent = true;
var choices = PlayerNotorietyBehavior.CaptureExperience(observer, false, false);
Check(choices.Count == 1 && choices[0].Day == 10 && choices[0].Evidence.Contains("did not win"), "recent-only knowledge selects newest factual event");
Check(choices[0].Evidence.Contains("Known alias") && !choices[0].Evidence.Contains("PLAYER_RAW"), "source uses existing name projection boundary");
var newest = choices[0];
Check(PlayerNotorietyBehavior.CanStillDiscussExperience(observer, newest, false, false), "selected recent source revalidates");
var r = ProactiveNpcRequestBehavior.TestState.Reserve("seen", observer.StringId, "map", newest, 10, 12);
ProactiveNpcRequestBehavior.TestState.Complete(r, 10);
choices = PlayerNotorietyBehavior.CaptureExperience(observer, true, false);
Check(choices.Count == 1 && choices[0].Day == 9, "same cached snapshot falls back to older unconsumed event across channel");
observer.KnowsHistory = true;
choices = PlayerNotorietyBehavior.CaptureExperience(observer, false, false);
Check(choices.Count == 2 && choices[1].Kind == PlayerExperienceContactState.HistoryKind, "history remains separate choice alongside recent event");
var career = choices[1];
source.SetHistory("PLAYER_RAW once lost a town.");
Check(!PlayerNotorietyBehavior.CanStillDiscussExperience(observer, career, false, false), "edited biography invalidates pending old evidence");
choices = PlayerNotorietyBehavior.CaptureExperience(observer, false, false);
Check(choices[1].Evidence.Contains("lost"), "history cache refreshes at changed source");
source.RemoveEvents();
Check(!PlayerNotorietyBehavior.CanStillDiscussExperience(observer, newest, false, false), "deleted recent source cannot publish stale opening");
Check(PlayerNotorietyBehavior.CaptureExperience(observer, false, false).Count == 1, "history without any recent records still generates motive");
observer.KnowsHistory = false;
observer.KnowsRecent = false;
source.Add(10, 3, "party-event", "PLAYER_RAW recruited soldiers.");
var party = new TaleWorlds.CampaignSystem.Party.MobileParty();
TaleWorlds.CampaignSystem.Party.MobileParty.MainParty = party;
observer.PartyBelongedTo = party;
Check(PlayerNotorietyBehavior.CaptureExperience(observer, false, true).Count == 1, "current companion preserves existing witnessing eligibility");
Check(PlayerNotorietyBehavior.CaptureExperience(observer, true, false).Count == 0, "companion witnessing is not silently used for courier strangers");
Console.WriteLine($"PASS {checks} player experience source/persistence/prompt checks (game knowledge/render leaves substituted; not live game)");
