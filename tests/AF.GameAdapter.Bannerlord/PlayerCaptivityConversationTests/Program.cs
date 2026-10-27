using AnimusForge;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

int checks = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception("FAIL " + name); checks++; }
void Tick(float seconds = 0.3f) { Time.ApplicationTime += seconds; PlayerCaptivityConversation.Tick(); }
(Hero lord, PartyBase holder) Setup(bool dungeon = false)
{
    PlayerCaptivityConversation.Reset();
    Campaign.Current = new(); Game.Current = new(); Hero.MainHero = new() { IsPrisoner = true };
    PartyBase.MainParty = new(); MobileParty.MainParty = new() { Party = PartyBase.MainParty };
    var clan = new Clan(); var lord = new Hero { Clan = clan }; clan.Leader = lord;
    var holder = new PartyBase { LeaderHero = lord };
    if (dungeon)
    {
        holder.Settlement = new Settlement { OwnerClan = clan, Party = holder };
        lord.CurrentSettlement = holder.Settlement;
    }
    else holder.MobileParty = new MobileParty { Party = holder, LeaderHero = lord };
    Hero.MainHero.PartyBelongedToAsPrisoner = holder;
    holder.PrisonRoster.AddPrisoner(Hero.MainHero.CharacterObject);
    PlayerCaptivity.CaptorParty = holder;
    Campaign.Current.CurrentMenuContext.GameMenu.StringId = dungeon ? "settlement_wait" : "prisoner_wait";
    PlayerEncounterCompat.PostBattle = false; EndCaptivityAction.Calls = 0; EndCaptivityAction.ThrowBefore = EndCaptivityAction.ThrowAfter = false;
    EndCaptivityAction.AfterRelease = null;
    MyBehavior.Facts.Clear(); InformationManager.Messages.Clear(); Time.ApplicationTime = 0;
    InformationManager.Inquiry = false; TaleWorlds.MountAndBlade.Mission.Current = null;
    CampaignMapConversation.Calls = 0; CampaignMapConversation.Fail = false; CampaignMapConversation.Opening = null;
    PlayerCaptivity.CaptivityStartTime = new CampaignTime(1);
    CompanionProactiveChatBehavior.Probes = ProactiveNpcRequestBehavior.Probes = 0;
    Campaign.Current.TimeControlMode = CampaignTimeControlMode.UnstoppableFastForward;
    return (lord, holder);
}
void End() { Campaign.Current.ConversationManager.EndConversation(); ((MapState)Game.Current.GameStateManager.ActiveState).MapConversationActive = false; }
void Release(Hero lord) { string text = "Granted. " + PlayerCaptivityConversation.ReleaseTag; Check(PlayerCaptivityConversation.ConsumeReleaseTag(lord, ref text), "release accepted"); Check(!text.Contains("[ACTION:"), "tag removed"); }

var pair = Setup();
Check(PlayerCaptivityConversation.GetTargets(pair.holder).Count == 1, "mobile lord");
Check(PlayerCaptivityConversation.TryOpen(pair.lord), "opens captured player dialogue");
Check(Campaign.Current.CurrentConversationContext == ConversationContext.Default, "not an encounter context");
Check(Campaign.Current.TimeControlMode == CampaignTimeControlMode.Stop, "time paused");
Check(PlayerCaptivityConversation.CanRelease(pair.lord), "valid captor permitted");
Check(!PlayerCaptivityConversation.CanRelease(new Hero()), "other lord rejected");
End(); Tick();
Check(PlayerCaptivity.IsCaptive && EndCaptivityAction.Calls == 0, "farewell never releases");
Check(Campaign.Current.CurrentMenuContext.GameMenu.StringId == "prisoner_wait", "back to prisoner wait");

pair = Setup(true);
var governor = new Hero { Clan = pair.lord.Clan, CurrentSettlement = pair.holder.Settlement };
pair.holder.Settlement.Town.Governor = governor;
Check(PlayerCaptivityConversation.GetTargets(pair.holder).Count == 2, "local owner and governor");
pair.holder.Settlement.Town.Governor = pair.lord;
Check(PlayerCaptivityConversation.GetTargets(pair.holder).Count == 1, "deduplicate owner governor");
pair.lord.CurrentSettlement = null;
Check(PlayerCaptivityConversation.GetTargets(pair.holder).Count == 0, "remote lord unavailable");
pair.holder.Settlement.Town.Governor = governor; governor.Clan = new Clan();
Check(PlayerCaptivityConversation.GetTargets(pair.holder).Count == 0, "foreign clan governor rejected");

