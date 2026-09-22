using System;
using AnimusForge;

internal static class Program
{
	private static int _assertions;

	private static void Expect(bool condition, string name)
	{
		_assertions++;
		if (!condition)
		{
			throw new InvalidOperationException("FAILED: " + name);
		}
	}

	private static void TestDirectDiplomacyWarGuard()
	{
		Expect(!DirectDiplomacyWarGuard.CanDeclareForPlayerKingdom(false, true), "non-sovereign player is rejected");
		Expect(!DirectDiplomacyWarGuard.CanDeclareForPlayerKingdom(true, false), "mismatched player kingdom is rejected");
		Expect(DirectDiplomacyWarGuard.CanDeclareForPlayerKingdom(true, true), "sovereign matching player kingdom is accepted");
		Expect(!DirectDiplomacyWarGuard.DidDeclarationTakeEffect(false), "blocked declaration is not published");
		Expect(DirectDiplomacyWarGuard.DidDeclarationTakeEffect(true), "observed declaration may be published");
	}

	private static void TestWorldDiplomacyRequestLease()
	{
		WorldDiplomacyRequestLeaseCoordinator owner = new WorldDiplomacyRequestLeaseCoordinator();
		Expect(owner.TryClaim("job-a", 7L, 12, 0, out WorldDiplomacyRequestSnapshot first), "first request claims lease");
		Expect(owner.IsRunning, "lease reports running");
		Expect(first.JobId == "job-a" && first.RuntimeGeneration == 7L, "snapshot freezes identity");
		Expect(first.MaxTokens == 256 && first.TimeoutMilliseconds == 1, "snapshot clamps transport bounds");
		Expect(!owner.TryClaim("job-b", 7L, 512, 30, out _), "second request cannot claim active lease");
		Expect(!owner.TryRelease("job-a", 6L), "old generation cannot release active lease");
		Expect(!owner.TryRelease("job-b", 7L), "other job cannot release active lease");
		Expect(owner.IsRunning, "rejected releases preserve owner");
		Expect(owner.TryRelease("job-a", 7L), "captured identity releases own lease");
		Expect(!owner.IsRunning, "released lease is idle");
		Expect(owner.TryClaim("job-a", 8L, 900, 1000, out WorldDiplomacyRequestSnapshot second), "same job id may be reused by new generation");
		Expect(!owner.TryRelease(first.JobId, first.RuntimeGeneration), "late prior generation cannot release reused job id");
		Expect(owner.IsRunning, "late completion leaves new generation active");
		Expect(owner.TryRelease(second.JobId, second.RuntimeGeneration), "new generation releases itself");
		Expect(!owner.TryClaim("", 9L, 900, 1000, out _), "empty job id is rejected");
		Expect(!owner.TryClaim("job-c", 0L, 900, 1000, out _), "invalid generation is rejected");
	}

	private static void TestWorldDiplomacyJobRuntime()
	{
		WorldDiplomacyJobQueueItem[] jobs =
		{
			new WorldDiplomacyJobQueueItem { JobId = "running", Priority = 99, CreatedDay = 1, IsRunning = true },
			new WorldDiplomacyJobQueueItem { JobId = "waiting", Priority = 90, CreatedDay = 1, AwaitingHistoryCompression = true },
			new WorldDiplomacyJobQueueItem { JobId = "older", Priority = 80, CreatedDay = 2, CacheAffinityKey = "other" },
			new WorldDiplomacyJobQueueItem { JobId = "affinity", Priority = 80, CreatedDay = 5, CacheAffinityKey = "cache-a" },
			new WorldDiplomacyJobQueueItem { JobId = "alphabetical", Priority = 80, CreatedDay = 2, CacheAffinityKey = "other" }
		};

		Expect(WorldDiplomacyJobRuntimeCoordinator.SelectNextJobId(jobs, false, "cache-a") == "affinity",
			"cache affinity wins within highest runnable priority");
		Expect(WorldDiplomacyJobRuntimeCoordinator.SelectNextJobId(jobs, true, "cache-a") == "waiting",
			"compression-ready higher priority job becomes runnable");
		Expect(WorldDiplomacyJobRuntimeCoordinator.SelectNextJobId(Array.Empty<WorldDiplomacyJobQueueItem>(), true, "") == "",
			"empty queue has no selection");
		Expect(WorldDiplomacyJobRuntimeCoordinator.IsCurrentCompletion("job", 8L, 8L, false),
			"matching runtime completion is current");
		Expect(!WorldDiplomacyJobRuntimeCoordinator.IsCurrentCompletion("job", 7L, 8L, false),
			"old generation completion is stale");
		Expect(!WorldDiplomacyJobRuntimeCoordinator.IsCurrentCompletion("job", 8L, 8L, true),
			"save-runtime stale completion is rejected");
		Expect(!WorldDiplomacyJobRuntimeCoordinator.IsCurrentCompletion("", 8L, 8L, false),
			"completion without job identity is rejected");
		Expect(WorldDiplomacyJobRuntimeCoordinator.Classify("generate") == WorldDiplomacyJobRoute.Generate,
			"generate route is classified");
		Expect(WorldDiplomacyJobRuntimeCoordinator.Classify("ANALYZE") == WorldDiplomacyJobRoute.Analyze,
			"analyze route is case insensitive");
		Expect(WorldDiplomacyJobRuntimeCoordinator.Classify("compress") == WorldDiplomacyJobRoute.Compress,
			"compress route is classified");
		Expect(WorldDiplomacyJobRuntimeCoordinator.Classify("round_plan") == WorldDiplomacyJobRoute.RoundPlan,
			"round plan route is classified");
		Expect(WorldDiplomacyJobRuntimeCoordinator.Classify("round_compress") == WorldDiplomacyJobRoute.RoundCompress,
			"round compression route is classified");
		Expect(WorldDiplomacyJobRuntimeCoordinator.Classify("future-kind") == WorldDiplomacyJobRoute.Unknown,
			"unknown route fails closed");
	}

