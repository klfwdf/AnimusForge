using System.Reflection;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;

internal static class LegacyAnonymousPromptTests
{
    internal static void Run()
    {
        int count = 0;
        var failures = new List<string>();
        void Case(string name, Action action)
        {
            count++;
            try { action(); }
            catch (Exception exception) { failures.Add(name + ": " + exception.Message); }
        }
        Case("real-anonymous-shape-order-whitespace-budget", () =>
        {
            var messages = new List<object>
            {
                new { role = "system", content = "  system\r\nline\t " },
                new { role = " USER ", content = "\nplayer letter\n" },
                new { role = "assistant", content = " response " }
            };
            PromptPackage package = LegacyPromptPackageAdapter.FromLegacyMessages(messages, 5000, "model-owner");
            messages.Clear();
            Check(package.MaxTokens == 5000 && package.Model == "model-owner", "token budget or model changed");
            Check(package.Messages.Select(item => item.Role).SequenceEqual(new[] { "system", "user", "assistant" }), "message order/roles changed");
            Check(package.Messages.Select(item => item.Content).SequenceEqual(new[] { "  system\r\nline\t ", "\nplayer letter\n", " response " }), "content whitespace changed or source collection retained");
        });
        Case("read-only-string-properties-no-extra-getter", () =>
        {
            var message = new ReadOnlyMessage();
            PromptPackage package = Convert(message);
            Check(package.Messages.Single().Role == "assistant" && package.Messages.Single().Content == "read only", "string property shape not supported");
            Check(message.RoleReads == 1 && message.ContentReads == 1 && message.ExtraReads == 0, "adapter serialized unknown properties or read twice");
        });
        Case("anonymous-extra-value-never-serialized", () =>
        {
            PromptPackage package = Convert(new { role = "user", content = "only text", extra = new UnknownMessage() });
            Check(package.Messages.Single().Content == "only text", "anonymous extra value affected prompt");
        });
        Case("string-object-dictionary-compatibility", () =>
        {
            var map = new Dictionary<string, object> { ["role"] = "assistant", ["content"] = "original", ["extra"] = new UnknownMessage() };
            PromptPackage package = Convert(map);
            map["content"] = "changed";
            Check(package.Messages.Single().Role == "assistant" && package.Messages.Single().Content == "original", "dictionary compatibility/copy failed");
        });
        Case("string-dictionary-compatibility", () =>
        {
            PromptPackage package = Convert(new Dictionary<string, string> { ["role"] = "system", ["content"] = "rules" });
            Check(package.Messages.Single().Role == "system" && package.Messages.Single().Content == "rules", "string dictionary changed");
        });
        Case("unknown-role-fallback", () => Check(Convert(new { role = "unknown", content = "text" }).Messages.Single().Role == "user", "unknown role no longer maps to user"));
        Case("empty-messages-filtered", () =>
        {
            PromptPackage package = LegacyPromptPackageAdapter.FromLegacyMessages(new object[] { null, new { role = "user", content = " \n\t" }, new { role = "user", content = (string)null } }, 5000, "fixture");
            Check(package.Messages.Count == 0, "empty message was included");
        });
        Case("unknown-shape-no-stringification", () =>
        {
            PromptPackage package = LegacyPromptPackageAdapter.FromLegacyMessages(new object[] { new UnknownMessage(), new object(), "not a message", 12 }, 5000, "fixture");
            Check(package.Messages.Count == 0, "unknown message shape was accepted");
        });
        Case("non-string-properties-rejected-without-read", () =>
        {
            var wrong = new WrongPropertyType();
            Check(Convert(wrong).Messages.Count == 0 && wrong.Reads == 0, "non-string properties were read or stringified");
        });
        Case("roundtrip-keeps-whitespace-and-order", () =>
        {
            PromptPackage original = LegacyPromptPackageAdapter.FromLegacyMessages(new object[] { new { role = "system", content = " x\n " }, new { role = "user", content = " z " } }, 5000, "fixture");
            PromptPackage roundtrip = LegacyPromptPackageAdapter.FromLegacyMessages(LegacyPromptPackageAdapter.ToLegacyMessages(original), original.MaxTokens, original.Model);
            Check(roundtrip.Messages.Select(item => item.Content).SequenceEqual(original.Messages.Select(item => item.Content)) && roundtrip.MaxTokens == 5000, "roundtrip changed strings/budget");
        });
        Case("throwing-owner-getter-fails-closed", () => ExpectFailure<TargetInvocationException>(() => Convert(new ThrowingMessage())));
        Case("ambiguous-shape-fails-closed", () => ExpectFailure<AmbiguousMatchException>(() => Convert(new AmbiguousMessage())));
        Case("concurrent-accessor-cache-shapes-isolated", () =>
        {
            Parallel.For(0, 64, index =>
            {
                object message = index % 2 == 0 ? new { role = "system", content = "a" + index } : new { Role = "assistant", Content = "b" + index };
                PromptMessage copied = Convert(message).Messages.Single();
                Check(copied.Role == (index % 2 == 0 ? "system" : "assistant") && copied.Content == (index % 2 == 0 ? "a" : "b") + index, "cached accessors crossed types/instances");
            });
        });
        if (failures.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
        Console.WriteLine("PASS legacyAnonymousPrompt cases=" + count + " realAdapter=true stringPropertiesOnly=true legacyDictionaryCoercion=unchanged game=NOT_RUN");
    }

    private static PromptPackage Convert(object message) => LegacyPromptPackageAdapter.FromLegacyMessages(new[] { message }, 5000, "fixture");
    private static void ExpectFailure<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("malformed getter shape silently produced a partial prompt");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class ReadOnlyMessage
    {
        internal int RoleReads, ContentReads, ExtraReads;
        public string Role { get { RoleReads++; return "assistant"; } }
        public string Content { get { ContentReads++; return "read only"; } }
        public object Extra { get { ExtraReads++; throw new InvalidOperationException("extra getter must not run"); } }
    }
    private sealed class UnknownMessage
    {
        public object Extra => throw new InvalidOperationException("unknown getter must not run");
        public override string ToString() => throw new InvalidOperationException("unknown message must not be serialized");
    }
    private sealed class WrongPropertyType
    {
        internal int Reads;
        public string role { get { Reads++; return "user"; } }
        public object content { get { Reads++; throw new InvalidOperationException("wrong-type content must not be read"); } }
    }
    private sealed class ThrowingMessage
    {
        public string role => "user";
        public string content => throw new InvalidOperationException("owner failed");
    }
    private sealed class AmbiguousMessage
    {
        public string role => "user";
        public string Role => "assistant";
        public string content => "ambiguous";
    }
}