foreach (bool dungeon in new[] { false, true })
{
    pair = Setup(dungeon); Check(PlayerCaptivityConversation.TryOpen(pair.lord), "open before release");
    Release(pair.lord); string duplicate = PlayerCaptivityConversation.ReleaseTag;
    Check(!PlayerCaptivityConversation.ConsumeReleaseTag(pair.lord, ref duplicate), "duplicate not scheduled");
    Tick(11); Check(Campaign.Current.ConversationManager.IsConversationInProgress && EndCaptivityAction.Calls == 0 && MyBehavior.Facts.Count == 0, "tag never auto-closes after ten seconds");
    Check(InformationManager.Messages.Last().Contains("Leave"), "authorization notice explains player-controlled leave");
    Tick(130); Check(Campaign.Current.ConversationManager.IsConversationInProgress && PlayerCaptivity.IsCaptive && MyBehavior.Facts.Count == 0, "reading longer than two minutes preserves authorization without releasing");
    Campaign.Current.ConversationManager.EndConversation(); Tick();
    Check(EndCaptivityAction.Calls == 0, "player leave still waits for map UI teardown");
    ((MapState)Game.Current.GameStateManager.ActiveState).MapConversationActive = false; Tick();
    Check(EndCaptivityAction.Calls == 1 && !PlayerCaptivity.IsCaptive && !Hero.MainHero.IsPrisoner, "native release");
    Check(!pair.holder.PrisonRoster.Contains(Hero.MainHero.CharacterObject), "removed from old prison roster");
    Check(PartyBase.MainParty.MemberRoster.GetTroopCount(Hero.MainHero.CharacterObject) == 1, "native member restoration happens once");
    Check(MyBehavior.Facts.Count == 1 && EndCaptivityAction.Facilitator == null, "one fact and no fake ransom facilitator");
    Tick(20); Check(EndCaptivityAction.Calls == 1, "exactly once");
}

pair = Setup(); PlayerCaptivityConversation.TryOpen(pair.lord); Release(pair.lord);
Campaign.Current.ConversationManager.IsConversationInProgress = false;
((MapState)Game.Current.GameStateManager.ActiveState).MapConversationActive = false;
Tick(); Check(EndCaptivityAction.Calls == 0 && MyBehavior.Facts.Count == 0 && PlayerCaptivity.IsCaptive,
    "lost conversation flags without end event never authorize release");

foreach (string change in new[] { "transfer", "owner", "dead", "inactive", "target_battle", "prisoner", "generation", "campaign", "manager", "battle", "already_free" })
{
    pair = Setup(); PlayerCaptivityConversation.TryOpen(pair.lord); Release(pair.lord);
    switch (change)
    {
        case "transfer": PlayerCaptivity.CaptorParty = new PartyBase(); break;
        case "owner": pair.holder.LeaderHero = new Hero(); break;
        case "dead": pair.lord.IsAlive = false; break;
        case "inactive": pair.lord.IsActive = false; break;
        case "target_battle": pair.lord.PartyBelongedTo = new() { MapEvent = new object() }; break;
        case "prisoner": pair.lord.IsPrisoner = true; break;
        case "generation": SaveRuntimeGuard.Generation++; break;
        case "campaign": Campaign.Current = new(); break;
        case "manager": Campaign.Current.ConversationManager = new(); break;
        case "battle": pair.holder.MapEvent = new object(); break;
        case "already_free": PlayerCaptivity.CaptorParty = null; Hero.MainHero.IsPrisoner = false; break;
    }
    Check(!PlayerCaptivityConversation.CanRelease(pair.lord), "revalidate " + change);
    Tick(11); Tick(); Check(EndCaptivityAction.Calls == 0 && MyBehavior.Facts.Count == 0, "stale release rejected " + change);
}
foreach (bool after in new[] { false, true })
{
    pair = Setup(); PlayerCaptivityConversation.TryOpen(pair.lord); Release(pair.lord);
    EndCaptivityAction.ThrowBefore = !after; EndCaptivityAction.ThrowAfter = after; End(); Tick(); Tick(20);
    Check(EndCaptivityAction.Calls == 1, "exception never retries");
    Check(MyBehavior.Facts.Count == (after ? 1 : 0), "actual result after exception");
}
pair = Setup(); PlayerEncounterCompat.PostBattle = true;
Check(!PlayerCaptivityConversation.TryOpen(pair.lord), "post-battle blocked");
PlayerEncounterCompat.PostBattle = false; pair.holder.LeaderHero = null;
Check(!PlayerCaptivityConversation.TryOpen(pair.lord), "no lord blocked");
pair = Setup(); string unrelated = PlayerCaptivityConversation.ReleaseTag;
Check(!PlayerCaptivityConversation.ConsumeReleaseTag(pair.lord, ref unrelated) && unrelated == "", "tag outside session stripped and denied");
pair = Setup(true); governor = new Hero { Clan = pair.lord.Clan, CurrentSettlement = pair.holder.Settlement };
pair.holder.Settlement.Town.Governor = governor;
Check(PlayerCaptivityConversation.TryOpen(governor) && PlayerCaptivityConversation.CanRelease(governor), "selected governor conversation target");
Check(PlayerCaptivityConversation.BuildPostprocessRules(governor, "native_conversation", true)?.Count == 1, "selected topic injects native release rule");
foreach (string channel in new[] { "courier", "scene", "meeting" })
    Check(PlayerCaptivityConversation.BuildPostprocessRules(governor, channel, true) == null, "new release excluded from " + channel);
