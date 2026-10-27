using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AnimusForge.Refactor.Modules;
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
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

// A map-conversation adapter, not another dialogue pipeline. No save keys or world scans.
internal static class PlayerCaptivityConversation
{
    internal const string RuleId = "player_captivity_release";
    internal const string ReleaseTag = "[ACTION:RELEASE_PLAYER_CAPTIVITY]";
    private static readonly Regex ReleaseRegex = new Regex(@"\[ACTION:RELEASE_PLAYER_CAPTIVITY\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RestrictedTags = new Regex(@"\[(?:ACTION:(?:DUEL[^:\]]*|MEETING_TAUNT[^:\]]*|LET_PLAYER_GO|NPC_SURRENDER|SCENE_[^:\]]*|WORLDMAP_ORDER|OPEN_LORDS_HALL|LORDS_HALL_BRIBE_PRICE|PUBLIC_EXECUTION_START|NOBLE_[^:\]]*|TROOP_INSPECTION_SLAUGHTER_PRISONERS)(?::[^\]]*)?|A:H_J_P_P[^\]]*|(?:ASS|GUI|ATT|ATP):[^\]]*|END|FOL[^\]]*|STP[^\]]*)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly EncounterReleaseOwner<ReleaseRequest> Releases = new EncounterReleaseOwner<ReleaseRequest>();
    private static Session _session;
    private static CaptureOpening _captureOpening;
    private static float _nextTick;
    internal static bool IsOpening { get; private set; }
    internal static bool HasSession => _session != null;

    private sealed class Session
    {
        internal Campaign Campaign;
        internal ConversationManager Manager;
        internal PartyBase Holder;
        internal Hero Target;
        internal Clan OwnerClan;
        internal long Generation;
        internal CampaignTime CaptivityStartedAt;
        internal CampaignTimeControlMode PreviousTime;
        internal Action EndHandler;
        internal bool Ended, CloseRequested, NpcInitiated, OpeningPending;
    }

    // Kept after a normal farewell; only a different captivity can arm another opening.
    private sealed class CaptureOpening
    {
        internal Campaign Campaign;
        internal long Generation;
        internal CampaignTime StartedAt;
        internal PartyBase Holder;
        internal Clan OwnerClan;
        internal Hero Leader;
        internal bool Pending;
    }

    private sealed class ReleaseRequest : IEncounterReleaseRequest
    {
        internal Session Session;
        internal string Fact;
        public object EncounterIdentity => Session;
        public object PartyIdentity => Session.Holder;
        public object SourceMissionIdentity => null;
        public long RuntimeGeneration => Session.Generation;
        public float RequestedAt { get; set; }
        public float LastAttemptAt { get; set; } = -1f;
    }

    internal static void Register(CampaignGameStarter starter)
    {
        Reset();
        // Loading an existing captivity is not a new capture, even if an event is replayed.
        if (Campaign.Current != null && PlayerCaptivity.IsCaptive)
            _captureOpening = CaptureOpeningIdentity(PlayerCaptivity.CaptorParty, pending: false);
        foreach (string menu in new[] { "prisoner_wait", "settlement_wait" })
            starter.AddGameMenuOption(menu, "af_captivity_talk", "{=AF_ENC_UI_CaptivityTalk}Speak with your captor", args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
                args.IsEnabled = CanOpenMenu() && GetTargets(PlayerCaptivity.CaptorParty).Count > 0;
                if (!args.IsEnabled) args.Tooltip = Ui("Unavailable", "No captor with authority is available to speak right now.");
                return PlayerCaptivity.IsCaptive;
            }, _ => ChooseTarget());
        starter.AddDialogLine("af_captivity_auto_start", "start", "af_captivity_options",
            "{=AF_ENC_UI_CaptivityAutoOpening}...",
            () => IsOpening && _session?.NpcInitiated == true && IsSessionValid(_session), null, 3001);
        starter.AddDialogLine("af_captivity_start", "start", "af_captivity_options",
            "{=AF_ENC_UI_CaptivityOpening}You asked to speak to me. What is it?",
            () => IsOpening && _session?.NpcInitiated == false && IsSessionValid(_session), null, 3000);
        starter.AddPlayerLine("af_captivity_talk_again", "af_captivity_options", "af_captivity_listen",
            "{=AF_ENC_UI_CaptivityContinue}There is something I want to discuss.", null,
            () => ShoutBehavior.OpenNativeConversationInputSilentlyForExternal());
        starter.AddDialogLine("af_captivity_listen", "af_captivity_listen", "af_captivity_options",
            "{=AF_ENC_UI_CaptivityListen}I am listening.", null, null);
        starter.AddPlayerLine("af_captivity_leave", "af_captivity_options", "close_window",
            "{=AF_ENC_UI_CaptivityLeave}That is all for now.", null, null);
    }

    private static TextObject Ui(string key, string fallback) => new TextObject("{=AF_ENC_UI_Captivity" + key + "}" + fallback);
    private static void Notify(string key, string fallback) => InformationManager.DisplayMessage(new InformationMessage(Ui(key, fallback).ToString()));

    private static CaptureOpening CaptureOpeningIdentity(PartyBase holder, bool pending)
        => new CaptureOpening { Campaign = Campaign.Current, Generation = SaveRuntimeGuard.CaptureGeneration(),
            StartedAt = PlayerCaptivity.CaptivityStartTime, Holder = holder, OwnerClan = holder?.Settlement?.OwnerClan,
            Leader = holder?.IsMobile == true ? holder.LeaderHero : null, Pending = pending };

    internal static void OnPlayerTakenPrisoner(PartyBase capturer, Hero prisoner)
    {
        if (prisoner == null || prisoner != Hero.MainHero || Campaign.Current == null
            || !PlayerCaptivity.IsCaptive || !ReferenceEquals(capturer, PlayerCaptivity.CaptorParty)) return;
        if (_captureOpening != null && ReferenceEquals(_captureOpening.Campaign, Campaign.Current)
            && SaveRuntimeGuard.IsCurrentGeneration(_captureOpening.Generation)
            && _captureOpening.StartedAt == PlayerCaptivity.CaptivityStartTime) return;
        _captureOpening = CaptureOpeningIdentity(capturer, pending: true);
        _nextTick = 0f;
    }

    private static void TryOpenPendingCapture()
    {
        CaptureOpening capture = _captureOpening;
        if (capture?.Pending != true) return;
        try
        {
            if (!ReferenceEquals(capture.Campaign, Campaign.Current) || !SaveRuntimeGuard.IsCurrentGeneration(capture.Generation)
                || !PlayerCaptivity.IsCaptive || Hero.MainHero?.IsPrisoner != true
                || !Hero.MainHero.IsAlive || capture.Holder?.IsActive != true
                || capture.StartedAt != PlayerCaptivity.CaptivityStartTime
                || !ReferenceEquals(capture.Holder, PlayerCaptivity.CaptorParty)
                || !ReferenceEquals(capture.Holder, Hero.MainHero.PartyBelongedToAsPrisoner)
                || capture.Holder.PrisonRoster?.Contains(Hero.MainHero.CharacterObject) != true
                || !ReferenceEquals(capture.OwnerClan, capture.Holder?.Settlement?.OwnerClan)
                || (capture.Holder.IsMobile && !ReferenceEquals(capture.Leader, capture.Holder.LeaderHero)))
            {
                capture.Pending = false;
                return;
            }
            if (!CanOpenMenu() || InformationManager.IsAnyInquiryActive() || !IsHolderStable(capture.Holder)) return;
            capture.Pending = false; // Consume before entering native UI/callbacks; no automatic retry.
            var targets = GetTargets(capture.Holder);
            if (targets.Count > 0) TryOpen(targets[0], npcInitiated: true);
            else Logger.Log("PlayerCaptivity", "automatic_opening_skipped no_eligible_lord");
        }
        catch (Exception ex)
        {
            capture.Pending = false;
            Logger.Log("PlayerCaptivity", "automatic_opening_failed " + ex.GetType().Name);
        }
    }

    internal static bool HasPendingNativeOpeningForCurrentConversation()
        => _session?.OpeningPending == true && AllowsInteraction(_session.Target)
            && _session.Manager.IsConversationInProgress && ReferenceEquals(Hero.OneToOneConversationHero, _session.Target);

    internal static bool TryConsumePendingNativeOpening(Hero hero, out string extraFact, out string promptText)
    {
        extraFact = promptText = "";
        if (!HasPendingNativeOpeningForCurrentConversation() || !ReferenceEquals(hero, _session.Target)) return false;
        extraFact = PromptComposerTexts.Raw("Template_captivity_opening");
        promptText = EncounterPromptTexts.Get("PlayerCaptivityOpeningPrompt");
        _session.OpeningPending = false;
        return true;
    }

    private static bool IsHolderStable(PartyBase holder)
        => Campaign.Current != null && Hero.MainHero?.IsAlive == true && Hero.MainHero.IsPrisoner
            && PlayerCaptivity.IsCaptive && holder != null && holder.IsActive
            && ReferenceEquals(PlayerCaptivity.CaptorParty, holder)
            && ReferenceEquals(Hero.MainHero.PartyBelongedToAsPrisoner, holder)
            && holder.PrisonRoster?.Contains(Hero.MainHero.CharacterObject) == true
            && holder.MapEvent == null && holder.Settlement?.SiegeEvent == null
            && !PlayerEncounterCompat.IsInPostBattleResultFlow();

    private static bool Eligible(Hero hero, PartyBase holder)
    {
        if (hero == null || hero == Hero.MainHero || !hero.IsAlive || !hero.IsActive || hero.IsPrisoner
            || hero.PartyBelongedToAsPrisoner != null || hero.PartyBelongedTo?.MapEvent != null) return false;
        if (holder?.IsMobile == true) return hero.IsLord && ReferenceEquals(holder.LeaderHero, hero);
        Settlement settlement = holder?.Settlement;
        return settlement != null && (settlement.IsTown || settlement.IsCastle)
            && ReferenceEquals(hero.CurrentSettlement, settlement) && settlement.OwnerClan != null
            && ReferenceEquals(hero.Clan, settlement.OwnerClan)
            && (ReferenceEquals(hero, settlement.OwnerClan.Leader) || ReferenceEquals(hero, settlement.Town?.Governor));
    }

    internal static List<Hero> GetTargets(PartyBase holder)
    {
        var targets = new List<Hero>(2);
        if (!IsHolderStable(holder)) return targets;
        if (holder.IsMobile)
        {
            if (Eligible(holder.LeaderHero, holder)) targets.Add(holder.LeaderHero);
        }
        else if (holder.IsSettlement)
        {
            Hero owner = holder.Settlement.OwnerClan?.Leader;
            Hero governor = holder.Settlement.Town?.Governor;
            if (Eligible(owner, holder)) targets.Add(owner);
            if (!ReferenceEquals(owner, governor) && Eligible(governor, holder)) targets.Add(governor);
        }
        return targets;
    }

    private static bool CanOpenMenu()
        => _session == null && Campaign.Current?.ConversationManager?.IsConversationInProgress == false
            && Mission.Current == null && Game.Current?.GameStateManager?.ActiveState is MapState map && !map.MapConversationActive
            && (Campaign.Current.CurrentMenuContext?.GameMenu?.StringId == "prisoner_wait"
                || Campaign.Current.CurrentMenuContext?.GameMenu?.StringId == "settlement_wait");

    private static void ChooseTarget()
    {
        if (!CanOpenMenu()) return;
        PartyBase holder = PlayerCaptivity.CaptorParty;
        var targets = GetTargets(holder);
        if (targets.Count == 1) { TryOpen(targets[0]); return; }
        if (targets.Count == 0) { Notify("Unavailable", "No captor with authority is available to speak right now."); return; }
        Campaign campaign = Campaign.Current;
        long generation = SaveRuntimeGuard.CaptureGeneration();
        var choices = targets.ConvertAll(hero => new InquiryElement(hero, hero.Name.ToString(), null, true, ""));
        MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
            Ui("Talk", "Speak with your captor").ToString(), Ui("Choose", "Who would you like to speak with?").ToString(),
            choices, true, 1, 1, Ui("Select", "Speak").ToString(), Ui("Cancel", "Cancel").ToString(), selected =>
            {
                if (ReferenceEquals(campaign, Campaign.Current) && SaveRuntimeGuard.IsCurrentGeneration(generation)
                    && ReferenceEquals(holder, PlayerCaptivity.CaptorParty) && selected?.Count == 1)
                    TryOpen(selected[0].Identifier as Hero);
            }, null), pauseGameActiveState: true);
    }

    internal static bool TryOpen(Hero target, bool npcInitiated = false)
    {
        if (!CanOpenMenu()) return false;
        PartyBase holder = PlayerCaptivity.CaptorParty;
        if (!IsHolderStable(holder) || !Eligible(target, holder)) return false;
        var session = new Session { Campaign = Campaign.Current, Manager = Campaign.Current.ConversationManager,
            Holder = holder, Target = target, OwnerClan = holder.Settlement?.OwnerClan,
            Generation = SaveRuntimeGuard.CaptureGeneration(), PreviousTime = Campaign.Current.TimeControlMode,
            CaptivityStartedAt = PlayerCaptivity.CaptivityStartTime,
            NpcInitiated = npcInitiated, OpeningPending = npcInitiated };
        // A manual conversation before the deferred opening also satisfies this opportunity.
        if (_captureOpening != null) _captureOpening.Pending = false;
        _session = session;
        _nextTick = 0f;
        session.EndHandler = () => { if (ReferenceEquals(_session, session)) session.Ended = true; };
        session.Manager.ConversationEnd += session.EndHandler;
        try
        {
            Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
            Campaign.Current.CurrentConversationContext = ConversationContext.Default;
            IsOpening = true;
            CampaignMapConversation.OpenConversation(
                new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, noHorse: true, noWeapon: true, noBodyguards: true),
                new ConversationCharacterData(target.CharacterObject, holder, noHorse: true, noBodyguards: true));
            if (!OwnsCampaign(session) || !ReferenceEquals(Hero.OneToOneConversationHero, target))
                throw new InvalidOperationException("captivity_conversation_context_changed");
            ShoutBehavior.OpenNativeConversationInputSilentlyForExternal();
            Logger.Log("PlayerCaptivity", "conversation_opened target=" + target.StringId + " npcInitiated=" + npcInitiated);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log("PlayerCaptivity", "conversation_open_failed " + ex.GetType().Name);
            RequestClose(session);
            session.Ended = true;
            Notify("OpenFailed", "The conversation could not be opened. Your captivity has not been changed.");
            return false;
        }
        finally { IsOpening = false; }
    }

    private static bool OwnsCampaign(Session session)
        => session != null && ReferenceEquals(_session, session) && SaveRuntimeGuard.IsCurrentGeneration(session.Generation)
            && ReferenceEquals(session.Campaign, Campaign.Current) && ReferenceEquals(session.Manager, Campaign.Current?.ConversationManager);

    private static bool IsSessionValid(Session session)
        => OwnsCampaign(session) && Mission.Current == null && IsHolderStable(session.Holder) && Eligible(session.Target, session.Holder)
            && session.CaptivityStartedAt == PlayerCaptivity.CaptivityStartTime
            && ReferenceEquals(session.OwnerClan, session.Holder.Settlement?.OwnerClan);

    internal static bool AllowsInteraction(Hero target)
        => _session == null || (!_session.Ended && !_session.CloseRequested && IsSessionValid(_session) && ReferenceEquals(_session.Target, target));

    internal static bool CanRelease(Hero target)
        => _session != null && AllowsInteraction(target) && _session.Manager.IsConversationInProgress
            && ReferenceEquals(Hero.OneToOneConversationHero, target) && Releases.Pending == null;

    internal static bool ExcludesRule(string ruleId)
    {
        if (_session == null) return false;
        switch (ruleId)
        {
            case "duel": case "meeting_taunt": case "encounter_release_player": case "scene_mechanism_actions":
            case "worldmap_party_command": case "lords_hall_access": case "party_transfer": case "kingdom_service":
            case "hero_join_party": case "public_execution_start": case "noble_gathering": return true;
            default: return false;
        }
    }

    internal static void StripRestrictedTags(ref string text)
    {
        if (_session != null) text = FilterRestrictedTags(text);
    }

    internal static string FilterRestrictedTags(string text) => string.IsNullOrEmpty(text) ? text : RestrictedTags.Replace(text, "");
    internal static bool IsRestrictedTag(string tag) => !string.IsNullOrEmpty(tag) && RestrictedTags.IsMatch(tag);

    internal static string BuildRuntimeContext(Hero target)
    {
        if (_session == null || !AllowsInteraction(target)) return "";
        string authority = _session.Holder.IsMobile ? "PlayerCaptivityPartyAuthority"
            : ReferenceEquals(_session.OwnerClan.Leader, target) ? "PlayerCaptivityClanAuthority" : "PlayerCaptivityGovernorAuthority";
        return PromptComposerTexts.FormatTemplate(EncounterPromptTexts.Get("PlayerCaptivityContext"),
            ("PLAYER", Hero.MainHero.Name), ("CAPTOR", target.Name), ("HOLDER", _session.Holder.Name),
            ("AUTHORITY", EncounterPromptTexts.Get(authority)));
    }

    internal static List<PostprocessRuleEntry> BuildPostprocessRules(Hero target, string chainName, bool selected)
        => selected && chainName == "native_conversation" && CanRelease(target)
            ? AIConfigHandler.GetGuardrailRulePostprocessRules(RuleId) : null;

    internal static bool ConsumeReleaseTag(Hero target, ref string text)
    {
        if (string.IsNullOrEmpty(text) || !ReleaseRegex.IsMatch(text)) return false;
        text = ReleaseRegex.Replace(text, "").Trim();
        if (!CanRelease(target)) { Logger.Log("PlayerCaptivity", "release_rejected invalid_context"); return false; }
        Releases.Schedule(new ReleaseRequest { Session = _session, RequestedAt = Time.ApplicationTime,
            Fact = AdapterPromptCaptureTexts.PcapCompat("Host_AfefNpcPrefix") + PromptComposerTexts.Raw("Template_captivity_released") });
        Notify("ReleaseScheduled", "You have been granted release. Select Leave to return to the world map and end your captivity.");
        Logger.Log("PlayerCaptivity", "release_scheduled target=" + target.StringId);
        return true;
    }

    private static void RequestClose(Session session)
    {
        if (!OwnsCampaign(session) || session.CloseRequested) return;
        session.CloseRequested = true;
        if (session.Manager.IsConversationInProgress && ReferenceEquals(Hero.OneToOneConversationHero, session.Target))
            session.Manager.EndConversation();
    }

    // One target only, at most four validations per second. 1.5 needs the same
    // visibility repair on entering captivity (including bandits), without a conversation.
    internal static void Tick()
    {
        // Application ticks also run before a campaign exists and during teardown.
        // Native captivity static getters dereference both of these owners.
        if (Campaign.Current?.PlayerCaptivity == null) return;
        Session session = _session;
        bool maintainCaptor = false;
#if BANNERLORD_1_5_OR_GREATER
        maintainCaptor = PlayerCaptivity.IsCaptive;
#endif
        if ((session == null && _captureOpening?.Pending != true && !maintainCaptor) || Time.ApplicationTime < _nextTick) return;
        _nextTick = Time.ApplicationTime + 0.25f;
        if (maintainCaptor && session == null)
        {
            try
            {
                PartyBase holder = PlayerCaptivity.CaptorParty;
                string menu = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
                if (Mission.Current == null && Game.Current?.GameStateManager?.ActiveState is MapState map
                    && !map.MapConversationActive && (menu == "prisoner_wait" || menu == "settlement_wait")
                    && holder?.MobileParty?.IsVisible == false && IsHolderStable(holder))
                    CaptivityMapVisibilityCompat.RestoreCurrentCaptor(Campaign.Current, holder);
            }
            catch (Exception ex) { Logger.Log("PlayerCaptivity", "capture_visibility_failed " + ex.GetType().Name); }
        }
        if (session == null) { TryOpenPendingCapture(); return; }
        try
        {
            if (!OwnsCampaign(session)) { Reset(); return; }
            bool valid = IsSessionValid(session);
            if (!valid)
            {
                Releases.ClearPending();
                RequestClose(session);
            }
            if (!OwnsCampaign(session)) { Retire(session); return; }
            ReleaseRequest pending = Releases.Pending;
            // Authorization lasts for this valid conversation, not a reading-time deadline.
            // The player's normal Leave command ends it; never auto-close after an AI tag.
            if (session.Manager.IsConversationInProgress)
            {
                if (ReferenceEquals(Hero.OneToOneConversationHero, session.Target)) Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
                else ClearSession();
                return;
            }
            if (!(Game.Current?.GameStateManager?.ActiveState is MapState map) || map.MapConversationActive) return;
            if (pending != null && session.Ended && valid && Releases.ConsumeAuthorization(r => ReferenceEquals(r.Session, session) && IsSessionValid(session), Time.ApplicationTime, float.PositiveInfinity))
            {
                Releases.ClearPending(); // Retire before entering native callbacks; never retry a partially applied action.
                ExecuteRelease(pending);
            }
            RestoreMap(session);
            Retire(session);
        }
        catch (Exception ex)
        {
            Logger.Log("PlayerCaptivity", "session_tick_failed " + ex.GetType().Name);
            Releases.ClearPending();
            Notify("ReleaseFailed", "Release could not be confirmed. Please check your current captivity status.");
            try { RequestClose(session); }
            catch (Exception closeException) { Logger.Log("PlayerCaptivity", "session_close_failed " + closeException.GetType().Name); }
            Retire(session); // A broken native lifecycle must not become an endless tick/release retry.
        }
    }

    private static void ExecuteRelease(ReleaseRequest request)
    {
        Session session = request.Session;
        // Native release adds the player once. Do not duplicate an already-corrupt member entry.
        if (PartyBase.MainParty?.MemberRoster?.GetTroopCount(Hero.MainHero.CharacterObject) != 0)
        {
            Logger.Log("PlayerCaptivity", "release_rejected duplicate_main_hero_member");
            Notify("ReleaseFailed", "Release could not be confirmed. Please check your current captivity status.");
            return;
        }
        bool nativeFailed = false;
        try { EndCaptivityAction.ApplyByReleasedByChoice(Hero.MainHero); }
        catch (Exception ex) { nativeFailed = true; Logger.Log("PlayerCaptivity", "native_release_exception " + ex.GetType().Name); }
        if (!OwnsCampaign(session)) return; // Native events can replace the campaign or load a save.
        bool released = !PlayerCaptivity.IsCaptive && !Hero.MainHero.IsPrisoner
            && Hero.MainHero.PartyBelongedToAsPrisoner == null && !session.Holder.PrisonRoster.Contains(Hero.MainHero.CharacterObject);
        Logger.Log("PlayerCaptivity", "release_result success=" + released + " target=" + session.Target.StringId);
        if (!released) { Notify("ReleaseFailed", "Release could not be confirmed. Please check your current captivity status."); return; }
        MyBehavior.AppendExternalDialogueHistory(session.Target, null, null, request.Fact);
        if (nativeFailed) Notify("ReleaseFailed", "Release could not be confirmed. Please check your current captivity status.");
        else Notify("Released", "You have been released from captivity.");
    }

    private static void RestoreMap(Session session)
    {
        if (!OwnsCampaign(session)) return;
        if (PlayerCaptivity.IsCaptive && PlayerCaptivity.CaptorParty != null)
        {
            // Visibility is a cosmetic repair; it must never block the menu/time restore.
            try { CaptivityMapVisibilityCompat.RestoreCurrentCaptor(session.Campaign, session.Holder); }
            catch (Exception ex) { Logger.Log("PlayerCaptivity", "restore_visibility_failed " + ex.GetType().Name); }
            string menu = PlayerCaptivity.CaptorParty.IsSettlement ? "settlement_wait" : "prisoner_wait";
            if (Campaign.Current.CurrentMenuContext?.GameMenu?.StringId != menu) GameMenu.ActivateGameMenu(menu);
            Campaign.Current.TimeControlMode = session.PreviousTime;
        }
        else Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
    }

    internal static void Reset()
    {
        _captureOpening = null;
        ClearSession();
    }

    private static void ClearSession()
    {
        if (_session != null) _session.Manager.ConversationEnd -= _session.EndHandler;
        _session = null;
        Releases.ClearPending(); Releases.ClearAuthorization();
        IsOpening = false;
    }

    private static void Retire(Session session)
    {
        if (ReferenceEquals(_session, session)) ClearSession();
    }
}
