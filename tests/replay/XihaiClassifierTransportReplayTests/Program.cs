using AnimusForge.SceneActions.Core;
using AnimusForge.XihaiAction;

static void AssertTrue(bool value, string message)
{
    if (!value) throw new InvalidOperationException(message);
}

FakeTransport transport = new FakeTransport();
using (AfV130AuxiliaryTextClassifier classifier = new AfV130AuxiliaryTextClassifier(transport))
{
    string none = await classifier.ClassifyAsync(new ClassifierRequest
    {
        InputSource = SceneInputSource.PlayerSceneShout,
        Text = "hello",
        AllowedIntentKeys = new[] { SceneActionFrameworkV4.Kneel, SceneActionFrameworkV4.Fear },
        ImplicitEmotionIntentKeys = new[] { SceneActionFrameworkV4.Fear }
    }, CancellationToken.None);
    AssertTrue(none == "NONE", "classifier response was not returned");
    AssertTrue(transport.Calls.Count == 1 && transport.Calls[0].OutputTokenLimit == 32, "ordinary classifier token limit mismatch");
    AssertTrue(transport.Calls[0].Messages.Count == 2, "ordinary classifier did not freeze two messages");
    AssertTrue(transport.Calls[0].Messages[0].Contains("system", StringComparison.Ordinal), "ordinary classifier system message missing");
    AssertTrue(transport.Calls[0].Messages[1].Contains("untrustedText", StringComparison.Ordinal), "ordinary classifier payload missing");

    int beforeShortCircuit = transport.Calls.Count;
    string shortCircuit = await classifier.ClassifyAsync(new ClassifierRequest
    {
        InputSource = SceneInputSource.PlayerSceneShout,
        Text = "",
        AllowedIntentKeys = new[] { SceneActionFrameworkV4.Kneel }
    }, CancellationToken.None);
    AssertTrue(shortCircuit == "NONE" && transport.Calls.Count == beforeShortCircuit, "empty classifier input was not short-circuited");

    bool rejected = false;
    try
    {
        await classifier.ClassifyAsync(new ClassifierRequest
        {
            InputSource = SceneInputSource.PlayerSceneShout,
            Text = "invalid",
            AllowedIntentKeys = new[] { "act_raw_native_id" }
        }, CancellationToken.None);
    }
    catch (InvalidOperationException) { rejected = true; }
    AssertTrue(rejected, "classifier accepted an out-of-contract allow-list key");

    string consent = await classifier.ClassifyConsentAsync(new ConsentClassifierRequest
    {
        FrozenProgram = SceneActionFrameworkV4.Kneel,
        ReplyText = "我同意"
    }, CancellationToken.None);
    AssertTrue(consent == "NONE", "consent classifier response was not returned");
    AssertTrue(transport.Calls[^1].OutputTokenLimit == 8, "consent classifier token limit mismatch");

    string trigger = await classifier.ClassifyBattleSpeechTriggerAsync(new BattleSpeechTriggerClassifierRequestV2
    {
        PlayerText = "向前推进",
        HasPrimaryNpcTarget = true
    }, CancellationToken.None);
    AssertTrue(trigger == "NONE" && transport.Calls[^1].OutputTokenLimit == 8, "battle speech trigger contract mismatch");

    string plan = await classifier.ClassifyBattleSpeechPlanAsync(new BattleSpeechPlanClassifierRequestV2
    {
        SpeechText = "士兵们，向前推进！",
        AllowedIntentKeys = new[] { SceneActionFrameworkV4.Command },
        AllowAdvance = true,
        AudienceReplyCount = 2,
        AudienceReplyMinimumChars = 8,
        AudienceReplyMaximumChars = 24
    }, CancellationToken.None);
    AssertTrue(plan == "NONE" && transport.Calls[^1].OutputTokenLimit >= 192, "battle speech plan contract mismatch");
}

FakeTransport singleFlightTransport = new FakeTransport { DelayMilliseconds = 100 };
using (AfV130AuxiliaryTextClassifier classifier = new AfV130AuxiliaryTextClassifier(singleFlightTransport))
{
    Task<string> first = classifier.ClassifyAsync(new ClassifierRequest
    {
        InputSource = SceneInputSource.PlayerSceneShout,
        Text = "first",
        AllowedIntentKeys = new[] { SceneActionFrameworkV4.Kneel }
    }, CancellationToken.None);
    Task<string> second = classifier.ClassifyAsync(new ClassifierRequest
    {
        InputSource = SceneInputSource.PlayerSceneShout,
        Text = "second",
        AllowedIntentKeys = new[] { SceneActionFrameworkV4.Kneel }
    }, CancellationToken.None);
    await Task.WhenAll(first, second);
    AssertTrue(singleFlightTransport.MaximumActive == 1, "ordinary classifier requests were not single-flight");
}

FakeTransport cancellationTransport = new FakeTransport { BlockUntilCancelled = true };
using (AfV130AuxiliaryTextClassifier classifier = new AfV130AuxiliaryTextClassifier(cancellationTransport))
{
    Task<string> pending = classifier.ClassifyAsync(new ClassifierRequest
    {
        InputSource = SceneInputSource.PlayerSceneShout,
        Text = "cancel me",
        AllowedIntentKeys = new[] { SceneActionFrameworkV4.Kneel }
    }, CancellationToken.None);
    await cancellationTransport.Started.Task;
    classifier.Dispose();
    bool cancelled = false;
    try { await pending; } catch (OperationCanceledException) { cancelled = true; }
    AssertTrue(cancelled, "classifier dispose did not cancel the in-flight transport");
}

Console.WriteLine("PASS xihaiClassifierTransportReplay shortCircuit=1 closedSet=1 ordinarySingleFlight=1 consentLimit=1 battleSpeechLimits=1 lifetimeCancellation=1");

internal sealed class FakeTransport : IAfClassifierTransport
{
    private int _active;
    public List<FakeCall> Calls { get; } = new List<FakeCall>();
    public int MaximumActive { get; private set; }
    public int DelayMilliseconds { get; set; }
    public bool BlockUntilCancelled { get; set; }
    public TaskCompletionSource<bool> Started { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task<string> SendAsync(List<object> messages, int outputTokenLimit, CancellationToken cancellationToken)
    {
        int active = Interlocked.Increment(ref _active);
        lock (this)
        {
            if (active > MaximumActive) MaximumActive = active;
            Calls.Add(new FakeCall(messages.Select(message => message?.ToString() ?? string.Empty).ToList(), outputTokenLimit));
        }
        Started.TrySetResult(true);
        try
        {
            if (BlockUntilCancelled)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            else if (DelayMilliseconds > 0)
            {
                await Task.Delay(DelayMilliseconds, cancellationToken);
            }
            return "NONE";
        }
        finally
        {
            Interlocked.Decrement(ref _active);
        }
    }

    public void Dispose() { }
}

internal sealed record FakeCall(List<string> Messages, int OutputTokenLimit);