Check(PlayerCaptivityConversation.BuildPostprocessRules(governor, "native_conversation", false) == null, "unselected topic has no release rule");
string quote = "I will release you once you pay 1000 denars.";
Check(!PlayerCaptivityConversation.ConsumeReleaseTag(governor, ref quote), "conditional quotation is not an action");
string otherActions = "[ACTION:DUEL][ACTION:LET_PLAYER_GO][ACTION:WORLDMAP_ORDER:x][ATT:1][ASS:2][GUI:3][END]" + PlayerCaptivityConversation.ReleaseTag;
PlayerCaptivityConversation.StripRestrictedTags(ref otherActions);
Check(otherActions == PlayerCaptivityConversation.ReleaseTag, "captive strips battle movement and encounter release but preserves new tag");
pair.holder.Settlement.OwnerClan = new Clan();
Check(!PlayerCaptivityConversation.CanRelease(governor), "dungeon ownership changed");
pair = Setup(); PlayerCaptivityConversation.TryOpen(pair.lord); Release(pair.lord);
PartyBase.MainParty.MemberRoster.AddPrisoner(Hero.MainHero.CharacterObject); End(); Tick();
Check(EndCaptivityAction.Calls == 0 && PlayerCaptivity.IsCaptive, "existing player member fails closed before native duplicate add");
pair = Setup(); PlayerCaptivityConversation.TryOpen(pair.lord);
Hero.MainHero.Name = new("Player {CAPTOR}");
Check(PlayerCaptivityConversation.BuildRuntimeContext(pair.lord).Contains("Player {CAPTOR}"), "dynamic names are not reinterpreted as template slots");
var rules = PlayerCaptivityConversation.BuildPostprocessRules(pair.lord, "native_conversation", true);
string raw = PlayerCaptivityConversation.ReleaseTag + PlayerCaptivityConversation.ReleaseTag + "[ACTION:LET_PLAYER_GO]";
string final = PostprocessProbe.NormalizeEncounterReleasePostprocessTagsForScene(raw, rules);
Check(final.Split(PlayerCaptivityConversation.ReleaseTag).Length == 2 && !final.Contains("LET_PLAYER_GO"), "RAW to FINAL exact whitelist and duplicate normalization");
Check(!PostprocessProbe.NormalizeEncounterReleasePostprocessTagsForScene(raw, null).Contains("RELEASE_PLAYER_CAPTIVITY"), "unoffered tag stripped from FINAL");
Check(!PostprocessProbe.NormalizeEncounterReleasePostprocessTagsForScene("[ACTION:RELEASE_PLAYER_CAPTIVITY:1000]", rules).Contains("RELEASE_PLAYER_CAPTIVITY"), "release tag takes no price payload");
Check(PlayerCaptivityConversation.ConsumeReleaseTag(pair.lord, ref final), "normalized FINAL reaches execution");
Check(!final.Contains("RELEASE_PLAYER_CAPTIVITY"), "executed tag never leaks into visible reply");
End(); Tick();
Check(MyBehavior.Facts.Count == 1, "normalized release writes confirmed fact");
string projected = MemorySystemFactProjection.ProjectNpcFact(MyBehavior.Facts[0]);
Check(projected.Contains(PromptComposerTexts.Get("memory_fact_captivity_released")), "AFEF reader projects current content edition");
Check(MyBehavior.Facts[0].Contains(PromptComposerTexts.Raw("Template_captivity_released")), "saved fact remains canonical across editions");
pair = Setup(); PlayerCaptivityConversation.TryOpen(pair.lord); Release(pair.lord);
Campaign.Current.ConversationManager.ConversationEnd += () => SaveRuntimeGuard.Generation++;
End(); Tick(); Check(EndCaptivityAction.Calls == 0 && MyBehavior.Facts.Count == 0, "close callback generation change cancels release");
pair = Setup(); PlayerCaptivityConversation.TryOpen(pair.lord); Release(pair.lord);
EndCaptivityAction.AfterRelease = () => SaveRuntimeGuard.Generation++;
End(); Tick(); Check(EndCaptivityAction.Calls == 1 && MyBehavior.Facts.Count == 0, "native callback generation change suppresses stale history");
pair = Setup(); PlayerCaptivityConversation.TryOpen(pair.lord); Release(pair.lord);
Campaign.Current.ConversationManager.ConversationEnd += () => {
    Setup(true); PlayerCaptivityConversation.TryOpen(PlayerCaptivity.CaptorParty.LeaderHero);
    ((MapState)Game.Current.GameStateManager.ActiveState).MapConversationActive = false;
};
End(); Tick();
Check(PlayerCaptivityConversation.HasSession && PlayerCaptivityConversation.CanRelease(PlayerCaptivity.CaptorParty.LeaderHero), "old close callback cannot clear replacement session");
// New captures queue, never open from the event callback. Old/loaded captivity does not auto-open.
pair = Setup(); PlayerCaptivity.CaptorParty = null;
var starter = new CampaignGameStarter(); PlayerCaptivityConversation.Register(starter); PlayerCaptivity.CaptorParty = pair.holder;
Check(starter.MenuOptions.SequenceEqual(new[] { "prisoner_wait:af_captivity_talk", "settlement_wait:af_captivity_talk" }), "only existing manual menu entries registered");
CampaignMapConversation.Opening = () => {
    Check(starter.Dialogs["af_captivity_auto_start"].condition(), "auto native node selected during opening");
    Check(!starter.Dialogs["af_captivity_start"].condition(), "auto opening never claims player requested it");
    Check(starter.Dialogs["af_captivity_auto_start"].text == "{=AF_ENC_UI_CaptivityAutoOpening}...", "neutral UI placeholder instead of prescribed NPC dialogue");
};
PlayerCaptivityConversation.OnPlayerTakenPrisoner(pair.holder, Hero.MainHero); Tick();
End(); Tick(); CampaignMapConversation.Opening = () => {
    Check(starter.Dialogs["af_captivity_start"].condition() && !starter.Dialogs["af_captivity_auto_start"].condition(), "manual native opening remains unchanged");
};
PlayerCaptivityConversation.TryOpen(pair.lord);
pair = Setup(); Tick(); Check(CampaignMapConversation.Calls == 0, "no event means no catch-up on load");
PlayerCaptivityConversation.OnPlayerTakenPrisoner(pair.holder, new Hero()); Tick();
Check(CampaignMapConversation.Calls == 0, "NPC capture ignored");
PlayerCaptivityConversation.OnPlayerTakenPrisoner(pair.holder, Hero.MainHero);
Check(CampaignMapConversation.Calls == 0, "capture callback only queues");
Tick(); Check(CampaignMapConversation.Calls == 1 && PlayerCaptivityConversation.HasSession, "new capture opens on safe tick");
Check(NpcInitiatedOpeningRouter.HasPendingNativeOpeningForCurrentConversation(), "overlay sees captor opening");
Check(!NpcInitiatedOpeningRouter.TryConsumePendingNativeOpening(new Hero(), out _, out _, out _), "unrelated hero cannot consume opening");
Check(NpcInitiatedOpeningRouter.TryConsumePendingNativeOpening(pair.lord, out string openingFact, out string openingPrompt, out string openingSource), "native admission consumes captor opening");
Check(openingSource == "PlayerCaptivity" && openingFact == PromptComposerTexts.Raw("Template_captivity_opening"), "canonical opening source and fact");
Check(openingPrompt == EncounterPromptTexts.Get("PlayerCaptivityOpeningPrompt"), "actual CN/EN reader delivers free opening");
string openingUserText = PostprocessProbe.BuildNpcInitiatedOpeningUserText(openingFact, openingPrompt);
Check(openingUserText.Contains(PromptComposerTexts.Get("memory_fact_captivity_opening")) && openingUserText.Contains(openingPrompt), "actual opening composer renders fact and free opening in edition language");
Check(openingUserText.Contains(PromptComposerTexts.Get("scene_live_020")), "opening explicitly is not a player utterance");
Check(!openingUserText.Contains("[ACTION:") && !openingUserText.Contains("MoneyShortage"), "no tag grammar or forced need added to opening");
Check(PostprocessProbe.BuildNpcInitiatedOpeningPersistentFactText(openingFact).Contains(PromptComposerTexts.Raw("Template_captivity_opening")), "history composer retains canonical fact, not translated instructions");
Check(!NpcInitiatedOpeningRouter.TryConsumePendingNativeOpening(pair.lord, out _, out _, out _) && !NpcInitiatedOpeningRouter.HasPendingNativeOpeningForCurrentConversation(), "opening consumed only once");
Check(CompanionProactiveChatBehavior.Probes == 0 && ProactiveNpcRequestBehavior.Probes == 0, "captivity never steals other proactive openings");
Check(MemorySystemFactProjection.ProjectBody(openingFact) == PromptComposerTexts.Get("memory_fact_captivity_opening"), "canonical opening is projected by shared history reader");
Check(MyBehavior.Facts.Count == 0 && EndCaptivityAction.Calls == 0, "queue and opening do not invent completed actions");
End(); Tick();
Check(Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForward && PlayerCaptivity.IsCaptive, "ordinary exit resumes captivity time");
PlayerCaptivityConversation.OnPlayerTakenPrisoner(pair.holder, Hero.MainHero); Tick(30);
Check(CampaignMapConversation.Calls == 1, "ordinary retirement retains capture identity");
Check(PlayerCaptivityConversation.TryOpen(pair.lord), "manual re-open stays available");
Check(!NpcInitiatedOpeningRouter.HasPendingNativeOpeningForCurrentConversation(), "manual re-open does not force another NPC opening");
End(); Tick();
PlayerCaptivity.CaptivityStartTime = new CampaignTime(2);
PlayerCaptivityConversation.OnPlayerTakenPrisoner(pair.holder, Hero.MainHero); Tick();
Check(CampaignMapConversation.Calls == 3 && NpcInitiatedOpeningRouter.HasPendingNativeOpeningForCurrentConversation(), "next distinct captivity can open again");

