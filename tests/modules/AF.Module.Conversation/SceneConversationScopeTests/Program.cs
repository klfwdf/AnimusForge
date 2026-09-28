using AnimusForge;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

int passed = 0;
int failed = 0;

void Case(string name, Action body)
{
    try
    {
        body();
        passed++;
        Console.WriteLine("PASS " + name);
    }
    catch (Exception exception)
    {
        failed++;
        Console.WriteLine("FAIL " + name + ": " + exception.Message);
    }
}

void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

Agent NewAgent(Mission mission, int index, float x, string id, bool hero = false)
{
    BasicCharacterObject character = new BasicCharacterObject { StringId = id, IsHero = hero };
    return new Agent
    {
        Index = index,
        Character = character,
        Origin = new AgentOrigin { Troop = character },
        Position = new Vec3(x, 0f, 0f),
        Mission = mission
    };
}

(Mission Mission, Agent Player, Agent Primary, Agent Secondary, Agent Observer, SceneShoutConversationScope Scope) World()
{
    Mission mission = new Mission();
    Agent player = NewAgent(mission, 1, 0f, "player", hero: true);
    Agent primary = NewAgent(mission, 2, 2f, "primary", hero: true);
    Agent secondary = NewAgent(mission, 3, 4f, "secondary");
    Agent observer = NewAgent(mission, 4, 6f, "observer");

    bool created = SceneShoutConversationScope.TryCreate(
        mission,
        conversationEpoch: 7,
        primary,
        player,
        primary.Position,
        player.Position,
        new[] { secondary, primary },
        new[] { primary, secondary },
        new[] { observer, secondary },
        out SceneShoutConversationScope scope);
    Require(created && scope != null, "valid scope was rejected");
    return (mission, player, primary, secondary, observer, scope);
}

Case("origin-flags-merged", () =>
{
    var world = World();
    Require(world.Scope.Count == 3, "audience was not deduplicated");
    Require(ReferenceEquals(world.Scope.Entries[0].AgentReference, world.Primary), "primary did not remain first");
    Require(ReferenceEquals(world.Scope.Entries[1].AgentReference, world.Secondary), "framed order changed");
    Require(world.Scope.FramedCount == 2, "framed count changed");
    Require(world.Scope.PrimaryAnchorCount == 2, "primary anchor count changed");
    Require(world.Scope.PlayerAnchorCount == 2, "player anchor count changed");
    Require(world.Scope.Primary.IsPrimary && world.Scope.Primary.IsFramed && world.Scope.Primary.IsFromPrimaryAnchor,
        "primary origins were not merged");
    Require(world.Scope.TryGetEntry(world.Secondary.Index, out SceneShoutAudienceEntry secondary)
        && secondary.IsFramed
        && secondary.IsFromPrimaryAnchor
        && secondary.IsFromPlayerAnchor,
        "secondary origins were not merged");
});

Case("epoch-mismatch-rejected", () =>
{
    var world = World();
    SceneShoutLiveValidationResult result = world.Scope.ValidateLiveAgent(
        world.Mission,
        currentConversationEpoch: 8,
        world.Primary.Index,
        world.Primary,
        requireActiveSpeaker: true,
        out _);
    Require(result == SceneShoutLiveValidationResult.EpochMismatch, "stale epoch was accepted: " + result);
});

Case("reference-reuse-rejected", () =>
{
    var world = World();
    Agent replacement = new Agent
    {
        Index = world.Primary.Index,
        Character = world.Primary.Character,
        Origin = world.Primary.Origin,
        Position = world.Primary.Position,
        Mission = world.Mission
    };
    SceneShoutLiveValidationResult result = world.Scope.ValidateLiveAgent(
        world.Mission,
        currentConversationEpoch: 7,
        replacement.Index,
        replacement,
        requireActiveSpeaker: true,
        out _);
    Require(result == SceneShoutLiveValidationResult.AgentReferenceMismatch, "reused index/reference was accepted: " + result);
});

Case("identity-and-liveness-revalidated", () =>
{
    var world = World();
    Require(world.Scope.ValidateLiveAgent(world.Mission, 7, world.Primary.Index, world.Primary, true, out _)
        == SceneShoutLiveValidationResult.Valid, "current primary was rejected");
    world.Primary.Character = new BasicCharacterObject { StringId = "replacement", IsHero = true };
    Require(world.Scope.ValidateLiveAgent(world.Mission, 7, world.Primary.Index, world.Primary, true, out _)
        == SceneShoutLiveValidationResult.CharacterIdentityMismatch, "changed character identity was accepted");

    var inactiveWorld = World();
    inactiveWorld.Primary.Active = false;
    Require(inactiveWorld.Scope.ValidateLiveAgent(
        inactiveWorld.Mission, 7, inactiveWorld.Primary.Index, inactiveWorld.Primary, true, out _)
        == SceneShoutLiveValidationResult.Inactive, "inactive speaker was accepted");
});

Case("invalid-capture-fails-closed", () =>
{
    var world = World();
    world.Primary.Mission = new Mission();
    Require(!SceneShoutConversationScope.TryCreate(
        world.Mission,
        9,
        world.Primary,
        world.Player,
        world.Primary.Position,
        world.Player.Position,
        Array.Empty<Agent>(),
        Array.Empty<Agent>(),
        Array.Empty<Agent>(),
        out _),
        "mission-mismatched primary was captured");
});

Console.WriteLine($"SceneConversationScope cases={passed + failed} PASS={passed} FAIL={failed}");
return failed == 0 ? 0 : 1;
