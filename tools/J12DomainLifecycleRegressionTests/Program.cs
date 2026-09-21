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

	private static int Main()
	{
		TestDirectDiplomacyWarGuard();
		TestWorldDiplomacyRequestLease();
		TestWorldMapDelayedRequestOwnership();
		Console.WriteLine("J12 domain lifecycle regression tests passed: " + _assertions);
		return 0;
	}
}