foreach (string busy in new[] { "results", "battle", "menu", "inquiry", "mission", "conversation", "map_ui", "siege" })
{
    pair = Setup(true); PlayerCaptivityConversation.OnPlayerTakenPrisoner(pair.holder, Hero.MainHero);
    switch (busy)
    {
        case "results": PlayerEncounterCompat.PostBattle = true; break;
        case "battle": pair.holder.MapEvent = new(); break;
        case "menu": Campaign.Current.CurrentMenuContext.GameMenu.StringId = "menu_captivity_transfer_to_town"; break;
        case "inquiry": InformationManager.Inquiry = true; break;
        case "mission": TaleWorlds.MountAndBlade.Mission.Current = new(); break;
        case "conversation": Campaign.Current.ConversationManager.IsConversationInProgress = true; break;
        case "map_ui": ((MapState)Game.Current.GameStateManager.ActiveState).MapConversationActive = true; break;
        case "siege": pair.holder.Settlement.SiegeEvent = new(); break;
    }
    Tick(); Check(CampaignMapConversation.Calls == 0, "auto defers " + busy);
    PlayerEncounterCompat.PostBattle = false; pair.holder.MapEvent = null; pair.holder.Settlement.SiegeEvent = null;
    Campaign.Current.CurrentMenuContext.GameMenu.StringId = "settlement_wait"; InformationManager.Inquiry = false;
    TaleWorlds.MountAndBlade.Mission.Current = null; Campaign.Current.ConversationManager.IsConversationInProgress = false;
    ((MapState)Game.Current.GameStateManager.ActiveState).MapConversationActive = false;
    Tick(); Check(CampaignMapConversation.Calls == 1, "auto resumes after " + busy);
}
foreach (bool ownerHere in new[] { true, false })
{
    pair = Setup(true); governor = new Hero { Clan = pair.lord.Clan, CurrentSettlement = pair.holder.Settlement };
    pair.holder.Settlement.Town.Governor = governor;
    if (!ownerHere) pair.lord.CurrentSettlement = null;
    PlayerCaptivityConversation.OnPlayerTakenPrisoner(pair.holder, Hero.MainHero); Tick();
    Check(Hero.OneToOneConversationHero == (ownerHere ? pair.lord : governor), "auto prefers local clan leader, then governor");
}
pair = Setup(); pair.lord.IsAlive = false;
PlayerCaptivityConversation.OnPlayerTakenPrisoner(pair.holder, Hero.MainHero); Tick(); pair.lord.IsAlive = true; Tick(30);
Check(CampaignMapConversation.Calls == 0, "no eligible target consumes opportunity without polling for a future lord");
pair = Setup(); PlayerCaptivityConversation.OnPlayerTakenPrisoner(pair.holder, Hero.MainHero);
Check(PlayerCaptivityConversation.TryOpen(pair.lord), "manual opening can precede deferred auto opening");
End(); Tick(); Tick(10); Check(CampaignMapConversation.Calls == 1, "manual meeting consumes pending opportunity");
pair = Setup(); PlayerCaptivityConversation.OnPlayerTakenPrisoner(pair.holder, Hero.MainHero); Tick(); End(); Tick();
var transferred = new PartyBase { LeaderHero = new Hero() }; transferred.MobileParty = new() { Party = transferred };
transferred.PrisonRoster.AddPrisoner(Hero.MainHero.CharacterObject);
PlayerCaptivity.CaptorParty = transferred; Hero.MainHero.PartyBelongedToAsPrisoner = transferred;
PlayerCaptivityConversation.OnPlayerTakenPrisoner(transferred, Hero.MainHero); Tick();
Check(CampaignMapConversation.Calls == 1, "transfer event with unchanged captivity identity does not reopen");
pair = Setup(); CampaignMapConversation.Fail = true;
PlayerCaptivityConversation.OnPlayerTakenPrisoner(pair.holder, Hero.MainHero); Tick(); Tick(); CampaignMapConversation.Fail = false; Tick(30);
Check(CampaignMapConversation.Calls == 1 && PlayerCaptivity.IsCaptive, "failed open never loops or releases");
Check(PlayerCaptivityConversation.TryOpen(pair.lord), "manual opening still works after automatic failure");

