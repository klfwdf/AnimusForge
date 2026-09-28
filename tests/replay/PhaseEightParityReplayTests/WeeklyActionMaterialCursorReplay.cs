using System;
using System.Collections.Generic;
using System.Reflection;

internal static class WeeklyActionMaterialCursorReplay
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Run(Assembly af)
    {
        Type cursorType = af.GetType("AnimusForge.WeeklyActionMaterialCursor`2", true)
            .MakeGenericType(typeof(string), typeof(string));
        var owners = new List<KeyValuePair<string, List<string>>>();
        for (int i = 0; i < 1000; i++)
            owners.Add(new KeyValuePair<string, List<string>>("missing" + i, new List<string> { "ignored" }));
        owners.Add(new KeyValuePair<string, List<string>>("empty", new List<string>()));
        owners.Add(new KeyValuePair<string, List<string>>("valid", new List<string> { "a", "b" }));
        object cursor = Activator.CreateInstance(cursorType, Members, null, new object[] { owners }, null);
        int resolved = 0;
        var consumed = new List<string>();
        Func<string, string> resolve = id => { resolved++; return id.StartsWith("missing", StringComparison.Ordinal) ? null : id; };
        Action<string, string> consume = (hero, action) => consumed.Add(hero + ":" + action);
        MethodInfo advance = cursorType.GetMethod("Advance", Members);
        bool Step()
        {
            int beforeResolved = resolved, beforeConsumed = consumed.Count;
            bool complete = (bool)advance.Invoke(cursor, new object[] { resolve, consume });
            Check(resolved - beforeResolved <= 1 && consumed.Count - beforeConsumed <= 1,
                "one owner boundary or one action per budget step");
            return complete;
        }
        for (int i = 0; i < 1000; i++) Check(!Step() && consumed.Count == 0, "invalid owner cannot skip unbounded list");
        Check(!Step() && resolved == 1001, "empty owner consumes one step");
        Check(!Step() && consumed.Count == 1 && consumed[0] == "valid:a", "first action");
        Check(!Step() && consumed.Count == 2 && consumed[1] == "valid:b", "resume same hero without replay");
        Check(Step() && resolved == 1002, "complete after last owner boundary");
        Check(Step() && consumed.Count == 2 && resolved == 1002, "completed cursor is idempotent");
        Console.WriteLine("PASS WeeklyActionMaterialCursorReplay bounded-empty-owner/action/resume/complete live=NOT_RUN");
    }

    private static void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException("WeeklyActionMaterialCursorReplay: " + name);
    }
}