	private static void TestWorldMapDelayedRequestOwnership()
	{
		WorldMapDelayedRequestCoordinator owner = new WorldMapDelayedRequestCoordinator();
		Expect(owner.TryBegin(out long first), "first native screen request begins");
		Expect(owner.HasActiveRequest, "screen request reports active");
		Expect(!owner.TryBegin(out _), "second screen cannot overlap first");
		Expect(owner.TryClaimCompletion(first), "first callback claims its request once");
		Expect(!owner.TryClaimCompletion(first), "duplicate callback is ignored");
		Expect(owner.TryBegin(out long second), "new request begins after first completion");
		Expect(!owner.TryClaimCompletion(first), "stale callback cannot claim new request");
		Expect(owner.HasActiveRequest, "stale callback cannot clear new request busy state");
		Expect(!owner.TryCancelOpen(first), "old opener cannot cancel new request");
		Expect(owner.TryCancelOpen(second), "current opener can cancel its own request");
		Expect(!owner.HasActiveRequest, "owned cancel releases busy state");
	}

	private static void TestWorldMapProtocolAndAdmission()
	{
		Expect(WorldMapOrderCoordinator.TryParseToken(" [ACTION:WORLDMAP_ORDER:STOP] ", out WorldMapOrderToken stop)
			&& stop.IsStop && stop.Kind == "STOP", "STOP protocol token parses");
		Expect(WorldMapOrderCoordinator.TryParseToken("[ACTION:WORLDMAP_ORDER:ATTACK:hero:lord_1:3:force]", out WorldMapOrderToken attack)
			&& attack.Parts.Length == 5 && attack.Parts[2] == "lord_1", "attack protocol preserves detached fields");
		Expect(!WorldMapOrderCoordinator.TryParseToken("[ACTION:WORLDMAP_ORDER:]", out _), "empty protocol kind is rejected");
		Expect(!WorldMapOrderCoordinator.TryParseToken("ACTION:WORLDMAP_ORDER:STOP", out _), "unframed protocol token is rejected");
		Expect(WorldMapOrderCoordinator.IsSafeIdentifier("party_1"), "safe runtime id is accepted");
		Expect(!WorldMapOrderCoordinator.IsSafeIdentifier("party]bad"), "framing characters are rejected in ids");

		WorldMapOrderSequenceCoordinator sequence = new WorldMapOrderSequenceCoordinator();
		Expect(sequence.Observe(true) == WorldMapOrderSequenceDecision.AcceptLeadingStop && sequence.LeadingStop,
			"first STOP is accepted as leading reset");
		Expect(sequence.Observe(false) == WorldMapOrderSequenceDecision.AcceptCommand,
			"command after leading STOP is accepted");
		Expect(sequence.Observe(true) == WorldMapOrderSequenceDecision.RejectNonLeadingStop,
			"later STOP is rejected without changing leading reset");

		Expect(WorldMapOrderCoordinator.SelectAdmissionRoute(false, false, false, false) == WorldMapOrderAdmissionRoute.NonHeroParty,
			"non-hero speech routes to party owner");
		Expect(WorldMapOrderCoordinator.SelectAdmissionRoute(true, true, true, true) == WorldMapOrderAdmissionRoute.CompanionPartyCreation,
			"main-party hero task keeps companion creation precedence");
		Expect(WorldMapOrderCoordinator.SelectAdmissionRoute(true, false, true, true) == WorldMapOrderAdmissionRoute.GovernorExpedition,
			"governor task routes to expedition owner");
		Expect(WorldMapOrderCoordinator.SelectAdmissionRoute(true, false, false, false) == WorldMapOrderAdmissionRoute.ExistingHeroParty,
			"ordinary hero routes to existing party queue");
		Expect(WorldMapOrderCoordinator.ClassifyCommand("AttackParty") == WorldMapCommandRoute.AttackParty,
			"persisted command kind routes case-insensitively");
		Expect(WorldMapOrderCoordinator.ClassifyCommand("future") == WorldMapCommandRoute.Unknown,
			"unknown persisted command fails closed");
	}

	private static void TestGovernorPendingRequestOwnership()
	{
		WorldMapPendingRequestCoordinator owner = new WorldMapPendingRequestCoordinator();
		long first = owner.GetOrCreate("governor-a");
		Expect(first > 0L && owner.GetOrCreate("governor-a") == first,
			"merged pending request retains one owner ticket");
		Expect(owner.IsCurrent("governor-a", first), "pending ticket is current");
		Expect(!owner.TryClaim("governor-a", first + 1L), "foreign ticket cannot claim pending request");
		Expect(owner.TryClaim("governor-a", first), "pending request claims once");
		long second = owner.GetOrCreate("governor-a");
		Expect(second != first && second > 0L, "new request receives new ticket");
		Expect(!owner.TryCancel("governor-a", first), "old request cannot cancel replacement");
		Expect(owner.IsCurrent("governor-a", second), "replacement remains current after stale cancel");
		owner.Reset();
		Expect(!owner.IsCurrent("governor-a", second), "runtime reset invalidates process-local pending requests");
	}

	private static int Main()
	{
		TestDirectDiplomacyWarGuard();
		TestWorldDiplomacyRequestLease();
		TestWorldDiplomacyJobRuntime();
		TestWorldMapDelayedRequestOwnership();
		TestWorldMapProtocolAndAdmission();
		TestGovernorPendingRequestOwnership();
		Console.WriteLine("J12 domain lifecycle regression tests passed: " + _assertions);
		return 0;
	}
}