foreach (string change in new[] { "transfer", "leader", "owner", "generation", "campaign", "free", "captivity", "inactive", "roster", "dead" })
{
    pair = Setup(change == "owner"); PlayerCaptivityConversation.OnPlayerTakenPrisoner(pair.holder, Hero.MainHero);
    switch (change)
    {
        case "transfer": PlayerCaptivity.CaptorParty = new PartyBase(); Hero.MainHero.PartyBelongedToAsPrisoner = PlayerCaptivity.CaptorParty; break;
        case "leader": pair.holder.LeaderHero = new Hero(); break;
        case "owner": pair.holder.Settlement.OwnerClan = new Clan(); break;
        case "generation": SaveRuntimeGuard.Generation++; break;
        case "campaign": Campaign.Current = new(); break;
        case "free": PlayerCaptivity.CaptorParty = null; Hero.MainHero.IsPrisoner = false; break;
        case "captivity": PlayerCaptivity.CaptivityStartTime = new CampaignTime(5); break;
        case "inactive": pair.holder.IsActive = false; break;
        case "roster": pair.holder.PrisonRoster.Remove(Hero.MainHero.CharacterObject); break;
        case "dead": Hero.MainHero.IsAlive = false; break;
    }
    Tick(); Check(CampaignMapConversation.Calls == 0, "pending capture cancelled on " + change);
    pair.holder.IsActive = true; Hero.MainHero.IsAlive = true; pair.holder.PrisonRoster.AddPrisoner(Hero.MainHero.CharacterObject);
    Tick(); Check(CampaignMapConversation.Calls == 0, "cancelled capture never re-arms on " + change);
}
pair = Setup(); PlayerCaptivityConversation.Register(new CampaignGameStarter());
PlayerCaptivityConversation.OnPlayerTakenPrisoner(pair.holder, Hero.MainHero); Tick();
Check(CampaignMapConversation.Calls == 0, "loaded captive baseline does not replay a duplicate capture event");
pair = Setup(); PlayerCaptivityConversation.OnPlayerTakenPrisoner(pair.holder, Hero.MainHero); Tick();
SaveRuntimeGuard.Generation++;
Check(!NpcInitiatedOpeningRouter.TryConsumePendingNativeOpening(pair.lord, out _, out _, out _), "stale opening rejected before admission");
pair = Setup(); PlayerCaptivityConversation.OnPlayerTakenPrisoner(pair.holder, Hero.MainHero); Tick(); Release(pair.lord);
PlayerCaptivity.CaptivityStartTime = new CampaignTime(2);
Check(!PlayerCaptivityConversation.CanRelease(pair.lord), "old dialogue cannot release a different captivity with the same captor");
End(); Tick(); Check(EndCaptivityAction.Calls == 0, "recapture invalidates delayed release");
PlayerCaptivityConversation.Reset();
Check(!NpcInitiatedOpeningRouter.HasPendingNativeOpeningForCurrentConversation()
    && CompanionProactiveChatBehavior.Probes > 0 && ProactiveNpcRequestBehavior.Probes > 0, "ordinary router still visits existing opening sources outside captivity");

