using AnimusForge;

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

ConversationMessage Fact(int sequence) => new ConversationMessage
{
    EventSequence = sequence,
    Role = "system",
    Content = "[AFEF] fact-" + sequence
};

Case("bounded-oldest-eviction", () =>
{
    ScenePendingAfefFactsOwner owner = new ScenePendingAfefFactsOwner();
    for (int i = 1; i <= 14; i++)
    {
        owner.Queue(7, Fact(i));
    }
    List<ConversationMessage> facts = owner.Consume(7);
    Require(facts.Count == ScenePendingAfefFactsOwner.MaxPendingFactsPerAgent, "pending facts were not bounded");
    Require(facts[0].EventSequence == 3 && facts[^1].EventSequence == 14, "oldest-first eviction/order changed");
});

Case("consume-is-one-shot", () =>
{
    ScenePendingAfefFactsOwner owner = new ScenePendingAfefFactsOwner();
    owner.Queue(7, Fact(1));
    Require(owner.Consume(7).Count == 1, "first consume lost the fact");
    Require(owner.Consume(7).Count == 0, "second consume replayed the fact");
});

Case("agents-are-isolated", () =>
{
    ScenePendingAfefFactsOwner owner = new ScenePendingAfefFactsOwner();
    ConversationMessage first = Fact(1);
    ConversationMessage second = Fact(2);
    owner.Queue(7, first);
    owner.Queue(8, second);
    Require(ReferenceEquals(owner.Consume(7).Single(), first), "agent 7 received another agent's fact");
    Require(ReferenceEquals(owner.Consume(8).Single(), second), "agent 8 fact was removed by agent 7");
});

Case("clear-retires-all-agents", () =>
{
    ScenePendingAfefFactsOwner owner = new ScenePendingAfefFactsOwner();
    owner.Queue(7, Fact(1));
    owner.Queue(8, Fact(2));
    owner.Clear();
    Require(owner.Consume(7).Count == 0 && owner.Consume(8).Count == 0, "clear left a pending fact");
});

Case("invalid-input-fails-closed", () =>
{
    ScenePendingAfefFactsOwner owner = new ScenePendingAfefFactsOwner();
    owner.Queue(-1, Fact(1));
    owner.Queue(7, null);
    Require(owner.Consume(-1).Count == 0 && owner.Consume(7).Count == 0, "invalid input created pending state");
});

Console.WriteLine($"ScenePendingAfefFactsOwner cases={passed + failed} PASS={passed} FAIL={failed}");
return failed == 0 ? 0 : 1;
