using System;
using System.IO;

internal static class J13E3DomainOwnerContractReplay
{
    internal static void Run(string repo)
    {
        string Read(string path) => File.ReadAllText(Path.Combine(repo, path));
        void Require(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("J13E3 owner contract: " + label);
        }
        int Count(string source, string value) => source.Split(value, StringSplitOptions.None).Length - 1;

        string campaign = Read("src/AF.GameAdapter.Bannerlord/Composition/CampaignComposition.cs");
        string ticks = Read("src/AF.GameAdapter.Bannerlord/Composition/ApplicationTickComposition.cs");
        string patches = Read("src/AF.GameAdapter.Bannerlord/Composition/StartupPatchComposition.cs");
        string host = Read("LordEncounterBehavior.cs");
        string resolver = Read("EncounterConversationTargetResolver.cs");
        string target = Read("src/modules/AF.Module.Encounter/EncounterTargetOwner.cs");
        string conversation = Read("src/modules/AF.Module.Encounter/EncounterConversationTargetOwner.cs");
        string release = Read("src/modules/AF.Module.Encounter/EncounterReleaseOwner.cs");
        string pendingReturn = Read("src/modules/AF.Module.Encounter/EncounterPendingReturnOwner.cs");

        Require(Count(campaign, "campaignGameStarter.AddBehavior(new LordEncounterBehavior())") == 1
            && ticks.Contains("LordEncounterBehavior.OnEngineTick()", StringComparison.Ordinal)
            && host.Contains("public override void RegisterEvents()", StringComparison.Ordinal)
            && host.Contains("public override void SyncData(IDataStore dataStore)", StringComparison.Ordinal),
            "original Campaign/tick/event/save host is disconnected");
        foreach (string patch in new[] { "Patch_Conversation_Start_Intercept",
            "Patch_ConversationManager_OpenMapConversation", "Patch_ConversationManager_SetupAndStartMapConversation" })
        {
            string source = Read(patch + ".cs");
            Require(Count(patches, patch + ".ManualPatch(harmony)") == 1
                && source.Contains("EncounterConversationTargetResolver.TryResolveExplicitPrisonerFromArguments(__args)", StringComparison.Ordinal)
                && source.Contains("EncounterConversationTargetResolver.TryResolveLordFromArgumentsThenEncounterLeader(", StringComparison.Ordinal)
                && source.Contains("LordEncounterBehavior.SetTarget(hero)", StringComparison.Ordinal),
                "native conversation patch target/selected hero handoff drifted: " + patch);
        }
        Require(resolver.Contains("return EncounterConversationTargetOwner.Resolve(instance, args, ExtractHero, UsableLord, EncounterLeader);", StringComparison.Ordinal)
            && resolver.Contains("hero.IsPrisoner || hero.PartyBelongedToAsPrisoner != null", StringComparison.Ordinal)
            && conversation.Contains("foreach (object arg in args)", StringComparison.Ordinal)
            && conversation.IndexOf("foreach (object arg in args)", StringComparison.Ordinal)
                < conversation.IndexOf("TTarget instanceTarget = extract(instance)", StringComparison.Ordinal)
            && conversation.IndexOf("TTarget instanceTarget = extract(instance)", StringComparison.Ordinal)
                < conversation.IndexOf("TTarget fallback = encounterFallback()", StringComparison.Ordinal),
            "argument > instance > encounter leader or explicit prisoner bypass drifted");
        Require(host.Contains("private static readonly EncounterTargetOwner<Hero, PartyBase> _targetOwner", StringComparison.Ordinal)
            && host.Contains("_targetOwner.Set(target)", StringComparison.Ordinal)
            && host.Contains("_targetOwner.Ensure(encounteredParty, TargetEligibility, TargetFallback", StringComparison.Ordinal)
            && host.Contains("!IsEncounterArmyMemberTarget(hero, encounterParty)", StringComparison.Ordinal)
            && host.Contains("leaderParty.AttachedParties.Contains(targetParty)", StringComparison.Ordinal)
            && target.Contains("if (Target != null && eligible(Target, encounter)) return Target;", StringComparison.Ordinal),
            "selected army member retention or encounter change fallback drifted");
        Require(host.Contains("private static readonly EncounterReleaseOwner<MeetingPlayerReleaseRequest> _releaseOwner", StringComparison.Ordinal)
            && host.Contains("_releaseOwner.Authorize(CaptureMeetingPlayerReleaseRequest(_targetHero, reason))", StringComparison.Ordinal)
            && host.Contains("_releaseOwner.ConsumeAuthorization(", StringComparison.Ordinal)
            && host.Contains("_releaseOwner.Schedule(request)", StringComparison.Ordinal)
            && host.Contains("_releaseOwner.HasCurrentPending(ReleaseRequestCurrent,", StringComparison.Ordinal)
            && host.Contains("_releaseOwner.TryBeginPendingAttempt(applicationTime, missionActive,", StringComparison.Ordinal)
            && release.Contains("ReferenceEquals(request.EncounterIdentity, encounter)", StringComparison.Ordinal)
            && release.Contains("ReferenceEquals(request.PartyIdentity, party)", StringComparison.Ordinal)
            && release.Contains("ReferenceEquals(request.SourceMissionIdentity, mission)", StringComparison.Ordinal)
            && release.Contains("now - request.RequestedAt > lifetimeSeconds", StringComparison.Ordinal)
            && host.Contains("request.SourceMission == null || !ReferenceEquals(request.SourceMission, Mission.Current)", StringComparison.Ordinal),
            "native release authorization/delay/exact Mission guard drifted");
        Require(host.Contains("private static readonly EncounterPendingReturnOwner<PlayerEncounter, PartyBase> _pendingReturnOwner", StringComparison.Ordinal)
            && host.Contains("_pendingReturnOwner.Mark(PlayerEncounter.Current, GetCurrentEncounterPartySafe(),", StringComparison.Ordinal)
            && host.Contains("_pendingReturnOwner.IsCurrent(PlayerEncounter.Current, GetCurrentEncounterPartySafe(),", StringComparison.Ordinal)
            && host.Contains("MarkPendingReturnToEncounterMenuAfterUnauthorizedMeetingExit(\"meeting_mission_exit_without_release\")", StringComparison.Ordinal)
            && pendingReturn.Contains("ReferenceEquals(_encounter, encounter)", StringComparison.Ordinal)
            && pendingReturn.Contains("ReferenceEquals(_party, party) && _generation == generation", StringComparison.Ordinal),
            "unauthorized mission exit return or exact encounter/party/save guard drifted");

        Console.WriteLine("PASS J13E3DomainOwnerContractReplay campaign/tick/three patches=retained conversation/target/release/return=owned; source-wiring-only live=NOT_RUN");
    }
}