// Captor visibility restoration is scoped to the same actual captivity and
// happens after Leave, without releasing the player or recreating either party.
pair = Setup(); PlayerCaptivityConversation.TryOpen(pair.lord);
pair.holder.MobileParty.IsVisible = false;
var unrelatedParty = new PartyBase { MobileParty = new MobileParty { IsVisible = false } };
Campaign.Current._tickData.Previous.Add(pair.holder);
Campaign.Current._tickData.Previous.Add(unrelatedParty);
Campaign.Current._tickData.Current.Add(pair.holder);
End(); Tick();
Check(pair.holder.MobileParty.IsVisible, "Leave restores hidden current captor");
Check(!unrelatedParty.MobileParty.IsVisible, "Leave does not reveal other parties");
Check(PlayerCaptivity.IsCaptive && EndCaptivityAction.Calls == 0 && pair.holder.PrisonRoster.Contains(Hero.MainHero.CharacterObject), "visibility restoration preserves captivity and roster");
Check(!PlayerCaptivityConversation.HasSession && Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForward, "visibility restoration still completes return and time restoration");
#if BANNERLORD_1_5_OR_GREATER
Check(!Campaign.Current._tickData.Previous.Contains(pair.holder) && !Campaign.Current._tickData.Current.Contains(pair.holder)
    && Campaign.Current._tickData.Previous.Contains(unrelatedParty), "1.5 removes only captor from both transient visibility lists");
// Native 1.5 skips persistent captors in its visibility computation and then
// hides leftovers from this previous list. After the correction it stays shown.
foreach (var oldVisible in Campaign.Current._tickData.Previous) oldVisible.MobileParty.IsVisible = false;
Check(pair.holder.MobileParty.IsVisible, "next native cleanup cannot re-hide the captor");
#endif
pair = Setup(); PlayerCaptivityConversation.TryOpen(pair.lord); pair.holder.MobileParty.IsVisible = false;
pair.holder.MobileParty.CurrentSettlement = new Settlement(); End(); Tick();
Check(!pair.holder.MobileParty.IsVisible, "captor entering settlement is not revealed");
pair = Setup(); PlayerCaptivityConversation.TryOpen(pair.lord); pair.holder.MobileParty.IsVisible = false;
pair.holder.IsActive = false; End(); Tick();
Check(!pair.holder.MobileParty.IsVisible, "destroyed or inactive captor is not revived");
pair = Setup(); PlayerCaptivityConversation.TryOpen(pair.lord); pair.holder.MobileParty.IsVisible = false;
var changedHolder = new PartyBase { MobileParty = new MobileParty { IsVisible = false } };
PlayerCaptivity.CaptorParty = changedHolder; Hero.MainHero.PartyBelongedToAsPrisoner = changedHolder;
changedHolder.PrisonRoster.AddPrisoner(Hero.MainHero.CharacterObject); End(); Tick();
Check(!pair.holder.MobileParty.IsVisible && !changedHolder.MobileParty.IsVisible, "old conversation cannot reveal transferred captor");
pair = Setup(); PlayerCaptivityConversation.TryOpen(pair.lord); pair.holder.MobileParty.IsVisible = false;
Release(pair.lord); End(); Tick();
Check(!PlayerCaptivity.IsCaptive && !pair.holder.MobileParty.IsVisible, "release does not permanently reveal former captor");
pair = Setup(true); PlayerCaptivityConversation.TryOpen(pair.lord); End(); Tick();
Check(Campaign.Current.CurrentMenuContext.GameMenu.StringId == "settlement_wait" && PlayerCaptivity.IsCaptive, "dungeon farewell retains native menu and captivity");
#if BANNERLORD_1_5_OR_GREATER
// Auto-resolve/bandit captivity has no eligible lord and may never open a conversation.
pair = Setup(); pair.holder.LeaderHero = null; pair.holder.MobileParty.LeaderHero = null;
pair.holder.MobileParty.IsVisible = false;
Campaign.Current._tickData.Previous.Add(pair.holder); Campaign.Current._tickData.Current.Add(pair.holder);
PlayerCaptivityConversation.OnPlayerTakenPrisoner(pair.holder, Hero.MainHero); Tick();
Check(pair.holder.MobileParty.IsVisible && CampaignMapConversation.Calls == 0, "capture entry restores bandit captor without opening conversation");
Check(!Campaign.Current._tickData.Previous.Contains(pair.holder) && !Campaign.Current._tickData.Current.Contains(pair.holder), "capture entry evicts both stale native visibility entries");
pair.holder.MobileParty.IsVisible = false; Tick();
Check(pair.holder.MobileParty.IsVisible, "repair remains available after no-lord auto opening is consumed");
Check(PlayerCaptivity.IsCaptive && EndCaptivityAction.Calls == 0 && pair.holder.PrisonRoster.Contains(Hero.MainHero.CharacterObject), "entry repair preserves native captivity and prisoners");
foreach (string blocked in new[] { "results", "battle", "mission", "menu", "settlement", "roster", "inactive", "free", "identity" })
{
    pair = Setup(); pair.holder.LeaderHero = null; pair.holder.MobileParty.IsVisible = false;
    switch (blocked)
    {
        case "results": PlayerEncounterCompat.PostBattle = true; break;
        case "battle": pair.holder.MapEvent = new(); break;
        case "mission": TaleWorlds.MountAndBlade.Mission.Current = new(); break;
        case "menu": Campaign.Current.CurrentMenuContext.GameMenu.StringId = "encounter"; break;
        case "settlement": pair.holder.MobileParty.CurrentSettlement = new(); break;
        case "roster": pair.holder.PrisonRoster.Remove(Hero.MainHero.CharacterObject); break;
        case "inactive": pair.holder.IsActive = false; break;
        case "free": Hero.MainHero.IsPrisoner = false; break;
        case "identity": Hero.MainHero.PartyBelongedToAsPrisoner = new(); break;
    }
    Tick(); Check(!pair.holder.MobileParty.IsVisible, "capture entry preserves native visibility during " + blocked);
}
pair = Setup(); pair.holder.LeaderHero = null; pair.holder.MobileParty.IsVisible = false;
PlayerCaptivityConversation.Register(new CampaignGameStarter()); Tick(1);
Check(pair.holder.MobileParty.IsVisible && CampaignMapConversation.Calls == 0, "loaded captivity repaired without replaying capture dialogue");
#endif
// Native IsCaptive is deliberately unsafe without Campaign and its subsystem.
foreach (bool partial in new[] { false, true })
{
    PlayerCaptivityConversation.Reset();
    Game.Current = null; Hero.MainHero = null;
    Campaign.Current = partial ? new Campaign { PlayerCaptivity = null } : null;
    int reads = PlayerCaptivity.IsCaptiveReads;
    for (int i = 0; i < 3; i++) Tick(1);
    Check(PlayerCaptivity.IsCaptiveReads == reads, partial ? "partially constructed campaign skips native captivity getter" : "main menu skips native captivity getter");
}
pair = Setup(); Check(PlayerCaptivityConversation.TryOpen(pair.lord), "open session before campaign teardown"); Release(pair.lord);
Campaign.Current = null; Game.Current = null; Hero.MainHero = null;
int teardownReads = PlayerCaptivity.IsCaptiveReads;
Tick(1); Check(PlayerCaptivity.IsCaptiveReads == teardownReads && EndCaptivityAction.Calls == 0, "teardown skips native access and never executes delayed release");
Campaign.Current = new Campaign(); Game.Current = new(); Hero.MainHero = new(); Tick(1);
Check(!PlayerCaptivityConversation.HasSession && EndCaptivityAction.Calls == 0, "next campaign retires old session without release");
pair = Setup(); pair.holder.LeaderHero = null; pair.holder.MobileParty.IsVisible = false; Tick(10);
#if BANNERLORD_1_5_OR_GREATER
Check(pair.holder.MobileParty.IsVisible, "initialized campaign still repairs hidden captor after lifecycle transitions");
#endif
Console.WriteLine($"PASS {checks} captivity lifecycle checks (production source, fake game ports; live game NOT RUN)");
